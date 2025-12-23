using Core.Entities.Expense;
using Core.Entities.Ledger;
using Core.Interfaces.Expense;
using Core.Interfaces.Ledger;
using Microsoft.Extensions.Logging;

namespace Application.Services.Expense
{
    /// <summary>
    /// Service for posting expense claim journal entries.
    ///
    /// Implements single-stage journal model following Indian accounting standards:
    /// - On reimbursement: Expense recognition with employee payable
    ///
    /// GST Treatment (as per GST Act 2017):
    /// - Intra-state: CGST + SGST on base amount
    /// - Inter-state: IGST on base amount
    /// - ITC (Input Tax Credit) eligible if proper invoice from registered vendor
    ///
    /// References:
    /// - GST Act 2017 Sections 16-21 (Input Tax Credit)
    /// - ClearTax: https://cleartax.in/s/input-tax-credit-gst
    /// </summary>
    public class ExpensePostingService : IExpensePostingService
    {
        private readonly IExpenseClaimRepository _expenseRepository;
        private readonly IExpenseCategoryRepository _categoryRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly ILogger<ExpensePostingService> _logger;

        /// <summary>
        /// Account codes following standard Indian chart of accounts
        /// </summary>
        private static class AccountCodes
        {
            // Expenses (5xxx)
            public const string GeneralExpenses = "5100";

            // Assets (1xxx) - GST Input Accounts
            public const string CgstInput = "1141";
            public const string SgstInput = "1142";
            public const string IgstInput = "1143";

            // Liabilities (2xxx)
            public const string EmployeeReimbursementPayable = "2102";
        }

        public ExpensePostingService(
            IExpenseClaimRepository expenseRepository,
            IExpenseCategoryRepository categoryRepository,
            IJournalEntryRepository journalRepository,
            IChartOfAccountRepository accountRepository,
            ILogger<ExpensePostingService> logger)
        {
            _expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _journalRepository = journalRepository ?? throw new ArgumentNullException(nameof(journalRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Posts journal entry when expense claim is reimbursed.
        ///
        /// Journal Entry (Without GST):
        /// Dr. Expense Account                      [Total Amount]
        ///     Cr. Employee Reimbursement Payable   [Total Amount]
        ///
        /// Journal Entry (With GST - Intra-State):
        /// Dr. Expense Account                      [Base Amount]
        /// Dr. CGST Input (1141)                    [CGST Amount]
        /// Dr. SGST Input (1142)                    [SGST Amount]
        ///     Cr. Employee Reimbursement Payable   [Total Amount]
        ///
        /// Journal Entry (With GST - Inter-State):
        /// Dr. Expense Account                      [Base Amount]
        /// Dr. IGST Input (1143)                    [IGST Amount]
        ///     Cr. Employee Reimbursement Payable   [Total Amount]
        /// </summary>
        public async Task<JournalEntry?> PostReimbursementAsync(Guid expenseClaimId, Guid? postedBy = null)
        {
            try
            {
                // Idempotency check - prevent duplicate postings
                var idempotencyKey = $"EXPENSE_REIMBURSEMENT_{expenseClaimId}";
                var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "Journal already exists for expense claim {ClaimId}. Returning existing entry {JournalNumber}",
                        expenseClaimId, existing.JournalNumber);
                    return existing;
                }

                // Get expense claim
                var claim = await _expenseRepository.GetByIdAsync(expenseClaimId);
                if (claim == null)
                {
                    _logger.LogWarning("Expense claim {ClaimId} not found", expenseClaimId);
                    return null;
                }

                // Validate status - only reimbursed claims should have journal entry
                if (claim.Status != ExpenseClaimStatus.Reimbursed)
                {
                    _logger.LogWarning(
                        "Expense claim {ClaimId} is not reimbursed. Status: {Status}",
                        expenseClaimId, claim.Status);
                    return null;
                }

                // Get expense category for GL account
                var category = await _categoryRepository.GetByIdAsync(claim.CategoryId);
                var expenseAccountCode = category?.GlAccountCode ?? AccountCodes.GeneralExpenses;

                // Determine journal date
                var journalDate = claim.ReimbursedAt.HasValue
                    ? DateOnly.FromDateTime(claim.ReimbursedAt.Value)
                    : DateOnly.FromDateTime(DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                // Build narration with complete audit trail
                var narration = BuildReimbursementNarration(claim, category?.Name);

                // Create journal entry
                var entry = new JournalEntry
                {
                    CompanyId = claim.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "expense_claim",
                    SourceId = expenseClaimId,
                    SourceNumber = claim.ClaimNumber,
                    Description = $"Expense claim {claim.ClaimNumber}: {claim.Title}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "EXPENSE_REIMBURSEMENT",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Add journal lines
                await AddReimbursementLines(entry, claim, expenseAccountCode);

                // Calculate and validate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry (critical for double-entry accounting)
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "Journal for expense claim {ClaimId} is not balanced. " +
                        "Debit: {Debit:C}, Credit: {Credit:C}. This indicates a calculation error.",
                        expenseClaimId, entry.TotalDebit, entry.TotalCredit);
                    return null;
                }

                // Check if there are any lines
                if (!entry.Lines.Any())
                {
                    _logger.LogError(
                        "No journal lines created for expense claim {ClaimId}. Check if accounts exist.",
                        expenseClaimId);
                    return null;
                }

                // Save journal entry
                var savedEntry = await _journalRepository.AddAsync(entry);

                _logger.LogInformation(
                    "Created journal {JournalNumber} for expense claim {ClaimId}. " +
                    "Amount: {Amount:C}, GST: {GstAmount:C}",
                    savedEntry.JournalNumber, expenseClaimId,
                    claim.Amount, claim.TotalGstAmount);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating journal for expense claim {ClaimId}",
                    expenseClaimId);
                throw;
            }
        }

        public async Task<JournalEntry?> ReverseEntryAsync(
            Guid journalEntryId,
            Guid reversedBy,
            string reason)
        {
            try
            {
                return await _journalRepository.CreateReversalAsync(journalEntryId, reversedBy, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reversing journal entry {JournalEntryId}", journalEntryId);
                throw;
            }
        }

        public async Task<bool> HasJournalEntryAsync(Guid expenseClaimId)
        {
            var idempotencyKey = $"EXPENSE_REIMBURSEMENT_{expenseClaimId}";
            var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            return existing != null;
        }

        public async Task<IEnumerable<JournalEntry>> GetClaimEntriesAsync(Guid expenseClaimId)
        {
            return await _journalRepository.GetBySourceAsync("expense_claim", expenseClaimId);
        }

        public async Task<ExpensePostingSummary> GetPostingSummaryAsync(
            Guid companyId,
            int month,
            int year)
        {
            // Get all reimbursed claims for the company and filter by reimbursement date
            var allReimbursed = await _expenseRepository.GetByStatusAsync(companyId, ExpenseClaimStatus.Reimbursed);
            var claimList = allReimbursed
                .Where(c => c.ReimbursedAt.HasValue &&
                           c.ReimbursedAt.Value.Month == month &&
                           c.ReimbursedAt.Value.Year == year)
                .ToList();

            // Check which ones have journal entries
            var claimsWithJournal = 0;
            foreach (var claim in claimList)
            {
                if (await HasJournalEntryAsync(claim.Id))
                    claimsWithJournal++;
            }

            return new ExpensePostingSummary
            {
                Month = month,
                Year = year,
                TotalClaims = claimList.Count,
                ClaimsWithJournalEntry = claimsWithJournal,
                TotalExpenseAmount = claimList.Sum(c => c.BaseAmount ?? (c.Amount - c.TotalGstAmount)),
                TotalGstAmount = claimList.Sum(c => c.TotalGstAmount),
                TotalReimbursementAmount = claimList.Sum(c => c.Amount)
            };
        }

        // ==================== Private Helper Methods ====================

        private async Task AddReimbursementLines(JournalEntry entry, ExpenseClaim claim, string expenseAccountCode)
        {
            var companyId = claim.CompanyId;

            // DEBIT LINES

            // Calculate base amount (amount before GST)
            var baseAmount = claim.BaseAmount ?? (claim.Amount - claim.TotalGstAmount);

            // 1. Expense Account (base amount or full amount if no GST)
            var expenseAmount = claim.IsGstApplicable ? baseAmount : claim.Amount;
            if (expenseAmount > 0)
            {
                await AddLineIfAccountExists(entry, companyId, expenseAccountCode,
                    expenseAmount, 0,
                    $"Expense: {claim.Title}");
            }

            // 2. GST Input (if applicable)
            if (claim.IsGstApplicable && claim.TotalGstAmount > 0)
            {
                if (claim.SupplyType == "intra_state")
                {
                    // Intra-state: CGST + SGST
                    if (claim.CgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, companyId, AccountCodes.CgstInput,
                            claim.CgstAmount, 0,
                            $"CGST Input @ {claim.CgstRate}% on {claim.InvoiceNumber ?? claim.ClaimNumber}");
                    }

                    if (claim.SgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, companyId, AccountCodes.SgstInput,
                            claim.SgstAmount, 0,
                            $"SGST Input @ {claim.SgstRate}% on {claim.InvoiceNumber ?? claim.ClaimNumber}");
                    }
                }
                else
                {
                    // Inter-state: IGST
                    if (claim.IgstAmount > 0)
                    {
                        await AddLineIfAccountExists(entry, companyId, AccountCodes.IgstInput,
                            claim.IgstAmount, 0,
                            $"IGST Input @ {claim.IgstRate}% on {claim.InvoiceNumber ?? claim.ClaimNumber}");
                    }
                }
            }

            // CREDIT LINE

            // Employee Reimbursement Payable (total amount)
            if (claim.Amount > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployeeReimbursementPayable,
                    0, claim.Amount,
                    $"Payable to {claim.EmployeeName ?? "employee"} for {claim.Title}",
                    "employee", claim.EmployeeId);
            }
        }

        private async Task AddLineIfAccountExists(
            JournalEntry entry,
            Guid companyId,
            string accountCode,
            decimal debitAmount,
            decimal creditAmount,
            string description,
            string? subledgerType = null,
            Guid? subledgerId = null)
        {
            var account = await GetAccountAsync(companyId, accountCode);
            if (account == null)
            {
                _logger.LogWarning(
                    "Account {AccountCode} not found for company {CompanyId}. Skipping line: {Description}",
                    accountCode, companyId, description);
                return;
            }

            entry.Lines!.Add(new JournalEntryLine
            {
                AccountId = account.Id,
                DebitAmount = debitAmount,
                CreditAmount = creditAmount,
                Description = description,
                Currency = "INR",
                ExchangeRate = 1,
                SubledgerType = subledgerType,
                SubledgerId = subledgerId
            });
        }

        private async Task<ChartOfAccount?> GetAccountAsync(Guid companyId, string accountCode)
        {
            return await _accountRepository.GetByCodeAsync(companyId, accountCode);
        }

        private static string BuildReimbursementNarration(ExpenseClaim claim, string? categoryName)
        {
            var narration = $"Being expense claim reimbursement for {claim.Title}. ";
            narration += $"Claim No: {claim.ClaimNumber}. ";
            narration += $"Category: {categoryName ?? "General"}. ";
            narration += $"Expense Date: {claim.ExpenseDate:dd-MMM-yyyy}. ";

            if (!string.IsNullOrEmpty(claim.VendorName))
            {
                narration += $"Vendor: {claim.VendorName}. ";
            }

            if (!string.IsNullOrEmpty(claim.InvoiceNumber))
            {
                narration += $"Invoice: {claim.InvoiceNumber}. ";
            }

            if (claim.IsGstApplicable && claim.TotalGstAmount > 0)
            {
                narration += $"GST Amount: ₹{claim.TotalGstAmount:N2}";
                if (claim.ItcEligible)
                {
                    narration += " (ITC Eligible)";
                }
                narration += ". ";
            }

            narration += $"Total Amount: ₹{claim.Amount:N2}.";

            return narration;
        }

        private static string GetFinancialYear(DateOnly date)
        {
            // Indian financial year: April to March
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static int GetPeriodMonth(DateOnly date)
        {
            // April = 1, March = 12 (Indian financial year)
            return date.Month >= 4 ? date.Month - 3 : date.Month + 9;
        }
    }
}

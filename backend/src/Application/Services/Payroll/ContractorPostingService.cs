using Core.Entities.Ledger;
using Core.Entities.Payroll;
using Core.Interfaces.Ledger;
using Core.Interfaces.Payroll;
using Microsoft.Extensions.Logging;

namespace Application.Services.Payroll
{
    /// <summary>
    /// Service for posting contractor payment journal entries.
    ///
    /// Implements two-stage journal model following Indian accounting standards:
    /// 1. Accrual (on approval) - expense recognition with TDS liability
    /// 2. Disbursement (on payment) - contractor payable settlement
    ///
    /// TDS Treatment (as per Income Tax Act):
    /// - Section 194C: Contractors - 1% (individuals/HUF), 2% (others)
    /// - Section 194J: Professionals - 10%
    /// - TDS is calculated on base amount only (excluding GST)
    /// - Higher rate of 20% applies if PAN not provided
    ///
    /// References:
    /// - ClearTax: https://cleartax.in/s/section-194j
    /// - TaxWink: https://www.taxwink.com/blog/accounting-entries-tds-section-194c
    /// </summary>
    public class ContractorPostingService : IContractorPostingService
    {
        private readonly IContractorPaymentRepository _contractorRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly ILogger<ContractorPostingService> _logger;

        /// <summary>
        /// Account codes following standard Indian chart of accounts
        /// </summary>
        private static class AccountCodes
        {
            // Expenses (5xxx)
            public const string ProfessionalFees = "5550";

            // Assets (1xxx)
            public const string InputGst = "1210";
            public const string DefaultBank = "1112";

            // Liabilities (2xxx)
            public const string ContractorPayable = "2101";
            public const string TradePayables = "2100";  // Fallback if 2101 doesn't exist
            public const string TdsPayableProfessional = "2213";  // Section 194J
            public const string TdsPayableContractor = "2214";    // Section 194C
        }

        public ContractorPostingService(
            IContractorPaymentRepository contractorRepository,
            IJournalEntryRepository journalRepository,
            IChartOfAccountRepository accountRepository,
            ILogger<ContractorPostingService> logger)
        {
            _contractorRepository = contractorRepository ?? throw new ArgumentNullException(nameof(contractorRepository));
            _journalRepository = journalRepository ?? throw new ArgumentNullException(nameof(journalRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Posts accrual journal entry when contractor payment is approved.
        ///
        /// Journal Entry:
        /// Dr. Professional Fees (5550)           [Gross Amount]
        /// Dr. Input GST (1210)                   [GST Amount, if applicable]
        ///     Cr. TDS Payable - 194J/194C        [TDS Amount]
        ///     Cr. Contractor Payable (2101)      [Net Payable]
        /// </summary>
        public async Task<JournalEntry?> PostAccrualAsync(Guid contractorPaymentId, Guid? postedBy = null)
        {
            try
            {
                // Idempotency check - prevent duplicate postings
                var idempotencyKey = $"CONTRACTOR_ACCRUAL_{contractorPaymentId}";
                var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "Accrual journal already exists for contractor payment {PaymentId}. Returning existing entry {JournalNumber}",
                        contractorPaymentId, existing.JournalNumber);
                    return existing;
                }

                // Get contractor payment
                var payment = await _contractorRepository.GetByIdAsync(contractorPaymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Contractor payment {PaymentId} not found", contractorPaymentId);
                    return null;
                }

                // Validate status - only approved or paid payments can have accrual
                if (payment.Status != "approved" && payment.Status != "paid")
                {
                    _logger.LogWarning(
                        "Contractor payment {PaymentId} is not approved or paid. Status: {Status}",
                        contractorPaymentId, payment.Status);
                    return null;
                }

                // Determine journal date
                var journalDate = payment.UpdatedAt.HasValue
                    ? DateOnly.FromDateTime(payment.UpdatedAt.Value)
                    : DateOnly.FromDateTime(DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                // Build narration with complete audit trail
                var narration = BuildAccrualNarration(payment);

                // Create journal entry
                var entry = new JournalEntry
                {
                    CompanyId = payment.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "contractor_payment",
                    SourceId = contractorPaymentId,
                    SourceNumber = payment.InvoiceNumber ?? $"CP-{payment.Id.ToString()[..8]}",
                    Description = $"Contractor payment - {payment.InvoiceNumber ?? "N/A"} - {GetMonthName(payment.PaymentMonth)} {payment.PaymentYear}",
                    Narration = narration,
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "CONTRACTOR_ACCRUAL",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Add journal lines
                await AddAccrualLines(entry, payment);

                // Calculate and validate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced entry (critical for double-entry accounting)
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "Accrual journal for contractor payment {PaymentId} is not balanced. " +
                        "Debit: {Debit:C}, Credit: {Credit:C}. This indicates a calculation error.",
                        contractorPaymentId, entry.TotalDebit, entry.TotalCredit);
                    return null;
                }

                // Save journal entry
                var savedEntry = await _journalRepository.AddAsync(entry);

                // Link journal entry to contractor payment
                payment.AccrualJournalEntryId = savedEntry.Id;
                await _contractorRepository.UpdateAsync(payment);

                _logger.LogInformation(
                    "Created accrual journal {JournalNumber} for contractor payment {PaymentId}. " +
                    "Gross: {Gross:C}, TDS ({TdsSection}): {Tds:C}, Net: {Net:C}",
                    savedEntry.JournalNumber, contractorPaymentId,
                    payment.GrossAmount, payment.TdsSection, payment.TdsAmount, payment.NetPayable);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating accrual journal for contractor payment {PaymentId}",
                    contractorPaymentId);
                throw;
            }
        }

        /// <summary>
        /// Posts disbursement journal entry when contractor is paid.
        ///
        /// Journal Entry:
        /// Dr. Contractor Payable (2101)          [Net Payable]
        ///     Cr. Bank Account (1112)            [Net Payable]
        /// </summary>
        public async Task<JournalEntry?> PostDisbursementAsync(
            Guid contractorPaymentId,
            Guid bankAccountId,
            Guid? postedBy = null)
        {
            try
            {
                // Idempotency check
                var idempotencyKey = $"CONTRACTOR_DISBURSEMENT_{contractorPaymentId}";
                var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "Disbursement journal already exists for contractor payment {PaymentId}",
                        contractorPaymentId);
                    return existing;
                }

                var payment = await _contractorRepository.GetByIdAsync(contractorPaymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Contractor payment {PaymentId} not found", contractorPaymentId);
                    return null;
                }

                // Validate status
                if (payment.Status != "paid")
                {
                    _logger.LogWarning(
                        "Contractor payment {PaymentId} is not marked as paid. Status: {Status}",
                        contractorPaymentId, payment.Status);
                    return null;
                }

                var journalDate = payment.PaymentDate.HasValue
                    ? DateOnly.FromDateTime(payment.PaymentDate.Value)
                    : DateOnly.FromDateTime(DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                var entry = new JournalEntry
                {
                    CompanyId = payment.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "contractor_payment",
                    SourceId = contractorPaymentId,
                    SourceNumber = payment.InvoiceNumber ?? $"CP-{payment.Id.ToString()[..8]}",
                    Description = $"Contractor payment disbursement - {payment.InvoiceNumber ?? "N/A"}",
                    Narration = BuildDisbursementNarration(payment),
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "CONTRACTOR_DISBURSEMENT",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Debit: Contractor Payable (clear liability)
                var contractorPayableAccount = await GetAccountAsync(payment.CompanyId, AccountCodes.ContractorPayable)
                    ?? await GetAccountAsync(payment.CompanyId, AccountCodes.TradePayables);

                if (contractorPayableAccount != null && payment.NetPayable > 0)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = contractorPayableAccount.Id,
                        DebitAmount = payment.NetPayable,
                        CreditAmount = 0,
                        Description = "Clear contractor payable",
                        Currency = "INR",
                        ExchangeRate = 1
                    });
                }

                // Credit: Bank Account
                var bankAccount = await GetAccountAsync(payment.CompanyId, AccountCodes.DefaultBank);
                if (bankAccount != null && payment.NetPayable > 0)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = bankAccount.Id,
                        DebitAmount = 0,
                        CreditAmount = payment.NetPayable,
                        Description = $"Payment to contractor - {payment.PaymentMethod ?? "Bank Transfer"}",
                        Currency = "INR",
                        ExchangeRate = 1,
                        SubledgerType = "bank",
                        SubledgerId = bankAccountId
                    });
                }

                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "Disbursement journal for contractor payment {PaymentId} is not balanced",
                        contractorPaymentId);
                    return null;
                }

                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update contractor payment
                payment.DisbursementJournalEntryId = savedEntry.Id;
                await _contractorRepository.UpdateAsync(payment);

                _logger.LogInformation(
                    "Created disbursement journal {JournalNumber} for contractor payment {PaymentId}. Amount: {Amount:C}",
                    savedEntry.JournalNumber, contractorPaymentId, payment.NetPayable);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating disbursement journal for contractor payment {PaymentId}",
                    contractorPaymentId);
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

        public async Task<bool> HasAccrualEntryAsync(Guid contractorPaymentId)
        {
            var idempotencyKey = $"CONTRACTOR_ACCRUAL_{contractorPaymentId}";
            var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            return existing != null;
        }

        public async Task<bool> HasDisbursementEntryAsync(Guid contractorPaymentId)
        {
            var idempotencyKey = $"CONTRACTOR_DISBURSEMENT_{contractorPaymentId}";
            var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            return existing != null;
        }

        public async Task<IEnumerable<JournalEntry>> GetPaymentEntriesAsync(Guid contractorPaymentId)
        {
            return await _journalRepository.GetBySourceAsync("contractor_payment", contractorPaymentId);
        }

        public async Task<ContractorPostingSummary> GetPostingSummaryAsync(
            Guid companyId,
            int paymentMonth,
            int paymentYear)
        {
            var payments = await _contractorRepository.GetByMonthYearAsync(paymentMonth, paymentYear, companyId);
            var paymentList = payments.ToList();

            return new ContractorPostingSummary
            {
                PaymentMonth = paymentMonth,
                PaymentYear = paymentYear,
                TotalPayments = paymentList.Count,
                PaymentsWithAccrual = paymentList.Count(p => p.AccrualJournalEntryId.HasValue),
                PaymentsWithDisbursement = paymentList.Count(p => p.DisbursementJournalEntryId.HasValue),
                TotalGrossAmount = paymentList.Sum(p => p.GrossAmount),
                TotalTdsAmount = paymentList.Sum(p => p.TdsAmount),
                TotalGstAmount = paymentList.Sum(p => p.GstAmount),
                TotalNetPayable = paymentList.Sum(p => p.NetPayable)
            };
        }

        // ==================== Private Helper Methods ====================

        private async Task AddAccrualLines(JournalEntry entry, ContractorPayment payment)
        {
            var companyId = payment.CompanyId;

            // DEBIT LINES

            // 1. Professional Fees Expense (gross amount)
            if (payment.GrossAmount > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.ProfessionalFees,
                    payment.GrossAmount, 0,
                    $"Professional/contractor fees - {payment.Description ?? payment.InvoiceNumber ?? "N/A"}");
            }

            // 2. Input GST (if applicable and GST is charged)
            // Note: GST input credit can be claimed on contractor invoices
            if (payment.GstApplicable && payment.GstAmount > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.InputGst,
                    payment.GstAmount, 0,
                    $"Input GST on contractor invoice @ {payment.GstRate}%");
            }

            // CREDIT LINES

            // 1. TDS Payable (based on section - 194J or 194C)
            // TDS is deducted on base amount only (excluding GST) as per Indian tax law
            if (payment.TdsAmount > 0)
            {
                var tdsAccountCode = payment.TdsSection switch
                {
                    "194C" => AccountCodes.TdsPayableContractor,
                    "194J" => AccountCodes.TdsPayableProfessional,
                    _ => AccountCodes.TdsPayableProfessional // Default to 194J
                };

                // Try specific account, fallback to general TDS payable (2213)
                var tdsAccount = await GetAccountAsync(companyId, tdsAccountCode)
                    ?? await GetAccountAsync(companyId, AccountCodes.TdsPayableProfessional);

                if (tdsAccount != null)
                {
                    entry.Lines!.Add(new JournalEntryLine
                    {
                        AccountId = tdsAccount.Id,
                        DebitAmount = 0,
                        CreditAmount = payment.TdsAmount,
                        Description = $"TDS u/s {payment.TdsSection} @ {payment.TdsRate}%",
                        Currency = "INR",
                        ExchangeRate = 1
                    });
                }
            }

            // 2. Contractor Payable (net amount payable to contractor)
            if (payment.NetPayable > 0)
            {
                // Try specific contractor payable, fallback to trade payables
                var payableAccount = await GetAccountAsync(companyId, AccountCodes.ContractorPayable)
                    ?? await GetAccountAsync(companyId, AccountCodes.TradePayables);

                if (payableAccount != null)
                {
                    entry.Lines!.Add(new JournalEntryLine
                    {
                        AccountId = payableAccount.Id,
                        DebitAmount = 0,
                        CreditAmount = payment.NetPayable,
                        Description = $"Payable to contractor - Net of TDS",
                        Currency = "INR",
                        ExchangeRate = 1,
                        SubledgerType = "contractor",
                        SubledgerId = payment.EmployeeId
                    });
                }
            }
        }

        private async Task AddLineIfAccountExists(
            JournalEntry entry,
            Guid companyId,
            string accountCode,
            decimal debitAmount,
            decimal creditAmount,
            string description)
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
                ExchangeRate = 1
            });
        }

        private async Task<ChartOfAccount?> GetAccountAsync(Guid companyId, string accountCode)
        {
            return await _accountRepository.GetByCodeAsync(companyId, accountCode);
        }

        private static string BuildAccrualNarration(ContractorPayment payment)
        {
            var narration = $"Being professional/contractor fees recorded for services rendered. ";
            narration += $"Invoice: {payment.InvoiceNumber ?? "N/A"}. ";
            narration += $"Period: {GetMonthName(payment.PaymentMonth)} {payment.PaymentYear}. ";
            narration += $"Gross: ₹{payment.GrossAmount:N2}. ";

            if (payment.GstApplicable && payment.GstAmount > 0)
            {
                narration += $"GST @ {payment.GstRate}%: ₹{payment.GstAmount:N2}. ";
            }

            narration += $"TDS u/s {payment.TdsSection} @ {payment.TdsRate}%: ₹{payment.TdsAmount:N2}. ";
            narration += $"Net payable: ₹{payment.NetPayable:N2}.";

            if (!string.IsNullOrEmpty(payment.ContractorPan))
            {
                narration += $" PAN: {payment.ContractorPan}.";
            }

            if (!string.IsNullOrEmpty(payment.ContractReference))
            {
                narration += $" Contract Ref: {payment.ContractReference}.";
            }

            return narration;
        }

        private static string BuildDisbursementNarration(ContractorPayment payment)
        {
            var narration = $"Being payment made to contractor for invoice {payment.InvoiceNumber ?? "N/A"}. ";
            narration += $"Net amount: ₹{payment.NetPayable:N2}. ";
            narration += $"Payment mode: {payment.PaymentMethod ?? "Bank Transfer"}.";

            if (!string.IsNullOrEmpty(payment.PaymentReference))
            {
                narration += $" Reference: {payment.PaymentReference}.";
            }

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

        private static string GetMonthName(int month)
        {
            return new DateTime(2024, month, 1).ToString("MMMM");
        }
    }
}

using Core.Entities.Ledger;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Ledger;
using Core.Interfaces.Payroll;
using Microsoft.Extensions.Logging;

namespace Application.Services.Payroll
{
    /// <summary>
    /// Service for posting payroll-related journal entries
    /// Implements three-stage journal model:
    /// 1. Accrual (on approval) - expense recognition
    /// 2. Disbursement (on payment) - salary payment to employees
    /// 3. Statutory Remittance (on challan) - TDS/PF/ESI/PT payment to government
    /// </summary>
    public class PayrollPostingService : IPayrollPostingService
    {
        private readonly IPayrollRunRepository _payrollRunRepository;
        private readonly IPayrollTransactionRepository _transactionRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly IStatutoryPaymentRepository _statutoryRepository;
        private readonly ILogger<PayrollPostingService> _logger;

        // Account codes - following COA structure
        private static class AccountCodes
        {
            // Expenses (5xxx)
            public const string SalariesAndWages = "5210";
            public const string EmployerPF = "5220";
            public const string EmployerESI = "5230";
            public const string GratuityExpense = "5250";
            public const string BonusExpense = "5260";
            public const string InterestOnDelayedPayments = "5620";

            // Liabilities - Payroll (2xxx)
            public const string SalaryPayable = "2110";
            public const string TdsPayableSalary = "2212";
            public const string EmployeePfPayable = "2221";
            public const string EmployerPfPayable = "2222";
            public const string EmployeeEsiPayable = "2231";
            public const string EmployerEsiPayable = "2232";
            public const string PtPayable = "2240";
            public const string LwfPayable = "2245";
            public const string GratuityPayable = "2250";

            // Assets
            public const string DefaultBank = "1112";
        }

        public PayrollPostingService(
            IPayrollRunRepository payrollRunRepository,
            IPayrollTransactionRepository transactionRepository,
            IJournalEntryRepository journalRepository,
            IChartOfAccountRepository accountRepository,
            IStatutoryPaymentRepository statutoryRepository,
            ILogger<PayrollPostingService> logger)
        {
            _payrollRunRepository = payrollRunRepository;
            _transactionRepository = transactionRepository;
            _journalRepository = journalRepository;
            _accountRepository = accountRepository;
            _statutoryRepository = statutoryRepository;
            _logger = logger;
        }

        /// <summary>
        /// Posts accrual journal entry when payroll is approved
        /// </summary>
        public async Task<JournalEntry?> PostAccrualAsync(Guid payrollRunId, Guid? postedBy = null)
        {
            try
            {
                // Idempotency check
                var idempotencyKey = $"PAYROLL_ACCRUAL_{payrollRunId}";
                var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "Accrual journal already exists for payroll run {PayrollRunId}. Returning existing entry {JournalNumber}",
                        payrollRunId, existing.JournalNumber);
                    return existing;
                }

                // Get payroll run
                var payrollRun = await _payrollRunRepository.GetByIdAsync(payrollRunId);
                if (payrollRun == null)
                {
                    _logger.LogWarning("Payroll run {PayrollRunId} not found", payrollRunId);
                    return null;
                }

                // Validate status
                if (payrollRun.Status != "approved" && payrollRun.Status != "paid")
                {
                    _logger.LogWarning(
                        "Payroll run {PayrollRunId} is not approved or paid. Status: {Status}",
                        payrollRunId, payrollRun.Status);
                    return null;
                }

                // Get transactions for aggregation
                var transactions = await _transactionRepository.GetByPayrollRunIdAsync(payrollRunId);
                var transactionList = transactions.ToList();

                if (!transactionList.Any())
                {
                    _logger.LogWarning("No transactions found for payroll run {PayrollRunId}", payrollRunId);
                    return null;
                }

                // Aggregate amounts
                var aggregates = CalculateAggregates(transactionList);

                // Determine journal date and period
                var journalDate = payrollRun.ApprovedAt.HasValue
                    ? DateOnly.FromDateTime(payrollRun.ApprovedAt.Value)
                    : DateOnly.FromDateTime(DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                // Create journal entry
                var entry = new JournalEntry
                {
                    CompanyId = payrollRun.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "payroll_run",
                    SourceId = payrollRunId,
                    SourceNumber = $"PR-{payrollRun.PayrollYear}-{payrollRun.PayrollMonth:D2}",
                    Description = $"Salary accrual for {GetMonthName(payrollRun.PayrollMonth)} {payrollRun.PayrollYear}",
                    Narration = $"Being salary and statutory contributions accrued for the month of {GetMonthName(payrollRun.PayrollMonth)} {payrollRun.PayrollYear}. " +
                               $"Total employees: {transactionList.Count}. Payroll run: {payrollRunId}",
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "PAYROLL_ACCRUAL",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Build journal lines
                await AddAccrualLines(entry, payrollRun.CompanyId, aggregates);

                // Calculate totals
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "Accrual journal for payroll {PayrollRunId} is not balanced. Debit: {Debit}, Credit: {Credit}",
                        payrollRunId, entry.TotalDebit, entry.TotalCredit);
                    return null;
                }

                // Save journal entry
                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update payroll run with journal reference
                payrollRun.AccrualJournalEntryId = savedEntry.Id;
                await _payrollRunRepository.UpdateAsync(payrollRun);

                _logger.LogInformation(
                    "Created accrual journal {JournalNumber} for payroll run {PayrollRunId}. " +
                    "Total: {Total:C}",
                    savedEntry.JournalNumber, payrollRunId, entry.TotalDebit);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating accrual journal for payroll run {PayrollRunId}",
                    payrollRunId);
                throw;
            }
        }

        /// <summary>
        /// Posts disbursement journal entry when salaries are paid
        /// </summary>
        public async Task<JournalEntry?> PostDisbursementAsync(
            Guid payrollRunId,
            Guid bankAccountId,
            Guid? postedBy = null)
        {
            try
            {
                // Idempotency check
                var idempotencyKey = $"PAYROLL_DISBURSEMENT_{payrollRunId}";
                var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "Disbursement journal already exists for payroll run {PayrollRunId}",
                        payrollRunId);
                    return existing;
                }

                var payrollRun = await _payrollRunRepository.GetByIdAsync(payrollRunId);
                if (payrollRun == null)
                {
                    _logger.LogWarning("Payroll run {PayrollRunId} not found", payrollRunId);
                    return null;
                }

                if (payrollRun.Status != "paid")
                {
                    _logger.LogWarning(
                        "Payroll run {PayrollRunId} is not marked as paid. Status: {Status}",
                        payrollRunId, payrollRun.Status);
                    return null;
                }

                // Use default bank account code for journal entry
                // The bankAccountId is tracked for reference but GL posting uses the standard bank account
                var bankAccountCode = AccountCodes.DefaultBank;

                var journalDate = payrollRun.PaidAt.HasValue
                    ? DateOnly.FromDateTime(payrollRun.PaidAt.Value)
                    : DateOnly.FromDateTime(DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                var entry = new JournalEntry
                {
                    CompanyId = payrollRun.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "payroll_run",
                    SourceId = payrollRunId,
                    SourceNumber = $"PR-{payrollRun.PayrollYear}-{payrollRun.PayrollMonth:D2}",
                    Description = $"Salary payment for {GetMonthName(payrollRun.PayrollMonth)} {payrollRun.PayrollYear}",
                    Narration = $"Being net salary paid to employees for {GetMonthName(payrollRun.PayrollMonth)} {payrollRun.PayrollYear} " +
                               $"via {payrollRun.PaymentMode ?? "bank transfer"}. Reference: {payrollRun.PaymentReference ?? "N/A"}",
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "PAYROLL_DISBURSEMENT",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                var netSalary = payrollRun.TotalNetSalary;

                // Debit: Salary Payable (clear liability)
                var salaryPayableAccount = await _accountRepository
                    .GetByCodeAsync(payrollRun.CompanyId, AccountCodes.SalaryPayable);
                if (salaryPayableAccount != null && netSalary > 0)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = salaryPayableAccount.Id,
                        DebitAmount = netSalary,
                        CreditAmount = 0,
                        Description = "Clear net salary payable",
                        Currency = "INR",
                        ExchangeRate = 1
                    });
                }

                // Credit: Bank Account
                var bankAcc = await _accountRepository
                    .GetByCodeAsync(payrollRun.CompanyId, bankAccountCode);
                if (bankAcc != null && netSalary > 0)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = bankAcc.Id,
                        DebitAmount = 0,
                        CreditAmount = netSalary,
                        Description = $"Salary payment - {payrollRun.PaymentMode ?? "Bank Transfer"}",
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
                        "Disbursement journal for payroll {PayrollRunId} is not balanced",
                        payrollRunId);
                    return null;
                }

                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update payroll run
                payrollRun.DisbursementJournalEntryId = savedEntry.Id;
                await _payrollRunRepository.UpdateAsync(payrollRun);

                _logger.LogInformation(
                    "Created disbursement journal {JournalNumber} for payroll run {PayrollRunId}",
                    savedEntry.JournalNumber, payrollRunId);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating disbursement journal for payroll run {PayrollRunId}",
                    payrollRunId);
                throw;
            }
        }

        /// <summary>
        /// Posts statutory payment journal entry (TDS/PF/ESI/PT challan)
        /// </summary>
        public async Task<JournalEntry?> PostStatutoryPaymentAsync(
            Guid statutoryPaymentId,
            Guid? bankAccountId = null,
            Guid? postedBy = null)
        {
            try
            {
                var idempotencyKey = $"STATUTORY_PAYMENT_{statutoryPaymentId}";
                var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "Statutory payment journal already exists for {PaymentId}",
                        statutoryPaymentId);
                    return existing;
                }

                var payment = await _statutoryRepository.GetByIdAsync(statutoryPaymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Statutory payment {PaymentId} not found", statutoryPaymentId);
                    return null;
                }

                if (payment.Status != "paid")
                {
                    _logger.LogWarning(
                        "Statutory payment {PaymentId} is not marked as paid. Status: {Status}",
                        statutoryPaymentId, payment.Status);
                    return null;
                }

                // Get payment type details
                var paymentType = await _statutoryRepository.GetPaymentTypeAsync(payment.PaymentType);
                var paymentTypeName = paymentType?.Name ?? payment.PaymentType;

                var journalDate = payment.PaymentDate ?? DateOnly.FromDateTime(DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                var entry = new JournalEntry
                {
                    CompanyId = payment.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "statutory_payment",
                    SourceId = statutoryPaymentId,
                    SourceNumber = payment.ReferenceNumber ?? $"STAT-{payment.Id.ToString()[..8]}",
                    Description = $"{paymentTypeName} for period {payment.PeriodMonth}/{payment.PeriodYear}",
                    Narration = BuildStatutoryNarration(payment, paymentTypeName),
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "STATUTORY_REMITTANCE",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Determine payable account based on payment type
                var payableAccountCode = GetPayableAccountCode(payment.PaymentType);
                var payableAccount = await _accountRepository
                    .GetByCodeAsync(payment.CompanyId, payableAccountCode);

                if (payableAccount != null && payment.PrincipalAmount > 0)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = payableAccount.Id,
                        DebitAmount = payment.PrincipalAmount,
                        CreditAmount = 0,
                        Description = $"Clear {paymentTypeName} payable",
                        Currency = "INR",
                        ExchangeRate = 1
                    });
                }

                // Add interest expense line if applicable
                if (payment.InterestAmount > 0)
                {
                    var interestAccount = await _accountRepository
                        .GetByCodeAsync(payment.CompanyId, AccountCodes.InterestOnDelayedPayments);
                    if (interestAccount != null)
                    {
                        entry.Lines.Add(new JournalEntryLine
                        {
                            AccountId = interestAccount.Id,
                            DebitAmount = payment.InterestAmount,
                            CreditAmount = 0,
                            Description = $"Interest/penalty on late {payment.PaymentType} payment",
                            Currency = "INR",
                            ExchangeRate = 1
                        });
                    }
                }

                // Credit bank - use default bank account
                // The specific bank account is tracked in the payment record for reference
                var bankAccountCode = AccountCodes.DefaultBank;

                var bankAcc = await _accountRepository
                    .GetByCodeAsync(payment.CompanyId, bankAccountCode);
                if (bankAcc != null)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = bankAcc.Id,
                        DebitAmount = 0,
                        CreditAmount = payment.TotalAmount,
                        Description = $"{payment.PaymentType} payment - Ref: {payment.ReferenceNumber ?? payment.BankReference ?? "N/A"}",
                        Currency = "INR",
                        ExchangeRate = 1
                    });
                }

                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "Statutory payment journal for {PaymentId} is not balanced. Debit: {Debit}, Credit: {Credit}",
                        statutoryPaymentId, entry.TotalDebit, entry.TotalCredit);
                    return null;
                }

                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update statutory payment
                payment.JournalEntryId = savedEntry.Id;
                await _statutoryRepository.UpdateAsync(payment);

                _logger.LogInformation(
                    "Created statutory payment journal {JournalNumber} for payment {PaymentId}",
                    savedEntry.JournalNumber, statutoryPaymentId);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating statutory payment journal for {PaymentId}",
                    statutoryPaymentId);
                throw;
            }
        }

        public async Task<JournalEntry?> ReversePayrollEntryAsync(
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

        public async Task<bool> HasAccrualEntryAsync(Guid payrollRunId)
        {
            var idempotencyKey = $"PAYROLL_ACCRUAL_{payrollRunId}";
            var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            return existing != null;
        }

        public async Task<bool> HasDisbursementEntryAsync(Guid payrollRunId)
        {
            var idempotencyKey = $"PAYROLL_DISBURSEMENT_{payrollRunId}";
            var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            return existing != null;
        }

        public async Task<IEnumerable<JournalEntry>> GetPayrollEntriesAsync(Guid payrollRunId)
        {
            return await _journalRepository.GetBySourceAsync("payroll_run", payrollRunId);
        }

        public async Task<PayrollPostingSummary?> GetPostingSummaryAsync(
            Guid companyId,
            int payrollMonth,
            int payrollYear)
        {
            var payrollRun = await _payrollRunRepository.GetByCompanyAndMonthAsync(
                companyId, payrollMonth, payrollYear);

            if (payrollRun == null)
                return null;

            var summary = new PayrollPostingSummary
            {
                PayrollRunId = payrollRun.Id,
                PayrollMonth = payrollMonth,
                PayrollYear = payrollYear,
                FinancialYear = payrollRun.FinancialYear,
                PayrollStatus = payrollRun.Status,
                TotalGrossSalary = payrollRun.TotalGrossSalary,
                TotalNetSalary = payrollRun.TotalNetSalary,
                TotalDeductions = payrollRun.TotalDeductions,
                TotalEmployerCost = payrollRun.TotalEmployerCost
            };

            // Get accrual entry info
            if (payrollRun.AccrualJournalEntryId.HasValue)
            {
                var accrualEntry = await _journalRepository.GetByIdAsync(payrollRun.AccrualJournalEntryId.Value);
                if (accrualEntry != null)
                {
                    summary.HasAccrualEntry = true;
                    summary.AccrualJournalEntryId = accrualEntry.Id;
                    summary.AccrualJournalNumber = accrualEntry.JournalNumber;
                    summary.AccrualDate = accrualEntry.JournalDate;
                }
            }

            // Get disbursement entry info
            if (payrollRun.DisbursementJournalEntryId.HasValue)
            {
                var disbursementEntry = await _journalRepository.GetByIdAsync(payrollRun.DisbursementJournalEntryId.Value);
                if (disbursementEntry != null)
                {
                    summary.HasDisbursementEntry = true;
                    summary.DisbursementJournalEntryId = disbursementEntry.Id;
                    summary.DisbursementJournalNumber = disbursementEntry.JournalNumber;
                    summary.DisbursementDate = disbursementEntry.JournalDate;
                }
            }

            return summary;
        }

        // ==================== Private Helper Methods ====================

        private PayrollAggregates CalculateAggregates(IEnumerable<PayrollTransaction> transactions)
        {
            var list = transactions.ToList();
            return new PayrollAggregates
            {
                TotalGrossSalary = list.Sum(t => t.GrossEarnings),
                TotalNetSalary = list.Sum(t => t.NetPayable),
                TotalTds = list.Sum(t => t.TdsDeducted),
                TotalEmployeePf = list.Sum(t => t.PfEmployee),
                TotalEmployerPf = list.Sum(t => t.PfEmployer + t.PfAdminCharges),
                TotalEmployeeEsi = list.Sum(t => t.EsiEmployee),
                TotalEmployerEsi = list.Sum(t => t.EsiEmployer),
                TotalProfessionalTax = list.Sum(t => t.ProfessionalTax),
                TotalGratuity = list.Sum(t => t.GratuityProvision),
                TotalBonus = list.Sum(t => t.BonusPaid),
                TotalReimbursements = list.Sum(t => t.Reimbursements),
                TotalLoanRecovery = list.Sum(t => t.LoanRecovery),
                TotalAdvanceRecovery = list.Sum(t => t.AdvanceRecovery)
            };
        }

        private async Task AddAccrualLines(
            JournalEntry entry,
            Guid companyId,
            PayrollAggregates agg)
        {
            // DEBIT LINES (Expenses)

            // 1. Salaries and Wages Expense
            if (agg.TotalGrossSalary > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.SalariesAndWages,
                    agg.TotalGrossSalary, 0, "Salaries and wages expense");
            }

            // 2. Employer PF Contribution Expense
            if (agg.TotalEmployerPf > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployerPF,
                    agg.TotalEmployerPf, 0, "Employer PF contribution expense");
            }

            // 3. Employer ESI Contribution Expense
            if (agg.TotalEmployerEsi > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployerESI,
                    agg.TotalEmployerEsi, 0, "Employer ESI contribution expense");
            }

            // 4. Gratuity Expense
            if (agg.TotalGratuity > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.GratuityExpense,
                    agg.TotalGratuity, 0, "Gratuity provision for the month");
            }

            // CREDIT LINES (Liabilities)

            // 1. Net Salary Payable
            if (agg.TotalNetSalary > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.SalaryPayable,
                    0, agg.TotalNetSalary, "Net salary payable to employees");
            }

            // 2. TDS Payable - Salary (Section 192)
            if (agg.TotalTds > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.TdsPayableSalary,
                    0, agg.TotalTds, "TDS on salary (Section 192)");
            }

            // 3. Employee PF Contribution Payable
            if (agg.TotalEmployeePf > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployeePfPayable,
                    0, agg.TotalEmployeePf, "Employee PF contribution payable");
            }

            // 4. Employer PF Contribution Payable
            if (agg.TotalEmployerPf > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployerPfPayable,
                    0, agg.TotalEmployerPf, "Employer PF contribution payable");
            }

            // 5. Employee ESI Contribution Payable
            if (agg.TotalEmployeeEsi > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployeeEsiPayable,
                    0, agg.TotalEmployeeEsi, "Employee ESI contribution payable");
            }

            // 6. Employer ESI Contribution Payable
            if (agg.TotalEmployerEsi > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployerEsiPayable,
                    0, agg.TotalEmployerEsi, "Employer ESI contribution payable");
            }

            // 7. Professional Tax Payable
            if (agg.TotalProfessionalTax > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.PtPayable,
                    0, agg.TotalProfessionalTax, "Professional tax payable");
            }

            // 8. Gratuity Payable
            if (agg.TotalGratuity > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.GratuityPayable,
                    0, agg.TotalGratuity, "Gratuity provision payable");
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
            var account = await _accountRepository.GetByCodeAsync(companyId, accountCode);
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

        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static int GetPeriodMonth(DateOnly date)
        {
            // April = 1, March = 12
            return date.Month >= 4 ? date.Month - 3 : date.Month + 9;
        }

        private static string GetMonthName(int month)
        {
            return new DateTime(2024, month, 1).ToString("MMMM");
        }

        private static string BuildStatutoryNarration(StatutoryPayment payment, string paymentTypeName)
        {
            var narration = $"Being {paymentTypeName} remitted for the period {payment.PeriodMonth}/{payment.PeriodYear}.";

            if (!string.IsNullOrEmpty(payment.ReferenceNumber))
                narration += $" Challan: {payment.ReferenceNumber}.";

            if (!string.IsNullOrEmpty(payment.BankReference))
                narration += $" Bank Ref: {payment.BankReference}.";

            if (!string.IsNullOrEmpty(payment.Trrn))
                narration += $" TRRN: {payment.Trrn}.";

            if (!string.IsNullOrEmpty(payment.BsrCode))
                narration += $" BSR: {payment.BsrCode}.";

            if (!string.IsNullOrEmpty(payment.ReceiptNumber))
                narration += $" CIN: {payment.ReceiptNumber}.";

            return narration;
        }

        private static string GetPayableAccountCode(string paymentType)
        {
            return paymentType switch
            {
                "TDS_192" => AccountCodes.TdsPayableSalary,
                "TDS_194C" or "TDS_194J" => "2213", // TDS Payable - Professional
                "PF" => "2220", // Parent PF Payable
                "ESI" => "2230", // Parent ESI Payable
                "LWF" or "LWF_KA" or "LWF_MH" => AccountCodes.LwfPayable,
                _ when paymentType.StartsWith("PT_") => AccountCodes.PtPayable,
                _ => "2200" // Other current liabilities
            };
        }
    }

    /// <summary>
    /// Aggregate amounts from payroll transactions
    /// </summary>
    internal class PayrollAggregates
    {
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalNetSalary { get; set; }
        public decimal TotalTds { get; set; }
        public decimal TotalEmployeePf { get; set; }
        public decimal TotalEmployerPf { get; set; }
        public decimal TotalEmployeeEsi { get; set; }
        public decimal TotalEmployerEsi { get; set; }
        public decimal TotalProfessionalTax { get; set; }
        public decimal TotalGratuity { get; set; }
        public decimal TotalBonus { get; set; }
        public decimal TotalReimbursements { get; set; }
        public decimal TotalLoanRecovery { get; set; }
        public decimal TotalAdvanceRecovery { get; set; }
    }
}

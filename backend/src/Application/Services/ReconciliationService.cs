using Application.Common;
using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Core.Entities;
using Core.Entities.Ledger;
using Core.Interfaces;
using Core.Interfaces.Ledger;
using Core.Common;

namespace Application.Services
{
    /// <summary>
    /// Service for bank transaction reconciliation operations
    /// </summary>
    public class ReconciliationService : IReconciliationService
    {
        private readonly IBankTransactionRepository _repository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IJournalEntryRepository _journalEntryRepository;
        private readonly IChartOfAccountRepository _chartOfAccountRepository;

        // Account codes for adjustment entries (matching your Chart of Accounts)
        private static class AccountCodes
        {
            public const string TdsReceivable = "1130";       // TDS Receivable
            public const string BankCharges = "5580";         // Bank Charges
            public const string InterestIncome = "4510";      // Interest Income
            public const string ForexGain = "4540";           // Foreign Exchange Gain
            public const string ForexLoss = "5630";           // Foreign Exchange Loss
            public const string RoundOff = "5500";            // Other Expenses (for round-off)
            public const string OtherIncome = "4550";         // Miscellaneous Income
            public const string OtherExpense = "5500";        // Other Expenses
            public const string SuspenseAccount = "2900";     // Suspense Account (if exists)
            public const string BankAccount = "1112";         // Bank Accounts - Current
            public const string TradeReceivables = "1120";    // Trade Receivables
        }

        public ReconciliationService(
            IBankTransactionRepository repository,
            IBankAccountRepository bankAccountRepository,
            IJournalEntryRepository journalEntryRepository,
            IChartOfAccountRepository chartOfAccountRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _bankAccountRepository = bankAccountRepository ?? throw new ArgumentNullException(nameof(bankAccountRepository));
            _journalEntryRepository = journalEntryRepository ?? throw new ArgumentNullException(nameof(journalEntryRepository));
            _chartOfAccountRepository = chartOfAccountRepository ?? throw new ArgumentNullException(nameof(chartOfAccountRepository));
        }

        /// <inheritdoc />
        public async Task<Result> ReconcileTransactionAsync(Guid transactionId, ReconcileTransactionDto dto)
        {
            var validation = ServiceExtensions.ValidateGuid(transactionId);
            if (validation.IsFailure)
                return validation.Error!;

            if (dto == null)
                return Error.Validation("Reconciliation data is required");

            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                return Error.NotFound($"Bank transaction with ID {transactionId} not found");

            if (transaction.IsReconciled)
                return Error.Validation("Transaction is already reconciled");

            // Get bank account for company ID
            var bankAccount = await _bankAccountRepository.GetByIdAsync(transaction.BankAccountId);
            if (bankAccount == null || !bankAccount.CompanyId.HasValue)
                return Error.Validation("Bank account not found or not linked to a company");

            var companyId = bankAccount.CompanyId.Value;

            // Validate reconciled type
            var validTypes = new[] { "payment", "expense", "payroll", "tax_payment", "transfer", "contractor", "salary", "expense_claim", "subscription", "loan_payment", "asset_maintenance" };
            if (!validTypes.Contains(dto.ReconciledType.ToLower()))
                return Error.Validation($"Invalid reconciled type. Must be one of: {string.Join(", ", validTypes)}");

            // Validate difference type if provided
            Guid? adjustmentJournalId = null;
            if (!string.IsNullOrEmpty(dto.DifferenceType) && dto.DifferenceAmount.HasValue && Math.Abs(dto.DifferenceAmount.Value) > 0.01m)
            {
                var validDifferenceTypes = new[] {
                    "bank_interest", "bank_charges", "tds_deducted", "round_off",
                    "forex_gain", "forex_loss", "other_income", "other_expense", "suspense"
                };
                if (!validDifferenceTypes.Contains(dto.DifferenceType.ToLower()))
                    return Error.Validation($"Invalid difference type. Must be one of: {string.Join(", ", validDifferenceTypes)}");

                // Create adjustment journal entry for the difference
                var journalResult = await CreateAdjustmentJournalEntryAsync(
                    companyId,
                    transaction,
                    dto.DifferenceAmount.Value,
                    dto.DifferenceType,
                    dto.DifferenceNotes,
                    dto.TdsSection);

                if (journalResult.IsFailure)
                    return journalResult.Error!;

                adjustmentJournalId = journalResult.Value;
            }

            await _repository.ReconcileTransactionAsync(
                transactionId,
                dto.ReconciledType,
                dto.ReconciledId,
                dto.ReconciledBy,
                dto.DifferenceAmount,
                dto.DifferenceType,
                dto.DifferenceNotes,
                dto.TdsSection,
                adjustmentJournalId
            );

            return Result.Success();
        }

        /// <summary>
        /// Create adjustment journal entry for reconciliation differences per ICAI guidelines
        /// </summary>
        private async Task<Result<Guid>> CreateAdjustmentJournalEntryAsync(
            Guid companyId,
            BankTransaction transaction,
            decimal differenceAmount,
            string differenceType,
            string? notes,
            string? tdsSection)
        {
            // Determine accounts based on difference type
            var (debitAccountCode, creditAccountCode, description) = GetAccountsForDifferenceType(
                differenceType, differenceAmount, transaction.TransactionType, tdsSection);

            // Get bank account for the transaction (ledger link is optional for now)

            // Get the accounts
            var debitAccount = await _chartOfAccountRepository.GetByCodeAsync(companyId, debitAccountCode);
            var creditAccount = await _chartOfAccountRepository.GetByCodeAsync(companyId, creditAccountCode);

            // If account codes don't exist, try to use generic suspense
            if (debitAccount == null || creditAccount == null)
            {
                // Log warning but don't fail - use suspense account
                var suspenseAccount = await _chartOfAccountRepository.GetByCodeAsync(companyId, AccountCodes.SuspenseAccount);
                if (suspenseAccount == null)
                {
                    // Create the journal entry without account links - will need manual adjustment
                    return Error.Validation($"Required accounts not found in Chart of Accounts. Please create accounts with codes: {debitAccountCode}, {creditAccountCode}");
                }

                if (debitAccount == null) debitAccount = suspenseAccount;
                if (creditAccount == null) creditAccount = suspenseAccount;
            }

            var absAmount = Math.Abs(differenceAmount);
            var financialYear = GetFinancialYear(transaction.TransactionDate);

            // Generate journal number
            var journalNumber = await _journalEntryRepository.GenerateNextNumberAsync(companyId, financialYear);

            var journalEntry = new JournalEntry
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                JournalNumber = journalNumber,
                JournalDate = transaction.TransactionDate,
                FinancialYear = financialYear,
                PeriodMonth = GetPeriodMonth(transaction.TransactionDate),
                EntryType = "adjustment",
                SourceType = "bank_reconciliation",
                SourceId = transaction.Id,
                SourceNumber = transaction.ReferenceNumber,
                Description = $"Bank Reconciliation Adjustment: {description}" +
                    (string.IsNullOrEmpty(notes) ? "" : $" - {notes}"),
                TotalDebit = absAmount,
                TotalCredit = absAmount,
                Status = "posted", // Auto-post adjustment entries
                PostedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdJournal = await _journalEntryRepository.AddAsync(journalEntry);

            // Create journal lines
            var lines = new List<JournalEntryLine>
            {
                new JournalEntryLine
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = createdJournal.Id,
                    AccountId = debitAccount.Id,
                    LineNumber = 1,
                    DebitAmount = absAmount,
                    CreditAmount = 0,
                    Currency = "INR",
                    ExchangeRate = 1,
                    Description = $"Debit: {description}"
                },
                new JournalEntryLine
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = createdJournal.Id,
                    AccountId = creditAccount.Id,
                    LineNumber = 2,
                    DebitAmount = 0,
                    CreditAmount = absAmount,
                    Currency = "INR",
                    ExchangeRate = 1,
                    Description = $"Credit: {description}"
                }
            };

            await _journalEntryRepository.AddLinesAsync(createdJournal.Id, lines);

            return Result<Guid>.Success(createdJournal.Id);
        }

        /// <summary>
        /// Determine debit and credit accounts based on difference type per ICAI standards
        /// </summary>
        private (string debitAccountCode, string creditAccountCode, string description) GetAccountsForDifferenceType(
            string differenceType, decimal differenceAmount, string transactionType, string? tdsSection)
        {
            // Positive difference = Bank received more than expected (for credits) or paid less than expected (for debits)
            // Negative difference = Bank received less than expected (for credits) or paid more than expected (for debits)

            var isCredit = transactionType == "credit";
            var bankReceivedMore = (isCredit && differenceAmount > 0) || (!isCredit && differenceAmount < 0);

            return differenceType.ToLower() switch
            {
                "bank_interest" => (
                    AccountCodes.BankAccount,
                    AccountCodes.InterestIncome,
                    "Interest credited by bank"
                ),

                "bank_charges" => (
                    AccountCodes.BankCharges,
                    AccountCodes.BankAccount,
                    "Bank charges/fees deducted"
                ),

                "tds_deducted" => (
                    AccountCodes.TdsReceivable,
                    AccountCodes.TradeReceivables,
                    $"TDS deducted by customer u/s {tdsSection ?? "N/A"}"
                ),

                "round_off" => bankReceivedMore
                    ? (AccountCodes.BankAccount, AccountCodes.RoundOff, "Round off adjustment (gain)")
                    : (AccountCodes.RoundOff, AccountCodes.BankAccount, "Round off adjustment (loss)"),

                "forex_gain" => (
                    AccountCodes.BankAccount,
                    AccountCodes.ForexGain,
                    "Foreign exchange gain on realization"
                ),

                "forex_loss" => (
                    AccountCodes.ForexLoss,
                    AccountCodes.BankAccount,
                    "Foreign exchange loss on realization"
                ),

                "other_income" => (
                    AccountCodes.BankAccount,
                    AccountCodes.OtherIncome,
                    "Other income adjustment"
                ),

                "other_expense" => (
                    AccountCodes.OtherExpense,
                    AccountCodes.BankAccount,
                    "Other expense adjustment"
                ),

                "suspense" or _ => (
                    AccountCodes.SuspenseAccount,
                    AccountCodes.BankAccount,
                    "Parked for investigation - needs resolution"
                )
            };
        }

        private static string GetFinancialYear(DateOnly date)
        {
            // Indian FY is April to March
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static int GetPeriodMonth(DateOnly date)
        {
            // April = 1, March = 12
            return date.Month >= 4 ? date.Month - 3 : date.Month + 9;
        }

        /// <inheritdoc />
        public async Task<Result> UnreconcileTransactionAsync(Guid transactionId)
        {
            var validation = ServiceExtensions.ValidateGuid(transactionId);
            if (validation.IsFailure)
                return validation.Error!;

            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                return Error.NotFound($"Bank transaction with ID {transactionId} not found");

            if (!transaction.IsReconciled)
                return Error.Validation("Transaction is not reconciled");

            // If there's an adjustment journal entry, we should reverse it
            // For now, we'll just unreconcile - journal reversal can be added later
            // TODO: Reverse adjustment journal entry if exists

            await _repository.UnreconcileTransactionAsync(transactionId);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<ReconciliationSuggestionDto>>> GetReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 1000m, int maxResults = 10)
        {
            var validation = ServiceExtensions.ValidateGuid(transactionId);
            if (validation.IsFailure)
                return validation.Error!;

            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                return Error.NotFound($"Bank transaction with ID {transactionId} not found");

            var payments = await _repository.GetReconciliationSuggestionsAsync(transactionId, tolerance, maxResults);

            var suggestions = payments.Select(p => new ReconciliationSuggestionDto
            {
                PaymentId = p.Id,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                ReferenceNumber = p.ReferenceNumber,
                CustomerName = p.CustomerName ?? p.CustomerCompany,
                InvoiceNumber = p.InvoiceNumber,
                MatchScore = CalculateMatchScoreForPayment(transaction, p),
                AmountDifference = Math.Abs(transaction.Amount - p.Amount),
                DateDifferenceInDays = Math.Abs((transaction.TransactionDate.ToDateTime(TimeOnly.MinValue) -
                    p.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days)
            }).OrderByDescending(s => s.MatchScore).ToList();

            return Result<IEnumerable<ReconciliationSuggestionDto>>.Success(suggestions);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<ReconciliationSuggestionDto>>> SearchPaymentsAsync(
            Guid companyId,
            string? searchTerm = null,
            decimal? amountMin = null,
            decimal? amountMax = null,
            int maxResults = 20)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var payments = await _repository.SearchPaymentsAsync(companyId, searchTerm, amountMin, amountMax, maxResults);

            var suggestions = payments.Select(p => new ReconciliationSuggestionDto
            {
                PaymentId = p.Id,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                ReferenceNumber = p.ReferenceNumber,
                CustomerName = p.CustomerName ?? p.CustomerCompany,
                InvoiceNumber = p.InvoiceNumber,
                MatchScore = 0,
                AmountDifference = 0,
                DateDifferenceInDays = 0
            }).ToList();

            return Result<IEnumerable<ReconciliationSuggestionDto>>.Success(suggestions);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<DebitReconciliationSuggestionDto>>> GetDebitReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 1000m, int maxResults = 10)
        {
            var validation = ServiceExtensions.ValidateGuid(transactionId);
            if (validation.IsFailure)
                return validation.Error!;

            var transaction = await _repository.GetByIdAsync(transactionId);
            if (transaction == null)
                return Error.NotFound($"Bank transaction with ID {transactionId} not found");

            if (transaction.TransactionType != "debit")
                return Result<IEnumerable<DebitReconciliationSuggestionDto>>.Success(
                    Enumerable.Empty<DebitReconciliationSuggestionDto>());

            var bankAccount = await _bankAccountRepository.GetByIdAsync(transaction.BankAccountId);
            if (bankAccount == null)
                return Error.NotFound($"Bank account not found");

            if (!bankAccount.CompanyId.HasValue)
                return Error.Validation("Bank account is not associated with a company");

            var companyId = bankAccount.CompanyId.Value;

            var candidates = await _repository.GetDebitReconciliationCandidatesAsync(
                companyId, transaction.Amount, transaction.TransactionDate, tolerance, 7, maxResults * 2);

            var suggestions = candidates.Select(c => new DebitReconciliationSuggestionDto
            {
                RecordId = c.Id,
                RecordType = c.Type,
                RecordTypeDisplay = GetTypeDisplayName(c.Type),
                PaymentDate = c.PaymentDate,
                Amount = c.Amount,
                PayeeName = c.PayeeName,
                Description = c.Description,
                ReferenceNumber = c.ReferenceNumber,
                MatchScore = CalculateDebitMatchScore(transaction, c),
                AmountDifference = Math.Abs(transaction.Amount - c.Amount),
                TdsAmount = c.TdsAmount,
                TdsSection = c.TdsSection,
                IsReconciled = c.IsReconciled,
                Category = c.Category
            })
            .OrderByDescending(s => s.MatchScore)
            .Take(maxResults)
            .ToList();

            return Result<IEnumerable<DebitReconciliationSuggestionDto>>.Success(suggestions);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<DebitReconciliationSuggestionDto> Items, int TotalCount)>> SearchReconciliationCandidatesAsync(
            ReconciliationSearchRequest request)
        {
            if (request == null)
                return Error.Validation("Search request is required");

            var validation = ServiceExtensions.ValidateGuid(request.CompanyId);
            if (validation.IsFailure)
                return validation.Error!;

            var (candidates, totalCount) = await _repository.GetOutgoingPaymentsAsync(
                request.CompanyId,
                request.PageNumber,
                request.PageSize,
                reconciled: request.IncludeReconciled ? null : false,
                types: request.RecordTypes,
                fromDate: request.DateFrom,
                toDate: request.DateTo,
                searchTerm: request.SearchTerm);

            var filteredCandidates = candidates.AsEnumerable();
            if (request.AmountMin.HasValue)
                filteredCandidates = filteredCandidates.Where(c => c.Amount >= request.AmountMin.Value);
            if (request.AmountMax.HasValue)
                filteredCandidates = filteredCandidates.Where(c => c.Amount <= request.AmountMax.Value);

            var suggestions = filteredCandidates.Select(c => new DebitReconciliationSuggestionDto
            {
                RecordId = c.Id,
                RecordType = c.Type,
                RecordTypeDisplay = GetTypeDisplayName(c.Type),
                PaymentDate = c.PaymentDate,
                Amount = c.Amount,
                PayeeName = c.PayeeName,
                Description = c.Description,
                ReferenceNumber = c.ReferenceNumber,
                MatchScore = 0,
                AmountDifference = 0,
                TdsAmount = c.TdsAmount,
                TdsSection = c.TdsSection,
                IsReconciled = c.IsReconciled,
                Category = c.Category
            }).ToList();

            return Result<(IEnumerable<DebitReconciliationSuggestionDto> Items, int TotalCount)>.Success(
                (suggestions, totalCount));
        }

        /// <inheritdoc />
        public async Task<Result<AutoReconcileResultDto>> AutoReconcileAsync(
            Guid bankAccountId,
            int minMatchScore = 80,
            decimal amountTolerance = 100m,
            int dateTolerance = 3)
        {
            var validation = ServiceExtensions.ValidateGuid(bankAccountId);
            if (validation.IsFailure)
                return validation.Error!;

            var result = new AutoReconcileResultDto();

            var unreconciledTxns = await _repository.GetUnreconciledAsync(bankAccountId);
            var transactions = unreconciledTxns.ToList();
            result.TransactionsProcessed = transactions.Count;

            foreach (var txn in transactions)
            {
                var suggestions = await _repository.GetReconciliationSuggestionsAsync(txn.Id, amountTolerance, 5);
                var suggestionsList = suggestions.ToList();

                if (!suggestionsList.Any())
                {
                    result.TransactionsSkipped++;
                    continue;
                }

                PaymentWithDetails? bestMatch = null;
                int bestScore = 0;
                string matchReason = "";

                foreach (var payment in suggestionsList)
                {
                    var score = CalculateMatchScoreForPayment(txn, payment);
                    if (score >= minMatchScore && score > bestScore)
                    {
                        var dateDiff = Math.Abs((txn.TransactionDate.ToDateTime(TimeOnly.MinValue) -
                            payment.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days);
                        if (dateDiff <= dateTolerance)
                        {
                            bestMatch = payment;
                            bestScore = score;
                            matchReason = BuildMatchReasonForPayment(txn, payment, score);
                        }
                    }
                }

                if (bestMatch != null)
                {
                    await _repository.ReconcileTransactionAsync(txn.Id, "payment", bestMatch.Id, "auto-reconcile");

                    result.TransactionsReconciled++;
                    result.TotalAmountReconciled += txn.Amount;
                    result.Matches.Add(new AutoReconcileMatchDto
                    {
                        BankTransactionId = txn.Id,
                        PaymentId = bestMatch.Id,
                        Amount = txn.Amount,
                        MatchScore = bestScore,
                        MatchReason = matchReason
                    });
                }
                else
                {
                    result.TransactionsSkipped++;
                }
            }

            return Result<AutoReconcileResultDto>.Success(result);
        }

        #region Helper Methods

        private static string GetTypeDisplayName(string type) => type switch
        {
            "salary" => "Salary",
            "contractor" => "Contractor Payment",
            "expense_claim" => "Expense Claim",
            "subscription" => "Subscription",
            "loan_payment" => "Loan Payment",
            "asset_maintenance" => "Asset Maintenance",
            _ => type
        };

        private static int CalculateMatchScoreForPayment(BankTransaction transaction, PaymentWithDetails payment)
        {
            int score = 0;

            var amountDiff = Math.Abs(transaction.Amount - payment.Amount);
            if (amountDiff == 0) score += 40;
            else if (amountDiff <= 0.01m) score += 35;
            else if (amountDiff <= 1m) score += 25;
            else if (amountDiff <= 10m) score += 15;

            var dateDiff = Math.Abs((transaction.TransactionDate.ToDateTime(TimeOnly.MinValue) -
                payment.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days);
            if (dateDiff == 0) score += 30;
            else if (dateDiff <= 1) score += 25;
            else if (dateDiff <= 3) score += 15;
            else if (dateDiff <= 7) score += 5;

            if (!string.IsNullOrWhiteSpace(transaction.ReferenceNumber) &&
                !string.IsNullOrWhiteSpace(payment.ReferenceNumber))
            {
                if (transaction.ReferenceNumber.Equals(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                    score += 30;
                else if (transaction.ReferenceNumber.Contains(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase) ||
                         payment.ReferenceNumber.Contains(transaction.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                    score += 20;
            }

            return Math.Min(score, 100);
        }

        private static int CalculateDebitMatchScore(BankTransaction transaction, Core.Interfaces.OutgoingPaymentRecord payment)
        {
            int score = 0;

            var amountDiff = Math.Abs(transaction.Amount - payment.Amount);
            if (amountDiff == 0) score += 40;
            else if (amountDiff <= 1m) score += 35;
            else if (amountDiff <= 10m) score += 25;
            else if (amountDiff <= 100m) score += 15;
            else if (amountDiff <= 500m) score += 5;

            var dateDiff = Math.Abs((transaction.TransactionDate.ToDateTime(TimeOnly.MinValue) -
                payment.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days);
            if (dateDiff == 0) score += 30;
            else if (dateDiff <= 1) score += 25;
            else if (dateDiff <= 3) score += 15;
            else if (dateDiff <= 7) score += 5;

            if (!string.IsNullOrWhiteSpace(transaction.ReferenceNumber) &&
                !string.IsNullOrWhiteSpace(payment.ReferenceNumber))
            {
                if (transaction.ReferenceNumber.Equals(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                    score += 30;
                else if (transaction.ReferenceNumber.Contains(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase) ||
                         payment.ReferenceNumber.Contains(transaction.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                    score += 20;
            }

            if (!string.IsNullOrWhiteSpace(transaction.Description) &&
                !string.IsNullOrWhiteSpace(payment.PayeeName) &&
                transaction.Description.Contains(payment.PayeeName, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }

            return Math.Min(score, 100);
        }

        private static string BuildMatchReasonForPayment(BankTransaction txn, PaymentWithDetails payment, int score)
        {
            var reasons = new List<string>();

            if (Math.Abs(txn.Amount - payment.Amount) < 0.01m)
                reasons.Add("Exact amount match");
            else
                reasons.Add($"Amount within tolerance (diff: {Math.Abs(txn.Amount - payment.Amount):F2})");

            var dateDiff = Math.Abs((txn.TransactionDate.ToDateTime(TimeOnly.MinValue) -
                payment.PaymentDate.ToDateTime(TimeOnly.MinValue)).Days);
            if (dateDiff == 0)
                reasons.Add("Same date");
            else
                reasons.Add($"Date within {dateDiff} days");

            if (!string.IsNullOrWhiteSpace(txn.ReferenceNumber) &&
                !string.IsNullOrWhiteSpace(payment.ReferenceNumber) &&
                txn.ReferenceNumber.Contains(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                reasons.Add("Reference number match");

            return $"Score: {score}% - {string.Join(", ", reasons)}";
        }

        #endregion
    }
}

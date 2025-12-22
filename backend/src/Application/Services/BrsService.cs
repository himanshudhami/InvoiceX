using Application.Common;
using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Core.Interfaces;
using Core.Interfaces.Ledger;
using Core.Common;

namespace Application.Services
{
    /// <summary>
    /// Service for Bank Reconciliation Statement generation
    /// </summary>
    public class BrsService : IBrsService
    {
        private readonly IBankTransactionRepository _repository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IJournalEntryRepository _journalEntryRepository;
        private readonly IChartOfAccountRepository _chartOfAccountRepository;

        // TDS section descriptions for compliance reporting
        private static readonly Dictionary<string, string> TdsSectionDescriptions = new()
        {
            { "194A", "Interest other than interest on securities" },
            { "194C", "Payment to contractors" },
            { "194H", "Commission or brokerage" },
            { "194I", "Rent" },
            { "194J", "Professional or technical services" },
            { "194Q", "Purchase of goods" },
            { "195", "Payment to non-resident" },
            { "206C", "TCS on sale of goods" }
        };

        public BrsService(
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
        public async Task<Result<BankReconciliationStatementDto>> GenerateBrsAsync(
            Guid bankAccountId,
            DateOnly asOfDate)
        {
            var validation = ServiceExtensions.ValidateGuid(bankAccountId);
            if (validation.IsFailure)
                return validation.Error!;

            var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId);
            if (bankAccount == null)
                return Error.NotFound($"Bank account with ID {bankAccountId} not found");

            var brs = new BankReconciliationStatementDto
            {
                BankAccountId = bankAccountId,
                BankAccountName = bankAccount.AccountName ?? $"{bankAccount.BankName} - {bankAccount.AccountNumber}",
                AsOfDate = asOfDate,
                BankStatementBalance = bankAccount.CurrentBalance
            };

            var allTxns = await _repository.GetByDateRangeAsync(bankAccountId, DateOnly.MinValue, asOfDate);
            var transactions = allTxns.ToList();

            brs.TotalTransactions = transactions.Count;
            brs.ReconciledTransactions = transactions.Count(t => t.IsReconciled);
            brs.UnreconciledTransactions = transactions.Count(t => !t.IsReconciled);

            // Unreconciled bank credits (in bank but not in books)
            var unreconciledCredits = transactions.Where(t => !t.IsReconciled && t.TransactionType == "credit").ToList();
            brs.BankCreditsNotInBooks = unreconciledCredits.Sum(t => t.Amount);
            brs.BankCreditsNotInBooksItems = unreconciledCredits.Select(t => new BrsItemDto
            {
                Id = t.Id,
                Date = t.TransactionDate,
                Description = t.Description ?? "",
                ReferenceNumber = t.ReferenceNumber,
                Amount = t.Amount,
                Type = "bank_transaction"
            }).ToList();

            // Unreconciled bank debits (in bank but not in books)
            var unreconciledDebits = transactions.Where(t => !t.IsReconciled && t.TransactionType == "debit").ToList();
            brs.BankDebitsNotInBooks = unreconciledDebits.Sum(t => t.Amount);
            brs.BankDebitsNotInBooksItems = unreconciledDebits.Select(t => new BrsItemDto
            {
                Id = t.Id,
                Date = t.TransactionDate,
                Description = t.Description ?? "",
                ReferenceNumber = t.ReferenceNumber,
                Amount = t.Amount,
                Type = "bank_transaction"
            }).ToList();

            // Calculate book balance from reconciled transactions
            var reconciledCredits = transactions.Where(t => t.IsReconciled && t.TransactionType == "credit").Sum(t => t.Amount);
            var reconciledDebits = transactions.Where(t => t.IsReconciled && t.TransactionType == "debit").Sum(t => t.Amount);
            brs.BookBalance = reconciledCredits - reconciledDebits;

            // Deposits in transit and outstanding cheques (would need integration with payment/expense systems)
            brs.DepositsInTransit = 0;
            brs.OutstandingCheques = 0;

            // Calculate adjusted balances
            brs.AdjustedBankBalance = brs.BankStatementBalance + brs.DepositsInTransit - brs.OutstandingCheques;
            brs.AdjustedBookBalance = brs.BookBalance + brs.BankCreditsNotInBooks - brs.BankDebitsNotInBooks;

            return Result<BankReconciliationStatementDto>.Success(brs);
        }

        /// <inheritdoc />
        public async Task<Result<EnhancedBrsReportDto>> GenerateEnhancedBrsAsync(
            Guid bankAccountId,
            DateOnly asOfDate,
            DateOnly? periodStart = null)
        {
            var validation = ServiceExtensions.ValidateGuid(bankAccountId);
            if (validation.IsFailure)
                return validation.Error!;

            var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId);
            if (bankAccount == null)
                return Error.NotFound($"Bank account with ID {bankAccountId} not found");

            var startDate = periodStart ?? DateOnly.MinValue;

            var brs = new EnhancedBrsReportDto
            {
                BankAccountId = bankAccountId,
                BankAccountName = bankAccount.AccountName ?? $"{bankAccount.BankName} - {bankAccount.AccountNumber}",
                AsOfDate = asOfDate,
                PeriodStart = periodStart,
                BankStatementBalance = bankAccount.CurrentBalance,
                HasLedgerLink = bankAccount.LinkedAccountId.HasValue,
                LinkedAccountId = bankAccount.LinkedAccountId
            };

            // Get linked ledger account name if available
            if (bankAccount.LinkedAccountId.HasValue && bankAccount.CompanyId.HasValue)
            {
                var linkedAccount = await _chartOfAccountRepository.GetByIdAsync(bankAccount.LinkedAccountId.Value);
                if (linkedAccount != null)
                {
                    brs.LinkedAccountName = linkedAccount.AccountName;

                    // Calculate ledger balance from journal entries
                    var ledgerBalance = await _journalEntryRepository.GetAccountBalanceAsync(
                        bankAccount.CompanyId.Value,
                        bankAccount.LinkedAccountId.Value,
                        asOfDate);
                    brs.LedgerBalance = ledgerBalance;
                }
            }

            // Get all transactions for the period
            var allTxns = await _repository.GetByDateRangeAsync(bankAccountId, startDate, asOfDate);
            var transactions = allTxns.ToList();

            brs.TotalTransactions = transactions.Count;
            brs.ReconciledTransactions = transactions.Count(t => t.IsReconciled);
            brs.UnreconciledTransactions = transactions.Count(t => !t.IsReconciled);

            // Unreconciled bank credits (in bank but not in books)
            var unreconciledCredits = transactions.Where(t => !t.IsReconciled && t.TransactionType == "credit").ToList();
            brs.BankCreditsNotInBooks = unreconciledCredits.Sum(t => t.Amount);
            brs.BankCreditsNotInBooksItems = unreconciledCredits.Select(t => new BrsItemDto
            {
                Id = t.Id,
                Date = t.TransactionDate,
                Description = t.Description ?? "",
                ReferenceNumber = t.ReferenceNumber,
                Amount = t.Amount,
                Type = "bank_transaction"
            }).ToList();

            // Unreconciled bank debits (in bank but not in books)
            var unreconciledDebits = transactions.Where(t => !t.IsReconciled && t.TransactionType == "debit").ToList();
            brs.BankDebitsNotInBooks = unreconciledDebits.Sum(t => t.Amount);
            brs.BankDebitsNotInBooksItems = unreconciledDebits.Select(t => new BrsItemDto
            {
                Id = t.Id,
                Date = t.TransactionDate,
                Description = t.Description ?? "",
                ReferenceNumber = t.ReferenceNumber,
                Amount = t.Amount,
                Type = "bank_transaction"
            }).ToList();

            // Calculate book balance from reconciled transactions
            var reconciledCredits = transactions.Where(t => t.IsReconciled && t.TransactionType == "credit").Sum(t => t.Amount);
            var reconciledDebits = transactions.Where(t => t.IsReconciled && t.TransactionType == "debit").Sum(t => t.Amount);
            brs.BookBalance = reconciledCredits - reconciledDebits;

            brs.DepositsInTransit = 0;
            brs.OutstandingCheques = 0;
            brs.AdjustedBankBalance = brs.BankStatementBalance + brs.DepositsInTransit - brs.OutstandingCheques;
            brs.AdjustedBookBalance = brs.BookBalance + brs.BankCreditsNotInBooks - brs.BankDebitsNotInBooks;

            // ==================== Enhanced: TDS Summary ====================

            var tdsTransactions = transactions
                .Where(t => t.IsReconciled && !string.IsNullOrEmpty(t.ReconciliationTdsSection))
                .GroupBy(t => t.ReconciliationTdsSection!)
                .Select(g => new TdsSummaryItemDto
                {
                    Section = g.Key,
                    Description = TdsSectionDescriptions.TryGetValue(g.Key, out var desc) ? desc : g.Key,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(t => t.ReconciliationDifferenceAmount ?? 0)
                })
                .OrderBy(t => t.Section)
                .ToList();

            brs.TdsSummary = tdsTransactions;
            brs.TotalTdsDeducted = tdsTransactions.Sum(t => Math.Abs(t.TotalAmount));

            // ==================== Enhanced: Audit Metrics ====================

            // Reconciled transactions without JE link (should be 0 for full compliance)
            var unlinkedJeTransactions = transactions
                .Where(t => t.IsReconciled &&
                           !t.ReconciledJournalEntryId.HasValue &&
                           t.ReconciledType != "journal_entry")
                .ToList();

            brs.UnlinkedJeCount = unlinkedJeTransactions.Count;
            brs.UnlinkedJeTransactionIds = unlinkedJeTransactions.Select(t => t.Id).ToList();

            // Direct JE reconciliations (manual JE without source documents)
            brs.DirectJeReconciliationCount = transactions.Count(t =>
                t.IsReconciled && t.ReconciledType == "journal_entry");

            // ==================== Enhanced: Difference Type Summary ====================

            var differenceTypeSummary = transactions
                .Where(t => t.IsReconciled && !string.IsNullOrEmpty(t.ReconciliationDifferenceType))
                .GroupBy(t => t.ReconciliationDifferenceType!)
                .Select(g => new DifferenceTypeSummaryDto
                {
                    DifferenceType = g.Key,
                    Description = GetDifferenceTypeDescription(g.Key),
                    Count = g.Count(),
                    TotalAmount = g.Sum(t => t.ReconciliationDifferenceAmount ?? 0)
                })
                .OrderBy(d => d.DifferenceType)
                .ToList();

            brs.DifferenceTypeSummary = differenceTypeSummary;

            return Result<EnhancedBrsReportDto>.Success(brs);
        }

        private static string GetDifferenceTypeDescription(string differenceType)
        {
            return differenceType switch
            {
                "bank_interest" => "Interest credited by bank",
                "bank_charges" => "Bank charges/fees deducted",
                "tds_deducted" => "TDS deducted by customer",
                "round_off" => "Rounding difference",
                "forex_gain" => "Foreign exchange gain",
                "forex_loss" => "Foreign exchange loss",
                "other_income" => "Other income",
                "other_expense" => "Other expense",
                "suspense" => "Parked for investigation",
                _ => differenceType
            };
        }
    }
}

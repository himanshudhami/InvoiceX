using Application.DTOs.BankTransactions;
using Core.Entities;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for BankTransaction operations
    /// </summary>
    public interface IBankTransactionService
    {
        /// <summary>
        /// Get bank transaction by ID
        /// </summary>
        Task<Result<BankTransaction>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all bank transactions
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetAllAsync();

        /// <summary>
        /// Get paginated bank transactions with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<BankTransaction> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Create a new bank transaction
        /// </summary>
        Task<Result<BankTransaction>> CreateAsync(CreateBankTransactionDto dto);

        /// <summary>
        /// Update an existing bank transaction
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateBankTransactionDto dto);

        /// <summary>
        /// Delete a bank transaction by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        // ==================== Bank Account Specific Methods ====================

        /// <summary>
        /// Get transactions for a specific bank account
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetByBankAccountIdAsync(Guid bankAccountId);

        /// <summary>
        /// Get transactions within a date range for a specific bank account
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetByDateRangeAsync(
            Guid bankAccountId, DateOnly fromDate, DateOnly toDate);

        // ==================== Reconciliation Methods ====================

        /// <summary>
        /// Get unreconciled transactions, optionally filtered by bank account
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetUnreconciledAsync(Guid? bankAccountId = null);

        /// <summary>
        /// Reconcile a transaction with a payment or other record
        /// </summary>
        Task<Result> ReconcileTransactionAsync(Guid transactionId, ReconcileTransactionDto dto);

        /// <summary>
        /// Remove reconciliation from a transaction
        /// </summary>
        Task<Result> UnreconcileTransactionAsync(Guid transactionId);

        /// <summary>
        /// Get reconciliation suggestions for a transaction
        /// </summary>
        Task<Result<IEnumerable<ReconciliationSuggestionDto>>> GetReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 0.01m, int maxResults = 10);

        /// <summary>
        /// Automatically reconcile unmatched transactions with payments
        /// </summary>
        Task<Result<AutoReconcileResultDto>> AutoReconcileAsync(
            Guid bankAccountId,
            int minMatchScore = 80,
            decimal amountTolerance = 0.01m,
            int dateTolerance = 3);

        /// <summary>
        /// Generate Bank Reconciliation Statement
        /// </summary>
        Task<Result<BankReconciliationStatementDto>> GenerateBrsAsync(
            Guid bankAccountId,
            DateOnly asOfDate);

        // ==================== Import Methods ====================

        /// <summary>
        /// Import bank transactions from parsed CSV data
        /// </summary>
        Task<Result<ImportBankTransactionsResult>> ImportTransactionsAsync(ImportBankTransactionsRequest request);

        /// <summary>
        /// Get transactions from a specific import batch
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetByImportBatchIdAsync(Guid batchId);

        /// <summary>
        /// Delete all transactions from a specific import batch (rollback import)
        /// </summary>
        Task<Result> DeleteImportBatchAsync(Guid batchId);

        // ==================== Summary Methods ====================

        /// <summary>
        /// Get summary statistics for a bank account
        /// </summary>
        Task<Result<BankTransactionSummaryDto>> GetSummaryAsync(
            Guid bankAccountId, DateOnly? fromDate = null, DateOnly? toDate = null);
    }

    /// <summary>
    /// Summary statistics for bank transactions
    /// </summary>
    public class BankTransactionSummaryDto
    {
        public int TotalCount { get; set; }
        public int ReconciledCount { get; set; }
        public int UnreconciledCount => TotalCount - ReconciledCount;
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal NetAmount => TotalCredits - TotalDebits;
        public double ReconciliationPercentage => TotalCount > 0 ? Math.Round((double)ReconciledCount / TotalCount * 100, 2) : 0;
    }

    /// <summary>
    /// Result of auto-reconciliation process
    /// </summary>
    public class AutoReconcileResultDto
    {
        public int TransactionsProcessed { get; set; }
        public int TransactionsReconciled { get; set; }
        public int TransactionsSkipped { get; set; }
        public decimal TotalAmountReconciled { get; set; }
        public List<AutoReconcileMatchDto> Matches { get; set; } = new();
    }

    public class AutoReconcileMatchDto
    {
        public Guid BankTransactionId { get; set; }
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; }
        public int MatchScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bank Reconciliation Statement
    /// </summary>
    public class BankReconciliationStatementDto
    {
        public Guid BankAccountId { get; set; }
        public string BankAccountName { get; set; } = string.Empty;
        public DateOnly AsOfDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Bank Balance
        public decimal BankStatementBalance { get; set; }

        // Add: Deposits in transit (recorded in books but not yet in bank)
        public decimal DepositsInTransit { get; set; }
        public List<BrsItemDto> DepositsInTransitItems { get; set; } = new();

        // Less: Outstanding cheques (recorded in books but not yet cleared)
        public decimal OutstandingCheques { get; set; }
        public List<BrsItemDto> OutstandingChequeItems { get; set; } = new();

        // Adjusted Bank Balance
        public decimal AdjustedBankBalance { get; set; }

        // Book Balance
        public decimal BookBalance { get; set; }

        // Add: Bank credits not in books
        public decimal BankCreditsNotInBooks { get; set; }
        public List<BrsItemDto> BankCreditsNotInBooksItems { get; set; } = new();

        // Less: Bank debits not in books
        public decimal BankDebitsNotInBooks { get; set; }
        public List<BrsItemDto> BankDebitsNotInBooksItems { get; set; } = new();

        // Adjusted Book Balance
        public decimal AdjustedBookBalance { get; set; }

        // Difference (should be 0 if reconciled)
        public decimal Difference => AdjustedBankBalance - AdjustedBookBalance;
        public bool IsReconciled => Math.Abs(Difference) < 0.01m;

        // Summary
        public int TotalTransactions { get; set; }
        public int ReconciledTransactions { get; set; }
        public int UnreconciledTransactions { get; set; }
    }

    public class BrsItemDto
    {
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // 'bank_transaction', 'payment', 'expense'
    }
}

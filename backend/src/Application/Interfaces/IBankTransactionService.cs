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
}

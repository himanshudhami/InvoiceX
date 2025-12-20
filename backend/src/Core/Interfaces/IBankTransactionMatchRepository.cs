using Core.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for bank transaction match operations
    /// </summary>
    public interface IBankTransactionMatchRepository
    {
        // ==================== Basic CRUD ====================

        Task<BankTransactionMatch?> GetByIdAsync(Guid id);
        Task<IEnumerable<BankTransactionMatch>> GetAllAsync();
        Task<(IEnumerable<BankTransactionMatch> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<BankTransactionMatch> AddAsync(BankTransactionMatch entity);
        Task UpdateAsync(BankTransactionMatch entity);
        Task DeleteAsync(Guid id);

        // ==================== Query by Related Entities ====================

        /// <summary>
        /// Get all matches for a specific bank transaction
        /// </summary>
        Task<IEnumerable<BankTransactionMatch>> GetByBankTransactionIdAsync(Guid bankTransactionId);

        /// <summary>
        /// Get all matches for a company
        /// </summary>
        Task<IEnumerable<BankTransactionMatch>> GetByCompanyIdAsync(Guid companyId);

        /// <summary>
        /// Get match by matched record (e.g., payment, expense)
        /// </summary>
        Task<IEnumerable<BankTransactionMatch>> GetByMatchedRecordAsync(string matchedType, Guid matchedId);

        // ==================== Match Summary ====================

        /// <summary>
        /// Get total matched amount for a bank transaction
        /// </summary>
        Task<decimal> GetTotalMatchedForTransactionAsync(Guid bankTransactionId);

        /// <summary>
        /// Get unmatched amount for a bank transaction
        /// </summary>
        Task<decimal> GetUnmatchedAmountAsync(Guid bankTransactionId);

        /// <summary>
        /// Check if a bank transaction is fully matched
        /// </summary>
        Task<bool> IsTransactionFullyMatchedAsync(Guid bankTransactionId);

        // ==================== Reconciliation Helpers ====================

        /// <summary>
        /// Get unreconciled transactions for a bank account
        /// </summary>
        Task<IEnumerable<dynamic>> GetUnreconciledTransactionsAsync(Guid bankAccountId);

        /// <summary>
        /// Get potential matches for a bank transaction (for auto-matching suggestions)
        /// </summary>
        Task<IEnumerable<dynamic>> GetPotentialMatchesAsync(Guid bankTransactionId);

        // ==================== Bulk Operations ====================

        /// <summary>
        /// Add multiple matches in a transaction
        /// </summary>
        Task<IEnumerable<BankTransactionMatch>> AddBulkAsync(IEnumerable<BankTransactionMatch> matches);

        /// <summary>
        /// Delete all matches for a bank transaction (un-reconcile)
        /// </summary>
        Task DeleteByBankTransactionIdAsync(Guid bankTransactionId);
    }
}

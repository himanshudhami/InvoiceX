using Core.Entities.Tags;

namespace Core.Interfaces.Tags
{
    public interface ITransactionTagRepository
    {
        // ==================== Basic CRUD ====================
        Task<TransactionTag?> GetByIdAsync(Guid id);
        Task<TransactionTag> AddAsync(TransactionTag entity);
        Task UpdateAsync(TransactionTag entity);
        Task DeleteAsync(Guid id);

        // ==================== By Transaction ====================
        /// <summary>
        /// Get all tags for a specific transaction
        /// </summary>
        Task<IEnumerable<TransactionTag>> GetByTransactionAsync(Guid transactionId, string transactionType);

        /// <summary>
        /// Get all tags for multiple transactions of the same type
        /// </summary>
        Task<IEnumerable<TransactionTag>> GetByTransactionsAsync(IEnumerable<Guid> transactionIds, string transactionType);

        /// <summary>
        /// Remove all tags from a transaction
        /// </summary>
        Task RemoveAllFromTransactionAsync(Guid transactionId, string transactionType);

        /// <summary>
        /// Remove specific tag from a transaction
        /// </summary>
        Task RemoveTagFromTransactionAsync(Guid transactionId, string transactionType, Guid tagId);

        // ==================== By Tag ====================
        /// <summary>
        /// Get all transactions with a specific tag
        /// </summary>
        Task<IEnumerable<TransactionTag>> GetByTagAsync(Guid tagId);

        /// <summary>
        /// Get paged transactions with a specific tag
        /// </summary>
        Task<(IEnumerable<TransactionTag> Items, int TotalCount)> GetByTagPagedAsync(
            Guid tagId,
            int pageNumber,
            int pageSize,
            string? transactionType = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null);

        // ==================== By Source ====================
        /// <summary>
        /// Get tags applied by a specific rule
        /// </summary>
        Task<IEnumerable<TransactionTag>> GetByRuleAsync(Guid attributionRuleId);

        /// <summary>
        /// Get AI-suggested tags pending review
        /// </summary>
        Task<IEnumerable<TransactionTag>> GetPendingAiSuggestionsAsync(Guid companyId);

        // ==================== Validation ====================
        /// <summary>
        /// Check if a tag is already applied to a transaction
        /// </summary>
        Task<bool> ExistsAsync(Guid transactionId, string transactionType, Guid tagId);

        // ==================== Bulk Operations ====================
        /// <summary>
        /// Apply multiple tags to a transaction
        /// </summary>
        Task<IEnumerable<TransactionTag>> AddManyAsync(IEnumerable<TransactionTag> transactionTags);

        /// <summary>
        /// Replace all tags on a transaction with new ones
        /// </summary>
        Task ReplaceTransactionTagsAsync(Guid transactionId, string transactionType, IEnumerable<TransactionTag> newTags);

        // ==================== Statistics ====================
        /// <summary>
        /// Get allocation summary for a tag within a date range
        /// </summary>
        Task<TagAllocationSummary> GetTagAllocationSummaryAsync(
            Guid tagId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null);

        /// <summary>
        /// Get allocation breakdown by transaction type for a tag
        /// </summary>
        Task<IEnumerable<TagTransactionTypeBreakdown>> GetTagBreakdownByTransactionTypeAsync(
            Guid tagId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null);
    }

    // ==================== Summary DTOs ====================

    public class TagAllocationSummary
    {
        public Guid TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalAllocatedAmount { get; set; }
        public decimal? BudgetAmount { get; set; }
        public decimal? BudgetVariance { get; set; }
    }

    public class TagTransactionTypeBreakdown
    {
        public string TransactionType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }
}

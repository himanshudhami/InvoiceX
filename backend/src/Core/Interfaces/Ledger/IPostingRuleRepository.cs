using Core.Entities.Ledger;

namespace Core.Interfaces.Ledger
{
    /// <summary>
    /// Repository interface for Posting Rule operations
    /// </summary>
    public interface IPostingRuleRepository
    {
        // ==================== Basic CRUD ====================

        Task<PostingRule?> GetByIdAsync(Guid id);
        Task<IEnumerable<PostingRule>> GetAllAsync();
        Task<(IEnumerable<PostingRule> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<PostingRule> AddAsync(PostingRule entity);
        Task UpdateAsync(PostingRule entity);
        Task DeleteAsync(Guid id);

        // ==================== Company-Specific Queries ====================

        /// <summary>
        /// Get all rules for a company (including system-wide templates)
        /// </summary>
        Task<IEnumerable<PostingRule>> GetByCompanyIdAsync(Guid companyId);

        /// <summary>
        /// Get rule by code
        /// </summary>
        Task<PostingRule?> GetByCodeAsync(Guid? companyId, string ruleCode);

        /// <summary>
        /// Get rules by source type
        /// </summary>
        Task<IEnumerable<PostingRule>> GetBySourceTypeAsync(
            Guid companyId,
            string sourceType,
            string? triggerEvent = null);

        // ==================== Rule Matching ====================

        /// <summary>
        /// Find matching rules for a source event
        /// Returns rules ordered by priority
        /// </summary>
        Task<IEnumerable<PostingRule>> FindMatchingRulesAsync(
            Guid companyId,
            string sourceType,
            string triggerEvent,
            DateOnly transactionDate,
            string? financialYear = null);

        /// <summary>
        /// Get the best matching rule for a source event
        /// </summary>
        Task<PostingRule?> GetBestMatchingRuleAsync(
            Guid companyId,
            string sourceType,
            string triggerEvent,
            Dictionary<string, object> conditions,
            DateOnly transactionDate);

        // ==================== Initialization ====================

        /// <summary>
        /// Check if a company has posting rules initialized
        /// </summary>
        Task<bool> HasRulesAsync(Guid companyId);

        /// <summary>
        /// Initialize default posting rules for a company
        /// </summary>
        Task InitializeDefaultRulesAsync(Guid companyId, Guid? createdBy = null);

        // ==================== Usage Logging ====================

        /// <summary>
        /// Log the usage of a posting rule
        /// </summary>
        Task LogUsageAsync(PostingRuleUsageLog usageLog);

        /// <summary>
        /// Get usage history for a rule
        /// </summary>
        Task<IEnumerable<PostingRuleUsageLog>> GetUsageHistoryAsync(Guid ruleId, int limit = 100);
    }
}

using Core.Entities.Tags;

namespace Core.Interfaces.Tags
{
    public interface IAttributionRuleRepository
    {
        // ==================== Basic CRUD ====================
        Task<AttributionRule?> GetByIdAsync(Guid id);
        Task<IEnumerable<AttributionRule>> GetAllAsync();
        Task<IEnumerable<AttributionRule>> GetByCompanyIdAsync(Guid companyId);
        Task<AttributionRule> AddAsync(AttributionRule entity);
        Task UpdateAsync(AttributionRule entity);
        Task DeleteAsync(Guid id);

        // ==================== Paged/Filtered ====================
        Task<(IEnumerable<AttributionRule> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        // ==================== Active Rules ====================
        /// <summary>
        /// Get active rules for a company, ordered by priority
        /// </summary>
        Task<IEnumerable<AttributionRule>> GetActiveRulesAsync(Guid companyId);

        /// <summary>
        /// Get active rules that apply to a specific transaction type
        /// </summary>
        Task<IEnumerable<AttributionRule>> GetRulesForTransactionTypeAsync(
            Guid companyId,
            string transactionType);

        /// <summary>
        /// Get active rules by rule type
        /// </summary>
        Task<IEnumerable<AttributionRule>> GetByRuleTypeAsync(Guid companyId, string ruleType);

        // ==================== Matching ====================
        /// <summary>
        /// Get rules that might match a vendor (for vendor-based rules)
        /// </summary>
        Task<IEnumerable<AttributionRule>> GetVendorRulesAsync(Guid companyId, Guid vendorId);

        /// <summary>
        /// Get rules that might match a customer (for customer-based rules)
        /// </summary>
        Task<IEnumerable<AttributionRule>> GetCustomerRulesAsync(Guid companyId, Guid customerId);

        /// <summary>
        /// Get rules that might match an account (for account-based rules)
        /// </summary>
        Task<IEnumerable<AttributionRule>> GetAccountRulesAsync(Guid companyId, Guid accountId);

        // ==================== Statistics ====================
        /// <summary>
        /// Update rule statistics after application
        /// </summary>
        Task UpdateRuleStatisticsAsync(Guid ruleId, decimal amountTagged);

        /// <summary>
        /// Get rule performance summary
        /// </summary>
        Task<IEnumerable<RulePerformanceSummary>> GetRulePerformanceSummaryAsync(Guid companyId);

        // ==================== Validation ====================
        Task<bool> ExistsAsync(Guid id);
        Task<bool> NameExistsAsync(Guid companyId, string name, Guid? excludeId = null);

        // ==================== Bulk Operations ====================
        Task<IEnumerable<AttributionRule>> AddManyAsync(IEnumerable<AttributionRule> rules);
        Task ReorderPrioritiesAsync(Guid companyId, IEnumerable<(Guid RuleId, int NewPriority)> priorities);
    }

    // ==================== Performance DTO ====================

    public class RulePerformanceSummary
    {
        public Guid RuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public int TimesApplied { get; set; }
        public decimal TotalAmountTagged { get; set; }
        public DateTime? LastAppliedAt { get; set; }
        public int CurrentTagsCount { get; set; }
    }
}

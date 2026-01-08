using Core.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for Tag-driven TDS Rules
    /// Tags drive TDS behavior instead of hard-coded vendor types
    /// </summary>
    public interface ITdsTagRuleRepository
    {
        // ==================== Basic CRUD ====================

        Task<TdsTagRule?> GetByIdAsync(Guid id);
        Task<IEnumerable<TdsTagRule>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<TdsTagRule>> GetActiveByCompanyIdAsync(Guid companyId);
        Task<TdsTagRule> AddAsync(TdsTagRule entity);
        Task UpdateAsync(TdsTagRule entity);
        Task DeleteAsync(Guid id);

        // ==================== Tag-based Queries ====================

        /// <summary>
        /// Get TDS rule by tag ID (currently effective)
        /// </summary>
        Task<TdsTagRule?> GetByTagIdAsync(Guid tagId);

        /// <summary>
        /// Get all TDS rules for a tag (including historical)
        /// </summary>
        Task<IEnumerable<TdsTagRule>> GetAllByTagIdAsync(Guid tagId);

        /// <summary>
        /// Get TDS rules by section code
        /// </summary>
        Task<IEnumerable<TdsTagRule>> GetBySectionAsync(Guid companyId, string tdsSection);

        /// <summary>
        /// Get currently effective rules for a company
        /// </summary>
        Task<IEnumerable<TdsTagRule>> GetEffectiveRulesAsync(Guid companyId, DateOnly? asOfDate = null);

        // ==================== Party TDS Detection ====================

        /// <summary>
        /// Get TDS rule for a party based on their TDS section tags
        /// Returns the first matching rule ordered by tag assignment
        /// </summary>
        Task<TdsTagRule?> GetRuleForPartyAsync(Guid partyId);

        /// <summary>
        /// Get all TDS section tags assigned to a party with their rules
        /// </summary>
        Task<IEnumerable<TdsTagRule>> GetRulesForPartyTagsAsync(Guid partyId);

        // ==================== Paged Query ====================

        Task<(IEnumerable<TdsTagRule> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? tdsSection = null,
            bool? isActive = null,
            string? sortBy = null,
            bool sortDescending = false);

        // ==================== Seeding ====================

        /// <summary>
        /// Seed default TDS tags and rules for a company
        /// Calls the seed_tds_system database function
        /// </summary>
        Task SeedDefaultsAsync(Guid companyId);

        /// <summary>
        /// Check if company has TDS tags seeded
        /// </summary>
        Task<bool> HasTdsTagsAsync(Guid companyId);

        // ==================== Bulk Operations ====================

        Task<IEnumerable<TdsTagRule>> BulkAddAsync(IEnumerable<TdsTagRule> rules);
    }
}

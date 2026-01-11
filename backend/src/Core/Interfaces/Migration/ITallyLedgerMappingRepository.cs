using Core.Entities.Migration;

namespace Core.Interfaces.Migration
{
    /// <summary>
    /// Repository for Tally ledger to control account + party mappings.
    /// Enables Tally import/export without per-party COA entries.
    /// Part of COA Modernization (Task 16).
    /// </summary>
    public interface ITallyLedgerMappingRepository
    {
        // ==================== CRUD ====================

        Task<TallyLedgerMapping?> GetByIdAsync(Guid id);
        Task<IEnumerable<TallyLedgerMapping>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<TallyLedgerMapping>> GetActiveByCompanyIdAsync(Guid companyId);
        Task<(IEnumerable<TallyLedgerMapping> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? partyType = null,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false);
        Task<TallyLedgerMapping> AddAsync(TallyLedgerMapping mapping);
        Task<IEnumerable<TallyLedgerMapping>> BulkAddAsync(IEnumerable<TallyLedgerMapping> mappings);
        Task UpdateAsync(TallyLedgerMapping mapping);
        Task DeleteAsync(Guid id);

        // ==================== Lookup Methods ====================

        /// <summary>
        /// Get mapping by Tally ledger name (for import)
        /// </summary>
        Task<TallyLedgerMapping?> GetByTallyLedgerNameAsync(Guid companyId, string tallyLedgerName);

        /// <summary>
        /// Get mapping by Tally GUID (for exact sync)
        /// </summary>
        Task<TallyLedgerMapping?> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid);

        /// <summary>
        /// Get mapping by party ID (for export)
        /// </summary>
        Task<TallyLedgerMapping?> GetByPartyIdAsync(Guid companyId, Guid partyId);

        /// <summary>
        /// Get all mappings for a party type
        /// </summary>
        Task<IEnumerable<TallyLedgerMapping>> GetByPartyTypeAsync(Guid companyId, string partyType);

        // ==================== Seed Methods ====================

        /// <summary>
        /// Seed mappings from existing parties (vendors/customers)
        /// </summary>
        Task<int> SeedFromPartiesAsync(Guid companyId);

        /// <summary>
        /// Check if mappings exist for this company
        /// </summary>
        Task<bool> HasMappingsAsync(Guid companyId);

        /// <summary>
        /// Get count of mappings by party type
        /// </summary>
        Task<Dictionary<string, int>> GetMappingCountsByTypeAsync(Guid companyId);
    }
}

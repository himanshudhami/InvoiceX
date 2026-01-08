using Core.Entities.Migration;

namespace Core.Interfaces.Migration
{
    public interface ITallyFieldMappingRepository
    {
        Task<TallyFieldMapping?> GetByIdAsync(Guid id);
        Task<IEnumerable<TallyFieldMapping>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<TallyFieldMapping>> GetActiveByCompanyIdAsync(Guid companyId);
        Task<(IEnumerable<TallyFieldMapping> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? mappingType = null,
            string? sortBy = null,
            bool sortDescending = false);
        Task<TallyFieldMapping> AddAsync(TallyFieldMapping mapping);
        Task<IEnumerable<TallyFieldMapping>> BulkAddAsync(IEnumerable<TallyFieldMapping> mappings);
        Task UpdateAsync(TallyFieldMapping mapping);
        Task DeleteAsync(Guid id);

        // Lookup methods for import
        Task<TallyFieldMapping?> GetMappingForGroupAsync(Guid companyId, string tallyGroupName);
        Task<TallyFieldMapping?> GetMappingForLedgerAsync(Guid companyId, string tallyGroupName, string tallyName);
        Task<TallyFieldMapping?> GetMappingForStockGroupAsync(Guid companyId, string tallyStockGroupName);
        Task<TallyFieldMapping?> GetMappingForCostCategoryAsync(Guid companyId, string tallyCostCategoryName);

        // Get target entity for a Tally group
        Task<string> GetTargetEntityAsync(Guid companyId, string tallyGroupName, string? tallyName = null);

        // Get tag assignments for a Tally group (for auto-tagging parties during import)
        Task<List<string>> GetTagAssignmentsForGroupAsync(Guid companyId, string tallyGroupName);

        // Seed default mappings
        Task SeedDefaultMappingsAsync(Guid companyId, Guid? createdBy = null);

        // Check if defaults exist
        Task<bool> HasDefaultMappingsAsync(Guid companyId);
    }
}

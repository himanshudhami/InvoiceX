using Core.Entities.Tags;

namespace Core.Interfaces.Tags
{
    public interface ITagRepository
    {
        // ==================== Basic CRUD ====================
        Task<Tag?> GetByIdAsync(Guid id);
        Task<IEnumerable<Tag>> GetAllAsync();
        Task<IEnumerable<Tag>> GetByCompanyIdAsync(Guid companyId);
        Task<Tag> AddAsync(Tag entity);
        Task UpdateAsync(Tag entity);
        Task DeleteAsync(Guid id);

        // ==================== Paged/Filtered ====================
        Task<(IEnumerable<Tag> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        // ==================== By Group ====================
        Task<IEnumerable<Tag>> GetByGroupAsync(Guid companyId, string tagGroup);
        Task<IEnumerable<Tag>> GetActiveByGroupAsync(Guid companyId, string tagGroup);

        // ==================== Hierarchy ====================
        Task<IEnumerable<Tag>> GetChildrenAsync(Guid parentTagId);
        Task<IEnumerable<Tag>> GetRootTagsAsync(Guid companyId);
        Task<IEnumerable<Tag>> GetTagHierarchyAsync(Guid companyId, string? tagGroup = null);

        // ==================== Lookups ====================
        Task<Tag?> GetByNameAsync(Guid companyId, string name, Guid? parentTagId = null);
        Task<Tag?> GetByCodeAsync(Guid companyId, string code);
        Task<Tag?> GetByTallyGuidAsync(string tallyCostCenterGuid);

        // ==================== Validation ====================
        Task<bool> ExistsAsync(Guid id);
        Task<bool> NameExistsAsync(Guid companyId, string name, Guid? parentTagId = null, Guid? excludeId = null);
        Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId = null);
        Task<bool> HasChildrenAsync(Guid tagId);
        Task<bool> HasTransactionsAsync(Guid tagId);

        // ==================== Statistics ====================
        Task<int> GetTransactionCountAsync(Guid tagId);
        Task<decimal> GetTotalAllocatedAmountAsync(Guid tagId, DateOnly? fromDate = null, DateOnly? toDate = null);

        // ==================== Bulk Operations ====================
        Task<IEnumerable<Tag>> AddManyAsync(IEnumerable<Tag> tags);
        Task UpdateManyAsync(IEnumerable<Tag> tags);
        Task SeedDefaultTagsAsync(Guid companyId, Guid? createdBy = null);
    }
}

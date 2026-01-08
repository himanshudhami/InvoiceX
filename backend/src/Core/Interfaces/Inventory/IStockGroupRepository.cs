using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface IStockGroupRepository
{
    Task<StockGroup?> GetByIdAsync(Guid id);
    Task<IEnumerable<StockGroup>> GetAllAsync();
    Task<IEnumerable<StockGroup>> GetByCompanyIdAsync(Guid companyId);
    Task<(IEnumerable<StockGroup> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<StockGroup> AddAsync(StockGroup entity);
    Task UpdateAsync(StockGroup entity);
    Task DeleteAsync(Guid id);

    // Specialized queries
    Task<IEnumerable<StockGroup>> GetActiveAsync(Guid companyId);
    Task<IEnumerable<StockGroup>> GetRootGroupsAsync(Guid companyId);
    Task<IEnumerable<StockGroup>> GetChildGroupsAsync(Guid parentStockGroupId);
    Task<IEnumerable<StockGroup>> GetHierarchyAsync(Guid companyId);
    Task<bool> ExistsAsync(Guid companyId, string name, Guid? parentGroupId = null, Guid? excludeId = null);
    Task<bool> HasChildrenAsync(Guid stockGroupId);
    Task<bool> HasItemsAsync(Guid stockGroupId);
    Task<string> GetFullPathAsync(Guid stockGroupId);

    // ==================== Tally Migration ====================
    Task<StockGroup?> GetByTallyGuidAsync(Guid companyId, string tallyStockGroupGuid);
    Task<StockGroup?> GetByNameAsync(Guid companyId, string name);
}

using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface IStockItemRepository
{
    Task<StockItem?> GetByIdAsync(Guid id);
    Task<StockItem?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<StockItem>> GetAllAsync();
    Task<IEnumerable<StockItem>> GetByCompanyIdAsync(Guid companyId);
    Task<(IEnumerable<StockItem> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<StockItem> AddAsync(StockItem entity);
    Task UpdateAsync(StockItem entity);
    Task DeleteAsync(Guid id);

    // Specialized queries
    Task<IEnumerable<StockItem>> GetActiveAsync(Guid companyId);
    Task<StockItem?> GetBySkuAsync(Guid companyId, string sku);
    Task<IEnumerable<StockItem>> GetByStockGroupIdAsync(Guid stockGroupId);
    Task<IEnumerable<StockItem>> GetLowStockItemsAsync(Guid companyId);
    Task<bool> ExistsAsync(Guid companyId, string name, Guid? excludeId = null);
    Task<bool> SkuExistsAsync(Guid companyId, string sku, Guid? excludeId = null);
    Task<bool> HasMovementsAsync(Guid stockItemId);

    // Stock quantity updates
    Task UpdateCurrentStockAsync(Guid stockItemId, decimal quantityChange, decimal valueChange);
    Task RecalculateStockAsync(Guid stockItemId);
}

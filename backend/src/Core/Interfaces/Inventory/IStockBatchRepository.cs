using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface IStockBatchRepository
{
    Task<StockBatch?> GetByIdAsync(Guid id);
    Task<IEnumerable<StockBatch>> GetByStockItemIdAsync(Guid stockItemId);
    Task<IEnumerable<StockBatch>> GetByWarehouseIdAsync(Guid warehouseId);
    Task<IEnumerable<StockBatch>> GetByStockItemAndWarehouseAsync(Guid stockItemId, Guid warehouseId);
    Task<(IEnumerable<StockBatch> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<StockBatch> AddAsync(StockBatch entity);
    Task UpdateAsync(StockBatch entity);
    Task DeleteAsync(Guid id);

    // Specialized queries
    Task<StockBatch?> GetByBatchNumberAsync(Guid stockItemId, Guid warehouseId, string batchNumber);
    Task<IEnumerable<StockBatch>> GetExpiringBatchesAsync(Guid companyId, int daysFromNow);
    Task<IEnumerable<StockBatch>> GetExpiredBatchesAsync(Guid companyId);
    Task<IEnumerable<StockBatch>> GetBatchesWithStockAsync(Guid stockItemId, Guid? warehouseId = null);
    Task<bool> ExistsAsync(Guid stockItemId, Guid warehouseId, string batchNumber, Guid? excludeId = null);
    Task<bool> HasMovementsAsync(Guid batchId);

    // Stock updates
    Task UpdateQuantityAsync(Guid batchId, decimal quantityChange, decimal valueChange);
}

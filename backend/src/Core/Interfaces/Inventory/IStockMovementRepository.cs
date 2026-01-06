using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(Guid id);
    Task<IEnumerable<StockMovement>> GetByCompanyIdAsync(Guid companyId);
    Task<(IEnumerable<StockMovement> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<StockMovement> AddAsync(StockMovement entity);
    Task<IEnumerable<StockMovement>> AddRangeAsync(IEnumerable<StockMovement> entities);
    Task UpdateAsync(StockMovement entity);
    Task DeleteAsync(Guid id);

    // Stock ledger queries
    Task<IEnumerable<StockMovement>> GetStockLedgerAsync(
        Guid stockItemId,
        Guid? warehouseId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null);

    Task<IEnumerable<StockMovement>> GetByStockItemIdAsync(Guid stockItemId);
    Task<IEnumerable<StockMovement>> GetByWarehouseIdAsync(Guid warehouseId);
    Task<IEnumerable<StockMovement>> GetByBatchIdAsync(Guid batchId);
    Task<IEnumerable<StockMovement>> GetBySourceAsync(string sourceType, Guid sourceId);
    Task<IEnumerable<StockMovement>> GetByDateRangeAsync(Guid companyId, DateOnly fromDate, DateOnly toDate);

    // Aggregations
    Task<decimal> GetTotalQuantityAsync(Guid stockItemId, Guid? warehouseId = null, Guid? batchId = null);
    Task<decimal> GetTotalValueAsync(Guid stockItemId, Guid? warehouseId = null, Guid? batchId = null);
    Task<(decimal Quantity, decimal Value)> GetStockPositionAsync(
        Guid stockItemId,
        Guid? warehouseId = null,
        DateOnly? asOfDate = null);

    // Running totals
    Task RecalculateRunningTotalsAsync(Guid stockItemId, Guid? warehouseId = null);

    // Source-based operations
    Task DeleteBySourceAsync(string sourceType, Guid sourceId);
    Task<bool> HasMovementsForSourceAsync(string sourceType, Guid sourceId);
}

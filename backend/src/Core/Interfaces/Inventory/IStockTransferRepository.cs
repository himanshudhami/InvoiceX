using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface IStockTransferRepository
{
    Task<StockTransfer?> GetByIdAsync(Guid id);
    Task<StockTransfer?> GetByIdWithItemsAsync(Guid id);
    Task<IEnumerable<StockTransfer>> GetByCompanyIdAsync(Guid companyId);
    Task<(IEnumerable<StockTransfer> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<StockTransfer> AddAsync(StockTransfer entity);
    Task UpdateAsync(StockTransfer entity);
    Task DeleteAsync(Guid id);

    // Specialized queries
    Task<StockTransfer?> GetByTransferNumberAsync(Guid companyId, string transferNumber);
    Task<IEnumerable<StockTransfer>> GetByWarehouseAsync(Guid warehouseId, bool asSource = true);
    Task<IEnumerable<StockTransfer>> GetByStatusAsync(Guid companyId, string status);
    Task<IEnumerable<StockTransfer>> GetPendingTransfersAsync(Guid companyId);
    Task<bool> ExistsAsync(Guid companyId, string transferNumber, Guid? excludeId = null);

    // Status updates
    Task UpdateStatusAsync(Guid id, string status, Guid? userId = null);
    Task CompleteTransferAsync(Guid id, Guid completedBy);
    Task CancelTransferAsync(Guid id, Guid cancelledBy);

    // Number generation
    Task<string> GenerateTransferNumberAsync(Guid companyId);
}

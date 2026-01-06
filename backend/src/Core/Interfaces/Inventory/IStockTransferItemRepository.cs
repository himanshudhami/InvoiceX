using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface IStockTransferItemRepository
{
    Task<StockTransferItem?> GetByIdAsync(Guid id);
    Task<IEnumerable<StockTransferItem>> GetByTransferIdAsync(Guid stockTransferId);
    Task<StockTransferItem> AddAsync(StockTransferItem entity);
    Task<IEnumerable<StockTransferItem>> AddRangeAsync(IEnumerable<StockTransferItem> entities);
    Task UpdateAsync(StockTransferItem entity);
    Task DeleteAsync(Guid id);
    Task DeleteByTransferIdAsync(Guid stockTransferId);

    // Specialized queries
    Task<IEnumerable<StockTransferItem>> GetByStockItemIdAsync(Guid stockItemId);
    Task<decimal> GetTotalQuantityByTransferAsync(Guid stockTransferId);
    Task<decimal> GetTotalValueByTransferAsync(Guid stockTransferId);

    // Receive operations (for partial receipts)
    Task UpdateReceivedQuantityAsync(Guid id, decimal receivedQuantity);
    Task<bool> AllItemsReceivedAsync(Guid stockTransferId);

    // Bulk operations
    Task ReplaceItemsAsync(Guid stockTransferId, IEnumerable<StockTransferItem> items);
}

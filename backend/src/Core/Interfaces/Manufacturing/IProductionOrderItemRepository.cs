using Core.Entities.Manufacturing;

namespace Core.Interfaces.Manufacturing;

public interface IProductionOrderItemRepository
{
    Task<ProductionOrderItem?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProductionOrderItem>> GetByOrderIdAsync(Guid productionOrderId);
    Task<ProductionOrderItem> AddAsync(ProductionOrderItem entity);
    Task<IEnumerable<ProductionOrderItem>> AddRangeAsync(IEnumerable<ProductionOrderItem> entities);
    Task UpdateAsync(ProductionOrderItem entity);
    Task UpdateConsumedQuantityAsync(Guid id, decimal consumedQuantity);
    Task DeleteAsync(Guid id);
    Task DeleteByOrderIdAsync(Guid productionOrderId);
    Task ReplaceItemsAsync(Guid productionOrderId, IEnumerable<ProductionOrderItem> items);
    Task<decimal> GetTotalPlannedQuantityAsync(Guid productionOrderId);
    Task<decimal> GetTotalConsumedQuantityAsync(Guid productionOrderId);
    Task<bool> AllItemsConsumedAsync(Guid productionOrderId);
}

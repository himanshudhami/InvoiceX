using Core.Entities.Manufacturing;

namespace Core.Interfaces.Manufacturing;

public interface IBomItemRepository
{
    Task<BomItem?> GetByIdAsync(Guid id);
    Task<IEnumerable<BomItem>> GetByBomIdAsync(Guid bomId);
    Task<BomItem> AddAsync(BomItem entity);
    Task<IEnumerable<BomItem>> AddRangeAsync(IEnumerable<BomItem> entities);
    Task UpdateAsync(BomItem entity);
    Task DeleteAsync(Guid id);
    Task DeleteByBomIdAsync(Guid bomId);
    Task ReplaceItemsAsync(Guid bomId, IEnumerable<BomItem> items);
    Task<int> GetItemCountAsync(Guid bomId);
}

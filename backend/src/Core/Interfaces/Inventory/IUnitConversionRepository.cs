using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface IUnitConversionRepository
{
    Task<UnitConversion?> GetByIdAsync(Guid id);
    Task<IEnumerable<UnitConversion>> GetByStockItemIdAsync(Guid stockItemId);
    Task<UnitConversion> AddAsync(UnitConversion entity);
    Task UpdateAsync(UnitConversion entity);
    Task DeleteAsync(Guid id);
    Task DeleteByStockItemIdAsync(Guid stockItemId);

    // Specialized queries
    Task<UnitConversion?> GetConversionAsync(Guid stockItemId, Guid fromUnitId, Guid toUnitId);
    Task<decimal?> GetConversionFactorAsync(Guid stockItemId, Guid fromUnitId, Guid toUnitId);
    Task<bool> ExistsAsync(Guid stockItemId, Guid fromUnitId, Guid toUnitId, Guid? excludeId = null);

    // Bulk operations
    Task<IEnumerable<UnitConversion>> AddRangeAsync(IEnumerable<UnitConversion> entities);
    Task ReplaceConversionsAsync(Guid stockItemId, IEnumerable<UnitConversion> conversions);
}

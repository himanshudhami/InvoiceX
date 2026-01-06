using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface IUnitOfMeasureRepository
{
    Task<UnitOfMeasure?> GetByIdAsync(Guid id);
    Task<IEnumerable<UnitOfMeasure>> GetAllAsync();
    Task<IEnumerable<UnitOfMeasure>> GetByCompanyIdAsync(Guid? companyId);
    Task<IEnumerable<UnitOfMeasure>> GetSystemUnitsAsync();
    Task<IEnumerable<UnitOfMeasure>> GetAllAvailableAsync(Guid companyId);
    Task<(IEnumerable<UnitOfMeasure> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<UnitOfMeasure> AddAsync(UnitOfMeasure entity);
    Task UpdateAsync(UnitOfMeasure entity);
    Task DeleteAsync(Guid id);

    // Specialized queries
    Task<UnitOfMeasure?> GetBySymbolAsync(string symbol, Guid? companyId = null);
    Task<bool> ExistsAsync(string symbol, Guid? companyId = null, Guid? excludeId = null);
    Task<bool> IsInUseAsync(Guid unitId);
}

using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id);
    Task<IEnumerable<Warehouse>> GetAllAsync();
    Task<IEnumerable<Warehouse>> GetByCompanyIdAsync(Guid companyId);
    Task<(IEnumerable<Warehouse> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<Warehouse> AddAsync(Warehouse entity);
    Task UpdateAsync(Warehouse entity);
    Task DeleteAsync(Guid id);

    // Specialized queries
    Task<Warehouse?> GetDefaultAsync(Guid companyId);
    Task<IEnumerable<Warehouse>> GetActiveAsync(Guid companyId);
    Task<IEnumerable<Warehouse>> GetChildWarehousesAsync(Guid parentWarehouseId);
    Task<bool> ExistsAsync(Guid companyId, string name, Guid? excludeId = null);
    Task<bool> HasMovementsAsync(Guid warehouseId);
    Task SetDefaultAsync(Guid companyId, Guid warehouseId);
}

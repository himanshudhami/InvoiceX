using Core.Entities.Inventory;

namespace Core.Interfaces.Inventory;

public interface ISerialNumberRepository
{
    Task<SerialNumber?> GetByIdAsync(Guid id);
    Task<SerialNumber?> GetBySerialNoAsync(Guid companyId, Guid stockItemId, string serialNo);
    Task<IEnumerable<SerialNumber>> GetAllAsync(Guid companyId);
    Task<IEnumerable<SerialNumber>> GetByStockItemAsync(Guid stockItemId);
    Task<IEnumerable<SerialNumber>> GetByWarehouseAsync(Guid warehouseId);
    Task<IEnumerable<SerialNumber>> GetByStatusAsync(Guid companyId, string status);
    Task<IEnumerable<SerialNumber>> GetAvailableAsync(Guid stockItemId, Guid? warehouseId = null);
    Task<IEnumerable<SerialNumber>> GetByProductionOrderAsync(Guid productionOrderId);
    Task<(IEnumerable<SerialNumber> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? companyId = null,
        Guid? stockItemId = null,
        Guid? warehouseId = null,
        string? status = null,
        string? searchTerm = null);
    Task<SerialNumber> AddAsync(SerialNumber entity);
    Task<IEnumerable<SerialNumber>> AddRangeAsync(IEnumerable<SerialNumber> entities);
    Task UpdateAsync(SerialNumber entity);
    Task UpdateStatusAsync(Guid id, string status);
    Task MarkAsSoldAsync(Guid id, Guid invoiceId);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> SerialNoExistsAsync(Guid companyId, Guid stockItemId, string serialNo, Guid? excludeId = null);
    Task<int> GetCountByItemAsync(Guid stockItemId, string? status = null);
}

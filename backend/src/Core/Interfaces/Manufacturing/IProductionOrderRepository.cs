using Core.Entities.Manufacturing;

namespace Core.Interfaces.Manufacturing;

public interface IProductionOrderRepository
{
    Task<ProductionOrder?> GetByIdAsync(Guid id);
    Task<ProductionOrder?> GetByIdWithItemsAsync(Guid id);
    Task<IEnumerable<ProductionOrder>> GetAllAsync(Guid companyId);
    Task<IEnumerable<ProductionOrder>> GetByStatusAsync(Guid companyId, string status);
    Task<IEnumerable<ProductionOrder>> GetByBomAsync(Guid bomId);
    Task<IEnumerable<ProductionOrder>> GetByFinishedGoodAsync(Guid finishedGoodId);
    Task<(IEnumerable<ProductionOrder> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? companyId = null,
        string? searchTerm = null,
        string? status = null,
        Guid? bomId = null,
        Guid? finishedGoodId = null,
        Guid? warehouseId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null);
    Task<ProductionOrder> AddAsync(ProductionOrder entity);
    Task UpdateAsync(ProductionOrder entity);
    Task UpdateStatusAsync(Guid id, string status, Guid? userId = null);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<string> GenerateOrderNumberAsync(Guid companyId);
}

using Core.Entities.Manufacturing;

namespace Core.Interfaces.Manufacturing;

public interface IBomRepository
{
    Task<BillOfMaterials?> GetByIdAsync(Guid id);
    Task<BillOfMaterials?> GetByIdWithItemsAsync(Guid id);
    Task<IEnumerable<BillOfMaterials>> GetAllAsync(Guid companyId);
    Task<IEnumerable<BillOfMaterials>> GetActiveAsync(Guid companyId);
    Task<IEnumerable<BillOfMaterials>> GetByFinishedGoodAsync(Guid finishedGoodId);
    Task<BillOfMaterials?> GetActiveBomForProductAsync(Guid finishedGoodId);
    Task<(IEnumerable<BillOfMaterials> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? companyId = null,
        string? searchTerm = null,
        Guid? finishedGoodId = null,
        bool? isActive = null);
    Task<BillOfMaterials> AddAsync(BillOfMaterials entity);
    Task UpdateAsync(BillOfMaterials entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId = null);
}

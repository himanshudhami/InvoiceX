using Core.Entities;

namespace Core.Interfaces;

public interface IAssetsRepository
{
    Task<Assets?> GetByIdAsync(Guid id);
    Task<(IEnumerable<Assets> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<IEnumerable<Assets>> GetAllAsync();

    Task<Assets> AddAsync(Assets entity);
    Task UpdateAsync(Assets entity);
    Task DeleteAsync(Guid id);

    Task<IEnumerable<AssetAssignments>> GetAssignmentsAsync(Guid assetId);
    Task<IEnumerable<AssetAssignments>> GetAllAssignmentsAsync();
    Task<AssetAssignments?> GetAssignmentByIdAsync(Guid assignmentId);
    Task<AssetAssignments> AddAssignmentAsync(AssetAssignments assignment);
    Task ReturnAssignmentAsync(Guid assignmentId, DateTime? returnedOn, string? conditionIn);

    // Documents
    Task<IEnumerable<AssetDocuments>> GetDocumentsAsync(Guid assetId);
    Task<AssetDocuments> AddDocumentAsync(AssetDocuments document);
    Task DeleteDocumentAsync(Guid documentId);

    // Maintenance
    Task<(IEnumerable<AssetMaintenance> Items, int TotalCount)> GetMaintenancePagedAsync(int pageNumber, int pageSize, Dictionary<string, object>? filters = null);
    Task<IEnumerable<AssetMaintenance>> GetMaintenanceByAssetAsync(Guid assetId);
    Task<AssetMaintenance?> GetMaintenanceByIdAsync(Guid id);
    Task<AssetMaintenance> AddMaintenanceAsync(AssetMaintenance maintenance);
    Task UpdateMaintenanceAsync(AssetMaintenance maintenance);

    // Disposals
    Task<AssetDisposals?> GetDisposalByAssetAsync(Guid assetId);
    Task<AssetDisposals> AddDisposalAsync(AssetDisposals disposal);

    // Cost aggregates
    Task<IDictionary<Guid, decimal>> GetMaintenanceTotalsAsync();
    
    // Bulk operations
    Task<bool> AssetTagExistsAsync(Guid companyId, string assetTag);
}





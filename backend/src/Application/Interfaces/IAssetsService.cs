using Application.DTOs.Assets;
using Core.Common;
using Core.Entities;
using AssetsEntity = Core.Entities.Assets;

namespace Application.Interfaces;

public interface IAssetsService
{
    Task<Result<AssetsEntity>> GetByIdAsync(Guid id);
    Task<Result<(IEnumerable<AssetsEntity> Items, int TotalCount)>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortDescending = false, Dictionary<string, object>? filters = null);
    Task<Result<AssetsEntity>> CreateAsync(CreateAssetDto dto);
    Task<Result> UpdateAsync(Guid id, UpdateAssetDto dto);
    Task<Result> DeleteAsync(Guid id);

    Task<Result<IEnumerable<AssetDocuments>>> GetDocumentsAsync(Guid assetId);
    Task<Result<AssetDocuments>> AddDocumentAsync(Guid assetId, CreateAssetDocumentDto dto);
    Task<Result> DeleteDocumentAsync(Guid documentId);

    Task<Result<IEnumerable<AssetAssignments>>> GetAssignmentsAsync(Guid assetId);
    Task<Result<IEnumerable<AssetAssignments>>> GetAllAssignmentsAsync();
    Task<Result<IEnumerable<AssetAssignments>>> GetAssignmentsByEmployeeAsync(Guid employeeId);
    Task<Result<AssetAssignments>> AddAssignmentAsync(Guid assetId, CreateAssetAssignmentDto dto);
    Task<Result> ReturnAssignmentAsync(Guid assignmentId, ReturnAssetAssignmentDto dto);

    Task<Result<(IEnumerable<AssetMaintenance> Items, int TotalCount)>> GetMaintenancePagedAsync(int pageNumber, int pageSize, Dictionary<string, object>? filters = null);
    Task<Result<IEnumerable<AssetMaintenance>>> GetMaintenanceByAssetAsync(Guid assetId);
    Task<Result<AssetMaintenance>> CreateMaintenanceAsync(Guid assetId, CreateAssetMaintenanceDto dto);
    Task<Result> UpdateMaintenanceAsync(Guid maintenanceId, UpdateAssetMaintenanceDto dto);

    Task<Result<AssetDisposals>> DisposeAssetAsync(Guid assetId, CreateAssetDisposalDto dto);

    Task<Result<AssetCostSummaryDto>> GetCostSummaryAsync(Guid assetId);
    Task<Result<AssetCostReportDto>> GetCostReportAsync(Guid? companyId = null);
    
    Task<Result<BulkAssetsResultDto>> BulkCreateAsync(BulkAssetsDto dto);

    // Loan-related methods
    Task<Result<AssetsEntity>> LinkAssetToLoanAsync(Guid assetId, Guid loanId);
    Task<Result<IEnumerable<AssetsEntity>>> GetAssetsByLoanAsync(Guid loanId);
}





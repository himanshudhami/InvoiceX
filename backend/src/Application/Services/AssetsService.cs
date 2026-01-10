using Application.Common;
using Application.DTOs.Assets;
using Application.Interfaces;
using Application.Interfaces.Audit;
using Application.Services.Assets;
using Application.Validators.Assets;
using AutoMapper;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using FluentValidation;
using AssetsEntity = Core.Entities.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

public class AssetsService : IAssetsService
{
    private readonly IAssetsRepository _repository;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateAssetDto> _createValidator;
    private readonly IValidator<UpdateAssetDto> _updateValidator;
    private readonly AssetAssignmentService _assignmentService;
    private readonly AssetDocumentService _documentService;
    private readonly AssetMaintenanceService _maintenanceService;
    private readonly AssetDisposalService _disposalService;
    private readonly AssetCostService _costService;
    private readonly AssetBulkService _bulkService;

    public AssetsService(
        IAssetsRepository repository,
        IAuditService auditService,
        IMapper mapper,
        IValidator<CreateAssetDto> createValidator,
        IValidator<UpdateAssetDto> updateValidator,
        AssetAssignmentService assignmentService,
        AssetDocumentService documentService,
        AssetMaintenanceService maintenanceService,
        AssetDisposalService disposalService,
        AssetCostService costService,
        AssetBulkService bulkService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _assignmentService = assignmentService ?? throw new ArgumentNullException(nameof(assignmentService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
        _disposalService = disposalService ?? throw new ArgumentNullException(nameof(disposalService));
        _costService = costService ?? throw new ArgumentNullException(nameof(costService));
        _bulkService = bulkService ?? throw new ArgumentNullException(nameof(bulkService));
    }

    // Core CRUD operations
    public async Task<Result<AssetsEntity>> GetByIdAsync(Guid id)
    {
        var validation = ServiceExtensions.ValidateGuid(id);
        if (validation.IsFailure)
            return validation.Error!;

        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"Asset {id} not found");

        return Result<AssetsEntity>.Success(entity);
    }

    public async Task<Result<(IEnumerable<AssetsEntity> Items, int TotalCount)>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortDescending = false, Dictionary<string, object>? filters = null)
    {
        var validation = ServiceExtensions.ValidatePagination(pageNumber, pageSize);
        if (validation.IsFailure)
            return validation.Error!;

        var result = await _repository.GetPagedAsync(pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
        return Result<(IEnumerable<AssetsEntity> Items, int TotalCount)>.Success(result);
    }

    public async Task<Result<AssetsEntity>> CreateAsync(CreateAssetDto dto)
    {
        var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var entity = _mapper.Map<AssetsEntity>(dto);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(entity);

        // Audit trail
        if (created.CompanyId != Guid.Empty)
        {
            await _auditService.AuditCreateAsync(created, created.Id, created.CompanyId, created.Name);
        }

        return Result<AssetsEntity>.Success(created);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateAssetDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(id);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound($"Asset {id} not found");

        // Capture state before update for audit trail
        var oldEntity = _mapper.Map<AssetsEntity>(existing);

        _mapper.Map(dto, existing);
        existing.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(existing);

        // Audit trail
        if (existing.CompanyId != Guid.Empty)
        {
            await _auditService.AuditUpdateAsync(oldEntity, existing, existing.Id, existing.CompanyId, existing.Name);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var validation = ServiceExtensions.ValidateGuid(id);
        if (validation.IsFailure)
            return validation.Error!;

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound("Asset not found");

        // Audit trail before delete
        if (existing.CompanyId != Guid.Empty)
        {
            await _auditService.AuditDeleteAsync(existing, existing.Id, existing.CompanyId, existing.Name);
        }

        await _repository.DeleteAsync(id);
        return Result.Success();
    }

    // Assignment operations - delegated
    public async Task<Result<IEnumerable<AssetAssignments>>> GetAssignmentsAsync(Guid assetId)
        => await _assignmentService.GetAssignmentsAsync(assetId);

    public async Task<Result<IEnumerable<AssetAssignments>>> GetAllAssignmentsAsync()
        => await _assignmentService.GetAllAssignmentsAsync();

    public async Task<Result<IEnumerable<AssetAssignments>>> GetAssignmentsByEmployeeAsync(Guid employeeId)
        => await _assignmentService.GetAssignmentsByEmployeeAsync(employeeId);

    public async Task<Result<AssetAssignments>> AddAssignmentAsync(Guid assetId, CreateAssetAssignmentDto dto)
        => await _assignmentService.AddAssignmentAsync(assetId, dto);

    public async Task<Result> ReturnAssignmentAsync(Guid assignmentId, ReturnAssetAssignmentDto dto)
        => await _assignmentService.ReturnAssignmentAsync(assignmentId, dto);

    // Document operations - delegated
    public async Task<Result<IEnumerable<AssetDocuments>>> GetDocumentsAsync(Guid assetId)
        => await _documentService.GetDocumentsAsync(assetId);

    public async Task<Result<AssetDocuments>> AddDocumentAsync(Guid assetId, CreateAssetDocumentDto dto)
        => await _documentService.AddDocumentAsync(assetId, dto);

    public async Task<Result> DeleteDocumentAsync(Guid documentId)
        => await _documentService.DeleteDocumentAsync(documentId);

    // Maintenance operations - delegated
    public async Task<Result<(IEnumerable<AssetMaintenance> Items, int TotalCount)>> GetMaintenancePagedAsync(int pageNumber, int pageSize, Dictionary<string, object>? filters = null)
        => await _maintenanceService.GetMaintenancePagedAsync(pageNumber, pageSize, filters);

    public async Task<Result<IEnumerable<AssetMaintenance>>> GetMaintenanceByAssetAsync(Guid assetId)
        => await _maintenanceService.GetMaintenanceByAssetAsync(assetId);

    public async Task<Result<AssetMaintenance>> CreateMaintenanceAsync(Guid assetId, CreateAssetMaintenanceDto dto)
        => await _maintenanceService.CreateMaintenanceAsync(assetId, dto);

    public async Task<Result> UpdateMaintenanceAsync(Guid maintenanceId, UpdateAssetMaintenanceDto dto)
        => await _maintenanceService.UpdateMaintenanceAsync(maintenanceId, dto);

    // Disposal operations - delegated
    public async Task<Result<AssetDisposals>> DisposeAssetAsync(Guid assetId, CreateAssetDisposalDto dto)
        => await _disposalService.DisposeAssetAsync(assetId, dto);

    // Cost operations - delegated
    public async Task<Result<AssetCostSummaryDto>> GetCostSummaryAsync(Guid assetId)
        => await _costService.GetCostSummaryAsync(assetId);

    public async Task<Result<AssetCostReportDto>> GetCostReportAsync(Guid? companyId = null)
        => await _costService.GetCostReportAsync(companyId);

    // Bulk operations - delegated
    public async Task<Result<BulkAssetsResultDto>> BulkCreateAsync(BulkAssetsDto dto)
        => await _bulkService.BulkCreateAsync(dto);

    // Loan-related methods
    public async Task<Result<AssetsEntity>> LinkAssetToLoanAsync(Guid assetId, Guid loanId)
    {
        var assetValidation = ServiceExtensions.ValidateGuid(assetId);
        if (assetValidation.IsFailure)
            return assetValidation.Error!;

        var loanValidation = ServiceExtensions.ValidateGuid(loanId);
        if (loanValidation.IsFailure)
            return loanValidation.Error!;

        var asset = await _repository.GetByIdAsync(assetId);
        if (asset == null)
            return Error.NotFound($"Asset {assetId} not found");

        // Verify loan exists (would need ILoansRepository, but for now we'll just update the asset)
        asset.LinkedLoanId = loanId;
        asset.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(asset);

        return Result<AssetsEntity>.Success(asset);
    }

    public async Task<Result<IEnumerable<AssetsEntity>>> GetAssetsByLoanAsync(Guid loanId)
    {
        var validation = ServiceExtensions.ValidateGuid(loanId);
        if (validation.IsFailure)
            return validation.Error!;

        var allAssets = await _repository.GetAllAsync();
        var assetsByLoan = allAssets.Where(a => a.LinkedLoanId == loanId).ToList();
        return Result<IEnumerable<AssetsEntity>>.Success(assetsByLoan);
    }

    public async Task<Result<IEnumerable<AssetsEntity>>> GetAvailableAssetsAsync(Guid companyId, string? searchTerm = null)
    {
        var validation = ServiceExtensions.ValidateGuid(companyId);
        if (validation.IsFailure)
            return validation.Error!;

        var assets = await _repository.GetAvailableAssetsAsync(companyId, searchTerm);
        return Result<IEnumerable<AssetsEntity>>.Success(assets);
    }
}
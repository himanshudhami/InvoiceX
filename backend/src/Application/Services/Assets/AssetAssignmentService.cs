using Application.Common;
using Application.DTOs.Assets;
using Application.Validators.Assets;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Assets;

public class AssetAssignmentService
{
    private readonly IAssetsRepository _repository;
    private readonly IValidator<CreateAssetAssignmentDto> _createAssignmentValidator;
    private readonly IValidator<ReturnAssetAssignmentDto> _returnAssignmentValidator;

    public AssetAssignmentService(
        IAssetsRepository repository,
        IValidator<CreateAssetAssignmentDto> createAssignmentValidator,
        IValidator<ReturnAssetAssignmentDto> returnAssignmentValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _createAssignmentValidator = createAssignmentValidator ?? throw new ArgumentNullException(nameof(createAssignmentValidator));
        _returnAssignmentValidator = returnAssignmentValidator ?? throw new ArgumentNullException(nameof(returnAssignmentValidator));
    }

    public async Task<Result<IEnumerable<AssetAssignments>>> GetAssignmentsAsync(Guid assetId)
    {
        var validation = ServiceExtensions.ValidateGuid(assetId, "AssetId");
        if (validation.IsFailure)
            return validation.Error!;

        var assignments = await _repository.GetAssignmentsAsync(assetId);
        return Result<IEnumerable<AssetAssignments>>.Success(assignments);
    }

    public async Task<Result<IEnumerable<AssetAssignments>>> GetAllAssignmentsAsync()
    {
        var assignments = await _repository.GetAllAssignmentsAsync();
        return Result<IEnumerable<AssetAssignments>>.Success(assignments);
    }

    public async Task<Result<AssetAssignments>> AddAssignmentAsync(Guid assetId, CreateAssetAssignmentDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(assetId, "AssetId");
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_createAssignmentValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var asset = await _repository.GetByIdAsync(assetId);
        if (asset == null)
            return Error.NotFound("Asset not found");
        if (asset.Status != "available" && asset.Status != "reserved")
            return Error.Validation("Asset not available for assignment");

        var assignment = new AssetAssignments
        {
            AssetId = assetId,
            CompanyId = dto.CompanyId,
            EmployeeId = dto.EmployeeId,
            TargetType = dto.TargetType,
            AssignedOn = dto.AssignedOn ?? DateTime.UtcNow.Date,
            ConditionOut = dto.ConditionOut,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAssignmentAsync(assignment);
        
        // Update asset status to assigned
        asset.Status = "assigned";
        asset.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(asset);
        
        return Result<AssetAssignments>.Success(created);
    }

    public async Task<Result> ReturnAssignmentAsync(Guid assignmentId, ReturnAssetAssignmentDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(assignmentId, "AssignmentId");
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_returnAssignmentValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        // Get the assignment to find the asset
        var assignment = await _repository.GetAssignmentByIdAsync(assignmentId);
        if (assignment == null) return Error.NotFound("Assignment not found");

        await _repository.ReturnAssignmentAsync(assignmentId, dto.ReturnedOn ?? DateTime.UtcNow.Date, dto.ConditionIn);
        
        // Check if asset has any other active assignments
        var allAssignments = await _repository.GetAssignmentsAsync(assignment.AssetId);
        var hasActiveAssignments = allAssignments.Any(a => a.Id != assignmentId && a.ReturnedOn == null);
        
        // Update asset status: available if no active assignments, otherwise keep as assigned
        var asset = await _repository.GetByIdAsync(assignment.AssetId);
        if (asset != null)
        {
            asset.Status = hasActiveAssignments ? "assigned" : "available";
            asset.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(asset);
        }
        
        return Result.Success();
    }
}

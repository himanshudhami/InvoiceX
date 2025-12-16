using Application.Common;
using Application.DTOs.Assets;
using Application.Validators.Assets;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using FluentValidation;
using AssetsEntity = Core.Entities.Assets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services.Assets;

public class AssetMaintenanceService
{
    private readonly IAssetsRepository _repository;
    private readonly IValidator<CreateAssetMaintenanceDto> _createValidator;
    private readonly IValidator<UpdateAssetMaintenanceDto> _updateValidator;

    public AssetMaintenanceService(
        IAssetsRepository repository,
        IValidator<CreateAssetMaintenanceDto> createValidator,
        IValidator<UpdateAssetMaintenanceDto> updateValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
    }

    public async Task<Result<(IEnumerable<AssetMaintenance> Items, int TotalCount)>> GetMaintenancePagedAsync(int pageNumber, int pageSize, Dictionary<string, object>? filters = null)
    {
        var validation = ServiceExtensions.ValidatePagination(pageNumber, pageSize);
        if (validation.IsFailure)
            return validation.Error!;

        var result = await _repository.GetMaintenancePagedAsync(pageNumber, pageSize, filters);
        return Result<(IEnumerable<AssetMaintenance> Items, int TotalCount)>.Success(result);
    }

    public async Task<Result<IEnumerable<AssetMaintenance>>> GetMaintenanceByAssetAsync(Guid assetId)
    {
        var validation = ServiceExtensions.ValidateGuid(assetId, "AssetId");
        if (validation.IsFailure)
            return validation.Error!;

        var asset = await _repository.GetByIdAsync(assetId);
        if (asset == null)
            return Error.NotFound("Asset not found");

        var items = await _repository.GetMaintenanceByAssetAsync(assetId);
        return Result<IEnumerable<AssetMaintenance>>.Success(items);
    }

    public async Task<Result<AssetMaintenance>> CreateMaintenanceAsync(Guid assetId, CreateAssetMaintenanceDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(assetId, "AssetId");
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var asset = await _repository.GetByIdAsync(assetId);
        if (asset == null)
            return Error.NotFound("Asset not found");

        var maintenance = new AssetMaintenance
        {
            AssetId = assetId,
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            OpenedAt = dto.OpenedAt ?? DateTime.UtcNow,
            DueDate = dto.DueDate,
            Vendor = dto.Vendor,
            Cost = dto.Cost,
            Currency = dto.Currency ?? asset.Currency ?? "USD",
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddMaintenanceAsync(maintenance);

        // When maintenance is opened, set asset status to maintenance if not retired
        if (asset.Status != "retired")
        {
            asset.Status = "maintenance";
            asset.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(asset);
        }

        return Result<AssetMaintenance>.Success(created);
    }

    public async Task<Result> UpdateMaintenanceAsync(Guid maintenanceId, UpdateAssetMaintenanceDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(maintenanceId, "MaintenanceId");
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var existing = await _repository.GetMaintenanceByIdAsync(maintenanceId);
        if (existing == null)
            return Error.NotFound("Maintenance record not found");

        if (!string.IsNullOrWhiteSpace(dto.Status)) existing.Status = dto.Status!;
        if (dto.ClosedAt.HasValue) existing.ClosedAt = dto.ClosedAt;
        if (dto.DueDate.HasValue) existing.DueDate = dto.DueDate;
        if (dto.Vendor != null) existing.Vendor = dto.Vendor;
        if (dto.Cost.HasValue) existing.Cost = dto.Cost;
        if (dto.Currency != null) existing.Currency = dto.Currency;
        if (dto.Notes != null) existing.Notes = dto.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateMaintenanceAsync(existing);

        // If closed, potentially update asset status back to available
        if (existing.AssetId != Guid.Empty && dto.Status != null && dto.Status.ToLowerInvariant() is "resolved" or "closed")
        {
            var asset = await _repository.GetByIdAsync(existing.AssetId);
            if (asset != null && asset.Status == "maintenance")
            {
                asset.Status = "available";
                asset.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(asset);
            }
        }

        return Result.Success();
    }
}

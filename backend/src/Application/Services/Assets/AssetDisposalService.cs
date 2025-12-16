using Application.Common;
using Application.DTOs.Assets;
using Application.Validators.Assets;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using FluentValidation;
using AssetsEntity = Core.Entities.Assets;
using System;
using System.Threading.Tasks;

namespace Application.Services.Assets;

public class AssetDisposalService
{
    private readonly IAssetsRepository _repository;
    private readonly IValidator<CreateAssetDisposalDto> _createValidator;

    public AssetDisposalService(
        IAssetsRepository repository,
        IValidator<CreateAssetDisposalDto> createValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
    }

    public async Task<Result<AssetDisposals>> DisposeAssetAsync(Guid assetId, CreateAssetDisposalDto dto)
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

        var disposal = new AssetDisposals
        {
            AssetId = assetId,
            DisposedOn = dto.DisposedOn ?? DateTime.UtcNow.Date,
            Method = dto.Method,
            Proceeds = dto.Proceeds,
            DisposalCost = dto.DisposalCost,
            Currency = dto.Currency ?? asset.Currency ?? "USD",
            Buyer = dto.Buyer,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddDisposalAsync(disposal);

        // Update asset status to retired
        asset.Status = "retired";
        asset.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(asset);

        return Result<AssetDisposals>.Success(created);
    }
}

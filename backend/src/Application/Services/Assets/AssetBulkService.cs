using Application.Common;
using Application.DTOs.Assets;
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

namespace Application.Services.Assets;

public class AssetBulkService
{
    private readonly IAssetsRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateAssetDto> _createValidator;

    public AssetBulkService(
        IAssetsRepository repository, 
        IMapper mapper,
        IValidator<CreateAssetDto> createValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
    }

    public async Task<Result<BulkAssetsResultDto>> BulkCreateAsync(BulkAssetsDto dto)
    {
        try
        {
            if (dto.Assets == null || dto.Assets.Count == 0)
                return Error.Validation("No assets provided for bulk upload");

            var result = new BulkAssetsResultDto
            {
                TotalCount = dto.Assets.Count
            };

            for (int i = 0; i < dto.Assets.Count; i++)
            {
                var assetDto = dto.Assets[i];
                var rowNumber = i + 1;

                try
                {
                    // Basic validation
                    if (string.IsNullOrWhiteSpace(assetDto.AssetTag))
                        throw new ArgumentException("Asset tag is required", nameof(assetDto.AssetTag));

                    if (string.IsNullOrWhiteSpace(assetDto.Name))
                        throw new ArgumentException("Asset name is required", nameof(assetDto.Name));

                    if (assetDto.CompanyId == Guid.Empty)
                        throw new ArgumentException("Company ID is required", nameof(assetDto.CompanyId));

                    // Asset tag uniqueness check (per company)
                    var exists = await _repository.AssetTagExistsAsync(assetDto.CompanyId, assetDto.AssetTag);
                    if (exists)
                        throw new ArgumentException($"Asset tag {assetDto.AssetTag} already exists for this company", nameof(assetDto.AssetTag));

                    // Validate using the injected validator
                    var validationResult = await _createValidator.ValidateAsync(assetDto);
                    if (!validationResult.IsValid)
                        throw new ArgumentException(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

                    var entity = _mapper.Map<AssetsEntity>(assetDto);
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;

                    var created = await _repository.AddAsync(entity);

                    result.SuccessCount++;
                    result.CreatedIds.Add(created.Id);
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add(new BulkAssetsErrorDto
                    {
                        RowNumber = rowNumber,
                        AssetReference = assetDto.AssetTag ?? assetDto.Name,
                        ErrorMessage = ex.Message,
                        FieldName = ex is ArgumentException argEx ? argEx.ParamName : null
                    });

                    if (!dto.SkipValidationErrors)
                    {
                        return Result<BulkAssetsResultDto>.Success(result);
                    }
                }
            }

            return Result<BulkAssetsResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to bulk create assets: {ex.Message}");
        }
    }
}

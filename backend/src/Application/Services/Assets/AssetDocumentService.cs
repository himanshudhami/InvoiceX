using Application.Common;
using Application.DTOs.Assets;
using Application.Validators.Assets;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services.Assets;

public class AssetDocumentService
{
    private readonly IAssetsRepository _repository;
    private readonly IValidator<CreateAssetDocumentDto> _createValidator;

    public AssetDocumentService(
        IAssetsRepository repository,
        IValidator<CreateAssetDocumentDto> createValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
    }

    public async Task<Result<IEnumerable<AssetDocuments>>> GetDocumentsAsync(Guid assetId)
    {
        var validation = ServiceExtensions.ValidateGuid(assetId, "AssetId");
        if (validation.IsFailure)
            return validation.Error!;

        var asset = await _repository.GetByIdAsync(assetId);
        if (asset == null)
            return Error.NotFound("Asset not found");

        var docs = await _repository.GetDocumentsAsync(assetId);
        return Result<IEnumerable<AssetDocuments>>.Success(docs);
    }

    public async Task<Result<AssetDocuments>> AddDocumentAsync(Guid assetId, CreateAssetDocumentDto dto)
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

        var doc = new AssetDocuments
        {
            AssetId = assetId,
            Name = dto.Name,
            Url = dto.Url,
            ContentType = dto.ContentType,
            Notes = dto.Notes,
            UploadedAt = DateTime.UtcNow
        };

        var created = await _repository.AddDocumentAsync(doc);
        return Result<AssetDocuments>.Success(created);
    }

    public async Task<Result> DeleteDocumentAsync(Guid documentId)
    {
        var validation = ServiceExtensions.ValidateGuid(documentId, "DocumentId");
        if (validation.IsFailure)
            return validation.Error!;

        await _repository.DeleteDocumentAsync(documentId);
        return Result.Success();
    }
}

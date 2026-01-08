using Application.DTOs.Manufacturing;
using Application.Validators.Manufacturing;
using Core.Common;
using Core.Entities.Manufacturing;
using Core.Interfaces.Inventory;
using Core.Interfaces.Manufacturing;

namespace Application.Services.Manufacturing;

public interface IBomService
{
    Task<Result<BillOfMaterials>> GetByIdAsync(Guid id);
    Task<Result<BillOfMaterials>> GetByIdWithItemsAsync(Guid id);
    Task<Result<IEnumerable<BillOfMaterials>>> GetAllAsync(Guid companyId);
    Task<Result<IEnumerable<BillOfMaterials>>> GetActiveAsync(Guid companyId);
    Task<Result<IEnumerable<BillOfMaterials>>> GetByFinishedGoodAsync(Guid finishedGoodId);
    Task<Result<BillOfMaterials>> GetActiveBomForProductAsync(Guid finishedGoodId);
    Task<Result<(IEnumerable<BillOfMaterials> Items, int TotalCount)>> GetPagedAsync(
        int pageNumber, int pageSize, Guid? companyId = null, string? searchTerm = null,
        Guid? finishedGoodId = null, bool? isActive = null);
    Task<Result<BillOfMaterials>> CreateAsync(CreateBomDto dto);
    Task<Result> UpdateAsync(Guid id, UpdateBomDto dto);
    Task<Result> DeleteAsync(Guid id);
    Task<Result<BillOfMaterials>> CopyAsync(Guid id, CopyBomDto dto);
}

public class BomService : IBomService
{
    private readonly IBomRepository _repository;
    private readonly IBomItemRepository _itemRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly CreateBomDtoValidator _createValidator;
    private readonly UpdateBomDtoValidator _updateValidator;
    private readonly CopyBomDtoValidator _copyValidator;

    public BomService(
        IBomRepository repository,
        IBomItemRepository itemRepository,
        IStockItemRepository stockItemRepository)
    {
        _repository = repository;
        _itemRepository = itemRepository;
        _stockItemRepository = stockItemRepository;
        _createValidator = new CreateBomDtoValidator();
        _updateValidator = new UpdateBomDtoValidator();
        _copyValidator = new CopyBomDtoValidator();
    }

    public async Task<Result<BillOfMaterials>> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"BOM with ID {id} not found");
        return Result<BillOfMaterials>.Success(entity);
    }

    public async Task<Result<BillOfMaterials>> GetByIdWithItemsAsync(Guid id)
    {
        var entity = await _repository.GetByIdWithItemsAsync(id);
        if (entity == null)
            return Error.NotFound($"BOM with ID {id} not found");
        return Result<BillOfMaterials>.Success(entity);
    }

    public async Task<Result<IEnumerable<BillOfMaterials>>> GetAllAsync(Guid companyId)
    {
        var entities = await _repository.GetAllAsync(companyId);
        return Result<IEnumerable<BillOfMaterials>>.Success(entities);
    }

    public async Task<Result<IEnumerable<BillOfMaterials>>> GetActiveAsync(Guid companyId)
    {
        var entities = await _repository.GetActiveAsync(companyId);
        return Result<IEnumerable<BillOfMaterials>>.Success(entities);
    }

    public async Task<Result<IEnumerable<BillOfMaterials>>> GetByFinishedGoodAsync(Guid finishedGoodId)
    {
        var entities = await _repository.GetByFinishedGoodAsync(finishedGoodId);
        return Result<IEnumerable<BillOfMaterials>>.Success(entities);
    }

    public async Task<Result<BillOfMaterials>> GetActiveBomForProductAsync(Guid finishedGoodId)
    {
        var entity = await _repository.GetActiveBomForProductAsync(finishedGoodId);
        if (entity == null)
            return Error.NotFound($"No active BOM found for product {finishedGoodId}");
        return Result<BillOfMaterials>.Success(entity);
    }

    public async Task<Result<(IEnumerable<BillOfMaterials> Items, int TotalCount)>> GetPagedAsync(
        int pageNumber, int pageSize, Guid? companyId = null, string? searchTerm = null,
        Guid? finishedGoodId = null, bool? isActive = null)
    {
        var result = await _repository.GetPagedAsync(pageNumber, pageSize, companyId, searchTerm, finishedGoodId, isActive);
        return Result<(IEnumerable<BillOfMaterials>, int)>.Success(result);
    }

    public async Task<Result<BillOfMaterials>> CreateAsync(CreateBomDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Error.Validation(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

        // Validate finished good exists
        var finishedGood = await _stockItemRepository.GetByIdAsync(dto.FinishedGoodId);
        if (finishedGood == null)
            return Error.NotFound($"Finished good with ID {dto.FinishedGoodId} not found");

        // Validate code uniqueness
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var codeExists = await _repository.CodeExistsAsync(dto.CompanyId!.Value, dto.Code);
            if (codeExists)
                return Error.Conflict($"BOM with code '{dto.Code}' already exists");
        }

        // Validate all components exist
        foreach (var item in dto.Items)
        {
            var component = await _stockItemRepository.GetByIdAsync(item.ComponentId);
            if (component == null)
                return Error.NotFound($"Component with ID {item.ComponentId} not found");

            // Prevent using the finished good as its own component
            if (item.ComponentId == dto.FinishedGoodId)
                return Error.Validation("A product cannot be a component of itself");
        }

        var entity = new BillOfMaterials
        {
            Id = Guid.NewGuid(),
            CompanyId = dto.CompanyId!.Value,
            FinishedGoodId = dto.FinishedGoodId,
            Name = dto.Name,
            Code = dto.Code,
            Version = dto.Version,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            OutputQuantity = dto.OutputQuantity,
            OutputUnitId = dto.OutputUnitId,
            IsActive = dto.IsActive,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var createdEntity = await _repository.AddAsync(entity);

        // Create items
        var items = dto.Items.Select((itemDto, index) => new BomItem
        {
            Id = Guid.NewGuid(),
            BomId = createdEntity.Id,
            ComponentId = itemDto.ComponentId,
            Quantity = itemDto.Quantity,
            UnitId = itemDto.UnitId,
            ScrapPercentage = itemDto.ScrapPercentage,
            IsOptional = itemDto.IsOptional,
            Sequence = itemDto.Sequence > 0 ? itemDto.Sequence : index,
            Notes = itemDto.Notes,
            CreatedAt = DateTime.UtcNow
        });

        await _itemRepository.AddRangeAsync(items);

        return Result<BillOfMaterials>.Success(createdEntity);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateBomDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Error.Validation(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            return Error.NotFound($"BOM with ID {id} not found");

        // Validate finished good exists
        var finishedGood = await _stockItemRepository.GetByIdAsync(dto.FinishedGoodId);
        if (finishedGood == null)
            return Error.NotFound($"Finished good with ID {dto.FinishedGoodId} not found");

        // Validate code uniqueness
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var codeExists = await _repository.CodeExistsAsync(existingEntity.CompanyId, dto.Code, id);
            if (codeExists)
                return Error.Conflict($"BOM with code '{dto.Code}' already exists");
        }

        // Validate all components exist
        foreach (var item in dto.Items)
        {
            var component = await _stockItemRepository.GetByIdAsync(item.ComponentId);
            if (component == null)
                return Error.NotFound($"Component with ID {item.ComponentId} not found");

            if (item.ComponentId == dto.FinishedGoodId)
                return Error.Validation("A product cannot be a component of itself");
        }

        existingEntity.FinishedGoodId = dto.FinishedGoodId;
        existingEntity.Name = dto.Name;
        existingEntity.Code = dto.Code;
        existingEntity.Version = dto.Version;
        existingEntity.EffectiveFrom = dto.EffectiveFrom;
        existingEntity.EffectiveTo = dto.EffectiveTo;
        existingEntity.OutputQuantity = dto.OutputQuantity;
        existingEntity.OutputUnitId = dto.OutputUnitId;
        existingEntity.IsActive = dto.IsActive;
        existingEntity.Notes = dto.Notes;
        existingEntity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingEntity);

        // Replace items
        var items = dto.Items.Select((itemDto, index) => new BomItem
        {
            Id = itemDto.Id ?? Guid.NewGuid(),
            BomId = id,
            ComponentId = itemDto.ComponentId,
            Quantity = itemDto.Quantity,
            UnitId = itemDto.UnitId,
            ScrapPercentage = itemDto.ScrapPercentage,
            IsOptional = itemDto.IsOptional,
            Sequence = itemDto.Sequence > 0 ? itemDto.Sequence : index,
            Notes = itemDto.Notes,
            CreatedAt = DateTime.UtcNow
        });

        await _itemRepository.ReplaceItemsAsync(id, items);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            return Error.NotFound($"BOM with ID {id} not found");

        // Items will be cascade deleted due to FK constraint
        await _repository.DeleteAsync(id);

        return Result.Success();
    }

    public async Task<Result<BillOfMaterials>> CopyAsync(Guid id, CopyBomDto dto)
    {
        var validation = await _copyValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Error.Validation(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

        var sourceBom = await _repository.GetByIdWithItemsAsync(id);
        if (sourceBom == null)
            return Error.NotFound($"BOM with ID {id} not found");

        // Validate code uniqueness
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var codeExists = await _repository.CodeExistsAsync(sourceBom.CompanyId, dto.Code);
            if (codeExists)
                return Error.Conflict($"BOM with code '{dto.Code}' already exists");
        }

        var newBom = new BillOfMaterials
        {
            Id = Guid.NewGuid(),
            CompanyId = sourceBom.CompanyId,
            FinishedGoodId = sourceBom.FinishedGoodId,
            Name = dto.Name,
            Code = dto.Code,
            Version = dto.Version,
            EffectiveFrom = sourceBom.EffectiveFrom,
            EffectiveTo = sourceBom.EffectiveTo,
            OutputQuantity = sourceBom.OutputQuantity,
            OutputUnitId = sourceBom.OutputUnitId,
            IsActive = true,
            Notes = sourceBom.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var createdBom = await _repository.AddAsync(newBom);

        // Copy items
        if (sourceBom.Items != null && sourceBom.Items.Any())
        {
            var newItems = sourceBom.Items.Select(item => new BomItem
            {
                Id = Guid.NewGuid(),
                BomId = createdBom.Id,
                ComponentId = item.ComponentId,
                Quantity = item.Quantity,
                UnitId = item.UnitId,
                ScrapPercentage = item.ScrapPercentage,
                IsOptional = item.IsOptional,
                Sequence = item.Sequence,
                Notes = item.Notes,
                CreatedAt = DateTime.UtcNow
            });

            await _itemRepository.AddRangeAsync(newItems);
        }

        return Result<BillOfMaterials>.Success(createdBom);
    }
}

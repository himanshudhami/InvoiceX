using Application.Common;
using Application.Interfaces.Inventory;
using Application.DTOs.Inventory;
using Core.Entities.Inventory;
using Core.Interfaces.Inventory;
using Core.Common;
using AutoMapper;
using FluentValidation;

namespace Application.Services.Inventory
{
    /// <summary>
    /// Service implementation for Stock Item operations
    /// </summary>
    public class StockItemService : IStockItemService
    {
        private readonly IStockItemRepository _repository;
        private readonly IUnitConversionRepository _conversionRepository;
        private readonly IStockMovementRepository _movementRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateStockItemDto> _createValidator;
        private readonly IValidator<UpdateStockItemDto> _updateValidator;

        public StockItemService(
            IStockItemRepository repository,
            IUnitConversionRepository conversionRepository,
            IStockMovementRepository movementRepository,
            IMapper mapper,
            IValidator<CreateStockItemDto> createValidator,
            IValidator<UpdateStockItemDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _conversionRepository = conversionRepository ?? throw new ArgumentNullException(nameof(conversionRepository));
            _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        public async Task<Result<StockItem>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Stock Item with ID {id} not found");

            return Result<StockItem>.Success(entity);
        }

        public async Task<Result<StockItem>> GetByIdWithDetailsAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdWithDetailsAsync(id);
            if (entity == null)
                return Error.NotFound($"Stock Item with ID {id} not found");

            return Result<StockItem>.Success(entity);
        }

        public async Task<Result<IEnumerable<StockItem>>> GetByCompanyIdAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetByCompanyIdAsync(companyId);
            return Result<IEnumerable<StockItem>>.Success(entities);
        }

        public async Task<Result<(IEnumerable<StockItem> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            var validation = ServiceExtensions.ValidatePagination(pageNumber, pageSize);
            if (validation.IsFailure)
                return validation.Error!;

            var result = await _repository.GetPagedAsync(
                pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);

            return Result<(IEnumerable<StockItem> Items, int TotalCount)>.Success(result);
        }

        public async Task<Result<IEnumerable<StockItem>>> GetActiveAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetActiveAsync(companyId);
            return Result<IEnumerable<StockItem>>.Success(entities);
        }

        public async Task<Result<IEnumerable<StockItem>>> GetLowStockItemsAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetLowStockItemsAsync(companyId);
            return Result<IEnumerable<StockItem>>.Success(entities);
        }

        public async Task<Result<IEnumerable<StockItem>>> GetByStockGroupIdAsync(Guid stockGroupId)
        {
            var validation = ServiceExtensions.ValidateGuid(stockGroupId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetByStockGroupIdAsync(stockGroupId);
            return Result<IEnumerable<StockItem>>.Success(entities);
        }

        public async Task<Result<StockPositionDto>> GetStockPositionAsync(Guid stockItemId, Guid? warehouseId = null)
        {
            var validation = ServiceExtensions.ValidateGuid(stockItemId);
            if (validation.IsFailure)
                return validation.Error!;

            var item = await _repository.GetByIdWithDetailsAsync(stockItemId);
            if (item == null)
                return Error.NotFound($"Stock Item with ID {stockItemId} not found");

            var position = await _movementRepository.GetStockPositionAsync(stockItemId, warehouseId);

            var dto = new StockPositionDto
            {
                StockItemId = stockItemId,
                StockItemName = item.Name,
                Sku = item.Sku,
                WarehouseId = warehouseId,
                Quantity = item.OpeningQuantity + position.Quantity,
                Value = item.OpeningValue + position.Value,
                ReorderLevel = item.ReorderLevel
            };

            return Result<StockPositionDto>.Success(dto);
        }

        public async Task<Result<StockItem>> CreateAsync(CreateStockItemDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Check for duplicate name
            if (dto.CompanyId.HasValue)
            {
                var exists = await _repository.ExistsAsync(dto.CompanyId.Value, dto.Name);
                if (exists)
                    return Error.Conflict($"Stock Item with name '{dto.Name}' already exists");

                // Check for duplicate SKU if provided
                if (!string.IsNullOrWhiteSpace(dto.Sku))
                {
                    var skuExists = await _repository.SkuExistsAsync(dto.CompanyId.Value, dto.Sku);
                    if (skuExists)
                        return Error.Conflict($"Stock Item with SKU '{dto.Sku}' already exists");
                }
            }

            var entity = _mapper.Map<StockItem>(dto);
            entity.CurrentQuantity = dto.OpeningQuantity;
            entity.CurrentValue = dto.OpeningValue;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);

            // Create unit conversions if provided
            if (dto.UnitConversions != null && dto.UnitConversions.Any())
            {
                foreach (var conversion in dto.UnitConversions)
                {
                    var unitConversion = new UnitConversion
                    {
                        StockItemId = createdEntity.Id,
                        FromUnitId = conversion.FromUnitId,
                        ToUnitId = conversion.ToUnitId,
                        ConversionFactor = conversion.ConversionFactor,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _conversionRepository.AddAsync(unitConversion);
                }
            }

            return Result<StockItem>.Success(createdEntity);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdateStockItemDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Stock Item with ID {id} not found");

            // Check for duplicate name if changed
            if (dto.Name != existingEntity.Name)
            {
                var exists = await _repository.ExistsAsync(existingEntity.CompanyId, dto.Name, id);
                if (exists)
                    return Error.Conflict($"Stock Item with name '{dto.Name}' already exists");
            }

            // Check for duplicate SKU if changed
            if (!string.IsNullOrWhiteSpace(dto.Sku) && dto.Sku != existingEntity.Sku)
            {
                var skuExists = await _repository.SkuExistsAsync(existingEntity.CompanyId, dto.Sku, id);
                if (skuExists)
                    return Error.Conflict($"Stock Item with SKU '{dto.Sku}' already exists");
            }

            _mapper.Map(dto, existingEntity);
            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);

            // Update unit conversions if provided
            if (dto.UnitConversions != null)
            {
                var conversions = dto.UnitConversions.Select(c => new UnitConversion
                {
                    StockItemId = id,
                    FromUnitId = c.FromUnitId,
                    ToUnitId = c.ToUnitId,
                    ConversionFactor = c.ConversionFactor,
                    CreatedAt = DateTime.UtcNow
                });
                await _conversionRepository.ReplaceConversionsAsync(id, conversions);
            }

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Stock Item with ID {id} not found");

            // Check if item has movements
            var hasMovements = await _repository.HasMovementsAsync(id);
            if (hasMovements)
                return Error.Conflict("Cannot delete stock item with movements. Deactivate it instead.");

            // Delete unit conversions first
            await _conversionRepository.DeleteByStockItemIdAsync(id);

            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        public async Task<Result> RecalculateStockAsync(Guid stockItemId)
        {
            var validation = ServiceExtensions.ValidateGuid(stockItemId);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(stockItemId);
            if (existingEntity == null)
                return Error.NotFound($"Stock Item with ID {stockItemId} not found");

            await _repository.RecalculateStockAsync(stockItemId);
            return Result.Success();
        }
    }
}

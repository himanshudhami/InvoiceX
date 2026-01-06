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
    /// Service implementation for Stock Movement operations
    /// </summary>
    public class StockMovementService : IStockMovementService
    {
        private readonly IStockMovementRepository _repository;
        private readonly IStockItemRepository _stockItemRepository;
        private readonly IStockBatchRepository _batchRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateStockMovementDto> _createValidator;
        private readonly IValidator<UpdateStockMovementDto> _updateValidator;

        public StockMovementService(
            IStockMovementRepository repository,
            IStockItemRepository stockItemRepository,
            IStockBatchRepository batchRepository,
            IMapper mapper,
            IValidator<CreateStockMovementDto> createValidator,
            IValidator<UpdateStockMovementDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _stockItemRepository = stockItemRepository ?? throw new ArgumentNullException(nameof(stockItemRepository));
            _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        public async Task<Result<StockMovement>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Stock Movement with ID {id} not found");

            return Result<StockMovement>.Success(entity);
        }

        public async Task<Result<(IEnumerable<StockMovement> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<StockMovement> Items, int TotalCount)>.Success(result);
        }

        public async Task<Result<IEnumerable<StockLedgerDto>>> GetStockLedgerAsync(
            Guid stockItemId,
            Guid? warehouseId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            var validation = ServiceExtensions.ValidateGuid(stockItemId);
            if (validation.IsFailure)
                return validation.Error!;

            var stockItem = await _stockItemRepository.GetByIdAsync(stockItemId);
            if (stockItem == null)
                return Error.NotFound($"Stock Item with ID {stockItemId} not found");

            var movements = await _repository.GetStockLedgerAsync(stockItemId, warehouseId, fromDate, toDate);

            var ledgerEntries = new List<StockLedgerDto>();
            decimal runningQty = stockItem.OpeningQuantity;
            decimal runningVal = stockItem.OpeningValue;

            // Add opening balance entry
            ledgerEntries.Add(new StockLedgerDto
            {
                MovementDate = fromDate ?? DateOnly.FromDateTime(DateTime.MinValue),
                MovementType = "opening",
                RunningQuantity = runningQty,
                RunningValue = runningVal
            });

            foreach (var movement in movements)
            {
                runningQty += movement.Quantity;
                runningVal += movement.Value ?? 0;

                ledgerEntries.Add(new StockLedgerDto
                {
                    Id = movement.Id,
                    MovementDate = movement.MovementDate,
                    MovementType = movement.MovementType,
                    SourceType = movement.SourceType,
                    SourceNumber = movement.SourceNumber,
                    QuantityIn = movement.Quantity > 0 ? movement.Quantity : null,
                    QuantityOut = movement.Quantity < 0 ? Math.Abs(movement.Quantity) : null,
                    Rate = movement.Rate,
                    ValueIn = movement.Value > 0 ? movement.Value : null,
                    ValueOut = movement.Value < 0 ? Math.Abs(movement.Value ?? 0) : null,
                    RunningQuantity = runningQty,
                    RunningValue = runningVal,
                    Notes = movement.Notes
                });
            }

            return Result<IEnumerable<StockLedgerDto>>.Success(ledgerEntries);
        }

        public async Task<Result<StockPositionDto>> GetStockPositionAsync(
            Guid stockItemId,
            Guid? warehouseId = null,
            DateOnly? asOfDate = null)
        {
            var validation = ServiceExtensions.ValidateGuid(stockItemId);
            if (validation.IsFailure)
                return validation.Error!;

            var stockItem = await _stockItemRepository.GetByIdWithDetailsAsync(stockItemId);
            if (stockItem == null)
                return Error.NotFound($"Stock Item with ID {stockItemId} not found");

            var position = await _repository.GetStockPositionAsync(stockItemId, warehouseId, asOfDate);

            var dto = new StockPositionDto
            {
                StockItemId = stockItemId,
                StockItemName = stockItem.Name,
                Sku = stockItem.Sku,
                WarehouseId = warehouseId,
                Quantity = stockItem.OpeningQuantity + position.Quantity,
                Value = stockItem.OpeningValue + position.Value,
                ReorderLevel = stockItem.ReorderLevel
            };

            return Result<StockPositionDto>.Success(dto);
        }

        public async Task<Result<StockMovement>> RecordMovementAsync(CreateStockMovementDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Validate stock item exists
            var stockItem = await _stockItemRepository.GetByIdAsync(dto.StockItemId);
            if (stockItem == null)
                return Error.NotFound($"Stock Item with ID {dto.StockItemId} not found");

            // Validate batch if provided
            if (dto.BatchId.HasValue)
            {
                var batch = await _batchRepository.GetByIdAsync(dto.BatchId.Value);
                if (batch == null)
                    return Error.NotFound($"Batch with ID {dto.BatchId} not found");

                // For outgoing movements, check batch has sufficient quantity
                if (dto.Quantity < 0 && batch.Quantity < Math.Abs(dto.Quantity))
                    return Error.Validation($"Insufficient batch quantity. Available: {batch.Quantity}");
            }

            // For outgoing movements on non-batch items, check overall stock
            if (dto.Quantity < 0 && !dto.BatchId.HasValue)
            {
                var currentPosition = await _repository.GetStockPositionAsync(dto.StockItemId, dto.WarehouseId);
                var totalAvailable = stockItem.OpeningQuantity + currentPosition.Quantity;
                if (totalAvailable < Math.Abs(dto.Quantity))
                    return Error.Validation($"Insufficient stock. Available: {totalAvailable}");
            }

            var entity = _mapper.Map<StockMovement>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            // Calculate value if not provided
            if (!entity.Value.HasValue && entity.Rate.HasValue)
            {
                entity.Value = entity.Quantity * entity.Rate.Value;
            }

            var createdEntity = await _repository.AddAsync(entity);

            // Update stock item current quantity/value
            await _stockItemRepository.RecalculateStockAsync(dto.StockItemId);

            // Update batch quantity if applicable
            if (dto.BatchId.HasValue)
            {
                await _batchRepository.UpdateQuantityAsync(dto.BatchId.Value, dto.Quantity, dto.Value ?? 0);
            }

            // Recalculate running totals
            await _repository.RecalculateRunningTotalsAsync(dto.StockItemId, dto.WarehouseId);

            return Result<StockMovement>.Success(createdEntity);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdateStockMovementDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Stock Movement with ID {id} not found");

            var originalQuantity = existingEntity.Quantity;
            var originalValue = existingEntity.Value ?? 0;

            _mapper.Map(dto, existingEntity);

            // Recalculate value if rate changed
            if (existingEntity.Rate.HasValue)
            {
                existingEntity.Value = existingEntity.Quantity * existingEntity.Rate.Value;
            }

            await _repository.UpdateAsync(existingEntity);

            // Update stock item and batch balances
            await _stockItemRepository.RecalculateStockAsync(existingEntity.StockItemId);

            if (existingEntity.BatchId.HasValue)
            {
                var quantityDiff = existingEntity.Quantity - originalQuantity;
                var valueDiff = (existingEntity.Value ?? 0) - originalValue;
                await _batchRepository.UpdateQuantityAsync(existingEntity.BatchId.Value, quantityDiff, valueDiff);
            }

            // Recalculate running totals
            await _repository.RecalculateRunningTotalsAsync(existingEntity.StockItemId, existingEntity.WarehouseId);

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Stock Movement with ID {id} not found");

            // Reverse batch quantity before deletion
            if (existingEntity.BatchId.HasValue)
            {
                await _batchRepository.UpdateQuantityAsync(
                    existingEntity.BatchId.Value,
                    -existingEntity.Quantity,
                    -(existingEntity.Value ?? 0));
            }

            await _repository.DeleteAsync(id);

            // Recalculate stock item balance
            await _stockItemRepository.RecalculateStockAsync(existingEntity.StockItemId);

            // Recalculate running totals
            await _repository.RecalculateRunningTotalsAsync(existingEntity.StockItemId, existingEntity.WarehouseId);

            return Result.Success();
        }

        public async Task<Result> RecalculateRunningTotalsAsync(Guid stockItemId, Guid? warehouseId = null)
        {
            var validation = ServiceExtensions.ValidateGuid(stockItemId);
            if (validation.IsFailure)
                return validation.Error!;

            await _repository.RecalculateRunningTotalsAsync(stockItemId, warehouseId);
            return Result.Success();
        }
    }
}

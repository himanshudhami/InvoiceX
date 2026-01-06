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
    /// Service implementation for Stock Transfer operations
    /// </summary>
    public class StockTransferService : IStockTransferService
    {
        private readonly IStockTransferRepository _repository;
        private readonly IStockTransferItemRepository _itemRepository;
        private readonly IStockMovementRepository _movementRepository;
        private readonly IStockItemRepository _stockItemRepository;
        private readonly IStockBatchRepository _batchRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateStockTransferDto> _createValidator;
        private readonly IValidator<UpdateStockTransferDto> _updateValidator;

        public StockTransferService(
            IStockTransferRepository repository,
            IStockTransferItemRepository itemRepository,
            IStockMovementRepository movementRepository,
            IStockItemRepository stockItemRepository,
            IStockBatchRepository batchRepository,
            IMapper mapper,
            IValidator<CreateStockTransferDto> createValidator,
            IValidator<UpdateStockTransferDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
            _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
            _stockItemRepository = stockItemRepository ?? throw new ArgumentNullException(nameof(stockItemRepository));
            _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        public async Task<Result<StockTransfer>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Stock Transfer with ID {id} not found");

            return Result<StockTransfer>.Success(entity);
        }

        public async Task<Result<StockTransfer>> GetByIdWithItemsAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdWithItemsAsync(id);
            if (entity == null)
                return Error.NotFound($"Stock Transfer with ID {id} not found");

            return Result<StockTransfer>.Success(entity);
        }

        public async Task<Result<(IEnumerable<StockTransfer> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<StockTransfer> Items, int TotalCount)>.Success(result);
        }

        public async Task<Result<IEnumerable<StockTransfer>>> GetPendingTransfersAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetPendingTransfersAsync(companyId);
            return Result<IEnumerable<StockTransfer>>.Success(entities);
        }

        public async Task<Result<IEnumerable<StockTransfer>>> GetByStatusAsync(Guid companyId, string status)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetByStatusAsync(companyId, status);
            return Result<IEnumerable<StockTransfer>>.Success(entities);
        }

        public async Task<Result<StockTransfer>> CreateAsync(CreateStockTransferDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Generate transfer number if not provided
            if (string.IsNullOrWhiteSpace(dto.TransferNumber) && dto.CompanyId.HasValue)
            {
                dto.TransferNumber = await _repository.GenerateTransferNumberAsync(dto.CompanyId.Value);
            }

            // Validate stock availability for each item
            foreach (var item in dto.Items)
            {
                var stockItem = await _stockItemRepository.GetByIdAsync(item.StockItemId);
                if (stockItem == null)
                    return Error.NotFound($"Stock Item with ID {item.StockItemId} not found");

                // Check available stock at source warehouse
                var position = await _movementRepository.GetStockPositionAsync(item.StockItemId, dto.FromWarehouseId);
                var available = stockItem.OpeningQuantity + position.Quantity;

                if (available < item.Quantity)
                    return Error.Validation($"Insufficient stock for '{stockItem.Name}'. Available: {available}, Requested: {item.Quantity}");
            }

            var entity = _mapper.Map<StockTransfer>(dto);
            entity.Status = "draft";
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);

            // Create transfer items
            foreach (var itemDto in dto.Items)
            {
                var item = new StockTransferItem
                {
                    StockTransferId = createdEntity.Id,
                    StockItemId = itemDto.StockItemId,
                    BatchId = itemDto.BatchId,
                    Quantity = itemDto.Quantity,
                    Rate = itemDto.Rate,
                    Value = itemDto.Value ?? (itemDto.Quantity * (itemDto.Rate ?? 0)),
                    CreatedAt = DateTime.UtcNow
                };
                await _itemRepository.AddAsync(item);
            }

            return Result<StockTransfer>.Success(createdEntity);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdateStockTransferDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Stock Transfer with ID {id} not found");

            // Only allow updates on draft transfers
            if (existingEntity.Status != "draft")
                return Error.Validation("Only draft transfers can be updated");

            _mapper.Map(dto, existingEntity);
            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);

            // Replace items
            var items = dto.Items.Select(itemDto => new StockTransferItem
            {
                StockTransferId = id,
                StockItemId = itemDto.StockItemId,
                BatchId = itemDto.BatchId,
                Quantity = itemDto.Quantity,
                Rate = itemDto.Rate,
                Value = itemDto.Value ?? (itemDto.Quantity * (itemDto.Rate ?? 0)),
                CreatedAt = DateTime.UtcNow
            });

            await _itemRepository.ReplaceItemsAsync(id, items);

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Stock Transfer with ID {id} not found");

            // Only allow deletion of draft transfers
            if (existingEntity.Status != "draft")
                return Error.Validation("Only draft transfers can be deleted");

            // Delete items first
            await _itemRepository.DeleteByTransferIdAsync(id);
            await _repository.DeleteAsync(id);

            return Result.Success();
        }

        public async Task<Result> DispatchAsync(Guid id, Guid userId)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var transfer = await _repository.GetByIdWithItemsAsync(id);
            if (transfer == null)
                return Error.NotFound($"Stock Transfer with ID {id} not found");

            if (transfer.Status != "draft")
                return Error.Validation("Only draft transfers can be dispatched");

            // Create transfer_out movements for source warehouse
            foreach (var item in transfer.Items)
            {
                var movement = new StockMovement
                {
                    CompanyId = transfer.CompanyId,
                    StockItemId = item.StockItemId,
                    WarehouseId = transfer.FromWarehouseId,
                    BatchId = item.BatchId,
                    MovementDate = transfer.TransferDate,
                    MovementType = "transfer_out",
                    Quantity = -item.Quantity, // Negative for out
                    Rate = item.Rate,
                    Value = -(item.Value ?? 0),
                    SourceType = "stock_transfer",
                    SourceId = transfer.Id,
                    SourceNumber = transfer.TransferNumber,
                    CreatedAt = DateTime.UtcNow
                };
                await _movementRepository.AddAsync(movement);

                // Update stock item balance
                await _stockItemRepository.RecalculateStockAsync(item.StockItemId);

                // Update batch if applicable
                if (item.BatchId.HasValue)
                {
                    await _batchRepository.UpdateQuantityAsync(item.BatchId.Value, -item.Quantity, -(item.Value ?? 0));
                }
            }

            await _repository.UpdateStatusAsync(id, "in_transit", userId);

            return Result.Success();
        }

        public async Task<Result> CompleteAsync(Guid id, Guid userId)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var transfer = await _repository.GetByIdWithItemsAsync(id);
            if (transfer == null)
                return Error.NotFound($"Stock Transfer with ID {id} not found");

            if (transfer.Status != "in_transit")
                return Error.Validation("Only in-transit transfers can be completed");

            // Create transfer_in movements for destination warehouse
            foreach (var item in transfer.Items)
            {
                var movement = new StockMovement
                {
                    CompanyId = transfer.CompanyId,
                    StockItemId = item.StockItemId,
                    WarehouseId = transfer.ToWarehouseId,
                    BatchId = item.BatchId,
                    MovementDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    MovementType = "transfer_in",
                    Quantity = item.ReceivedQuantity ?? item.Quantity, // Use received qty if partial
                    Rate = item.Rate,
                    Value = item.Value ?? 0,
                    SourceType = "stock_transfer",
                    SourceId = transfer.Id,
                    SourceNumber = transfer.TransferNumber,
                    CreatedAt = DateTime.UtcNow
                };
                await _movementRepository.AddAsync(movement);

                // Update stock item balance
                await _stockItemRepository.RecalculateStockAsync(item.StockItemId);
            }

            await _repository.CompleteTransferAsync(id, userId);

            return Result.Success();
        }

        public async Task<Result> CancelAsync(Guid id, Guid userId)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var transfer = await _repository.GetByIdWithItemsAsync(id);
            if (transfer == null)
                return Error.NotFound($"Stock Transfer with ID {id} not found");

            if (transfer.Status == "completed" || transfer.Status == "cancelled")
                return Error.Validation("Completed or cancelled transfers cannot be cancelled");

            // If in transit, reverse the transfer_out movements
            if (transfer.Status == "in_transit")
            {
                await _movementRepository.DeleteBySourceAsync("stock_transfer", id);

                foreach (var item in transfer.Items)
                {
                    // Recalculate stock item balance
                    await _stockItemRepository.RecalculateStockAsync(item.StockItemId);

                    // Restore batch quantity if applicable
                    if (item.BatchId.HasValue)
                    {
                        await _batchRepository.UpdateQuantityAsync(item.BatchId.Value, item.Quantity, item.Value ?? 0);
                    }
                }
            }

            await _repository.CancelTransferAsync(id, userId);

            return Result.Success();
        }
    }
}

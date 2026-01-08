using Application.DTOs.Manufacturing;
using Application.DTOs.Inventory;
using Application.Validators.Manufacturing;
using Core.Common;
using Core.Entities.Manufacturing;
using Core.Interfaces.Inventory;
using Core.Interfaces.Manufacturing;

namespace Application.Services.Manufacturing;

public interface IProductionOrderService
{
    Task<Result<ProductionOrder>> GetByIdAsync(Guid id);
    Task<Result<ProductionOrder>> GetByIdWithItemsAsync(Guid id);
    Task<Result<IEnumerable<ProductionOrder>>> GetAllAsync(Guid companyId);
    Task<Result<IEnumerable<ProductionOrder>>> GetByStatusAsync(Guid companyId, string status);
    Task<Result<(IEnumerable<ProductionOrder> Items, int TotalCount)>> GetPagedAsync(
        int pageNumber, int pageSize, Guid? companyId = null, string? searchTerm = null,
        string? status = null, Guid? bomId = null, Guid? finishedGoodId = null,
        Guid? warehouseId = null, DateOnly? fromDate = null, DateOnly? toDate = null);
    Task<Result<ProductionOrder>> CreateAsync(CreateProductionOrderDto dto);
    Task<Result> UpdateAsync(Guid id, UpdateProductionOrderDto dto);
    Task<Result> DeleteAsync(Guid id);
    Task<Result> ReleaseAsync(Guid id, ReleaseProductionOrderDto dto);
    Task<Result> StartAsync(Guid id, StartProductionOrderDto dto);
    Task<Result> CompleteAsync(Guid id, CompleteProductionOrderDto dto);
    Task<Result> CancelAsync(Guid id, CancelProductionOrderDto dto);
    Task<Result> ConsumeItemAsync(Guid orderId, ConsumeItemDto dto);
}

public class ProductionOrderService : IProductionOrderService
{
    private readonly IProductionOrderRepository _repository;
    private readonly IProductionOrderItemRepository _itemRepository;
    private readonly IBomRepository _bomRepository;
    private readonly IBomItemRepository _bomItemRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly CreateProductionOrderDtoValidator _createValidator;
    private readonly UpdateProductionOrderDtoValidator _updateValidator;
    private readonly CompleteProductionOrderDtoValidator _completeValidator;
    private readonly ConsumeItemDtoValidator _consumeValidator;

    public ProductionOrderService(
        IProductionOrderRepository repository,
        IProductionOrderItemRepository itemRepository,
        IBomRepository bomRepository,
        IBomItemRepository bomItemRepository,
        IStockItemRepository stockItemRepository,
        IStockMovementRepository movementRepository,
        IWarehouseRepository warehouseRepository)
    {
        _repository = repository;
        _itemRepository = itemRepository;
        _bomRepository = bomRepository;
        _bomItemRepository = bomItemRepository;
        _stockItemRepository = stockItemRepository;
        _movementRepository = movementRepository;
        _warehouseRepository = warehouseRepository;
        _createValidator = new CreateProductionOrderDtoValidator();
        _updateValidator = new UpdateProductionOrderDtoValidator();
        _completeValidator = new CompleteProductionOrderDtoValidator();
        _consumeValidator = new ConsumeItemDtoValidator();
    }

    public async Task<Result<ProductionOrder>> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"Production order with ID {id} not found");
        return Result<ProductionOrder>.Success(entity);
    }

    public async Task<Result<ProductionOrder>> GetByIdWithItemsAsync(Guid id)
    {
        var entity = await _repository.GetByIdWithItemsAsync(id);
        if (entity == null)
            return Error.NotFound($"Production order with ID {id} not found");
        return Result<ProductionOrder>.Success(entity);
    }

    public async Task<Result<IEnumerable<ProductionOrder>>> GetAllAsync(Guid companyId)
    {
        var entities = await _repository.GetAllAsync(companyId);
        return Result<IEnumerable<ProductionOrder>>.Success(entities);
    }

    public async Task<Result<IEnumerable<ProductionOrder>>> GetByStatusAsync(Guid companyId, string status)
    {
        var entities = await _repository.GetByStatusAsync(companyId, status);
        return Result<IEnumerable<ProductionOrder>>.Success(entities);
    }

    public async Task<Result<(IEnumerable<ProductionOrder> Items, int TotalCount)>> GetPagedAsync(
        int pageNumber, int pageSize, Guid? companyId = null, string? searchTerm = null,
        string? status = null, Guid? bomId = null, Guid? finishedGoodId = null,
        Guid? warehouseId = null, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        var result = await _repository.GetPagedAsync(pageNumber, pageSize, companyId, searchTerm,
            status, bomId, finishedGoodId, warehouseId, fromDate, toDate);
        return Result<(IEnumerable<ProductionOrder>, int)>.Success(result);
    }

    public async Task<Result<ProductionOrder>> CreateAsync(CreateProductionOrderDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Error.Validation(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

        // Validate BOM exists and is active
        var bom = await _bomRepository.GetByIdWithItemsAsync(dto.BomId);
        if (bom == null)
            return Error.NotFound($"BOM with ID {dto.BomId} not found");
        if (!bom.IsActive)
            return Error.Validation("Cannot create production order from inactive BOM");

        // Validate warehouse exists
        var warehouse = await _warehouseRepository.GetByIdAsync(dto.WarehouseId);
        if (warehouse == null)
            return Error.NotFound($"Warehouse with ID {dto.WarehouseId} not found");

        // Generate order number
        var orderNumber = dto.OrderNumber;
        if (string.IsNullOrWhiteSpace(orderNumber))
            orderNumber = await _repository.GenerateOrderNumberAsync(dto.CompanyId!.Value);

        var entity = new ProductionOrder
        {
            Id = Guid.NewGuid(),
            CompanyId = dto.CompanyId!.Value,
            OrderNumber = orderNumber,
            BomId = dto.BomId,
            FinishedGoodId = bom.FinishedGoodId,
            WarehouseId = dto.WarehouseId,
            PlannedQuantity = dto.PlannedQuantity,
            PlannedStartDate = dto.PlannedStartDate,
            PlannedEndDate = dto.PlannedEndDate,
            Status = "draft",
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var createdEntity = await _repository.AddAsync(entity);

        // Create items from BOM (scaled by planned quantity)
        if (dto.Items != null && dto.Items.Any())
        {
            // Use provided items
            var items = dto.Items.Select(itemDto => new ProductionOrderItem
            {
                Id = Guid.NewGuid(),
                ProductionOrderId = createdEntity.Id,
                ComponentId = itemDto.ComponentId,
                PlannedQuantity = itemDto.PlannedQuantity,
                UnitId = itemDto.UnitId,
                BatchId = itemDto.BatchId,
                WarehouseId = itemDto.WarehouseId ?? dto.WarehouseId,
                Notes = itemDto.Notes,
                CreatedAt = DateTime.UtcNow
            });
            await _itemRepository.AddRangeAsync(items);
        }
        else if (bom.Items != null && bom.Items.Any())
        {
            // Copy from BOM and scale by quantity
            var scaleFactor = dto.PlannedQuantity / bom.OutputQuantity;
            var items = bom.Items.Select(bomItem => new ProductionOrderItem
            {
                Id = Guid.NewGuid(),
                ProductionOrderId = createdEntity.Id,
                ComponentId = bomItem.ComponentId,
                PlannedQuantity = bomItem.Quantity * scaleFactor * (1 + bomItem.ScrapPercentage / 100),
                UnitId = bomItem.UnitId,
                WarehouseId = dto.WarehouseId,
                CreatedAt = DateTime.UtcNow
            });
            await _itemRepository.AddRangeAsync(items);
        }

        return Result<ProductionOrder>.Success(createdEntity);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateProductionOrderDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Error.Validation(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            return Error.NotFound($"Production order with ID {id} not found");

        if (existingEntity.Status != "draft")
            return Error.Validation("Only draft production orders can be updated");

        // Validate BOM exists
        var bom = await _bomRepository.GetByIdAsync(dto.BomId);
        if (bom == null)
            return Error.NotFound($"BOM with ID {dto.BomId} not found");

        // Validate warehouse exists
        var warehouse = await _warehouseRepository.GetByIdAsync(dto.WarehouseId);
        if (warehouse == null)
            return Error.NotFound($"Warehouse with ID {dto.WarehouseId} not found");

        existingEntity.BomId = dto.BomId;
        existingEntity.FinishedGoodId = bom.FinishedGoodId;
        existingEntity.WarehouseId = dto.WarehouseId;
        existingEntity.PlannedQuantity = dto.PlannedQuantity;
        existingEntity.PlannedStartDate = dto.PlannedStartDate;
        existingEntity.PlannedEndDate = dto.PlannedEndDate;
        existingEntity.Notes = dto.Notes;
        existingEntity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingEntity);

        // Replace items if provided
        if (dto.Items != null)
        {
            var items = dto.Items.Select(itemDto => new ProductionOrderItem
            {
                Id = itemDto.Id ?? Guid.NewGuid(),
                ProductionOrderId = id,
                ComponentId = itemDto.ComponentId,
                PlannedQuantity = itemDto.PlannedQuantity,
                ConsumedQuantity = itemDto.ConsumedQuantity,
                UnitId = itemDto.UnitId,
                BatchId = itemDto.BatchId,
                WarehouseId = itemDto.WarehouseId ?? dto.WarehouseId,
                Notes = itemDto.Notes,
                CreatedAt = DateTime.UtcNow
            });
            await _itemRepository.ReplaceItemsAsync(id, items);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            return Error.NotFound($"Production order with ID {id} not found");

        if (existingEntity.Status != "draft")
            return Error.Validation("Only draft production orders can be deleted");

        await _repository.DeleteAsync(id);
        return Result.Success();
    }

    public async Task<Result> ReleaseAsync(Guid id, ReleaseProductionOrderDto dto)
    {
        var order = await _repository.GetByIdWithItemsAsync(id);
        if (order == null)
            return Error.NotFound($"Production order with ID {id} not found");

        if (order.Status != "draft")
            return Error.Validation("Only draft orders can be released");

        if (order.Items == null || !order.Items.Any())
            return Error.Validation("Production order must have at least one item");

        // Validate stock availability for all components
        foreach (var item in order.Items)
        {
            var stockItem = await _stockItemRepository.GetByIdAsync(item.ComponentId);
            if (stockItem == null)
                return Error.NotFound($"Component {item.ComponentId} not found");

            var position = await _movementRepository.GetStockPositionAsync(item.ComponentId, item.WarehouseId ?? order.WarehouseId);
            var available = stockItem.OpeningQuantity + position.Quantity;
            if (available < item.PlannedQuantity)
                return Error.Validation($"Insufficient stock for {stockItem.Name}. Available: {available}, Required: {item.PlannedQuantity}");
        }

        order.Status = "released";
        order.ReleasedBy = dto.UserId;
        order.ReleasedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(order);
        return Result.Success();
    }

    public async Task<Result> StartAsync(Guid id, StartProductionOrderDto dto)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null)
            return Error.NotFound($"Production order with ID {id} not found");

        if (order.Status != "released")
            return Error.Validation("Only released orders can be started");

        order.Status = "in_progress";
        order.StartedBy = dto.UserId;
        order.StartedAt = DateTime.UtcNow;
        order.ActualStartDate = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(order);
        return Result.Success();
    }

    public async Task<Result> CompleteAsync(Guid id, CompleteProductionOrderDto dto)
    {
        var validation = await _completeValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Error.Validation(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

        var order = await _repository.GetByIdWithItemsAsync(id);
        if (order == null)
            return Error.NotFound($"Production order with ID {id} not found");

        if (order.Status != "in_progress")
            return Error.Validation("Only in-progress orders can be completed");

        // Calculate production cost from consumed materials
        decimal totalCost = 0;
        if (order.Items != null)
        {
            foreach (var item in order.Items)
            {
                var stockItem = await _stockItemRepository.GetByIdAsync(item.ComponentId);
                if (stockItem != null && stockItem.CostPrice.HasValue)
                    totalCost += item.ConsumedQuantity * stockItem.CostPrice.Value;
            }
        }
        var unitCost = dto.ActualQuantity > 0 ? totalCost / dto.ActualQuantity : 0;

        // Create finished goods receipt movement
        await _movementRepository.AddAsync(new Core.Entities.Inventory.StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = order.CompanyId,
            StockItemId = order.FinishedGoodId,
            WarehouseId = order.WarehouseId,
            MovementType = "production_receipt",
            Quantity = dto.ActualQuantity,
            Rate = unitCost,
            Value = totalCost,
            MovementDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SourceType = "production_order",
            SourceId = order.Id,
            SourceNumber = order.OrderNumber,
            CreatedAt = DateTime.UtcNow
        });

        // Update stock item current quantity
        var finishedGood = await _stockItemRepository.GetByIdAsync(order.FinishedGoodId);
        if (finishedGood != null)
        {
            finishedGood.CurrentQuantity += dto.ActualQuantity;
            finishedGood.CurrentValue += totalCost;
            await _stockItemRepository.UpdateAsync(finishedGood);
        }

        order.Status = "completed";
        order.ActualQuantity = dto.ActualQuantity;
        order.ActualEndDate = DateTime.UtcNow;
        order.CompletedBy = dto.UserId;
        order.CompletedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(order);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(Guid id, CancelProductionOrderDto dto)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order == null)
            return Error.NotFound($"Production order with ID {id} not found");

        if (order.Status == "completed" || order.Status == "cancelled")
            return Error.Validation("Completed or cancelled orders cannot be cancelled");

        // If in progress, we may need to reverse consumption movements
        // For now, we'll just change status - reversals can be done manually

        order.Status = "cancelled";
        order.CancelledBy = dto.UserId;
        order.CancelledAt = DateTime.UtcNow;
        order.Notes = string.IsNullOrEmpty(order.Notes)
            ? $"Cancelled: {dto.Reason}"
            : $"{order.Notes}\nCancelled: {dto.Reason}";
        order.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(order);
        return Result.Success();
    }

    public async Task<Result> ConsumeItemAsync(Guid orderId, ConsumeItemDto dto)
    {
        var validation = await _consumeValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Error.Validation(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

        var order = await _repository.GetByIdAsync(orderId);
        if (order == null)
            return Error.NotFound($"Production order with ID {orderId} not found");

        if (order.Status != "in_progress")
            return Error.Validation("Materials can only be consumed for in-progress orders");

        var item = await _itemRepository.GetByIdAsync(dto.ItemId);
        if (item == null || item.ProductionOrderId != orderId)
            return Error.NotFound($"Production order item with ID {dto.ItemId} not found");

        // Validate stock availability
        var stockItem = await _stockItemRepository.GetByIdAsync(item.ComponentId);
        if (stockItem == null)
            return Error.NotFound($"Stock item {item.ComponentId} not found");

        var warehouseId = item.WarehouseId ?? order.WarehouseId;
        var position = await _movementRepository.GetStockPositionAsync(item.ComponentId, warehouseId);
        var available = stockItem.OpeningQuantity + position.Quantity;
        if (available < dto.Quantity)
            return Error.Validation($"Insufficient stock. Available: {available}, Requested: {dto.Quantity}");

        // Create consumption movement
        await _movementRepository.AddAsync(new Core.Entities.Inventory.StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = order.CompanyId,
            StockItemId = item.ComponentId,
            WarehouseId = warehouseId,
            BatchId = dto.BatchId ?? item.BatchId,
            MovementType = "production_consumption",
            Quantity = -dto.Quantity,
            Rate = stockItem.CostPrice ?? 0,
            Value = -(dto.Quantity * (stockItem.CostPrice ?? 0)),
            MovementDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SourceType = "production_order",
            SourceId = order.Id,
            SourceNumber = order.OrderNumber,
            CreatedAt = DateTime.UtcNow
        });

        // Update consumed quantity on item
        item.ConsumedQuantity += dto.Quantity;
        await _itemRepository.UpdateConsumedQuantityAsync(item.Id, item.ConsumedQuantity);

        // Update stock item current quantity
        stockItem.CurrentQuantity -= dto.Quantity;
        stockItem.CurrentValue -= dto.Quantity * (stockItem.CostPrice ?? 0);
        await _stockItemRepository.UpdateAsync(stockItem);

        return Result.Success();
    }
}

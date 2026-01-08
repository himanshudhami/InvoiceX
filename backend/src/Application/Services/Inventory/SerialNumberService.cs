using Application.DTOs.Inventory;
using Core.Common;
using Core.Entities.Inventory;
using Core.Interfaces.Inventory;

namespace Application.Services.Inventory;

public interface ISerialNumberService
{
    Task<Result<SerialNumber>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<SerialNumber>>> GetAllAsync(Guid companyId);
    Task<Result<IEnumerable<SerialNumber>>> GetByStockItemAsync(Guid stockItemId);
    Task<Result<IEnumerable<SerialNumber>>> GetAvailableAsync(Guid stockItemId, Guid? warehouseId = null);
    Task<Result<(IEnumerable<SerialNumber> Items, int TotalCount)>> GetPagedAsync(SerialNumberFilterParams filter);
    Task<Result<SerialNumber>> CreateAsync(CreateSerialNumberDto dto);
    Task<Result<IEnumerable<SerialNumber>>> BulkCreateAsync(BulkCreateSerialNumberDto dto);
    Task<Result> UpdateAsync(Guid id, UpdateSerialNumberDto dto);
    Task<Result> DeleteAsync(Guid id);
    Task<Result> MarkAsSoldAsync(Guid id, MarkSerialAsSoldDto dto);
    Task<Result> UpdateStatusAsync(Guid id, string status);
}

public class SerialNumberService : ISerialNumberService
{
    private readonly ISerialNumberRepository _repository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public SerialNumberService(
        ISerialNumberRepository repository,
        IStockItemRepository stockItemRepository,
        IWarehouseRepository warehouseRepository)
    {
        _repository = repository;
        _stockItemRepository = stockItemRepository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<Result<SerialNumber>> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"Serial number with ID {id} not found");
        return Result<SerialNumber>.Success(entity);
    }

    public async Task<Result<IEnumerable<SerialNumber>>> GetAllAsync(Guid companyId)
    {
        var entities = await _repository.GetAllAsync(companyId);
        return Result<IEnumerable<SerialNumber>>.Success(entities);
    }

    public async Task<Result<IEnumerable<SerialNumber>>> GetByStockItemAsync(Guid stockItemId)
    {
        var entities = await _repository.GetByStockItemAsync(stockItemId);
        return Result<IEnumerable<SerialNumber>>.Success(entities);
    }

    public async Task<Result<IEnumerable<SerialNumber>>> GetAvailableAsync(Guid stockItemId, Guid? warehouseId = null)
    {
        var entities = await _repository.GetAvailableAsync(stockItemId, warehouseId);
        return Result<IEnumerable<SerialNumber>>.Success(entities);
    }

    public async Task<Result<(IEnumerable<SerialNumber> Items, int TotalCount)>> GetPagedAsync(SerialNumberFilterParams filter)
    {
        var result = await _repository.GetPagedAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.CompanyId,
            filter.StockItemId,
            filter.WarehouseId,
            filter.Status,
            filter.SearchTerm);
        return Result<(IEnumerable<SerialNumber>, int)>.Success(result);
    }

    public async Task<Result<SerialNumber>> CreateAsync(CreateSerialNumberDto dto)
    {
        // Validate stock item exists and has serial tracking enabled
        var stockItem = await _stockItemRepository.GetByIdAsync(dto.StockItemId);
        if (stockItem == null)
            return Error.NotFound($"Stock item with ID {dto.StockItemId} not found");
        if (!stockItem.IsSerialEnabled)
            return Error.Validation($"Stock item '{stockItem.Name}' does not have serial number tracking enabled");

        // Validate warehouse if provided
        if (dto.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(dto.WarehouseId.Value);
            if (warehouse == null)
                return Error.NotFound($"Warehouse with ID {dto.WarehouseId} not found");
        }

        // Check serial number uniqueness
        var exists = await _repository.SerialNoExistsAsync(dto.CompanyId!.Value, dto.StockItemId, dto.SerialNo);
        if (exists)
            return Error.Conflict($"Serial number '{dto.SerialNo}' already exists for this item");

        var entity = new SerialNumber
        {
            Id = Guid.NewGuid(),
            CompanyId = dto.CompanyId!.Value,
            StockItemId = dto.StockItemId,
            SerialNo = dto.SerialNo,
            WarehouseId = dto.WarehouseId,
            BatchId = dto.BatchId,
            Status = dto.Status,
            ManufacturingDate = dto.ManufacturingDate,
            WarrantyExpiry = dto.WarrantyExpiry,
            ProductionOrderId = dto.ProductionOrderId,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var createdEntity = await _repository.AddAsync(entity);
        return Result<SerialNumber>.Success(createdEntity);
    }

    public async Task<Result<IEnumerable<SerialNumber>>> BulkCreateAsync(BulkCreateSerialNumberDto dto)
    {
        // Validate stock item exists and has serial tracking enabled
        var stockItem = await _stockItemRepository.GetByIdAsync(dto.StockItemId);
        if (stockItem == null)
            return Error.NotFound($"Stock item with ID {dto.StockItemId} not found");
        if (!stockItem.IsSerialEnabled)
            return Error.Validation($"Stock item '{stockItem.Name}' does not have serial number tracking enabled");

        // Validate warehouse if provided
        if (dto.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(dto.WarehouseId.Value);
            if (warehouse == null)
                return Error.NotFound($"Warehouse with ID {dto.WarehouseId} not found");
        }

        var entities = new List<SerialNumber>();
        var duplicates = new List<string>();

        for (int i = 0; i < dto.Count; i++)
        {
            var serialNo = $"{dto.Prefix}{(dto.StartNumber + i).ToString().PadLeft(6, '0')}";

            // Check for duplicates
            var exists = await _repository.SerialNoExistsAsync(dto.CompanyId!.Value, dto.StockItemId, serialNo);
            if (exists)
            {
                duplicates.Add(serialNo);
                continue;
            }

            entities.Add(new SerialNumber
            {
                Id = Guid.NewGuid(),
                CompanyId = dto.CompanyId!.Value,
                StockItemId = dto.StockItemId,
                SerialNo = serialNo,
                WarehouseId = dto.WarehouseId,
                BatchId = dto.BatchId,
                Status = "available",
                ManufacturingDate = dto.ManufacturingDate,
                WarrantyExpiry = dto.WarrantyExpiry,
                ProductionOrderId = dto.ProductionOrderId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (duplicates.Any())
        {
            // Could return warning but continue, or fail entirely
            // For now, we'll skip duplicates and create the rest
        }

        if (!entities.Any())
            return Error.Validation("No serial numbers could be created (all duplicates)");

        var createdEntities = await _repository.AddRangeAsync(entities);
        return Result<IEnumerable<SerialNumber>>.Success(createdEntities);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateSerialNumberDto dto)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            return Error.NotFound($"Serial number with ID {id} not found");

        if (existingEntity.Status == "sold")
            return Error.Validation("Cannot update a sold serial number");

        // Validate warehouse if provided
        if (dto.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(dto.WarehouseId.Value);
            if (warehouse == null)
                return Error.NotFound($"Warehouse with ID {dto.WarehouseId} not found");
        }

        existingEntity.WarehouseId = dto.WarehouseId;
        existingEntity.BatchId = dto.BatchId;
        existingEntity.Status = dto.Status;
        existingEntity.ManufacturingDate = dto.ManufacturingDate;
        existingEntity.WarrantyExpiry = dto.WarrantyExpiry;
        existingEntity.Notes = dto.Notes;
        existingEntity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingEntity);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            return Error.NotFound($"Serial number with ID {id} not found");

        if (existingEntity.Status == "sold")
            return Error.Validation("Cannot delete a sold serial number");

        await _repository.DeleteAsync(id);
        return Result.Success();
    }

    public async Task<Result> MarkAsSoldAsync(Guid id, MarkSerialAsSoldDto dto)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            return Error.NotFound($"Serial number with ID {id} not found");

        if (existingEntity.Status != "available")
            return Error.Validation($"Serial number is not available (current status: {existingEntity.Status})");

        await _repository.MarkAsSoldAsync(id, dto.InvoiceId);
        return Result.Success();
    }

    public async Task<Result> UpdateStatusAsync(Guid id, string status)
    {
        var validStatuses = new[] { "available", "sold", "reserved", "damaged" };
        if (!validStatuses.Contains(status))
            return Error.Validation($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            return Error.NotFound($"Serial number with ID {id} not found");

        await _repository.UpdateStatusAsync(id, status);
        return Result.Success();
    }
}

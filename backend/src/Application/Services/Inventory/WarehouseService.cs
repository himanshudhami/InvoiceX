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
    /// Service implementation for Warehouse operations
    /// </summary>
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateWarehouseDto> _createValidator;
        private readonly IValidator<UpdateWarehouseDto> _updateValidator;

        public WarehouseService(
            IWarehouseRepository repository,
            IMapper mapper,
            IValidator<CreateWarehouseDto> createValidator,
            IValidator<UpdateWarehouseDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        public async Task<Result<Warehouse>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Warehouse with ID {id} not found");

            return Result<Warehouse>.Success(entity);
        }

        public async Task<Result<IEnumerable<Warehouse>>> GetByCompanyIdAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetByCompanyIdAsync(companyId);
            return Result<IEnumerable<Warehouse>>.Success(entities);
        }

        public async Task<Result<(IEnumerable<Warehouse> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<Warehouse> Items, int TotalCount)>.Success(result);
        }

        public async Task<Result<Warehouse?>> GetDefaultAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetDefaultAsync(companyId);
            return Result<Warehouse?>.Success(entity);
        }

        public async Task<Result<IEnumerable<Warehouse>>> GetActiveAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetActiveAsync(companyId);
            return Result<IEnumerable<Warehouse>>.Success(entities);
        }

        public async Task<Result<Warehouse>> CreateAsync(CreateWarehouseDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Check for duplicate name
            if (dto.CompanyId.HasValue)
            {
                var exists = await _repository.ExistsAsync(dto.CompanyId.Value, dto.Name);
                if (exists)
                    return Error.Conflict($"Warehouse with name '{dto.Name}' already exists");
            }

            var entity = _mapper.Map<Warehouse>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // If this is set as default, unset any existing default
            if (dto.IsDefault && dto.CompanyId.HasValue)
            {
                var currentDefault = await _repository.GetDefaultAsync(dto.CompanyId.Value);
                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                    await _repository.UpdateAsync(currentDefault);
                }
            }

            var createdEntity = await _repository.AddAsync(entity);
            return Result<Warehouse>.Success(createdEntity);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdateWarehouseDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Warehouse with ID {id} not found");

            // Check for duplicate name if changed
            if (dto.Name != existingEntity.Name)
            {
                var exists = await _repository.ExistsAsync(existingEntity.CompanyId, dto.Name, id);
                if (exists)
                    return Error.Conflict($"Warehouse with name '{dto.Name}' already exists");
            }

            // Handle default warehouse change
            if (dto.IsDefault && !existingEntity.IsDefault)
            {
                await _repository.SetDefaultAsync(existingEntity.CompanyId, id);
            }

            _mapper.Map(dto, existingEntity);
            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);
            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Warehouse with ID {id} not found");

            // Check if warehouse has any movements
            var hasMovements = await _repository.HasMovementsAsync(id);
            if (hasMovements)
                return Error.Conflict("Cannot delete warehouse with stock movements. Deactivate it instead.");

            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        public async Task<Result> SetDefaultAsync(Guid companyId, Guid warehouseId)
        {
            var companyValidation = ServiceExtensions.ValidateGuid(companyId);
            if (companyValidation.IsFailure)
                return companyValidation.Error!;

            var warehouseValidation = ServiceExtensions.ValidateGuid(warehouseId);
            if (warehouseValidation.IsFailure)
                return warehouseValidation.Error!;

            var warehouse = await _repository.GetByIdAsync(warehouseId);
            if (warehouse == null)
                return Error.NotFound($"Warehouse with ID {warehouseId} not found");

            await _repository.SetDefaultAsync(companyId, warehouseId);
            return Result.Success();
        }
    }
}

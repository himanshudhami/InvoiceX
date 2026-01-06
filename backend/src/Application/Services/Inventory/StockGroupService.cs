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
    /// Service implementation for Stock Group operations
    /// </summary>
    public class StockGroupService : IStockGroupService
    {
        private readonly IStockGroupRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateStockGroupDto> _createValidator;
        private readonly IValidator<UpdateStockGroupDto> _updateValidator;

        public StockGroupService(
            IStockGroupRepository repository,
            IMapper mapper,
            IValidator<CreateStockGroupDto> createValidator,
            IValidator<UpdateStockGroupDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        public async Task<Result<StockGroup>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Stock Group with ID {id} not found");

            return Result<StockGroup>.Success(entity);
        }

        public async Task<Result<IEnumerable<StockGroup>>> GetByCompanyIdAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetByCompanyIdAsync(companyId);
            return Result<IEnumerable<StockGroup>>.Success(entities);
        }

        public async Task<Result<(IEnumerable<StockGroup> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<StockGroup> Items, int TotalCount)>.Success(result);
        }

        public async Task<Result<IEnumerable<StockGroup>>> GetHierarchyAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetHierarchyAsync(companyId);
            return Result<IEnumerable<StockGroup>>.Success(entities);
        }

        public async Task<Result<IEnumerable<StockGroup>>> GetActiveAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetActiveAsync(companyId);
            return Result<IEnumerable<StockGroup>>.Success(entities);
        }

        public async Task<Result<string>> GetFullPathAsync(Guid stockGroupId)
        {
            var validation = ServiceExtensions.ValidateGuid(stockGroupId);
            if (validation.IsFailure)
                return validation.Error!;

            var path = await _repository.GetFullPathAsync(stockGroupId);
            return Result<string>.Success(path);
        }

        public async Task<Result<StockGroup>> CreateAsync(CreateStockGroupDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Check for duplicate name at same level
            if (dto.CompanyId.HasValue)
            {
                var exists = await _repository.ExistsAsync(dto.CompanyId.Value, dto.Name, dto.ParentStockGroupId);
                if (exists)
                    return Error.Conflict($"Stock Group with name '{dto.Name}' already exists at this level");
            }

            // Validate parent exists if specified
            if (dto.ParentStockGroupId.HasValue)
            {
                var parent = await _repository.GetByIdAsync(dto.ParentStockGroupId.Value);
                if (parent == null)
                    return Error.NotFound($"Parent Stock Group with ID {dto.ParentStockGroupId} not found");
            }

            var entity = _mapper.Map<StockGroup>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);
            return Result<StockGroup>.Success(createdEntity);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdateStockGroupDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Stock Group with ID {id} not found");

            // Check for duplicate name at same level if changed
            if (dto.Name != existingEntity.Name)
            {
                var exists = await _repository.ExistsAsync(
                    existingEntity.CompanyId, dto.Name, dto.ParentStockGroupId, id);
                if (exists)
                    return Error.Conflict($"Stock Group with name '{dto.Name}' already exists at this level");
            }

            // Prevent circular reference
            if (dto.ParentStockGroupId == id)
                return Error.Validation("A stock group cannot be its own parent");

            // Validate parent exists if specified
            if (dto.ParentStockGroupId.HasValue)
            {
                var parent = await _repository.GetByIdAsync(dto.ParentStockGroupId.Value);
                if (parent == null)
                    return Error.NotFound($"Parent Stock Group with ID {dto.ParentStockGroupId} not found");
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
                return Error.NotFound($"Stock Group with ID {id} not found");

            // Check if group has children
            var hasChildren = await _repository.HasChildrenAsync(id);
            if (hasChildren)
                return Error.Conflict("Cannot delete stock group with child groups. Delete or move children first.");

            // Check if group has items
            var hasItems = await _repository.HasItemsAsync(id);
            if (hasItems)
                return Error.Conflict("Cannot delete stock group with stock items. Delete or reassign items first.");

            await _repository.DeleteAsync(id);
            return Result.Success();
        }
    }
}

using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Audit;
using Application.DTOs.Products;
using Application.Validators.Products;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for Products operations
    /// </summary>
    public class ProductsService : IProductsService
    {
        private readonly IProductsRepository _repository;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateProductsDto> _createValidator;
        private readonly IValidator<UpdateProductsDto> _updateValidator;

        public ProductsService(
            IProductsRepository repository,
            IAuditService auditService,
            IMapper mapper,
            IValidator<CreateProductsDto> createValidator,
            IValidator<UpdateProductsDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        /// <inheritdoc />
        public async Task<Result<Products>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Products with ID {id} not found");

            return Result<Products>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Products>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<Products>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<Products> Items, int TotalCount)>> GetPagedAsync(
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
                pageNumber, 
                pageSize, 
                searchTerm, 
                sortBy, 
                sortDescending, 
                filters);
                
            return Result<(IEnumerable<Products> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<Products>> CreateAsync(CreateProductsDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Map DTO to entity
            var entity = _mapper.Map<Products>(dto);
            
            // Set timestamps
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);

            // Audit trail
            if (createdEntity.CompanyId.HasValue)
            {
                await _auditService.AuditCreateAsync(createdEntity, createdEntity.Id, createdEntity.CompanyId.Value, createdEntity.Name);
            }

            return Result<Products>.Success(createdEntity);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateProductsDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Check if entity exists
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Products with ID {id} not found");

            // Capture state before update for audit trail
            var oldEntity = _mapper.Map<Products>(existingEntity);

            // Map DTO to existing entity
            _mapper.Map(dto, existingEntity);

            // Set updated timestamp
            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);

            // Audit trail
            if (existingEntity.CompanyId.HasValue)
            {
                await _auditService.AuditUpdateAsync(oldEntity, existingEntity, existingEntity.Id, existingEntity.CompanyId.Value, existingEntity.Name);
            }

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Products with ID {id} not found");

            // Audit trail before delete
            if (existingEntity.CompanyId.HasValue)
            {
                await _auditService.AuditDeleteAsync(existingEntity, existingEntity.Id, existingEntity.CompanyId.Value, existingEntity.Name);
            }

            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<bool>> ExistsAsync(Guid id)
        {
            if (id == default(Guid))
                return Result<bool>.Success(false);

            var entity = await _repository.GetByIdAsync(id);
            return Result<bool>.Success(entity != null);
        }
    }
}

using Application.Common;
using Application.Interfaces;
using Application.DTOs.InvoiceItems;
using Application.Validators.InvoiceItems;
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
    /// Service implementation for InvoiceItems operations
    /// </summary>
    public class InvoiceItemsService : IInvoiceItemsService
    {
        private readonly IInvoiceItemsRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateInvoiceItemsDto> _createValidator;
        private readonly IValidator<UpdateInvoiceItemsDto> _updateValidator;

        public InvoiceItemsService(
            IInvoiceItemsRepository repository, 
            IMapper mapper,
            IValidator<CreateInvoiceItemsDto> createValidator,
            IValidator<UpdateInvoiceItemsDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        /// <inheritdoc />
        public async Task<Result<InvoiceItems>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"InvoiceItems with ID {id} not found");

            return Result<InvoiceItems>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<InvoiceItems>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<InvoiceItems>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<InvoiceItems> Items, int TotalCount)>> GetPagedAsync(
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
                
            return Result<(IEnumerable<InvoiceItems> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<InvoiceItems>> CreateAsync(CreateInvoiceItemsDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Map DTO to entity
            var entity = _mapper.Map<InvoiceItems>(dto);
            
            // Set timestamps
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);
            return Result<InvoiceItems>.Success(createdEntity);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateInvoiceItemsDto dto)
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
                return Error.NotFound($"InvoiceItems with ID {id} not found");

            // Map DTO to existing entity
            _mapper.Map(dto, existingEntity);
            
            // Set updated timestamp
            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);
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
                return Error.NotFound($"InvoiceItems with ID {id} not found");

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

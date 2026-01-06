using Application.Common;
using Application.Interfaces;
using Application.DTOs.Vendors;
using Application.Validators.Vendors;
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
    /// Service implementation for Vendors operations
    /// </summary>
    public class VendorsService : IVendorsService
    {
        private readonly IVendorsRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateVendorsDto> _createValidator;
        private readonly IValidator<UpdateVendorsDto> _updateValidator;

        public VendorsService(
            IVendorsRepository repository,
            IMapper mapper,
            IValidator<CreateVendorsDto> createValidator,
            IValidator<UpdateVendorsDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        /// <inheritdoc />
        public async Task<Result<Vendors>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Vendor with ID {id} not found");

            return Result<Vendors>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Vendors>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<Vendors>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<Vendors> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<Vendors> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<Vendors>> GetByGstinAsync(Guid companyId, string gstin)
        {
            var companyValidation = ServiceExtensions.ValidateGuid(companyId);
            if (companyValidation.IsFailure)
                return companyValidation.Error!;

            if (string.IsNullOrWhiteSpace(gstin))
                return Error.Validation("GSTIN is required");

            var entity = await _repository.GetByGstinAsync(companyId, gstin);
            if (entity == null)
                return Error.NotFound($"Vendor with GSTIN {gstin} not found");

            return Result<Vendors>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<Vendors>> GetByPanAsync(Guid companyId, string panNumber)
        {
            var companyValidation = ServiceExtensions.ValidateGuid(companyId);
            if (companyValidation.IsFailure)
                return companyValidation.Error!;

            if (string.IsNullOrWhiteSpace(panNumber))
                return Error.Validation("PAN number is required");

            var entity = await _repository.GetByPanAsync(companyId, panNumber);
            if (entity == null)
                return Error.NotFound($"Vendor with PAN {panNumber} not found");

            return Result<Vendors>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Vendors>>> GetMsmeVendorsAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetMsmeVendorsAsync(companyId);
            return Result<IEnumerable<Vendors>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Vendors>>> GetTdsApplicableVendorsAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetTdsApplicableVendorsAsync(companyId);
            return Result<IEnumerable<Vendors>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<decimal>> GetOutstandingBalanceAsync(Guid vendorId)
        {
            var validation = ServiceExtensions.ValidateGuid(vendorId);
            if (validation.IsFailure)
                return validation.Error!;

            var balance = await _repository.GetOutstandingBalanceAsync(vendorId);
            return Result<decimal>.Success(balance);
        }

        /// <inheritdoc />
        public async Task<Result<Vendors>> CreateAsync(CreateVendorsDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Check for duplicate GSTIN if provided
            if (!string.IsNullOrWhiteSpace(dto.Gstin) && dto.CompanyId.HasValue)
            {
                var existingGstin = await _repository.GetByGstinAsync(dto.CompanyId.Value, dto.Gstin);
                if (existingGstin != null)
                    return Error.Conflict($"Vendor with GSTIN {dto.Gstin} already exists");
            }

            // Check for duplicate PAN if provided
            if (!string.IsNullOrWhiteSpace(dto.PanNumber) && dto.CompanyId.HasValue)
            {
                var existingPan = await _repository.GetByPanAsync(dto.CompanyId.Value, dto.PanNumber);
                if (existingPan != null)
                    return Error.Conflict($"Vendor with PAN {dto.PanNumber} already exists");
            }

            // Map DTO to entity
            var entity = _mapper.Map<Vendors>(dto);

            // Set timestamps
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);
            return Result<Vendors>.Success(createdEntity);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateVendorsDto dto)
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
                return Error.NotFound($"Vendor with ID {id} not found");

            // Check for duplicate GSTIN if provided and changed
            if (!string.IsNullOrWhiteSpace(dto.Gstin) &&
                dto.Gstin != existingEntity.Gstin &&
                existingEntity.CompanyId.HasValue)
            {
                var existingGstin = await _repository.GetByGstinAsync(existingEntity.CompanyId.Value, dto.Gstin);
                if (existingGstin != null && existingGstin.Id != id)
                    return Error.Conflict($"Vendor with GSTIN {dto.Gstin} already exists");
            }

            // Check for duplicate PAN if provided and changed
            if (!string.IsNullOrWhiteSpace(dto.PanNumber) &&
                dto.PanNumber != existingEntity.PanNumber &&
                existingEntity.CompanyId.HasValue)
            {
                var existingPan = await _repository.GetByPanAsync(existingEntity.CompanyId.Value, dto.PanNumber);
                if (existingPan != null && existingPan.Id != id)
                    return Error.Conflict($"Vendor with PAN {dto.PanNumber} already exists");
            }

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
                return Error.NotFound($"Vendor with ID {id} not found");

            // Check for outstanding balance before deletion
            var balance = await _repository.GetOutstandingBalanceAsync(id);
            if (balance > 0)
                return Error.Conflict($"Cannot delete vendor with outstanding balance of {balance:C}");

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

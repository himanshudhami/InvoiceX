using Application.Common;
using Application.Interfaces;
using Application.DTOs.VendorInvoices;
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
    /// Service implementation for Vendor Invoices operations
    /// </summary>
    public class VendorInvoicesService : IVendorInvoicesService
    {
        private readonly IVendorInvoicesRepository _repository;
        private readonly IVendorsRepository _vendorsRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateVendorInvoiceDto> _createValidator;
        private readonly IValidator<UpdateVendorInvoiceDto> _updateValidator;

        public VendorInvoicesService(
            IVendorInvoicesRepository repository,
            IVendorsRepository vendorsRepository,
            IMapper mapper,
            IValidator<CreateVendorInvoiceDto> createValidator,
            IValidator<UpdateVendorInvoiceDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _vendorsRepository = vendorsRepository ?? throw new ArgumentNullException(nameof(vendorsRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        /// <inheritdoc />
        public async Task<Result<VendorInvoice>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Vendor invoice with ID {id} not found");

            return Result<VendorInvoice>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<VendorInvoice>> GetByIdWithItemsAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdWithItemsAsync(id);
            if (entity == null)
                return Error.NotFound($"Vendor invoice with ID {id} not found");

            return Result<VendorInvoice>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorInvoice>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<VendorInvoice>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<VendorInvoice> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<VendorInvoice> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorInvoice>>> GetPendingApprovalAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetPendingApprovalAsync(companyId);
            return Result<IEnumerable<VendorInvoice>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorInvoice>>> GetUnpaidAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetUnpaidAsync(companyId);
            return Result<IEnumerable<VendorInvoice>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorInvoice>>> GetOverdueAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetOverdueAsync(companyId);
            return Result<IEnumerable<VendorInvoice>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorInvoice>>> GetItcEligibleAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetItcEligibleAsync(companyId);
            return Result<IEnumerable<VendorInvoice>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorInvoice>>> GetUnmatchedWithGstr2BAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetUnmatchedWithGstr2BAsync(companyId);
            return Result<IEnumerable<VendorInvoice>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<VendorInvoice>> CreateAsync(CreateVendorInvoiceDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Validate vendor exists
            var vendor = await _vendorsRepository.GetByIdAsync(dto.VendorId);
            if (vendor == null)
                return Error.NotFound($"Vendor with ID {dto.VendorId} not found");

            // Map DTO to entity
            var entity = _mapper.Map<VendorInvoice>(dto);

            // Apply vendor defaults if applicable
            if (vendor.TdsApplicable && !dto.TdsApplicable)
            {
                entity.TdsApplicable = true;
                entity.TdsSection = dto.TdsSection ?? vendor.DefaultTdsSection;
                entity.TdsRate = dto.TdsRate ?? vendor.DefaultTdsRate;
            }

            // Set timestamps
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);

            // Add items if provided
            if (dto.Items != null && dto.Items.Any())
            {
                foreach (var itemDto in dto.Items)
                {
                    var item = _mapper.Map<VendorInvoiceItem>(itemDto);
                    item.VendorInvoiceId = createdEntity.Id;
                    await _repository.AddItemAsync(item);
                }
            }

            return Result<VendorInvoice>.Success(createdEntity);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateVendorInvoiceDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Vendor invoice with ID {id} not found");

            // Cannot update posted invoices
            if (existingEntity.IsPosted)
                return Error.Conflict("Cannot update a posted invoice");

            // Cannot update paid invoices
            if (existingEntity.Status == "paid")
                return Error.Conflict("Cannot update a paid invoice");

            // Map DTO to existing entity
            _mapper.Map(dto, existingEntity);
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
                return Error.NotFound($"Vendor invoice with ID {id} not found");

            // Cannot delete posted invoices
            if (existingEntity.IsPosted)
                return Error.Conflict("Cannot delete a posted invoice");

            // Cannot delete paid invoices
            if (existingEntity.PaidAmount > 0)
                return Error.Conflict("Cannot delete an invoice with payments");

            // Delete items first
            await _repository.DeleteItemsByInvoiceIdAsync(id);
            await _repository.DeleteAsync(id);

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> UpdateStatusAsync(Guid id, string status)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Vendor invoice with ID {id} not found");

            var validStatuses = new[] { "draft", "pending_approval", "approved", "partially_paid", "paid", "cancelled" };
            if (!validStatuses.Contains(status.ToLowerInvariant()))
                return Error.Validation($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

            await _repository.UpdateStatusAsync(id, status);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> ApproveAsync(Guid id, Guid approvedBy, string? notes = null)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Vendor invoice with ID {id} not found");

            if (existingEntity.Status != "pending_approval")
                return Error.Conflict("Invoice is not pending approval");

            existingEntity.Status = "approved";
            existingEntity.ApprovedBy = approvedBy;
            existingEntity.ApprovedAt = DateTime.UtcNow;
            existingEntity.ApprovalNotes = notes;
            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> MarkAsPostedAsync(Guid id, Guid journalId)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Vendor invoice with ID {id} not found");

            if (existingEntity.IsPosted)
                return Error.Conflict("Invoice is already posted");

            await _repository.MarkAsPostedAsync(id, journalId);
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

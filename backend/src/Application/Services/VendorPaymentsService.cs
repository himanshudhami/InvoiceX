using Application.Common;
using Application.Interfaces;
using Application.DTOs.VendorPayments;
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
    /// Service implementation for Vendor Payments operations
    /// </summary>
    public class VendorPaymentsService : IVendorPaymentsService
    {
        private readonly IVendorPaymentsRepository _repository;
        private readonly IVendorsRepository _vendorsRepository;
        private readonly IVendorInvoicesRepository _invoicesRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateVendorPaymentDto> _createValidator;
        private readonly IValidator<UpdateVendorPaymentDto> _updateValidator;

        public VendorPaymentsService(
            IVendorPaymentsRepository repository,
            IVendorsRepository vendorsRepository,
            IVendorInvoicesRepository invoicesRepository,
            IMapper mapper,
            IValidator<CreateVendorPaymentDto> createValidator,
            IValidator<UpdateVendorPaymentDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _vendorsRepository = vendorsRepository ?? throw new ArgumentNullException(nameof(vendorsRepository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        /// <inheritdoc />
        public async Task<Result<VendorPayment>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Vendor payment with ID {id} not found");

            return Result<VendorPayment>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<VendorPayment>> GetByIdWithAllocationsAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdWithAllocationsAsync(id);
            if (entity == null)
                return Error.NotFound($"Vendor payment with ID {id} not found");

            return Result<VendorPayment>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorPayment>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<VendorPayment>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<VendorPayment> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<VendorPayment> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorPayment>>> GetPendingApprovalAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetPendingApprovalAsync(companyId);
            return Result<IEnumerable<VendorPayment>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorPayment>>> GetUnreconciledAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetUnreconciledAsync(companyId);
            return Result<IEnumerable<VendorPayment>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorPayment>>> GetTdsPaymentsAsync(Guid companyId, string financialYear)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            if (string.IsNullOrWhiteSpace(financialYear))
                return Error.Validation("Financial year is required");

            var entities = await _repository.GetTdsPaymentsAsync(companyId, financialYear);
            return Result<IEnumerable<VendorPayment>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<VendorPayment>>> GetPendingTdsDepositAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetPendingTdsDepositAsync(companyId);
            return Result<IEnumerable<VendorPayment>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<decimal>> GetTotalTdsDeductedAsync(Guid companyId, string financialYear)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            if (string.IsNullOrWhiteSpace(financialYear))
                return Error.Validation("Financial year is required");

            var total = await _repository.GetTotalTdsDeductedAsync(companyId, financialYear);
            return Result<decimal>.Success(total);
        }

        /// <inheritdoc />
        public async Task<Result<decimal>> GetTotalPaidToVendorAsync(Guid vendorId, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            var validation = ServiceExtensions.ValidateGuid(vendorId);
            if (validation.IsFailure)
                return validation.Error!;

            var total = await _repository.GetTotalPaidToVendorAsync(vendorId, fromDate, toDate);
            return Result<decimal>.Success(total);
        }

        /// <inheritdoc />
        public async Task<Result<VendorPayment>> CreateAsync(CreateVendorPaymentDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Validate vendor exists
            var vendor = await _vendorsRepository.GetByIdAsync(dto.VendorId);
            if (vendor == null)
                return Error.NotFound($"Vendor with ID {dto.VendorId} not found");

            // Map DTO to entity
            var entity = _mapper.Map<VendorPayment>(dto);

            // Apply vendor TDS defaults if applicable
            if (vendor.TdsApplicable && !dto.TdsApplicable)
            {
                entity.TdsApplicable = true;
                entity.TdsSection = dto.TdsSection ?? vendor.DefaultTdsSection;
                entity.TdsRate = dto.TdsRate ?? vendor.DefaultTdsRate;
            }

            // Calculate TDS amount if rate is provided but amount is not
            if (entity.TdsApplicable && entity.TdsRate.HasValue && !entity.TdsAmount.HasValue && entity.GrossAmount.HasValue)
            {
                entity.TdsAmount = Math.Round(entity.GrossAmount.Value * entity.TdsRate.Value / 100, 2);
            }

            // Set timestamps
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);

            // Add allocations if provided
            if (dto.Allocations != null && dto.Allocations.Any())
            {
                foreach (var allocationDto in dto.Allocations)
                {
                    var allocation = _mapper.Map<VendorPaymentAllocation>(allocationDto);
                    allocation.VendorPaymentId = createdEntity.Id;
                    await _repository.AddAllocationAsync(allocation);
                }
            }

            return Result<VendorPayment>.Success(createdEntity);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateVendorPaymentDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Vendor payment with ID {id} not found");

            // Cannot update posted payments
            if (existingEntity.IsPosted)
                return Error.Conflict("Cannot update a posted payment");

            // Cannot update reconciled payments
            if (existingEntity.IsReconciled)
                return Error.Conflict("Cannot update a reconciled payment");

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
                return Error.NotFound($"Vendor payment with ID {id} not found");

            // Cannot delete posted payments
            if (existingEntity.IsPosted)
                return Error.Conflict("Cannot delete a posted payment");

            // Cannot delete reconciled payments
            if (existingEntity.IsReconciled)
                return Error.Conflict("Cannot delete a reconciled payment");

            // Delete allocations first
            await _repository.DeleteAllocationsByPaymentIdAsync(id);
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
                return Error.NotFound($"Vendor payment with ID {id} not found");

            var validStatuses = new[] { "draft", "pending_approval", "approved", "processed", "cancelled", "failed" };
            if (!validStatuses.Contains(status.ToLowerInvariant()))
                return Error.Validation($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

            await _repository.UpdateStatusAsync(id, status);
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
                return Error.NotFound($"Vendor payment with ID {id} not found");

            if (existingEntity.IsPosted)
                return Error.Conflict("Payment is already posted");

            await _repository.MarkAsPostedAsync(id, journalId);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> MarkAsReconciledAsync(Guid id, Guid bankTransactionId)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Vendor payment with ID {id} not found");

            if (existingEntity.IsReconciled)
                return Error.Conflict("Payment is already reconciled");

            await _repository.MarkAsReconciledAsync(id, bankTransactionId);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> MarkTdsDepositedAsync(Guid id, string challanNumber, DateOnly depositDate)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            if (string.IsNullOrWhiteSpace(challanNumber))
                return Error.Validation("Challan number is required");

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Vendor payment with ID {id} not found");

            if (!existingEntity.TdsApplicable)
                return Error.Conflict("TDS is not applicable for this payment");

            if (existingEntity.TdsDeposited)
                return Error.Conflict("TDS is already marked as deposited");

            await _repository.MarkTdsDepositedAsync(id, challanNumber, depositDate);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<VendorPaymentAllocation>> AddAllocationAsync(Guid paymentId, CreateVendorPaymentAllocationDto dto)
        {
            var validation = ServiceExtensions.ValidateGuid(paymentId);
            if (validation.IsFailure)
                return validation.Error!;

            var payment = await _repository.GetByIdAsync(paymentId);
            if (payment == null)
                return Error.NotFound($"Vendor payment with ID {paymentId} not found");

            // Validate invoice if provided
            if (dto.VendorInvoiceId.HasValue)
            {
                var invoice = await _invoicesRepository.GetByIdAsync(dto.VendorInvoiceId.Value);
                if (invoice == null)
                    return Error.NotFound($"Vendor invoice with ID {dto.VendorInvoiceId} not found");

                // Validate same vendor
                if (invoice.VendorId != payment.VendorId)
                    return Error.Validation("Invoice vendor does not match payment vendor");
            }

            var allocation = _mapper.Map<VendorPaymentAllocation>(dto);
            allocation.VendorPaymentId = paymentId;

            var createdAllocation = await _repository.AddAllocationAsync(allocation);
            return Result<VendorPaymentAllocation>.Success(createdAllocation);
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

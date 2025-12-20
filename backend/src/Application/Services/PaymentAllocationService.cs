using Application.Common;
using Application.Interfaces;
using Application.DTOs.PaymentAllocations;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for payment allocation operations
    /// Enables partial payment tracking and invoice settlement
    /// </summary>
    public class PaymentAllocationService : IPaymentAllocationService
    {
        private readonly IPaymentAllocationRepository _repository;
        private readonly IPaymentsRepository _paymentsRepository;
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly IMapper _mapper;

        public PaymentAllocationService(
            IPaymentAllocationRepository repository,
            IPaymentsRepository paymentsRepository,
            IInvoicesRepository invoicesRepository,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _paymentsRepository = paymentsRepository ?? throw new ArgumentNullException(nameof(paymentsRepository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // ==================== Basic CRUD ====================

        public async Task<Result<PaymentAllocation>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Payment allocation with ID {id} not found");

            return Result<PaymentAllocation>.Success(entity);
        }

        public async Task<Result<IEnumerable<PaymentAllocation>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<PaymentAllocation>>.Success(entities);
        }

        public async Task<Result<(IEnumerable<PaymentAllocation> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<PaymentAllocation> Items, int TotalCount)>.Success(result);
        }

        public async Task<Result<PaymentAllocation>> CreateAsync(CreatePaymentAllocationDto dto)
        {
            // Validate payment exists
            var payment = await _paymentsRepository.GetByIdAsync(dto.PaymentId);
            if (payment == null)
                return Error.NotFound($"Payment with ID {dto.PaymentId} not found");

            // Validate invoice exists if provided
            if (dto.InvoiceId.HasValue)
            {
                var invoice = await _invoicesRepository.GetByIdAsync(dto.InvoiceId.Value);
                if (invoice == null)
                    return Error.NotFound($"Invoice with ID {dto.InvoiceId} not found");
            }

            // Check if allocation amount is valid
            if (dto.AllocatedAmount <= 0)
                return Error.Validation("Allocation amount must be greater than zero");

            // Check if allocation doesn't exceed unallocated amount
            var unallocated = await _repository.GetUnallocatedAmountAsync(dto.PaymentId);
            if (dto.AllocatedAmount > unallocated)
                return Error.Validation($"Allocation amount ({dto.AllocatedAmount}) exceeds unallocated amount ({unallocated})");

            var entity = new PaymentAllocation
            {
                CompanyId = dto.CompanyId,
                PaymentId = dto.PaymentId,
                InvoiceId = dto.InvoiceId,
                AllocatedAmount = dto.AllocatedAmount,
                Currency = dto.Currency,
                AmountInInr = dto.AmountInInr ?? (dto.Currency == "INR" ? dto.AllocatedAmount : null),
                ExchangeRate = dto.ExchangeRate,
                AllocationDate = dto.AllocationDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                AllocationType = dto.AllocationType,
                TdsAllocated = dto.TdsAllocated,
                Notes = dto.Notes,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddAsync(entity);
            return Result<PaymentAllocation>.Success(created);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdatePaymentAllocationDto dto)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Payment allocation with ID {id} not found");

            // Validate invoice if changed
            if (dto.InvoiceId.HasValue && dto.InvoiceId != existing.InvoiceId)
            {
                var invoice = await _invoicesRepository.GetByIdAsync(dto.InvoiceId.Value);
                if (invoice == null)
                    return Error.NotFound($"Invoice with ID {dto.InvoiceId} not found");
            }

            // Check allocation amount validity
            if (dto.AllocatedAmount <= 0)
                return Error.Validation("Allocation amount must be greater than zero");

            // Check if new amount doesn't exceed available (current unallocated + current allocation)
            var unallocated = await _repository.GetUnallocatedAmountAsync(existing.PaymentId);
            var available = unallocated + existing.AllocatedAmount;
            if (dto.AllocatedAmount > available)
                return Error.Validation($"Allocation amount ({dto.AllocatedAmount}) exceeds available amount ({available})");

            existing.InvoiceId = dto.InvoiceId;
            existing.AllocatedAmount = dto.AllocatedAmount;
            existing.Currency = dto.Currency;
            existing.AmountInInr = dto.AmountInInr ?? (dto.Currency == "INR" ? dto.AllocatedAmount : null);
            existing.ExchangeRate = dto.ExchangeRate;
            existing.AllocationDate = dto.AllocationDate ?? existing.AllocationDate;
            existing.AllocationType = dto.AllocationType;
            existing.TdsAllocated = dto.TdsAllocated;
            existing.Notes = dto.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);
            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Payment allocation with ID {id} not found");

            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        // ==================== Query Operations ====================

        public async Task<Result<IEnumerable<PaymentAllocation>>> GetByPaymentIdAsync(Guid paymentId)
        {
            var validation = ServiceExtensions.ValidateGuid(paymentId);
            if (validation.IsFailure)
                return validation.Error!;

            var allocations = await _repository.GetByPaymentIdAsync(paymentId);
            return Result<IEnumerable<PaymentAllocation>>.Success(allocations);
        }

        public async Task<Result<IEnumerable<PaymentAllocation>>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            var validation = ServiceExtensions.ValidateGuid(invoiceId);
            if (validation.IsFailure)
                return validation.Error!;

            var allocations = await _repository.GetByInvoiceIdAsync(invoiceId);
            return Result<IEnumerable<PaymentAllocation>>.Success(allocations);
        }

        public async Task<Result<IEnumerable<PaymentAllocation>>> GetByCompanyIdAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var allocations = await _repository.GetByCompanyIdAsync(companyId);
            return Result<IEnumerable<PaymentAllocation>>.Success(allocations);
        }

        // ==================== Allocation Operations ====================

        public async Task<Result<IEnumerable<PaymentAllocation>>> AllocatePaymentAsync(BulkAllocationDto dto)
        {
            // Validate payment exists
            var payment = await _paymentsRepository.GetByIdAsync(dto.PaymentId);
            if (payment == null)
                return Error.NotFound($"Payment with ID {dto.PaymentId} not found");

            if (dto.Allocations == null || !dto.Allocations.Any())
                return Error.Validation("At least one allocation is required");

            // Calculate total allocation
            var totalAllocation = dto.Allocations.Sum(a => a.Amount);

            // Get unallocated amount
            var unallocated = await _repository.GetUnallocatedAmountAsync(dto.PaymentId);

            if (totalAllocation > unallocated)
                return Error.Validation($"Total allocation ({totalAllocation}) exceeds unallocated amount ({unallocated})");

            // Validate all invoices exist
            foreach (var allocation in dto.Allocations)
            {
                var invoice = await _invoicesRepository.GetByIdAsync(allocation.InvoiceId);
                if (invoice == null)
                    return Error.NotFound($"Invoice with ID {allocation.InvoiceId} not found");

                if (allocation.Amount <= 0)
                    return Error.Validation($"Allocation amount must be greater than zero for invoice {allocation.InvoiceId}");
            }

            // Create allocations
            var entities = dto.Allocations.Select(a => new PaymentAllocation
            {
                CompanyId = dto.CompanyId,
                PaymentId = dto.PaymentId,
                InvoiceId = a.InvoiceId,
                AllocatedAmount = a.Amount,
                Currency = payment.Currency ?? "INR",
                AmountInInr = payment.Currency == "INR" ? a.Amount : null,
                ExchangeRate = 1,
                AllocationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                AllocationType = "invoice_settlement",
                TdsAllocated = a.TdsAmount,
                Notes = a.Notes,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            var created = await _repository.AddBulkAsync(entities);
            return Result<IEnumerable<PaymentAllocation>>.Success(created);
        }

        public async Task<Result<decimal>> GetUnallocatedAmountAsync(Guid paymentId)
        {
            var validation = ServiceExtensions.ValidateGuid(paymentId);
            if (validation.IsFailure)
                return validation.Error!;

            var payment = await _paymentsRepository.GetByIdAsync(paymentId);
            if (payment == null)
                return Error.NotFound($"Payment with ID {paymentId} not found");

            var unallocated = await _repository.GetUnallocatedAmountAsync(paymentId);
            return Result<decimal>.Success(unallocated);
        }

        public async Task<Result<PaymentAllocationSummaryDto>> GetPaymentAllocationSummaryAsync(Guid paymentId)
        {
            var validation = ServiceExtensions.ValidateGuid(paymentId);
            if (validation.IsFailure)
                return validation.Error!;

            var payment = await _paymentsRepository.GetByIdAsync(paymentId);
            if (payment == null)
                return Error.NotFound($"Payment with ID {paymentId} not found");

            var allocations = await _repository.GetByPaymentIdAsync(paymentId);
            var totalAllocated = allocations.Sum(a => a.AllocatedAmount);

            var allocationDetails = new List<AllocationDetailDto>();
            foreach (var allocation in allocations)
            {
                string? invoiceNumber = null;
                if (allocation.InvoiceId.HasValue)
                {
                    var invoice = await _invoicesRepository.GetByIdAsync(allocation.InvoiceId.Value);
                    invoiceNumber = invoice?.InvoiceNumber;
                }

                allocationDetails.Add(new AllocationDetailDto
                {
                    Id = allocation.Id,
                    InvoiceId = allocation.InvoiceId,
                    InvoiceNumber = invoiceNumber,
                    AllocatedAmount = allocation.AllocatedAmount,
                    TdsAllocated = allocation.TdsAllocated,
                    AllocationDate = allocation.AllocationDate,
                    AllocationType = allocation.AllocationType,
                    Notes = allocation.Notes
                });
            }

            var summary = new PaymentAllocationSummaryDto
            {
                PaymentId = paymentId,
                PaymentAmount = payment.Amount,
                TotalAllocated = totalAllocated,
                Unallocated = payment.Amount - totalAllocated,
                AllocationCount = allocationDetails.Count,
                Allocations = allocationDetails
            };

            return Result<PaymentAllocationSummaryDto>.Success(summary);
        }

        // ==================== Invoice Status ====================

        public async Task<Result<InvoicePaymentStatusDto>> GetInvoicePaymentStatusAsync(Guid invoiceId)
        {
            var validation = ServiceExtensions.ValidateGuid(invoiceId);
            if (validation.IsFailure)
                return validation.Error!;

            var invoice = await _invoicesRepository.GetByIdAsync(invoiceId);
            if (invoice == null)
                return Error.NotFound($"Invoice with ID {invoiceId} not found");

            var (totalPaid, balanceDue, status) = await _repository.GetInvoicePaymentStatusAsync(invoiceId);
            var allocations = await _repository.GetByInvoiceIdAsync(invoiceId);

            var result = new InvoicePaymentStatusDto
            {
                InvoiceId = invoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceTotal = invoice.TotalAmount,
                TotalPaid = totalPaid,
                BalanceDue = balanceDue,
                Status = status,
                PaymentCount = allocations.Count(),
                LastPaymentDate = allocations.Any() ? allocations.Max(a => a.AllocationDate) : null
            };

            return Result<InvoicePaymentStatusDto>.Success(result);
        }

        public async Task<Result<IEnumerable<InvoicePaymentStatusDto>>> GetCompanyInvoicePaymentStatusAsync(
            Guid companyId,
            string? financialYear = null)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var summaries = await _repository.GetInvoicePaymentSummaryAsync(companyId, financialYear);

            var results = new List<InvoicePaymentStatusDto>();
            foreach (var summary in summaries)
            {
                results.Add(new InvoicePaymentStatusDto
                {
                    InvoiceId = summary.invoice_id,
                    InvoiceNumber = summary.invoice_number,
                    InvoiceTotal = summary.invoice_total,
                    TotalPaid = summary.total_paid,
                    BalanceDue = summary.balance_due,
                    Status = summary.payment_status,
                    PaymentCount = (int)summary.payment_count,
                    LastPaymentDate = summary.last_payment_date != null
                        ? DateOnly.FromDateTime(summary.last_payment_date)
                        : null
                });
            }

            return Result<IEnumerable<InvoicePaymentStatusDto>>.Success(results);
        }

        public async Task<Result<IEnumerable<InvoicePaymentStatusDto>>> GetUnpaidInvoicesForCustomerAsync(Guid customerId)
        {
            var validation = ServiceExtensions.ValidateGuid(customerId);
            if (validation.IsFailure)
                return validation.Error!;

            // This would need a method on the invoices repository to get by customer
            // For now, return empty - this can be enhanced later
            return Result<IEnumerable<InvoicePaymentStatusDto>>.Success(new List<InvoicePaymentStatusDto>());
        }

        // ==================== Bulk Operations ====================

        public async Task<Result> RemoveAllAllocationsAsync(Guid paymentId)
        {
            var validation = ServiceExtensions.ValidateGuid(paymentId);
            if (validation.IsFailure)
                return validation.Error!;

            var payment = await _paymentsRepository.GetByIdAsync(paymentId);
            if (payment == null)
                return Error.NotFound($"Payment with ID {paymentId} not found");

            await _repository.DeleteByPaymentIdAsync(paymentId);
            return Result.Success();
        }
    }
}

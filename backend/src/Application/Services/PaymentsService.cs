using Application.Common;
using Application.Interfaces;
using Application.DTOs.Payments;
using Application.Validators.Payments;
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
    /// Service implementation for Payments operations
    /// Enhanced for Indian tax compliance with TDS tracking and direct payments
    /// </summary>
    public class PaymentsService : IPaymentsService
    {
        private readonly IPaymentsRepository _repository;
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly IPaymentAllocationRepository _allocationRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreatePaymentsDto> _createValidator;
        private readonly IValidator<UpdatePaymentsDto> _updateValidator;

        public PaymentsService(
            IPaymentsRepository repository,
            IInvoicesRepository invoicesRepository,
            IPaymentAllocationRepository allocationRepository,
            IMapper mapper,
            IValidator<CreatePaymentsDto> createValidator,
            IValidator<UpdatePaymentsDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _allocationRepository = allocationRepository ?? throw new ArgumentNullException(nameof(allocationRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        /// <inheritdoc />
        public async Task<Result<Payments>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Payment with ID {id} not found");

            return Result<Payments>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Payments>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<Payments>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<Payments> Items, int TotalCount)>> GetPagedAsync(
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

            return Result<(IEnumerable<Payments> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<Payments>> CreateAsync(CreatePaymentsDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Map DTO to entity
            var entity = _mapper.Map<Payments>(dto);

            // Set default values
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // Auto-populate company and customer from invoice if not provided
            if (entity.InvoiceId.HasValue)
            {
                var invoice = await _invoicesRepository.GetByIdAsync(entity.InvoiceId.Value);
                if (invoice != null)
                {
                    // Set company from invoice if not provided
                    if (!entity.CompanyId.HasValue && invoice.CompanyId != Guid.Empty)
                    {
                        entity.CompanyId = invoice.CompanyId;
                    }
                    // Set customer from invoice if not provided
                    if (!entity.CustomerId.HasValue && invoice.CustomerId != Guid.Empty)
                    {
                        entity.CustomerId = invoice.CustomerId;
                    }
                    // Set currency from invoice if not provided
                    if (string.IsNullOrEmpty(entity.Currency) && !string.IsNullOrEmpty(invoice.Currency))
                    {
                        entity.Currency = invoice.Currency;
                    }
                }
            }

            // Set default payment type if not provided
            if (string.IsNullOrEmpty(entity.PaymentType))
            {
                entity.PaymentType = entity.InvoiceId.HasValue ? "invoice_payment" : "direct_income";
            }

            // Set default currency if not provided
            if (string.IsNullOrEmpty(entity.Currency))
            {
                entity.Currency = "INR";
            }

            // Calculate TDS amount if TDS is applicable but amount not provided
            if (entity.TdsApplicable && entity.TdsRate.HasValue && entity.TdsRate > 0)
            {
                if (!entity.TdsAmount.HasValue && entity.GrossAmount.HasValue)
                {
                    entity.TdsAmount = entity.GrossAmount.Value * entity.TdsRate.Value / 100;
                }
                else if (!entity.GrossAmount.HasValue && entity.TdsAmount.HasValue)
                {
                    // Calculate gross from net + TDS
                    entity.GrossAmount = entity.Amount + entity.TdsAmount.Value;
                }
            }

            // If amount_in_inr not provided and currency is INR, set it
            if (!entity.AmountInInr.HasValue && entity.Currency == "INR")
            {
                entity.AmountInInr = entity.Amount;
            }

            var createdEntity = await _repository.AddAsync(entity);

            // Auto-create payment allocation when payment is linked to an invoice
            if (createdEntity.InvoiceId.HasValue && createdEntity.CompanyId.HasValue)
            {
                var allocation = new PaymentAllocation
                {
                    Id = Guid.NewGuid(),
                    CompanyId = createdEntity.CompanyId.Value,
                    PaymentId = createdEntity.Id,
                    InvoiceId = createdEntity.InvoiceId.Value,
                    AllocatedAmount = createdEntity.GrossAmount ?? createdEntity.Amount,
                    Currency = createdEntity.Currency ?? "INR",
                    AmountInInr = createdEntity.AmountInInr,
                    AllocationDate = createdEntity.PaymentDate,
                    AllocationType = "invoice_settlement",
                    Notes = $"Auto-allocated from payment {createdEntity.ReferenceNumber ?? createdEntity.Id.ToString()}",
                    CreatedAt = DateTime.UtcNow
                };

                await _allocationRepository.AddAsync(allocation);

                // Also update invoice's paidAmount to keep it in sync
                var invoice = await _invoicesRepository.GetByIdAsync(createdEntity.InvoiceId.Value);
                if (invoice != null)
                {
                    var paymentAmount = createdEntity.GrossAmount ?? createdEntity.Amount;
                    invoice.PaidAmount = (invoice.PaidAmount ?? 0) + paymentAmount;

                    // Update status based on paid amount
                    if (invoice.PaidAmount >= invoice.TotalAmount)
                    {
                        invoice.Status = "paid";
                        invoice.PaidAt = DateTime.UtcNow;
                    }
                    else if (invoice.PaidAmount > 0)
                    {
                        invoice.Status = "partially_paid";
                    }

                    invoice.UpdatedAt = DateTime.UtcNow;
                    await _invoicesRepository.UpdateAsync(invoice);
                }
            }

            return Result<Payments>.Success(createdEntity);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdatePaymentsDto dto)
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
                return Error.NotFound($"Payment with ID {id} not found");

            // Map DTO to existing entity
            _mapper.Map(dto, existingEntity);

            // Set updated timestamp
            existingEntity.UpdatedAt = DateTime.UtcNow;

            // Recalculate TDS if applicable
            if (existingEntity.TdsApplicable && existingEntity.TdsRate.HasValue && existingEntity.TdsRate > 0)
            {
                if (existingEntity.GrossAmount.HasValue && !existingEntity.TdsAmount.HasValue)
                {
                    existingEntity.TdsAmount = existingEntity.GrossAmount.Value * existingEntity.TdsRate.Value / 100;
                }
            }

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
                return Error.NotFound($"Payment with ID {id} not found");

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

        // ==================== New Methods ====================

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Payments>>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            var validation = ServiceExtensions.ValidateGuid(invoiceId);
            if (validation.IsFailure)
                return validation.Error!;

            var payments = await _repository.GetByInvoiceIdAsync(invoiceId);
            return Result<IEnumerable<Payments>>.Success(payments);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Payments>>> GetByCompanyIdAsync(Guid companyId)
        {
            var validation = ServiceExtensions.ValidateGuid(companyId);
            if (validation.IsFailure)
                return validation.Error!;

            var payments = await _repository.GetByCompanyIdAsync(companyId);
            return Result<IEnumerable<Payments>>.Success(payments);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Payments>>> GetByCustomerIdAsync(Guid customerId)
        {
            var validation = ServiceExtensions.ValidateGuid(customerId);
            if (validation.IsFailure)
                return validation.Error!;

            var payments = await _repository.GetByCustomerIdAsync(customerId);
            return Result<IEnumerable<Payments>>.Success(payments);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Payments>>> GetByFinancialYearAsync(string financialYear, Guid? companyId = null)
        {
            if (string.IsNullOrWhiteSpace(financialYear))
                return Error.Validation("Financial year is required");

            var payments = await _repository.GetByFinancialYearAsync(financialYear, companyId);
            return Result<IEnumerable<Payments>>.Success(payments);
        }

        /// <inheritdoc />
        public async Task<Result<IncomeSummaryDto>> GetIncomeSummaryAsync(
            Guid? companyId = null,
            string? financialYear = null,
            int? year = null,
            int? month = null)
        {
            var (totalGross, totalTds, totalNet, totalInr) = await _repository.GetIncomeSummaryAsync(
                companyId, financialYear, year, month);

            return Result<IncomeSummaryDto>.Success(new IncomeSummaryDto
            {
                TotalGross = totalGross,
                TotalTds = totalTds,
                TotalNet = totalNet,
                TotalInr = totalInr
            });
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<TdsSummaryDto>>> GetTdsSummaryAsync(Guid? companyId, string financialYear)
        {
            if (string.IsNullOrWhiteSpace(financialYear))
                return Error.Validation("Financial year is required");

            var results = await _repository.GetTdsSummaryAsync(companyId, financialYear);

            var summaries = new List<TdsSummaryDto>();
            foreach (var item in results)
            {
                summaries.Add(new TdsSummaryDto
                {
                    CustomerName = item.customer_name,
                    CustomerPan = item.customer_pan,
                    TdsSection = item.tds_section,
                    PaymentCount = (int)item.payment_count,
                    TotalGross = item.total_gross,
                    TotalTds = item.total_tds,
                    TotalNet = item.total_net
                });
            }

            return Result<IEnumerable<TdsSummaryDto>>.Success(summaries);
        }
    }
}

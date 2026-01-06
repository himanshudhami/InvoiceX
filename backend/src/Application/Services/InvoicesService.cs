using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Forex;
using Application.Interfaces.Ledger;
using Application.DTOs.Invoices;
using Application.DTOs.Payments;
using Application.Validators.Invoices;
using Application.Validators.Payments;
using Core.Entities;
using Core.Interfaces;
using Core.Interfaces.Forex;
using Core.Common;
using AutoMapper;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for Invoices operations
    /// Enhanced with forex accounting (Ind AS 21) and auto-posting support
    /// </summary>
    public class InvoicesService : IInvoicesService
    {
        private readonly IInvoicesRepository _repository;
        private readonly IInvoiceItemsRepository _invoiceItemsRepository;
        private readonly IPaymentsRepository _paymentsRepository;
        private readonly IPaymentAllocationRepository _allocationRepository;
        private readonly IForexService _forexService;
        private readonly IAutoPostingService _autoPostingService;
        private readonly ILutRegisterRepository _lutRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateInvoicesDto> _createValidator;
        private readonly IValidator<UpdateInvoicesDto> _updateValidator;

        // Default exchange rate for USD (fallback when RBI rate not available)
        private const decimal DefaultUsdRate = 83.50m;

        public InvoicesService(
            IInvoicesRepository repository,
            IInvoiceItemsRepository invoiceItemsRepository,
            IPaymentsRepository paymentsRepository,
            IPaymentAllocationRepository allocationRepository,
            IForexService forexService,
            IAutoPostingService autoPostingService,
            ILutRegisterRepository lutRepository,
            IMapper mapper,
            IValidator<CreateInvoicesDto> createValidator,
            IValidator<UpdateInvoicesDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _invoiceItemsRepository = invoiceItemsRepository ?? throw new ArgumentNullException(nameof(invoiceItemsRepository));
            _paymentsRepository = paymentsRepository ?? throw new ArgumentNullException(nameof(paymentsRepository));
            _allocationRepository = allocationRepository ?? throw new ArgumentNullException(nameof(allocationRepository));
            _forexService = forexService ?? throw new ArgumentNullException(nameof(forexService));
            _autoPostingService = autoPostingService ?? throw new ArgumentNullException(nameof(autoPostingService));
            _lutRepository = lutRepository ?? throw new ArgumentNullException(nameof(lutRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        /// <inheritdoc />
        public async Task<Result<Invoices>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Invoices with ID {id} not found");

            return Result<Invoices>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Invoices>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<Invoices>>.Success(entities);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<Invoices> Items, int TotalCount)>> GetPagedAsync(
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
                
            return Result<(IEnumerable<Invoices> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<Invoices>> CreateAsync(CreateInvoicesDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Map DTO to entity
            var entity = _mapper.Map<Invoices>(dto);

            // Set timestamps
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // Handle export invoice forex fields
            var isExport = IsExportInvoice(entity);
            if (isExport)
            {
                await ProcessExportInvoiceForexAsync(entity);
            }

            var createdEntity = await _repository.AddAsync(entity);

            // Create forex booking transaction for export invoices
            if (isExport && createdEntity.CompanyId.HasValue && createdEntity.InvoiceExchangeRate.HasValue)
            {
                await CreateForexBookingAsync(createdEntity);
            }

            // Trigger auto-posting if invoice is finalized (not draft)
            if (!string.IsNullOrEmpty(createdEntity.Status) &&
                !createdEntity.Status.Equals("draft", StringComparison.OrdinalIgnoreCase))
            {
                await _autoPostingService.PostInvoiceAsync(createdEntity.Id);
            }

            return Result<Invoices>.Success(createdEntity);
        }

        /// <summary>
        /// Check if invoice is an export invoice (foreign currency)
        /// </summary>
        private static bool IsExportInvoice(Invoices invoice)
        {
            return invoice.InvoiceType == "export" ||
                   invoice.SupplyType == "export" ||
                   (!string.IsNullOrEmpty(invoice.Currency) &&
                    !invoice.Currency.Equals("INR", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Process forex fields for export invoice (Ind AS 21)
        /// </summary>
        private async Task ProcessExportInvoiceForexAsync(Invoices entity)
        {
            // Set export type if not provided
            if (string.IsNullOrEmpty(entity.InvoiceType))
                entity.InvoiceType = "export";

            if (string.IsNullOrEmpty(entity.SupplyType))
                entity.SupplyType = "export";

            // Set foreign currency amount
            entity.ForeignCurrencyAmount = entity.TotalAmount;
            entity.ForeignCurrency = entity.Currency ?? "USD";

            // Get or use default exchange rate
            var exchangeRate = entity.InvoiceExchangeRate ?? entity.ExchangeRate ?? DefaultUsdRate;
            entity.InvoiceExchangeRate = exchangeRate;
            entity.ExchangeRate = exchangeRate;

            // Calculate INR amount at invoice date rate
            entity.InvoiceAmountInr = entity.ForeignCurrencyAmount * exchangeRate;

            // Set realization due date (9 months from invoice date per FEMA)
            entity.RealizationDueDate = entity.InvoiceDate.AddMonths(9);

            // Get and set LUT details if company has one
            if (entity.CompanyId.HasValue)
            {
                var lut = await _lutRepository.GetValidForDateAsync(entity.CompanyId.Value, entity.InvoiceDate);
                if (lut != null)
                {
                    entity.LutNumber = lut.LutNumber;
                    entity.LutValidFrom = lut.ValidFrom;
                    entity.LutValidTo = lut.ValidTo;
                }
            }

            // Set default purpose code for software services
            if (string.IsNullOrEmpty(entity.PurposeCode))
                entity.PurposeCode = "P0802"; // Computer Software
        }

        /// <summary>
        /// Create forex booking transaction when export invoice is created
        /// </summary>
        private async Task CreateForexBookingAsync(Invoices invoice)
        {
            if (!invoice.CompanyId.HasValue || !invoice.InvoiceExchangeRate.HasValue)
                return;

            var financialYear = GetFinancialYear(invoice.InvoiceDate);

            var bookingRequest = new ForexBookingRequest
            {
                CompanyId = invoice.CompanyId.Value,
                TransactionDate = invoice.InvoiceDate,
                FinancialYear = financialYear,
                SourceType = "invoice",
                SourceId = invoice.Id,
                SourceNumber = invoice.InvoiceNumber,
                Currency = invoice.ForeignCurrency ?? invoice.Currency ?? "USD",
                ForeignAmount = invoice.ForeignCurrencyAmount ?? invoice.TotalAmount,
                ExchangeRate = invoice.InvoiceExchangeRate.Value
            };

            await _forexService.RecordBookingAsync(bookingRequest);
        }

        /// <summary>
        /// Get financial year in format "2025-26" from a date
        /// </summary>
        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateInvoicesDto dto)
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
                return Error.NotFound($"Invoices with ID {id} not found");

            // Track if status is changing from draft to finalized
            var wasNotPosted = string.IsNullOrEmpty(existingEntity.Status) ||
                               existingEntity.Status.Equals("draft", StringComparison.OrdinalIgnoreCase);

            // Map DTO to existing entity
            _mapper.Map(dto, existingEntity);

            // Set updated timestamp
            existingEntity.UpdatedAt = DateTime.UtcNow;

            if (IsExportInvoice(existingEntity))
            {
                await ProcessExportInvoiceForexAsync(existingEntity);
            }

            await _repository.UpdateAsync(existingEntity);

            // Trigger auto-posting if status changed from draft to finalized
            var isNowFinalized = !string.IsNullOrEmpty(existingEntity.Status) &&
                                 !existingEntity.Status.Equals("draft", StringComparison.OrdinalIgnoreCase);
            if (wasNotPosted && isNowFinalized)
            {
                await _autoPostingService.PostInvoiceAsync(existingEntity.Id);
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
                return Error.NotFound($"Invoices with ID {id} not found");

            // Delete related invoice items first
            var invoiceItemsFilters = new Dictionary<string, object> { { "invoice_id", id } };
            var (invoiceItems, _) = await _invoiceItemsRepository.GetPagedAsync(1, int.MaxValue, filters: invoiceItemsFilters);
            
            foreach (var item in invoiceItems)
            {
                await _invoiceItemsRepository.DeleteAsync(item.Id);
            }

            // Delete related payments
            var paymentsFilters = new Dictionary<string, object> { { "invoice_id", id } };
            var (payments, _) = await _paymentsRepository.GetPagedAsync(1, int.MaxValue, filters: paymentsFilters);
            
            foreach (var payment in payments)
            {
                await _paymentsRepository.DeleteAsync(payment.Id);
            }

            // Finally delete the invoice
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

        /// <inheritdoc />
        public async Task<Result<Invoices>> DuplicateAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;
                // Get the original invoice
                var originalInvoice = await _repository.GetByIdAsync(id);
                if (originalInvoice == null)
                    return Error.NotFound($"Invoice with ID {id} not found");

                // Create a new invoice with copied data
                var newInvoice = new Invoices
                {
                    Id = Guid.NewGuid(),
                    CompanyId = originalInvoice.CompanyId,
                    CustomerId = originalInvoice.CustomerId,
                    InvoiceNumber = await GenerateNextInvoiceNumberAsync(),
                    InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), // Default 30 days from today
                    Status = "draft", // Always set to draft for duplicates
                    Subtotal = originalInvoice.Subtotal,
                    TaxAmount = originalInvoice.TaxAmount,
                    DiscountAmount = originalInvoice.DiscountAmount,
                    TotalAmount = originalInvoice.TotalAmount,
                    PaidAmount = 0, // Reset paid amount
                    Currency = originalInvoice.Currency,
                    Notes = originalInvoice.Notes,
                    Terms = originalInvoice.Terms,
                    PaymentInstructions = originalInvoice.PaymentInstructions,
                    PoNumber = originalInvoice.PoNumber,
                    ProjectName = originalInvoice.ProjectName,
                    SentAt = null, // Reset sent timestamp
                    ViewedAt = null, // Reset viewed timestamp
                    PaidAt = null, // Reset paid timestamp
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Create the new invoice
                var createdInvoice = await _repository.AddAsync(newInvoice);

                // Get and duplicate invoice items
                var invoiceItemsFilters = new Dictionary<string, object> { { "invoice_id", id } };
                var (invoiceItems, _) = await _invoiceItemsRepository.GetPagedAsync(1, int.MaxValue, filters: invoiceItemsFilters);

                foreach (var originalItem in invoiceItems)
                {
                    var newItem = new InvoiceItems
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = createdInvoice.Id,
                        ProductId = originalItem.ProductId,
                        Description = originalItem.Description,
                        Quantity = originalItem.Quantity,
                        UnitPrice = originalItem.UnitPrice,
                        TaxRate = originalItem.TaxRate,
                        DiscountRate = originalItem.DiscountRate,
                        LineTotal = originalItem.LineTotal,
                        SortOrder = originalItem.SortOrder,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _invoiceItemsRepository.AddAsync(newItem);
                }

                return Result<Invoices>.Success(createdInvoice);
        }

        /// <inheritdoc />
        public async Task<Result<Payments>> RecordPaymentAsync(Guid invoiceId, Application.DTOs.Payments.CreatePaymentsDto paymentDto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(invoiceId);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            // Verify invoice exists
            var invoice = await _repository.GetByIdAsync(invoiceId);
            if (invoice == null)
                return Error.NotFound($"Invoice with ID {invoiceId} not found");

            // Enrich payment DTO with invoice data
            paymentDto.InvoiceId = invoiceId;
            paymentDto.CompanyId = invoice.CompanyId;
            paymentDto.CustomerId = invoice.CustomerId;
            paymentDto.Currency = invoice.Currency;
            paymentDto.PaymentType = "invoice_payment";

            // Set description with invoice reference if not provided
            if (string.IsNullOrEmpty(paymentDto.Description))
            {
                paymentDto.Description = $"Payment for invoice {invoice.InvoiceNumber}";
            }

            // Determine income category based on currency (export vs domestic)
            if (string.IsNullOrEmpty(paymentDto.IncomeCategory))
            {
                paymentDto.IncomeCategory = invoice.Currency?.ToUpperInvariant() != "INR"
                    ? "export_services"
                    : "domestic_services";
            }

            // Create payment directly using repository (to avoid circular dependency)
            var paymentEntity = _mapper.Map<Payments>(paymentDto);
            paymentEntity.CreatedAt = DateTime.UtcNow;
            paymentEntity.UpdatedAt = DateTime.UtcNow;

            var createdPayment = await _paymentsRepository.AddAsync(paymentEntity);

            // Create payment allocation to track this payment against the invoice
            var allocation = new PaymentAllocation
            {
                Id = Guid.NewGuid(),
                CompanyId = invoice.CompanyId ?? Guid.Empty,
                PaymentId = createdPayment.Id,
                InvoiceId = invoiceId,
                AllocatedAmount = paymentDto.GrossAmount ?? paymentDto.Amount,
                Currency = invoice.Currency ?? "INR",
                AmountInInr = paymentDto.AmountInInr,
                AllocationDate = createdPayment.PaymentDate,
                AllocationType = "invoice_settlement",
                Notes = $"Payment for invoice {invoice.InvoiceNumber}",
                CreatedAt = DateTime.UtcNow
            };

            await _allocationRepository.AddAsync(allocation);

            // Update invoice paid amount and status
            var currentPaidAmount = invoice.PaidAmount ?? 0;
            var newPaidAmount = currentPaidAmount + paymentDto.Amount;
            invoice.PaidAmount = newPaidAmount;

            // Update invoice status based on paid amount
            if (newPaidAmount >= invoice.TotalAmount)
            {
                invoice.Status = "paid";
                invoice.PaidAt = DateTime.UtcNow;
            }
            else if (newPaidAmount > 0)
            {
                invoice.Status = "partially_paid";
            }

            invoice.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(invoice);

            return Result<Payments>.Success(createdPayment);
        }

        private async Task<string> GenerateNextInvoiceNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month.ToString("D2");

            // Get the highest invoice number for this year/month
            var existingInvoices = await _repository.GetAllAsync();
            var maxNumber = existingInvoices
                .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}{month}-"))
                .Select(i => {
                    var parts = i.InvoiceNumber.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var num))
                        return num;
                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"INV-{year}{month}-{(maxNumber + 1).ToString("D3")}";
        }

    }
}

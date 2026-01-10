using Application.Common;
using Application.DTOs.CreditNotes;
using Application.Interfaces;
using Application.Interfaces.Audit;
using Application.Validators;
using Core.Common;
using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using FluentValidation;

namespace Application.Services
{
    public class CreditNotesService : ICreditNotesService
    {
        private readonly ICreditNotesRepository _repository;
        private readonly ICreditNoteItemsRepository _itemsRepository;
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly IInvoiceItemsRepository _invoiceItemsRepository;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateCreditNotesDto> _createValidator;
        private readonly IValidator<UpdateCreditNotesDto> _updateValidator;

        public CreditNotesService(
            ICreditNotesRepository repository,
            ICreditNoteItemsRepository itemsRepository,
            IInvoicesRepository invoicesRepository,
            IInvoiceItemsRepository invoiceItemsRepository,
            IAuditService auditService,
            IMapper mapper,
            IValidator<CreateCreditNotesDto> createValidator,
            IValidator<UpdateCreditNotesDto> updateValidator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _itemsRepository = itemsRepository ?? throw new ArgumentNullException(nameof(itemsRepository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _invoiceItemsRepository = invoiceItemsRepository ?? throw new ArgumentNullException(nameof(invoiceItemsRepository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        public async Task<Result<CreditNotes>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Credit note with ID {id} not found");

            return Result<CreditNotes>.Success(entity);
        }

        public async Task<Result<IEnumerable<CreditNotes>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<CreditNotes>>.Success(entities);
        }

        public async Task<Result<(IEnumerable<CreditNotes> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            var result = await _repository.GetPagedAsync(
                pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
            return Result<(IEnumerable<CreditNotes>, int)>.Success(result);
        }

        public async Task<Result<CreditNotes>> CreateAsync(CreateCreditNotesDto dto, List<CreditNoteItemDto>? items = null)
        {
            // Validate DTO
            var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            // Get original invoice
            var invoice = await _invoicesRepository.GetByIdAsync(dto.OriginalInvoiceId);
            if (invoice == null)
                return Error.NotFound($"Original invoice with ID {dto.OriginalInvoiceId} not found");

            // Map DTO to entity
            var entity = _mapper.Map<CreditNotes>(dto);

            // Set invoice reference fields
            entity.OriginalInvoiceNumber = invoice.InvoiceNumber;
            entity.OriginalInvoiceDate = invoice.InvoiceDate;
            entity.PartyId = invoice.PartyId;

            // Inherit GST fields from invoice if not provided
            entity.InvoiceType ??= invoice.InvoiceType;
            entity.SupplyType ??= invoice.SupplyType;
            entity.PlaceOfSupply ??= invoice.PlaceOfSupply;
            entity.Currency ??= invoice.Currency;

            // Determine if ITC reversal is required (for domestic B2B)
            if (entity.InvoiceType == "domestic_b2b" && entity.TotalAmount > 0)
            {
                entity.ItcReversalRequired = true;
            }

            // Set timestamps
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // Create credit note
            var createdEntity = await _repository.AddAsync(entity);

            // Audit trail
            if (createdEntity.CompanyId.HasValue)
            {
                await _auditService.AuditCreateAsync(createdEntity, createdEntity.Id, createdEntity.CompanyId.Value, createdEntity.CreditNoteNumber);
            }

            // Create items if provided
            if (items != null && items.Any())
            {
                foreach (var itemDto in items)
                {
                    var item = _mapper.Map<CreditNoteItems>(itemDto);
                    item.CreditNoteId = createdEntity.Id;
                    await _itemsRepository.AddAsync(item);
                }
            }

            return Result<CreditNotes>.Success(createdEntity);
        }

        public async Task<Result<CreditNotes>> CreateFromInvoiceAsync(CreateCreditNoteFromInvoiceDto dto)
        {
            // Get and validate original invoice
            var invoice = await _invoicesRepository.GetByIdAsync(dto.InvoiceId);
            if (invoice == null)
                return Error.NotFound($"Invoice with ID {dto.InvoiceId} not found");

            var validationResult = await ValidateInvoiceForCreditNoteAsync(invoice, dto.InvoiceId);
            if (validationResult.IsFailure)
                return validationResult.Error!;

            var remainingAmount = validationResult.Value;

            // Get invoice items
            var itemFilters = new Dictionary<string, object> { { "invoice_id", dto.InvoiceId } };
            var (invoiceItemsResult, _) = await _invoiceItemsRepository.GetPagedAsync(1, 1000, filters: itemFilters);
            var invoiceItems = invoiceItemsResult.ToList();

            // Build credit note items based on full/partial
            var itemsResult = BuildCreditNoteItems(dto, invoiceItems);
            if (itemsResult.IsFailure)
                return itemsResult.Error!;

            var (creditNoteItems, totals) = itemsResult.Value;
            var totalAmount = totals.Subtotal + totals.TaxAmount - (invoice.DiscountAmount ?? 0);

            // Validate credit amount doesn't exceed remaining
            if (totalAmount > remainingAmount)
                return Error.Validation($"Credit note amount ({totalAmount}) exceeds remaining creditable amount ({remainingAmount})");

            // Generate credit note number and build entity
            var creditNoteNumber = await _repository.GenerateNextNumberAsync(invoice.CompanyId ?? Guid.Empty);
            var creditNote = BuildCreditNoteEntity(invoice, dto, creditNoteNumber, totals, totalAmount);

            // Save credit note and items
            var createdCreditNote = await _repository.AddAsync(creditNote);
            await SaveCreditNoteItemsAsync(creditNoteItems, createdCreditNote.Id);

            // Audit trail
            if (createdCreditNote.CompanyId.HasValue)
            {
                await _auditService.AuditCreateAsync(createdCreditNote, createdCreditNote.Id, createdCreditNote.CompanyId.Value, createdCreditNote.CreditNoteNumber);
            }

            return Result<CreditNotes>.Success(createdCreditNote);
        }

        private async Task<Result<decimal>> ValidateInvoiceForCreditNoteAsync(Invoices invoice, Guid invoiceId)
        {
            if (invoice.Status == "draft")
                return Error.Validation("Cannot create credit note for a draft invoice");

            if (invoice.Status == "cancelled")
                return Error.Validation("Cannot create credit note for a cancelled invoice");

            var existingCreditTotal = await _repository.GetTotalCreditedAmountForInvoiceAsync(invoiceId);
            var remainingAmount = invoice.TotalAmount - existingCreditTotal;

            if (remainingAmount <= 0)
                return Error.Validation("Invoice has already been fully credited");

            return Result<decimal>.Success(remainingAmount);
        }

        private Result<(List<CreditNoteItems> Items, CreditNoteTotals Totals)> BuildCreditNoteItems(
            CreateCreditNoteFromInvoiceDto dto,
            List<InvoiceItems> invoiceItems)
        {
            if (dto.IsFullCreditNote)
            {
                return Result<(List<CreditNoteItems>, CreditNoteTotals)>.Success(
                    CreateFullCreditNoteItems(invoiceItems));
            }

            if (dto.Items != null && dto.Items.Any())
            {
                return CreatePartialCreditNoteItems(dto.Items, invoiceItems);
            }

            return Error.Validation("Either IsFullCreditNote must be true or Items must be provided");
        }

        private (List<CreditNoteItems> Items, CreditNoteTotals Totals) CreateFullCreditNoteItems(
            List<InvoiceItems> invoiceItems)
        {
            var creditNoteItems = new List<CreditNoteItems>();
            var totals = new CreditNoteTotals();

            foreach (var item in invoiceItems)
            {
                creditNoteItems.Add(new CreditNoteItems
                {
                    OriginalInvoiceItemId = item.Id,
                    ProductId = item.ProductId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = item.TaxRate,
                    DiscountRate = item.DiscountRate,
                    LineTotal = item.LineTotal,
                    SortOrder = item.SortOrder,
                    HsnSacCode = item.HsnSacCode,
                    IsService = item.IsService,
                    CgstRate = item.CgstRate,
                    CgstAmount = item.CgstAmount,
                    SgstRate = item.SgstRate,
                    SgstAmount = item.SgstAmount,
                    IgstRate = item.IgstRate,
                    IgstAmount = item.IgstAmount,
                    CessRate = item.CessRate,
                    CessAmount = item.CessAmount
                });

                totals.Subtotal += item.LineTotal;
                totals.TotalCgst += item.CgstAmount;
                totals.TotalSgst += item.SgstAmount;
                totals.TotalIgst += item.IgstAmount;
                totals.TotalCess += item.CessAmount;
            }

            totals.TaxAmount = totals.TotalCgst + totals.TotalSgst + totals.TotalIgst + totals.TotalCess;
            return (creditNoteItems, totals);
        }

        private Result<(List<CreditNoteItems> Items, CreditNoteTotals Totals)> CreatePartialCreditNoteItems(
            List<PartialCreditNoteItem> partialItems,
            List<InvoiceItems> invoiceItems)
        {
            var creditNoteItems = new List<CreditNoteItems>();
            var totals = new CreditNoteTotals();

            foreach (var partialItem in partialItems)
            {
                var originalItem = invoiceItems.FirstOrDefault(i => i.Id == partialItem.OriginalItemId);
                if (originalItem == null)
                    return Error.Validation($"Invoice item with ID {partialItem.OriginalItemId} not found");

                if (partialItem.Quantity > originalItem.Quantity)
                    return Error.Validation($"Credit quantity ({partialItem.Quantity}) cannot exceed original quantity ({originalItem.Quantity})");

                var unitPrice = partialItem.UnitPrice ?? originalItem.UnitPrice;
                var lineTotal = partialItem.Quantity * unitPrice;
                var ratio = lineTotal / originalItem.LineTotal;

                creditNoteItems.Add(new CreditNoteItems
                {
                    OriginalInvoiceItemId = originalItem.Id,
                    ProductId = originalItem.ProductId,
                    Description = originalItem.Description,
                    Quantity = partialItem.Quantity,
                    UnitPrice = unitPrice,
                    TaxRate = originalItem.TaxRate,
                    DiscountRate = originalItem.DiscountRate,
                    LineTotal = lineTotal,
                    SortOrder = originalItem.SortOrder,
                    HsnSacCode = originalItem.HsnSacCode,
                    IsService = originalItem.IsService,
                    CgstRate = originalItem.CgstRate,
                    CgstAmount = originalItem.CgstAmount * ratio,
                    SgstRate = originalItem.SgstRate,
                    SgstAmount = originalItem.SgstAmount * ratio,
                    IgstRate = originalItem.IgstRate,
                    IgstAmount = originalItem.IgstAmount * ratio,
                    CessRate = originalItem.CessRate,
                    CessAmount = originalItem.CessAmount * ratio
                });

                totals.Subtotal += lineTotal;
                totals.TotalCgst += originalItem.CgstAmount * ratio;
                totals.TotalSgst += originalItem.SgstAmount * ratio;
                totals.TotalIgst += originalItem.IgstAmount * ratio;
                totals.TotalCess += originalItem.CessAmount * ratio;
            }

            totals.TaxAmount = totals.TotalCgst + totals.TotalSgst + totals.TotalIgst + totals.TotalCess;
            return Result<(List<CreditNoteItems>, CreditNoteTotals)>.Success((creditNoteItems, totals));
        }

        private CreditNotes BuildCreditNoteEntity(
            Invoices invoice,
            CreateCreditNoteFromInvoiceDto dto,
            string creditNoteNumber,
            CreditNoteTotals totals,
            decimal totalAmount)
        {
            var creditNote = new CreditNotes
            {
                CompanyId = invoice.CompanyId,
                PartyId = invoice.PartyId,
                CreditNoteNumber = creditNoteNumber,
                CreditNoteDate = DateOnly.FromDateTime(DateTime.Today),
                OriginalInvoiceId = invoice.Id,
                OriginalInvoiceNumber = invoice.InvoiceNumber,
                OriginalInvoiceDate = invoice.InvoiceDate,
                Reason = dto.Reason,
                ReasonDescription = dto.ReasonDescription,
                Status = "draft",
                Subtotal = totals.Subtotal,
                TaxAmount = totals.TaxAmount,
                DiscountAmount = 0,
                TotalAmount = totalAmount,
                Currency = invoice.Currency,
                Notes = dto.Notes,
                InvoiceType = invoice.InvoiceType,
                SupplyType = invoice.SupplyType,
                PlaceOfSupply = invoice.PlaceOfSupply,
                ReverseCharge = invoice.ReverseCharge,
                TotalCgst = totals.TotalCgst,
                TotalSgst = totals.TotalSgst,
                TotalIgst = totals.TotalIgst,
                TotalCess = totals.TotalCess,
                EInvoiceApplicable = invoice.EInvoiceApplicable,
                ForeignCurrency = invoice.ForeignCurrency,
                ExchangeRate = invoice.ExchangeRate,
                ItcReversalRequired = invoice.InvoiceType == "domestic_b2b",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (invoice.InvoiceType == "export" && invoice.ExchangeRate.HasValue)
            {
                creditNote.AmountInInr = totalAmount * invoice.ExchangeRate.Value;
            }

            return creditNote;
        }

        private async Task SaveCreditNoteItemsAsync(List<CreditNoteItems> items, Guid creditNoteId)
        {
            foreach (var item in items)
            {
                item.CreditNoteId = creditNoteId;
                await _itemsRepository.AddAsync(item);
            }
        }

        private class CreditNoteTotals
        {
            public decimal Subtotal { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal TotalCgst { get; set; }
            public decimal TotalSgst { get; set; }
            public decimal TotalIgst { get; set; }
            public decimal TotalCess { get; set; }
        }

        public async Task<Result> UpdateAsync(Guid id, UpdateCreditNotesDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Credit note with ID {id} not found");

            // Can only update draft credit notes
            if (existing.Status != "draft")
                return Error.Validation("Only draft credit notes can be updated");

            // Capture state before update for audit trail
            var oldEntity = _mapper.Map<CreditNotes>(existing);

            // Update entity
            _mapper.Map(dto, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);

            // Audit trail
            if (existing.CompanyId.HasValue)
            {
                await _auditService.AuditUpdateAsync(oldEntity, existing, existing.Id, existing.CompanyId.Value, existing.CreditNoteNumber);
            }

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Credit note with ID {id} not found");

            // Can only delete draft credit notes
            if (existing.Status != "draft")
                return Error.Validation("Only draft credit notes can be deleted");

            // Audit trail before delete
            if (existing.CompanyId.HasValue)
            {
                await _auditService.AuditDeleteAsync(existing, existing.Id, existing.CompanyId.Value, existing.CreditNoteNumber);
            }

            // Delete items first
            await _itemsRepository.DeleteByCreditNoteIdAsync(id);

            // Delete credit note
            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        public async Task<Result<bool>> ExistsAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return Result<bool>.Success(entity != null);
        }

        public async Task<Result<IEnumerable<CreditNotes>>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            var validation = ServiceExtensions.ValidateGuid(invoiceId);
            if (validation.IsFailure)
                return validation.Error!;

            var entities = await _repository.GetByInvoiceIdAsync(invoiceId);
            return Result<IEnumerable<CreditNotes>>.Success(entities);
        }

        public async Task<Result<string>> GenerateNextNumberAsync(Guid companyId)
        {
            var number = await _repository.GenerateNextNumberAsync(companyId);
            return Result<string>.Success(number);
        }

        public async Task<Result<CreditNotes>> IssueAsync(Guid id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Credit note with ID {id} not found");

            if (existing.Status != "draft")
                return Error.Validation("Only draft credit notes can be issued");

            existing.Status = "issued";
            existing.IssuedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);
            return Result<CreditNotes>.Success(existing);
        }

        public async Task<Result<CreditNotes>> CancelAsync(Guid id, string? reason = null)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Credit note with ID {id} not found");

            if (existing.Status == "cancelled")
                return Error.Validation("Credit note is already cancelled");

            // Cannot cancel if IRN is generated (need to cancel on IRP first)
            if (!string.IsNullOrEmpty(existing.Irn) && existing.IrnCancelledAt == null)
                return Error.Validation("Credit note has an active IRN. Cancel on e-invoice portal first.");

            existing.Status = "cancelled";
            existing.CancelledAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(reason))
            {
                existing.Notes = (existing.Notes ?? "") + $"\nCancellation reason: {reason}";
            }

            await _repository.UpdateAsync(existing);
            return Result<CreditNotes>.Success(existing);
        }

        public async Task<Result<IEnumerable<CreditNoteItems>>> GetItemsAsync(Guid creditNoteId)
        {
            var validation = ServiceExtensions.ValidateGuid(creditNoteId);
            if (validation.IsFailure)
                return validation.Error!;

            var items = await _itemsRepository.GetByCreditNoteIdAsync(creditNoteId);
            return Result<IEnumerable<CreditNoteItems>>.Success(items);
        }
    }
}

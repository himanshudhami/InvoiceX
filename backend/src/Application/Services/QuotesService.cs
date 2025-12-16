using Application.Interfaces;
using Application.DTOs.Quotes;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for Quotes operations
    /// </summary>
    public class QuotesService : IQuotesService
    {
        private readonly IQuotesRepository _repository;
        private readonly IQuoteItemsRepository _quoteItemsRepository;
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly IInvoiceItemsRepository _invoiceItemsRepository;
        private readonly IMapper _mapper;

        public QuotesService(
            IQuotesRepository repository,
            IQuoteItemsRepository quoteItemsRepository,
            IInvoicesRepository invoicesRepository,
            IInvoiceItemsRepository invoiceItemsRepository,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _quoteItemsRepository = quoteItemsRepository ?? throw new ArgumentNullException(nameof(quoteItemsRepository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _invoiceItemsRepository = invoiceItemsRepository ?? throw new ArgumentNullException(nameof(invoiceItemsRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public async Task<Result<Quotes>> GetByIdAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Quote with ID {id} not found");

            return Result<Quotes>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Quotes>>> GetAllAsync()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return Result<IEnumerable<Quotes>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve quotes: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<Quotes> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            try
            {
                var result = await _repository.GetPagedAsync(pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
                return Result<(IEnumerable<Quotes> Items, int TotalCount)>.Success(result);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve paged quotes: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<Quotes>> CreateAsync(CreateQuotesDto dto)
        {
            try
            {
                var entity = _mapper.Map<Quotes>(dto);
                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                var createdEntity = await _repository.AddAsync(entity);
                return Result<Quotes>.Success(createdEntity);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to create quote: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateQuotesDto dto)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            try
            {
                var existingEntity = await _repository.GetByIdAsync(id);
                if (existingEntity == null)
                    return Error.NotFound($"Quote with ID {id} not found");

                _mapper.Map(dto, existingEntity);
                existingEntity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(existingEntity);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to update quote: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Error.NotFound($"Quote with ID {id} not found");

                await _repository.DeleteAsync(id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to delete quote: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<bool>> ExistsAsync(Guid id)
        {
            try
            {
                var exists = await _repository.ExistsAsync(id);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to check quote existence: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<Quotes>> DuplicateAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            try
            {
                var originalQuote = await _repository.GetByIdAsync(id);
                if (originalQuote == null)
                    return Error.NotFound($"Quote with ID {id} not found");

                var duplicatedQuote = new Quotes
                {
                    Id = Guid.NewGuid(),
                    CompanyId = originalQuote.CompanyId,
                    CustomerId = originalQuote.CustomerId,
                    QuoteNumber = await GenerateNextQuoteNumberAsync(originalQuote.CompanyId),
                    QuoteDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                    Status = "draft",
                    Subtotal = originalQuote.Subtotal,
                    DiscountType = originalQuote.DiscountType,
                    DiscountValue = originalQuote.DiscountValue,
                    DiscountAmount = originalQuote.DiscountAmount,
                    TaxAmount = originalQuote.TaxAmount,
                    TotalAmount = originalQuote.TotalAmount,
                    Currency = originalQuote.Currency,
                    Notes = originalQuote.Notes,
                    Terms = originalQuote.Terms,
                    PaymentInstructions = originalQuote.PaymentInstructions,
                    PoNumber = originalQuote.PoNumber,
                    ProjectName = originalQuote.ProjectName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdQuote = await _repository.AddAsync(duplicatedQuote);

                // Duplicate quote items
                var originalItems = await _quoteItemsRepository.GetByQuoteIdAsync(id);
                foreach (var item in originalItems)
                {
                    var duplicatedItem = new QuoteItems
                    {
                        Id = Guid.NewGuid(),
                        QuoteId = createdQuote.Id,
                        ProductId = item.ProductId,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TaxRate = item.TaxRate,
                        DiscountRate = item.DiscountRate,
                        LineTotal = item.LineTotal,
                        SortOrder = item.SortOrder,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _quoteItemsRepository.AddAsync(duplicatedItem);
                }

                return Result<Quotes>.Success(createdQuote);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to duplicate quote: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> SendAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Error.NotFound($"Quote with ID {id} not found");

                if (entity.Status != "draft")
                    return Error.Validation("Only draft quotes can be sent");

                entity.Status = "sent";
                entity.SentAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(entity);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to send quote: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> AcceptAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Error.NotFound($"Quote with ID {id} not found");

                if (entity.Status != "sent" && entity.Status != "viewed")
                    return Error.Validation("Only sent or viewed quotes can be accepted");

                entity.Status = "accepted";
                entity.AcceptedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(entity);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to accept quote: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> RejectAsync(Guid id, string? reason = null)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Error.NotFound($"Quote with ID {id} not found");

                if (entity.Status != "sent" && entity.Status != "viewed")
                    return Error.Validation("Only sent or viewed quotes can be rejected");

                entity.Status = "rejected";
                entity.RejectedAt = DateTime.UtcNow;
                entity.RejectedReason = reason;
                entity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(entity);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to reject quote: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<Invoices>> ConvertToInvoiceAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            try
            {
                var quote = await _repository.GetByIdAsync(id);
                if (quote == null)
                    return Error.NotFound($"Quote with ID {id} not found");

                if (quote.Status != "accepted")
                    return Error.Validation("Only accepted quotes can be converted to invoices");

                var invoice = new Invoices
                {
                    Id = Guid.NewGuid(),
                    CompanyId = quote.CompanyId,
                    CustomerId = quote.CustomerId,
                    InvoiceNumber = await GenerateNextInvoiceNumberAsync(quote.CompanyId),
                    InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), // Default 30 days
                    Status = "draft",
                    Subtotal = quote.Subtotal,
                    TaxAmount = quote.TaxAmount,
                    DiscountAmount = quote.DiscountAmount,
                    TotalAmount = quote.TotalAmount,
                    PaidAmount = 0,
                    Currency = quote.Currency,
                    Notes = quote.Notes,
                    Terms = quote.Terms,
                    PaymentInstructions = quote.PaymentInstructions,
                    PoNumber = quote.PoNumber,
                    ProjectName = quote.ProjectName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdInvoice = await _invoicesRepository.AddAsync(invoice);

                // Convert quote items to invoice items
                var quoteItems = await _quoteItemsRepository.GetByQuoteIdAsync(id);
                foreach (var item in quoteItems)
                {
                    var invoiceItem = new InvoiceItems
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = createdInvoice.Id,
                        ProductId = item.ProductId,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TaxRate = item.TaxRate,
                        DiscountRate = item.DiscountRate,
                        LineTotal = item.LineTotal,
                        SortOrder = item.SortOrder,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _invoiceItemsRepository.AddAsync(invoiceItem);
                }

                return Result<Invoices>.Success(createdInvoice);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to convert quote to invoice: {ex.Message}");
            }
        }

        private async Task<string> GenerateNextQuoteNumberAsync(Guid? companyId)
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month.ToString("D2");

            // Get the highest quote number for this year/month
            var existingQuotes = await _repository.GetAllAsync();
            var maxNumber = existingQuotes
                .Where(q => q.QuoteNumber.StartsWith($"QUO-{year}{month}-"))
                .Select(q => {
                    var parts = q.QuoteNumber.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var num))
                        return num;
                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"QUO-{year}{month}-{(maxNumber + 1).ToString("D3")}";
        }

        private async Task<string> GenerateNextInvoiceNumberAsync(Guid? companyId)
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month.ToString("D2");

            // Get the highest invoice number for this year/month
            var existingInvoices = await _invoicesRepository.GetAllAsync();
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

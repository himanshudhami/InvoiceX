using Application.DTOs.Gst;
using Application.Interfaces.Gst;
using Core.Common;
using Core.Interfaces;
using Core.Interfaces.Forex;

namespace Application.Services.Gst
{
    /// <summary>
    /// Service for GSTR-1 preparation and filing
    /// </summary>
    public class Gstr1Service : IGstr1Service
    {
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly ICustomersRepository _customersRepository;
        private readonly ICompaniesRepository _companiesRepository;
        private readonly ILutRegisterRepository _lutRepository;

        public Gstr1Service(
            IInvoicesRepository invoicesRepository,
            ICustomersRepository customersRepository,
            ICompaniesRepository companiesRepository,
            ILutRegisterRepository lutRepository)
        {
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _customersRepository = customersRepository ?? throw new ArgumentNullException(nameof(customersRepository));
            _companiesRepository = companiesRepository ?? throw new ArgumentNullException(nameof(companiesRepository));
            _lutRepository = lutRepository ?? throw new ArgumentNullException(nameof(lutRepository));
        }

        // ==================== Data Extraction ====================

        /// <inheritdoc />
        public async Task<Result<Gstr1DataDto>> GenerateGstr1DataAsync(Guid companyId, string returnPeriod)
        {
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
                return Error.NotFound($"Company with ID {companyId} not found");

            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);

            var data = new Gstr1DataDto
            {
                CompanyId = companyId,
                Gstin = company.Gstin ?? string.Empty,
                ReturnPeriod = returnPeriod,
                LegalName = company.Name ?? string.Empty
            };

            // Get B2B data
            var b2bResult = await GetB2bDataAsync(companyId, returnPeriod);
            if (b2bResult.IsSuccess)
                data.B2b = b2bResult.Value!.ToList();

            // Get B2C Large data
            var b2clResult = await GetB2clDataAsync(companyId, returnPeriod);
            if (b2clResult.IsSuccess)
                data.B2cl = b2clResult.Value!.ToList();

            // Get Exports data
            var exportsWithTax = await GetExportsWithTaxAsync(companyId, returnPeriod);
            var exportsWithoutTax = await GetExportsWithoutTaxAsync(companyId, returnPeriod);
            if (exportsWithTax.IsSuccess)
                data.Exp.AddRange(exportsWithTax.Value!);
            if (exportsWithoutTax.IsSuccess)
                data.Exp.AddRange(exportsWithoutTax.Value!);

            // Get HSN summary
            var hsnResult = await GetHsnSummaryAsync(companyId, returnPeriod);
            if (hsnResult.IsSuccess)
                data.Hsn = hsnResult.Value!.ToList();

            // Get document issued summary
            var docResult = await GetDocumentIssuedSummaryAsync(companyId, returnPeriod);
            if (docResult.IsSuccess)
                data.DocIssued = docResult.Value!.ToList();

            // Calculate tax summary
            var taxSummaryResult = await GetTaxSummaryAsync(companyId, returnPeriod);
            if (taxSummaryResult.IsSuccess)
                data.TaxSummary = taxSummaryResult.Value!;

            return Result<Gstr1DataDto>.Success(data);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Gstr1B2bDto>>> GetB2bDataAsync(Guid companyId, string returnPeriod)
        {
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var invoices = await GetInvoicesForPeriod(companyId, startDate, endDate);

            var b2bInvoices = new List<Gstr1B2bDto>();

            foreach (var invoice in invoices.Where(i =>
                i.Currency == "INR" &&
                i.InvoiceType == "domestic_b2b"))
            {
                var customer = await _customersRepository.GetByIdAsync(invoice.PartyId ?? Guid.Empty);
                if (customer == null || string.IsNullOrEmpty(customer.Gstin))
                    continue;

                b2bInvoices.Add(new Gstr1B2bDto
                {
                    ReceiverGstin = customer.Gstin,
                    ReceiverName = customer.Name ?? string.Empty,
                    InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                    InvoiceDate = invoice.InvoiceDate,
                    InvoiceValue = invoice.TotalAmount,
                    PlaceOfSupply = invoice.PlaceOfSupply ?? GetStateCode(customer.State),
                    ReverseCharge = invoice.ReverseCharge,
                    InvoiceType = "R",
                    Rate = GetPrimaryGstRate(invoice),
                    TaxableValue = invoice.Subtotal,
                    IgstAmount = invoice.TotalIgst,
                    CgstAmount = invoice.TotalCgst,
                    SgstAmount = invoice.TotalSgst,
                    CessAmount = invoice.TotalCess,
                    Irn = invoice.EInvoiceIrn,
                    IrnDate = null  // IRN date not stored on invoice
                });
            }

            return Result<IEnumerable<Gstr1B2bDto>>.Success(b2bInvoices);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Gstr1B2clDto>>> GetB2clDataAsync(Guid companyId, string returnPeriod)
        {
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var invoices = await GetInvoicesForPeriod(companyId, startDate, endDate);

            var b2clInvoices = new List<Gstr1B2clDto>();

            foreach (var invoice in invoices.Where(i =>
                i.Currency == "INR" &&
                i.TotalAmount > 250000 &&  // Interstate > 2.5L
                (i.InvoiceType == "domestic_b2c" || string.IsNullOrEmpty(i.InvoiceType))))
            {
                var customer = await _customersRepository.GetByIdAsync(invoice.PartyId ?? Guid.Empty);
                if (customer != null && !string.IsNullOrEmpty(customer.Gstin))
                    continue;  // B2B, not B2C

                // Check if interstate
                var company = await _companiesRepository.GetByIdAsync(companyId);
                if (company?.State == customer?.State)
                    continue;  // Intrastate, not B2CL

                b2clInvoices.Add(new Gstr1B2clDto
                {
                    PlaceOfSupply = invoice.PlaceOfSupply ?? GetStateCode(customer?.State),
                    InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                    InvoiceDate = invoice.InvoiceDate,
                    InvoiceValue = invoice.TotalAmount,
                    Rate = GetPrimaryGstRate(invoice),
                    TaxableValue = invoice.Subtotal,
                    IgstAmount = invoice.TotalIgst,
                    CessAmount = invoice.TotalCess
                });
            }

            return Result<IEnumerable<Gstr1B2clDto>>.Success(b2clInvoices);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Gstr1ExportDto>>> GetExportsWithTaxAsync(Guid companyId, string returnPeriod)
        {
            // Exports with payment of IGST - rare case, usually under LUT
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var invoices = await GetInvoicesForPeriod(companyId, startDate, endDate);

            var exports = new List<Gstr1ExportDto>();

            foreach (var invoice in invoices.Where(i =>
                !string.IsNullOrEmpty(i.Currency) &&
                i.Currency != "INR" &&
                i.TotalIgst > 0))  // Has IGST = with payment
            {
                var customer = await _customersRepository.GetByIdAsync(invoice.PartyId ?? Guid.Empty);

                exports.Add(await CreateExportDto(invoice, customer, "WPAY", companyId));
            }

            return Result<IEnumerable<Gstr1ExportDto>>.Success(exports);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Gstr1ExportDto>>> GetExportsWithoutTaxAsync(Guid companyId, string returnPeriod)
        {
            // Exports under LUT (without payment of IGST) - most common
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var invoices = await GetInvoicesForPeriod(companyId, startDate, endDate);

            var exports = new List<Gstr1ExportDto>();

            foreach (var invoice in invoices.Where(i =>
                !string.IsNullOrEmpty(i.Currency) &&
                i.Currency != "INR" &&
                (i.TotalIgst == 0 || i.TotalIgst == null)))  // No IGST = under LUT
            {
                var customer = await _customersRepository.GetByIdAsync(invoice.PartyId ?? Guid.Empty);

                exports.Add(await CreateExportDto(invoice, customer, "WOPAY", companyId));
            }

            return Result<IEnumerable<Gstr1ExportDto>>.Success(exports);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Gstr1HsnSummaryDto>>> GetHsnSummaryAsync(Guid companyId, string returnPeriod)
        {
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var invoices = await GetInvoicesForPeriod(companyId, startDate, endDate);

            // For services business, use default HSN code
            // In a full implementation, this would aggregate by HSN code from invoice items
            var defaultHsn = "998314";  // Default HSN for IT services
            var invoiceList = invoices.ToList();

            if (!invoiceList.Any())
                return Result<IEnumerable<Gstr1HsnSummaryDto>>.Success(Enumerable.Empty<Gstr1HsnSummaryDto>());

            var hsnSummary = new List<Gstr1HsnSummaryDto>
            {
                new Gstr1HsnSummaryDto
                {
                    HsnCode = defaultHsn,
                    Description = GetHsnDescription(defaultHsn),
                    Uqc = "NA",  // Not Applicable for services
                    TotalQuantity = invoiceList.Count,
                    TotalValue = invoiceList.Sum(i => i.TotalAmount),
                    TaxableValue = invoiceList.Sum(i => i.Subtotal),
                    IgstAmount = invoiceList.Sum(i => i.TotalIgst),
                    CgstAmount = invoiceList.Sum(i => i.TotalCgst),
                    SgstAmount = invoiceList.Sum(i => i.TotalSgst),
                    CessAmount = invoiceList.Sum(i => i.TotalCess)
                }
            };

            return Result<IEnumerable<Gstr1HsnSummaryDto>>.Success(hsnSummary);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Gstr1DocIssuedDto>>> GetDocumentIssuedSummaryAsync(Guid companyId, string returnPeriod)
        {
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var invoices = await GetInvoicesForPeriod(companyId, startDate, endDate);

            var allInvoices = invoices.ToList();
            var activeInvoices = allInvoices.Where(i => i.Status != "cancelled").ToList();
            var cancelledInvoices = allInvoices.Where(i => i.Status == "cancelled").ToList();

            var summary = new List<Gstr1DocIssuedDto>();

            if (allInvoices.Any())
            {
                var sortedInvoices = allInvoices.OrderBy(i => i.InvoiceNumber).ToList();

                summary.Add(new Gstr1DocIssuedDto
                {
                    DocumentType = "Invoices for outward supply",
                    SrNoFrom = sortedInvoices.First().InvoiceNumber ?? string.Empty,
                    SrNoTo = sortedInvoices.Last().InvoiceNumber ?? string.Empty,
                    TotalNumber = allInvoices.Count,
                    Cancelled = cancelledInvoices.Count
                });
            }

            return Result<IEnumerable<Gstr1DocIssuedDto>>.Success(summary);
        }

        // ==================== Filing Management ====================

        /// <inheritdoc />
        public Task<Result<Gstr1FilingDto>> SaveFilingAsync(Guid companyId, string returnPeriod, Gstr1DataDto data)
        {
            // In a full implementation, this would save to a gstr1_filings table
            var filing = new Gstr1FilingDto
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Gstin = data.Gstin,
                ReturnPeriod = returnPeriod,
                Status = "generated",
                GeneratedAt = DateTime.UtcNow,
                TotalB2bInvoices = data.B2b.Count,
                TotalExportInvoices = data.Exp.Count,
                TotalTaxableValue = data.TaxSummary.OutwardTaxableTotalValue + data.TaxSummary.ZeroRatedExportsWithoutTaxValue,
                TotalTaxAmount = data.TaxSummary.TotalTax
            };

            return Task.FromResult(Result<Gstr1FilingDto>.Success(filing));
        }

        /// <inheritdoc />
        public Task<Result> MarkAsFiledAsync(Guid filingId, string arn, DateTime filingDate)
        {
            // In a full implementation, this would update the gstr1_filings table
            return Task.FromResult(Result.Success());
        }

        /// <inheritdoc />
        public Task<Result<IEnumerable<Gstr1FilingDto>>> GetFilingHistoryAsync(Guid companyId, int? year = null)
        {
            // In a full implementation, this would query the gstr1_filings table
            return Task.FromResult(Result<IEnumerable<Gstr1FilingDto>>.Success(Enumerable.Empty<Gstr1FilingDto>()));
        }

        // ==================== Validation ====================

        /// <inheritdoc />
        public async Task<Result<Gstr1ValidationResultDto>> ValidateDataAsync(Guid companyId, string returnPeriod)
        {
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var invoices = (await GetInvoicesForPeriod(companyId, startDate, endDate)).ToList();

            var result = new Gstr1ValidationResultDto
            {
                TotalInvoices = invoices.Count
            };

            foreach (var invoice in invoices)
            {
                var validation = new Gstr1InvoiceValidationDto
                {
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                    IsValid = true
                };

                // Validate required fields
                if (string.IsNullOrEmpty(invoice.InvoiceNumber))
                {
                    validation.Errors.Add("Invoice number is missing");
                    validation.IsValid = false;
                }

                // For B2B, validate customer GSTIN
                if (invoice.InvoiceType == "domestic_b2b")
                {
                    var customer = await _customersRepository.GetByIdAsync(invoice.PartyId ?? Guid.Empty);
                    if (customer == null || string.IsNullOrEmpty(customer.Gstin))
                    {
                        validation.Errors.Add("B2B invoice requires customer GSTIN");
                        validation.IsValid = false;
                    }
                }

                // For exports, validate LUT
                if (!string.IsNullOrEmpty(invoice.Currency) && invoice.Currency != "INR")
                {
                    var hasLut = await _lutRepository.IsLutValidAsync(companyId, invoice.InvoiceDate);
                    if (!hasLut && invoice.TotalIgst == 0)
                    {
                        validation.Warnings.Add("Export invoice without LUT coverage");
                    }
                }

                // Check for e-invoice requirement
                if (invoice.InvoiceType == "domestic_b2b" && string.IsNullOrEmpty(invoice.EInvoiceIrn))
                {
                    validation.Warnings.Add("E-invoice (IRN) not generated for B2B invoice");
                }

                if (validation.IsValid)
                    result.ValidInvoices++;
                else
                    result.InvalidInvoices++;

                result.InvoiceValidations.Add(validation);
            }

            result.IsValid = result.InvalidInvoices == 0;

            if (result.InvalidInvoices > 0)
                result.Errors.Add($"{result.InvalidInvoices} invoices have validation errors");

            return Result<Gstr1ValidationResultDto>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<MissingInvoiceDto>>> GetMissingInvoicesAsync(Guid companyId, string returnPeriod)
        {
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var invoices = await GetInvoicesForPeriod(companyId, startDate, endDate);

            var missingInvoices = new List<MissingInvoiceDto>();

            foreach (var invoice in invoices.Where(i => i.Status == "draft"))
            {
                var customer = await _customersRepository.GetByIdAsync(invoice.PartyId ?? Guid.Empty);

                missingInvoices.Add(new MissingInvoiceDto
                {
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                    InvoiceDate = invoice.InvoiceDate,
                    TotalAmount = invoice.TotalAmount,
                    CustomerName = customer?.Name ?? "Unknown",
                    Reason = "Invoice is in draft status"
                });
            }

            return Result<IEnumerable<MissingInvoiceDto>>.Success(missingInvoices);
        }

        // ==================== Summary ====================

        /// <inheritdoc />
        public async Task<Result<Gstr1TaxSummaryDto>> GetTaxSummaryAsync(Guid companyId, string returnPeriod)
        {
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var invoices = (await GetInvoicesForPeriod(companyId, startDate, endDate)).ToList();

            var summary = new Gstr1TaxSummaryDto();

            // B2B taxable supplies
            var b2bInvoices = invoices.Where(i => i.Currency == "INR" && i.InvoiceType == "domestic_b2b").ToList();
            summary.OutwardTaxableB2bValue = b2bInvoices.Sum(i => i.Subtotal);

            // B2C taxable supplies
            var b2cInvoices = invoices.Where(i => i.Currency == "INR" && i.InvoiceType != "domestic_b2b").ToList();
            summary.OutwardTaxableB2cValue = b2cInvoices.Sum(i => i.Subtotal);

            summary.OutwardTaxableTotalValue = summary.OutwardTaxableB2bValue + summary.OutwardTaxableB2cValue;

            // Export invoices
            var exportInvoices = invoices.Where(i => !string.IsNullOrEmpty(i.Currency) && i.Currency != "INR").ToList();
            summary.ZeroRatedExportsWithTaxValue = exportInvoices.Where(i => i.TotalIgst > 0).Sum(i => i.Subtotal);
            summary.ZeroRatedExportsWithTaxIgst = exportInvoices.Where(i => i.TotalIgst > 0).Sum(i => i.TotalIgst);
            summary.ZeroRatedExportsWithoutTaxValue = exportInvoices.Where(i => i.TotalIgst == 0).Sum(i => i.Subtotal);

            // Tax totals
            summary.TotalIgst = invoices.Sum(i => i.TotalIgst);
            summary.TotalCgst = invoices.Sum(i => i.TotalCgst);
            summary.TotalSgst = invoices.Sum(i => i.TotalSgst);
            summary.TotalCess = invoices.Sum(i => i.TotalCess);

            return Result<Gstr1TaxSummaryDto>.Success(summary);
        }

        // ==================== Helper Methods ====================

        private async Task<IEnumerable<Core.Entities.Invoices>> GetInvoicesForPeriod(Guid companyId, DateOnly startDate, DateOnly endDate)
        {
            var filters = new Dictionary<string, object> { { "company_id", companyId } };
            var (allInvoices, _) = await _invoicesRepository.GetPagedAsync(1, int.MaxValue, null, null, false, filters);

            return allInvoices.Where(i =>
                i.InvoiceDate >= startDate &&
                i.InvoiceDate <= endDate &&
                i.Status != "draft" &&
                i.Status != "cancelled");
        }

        private async Task<Gstr1ExportDto> CreateExportDto(Core.Entities.Invoices invoice, Core.Entities.Customers? customer, string exportType, Guid companyId)
        {
            string? lutNumber = null;
            var lut = await _lutRepository.GetValidForDateAsync(companyId, invoice.InvoiceDate);
            if (lut != null)
                lutNumber = lut.LutNumber;

            return new Gstr1ExportDto
            {
                ExportType = exportType,
                InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                InvoiceDate = invoice.InvoiceDate,
                InvoiceValue = invoice.TotalAmount,
                PortCode = invoice.PortCode,
                ShippingBillNumber = invoice.ShippingBillNumber,
                ShippingBillDate = invoice.ShippingBillDate,
                Rate = GetPrimaryGstRate(invoice),
                TaxableValue = invoice.Subtotal,
                IgstAmount = invoice.TotalIgst,
                CessAmount = invoice.TotalCess,
                InvoiceId = invoice.Id,
                CustomerName = customer?.Name ?? string.Empty,
                CustomerCountry = customer?.Country,
                Currency = invoice.Currency ?? "USD",
                ForeignCurrencyValue = invoice.TotalAmount,
                LutNumber = lutNumber
            };
        }

        private static (DateOnly StartDate, DateOnly EndDate) ParseReturnPeriod(string returnPeriod)
        {
            // Format: MMYYYY
            if (returnPeriod.Length != 6)
                throw new ArgumentException("Return period must be in MMYYYY format");

            var month = int.Parse(returnPeriod[..2]);
            var year = int.Parse(returnPeriod[2..]);

            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            return (startDate, endDate);
        }

        private static decimal GetPrimaryGstRate(Core.Entities.Invoices invoice)
        {
            // Calculate effective GST rate
            if (invoice.Subtotal == 0) return 0;

            var totalGst = invoice.TotalIgst + invoice.TotalCgst + invoice.TotalSgst;
            return Math.Round(totalGst / invoice.Subtotal * 100, 2);
        }

        private static string GetStateCode(string? state)
        {
            // Returns GST state code (first 2 digits of GSTIN)
            // In a full implementation, this would map state names to codes
            return state switch
            {
                "Maharashtra" => "27",
                "Karnataka" => "29",
                "Tamil Nadu" => "33",
                "Delhi" => "07",
                "Gujarat" => "24",
                _ => "99"  // Other territory
            };
        }

        private static string GetHsnDescription(string hsnCode)
        {
            // In a full implementation, this would look up from a master table
            return hsnCode switch
            {
                "998314" => "IT and Software Services",
                "998313" => "IT Consulting Services",
                "998319" => "Other IT Services",
                _ => "Other Services"
            };
        }
    }
}

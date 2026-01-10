using Application.DTOs.Gst;
using Application.Interfaces.Gst;
using Core.Common;
using Core.Entities.Gst;
using Core.Interfaces;
using Core.Interfaces.Gst;
using System.Text.Json;

namespace Application.Services.Gst
{
    /// <summary>
    /// Service for GSTR-3B filing pack generation.
    /// Aggregates data from GSTR-1 (outward supplies), vendor invoices (ITC), and RCM transactions.
    /// </summary>
    public class Gstr3bService : IGstr3bService
    {
        private readonly IGstr3bRepository _gstr3bRepository;
        private readonly IGstr1Service _gstr1Service;
        private readonly IRcmTransactionRepository _rcmRepository;
        private readonly IVendorInvoicesRepository _vendorInvoicesRepository;
        private readonly ICompaniesRepository _companiesRepository;

        public Gstr3bService(
            IGstr3bRepository gstr3bRepository,
            IGstr1Service gstr1Service,
            IRcmTransactionRepository rcmRepository,
            IVendorInvoicesRepository vendorInvoicesRepository,
            ICompaniesRepository companiesRepository)
        {
            _gstr3bRepository = gstr3bRepository ?? throw new ArgumentNullException(nameof(gstr3bRepository));
            _gstr1Service = gstr1Service ?? throw new ArgumentNullException(nameof(gstr1Service));
            _rcmRepository = rcmRepository ?? throw new ArgumentNullException(nameof(rcmRepository));
            _vendorInvoicesRepository = vendorInvoicesRepository ?? throw new ArgumentNullException(nameof(vendorInvoicesRepository));
            _companiesRepository = companiesRepository ?? throw new ArgumentNullException(nameof(companiesRepository));
        }

        // ==================== Filing Generation ====================

        /// <inheritdoc />
        public async Task<Result<Gstr3bFilingDto>> GenerateFilingPackAsync(Guid companyId, string returnPeriod, Guid userId, bool regenerate = false)
        {
            // Validate company
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
                return Error.NotFound($"Company with ID {companyId} not found");

            // Check for existing filing
            var existingFiling = await _gstr3bRepository.GetByPeriodAsync(companyId, returnPeriod);
            if (existingFiling != null && !regenerate)
            {
                if (existingFiling.Status == Gstr3bFilingStatus.Filed)
                    return Error.Conflict($"GSTR-3B for {returnPeriod} is already filed");

                // Return existing filing if not regenerating
                return await GetFilingByIdAsync(existingFiling.Id);
            }

            // If regenerating, delete old line items and source docs
            if (existingFiling != null && regenerate)
            {
                await _gstr3bRepository.DeleteSourceDocumentsByFilingAsync(existingFiling.Id);
                await _gstr3bRepository.DeleteLineItemsByFilingAsync(existingFiling.Id);
            }

            // Build all tables
            var table31Result = await BuildTable31Async(companyId, returnPeriod);
            var table4Result = await BuildTable4Async(companyId, returnPeriod);
            var table5Result = await BuildTable5Async(companyId, returnPeriod);

            // Calculate variance
            var varianceResult = await GetVarianceAsync(companyId, returnPeriod);

            // Create or update filing
            var financialYear = GetFinancialYear(returnPeriod);
            var filing = existingFiling ?? new Gstr3bFiling
            {
                CompanyId = companyId,
                Gstin = company.Gstin ?? string.Empty,
                ReturnPeriod = returnPeriod,
                FinancialYear = financialYear
            };

            filing.Status = Gstr3bFilingStatus.Generated;
            filing.GeneratedAt = DateTime.UtcNow;
            filing.GeneratedBy = userId;
            filing.Table31Json = table31Result.IsSuccess ? JsonSerializer.Serialize(table31Result.Value) : null;
            filing.Table4Json = table4Result.IsSuccess ? JsonSerializer.Serialize(table4Result.Value) : null;
            filing.Table5Json = table5Result.IsSuccess ? JsonSerializer.Serialize(table5Result.Value) : null;
            filing.PreviousPeriodVarianceJson = varianceResult.IsSuccess ? JsonSerializer.Serialize(varianceResult.Value) : null;

            if (existingFiling != null)
            {
                await _gstr3bRepository.UpdateAsync(filing);
            }
            else
            {
                filing = await _gstr3bRepository.AddAsync(filing);
            }

            // Create line items with source documents
            await CreateLineItemsAsync(filing.Id, table31Result.Value, table4Result.Value, table5Result.Value, companyId, returnPeriod);

            return await GetFilingByIdAsync(filing.Id);
        }

        /// <inheritdoc />
        public async Task<Result<Gstr3bFilingDto>> GetFilingByIdAsync(Guid filingId)
        {
            var filing = await _gstr3bRepository.GetByIdAsync(filingId);
            if (filing == null)
                return Error.NotFound($"Filing with ID {filingId} not found");

            return Result<Gstr3bFilingDto>.Success(MapFilingToDto(filing));
        }

        /// <inheritdoc />
        public async Task<Result<Gstr3bFilingDto>> GetFilingByPeriodAsync(Guid companyId, string returnPeriod)
        {
            var filing = await _gstr3bRepository.GetByPeriodAsync(companyId, returnPeriod);
            if (filing == null)
                return Error.NotFound($"No GSTR-3B filing found for period {returnPeriod}");

            return Result<Gstr3bFilingDto>.Success(MapFilingToDto(filing));
        }

        // ==================== Table Builders ====================

        /// <inheritdoc />
        public async Task<Result<Gstr3bTable31Dto>> BuildTable31Async(Guid companyId, string returnPeriod)
        {
            var table = new Gstr3bTable31Dto();

            // Get GSTR-1 data for outward supplies
            var gstr1PeriodMmyyyy = ConvertToMmyyyyFormat(returnPeriod);
            var gstr1Result = await _gstr1Service.GetTaxSummaryAsync(companyId, gstr1PeriodMmyyyy);

            if (gstr1Result.IsSuccess)
            {
                var gstr1Data = gstr1Result.Value!;

                // 3.1(a) - Outward taxable supplies (other than zero/nil/exempt)
                table.OutwardTaxable = new Gstr3bRowDto
                {
                    TaxableValue = gstr1Data.OutwardTaxableTotalValue,
                    Igst = gstr1Data.TotalIgst,
                    Cgst = gstr1Data.TotalCgst,
                    Sgst = gstr1Data.TotalSgst,
                    Cess = gstr1Data.TotalCess
                };

                // 3.1(b) - Zero-rated supplies (exports)
                table.OutwardZeroRated = new Gstr3bRowDto
                {
                    TaxableValue = gstr1Data.ZeroRatedExportsWithTaxValue + gstr1Data.ZeroRatedExportsWithoutTaxValue,
                    Igst = gstr1Data.ZeroRatedExportsWithTaxIgst
                };
            }

            // 3.1(d) - RCM liability (inward supplies liable to reverse charge)
            var rcmSummary = await _rcmRepository.GetPeriodSummaryAsync(companyId, returnPeriod);
            table.InwardRcm = new Gstr3bRowDto
            {
                TaxableValue = rcmSummary.TotalTaxableValue,
                Igst = rcmSummary.TotalIgst,
                Cgst = rcmSummary.TotalCgst,
                Sgst = rcmSummary.TotalSgst,
                SourceCount = rcmSummary.TotalTransactions
            };

            return Result<Gstr3bTable31Dto>.Success(table);
        }

        /// <inheritdoc />
        public async Task<Result<Gstr3bTable4Dto>> BuildTable4Async(Guid companyId, string returnPeriod)
        {
            var table = new Gstr3bTable4Dto();
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);

            // Get vendor invoices for ITC
            var filters = new Dictionary<string, object> { { "company_id", companyId } };
            var (vendorInvoices, _) = await _vendorInvoicesRepository.GetPagedAsync(1, int.MaxValue, null, null, false, filters);

            var periodInvoices = vendorInvoices.Where(vi =>
                vi.InvoiceDate >= startDate &&
                vi.InvoiceDate <= endDate &&
                vi.Status != "draft" &&
                vi.Status != "cancelled").ToList();

            // 4(A)(5) - All other ITC (from vendor invoices, excluding RCM and imports)
            var regularItcInvoices = periodInvoices.Where(vi =>
                !vi.ReverseCharge &&
                vi.Currency == "INR").ToList();

            table.ItcAvailable.AllOtherItc = new Gstr3bItcRowDto
            {
                Igst = regularItcInvoices.Sum(vi => vi.TotalIgst),
                Cgst = regularItcInvoices.Sum(vi => vi.TotalCgst),
                Sgst = regularItcInvoices.Sum(vi => vi.TotalSgst),
                Cess = regularItcInvoices.Sum(vi => vi.TotalCess),
                SourceCount = regularItcInvoices.Count()
            };

            // 4(A)(2) - Import of services
            var importServiceInvoices = periodInvoices.Where(vi =>
                !string.IsNullOrEmpty(vi.Currency) &&
                vi.Currency != "INR").ToList();

            table.ItcAvailable.ImportServices = new Gstr3bItcRowDto
            {
                Igst = importServiceInvoices.Sum(vi => vi.TotalIgst),
                SourceCount = importServiceInvoices.Count()
            };

            // 4(A)(3) - RCM ITC claimed
            var rcmTransactions = await _rcmRepository.GetByCompanyAsync(companyId, returnPeriod);
            var rcmItcClaimed = rcmTransactions.Where(r => r.ItcClaimed && !r.ItcBlocked).ToList();

            table.ItcAvailable.RcmInward = new Gstr3bItcRowDto
            {
                Igst = rcmItcClaimed.Sum(r => r.IgstAmount),
                Cgst = rcmItcClaimed.Sum(r => r.CgstAmount),
                Sgst = rcmItcClaimed.Sum(r => r.SgstAmount),
                SourceCount = rcmItcClaimed.Count
            };

            // 4(D)(1) - Ineligible ITC (Section 17(5))
            var rcmBlocked = rcmTransactions.Where(r => r.ItcBlocked).ToList();
            table.ItcIneligible.Section17_5 = new Gstr3bItcRowDto
            {
                Igst = rcmBlocked.Sum(r => r.IgstAmount),
                Cgst = rcmBlocked.Sum(r => r.CgstAmount),
                Sgst = rcmBlocked.Sum(r => r.SgstAmount),
                SourceCount = rcmBlocked.Count
            };

            return Result<Gstr3bTable4Dto>.Success(table);
        }

        /// <inheritdoc />
        public async Task<Result<Gstr3bTable5Dto>> BuildTable5Async(Guid companyId, string returnPeriod)
        {
            // Table 5 - Exempt, nil-rated, non-GST inward supplies
            // This would typically come from vendor invoices marked as exempt or nil-rated
            var table = new Gstr3bTable5Dto
            {
                InterStateSupplies = new Gstr3bExemptRowDto(),
                IntraStateSupplies = new Gstr3bExemptRowDto()
            };

            return await Task.FromResult(Result<Gstr3bTable5Dto>.Success(table));
        }

        // ==================== Drill-down ====================

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Gstr3bLineItemDto>>> GetLineItemsAsync(Guid filingId, string? tableCode = null)
        {
            var lineItems = string.IsNullOrEmpty(tableCode)
                ? await _gstr3bRepository.GetLineItemsAsync(filingId)
                : await _gstr3bRepository.GetLineItemsByTableAsync(filingId, tableCode);

            var dtos = lineItems.Select(li => new Gstr3bLineItemDto
            {
                Id = li.Id,
                TableCode = li.TableCode,
                RowOrder = li.RowOrder,
                Description = li.Description,
                TaxableValue = li.TaxableValue,
                Igst = li.Igst,
                Cgst = li.Cgst,
                Sgst = li.Sgst,
                Cess = li.Cess,
                SourceCount = li.SourceCount,
                SourceType = li.SourceType,
                ComputationNotes = li.ComputationNotes
            });

            return Result<IEnumerable<Gstr3bLineItemDto>>.Success(dtos);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<Gstr3bSourceDocumentDto>>> GetSourceDocumentsAsync(Guid lineItemId)
        {
            var docs = await _gstr3bRepository.GetSourceDocumentsAsync(lineItemId);

            var dtos = docs.Select(d => new Gstr3bSourceDocumentDto
            {
                Id = d.Id,
                SourceType = d.SourceType,
                SourceId = d.SourceId,
                SourceNumber = d.SourceNumber,
                SourceDate = d.SourceDate,
                TaxableValue = d.TaxableValue,
                Igst = d.Igst,
                Cgst = d.Cgst,
                Sgst = d.Sgst,
                Cess = d.Cess,
                PartyName = d.PartyName,
                PartyGstin = d.PartyGstin
            });

            return Result<IEnumerable<Gstr3bSourceDocumentDto>>.Success(dtos);
        }

        // ==================== Variance ====================

        /// <inheritdoc />
        public async Task<Result<Gstr3bVarianceSummaryDto>> GetVarianceAsync(Guid companyId, string currentPeriod)
        {
            var previousFiling = await _gstr3bRepository.GetPreviousPeriodFilingAsync(companyId, currentPeriod);
            if (previousFiling == null)
            {
                return Result<Gstr3bVarianceSummaryDto>.Success(new Gstr3bVarianceSummaryDto
                {
                    PreviousPeriod = "N/A (First filing)",
                    Items = new List<Gstr3bVarianceItemDto>()
                });
            }

            var variance = new Gstr3bVarianceSummaryDto
            {
                PreviousPeriod = previousFiling.ReturnPeriod,
                Items = new List<Gstr3bVarianceItemDto>()
            };

            // Compare Table 3.1 totals
            if (!string.IsNullOrEmpty(previousFiling.Table31Json))
            {
                var prevTable31 = JsonSerializer.Deserialize<Gstr3bTable31Dto>(previousFiling.Table31Json);
                if (prevTable31 != null)
                {
                    // Current values would be from the newly built tables
                    // For variance, we just note the previous values
                    variance.Items.Add(new Gstr3bVarianceItemDto
                    {
                        Field = "Outward Taxable Supplies",
                        TableCode = "3.1(a)",
                        PreviousValue = prevTable31.OutwardTaxable.TaxableValue
                    });
                }
            }

            // Compare Table 4 totals
            if (!string.IsNullOrEmpty(previousFiling.Table4Json))
            {
                var prevTable4 = JsonSerializer.Deserialize<Gstr3bTable4Dto>(previousFiling.Table4Json);
                if (prevTable4 != null)
                {
                    variance.Items.Add(new Gstr3bVarianceItemDto
                    {
                        Field = "Total ITC Available",
                        TableCode = "4(A)",
                        PreviousValue = prevTable4.ItcAvailable.Total.TotalItc
                    });
                }
            }

            return Result<Gstr3bVarianceSummaryDto>.Success(variance);
        }

        // ==================== Filing Workflow ====================

        /// <inheritdoc />
        public async Task<Result> MarkAsReviewedAsync(Guid filingId, Guid userId, string? notes = null)
        {
            var filing = await _gstr3bRepository.GetByIdAsync(filingId);
            if (filing == null)
                return Error.NotFound($"Filing with ID {filingId} not found");

            if (filing.Status != Gstr3bFilingStatus.Generated)
                return Error.Validation($"Filing must be in 'generated' status to be reviewed");

            await _gstr3bRepository.MarkAsReviewedAsync(filingId, userId);

            if (!string.IsNullOrEmpty(notes))
            {
                filing.Notes = notes;
                await _gstr3bRepository.UpdateAsync(filing);
            }

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> MarkAsFiledAsync(Guid filingId, string arn, DateTime filingDate, Guid userId)
        {
            var filing = await _gstr3bRepository.GetByIdAsync(filingId);
            if (filing == null)
                return Error.NotFound($"Filing with ID {filingId} not found");

            if (filing.Status == Gstr3bFilingStatus.Filed)
                return Error.Conflict("Filing is already marked as filed");

            await _gstr3bRepository.MarkAsFiledAsync(filingId, arn, filingDate, userId);

            return Result.Success();
        }

        // ==================== History ====================

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<Gstr3bFilingHistoryDto> Items, int TotalCount)>> GetFilingHistoryAsync(
            Guid companyId,
            int pageNumber = 1,
            int pageSize = 12,
            string? financialYear = null,
            string? status = null)
        {
            var (filings, totalCount) = await _gstr3bRepository.GetFilingHistoryAsync(
                companyId, pageNumber, pageSize, financialYear, status);

            var dtos = filings.Select(f => new Gstr3bFilingHistoryDto
            {
                Id = f.Id,
                ReturnPeriod = f.ReturnPeriod,
                FinancialYear = f.FinancialYear,
                Status = f.Status,
                GeneratedAt = f.GeneratedAt,
                FiledAt = f.FiledAt,
                Arn = f.Arn
            });

            return Result<(IEnumerable<Gstr3bFilingHistoryDto>, int)>.Success((dtos, totalCount));
        }

        // ==================== Export ====================

        /// <inheritdoc />
        public async Task<Result<string>> ExportToJsonAsync(Guid filingId)
        {
            var filing = await _gstr3bRepository.GetByIdAsync(filingId);
            if (filing == null)
                return Error.NotFound($"Filing with ID {filingId} not found");

            var exportData = new
            {
                gstin = filing.Gstin,
                ret_period = filing.ReturnPeriod,
                table_3_1 = filing.Table31Json != null ? JsonDocument.Parse(filing.Table31Json).RootElement : default,
                table_4 = filing.Table4Json != null ? JsonDocument.Parse(filing.Table4Json).RootElement : default,
                table_5 = filing.Table5Json != null ? JsonDocument.Parse(filing.Table5Json).RootElement : default
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
            return Result<string>.Success(json);
        }

        // ==================== Private Helpers ====================

        private async Task CreateLineItemsAsync(
            Guid filingId,
            Gstr3bTable31Dto? table31,
            Gstr3bTable4Dto? table4,
            Gstr3bTable5Dto? table5,
            Guid companyId,
            string returnPeriod)
        {
            var lineItems = new List<Gstr3bLineItem>();

            // Table 3.1 line items
            if (table31 != null)
            {
                lineItems.Add(CreateLineItem(filingId, Gstr3bTableCodes.Table31a, 1,
                    "Outward taxable supplies (other than zero rated, nil rated and exempted)",
                    table31.OutwardTaxable, Gstr3bSourceTypes.Invoice));

                lineItems.Add(CreateLineItem(filingId, Gstr3bTableCodes.Table31b, 2,
                    "Outward taxable supplies (zero rated)",
                    table31.OutwardZeroRated, Gstr3bSourceTypes.Invoice));

                lineItems.Add(CreateLineItem(filingId, Gstr3bTableCodes.Table31c, 3,
                    "Other outward supplies (nil rated, exempted)",
                    table31.OtherOutward, Gstr3bSourceTypes.Invoice));

                lineItems.Add(CreateLineItem(filingId, Gstr3bTableCodes.Table31d, 4,
                    "Inward supplies (liable to reverse charge)",
                    table31.InwardRcm, Gstr3bSourceTypes.RcmTransaction));

                lineItems.Add(CreateLineItem(filingId, Gstr3bTableCodes.Table31e, 5,
                    "Non-GST outward supplies",
                    table31.NonGst, Gstr3bSourceTypes.Invoice));
            }

            // Table 4 line items (ITC)
            if (table4 != null)
            {
                lineItems.Add(CreateItcLineItem(filingId, Gstr3bTableCodes.Table4A1, 1,
                    "Import of goods", table4.ItcAvailable.ImportGoods, Gstr3bSourceTypes.VendorInvoice));

                lineItems.Add(CreateItcLineItem(filingId, Gstr3bTableCodes.Table4A2, 2,
                    "Import of services", table4.ItcAvailable.ImportServices, Gstr3bSourceTypes.VendorInvoice));

                lineItems.Add(CreateItcLineItem(filingId, Gstr3bTableCodes.Table4A3, 3,
                    "Inward supplies liable to reverse charge",
                    table4.ItcAvailable.RcmInward, Gstr3bSourceTypes.RcmTransaction));

                lineItems.Add(CreateItcLineItem(filingId, Gstr3bTableCodes.Table4A4, 4,
                    "Inward supplies from ISD", table4.ItcAvailable.IsdInward, Gstr3bSourceTypes.VendorInvoice));

                lineItems.Add(CreateItcLineItem(filingId, Gstr3bTableCodes.Table4A5, 5,
                    "All other ITC", table4.ItcAvailable.AllOtherItc, Gstr3bSourceTypes.VendorInvoice));

                lineItems.Add(CreateItcLineItem(filingId, Gstr3bTableCodes.Table4D1, 10,
                    "Ineligible ITC as per section 17(5)",
                    table4.ItcIneligible.Section17_5, Gstr3bSourceTypes.VendorInvoice));
            }

            await _gstr3bRepository.BulkInsertLineItemsAsync(lineItems);

            // Create source documents for RCM line item (example)
            var rcmLineItem = lineItems.FirstOrDefault(li => li.TableCode == Gstr3bTableCodes.Table31d);
            if (rcmLineItem != null)
            {
                var rcmTransactions = await _rcmRepository.GetByCompanyAsync(companyId, returnPeriod);
                var sourceDocs = rcmTransactions.Select(r => new Gstr3bSourceDocument
                {
                    LineItemId = rcmLineItem.Id,
                    SourceType = Gstr3bSourceTypes.RcmTransaction,
                    SourceId = r.Id,
                    SourceNumber = r.VendorInvoiceNumber,
                    SourceDate = r.VendorInvoiceDate,
                    TaxableValue = r.TaxableValue,
                    Igst = r.IgstAmount,
                    Cgst = r.CgstAmount,
                    Sgst = r.SgstAmount,
                    Cess = r.CessAmount,
                    PartyName = r.VendorName,
                    PartyGstin = r.VendorGstin
                });

                await _gstr3bRepository.BulkInsertSourceDocumentsAsync(sourceDocs);
            }
        }

        private static Gstr3bLineItem CreateLineItem(Guid filingId, string tableCode, int order,
            string description, Gstr3bRowDto row, string sourceType)
        {
            return new Gstr3bLineItem
            {
                Id = Guid.NewGuid(),
                FilingId = filingId,
                TableCode = tableCode,
                RowOrder = order,
                Description = description,
                TaxableValue = row.TaxableValue,
                Igst = row.Igst,
                Cgst = row.Cgst,
                Sgst = row.Sgst,
                Cess = row.Cess,
                SourceCount = row.SourceCount,
                SourceType = sourceType,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Gstr3bLineItem CreateItcLineItem(Guid filingId, string tableCode, int order,
            string description, Gstr3bItcRowDto row, string sourceType)
        {
            return new Gstr3bLineItem
            {
                Id = Guid.NewGuid(),
                FilingId = filingId,
                TableCode = tableCode,
                RowOrder = order,
                Description = description,
                TaxableValue = 0,
                Igst = row.Igst,
                Cgst = row.Cgst,
                Sgst = row.Sgst,
                Cess = row.Cess,
                SourceCount = row.SourceCount,
                SourceType = sourceType,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Gstr3bFilingDto MapFilingToDto(Gstr3bFiling filing)
        {
            var dto = new Gstr3bFilingDto
            {
                Id = filing.Id,
                CompanyId = filing.CompanyId,
                Gstin = filing.Gstin,
                ReturnPeriod = filing.ReturnPeriod,
                FinancialYear = filing.FinancialYear,
                Status = filing.Status,
                GeneratedAt = filing.GeneratedAt,
                GeneratedBy = filing.GeneratedBy,
                ReviewedAt = filing.ReviewedAt,
                ReviewedBy = filing.ReviewedBy,
                FiledAt = filing.FiledAt,
                FiledBy = filing.FiledBy,
                Arn = filing.Arn,
                FilingDate = filing.FilingDate,
                Notes = filing.Notes,
                CreatedAt = filing.CreatedAt,
                UpdatedAt = filing.UpdatedAt
            };

            // Deserialize table JSONs
            if (!string.IsNullOrEmpty(filing.Table31Json))
                dto.Table31 = JsonSerializer.Deserialize<Gstr3bTable31Dto>(filing.Table31Json);
            if (!string.IsNullOrEmpty(filing.Table4Json))
                dto.Table4 = JsonSerializer.Deserialize<Gstr3bTable4Dto>(filing.Table4Json);
            if (!string.IsNullOrEmpty(filing.Table5Json))
                dto.Table5 = JsonSerializer.Deserialize<Gstr3bTable5Dto>(filing.Table5Json);
            if (!string.IsNullOrEmpty(filing.PreviousPeriodVarianceJson))
                dto.Variance = JsonSerializer.Deserialize<Gstr3bVarianceSummaryDto>(filing.PreviousPeriodVarianceJson);

            return dto;
        }

        private static string GetFinancialYear(string returnPeriod)
        {
            // returnPeriod is in 'Jan-2025' format
            var parts = returnPeriod.Split('-');
            if (parts.Length != 2) return returnPeriod;

            var month = parts[0];
            var year = int.Parse(parts[1]);

            // Indian FY: April to March
            var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Jan"] = 1, ["Feb"] = 2, ["Mar"] = 3, ["Apr"] = 4,
                ["May"] = 5, ["Jun"] = 6, ["Jul"] = 7, ["Aug"] = 8,
                ["Sep"] = 9, ["Oct"] = 10, ["Nov"] = 11, ["Dec"] = 12
            };

            if (monthMap.TryGetValue(month, out var monthNum))
            {
                if (monthNum >= 4) // Apr-Dec belongs to FY starting that year
                    return $"{year}-{(year + 1) % 100:D2}";
                else // Jan-Mar belongs to FY starting previous year
                    return $"{year - 1}-{year % 100:D2}";
            }

            return returnPeriod;
        }

        private static string ConvertToMmyyyyFormat(string returnPeriod)
        {
            // Convert 'Jan-2025' to 'MMYYYY' format (012025)
            var parts = returnPeriod.Split('-');
            if (parts.Length != 2) return returnPeriod;

            var monthMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Jan"] = "01", ["Feb"] = "02", ["Mar"] = "03", ["Apr"] = "04",
                ["May"] = "05", ["Jun"] = "06", ["Jul"] = "07", ["Aug"] = "08",
                ["Sep"] = "09", ["Oct"] = "10", ["Nov"] = "11", ["Dec"] = "12"
            };

            if (monthMap.TryGetValue(parts[0], out var monthNum))
                return $"{monthNum}{parts[1]}";

            return returnPeriod;
        }

        private static (DateOnly StartDate, DateOnly EndDate) ParseReturnPeriod(string returnPeriod)
        {
            // Format: 'Jan-2025'
            var parts = returnPeriod.Split('-');
            if (parts.Length != 2)
                throw new ArgumentException("Return period must be in 'MMM-YYYY' format");

            var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Jan"] = 1, ["Feb"] = 2, ["Mar"] = 3, ["Apr"] = 4,
                ["May"] = 5, ["Jun"] = 6, ["Jul"] = 7, ["Aug"] = 8,
                ["Sep"] = 9, ["Oct"] = 10, ["Nov"] = 11, ["Dec"] = 12
            };

            if (!monthMap.TryGetValue(parts[0], out var month))
                throw new ArgumentException($"Invalid month: {parts[0]}");

            var year = int.Parse(parts[1]);
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            return (startDate, endDate);
        }
    }
}

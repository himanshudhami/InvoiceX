using Application.DTOs.Gst;
using Application.Interfaces.Gst;
using Core.Common;
using Core.Entities.Gst;
using Core.Interfaces;
using Core.Interfaces.Gst;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace Application.Services.Gst
{
    /// <summary>
    /// Service for GSTR-2B ingestion and reconciliation
    /// </summary>
    public class Gstr2bService : IGstr2bService
    {
        private readonly IGstr2bRepository _gstr2bRepository;
        private readonly IVendorInvoicesRepository _vendorInvoicesRepository;
        private readonly ICompaniesRepository _companiesRepository;

        public Gstr2bService(
            IGstr2bRepository gstr2bRepository,
            IVendorInvoicesRepository vendorInvoicesRepository,
            ICompaniesRepository companiesRepository)
        {
            _gstr2bRepository = gstr2bRepository ?? throw new ArgumentNullException(nameof(gstr2bRepository));
            _vendorInvoicesRepository = vendorInvoicesRepository ?? throw new ArgumentNullException(nameof(vendorInvoicesRepository));
            _companiesRepository = companiesRepository ?? throw new ArgumentNullException(nameof(companiesRepository));
        }

        // ==================== Import ====================

        public async Task<Result<Gstr2bImportDto>> ImportGstr2bAsync(
            Guid companyId, string returnPeriod, string jsonData, string? fileName, Guid userId)
        {
            // Validate company
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
                return Error.NotFound($"Company with ID {companyId} not found");

            // Calculate file hash for deduplication
            var fileHash = ComputeHash(jsonData);

            // Check for duplicate import
            var existingImport = await _gstr2bRepository.GetImportByHashAsync(companyId, returnPeriod, fileHash);
            if (existingImport != null)
                return Error.Conflict($"This GSTR-2B file has already been imported for {returnPeriod}");

            // Parse JSON
            Gstr2bJsonRoot? gstr2bData;
            try
            {
                gstr2bData = JsonSerializer.Deserialize<Gstr2bJsonRoot>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                return Error.Validation($"Invalid GSTR-2B JSON format: {ex.Message}");
            }

            if (gstr2bData == null)
                return Error.Validation("Failed to parse GSTR-2B JSON data");

            // Create import record
            var import = new Gstr2bImport
            {
                CompanyId = companyId,
                ReturnPeriod = returnPeriod,
                Gstin = company.Gstin ?? gstr2bData.Gstin ?? string.Empty,
                ImportSource = Gstr2bImportSource.FileUpload,
                FileName = fileName,
                FileHash = fileHash,
                ImportStatus = Gstr2bImportStatus.Processing,
                RawJson = jsonData,
                ImportedBy = userId
            };

            import = await _gstr2bRepository.AddImportAsync(import);

            try
            {
                // Parse and insert invoices
                var invoices = ParseGstr2bInvoices(gstr2bData, import.Id, companyId, returnPeriod);
                await _gstr2bRepository.BulkInsertInvoicesAsync(invoices);

                // Calculate ITC totals
                var totalItcIgst = invoices.Sum(i => i.ItcIgst);
                var totalItcCgst = invoices.Sum(i => i.ItcCgst);
                var totalItcSgst = invoices.Sum(i => i.ItcSgst);
                var totalItcCess = invoices.Sum(i => i.ItcCess);

                // Update import with summary
                import.TotalInvoices = invoices.Count;
                import.TotalItcIgst = totalItcIgst;
                import.TotalItcCgst = totalItcCgst;
                import.TotalItcSgst = totalItcSgst;
                import.TotalItcCess = totalItcCess;
                import.ImportStatus = Gstr2bImportStatus.Completed;
                import.ProcessedAt = DateTime.UtcNow;
                await _gstr2bRepository.UpdateImportAsync(import);

                return Result<Gstr2bImportDto>.Success(MapImportToDto(import));
            }
            catch (Exception ex)
            {
                // Mark import as failed
                await _gstr2bRepository.UpdateImportStatusAsync(import.Id, Gstr2bImportStatus.Failed, ex.Message);
                return Error.Internal($"Failed to process GSTR-2B data: {ex.Message}");
            }
        }

        public async Task<Result<Gstr2bImportDto>> GetImportByIdAsync(Guid importId)
        {
            var import = await _gstr2bRepository.GetImportByIdAsync(importId);
            if (import == null)
                return Error.NotFound($"Import with ID {importId} not found");

            return Result<Gstr2bImportDto>.Success(MapImportToDto(import));
        }

        public async Task<Result<Gstr2bImportDto>> GetImportByPeriodAsync(Guid companyId, string returnPeriod)
        {
            var import = await _gstr2bRepository.GetImportByPeriodAsync(companyId, returnPeriod);
            if (import == null)
                return Error.NotFound($"No GSTR-2B import found for period {returnPeriod}");

            return Result<Gstr2bImportDto>.Success(MapImportToDto(import));
        }

        public async Task<Result<(IEnumerable<Gstr2bImportDto> Items, int TotalCount)>> GetImportsAsync(
            Guid companyId, int pageNumber = 1, int pageSize = 12, string? status = null)
        {
            var (imports, totalCount) = await _gstr2bRepository.GetImportsPagedAsync(companyId, pageNumber, pageSize, status);
            var dtos = imports.Select(MapImportToDto);
            return Result<(IEnumerable<Gstr2bImportDto>, int)>.Success((dtos, totalCount));
        }

        public async Task<Result> DeleteImportAsync(Guid importId)
        {
            var import = await _gstr2bRepository.GetImportByIdAsync(importId);
            if (import == null)
                return Error.NotFound($"Import with ID {importId} not found");

            await _gstr2bRepository.DeleteInvoicesByImportAsync(importId);
            await _gstr2bRepository.DeleteImportAsync(importId);

            return Result.Success();
        }

        // ==================== Reconciliation ====================

        public async Task<Result<Gstr2bReconciliationSummaryDto>> RunReconciliationAsync(Guid importId, bool force = false)
        {
            var import = await _gstr2bRepository.GetImportByIdAsync(importId);
            if (import == null)
                return Error.NotFound($"Import with ID {importId} not found");

            // Get invoices to reconcile
            var gstr2bInvoices = await _gstr2bRepository.GetInvoicesByImportAsync(importId);
            var invoiceList = gstr2bInvoices.ToList();

            if (!invoiceList.Any())
                return Error.Validation("No invoices found in this import");

            // Get reconciliation rules
            var rules = await _gstr2bRepository.GetReconciliationRulesAsync(import.CompanyId);
            var rulesList = rules.OrderBy(r => r.Priority).ToList();

            // Get vendor invoices for the period
            var (startDate, endDate) = ParseReturnPeriod(import.ReturnPeriod);
            var filters = new Dictionary<string, object> { { "company_id", import.CompanyId } };
            var (vendorInvoices, _) = await _vendorInvoicesRepository.GetPagedAsync(1, int.MaxValue, null, null, false, filters);

            var vendorInvoiceList = vendorInvoices
                .Where(vi => vi.InvoiceDate >= startDate.AddMonths(-3) && vi.InvoiceDate <= endDate)
                .ToList();

            int matched = 0, partial = 0, unmatched = 0;

            foreach (var gstr2bInvoice in invoiceList)
            {
                // Skip if already processed and not forcing
                if (!force && gstr2bInvoice.MatchStatus != Gstr2bMatchStatus.Pending)
                {
                    if (gstr2bInvoice.MatchStatus == Gstr2bMatchStatus.Matched) matched++;
                    else if (gstr2bInvoice.MatchStatus == Gstr2bMatchStatus.PartialMatch) partial++;
                    else if (gstr2bInvoice.MatchStatus == Gstr2bMatchStatus.Unmatched) unmatched++;
                    continue;
                }

                // Try to match using rules
                var matchResult = TryMatchInvoice(gstr2bInvoice, vendorInvoiceList, rulesList);

                if (matchResult.IsMatch)
                {
                    var discrepancies = matchResult.Discrepancies.Any()
                        ? JsonSerializer.Serialize(matchResult.Discrepancies)
                        : null;

                    var matchDetails = JsonSerializer.Serialize(matchResult.Details);

                    var status = matchResult.Discrepancies.Any()
                        ? Gstr2bMatchStatus.PartialMatch
                        : Gstr2bMatchStatus.Matched;

                    await _gstr2bRepository.UpdateInvoiceMatchAsync(
                        gstr2bInvoice.Id, status, matchResult.MatchedVendorInvoiceId,
                        matchResult.ConfidenceScore, matchDetails, discrepancies);

                    if (status == Gstr2bMatchStatus.Matched) matched++;
                    else partial++;
                }
                else
                {
                    await _gstr2bRepository.UpdateInvoiceMatchAsync(
                        gstr2bInvoice.Id, Gstr2bMatchStatus.Unmatched, null, 0, null, null);
                    unmatched++;
                }
            }

            // Update import summary
            await _gstr2bRepository.UpdateImportSummaryAsync(importId, invoiceList.Count, matched, unmatched, partial);

            return await GetReconciliationSummaryAsync(import.CompanyId, import.ReturnPeriod);
        }

        public async Task<Result<Gstr2bReconciliationSummaryDto>> GetReconciliationSummaryAsync(Guid companyId, string returnPeriod)
        {
            var summary = await _gstr2bRepository.GetReconciliationSummaryAsync(companyId, returnPeriod);

            return Result<Gstr2bReconciliationSummaryDto>.Success(new Gstr2bReconciliationSummaryDto
            {
                ReturnPeriod = summary.ReturnPeriod,
                TotalInvoices = summary.TotalInvoices,
                MatchedInvoices = summary.MatchedInvoices,
                PartialMatchInvoices = summary.PartialMatchInvoices,
                UnmatchedInvoices = summary.UnmatchedInvoices,
                AcceptedInvoices = summary.AcceptedInvoices,
                RejectedInvoices = summary.RejectedInvoices,
                PendingReviewInvoices = summary.PendingReviewInvoices,
                MatchPercentage = summary.MatchPercentage,
                TotalTaxableValue = summary.TotalTaxableValue,
                MatchedTaxableValue = summary.MatchedTaxableValue,
                UnmatchedTaxableValue = summary.UnmatchedTaxableValue,
                TotalItcAvailable = summary.TotalItcAvailable,
                MatchedItc = summary.MatchedItc,
                UnmatchedItc = summary.UnmatchedItc
            });
        }

        public async Task<Result<IEnumerable<Gstr2bSupplierSummaryDto>>> GetSupplierSummaryAsync(Guid companyId, string returnPeriod)
        {
            var summaries = await _gstr2bRepository.GetSupplierWiseSummaryAsync(companyId, returnPeriod);

            var dtos = summaries.Select(s => new Gstr2bSupplierSummaryDto
            {
                SupplierGstin = s.SupplierGstin,
                SupplierName = s.SupplierName,
                InvoiceCount = s.InvoiceCount,
                MatchedCount = s.MatchedCount,
                UnmatchedCount = s.UnmatchedCount,
                TotalTaxableValue = s.TotalTaxableValue,
                TotalItc = s.TotalItc
            });

            return Result<IEnumerable<Gstr2bSupplierSummaryDto>>.Success(dtos);
        }

        public async Task<Result<Gstr2bItcComparisonDto>> GetItcComparisonAsync(Guid companyId, string returnPeriod)
        {
            var gstr2bItc = await _gstr2bRepository.GetItcSummaryAsync(companyId, returnPeriod);

            // Get books ITC from vendor invoices
            var (startDate, endDate) = ParseReturnPeriod(returnPeriod);
            var filters = new Dictionary<string, object> { { "company_id", companyId } };
            var (vendorInvoices, _) = await _vendorInvoicesRepository.GetPagedAsync(1, int.MaxValue, null, null, false, filters);

            var periodInvoices = vendorInvoices
                .Where(vi => vi.InvoiceDate >= startDate && vi.InvoiceDate <= endDate && vi.ItcEligible)
                .ToList();

            var booksItcIgst = periodInvoices.Sum(vi => vi.TotalIgst);
            var booksItcCgst = periodInvoices.Sum(vi => vi.TotalCgst);
            var booksItcSgst = periodInvoices.Sum(vi => vi.TotalSgst);
            var booksItcCess = periodInvoices.Sum(vi => vi.TotalCess);
            var booksItcTotal = booksItcIgst + booksItcCgst + booksItcSgst + booksItcCess;

            return Result<Gstr2bItcComparisonDto>.Success(new Gstr2bItcComparisonDto
            {
                ReturnPeriod = returnPeriod,
                Gstr2b = new Gstr2bItcBreakdownDto
                {
                    Igst = gstr2bItc.Gstr2bItcIgst,
                    Cgst = gstr2bItc.Gstr2bItcCgst,
                    Sgst = gstr2bItc.Gstr2bItcSgst,
                    Cess = gstr2bItc.Gstr2bItcCess,
                    Total = gstr2bItc.Gstr2bItcTotal
                },
                Books = new Gstr2bItcBreakdownDto
                {
                    Igst = booksItcIgst,
                    Cgst = booksItcCgst,
                    Sgst = booksItcSgst,
                    Cess = booksItcCess,
                    Total = booksItcTotal
                },
                Difference = new Gstr2bItcBreakdownDto
                {
                    Igst = gstr2bItc.Gstr2bItcIgst - booksItcIgst,
                    Cgst = gstr2bItc.Gstr2bItcCgst - booksItcCgst,
                    Sgst = gstr2bItc.Gstr2bItcSgst - booksItcSgst,
                    Cess = gstr2bItc.Gstr2bItcCess - booksItcCess,
                    Total = gstr2bItc.Gstr2bItcTotal - booksItcTotal
                }
            });
        }

        // ==================== Invoices ====================

        public async Task<Result<(IEnumerable<Gstr2bInvoiceListItemDto> Items, int TotalCount)>> GetInvoicesAsync(
            Guid importId, int pageNumber = 1, int pageSize = 50,
            string? matchStatus = null, string? invoiceType = null, string? searchTerm = null)
        {
            var (invoices, totalCount) = await _gstr2bRepository.GetInvoicesPagedAsync(
                importId, pageNumber, pageSize, matchStatus, invoiceType, searchTerm);

            var dtos = invoices.Select(i => new Gstr2bInvoiceListItemDto
            {
                Id = i.Id,
                SupplierGstin = i.SupplierGstin,
                SupplierName = i.SupplierName,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                InvoiceType = i.InvoiceType,
                TaxableValue = i.TaxableValue,
                TotalItc = i.ItcIgst + i.ItcCgst + i.ItcSgst + i.ItcCess,
                MatchStatus = i.MatchStatus,
                MatchConfidence = i.MatchConfidence,
                ActionStatus = i.ActionStatus
            });

            return Result<(IEnumerable<Gstr2bInvoiceListItemDto>, int)>.Success((dtos, totalCount));
        }

        public async Task<Result<Gstr2bInvoiceDto>> GetInvoiceByIdAsync(Guid invoiceId)
        {
            var invoice = await _gstr2bRepository.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
                return Error.NotFound($"Invoice with ID {invoiceId} not found");

            return Result<Gstr2bInvoiceDto>.Success(MapInvoiceToDto(invoice));
        }

        public async Task<Result<IEnumerable<Gstr2bInvoiceListItemDto>>> GetUnmatchedInvoicesAsync(Guid companyId, string returnPeriod)
        {
            var invoices = await _gstr2bRepository.GetUnmatchedInvoicesAsync(companyId, returnPeriod);

            var dtos = invoices.Select(i => new Gstr2bInvoiceListItemDto
            {
                Id = i.Id,
                SupplierGstin = i.SupplierGstin,
                SupplierName = i.SupplierName,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                InvoiceType = i.InvoiceType,
                TaxableValue = i.TaxableValue,
                TotalItc = i.ItcIgst + i.ItcCgst + i.ItcSgst + i.ItcCess,
                MatchStatus = i.MatchStatus,
                ActionStatus = i.ActionStatus
            });

            return Result<IEnumerable<Gstr2bInvoiceListItemDto>>.Success(dtos);
        }

        // ==================== Actions ====================

        public async Task<Result> AcceptMismatchAsync(Guid invoiceId, Guid userId, string? notes = null)
        {
            var invoice = await _gstr2bRepository.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
                return Error.NotFound($"Invoice with ID {invoiceId} not found");

            await _gstr2bRepository.UpdateInvoiceActionAsync(invoiceId, Gstr2bActionStatus.Accepted, userId, notes);
            return Result.Success();
        }

        public async Task<Result> RejectInvoiceAsync(Guid invoiceId, Guid userId, string reason)
        {
            var invoice = await _gstr2bRepository.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
                return Error.NotFound($"Invoice with ID {invoiceId} not found");

            await _gstr2bRepository.UpdateInvoiceActionAsync(invoiceId, Gstr2bActionStatus.Rejected, userId, reason);
            return Result.Success();
        }

        public async Task<Result> ManualMatchAsync(Guid gstr2bInvoiceId, Guid vendorInvoiceId, Guid userId, string? notes = null)
        {
            var gstr2bInvoice = await _gstr2bRepository.GetInvoiceByIdAsync(gstr2bInvoiceId);
            if (gstr2bInvoice == null)
                return Error.NotFound($"GSTR-2B Invoice with ID {gstr2bInvoiceId} not found");

            var vendorInvoice = await _vendorInvoicesRepository.GetByIdAsync(vendorInvoiceId);
            if (vendorInvoice == null)
                return Error.NotFound($"Vendor Invoice with ID {vendorInvoiceId} not found");

            // Check for discrepancies
            var discrepancies = new List<string>();
            if (gstr2bInvoice.TaxableValue != vendorInvoice.Subtotal)
                discrepancies.Add($"Taxable value mismatch: 2B={gstr2bInvoice.TaxableValue}, Books={vendorInvoice.Subtotal}");

            var totalGst2b = gstr2bInvoice.IgstAmount + gstr2bInvoice.CgstAmount + gstr2bInvoice.SgstAmount + gstr2bInvoice.CessAmount;
            var totalGstBooks = vendorInvoice.TotalIgst + vendorInvoice.TotalCgst + vendorInvoice.TotalSgst + vendorInvoice.TotalCess;
            if (totalGst2b != totalGstBooks)
                discrepancies.Add($"GST amount mismatch: 2B={totalGst2b}, Books={totalGstBooks}");

            var status = discrepancies.Any() ? Gstr2bMatchStatus.PartialMatch : Gstr2bMatchStatus.Matched;
            var matchDetails = JsonSerializer.Serialize(new { ManualMatch = true, MatchedBy = userId, Notes = notes });
            var discrepanciesJson = discrepancies.Any() ? JsonSerializer.Serialize(discrepancies) : null;

            await _gstr2bRepository.UpdateInvoiceMatchAsync(
                gstr2bInvoiceId, status, vendorInvoiceId, 100, matchDetails, discrepanciesJson);

            await _gstr2bRepository.UpdateInvoiceActionAsync(gstr2bInvoiceId, Gstr2bActionStatus.Accepted, userId, notes);

            return Result.Success();
        }

        public async Task<Result> ResetActionAsync(Guid invoiceId)
        {
            var invoice = await _gstr2bRepository.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
                return Error.NotFound($"Invoice with ID {invoiceId} not found");

            invoice.ActionStatus = null;
            invoice.ActionBy = null;
            invoice.ActionAt = null;
            invoice.ActionNotes = null;
            await _gstr2bRepository.UpdateInvoiceAsync(invoice);

            return Result.Success();
        }

        // ==================== Private Helpers ====================

        private Gstr2bMatchResult TryMatchInvoice(
            Gstr2bInvoice gstr2bInvoice,
            List<Core.Entities.VendorInvoice> vendorInvoices,
            List<Gstr2bReconciliationRule> rules)
        {
            // Filter vendor invoices by supplier GSTIN (always required)
            var candidateInvoices = vendorInvoices
                .Where(vi => vi.Party?.Gstin?.Equals(gstr2bInvoice.SupplierGstin, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (!candidateInvoices.Any())
            {
                return new Gstr2bMatchResult { IsMatch = false };
            }

            foreach (var rule in rules)
            {
                if (!rule.IsActive) continue;

                foreach (var vendorInvoice in candidateInvoices)
                {
                    var isMatch = true;
                    var discrepancies = new List<string>();
                    var details = new Dictionary<string, object> { { "RuleCode", rule.RuleCode } };

                    // Invoice number matching
                    if (rule.MatchInvoiceNumber)
                    {
                        var invoiceMatch = CompareInvoiceNumbers(
                            gstr2bInvoice.InvoiceNumber,
                            vendorInvoice.InvoiceNumber,
                            rule.InvoiceNumberFuzzyThreshold);

                        if (!invoiceMatch.IsMatch)
                        {
                            isMatch = false;
                            continue;
                        }
                        if (invoiceMatch.Distance > 0)
                        {
                            discrepancies.Add($"Invoice number fuzzy match (distance: {invoiceMatch.Distance})");
                        }
                    }

                    // Amount matching
                    if (rule.MatchAmount)
                    {
                        var amountMatch = CompareAmounts(
                            gstr2bInvoice.TaxableValue,
                            vendorInvoice.Subtotal,
                            rule.AmountTolerancePercentage,
                            rule.AmountToleranceAbsolute);

                        if (!amountMatch.IsMatch)
                        {
                            isMatch = false;
                            continue;
                        }
                        if (amountMatch.Difference != 0)
                        {
                            discrepancies.Add($"Amount difference: {amountMatch.Difference:C}");
                        }
                    }

                    // Date matching
                    if (rule.MatchInvoiceDate)
                    {
                        var dateMatch = CompareDates(
                            gstr2bInvoice.InvoiceDate,
                            vendorInvoice.InvoiceDate,
                            rule.DateToleranceDays);

                        if (!dateMatch.IsMatch)
                        {
                            isMatch = false;
                            continue;
                        }
                        if (dateMatch.DaysDifference != 0)
                        {
                            discrepancies.Add($"Invoice date difference: {dateMatch.DaysDifference} days");
                        }
                    }

                    if (isMatch)
                    {
                        // Check for GST amount discrepancies
                        var gst2b = gstr2bInvoice.IgstAmount + gstr2bInvoice.CgstAmount + gstr2bInvoice.SgstAmount + gstr2bInvoice.CessAmount;
                        var gstBooks = vendorInvoice.TotalIgst + vendorInvoice.TotalCgst + vendorInvoice.TotalSgst + vendorInvoice.TotalCess;
                        if (gst2b != gstBooks)
                        {
                            discrepancies.Add($"GST amount mismatch: 2B={gst2b:C}, Books={gstBooks:C}");
                        }

                        details["MatchedInvoiceNumber"] = vendorInvoice.InvoiceNumber;
                        details["MatchedInvoiceDate"] = vendorInvoice.InvoiceDate.ToString("yyyy-MM-dd");

                        return new Gstr2bMatchResult
                        {
                            IsMatch = true,
                            ConfidenceScore = rule.ConfidenceScore,
                            MatchedRuleCode = rule.RuleCode,
                            MatchedVendorInvoiceId = vendorInvoice.Id,
                            MatchedInvoiceNumber = vendorInvoice.InvoiceNumber,
                            Discrepancies = discrepancies,
                            Details = details
                        };
                    }
                }
            }

            return new Gstr2bMatchResult { IsMatch = false };
        }

        private static (bool IsMatch, int Distance) CompareInvoiceNumbers(string num1, string num2, int threshold)
        {
            // Normalize invoice numbers
            var n1 = NormalizeInvoiceNumber(num1);
            var n2 = NormalizeInvoiceNumber(num2);

            if (n1.Equals(n2, StringComparison.OrdinalIgnoreCase))
                return (true, 0);

            if (threshold == 0)
                return (false, -1);

            // Calculate Levenshtein distance
            var distance = LevenshteinDistance(n1, n2);
            return (distance <= threshold, distance);
        }

        private static string NormalizeInvoiceNumber(string invoiceNumber)
        {
            // Remove common prefixes/suffixes and normalize
            return invoiceNumber
                .ToUpperInvariant()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("/", "")
                .Replace("_", "");
        }

        private static int LevenshteinDistance(string s1, string s2)
        {
            var m = s1.Length;
            var n = s2.Length;
            var d = new int[m + 1, n + 1];

            for (var i = 0; i <= m; i++) d[i, 0] = i;
            for (var j = 0; j <= n; j++) d[0, j] = j;

            for (var j = 1; j <= n; j++)
            {
                for (var i = 1; i <= m; i++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[m, n];
        }

        private static (bool IsMatch, decimal Difference) CompareAmounts(decimal amount1, decimal amount2, decimal tolerancePercent, decimal toleranceAbsolute)
        {
            var difference = Math.Abs(amount1 - amount2);

            if (difference == 0)
                return (true, 0);

            // Check percentage tolerance
            if (tolerancePercent > 0)
            {
                var maxAmount = Math.Max(amount1, amount2);
                var percentDiff = maxAmount > 0 ? (difference / maxAmount) * 100 : 0;
                if (percentDiff <= tolerancePercent)
                    return (true, amount1 - amount2);
            }

            // Check absolute tolerance
            if (toleranceAbsolute > 0 && difference <= toleranceAbsolute)
                return (true, amount1 - amount2);

            return (false, amount1 - amount2);
        }

        private static (bool IsMatch, int DaysDifference) CompareDates(DateOnly date1, DateOnly date2, int toleranceDays)
        {
            var diff = Math.Abs(date1.DayNumber - date2.DayNumber);
            return (diff <= toleranceDays, diff);
        }

        private static List<Gstr2bInvoice> ParseGstr2bInvoices(Gstr2bJsonRoot data, Guid importId, Guid companyId, string returnPeriod)
        {
            var invoices = new List<Gstr2bInvoice>();

            // Parse B2B records
            if (data.Docdata?.B2b != null)
            {
                foreach (var supplier in data.Docdata.B2b)
                {
                    if (supplier.Inv == null) continue;

                    foreach (var inv in supplier.Inv)
                    {
                        var invoice = CreateInvoiceFromB2b(supplier, inv, importId, companyId, returnPeriod);
                        invoices.Add(invoice);
                    }
                }
            }

            // Parse CDNR (Credit/Debit Notes)
            if (data.Docdata?.Cdnr != null)
            {
                foreach (var supplier in data.Docdata.Cdnr)
                {
                    if (supplier.Nt == null) continue;

                    foreach (var note in supplier.Nt)
                    {
                        var invoice = CreateInvoiceFromCdnr(supplier, note, importId, companyId, returnPeriod);
                        invoices.Add(invoice);
                    }
                }
            }

            // Parse IMPG (Import of Goods)
            if (data.Docdata?.Impg != null)
            {
                foreach (var impg in data.Docdata.Impg)
                {
                    var invoice = CreateInvoiceFromImpg(impg, importId, companyId, returnPeriod);
                    invoices.Add(invoice);
                }
            }

            return invoices;
        }

        private static Gstr2bInvoice CreateInvoiceFromB2b(Gstr2bB2bRecord supplier, Gstr2bInvoiceRecord inv, Guid importId, Guid companyId, string returnPeriod)
        {
            var (igst, cgst, sgst, cess) = SumItemAmounts(inv.Items);

            return new Gstr2bInvoice
            {
                ImportId = importId,
                CompanyId = companyId,
                ReturnPeriod = returnPeriod,
                SupplierGstin = supplier.Ctin ?? string.Empty,
                SupplierTradeName = supplier.Trdnm,
                InvoiceNumber = inv.Inum ?? string.Empty,
                InvoiceDate = ParseDate(inv.Dt),
                InvoiceType = Gstr2bInvoiceTypes.B2B,
                DocumentType = "Invoice",
                TaxableValue = inv.Items?.Sum(i => i.Txval ?? 0) ?? 0,
                IgstAmount = igst,
                CgstAmount = cgst,
                SgstAmount = sgst,
                CessAmount = cess,
                TotalInvoiceValue = inv.Val ?? 0,
                ItcEligible = inv.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true,
                ItcIgst = inv.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true ? igst : 0,
                ItcCgst = inv.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true ? cgst : 0,
                ItcSgst = inv.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true ? sgst : 0,
                ItcCess = inv.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true ? cess : 0,
                PlaceOfSupply = inv.Pos,
                ReverseCharge = inv.Rev?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true,
                RawJson = JsonSerializer.Serialize(inv)
            };
        }

        private static Gstr2bInvoice CreateInvoiceFromCdnr(Gstr2bCdnRecord supplier, Gstr2bNoteRecord note, Guid importId, Guid companyId, string returnPeriod)
        {
            var (igst, cgst, sgst, cess) = SumItemAmounts(note.Items);
            var docType = note.Typ?.Equals("C", StringComparison.OrdinalIgnoreCase) == true ? "Credit Note" : "Debit Note";

            return new Gstr2bInvoice
            {
                ImportId = importId,
                CompanyId = companyId,
                ReturnPeriod = returnPeriod,
                SupplierGstin = supplier.Ctin ?? string.Empty,
                SupplierTradeName = supplier.Trdnm,
                InvoiceNumber = note.Ntnum ?? string.Empty,
                InvoiceDate = ParseDate(note.Dt),
                InvoiceType = Gstr2bInvoiceTypes.CDNR,
                DocumentType = docType,
                TaxableValue = note.Items?.Sum(i => i.Txval ?? 0) ?? 0,
                IgstAmount = igst,
                CgstAmount = cgst,
                SgstAmount = sgst,
                CessAmount = cess,
                TotalInvoiceValue = note.Val ?? 0,
                ItcEligible = note.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true,
                ItcIgst = note.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true ? igst : 0,
                ItcCgst = note.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true ? cgst : 0,
                ItcSgst = note.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true ? sgst : 0,
                ItcCess = note.Itcavl?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true ? cess : 0,
                PlaceOfSupply = note.Pos,
                ReverseCharge = note.Rev?.Equals("Y", StringComparison.OrdinalIgnoreCase) == true,
                RawJson = JsonSerializer.Serialize(note)
            };
        }

        private static Gstr2bInvoice CreateInvoiceFromImpg(Gstr2bImpgRecord impg, Guid importId, Guid companyId, string returnPeriod)
        {
            return new Gstr2bInvoice
            {
                ImportId = importId,
                CompanyId = companyId,
                ReturnPeriod = returnPeriod,
                SupplierGstin = "IMPORT",
                InvoiceNumber = impg.Benum ?? string.Empty,
                InvoiceDate = ParseDate(impg.Bedt),
                InvoiceType = Gstr2bInvoiceTypes.IMPG,
                DocumentType = "Bill of Entry",
                TaxableValue = impg.Txval ?? 0,
                IgstAmount = impg.Igst ?? 0,
                CgstAmount = 0,
                SgstAmount = 0,
                CessAmount = impg.Cess ?? 0,
                TotalInvoiceValue = (impg.Txval ?? 0) + (impg.Igst ?? 0) + (impg.Cess ?? 0),
                ItcEligible = true,
                ItcIgst = impg.Igst ?? 0,
                ItcCgst = 0,
                ItcSgst = 0,
                ItcCess = impg.Cess ?? 0,
                SupplyType = "import",
                RawJson = JsonSerializer.Serialize(impg)
            };
        }

        private static (decimal Igst, decimal Cgst, decimal Sgst, decimal Cess) SumItemAmounts(List<Gstr2bItemRecord>? items)
        {
            if (items == null || !items.Any())
                return (0, 0, 0, 0);

            return (
                items.Sum(i => i.Igst ?? 0),
                items.Sum(i => i.Cgst ?? 0),
                items.Sum(i => i.Sgst ?? 0),
                items.Sum(i => i.Cess ?? 0)
            );
        }

        private static DateOnly ParseDate(string? dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return DateOnly.FromDateTime(DateTime.Today);

            // Try DD-MM-YYYY format
            if (DateOnly.TryParseExact(dateStr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date1))
                return date1;

            // Try DD/MM/YYYY format
            if (DateOnly.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date2))
                return date2;

            // Try YYYY-MM-DD format
            if (DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date3))
                return date3;

            return DateOnly.FromDateTime(DateTime.Today);
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
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

        private static Gstr2bImportDto MapImportToDto(Gstr2bImport import)
        {
            return new Gstr2bImportDto
            {
                Id = import.Id,
                CompanyId = import.CompanyId,
                ReturnPeriod = import.ReturnPeriod,
                Gstin = import.Gstin,
                ImportSource = import.ImportSource,
                FileName = import.FileName,
                ImportStatus = import.ImportStatus,
                ErrorMessage = import.ErrorMessage,
                TotalInvoices = import.TotalInvoices,
                MatchedInvoices = import.MatchedInvoices,
                UnmatchedInvoices = import.UnmatchedInvoices,
                PartiallyMatchedInvoices = import.PartiallyMatchedInvoices,
                TotalItcIgst = import.TotalItcIgst,
                TotalItcCgst = import.TotalItcCgst,
                TotalItcSgst = import.TotalItcSgst,
                TotalItcCess = import.TotalItcCess,
                TotalItcAmount = import.TotalItcAmount,
                MatchedItcAmount = import.MatchedItcAmount,
                ImportedAt = import.ImportedAt,
                ProcessedAt = import.ProcessedAt,
                CreatedAt = import.CreatedAt
            };
        }

        private static Gstr2bInvoiceDto MapInvoiceToDto(Gstr2bInvoice invoice)
        {
            List<string>? discrepancies = null;
            if (!string.IsNullOrEmpty(invoice.MatchDiscrepancies))
            {
                try
                {
                    discrepancies = JsonSerializer.Deserialize<List<string>>(invoice.MatchDiscrepancies);
                }
                catch { }
            }

            return new Gstr2bInvoiceDto
            {
                Id = invoice.Id,
                ImportId = invoice.ImportId,
                ReturnPeriod = invoice.ReturnPeriod,
                SupplierGstin = invoice.SupplierGstin,
                SupplierName = invoice.SupplierName,
                SupplierTradeName = invoice.SupplierTradeName,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                InvoiceType = invoice.InvoiceType,
                DocumentType = invoice.DocumentType,
                TaxableValue = invoice.TaxableValue,
                IgstAmount = invoice.IgstAmount,
                CgstAmount = invoice.CgstAmount,
                SgstAmount = invoice.SgstAmount,
                CessAmount = invoice.CessAmount,
                TotalGst = invoice.TotalGst,
                TotalInvoiceValue = invoice.TotalInvoiceValue,
                ItcEligible = invoice.ItcEligible,
                ItcIgst = invoice.ItcIgst,
                ItcCgst = invoice.ItcCgst,
                ItcSgst = invoice.ItcSgst,
                ItcCess = invoice.ItcCess,
                PlaceOfSupply = invoice.PlaceOfSupply,
                SupplyType = invoice.SupplyType,
                ReverseCharge = invoice.ReverseCharge,
                MatchStatus = invoice.MatchStatus,
                MatchedVendorInvoiceId = invoice.MatchedVendorInvoiceId,
                MatchConfidence = invoice.MatchConfidence,
                Discrepancies = discrepancies,
                ActionStatus = invoice.ActionStatus,
                ActionNotes = invoice.ActionNotes
            };
        }
    }
}

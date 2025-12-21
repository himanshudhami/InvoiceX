using Application.DTOs.Forex;
using Application.Interfaces.Forex;
using Core.Common;
using Core.Entities.Forex;
using Core.Interfaces;
using Core.Interfaces.Forex;

namespace Application.Services.Forex
{
    /// <summary>
    /// Service for LUT (Letter of Undertaking) management
    /// LUT is required for zero-rated export supplies under GST
    /// </summary>
    public class LutService : ILutService
    {
        private readonly ILutRegisterRepository _lutRepository;
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly ICompaniesRepository _companiesRepository;

        public LutService(
            ILutRegisterRepository lutRepository,
            IInvoicesRepository invoicesRepository,
            ICompaniesRepository companiesRepository)
        {
            _lutRepository = lutRepository ?? throw new ArgumentNullException(nameof(lutRepository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _companiesRepository = companiesRepository ?? throw new ArgumentNullException(nameof(companiesRepository));
        }

        // ==================== CRUD Operations ====================

        /// <inheritdoc />
        public async Task<Result<LutRegister>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                return Error.Validation("LUT ID cannot be empty");

            var lut = await _lutRepository.GetByIdAsync(id);
            if (lut == null)
                return Error.NotFound($"LUT with ID {id} not found");

            return Result<LutRegister>.Success(lut);
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<LutRegister> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Guid? companyId = null,
            string? financialYear = null,
            string? status = null)
        {
            var filters = new Dictionary<string, object>();

            if (companyId.HasValue)
                filters["company_id"] = companyId.Value;
            if (!string.IsNullOrEmpty(financialYear))
                filters["financial_year"] = financialYear;
            if (!string.IsNullOrEmpty(status))
                filters["status"] = status;

            var result = await _lutRepository.GetPagedAsync(
                pageNumber, pageSize, null, "valid_from", true, filters);

            return Result<(IEnumerable<LutRegister> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<LutRegister>> CreateAsync(CreateLutDto dto)
        {
            if (dto.CompanyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            if (string.IsNullOrEmpty(dto.LutNumber))
                return Error.Validation("LUT number is required");

            if (string.IsNullOrEmpty(dto.FinancialYear))
                return Error.Validation("Financial year is required");

            if (string.IsNullOrEmpty(dto.Gstin))
                return Error.Validation("GSTIN is required");

            if (dto.ValidFrom >= dto.ValidTo)
                return Error.Validation("Valid from date must be before valid to date");

            // Check for existing active LUT in same financial year
            var existing = await _lutRepository.GetActiveForCompanyAsync(dto.CompanyId, dto.FinancialYear);
            if (existing != null)
            {
                // Mark existing as superseded if creating a new one
                existing.Status = "superseded";
                existing.UpdatedAt = DateTime.UtcNow;
                await _lutRepository.UpdateAsync(existing);
            }

            var lut = new LutRegister
            {
                Id = Guid.NewGuid(),
                CompanyId = dto.CompanyId,
                LutNumber = dto.LutNumber,
                FinancialYear = dto.FinancialYear,
                Gstin = dto.Gstin,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                FilingDate = dto.FilingDate,
                Arn = dto.Arn,
                Status = "active",
                Notes = dto.Notes,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _lutRepository.AddAsync(lut);
            return Result<LutRegister>.Success(created);
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateLutDto dto)
        {
            var lut = await _lutRepository.GetByIdAsync(id);
            if (lut == null)
                return Error.NotFound($"LUT with ID {id} not found");

            if (!string.IsNullOrEmpty(dto.LutNumber))
                lut.LutNumber = dto.LutNumber;
            if (dto.FilingDate.HasValue)
                lut.FilingDate = dto.FilingDate;
            if (!string.IsNullOrEmpty(dto.Arn))
                lut.Arn = dto.Arn;
            if (!string.IsNullOrEmpty(dto.Status))
                lut.Status = dto.Status;
            if (dto.Notes != null)
                lut.Notes = dto.Notes;

            lut.UpdatedAt = DateTime.UtcNow;

            await _lutRepository.UpdateAsync(lut);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            var lut = await _lutRepository.GetByIdAsync(id);
            if (lut == null)
                return Error.NotFound($"LUT with ID {id} not found");

            // Soft delete - mark as cancelled
            lut.Status = "cancelled";
            lut.UpdatedAt = DateTime.UtcNow;
            await _lutRepository.UpdateAsync(lut);

            return Result.Success();
        }

        // ==================== Validation ====================

        /// <inheritdoc />
        public async Task<Result<LutValidationResultDto>> ValidateLutForInvoiceAsync(Guid companyId, DateOnly invoiceDate)
        {
            var result = new LutValidationResultDto();

            var lut = await _lutRepository.GetValidForDateAsync(companyId, invoiceDate);

            if (lut == null)
            {
                result.IsValid = false;
                result.Message = $"No valid LUT found for invoice date {invoiceDate:dd-MMM-yyyy}. Export invoices require a valid LUT.";
                return Result<LutValidationResultDto>.Success(result);
            }

            result.IsValid = true;
            result.LutId = lut.Id;
            result.LutNumber = lut.LutNumber;
            result.FinancialYear = lut.FinancialYear;
            result.ValidFrom = lut.ValidFrom;
            result.ValidTo = lut.ValidTo;
            result.Message = "Valid LUT found for the invoice date";

            // Check for upcoming expiry warning
            var daysUntilExpiry = (lut.ValidTo.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days;
            if (daysUntilExpiry <= 30 && daysUntilExpiry > 0)
            {
                result.Warnings.Add($"LUT will expire in {daysUntilExpiry} days on {lut.ValidTo:dd-MMM-yyyy}. Consider renewal.");
            }

            return Result<LutValidationResultDto>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<LutRegister>> GetActiveAsync(Guid companyId, string financialYear)
        {
            var lut = await _lutRepository.GetActiveForCompanyAsync(companyId, financialYear);
            if (lut == null)
                return Error.NotFound($"No active LUT found for company in FY {financialYear}");

            return Result<LutRegister>.Success(lut);
        }

        /// <inheritdoc />
        public async Task<Result<LutRegister>> GetValidForDateAsync(Guid companyId, DateOnly date)
        {
            var lut = await _lutRepository.GetValidForDateAsync(companyId, date);
            if (lut == null)
                return Error.NotFound($"No valid LUT found for date {date:dd-MMM-yyyy}");

            return Result<LutRegister>.Success(lut);
        }

        // ==================== Status Management ====================

        /// <inheritdoc />
        public async Task<Result<int>> ExpireOldLutsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            await _lutRepository.ExpireOldLutsAsync(today);

            // Count how many were expired (not directly available, so return 0)
            return Result<int>.Success(0);
        }

        /// <inheritdoc />
        public async Task<Result<LutRegister>> RenewLutAsync(Guid existingLutId, CreateLutDto newLutDto)
        {
            var existingLut = await _lutRepository.GetByIdAsync(existingLutId);
            if (existingLut == null)
                return Error.NotFound($"Existing LUT with ID {existingLutId} not found");

            // Create new LUT
            var createResult = await CreateAsync(newLutDto);
            if (createResult.IsFailure)
                return createResult;

            // Mark old LUT as superseded
            await _lutRepository.SupersedeAsync(existingLutId, createResult.Value!.Id);

            return createResult;
        }

        /// <inheritdoc />
        public async Task<Result> CancelLutAsync(Guid id, string? reason)
        {
            var lut = await _lutRepository.GetByIdAsync(id);
            if (lut == null)
                return Error.NotFound($"LUT with ID {id} not found");

            lut.Status = "cancelled";
            if (!string.IsNullOrEmpty(reason))
                lut.Notes = (lut.Notes ?? "") + $"\nCancellation reason: {reason}";
            lut.UpdatedAt = DateTime.UtcNow;

            await _lutRepository.UpdateAsync(lut);
            return Result.Success();
        }

        // ==================== Reporting ====================

        /// <inheritdoc />
        public async Task<Result<LutUtilizationReportDto>> GetUtilizationReportAsync(
            Guid companyId,
            string financialYear)
        {
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
                return Error.NotFound($"Company with ID {companyId} not found");

            var lut = await _lutRepository.GetActiveForCompanyAsync(companyId, financialYear);

            // Get export invoices for the financial year
            var (startDate, endDate) = GetFinancialYearDates(financialYear);
            var filters = new Dictionary<string, object> { { "company_id", companyId } };
            var (allInvoices, _) = await _invoicesRepository.GetPagedAsync(1, int.MaxValue, null, null, false, filters);

            var exportInvoices = allInvoices
                .Where(i => !string.IsNullOrEmpty(i.Currency) &&
                            i.Currency != "INR" &&
                            i.Status != "draft" &&
                            i.Status != "cancelled" &&
                            i.InvoiceDate >= startDate &&
                            i.InvoiceDate <= endDate)
                .ToList();

            var report = new LutUtilizationReportDto
            {
                CompanyId = companyId,
                CompanyName = company.Name ?? string.Empty,
                FinancialYear = financialYear,
                LutId = lut?.Id,
                LutNumber = lut?.LutNumber,
                ValidFrom = lut?.ValidFrom,
                ValidTo = lut?.ValidTo,
                LutStatus = lut?.Status,
                TotalExportInvoices = exportInvoices.Count,
                TotalExportValueForeign = exportInvoices.Sum(i => i.TotalAmount),
                TotalExportValueInr = exportInvoices.Sum(i => i.ForeignCurrencyAmount ?? 0),
                PrimaryCurrency = exportInvoices.FirstOrDefault()?.Currency ?? "USD"
            };

            // Group by currency
            foreach (var group in exportInvoices.GroupBy(i => i.Currency ?? "USD"))
            {
                report.CurrencyBreakdown[group.Key] = new CurrencyExportSummaryDto
                {
                    Currency = group.Key,
                    InvoiceCount = group.Count(),
                    TotalAmount = group.Sum(i => i.TotalAmount),
                    TotalAmountInr = group.Sum(i => i.ForeignCurrencyAmount ?? 0)
                };
            }

            // Group by month
            foreach (var group in exportInvoices.GroupBy(i => new { i.InvoiceDate.Month, i.InvoiceDate.Year }))
            {
                report.MonthlyBreakdown.Add(new MonthlyExportSummaryDto
                {
                    Month = group.Key.Month,
                    Year = group.Key.Year,
                    MonthName = new DateTime(group.Key.Year, group.Key.Month, 1).ToString("MMM yyyy"),
                    InvoiceCount = group.Count(),
                    TotalAmountForeign = group.Sum(i => i.TotalAmount),
                    TotalAmountInr = group.Sum(i => i.ForeignCurrencyAmount ?? 0)
                });
            }

            report.MonthlyBreakdown = report.MonthlyBreakdown.OrderBy(m => m.Year).ThenBy(m => m.Month).ToList();

            return Result<LutUtilizationReportDto>.Success(report);
        }

        /// <inheritdoc />
        public async Task<Result<LutComplianceSummaryDto>> GetComplianceSummaryAsync(Guid companyId)
        {
            var allLuts = (await _lutRepository.GetByCompanyIdAsync(companyId)).ToList();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentFy = GetFinancialYear(today);

            var activeLut = allLuts.FirstOrDefault(l => l.IsValidForDate(today));

            var summary = new LutComplianceSummaryDto
            {
                CompanyId = companyId,
                HasActiveLut = activeLut != null,
                ActiveLutId = activeLut?.Id,
                ActiveLutNumber = activeLut?.LutNumber,
                ActiveLutExpiry = activeLut?.ValidTo,
                TotalLutsIssued = allLuts.Count,
                ActiveLuts = allLuts.Count(l => l.Status == "active"),
                ExpiredLuts = allLuts.Count(l => l.Status == "expired"),
                CancelledLuts = allLuts.Count(l => l.Status == "cancelled")
            };

            if (activeLut != null)
            {
                summary.DaysUntilExpiry = (activeLut.ValidTo.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days;
            }

            // Check compliance issues
            if (!summary.HasActiveLut)
            {
                summary.ComplianceIssues.Add("No active LUT found. Export invoices may not be valid without LUT.");
            }

            if (summary.DaysUntilExpiry.HasValue && summary.DaysUntilExpiry <= 30)
            {
                summary.ComplianceIssues.Add($"LUT expires in {summary.DaysUntilExpiry} days. Renewal required.");
            }

            // Get export invoices to check coverage
            var filters = new Dictionary<string, object> { { "company_id", companyId } };
            var (fyStartDate, fyEndDate) = GetFinancialYearDates(currentFy);
            var (allInvoices, _) = await _invoicesRepository.GetPagedAsync(1, int.MaxValue, null, null, false, filters);

            var exportInvoices = allInvoices
                .Where(i => !string.IsNullOrEmpty(i.Currency) &&
                            i.Currency != "INR" &&
                            i.Status != "draft" &&
                            i.Status != "cancelled" &&
                            i.InvoiceDate >= fyStartDate &&
                            i.InvoiceDate <= fyEndDate)
                .ToList();

            // Check each export invoice against LUT coverage
            foreach (var invoice in exportInvoices)
            {
                var hasValidLut = await _lutRepository.IsLutValidAsync(companyId, invoice.InvoiceDate);
                if (hasValidLut)
                {
                    summary.ExportInvoicesUnderLut++;
                    summary.ExportValueUnderLut += invoice.TotalAmount;
                }
                else
                {
                    summary.ExportInvoicesWithoutLut++;
                    summary.ExportValueWithoutLut += invoice.TotalAmount;
                    summary.ComplianceIssues.Add($"Invoice {invoice.InvoiceNumber} dated {invoice.InvoiceDate:dd-MMM-yyyy} has no valid LUT coverage.");
                }
            }

            summary.IsCompliant = summary.ComplianceIssues.Count == 0;

            return Result<LutComplianceSummaryDto>.Success(summary);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<LutExpiryAlertDto>>> GetExpiryAlertsAsync(
            Guid? companyId = null,
            int daysBeforeExpiry = 30)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var cutoffDate = today.AddDays(daysBeforeExpiry);

            IEnumerable<LutRegister> luts;
            if (companyId.HasValue)
                luts = await _lutRepository.GetByCompanyIdAsync(companyId.Value);
            else
                luts = await _lutRepository.GetAllAsync();

            var alerts = new List<LutExpiryAlertDto>();

            foreach (var lut in luts.Where(l => l.Status == "active"))
            {
                var daysUntilExpiry = (lut.ValidTo.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;

                if (daysUntilExpiry <= daysBeforeExpiry)
                {
                    var company = await _companiesRepository.GetByIdAsync(lut.CompanyId);
                    var alertLevel = daysUntilExpiry switch
                    {
                        < 0 => "expired",
                        <= 7 => "critical",
                        <= 14 => "warning",
                        _ => "normal"
                    };

                    alerts.Add(new LutExpiryAlertDto
                    {
                        LutId = lut.Id,
                        CompanyId = lut.CompanyId,
                        CompanyName = company?.Name ?? "Unknown",
                        LutNumber = lut.LutNumber,
                        FinancialYear = lut.FinancialYear,
                        ValidTo = lut.ValidTo,
                        DaysUntilExpiry = daysUntilExpiry,
                        AlertLevel = alertLevel
                    });
                }
            }

            return Result<IEnumerable<LutExpiryAlertDto>>.Success(
                alerts.OrderBy(a => a.DaysUntilExpiry));
        }

        // ==================== Helper Methods ====================

        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static (DateOnly StartDate, DateOnly EndDate) GetFinancialYearDates(string financialYear)
        {
            // Parse "2025-26" format
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = startYear + 1;

            return (new DateOnly(startYear, 4, 1), new DateOnly(endYear, 3, 31));
        }
    }
}

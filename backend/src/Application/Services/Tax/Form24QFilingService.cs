using System.Text.Json;
using Core.Common;
using Core.Entities.Tax;
using Core.Interfaces;
using Core.Interfaces.Tax;
using Microsoft.Extensions.Logging;

namespace Application.Services.Tax
{
    /// <summary>
    /// Service implementation for Form 24Q quarterly TDS filing management.
    /// Orchestrates data generation, validation, FVU generation, and filing workflow.
    /// </summary>
    public class Form24QFilingService : IForm24QFilingService
    {
        private readonly IForm24QFilingRepository _filingRepository;
        private readonly ITdsReturnService _tdsReturnService;
        private readonly IFvuFileGeneratorService _fvuGeneratorService;
        private readonly ICompaniesRepository _companiesRepository;
        private readonly ILogger<Form24QFilingService> _logger;

        // Quarter months mapping
        private static readonly Dictionary<string, (int StartMonth, int EndMonth)> QuarterMonths = new()
        {
            { "Q1", (4, 6) },   // Apr-Jun
            { "Q2", (7, 9) },   // Jul-Sep
            { "Q3", (10, 12) }, // Oct-Dec
            { "Q4", (1, 3) }    // Jan-Mar
        };

        public Form24QFilingService(
            IForm24QFilingRepository filingRepository,
            ITdsReturnService tdsReturnService,
            IFvuFileGeneratorService fvuGeneratorService,
            ICompaniesRepository companiesRepository,
            ILogger<Form24QFilingService> logger)
        {
            _filingRepository = filingRepository ?? throw new ArgumentNullException(nameof(filingRepository));
            _tdsReturnService = tdsReturnService ?? throw new ArgumentNullException(nameof(tdsReturnService));
            _fvuGeneratorService = fvuGeneratorService ?? throw new ArgumentNullException(nameof(fvuGeneratorService));
            _companiesRepository = companiesRepository ?? throw new ArgumentNullException(nameof(companiesRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== Retrieval Operations ====================

        public async Task<Result<Form24QFilingDto>> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting Form 24Q filing by ID: {FilingId}", id);

            var filing = await _filingRepository.GetByIdAsync(id);
            if (filing == null)
            {
                return Error.NotFound($"Form 24Q filing with ID {id} not found");
            }

            return Result<Form24QFilingDto>.Success(await MapToDto(filing));
        }

        public async Task<Result<Form24QFilingDto>> GetByCompanyQuarterAsync(
            Guid companyId,
            string financialYear,
            string quarter)
        {
            _logger.LogInformation("Getting Form 24Q filing for company {CompanyId}, FY {FY}, Quarter {Quarter}",
                companyId, financialYear, quarter);

            var filing = await _filingRepository.GetByCompanyQuarterAsync(companyId, financialYear, quarter);
            if (filing == null)
            {
                return Error.NotFound($"Form 24Q filing for {financialYear} {quarter} not found");
            }

            return Result<Form24QFilingDto>.Success(await MapToDto(filing));
        }

        public async Task<Result<PagedResult<Form24QFilingSummaryDto>>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? quarter = null,
            string? status = null,
            string? sortBy = null,
            bool sortDescending = false)
        {
            _logger.LogInformation("Getting paged Form 24Q filings for company {CompanyId}", companyId);

            var (items, totalCount) = await _filingRepository.GetPagedAsync(
                companyId, pageNumber, pageSize, financialYear, quarter, status, sortBy, sortDescending);

            var dtos = items.Select(f => MapToSummaryDto(f)).ToList();

            return Result<PagedResult<Form24QFilingSummaryDto>>.Success(new PagedResult<Form24QFilingSummaryDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        public async Task<Result<Form24QFilingStatisticsDto>> GetStatisticsAsync(
            Guid companyId,
            string financialYear)
        {
            _logger.LogInformation("Getting Form 24Q statistics for company {CompanyId}, FY {FY}",
                companyId, financialYear);

            var stats = await _filingRepository.GetStatisticsAsync(companyId, financialYear);

            return Result<Form24QFilingStatisticsDto>.Success(new Form24QFilingStatisticsDto
            {
                FinancialYear = stats.FinancialYear,
                TotalFilings = stats.TotalFilings,
                DraftCount = stats.DraftCount,
                ValidatedCount = stats.ValidatedCount,
                FvuGeneratedCount = stats.FvuGeneratedCount,
                SubmittedCount = stats.SubmittedCount,
                AcknowledgedCount = stats.AcknowledgedCount,
                RejectedCount = stats.RejectedCount,
                PendingCount = stats.PendingCount,
                OverdueCount = stats.OverdueCount,
                TotalTdsDeducted = stats.TotalTdsDeducted,
                TotalTdsDeposited = stats.TotalTdsDeposited,
                TotalVariance = stats.TotalVariance,
                Q1 = stats.Q1Status != null ? MapQuarterStatus(stats.Q1Status) : GetEmptyQuarterStatus("Q1", financialYear),
                Q2 = stats.Q2Status != null ? MapQuarterStatus(stats.Q2Status) : GetEmptyQuarterStatus("Q2", financialYear),
                Q3 = stats.Q3Status != null ? MapQuarterStatus(stats.Q3Status) : GetEmptyQuarterStatus("Q3", financialYear),
                Q4 = stats.Q4Status != null ? MapQuarterStatus(stats.Q4Status) : GetEmptyQuarterStatus("Q4", financialYear)
            });
        }

        public async Task<Result<IEnumerable<Form24QFilingSummaryDto>>> GetByFinancialYearAsync(
            Guid companyId,
            string financialYear)
        {
            var filings = await _filingRepository.GetByFinancialYearAsync(companyId, financialYear);
            return Result<IEnumerable<Form24QFilingSummaryDto>>.Success(
                filings.Select(f => MapToSummaryDto(f)));
        }

        public async Task<Result<IEnumerable<Form24QFilingSummaryDto>>> GetPendingFilingsAsync(
            Guid companyId,
            string financialYear)
        {
            var filings = await _filingRepository.GetPendingFilingsAsync(companyId, financialYear);
            return Result<IEnumerable<Form24QFilingSummaryDto>>.Success(
                filings.Select(f => MapToSummaryDto(f)));
        }

        public async Task<Result<IEnumerable<Form24QFilingSummaryDto>>> GetOverdueFilingsAsync(Guid companyId)
        {
            var filings = await _filingRepository.GetOverdueFilingsAsync(companyId);
            return Result<IEnumerable<Form24QFilingSummaryDto>>.Success(
                filings.Select(f => MapToSummaryDto(f)));
        }

        // ==================== Draft Operations ====================

        public async Task<Result<Form24QFilingDto>> CreateDraftAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            Guid? createdBy = null)
        {
            _logger.LogInformation("Creating Form 24Q draft for company {CompanyId}, FY {FY}, Quarter {Quarter}",
                companyId, financialYear, quarter);

            // Validate quarter
            if (!QuarterMonths.ContainsKey(quarter))
            {
                return Error.Validation($"Invalid quarter: {quarter}. Must be Q1, Q2, Q3, or Q4");
            }

            // Check if filing already exists
            var existingFiling = await _filingRepository.GetByCompanyQuarterAsync(companyId, financialYear, quarter);
            if (existingFiling != null)
            {
                return Error.Conflict($"Form 24Q filing for {financialYear} {quarter} already exists");
            }

            // Get company details
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                return Error.NotFound("Company not found");
            }

            if (string.IsNullOrEmpty(company.TaxNumber))
            {
                return Error.Validation("Company TAN is required for Form 24Q filing");
            }

            // Generate Form 24Q data
            Form24QData form24QData;
            try
            {
                form24QData = await _tdsReturnService.GenerateForm24QAsync(companyId, financialYear, quarter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Form 24Q data");
                return Error.Internal($"Error generating Form 24Q data: {ex.Message}");
            }

            // Generate Annexure II for Q4
            string? annexure2Json = null;
            if (quarter == "Q4")
            {
                try
                {
                    var annexureII = await _tdsReturnService.GenerateForm24QAnnexureIIAsync(companyId, financialYear);
                    annexure2Json = JsonSerializer.Serialize(annexureII);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error generating Annexure II, continuing without it");
                }
            }

            // Create filing record
            var filing = new Form24QFiling
            {
                CompanyId = companyId,
                FinancialYear = financialYear,
                Quarter = quarter,
                Tan = company.TaxNumber,
                FormType = "regular",
                TotalEmployees = form24QData.TotalEmployees,
                TotalSalaryPaid = form24QData.TotalSalary,
                TotalTdsDeducted = form24QData.TotalTdsDeducted,
                TotalTdsDeposited = form24QData.TotalTdsDeposited,
                Variance = form24QData.TotalTdsDeducted - form24QData.TotalTdsDeposited,
                EmployeeRecords = JsonSerializer.Serialize(form24QData.EmployeeRecords),
                ChallanRecords = JsonSerializer.Serialize(form24QData.Challans),
                Annexure1Data = JsonSerializer.Serialize(form24QData.Challans),
                Annexure2Data = annexure2Json,
                Status = "draft",
                CreatedBy = createdBy
            };

            var created = await _filingRepository.AddAsync(filing);
            _logger.LogInformation("Created Form 24Q draft with ID: {FilingId}", created.Id);

            return Result<Form24QFilingDto>.Success(await MapToDto(created));
        }

        public async Task<Result<Form24QFilingDto>> RefreshDataAsync(Guid filingId, Guid? updatedBy = null)
        {
            _logger.LogInformation("Refreshing Form 24Q data for filing {FilingId}", filingId);

            var filing = await _filingRepository.GetByIdAsync(filingId);
            if (filing == null)
            {
                return Error.NotFound("Filing not found");
            }

            if (filing.Status != "draft")
            {
                return Error.Validation("Can only refresh data for draft filings");
            }

            // Regenerate Form 24Q data
            var form24QData = await _tdsReturnService.GenerateForm24QAsync(
                filing.CompanyId, filing.FinancialYear, filing.Quarter);

            // Update filing
            filing.TotalEmployees = form24QData.TotalEmployees;
            filing.TotalSalaryPaid = form24QData.TotalSalary;
            filing.TotalTdsDeducted = form24QData.TotalTdsDeducted;
            filing.TotalTdsDeposited = form24QData.TotalTdsDeposited;
            filing.Variance = form24QData.TotalTdsDeducted - form24QData.TotalTdsDeposited;
            filing.EmployeeRecords = JsonSerializer.Serialize(form24QData.EmployeeRecords);
            filing.ChallanRecords = JsonSerializer.Serialize(form24QData.Challans);
            filing.Annexure1Data = JsonSerializer.Serialize(form24QData.Challans);
            filing.UpdatedBy = updatedBy;

            // Regenerate Annexure II for Q4
            if (filing.Quarter == "Q4")
            {
                var annexureII = await _tdsReturnService.GenerateForm24QAnnexureIIAsync(
                    filing.CompanyId, filing.FinancialYear);
                filing.Annexure2Data = JsonSerializer.Serialize(annexureII);
            }

            // Clear previous validation
            filing.ValidationErrors = null;
            filing.ValidationWarnings = null;
            filing.ValidatedAt = null;
            filing.ValidatedBy = null;

            await _filingRepository.UpdateAsync(filing);
            _logger.LogInformation("Refreshed Form 24Q data for filing {FilingId}", filingId);

            return Result<Form24QFilingDto>.Success(await MapToDto(filing));
        }

        public async Task<Result<Form24QPreviewData>> PreviewAsync(
            Guid companyId,
            string financialYear,
            string quarter)
        {
            _logger.LogInformation("Previewing Form 24Q for company {CompanyId}, FY {FY}, Quarter {Quarter}",
                companyId, financialYear, quarter);

            var form24QData = await _tdsReturnService.GenerateForm24QAsync(companyId, financialYear, quarter);

            return Result<Form24QPreviewData>.Success(new Form24QPreviewData
            {
                FinancialYear = financialYear,
                Quarter = quarter,
                TotalEmployees = form24QData.TotalEmployees,
                TotalSalaryPaid = form24QData.TotalSalary,
                TotalTdsDeducted = form24QData.TotalTdsDeducted,
                TotalTdsDeposited = form24QData.TotalTdsDeposited,
                Variance = form24QData.TotalTdsDeducted - form24QData.TotalTdsDeposited,
                Challans = form24QData.Challans.Select(c => new ChallanPreviewDto
                {
                    BsrCode = c.BsrCode,
                    ChallanDate = c.DepositDate,
                    ChallanSerial = c.ChallanNumber,
                    Amount = c.TotalAmount,
                    Cin = null // CIN is tracked separately
                }).ToList(),
                Employees = form24QData.EmployeeRecords.Select(e => new EmployeePreviewDto
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.EmployeeName,
                    Pan = e.EmployeePan,
                    GrossSalary = e.GrossSalary,
                    TdsDeducted = e.TdsDeducted
                }).ToList()
            });
        }

        // ==================== Validation Operations ====================

        public async Task<Result<Form24QValidationResult>> ValidateFilingAsync(Guid filingId)
        {
            _logger.LogInformation("Validating Form 24Q filing {FilingId}", filingId);

            var filing = await _filingRepository.GetByIdAsync(filingId);
            if (filing == null)
            {
                return Error.NotFound("Filing not found");
            }

            // Call TDS return validation
            var validationResult = await _tdsReturnService.ValidateForm24QAsync(
                filing.CompanyId, filing.FinancialYear, filing.Quarter);

            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                Code = e.Code,
                Message = e.Message,
                Field = e.Field,
                RecordIdentifier = e.RecordIdentifier
            }).ToList();

            var warnings = validationResult.Warnings.Select(w => new ValidationWarning
            {
                Code = w.Code,
                Message = w.Message,
                Field = w.Field,
                RecordIdentifier = w.RecordIdentifier
            }).ToList();

            // Update filing with validation results
            filing.ValidationErrors = errors.Any() ? JsonSerializer.Serialize(errors) : null;
            filing.ValidationWarnings = warnings.Any() ? JsonSerializer.Serialize(warnings) : null;
            filing.ValidatedAt = DateTime.UtcNow;

            if (validationResult.IsValid && filing.Status == "draft")
            {
                filing.Status = "validated";
            }

            await _filingRepository.UpdateAsync(filing);

            return Result<Form24QValidationResult>.Success(new Form24QValidationResult
            {
                IsValid = validationResult.IsValid,
                Errors = errors,
                Warnings = warnings
            });
        }

        // ==================== FVU Operations ====================

        public async Task<Result<Form24QFilingDto>> GenerateFvuAsync(Guid filingId, Guid? generatedBy = null)
        {
            _logger.LogInformation("Generating FVU for Form 24Q filing {FilingId}", filingId);

            var filing = await _filingRepository.GetByIdAsync(filingId);
            if (filing == null)
            {
                return Error.NotFound("Filing not found");
            }

            if (filing.Status != "validated" && filing.Status != "fvu_generated")
            {
                return Error.Validation("Filing must be validated before generating FVU file");
            }

            // Generate FVU file
            var isCorrection = filing.FormType == "correction";
            var fvuResult = await _fvuGeneratorService.GenerateForm24QFileAsync(
                filing.CompanyId, filing.FinancialYear, filing.Quarter, isCorrection);

            if (fvuResult.IsFailure)
            {
                return Error.Internal($"FVU generation failed: {fvuResult.Error?.Message}");
            }

            // Save the FVU stream to a file
            var fvuDirectory = Path.Combine(Path.GetTempPath(), "fvu_files");
            Directory.CreateDirectory(fvuDirectory);
            var fvuFileName = $"24Q_{filing.Tan}_{filing.FinancialYear}_{filing.Quarter}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
            var fvuFilePath = Path.Combine(fvuDirectory, fvuFileName);

            await using (var fileStream = new FileStream(fvuFilePath, FileMode.Create, FileAccess.Write))
            {
                await fvuResult.Value!.FileStream.CopyToAsync(fileStream);
            }

            // Update filing with FVU details
            filing.FvuFilePath = fvuFilePath;
            filing.FvuGeneratedAt = DateTime.UtcNow;
            filing.FvuVersion = "7.8";
            filing.Status = "fvu_generated";
            filing.UpdatedBy = generatedBy;

            await _filingRepository.UpdateAsync(filing);
            _logger.LogInformation("Generated FVU for Form 24Q filing {FilingId} at {FilePath}", filingId, fvuFilePath);

            return Result<Form24QFilingDto>.Success(await MapToDto(filing));
        }

        public async Task<Result<Form24QFileDownloadResult>> DownloadFvuAsync(Guid filingId)
        {
            _logger.LogInformation("Downloading FVU for Form 24Q filing {FilingId}", filingId);

            var filing = await _filingRepository.GetByIdAsync(filingId);
            if (filing == null)
            {
                return Error.NotFound("Filing not found");
            }

            if (string.IsNullOrEmpty(filing.FvuFilePath))
            {
                return Error.NotFound("FVU file not generated for this filing");
            }

            if (!File.Exists(filing.FvuFilePath))
            {
                return Error.NotFound("FVU file not found on disk");
            }

            var fileStream = new FileStream(filing.FvuFilePath, FileMode.Open, FileAccess.Read);
            var fileName = $"24Q_{filing.Tan}_{filing.FinancialYear}_{filing.Quarter}.txt";

            return Result<Form24QFileDownloadResult>.Success(new Form24QFileDownloadResult
            {
                FileStream = fileStream,
                FileName = fileName,
                ContentType = "text/plain"
            });
        }

        // ==================== Workflow Operations ====================

        public async Task<Result<Form24QFilingDto>> MarkAsSubmittedAsync(
            Guid filingId,
            DateOnly? filingDate = null,
            Guid? submittedBy = null)
        {
            _logger.LogInformation("Marking Form 24Q filing {FilingId} as submitted", filingId);

            var filing = await _filingRepository.GetByIdAsync(filingId);
            if (filing == null)
            {
                return Error.NotFound("Filing not found");
            }

            if (filing.Status != "fvu_generated")
            {
                return Error.Validation("FVU must be generated before marking as submitted");
            }

            filing.Status = "submitted";
            filing.FilingDate = filingDate ?? DateOnly.FromDateTime(DateTime.Today);
            filing.SubmittedAt = DateTime.UtcNow;
            filing.SubmittedBy = submittedBy;
            filing.UpdatedBy = submittedBy;

            await _filingRepository.UpdateAsync(filing);

            return Result<Form24QFilingDto>.Success(await MapToDto(filing));
        }

        public async Task<Result<Form24QFilingDto>> RecordAcknowledgementAsync(
            Guid filingId,
            string acknowledgementNumber,
            string? tokenNumber = null,
            DateOnly? filingDate = null,
            Guid? updatedBy = null)
        {
            _logger.LogInformation("Recording acknowledgement for Form 24Q filing {FilingId}", filingId);

            var filing = await _filingRepository.GetByIdAsync(filingId);
            if (filing == null)
            {
                return Error.NotFound("Filing not found");
            }

            if (filing.Status != "submitted" && filing.Status != "fvu_generated")
            {
                return Error.Validation("Filing must be submitted before recording acknowledgement");
            }

            filing.Status = "acknowledged";
            filing.AcknowledgementNumber = acknowledgementNumber;
            filing.TokenNumber = tokenNumber;
            filing.FilingDate = filingDate ?? filing.FilingDate ?? DateOnly.FromDateTime(DateTime.Today);
            filing.UpdatedBy = updatedBy;

            await _filingRepository.UpdateAsync(filing);

            return Result<Form24QFilingDto>.Success(await MapToDto(filing));
        }

        public async Task<Result<Form24QFilingDto>> MarkAsRejectedAsync(
            Guid filingId,
            string rejectionReason,
            Guid? updatedBy = null)
        {
            _logger.LogInformation("Marking Form 24Q filing {FilingId} as rejected", filingId);

            var filing = await _filingRepository.GetByIdAsync(filingId);
            if (filing == null)
            {
                return Error.NotFound("Filing not found");
            }

            filing.Status = "rejected";
            filing.RejectionReason = rejectionReason;
            filing.RejectedAt = DateTime.UtcNow;
            filing.UpdatedBy = updatedBy;

            await _filingRepository.UpdateAsync(filing);

            return Result<Form24QFilingDto>.Success(await MapToDto(filing));
        }

        // ==================== Correction Returns ====================

        public async Task<Result<Form24QFilingDto>> CreateCorrectionReturnAsync(
            Guid originalFilingId,
            Guid? createdBy = null)
        {
            _logger.LogInformation("Creating correction return for Form 24Q filing {FilingId}", originalFilingId);

            var original = await _filingRepository.GetByIdAsync(originalFilingId);
            if (original == null)
            {
                return Error.NotFound("Original filing not found");
            }

            if (original.Status != "acknowledged")
            {
                return Error.Validation("Can only create correction for acknowledged filings");
            }

            // Mark original as revised
            original.Status = "revised";
            await _filingRepository.UpdateAsync(original);

            // Get next revision number
            var revisionNumber = await _filingRepository.GetNextRevisionNumberAsync(
                original.CompanyId, original.FinancialYear, original.Quarter);

            // Get company details
            var company = await _companiesRepository.GetByIdAsync(original.CompanyId);

            // Generate fresh data for correction
            var form24QData = await _tdsReturnService.GenerateForm24QAsync(
                original.CompanyId, original.FinancialYear, original.Quarter);

            // Create correction filing
            var correction = new Form24QFiling
            {
                CompanyId = original.CompanyId,
                FinancialYear = original.FinancialYear,
                Quarter = original.Quarter,
                Tan = company?.TaxNumber ?? original.Tan,
                FormType = "correction",
                OriginalFilingId = originalFilingId,
                RevisionNumber = revisionNumber,
                TotalEmployees = form24QData.TotalEmployees,
                TotalSalaryPaid = form24QData.TotalSalary,
                TotalTdsDeducted = form24QData.TotalTdsDeducted,
                TotalTdsDeposited = form24QData.TotalTdsDeposited,
                Variance = form24QData.TotalTdsDeducted - form24QData.TotalTdsDeposited,
                EmployeeRecords = JsonSerializer.Serialize(form24QData.EmployeeRecords),
                ChallanRecords = JsonSerializer.Serialize(form24QData.Challans),
                Annexure1Data = JsonSerializer.Serialize(form24QData.Challans),
                Status = "draft",
                CreatedBy = createdBy
            };

            // Generate Annexure II for Q4
            if (original.Quarter == "Q4")
            {
                var annexureII = await _tdsReturnService.GenerateForm24QAnnexureIIAsync(
                    original.CompanyId, original.FinancialYear);
                correction.Annexure2Data = JsonSerializer.Serialize(annexureII);
            }

            var created = await _filingRepository.AddAsync(correction);
            _logger.LogInformation("Created correction return with ID: {FilingId}", created.Id);

            return Result<Form24QFilingDto>.Success(await MapToDto(created));
        }

        public async Task<Result<IEnumerable<Form24QFilingSummaryDto>>> GetCorrectionsAsync(Guid originalFilingId)
        {
            var corrections = await _filingRepository.GetCorrectionsAsync(originalFilingId);
            return Result<IEnumerable<Form24QFilingSummaryDto>>.Success(
                corrections.Select(f => MapToSummaryDto(f)));
        }

        // ==================== Delete Operations ====================

        public async Task<Result<bool>> DeleteDraftAsync(Guid filingId)
        {
            _logger.LogInformation("Deleting draft Form 24Q filing {FilingId}", filingId);

            var filing = await _filingRepository.GetByIdAsync(filingId);
            if (filing == null)
            {
                return Error.NotFound("Filing not found");
            }

            if (filing.Status != "draft")
            {
                return Error.Validation("Only draft filings can be deleted");
            }

            await _filingRepository.DeleteAsync(filingId);

            return Result<bool>.Success(true);
        }

        // ==================== Helper Methods ====================

        private async Task<Form24QFilingDto> MapToDto(Form24QFiling filing)
        {
            var company = await _companiesRepository.GetByIdAsync(filing.CompanyId);
            var dueDate = GetDueDate(filing.FinancialYear, filing.Quarter);

            var validationErrors = string.IsNullOrEmpty(filing.ValidationErrors)
                ? new List<Core.Interfaces.Tax.ValidationError>()
                : JsonSerializer.Deserialize<List<Core.Interfaces.Tax.ValidationError>>(filing.ValidationErrors) ?? new();

            var validationWarnings = string.IsNullOrEmpty(filing.ValidationWarnings)
                ? new List<Core.Interfaces.Tax.ValidationWarning>()
                : JsonSerializer.Deserialize<List<Core.Interfaces.Tax.ValidationWarning>>(filing.ValidationWarnings) ?? new();

            return new Form24QFilingDto
            {
                Id = filing.Id,
                CompanyId = filing.CompanyId,
                CompanyName = company?.Name ?? "",
                FinancialYear = filing.FinancialYear,
                Quarter = filing.Quarter,
                Tan = filing.Tan,
                FormType = filing.FormType,
                OriginalFilingId = filing.OriginalFilingId,
                RevisionNumber = filing.RevisionNumber,
                TotalEmployees = filing.TotalEmployees,
                TotalSalaryPaid = filing.TotalSalaryPaid,
                TotalTdsDeducted = filing.TotalTdsDeducted,
                TotalTdsDeposited = filing.TotalTdsDeposited,
                Variance = filing.Variance,
                Status = filing.Status,
                HasValidationErrors = validationErrors.Any(),
                ValidationErrorCount = validationErrors.Count,
                ValidationWarningCount = validationWarnings.Count,
                HasFvuFile = !string.IsNullOrEmpty(filing.FvuFilePath),
                FvuGeneratedAt = filing.FvuGeneratedAt,
                FvuVersion = filing.FvuVersion,
                FilingDate = filing.FilingDate,
                AcknowledgementNumber = filing.AcknowledgementNumber,
                TokenNumber = filing.TokenNumber,
                ProvisionalReceiptNumber = filing.ProvisionalReceiptNumber,
                DueDate = dueDate,
                IsOverdue = IsOverdue(filing.Status, dueDate),
                CreatedAt = filing.CreatedAt,
                UpdatedAt = filing.UpdatedAt
            };
        }

        private Form24QFilingSummaryDto MapToSummaryDto(Form24QFiling filing)
        {
            var dueDate = GetDueDate(filing.FinancialYear, filing.Quarter);

            return new Form24QFilingSummaryDto
            {
                Id = filing.Id,
                FinancialYear = filing.FinancialYear,
                Quarter = filing.Quarter,
                Tan = filing.Tan,
                FormType = filing.FormType,
                RevisionNumber = filing.RevisionNumber,
                TotalEmployees = filing.TotalEmployees,
                TotalTdsDeducted = filing.TotalTdsDeducted,
                TotalTdsDeposited = filing.TotalTdsDeposited,
                Variance = filing.Variance,
                Status = filing.Status,
                HasFvuFile = !string.IsNullOrEmpty(filing.FvuFilePath),
                AcknowledgementNumber = filing.AcknowledgementNumber,
                FilingDate = filing.FilingDate,
                DueDate = dueDate,
                IsOverdue = IsOverdue(filing.Status, dueDate),
                CreatedAt = filing.CreatedAt
            };
        }

        private static QuarterStatusDto MapQuarterStatus(QuarterStatus status)
        {
            return new QuarterStatusDto
            {
                Quarter = status.Quarter,
                Status = status.Status,
                HasFiling = status.HasFiling,
                IsOverdue = status.IsOverdue,
                DueDate = status.DueDate,
                TotalEmployees = status.TotalEmployees,
                TdsDeducted = status.TdsDeducted,
                TdsDeposited = status.TdsDeposited,
                AcknowledgementNumber = status.AcknowledgementNumber
            };
        }

        private static QuarterStatusDto GetEmptyQuarterStatus(string quarter, string financialYear)
        {
            return new QuarterStatusDto
            {
                Quarter = quarter,
                Status = "not_started",
                HasFiling = false,
                IsOverdue = IsOverdue("not_started", GetDueDate(financialYear, quarter)),
                DueDate = GetDueDate(financialYear, quarter),
                TotalEmployees = 0,
                TdsDeducted = 0,
                TdsDeposited = 0,
                AcknowledgementNumber = null
            };
        }

        private static DateOnly GetDueDate(string financialYear, string quarter)
        {
            // Parse FY: "2024-25" -> start year 2024
            var startYear = int.Parse(financialYear.Split('-')[0]);

            return quarter switch
            {
                "Q1" => new DateOnly(startYear, 7, 31),      // July 31
                "Q2" => new DateOnly(startYear, 10, 31),     // October 31
                "Q3" => new DateOnly(startYear + 1, 1, 31),  // January 31
                "Q4" => new DateOnly(startYear + 1, 5, 31),  // May 31
                _ => new DateOnly(startYear + 1, 5, 31)
            };
        }

        private static bool IsOverdue(string status, DateOnly dueDate)
        {
            if (status == "acknowledged" || status == "revised")
            {
                return false;
            }

            return DateOnly.FromDateTime(DateTime.Today) > dueDate;
        }
    }
}

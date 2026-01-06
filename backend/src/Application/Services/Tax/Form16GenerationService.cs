using System.Text.Json;
using Core.Common;
using Core.Entities.Tax;
using Core.Interfaces;
using Core.Interfaces.Payroll;
using Core.Interfaces.Tax;
using Microsoft.Extensions.Logging;

namespace Application.Services.Tax
{
    /// <summary>
    /// Service for Form 16 (TDS Certificate) generation and management.
    ///
    /// Implements:
    /// - Single Responsibility: Only handles Form 16 operations
    /// - Separation of Concerns: Delegates to specialized services for calculations
    /// - Open/Closed: Extensible for different tax regimes and FY rules
    ///
    /// Form 16 Structure:
    /// - Part A: Quarterly TDS deducted and deposited summary
    /// - Part B: Annual salary computation and tax calculation
    /// </summary>
    public class Form16GenerationService : IForm16Service
    {
        private readonly IForm16Repository _form16Repo;
        private readonly IPayrollTransactionRepository _payrollTxnRepo;
        private readonly IPayrollRunRepository _payrollRunRepo;
        private readonly IEmployeesRepository _employeesRepo;
        private readonly IEmployeePayrollInfoRepository _payrollInfoRepo;
        private readonly IEmployeeTaxDeclarationRepository _taxDeclarationRepo;
        private readonly ICompaniesRepository _companiesRepo;
        private readonly ICompanyStatutoryConfigRepository _statutoryConfigRepo;
        private readonly IStatutoryPaymentRepository _statutoryPaymentRepo;
        private readonly ILogger<Form16GenerationService> _logger;

        // Quarter month mappings (Indian FY: Apr-Mar)
        private static readonly Dictionary<string, int[]> QuarterMonths = new()
        {
            { "Q1", new[] { 4, 5, 6 } },
            { "Q2", new[] { 7, 8, 9 } },
            { "Q3", new[] { 10, 11, 12 } },
            { "Q4", new[] { 1, 2, 3 } }
        };

        public Form16GenerationService(
            IForm16Repository form16Repo,
            IPayrollTransactionRepository payrollTxnRepo,
            IPayrollRunRepository payrollRunRepo,
            IEmployeesRepository employeesRepo,
            IEmployeePayrollInfoRepository payrollInfoRepo,
            IEmployeeTaxDeclarationRepository taxDeclarationRepo,
            ICompaniesRepository companiesRepo,
            ICompanyStatutoryConfigRepository statutoryConfigRepo,
            IStatutoryPaymentRepository statutoryPaymentRepo,
            ILogger<Form16GenerationService> logger)
        {
            _form16Repo = form16Repo ?? throw new ArgumentNullException(nameof(form16Repo));
            _payrollTxnRepo = payrollTxnRepo ?? throw new ArgumentNullException(nameof(payrollTxnRepo));
            _payrollRunRepo = payrollRunRepo ?? throw new ArgumentNullException(nameof(payrollRunRepo));
            _employeesRepo = employeesRepo ?? throw new ArgumentNullException(nameof(employeesRepo));
            _payrollInfoRepo = payrollInfoRepo ?? throw new ArgumentNullException(nameof(payrollInfoRepo));
            _taxDeclarationRepo = taxDeclarationRepo ?? throw new ArgumentNullException(nameof(taxDeclarationRepo));
            _companiesRepo = companiesRepo ?? throw new ArgumentNullException(nameof(companiesRepo));
            _statutoryConfigRepo = statutoryConfigRepo ?? throw new ArgumentNullException(nameof(statutoryConfigRepo));
            _statutoryPaymentRepo = statutoryPaymentRepo ?? throw new ArgumentNullException(nameof(statutoryPaymentRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== Generation Operations ====================

        public async Task<Result<Form16GenerationResult>> GenerateForEmployeeAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear,
            Guid? generatedBy = null)
        {
            _logger.LogInformation(
                "Generating Form 16 for Employee {EmployeeId} FY {FinancialYear}",
                employeeId, financialYear);

            try
            {
                // Validate inputs
                var validation = await ValidateAsync(companyId, employeeId, financialYear);
                if (!validation.IsSuccess || !validation.Value!.IsValid)
                {
                    var errorMsg = validation.IsSuccess
                        ? string.Join(", ", validation.Value!.Errors)
                        : validation.Error?.Message ?? "Validation failed";

                    return Error.Validation(errorMsg);
                }

                // Get required data
                var company = await _companiesRepo.GetByIdAsync(companyId);
                var employee = await _employeesRepo.GetByIdAsync(employeeId);
                var payrollInfo = await _payrollInfoRepo.GetByEmployeeIdAsync(employeeId);
                var taxDeclaration = await _taxDeclarationRepo.GetByEmployeeAndYearAsync(
                    employeeId, financialYear);

                if (company == null || employee == null)
                {
                    return Error.NotFound("Company or Employee not found");
                }

                // Get statutory config for TAN
                var statutoryConfig = await _statutoryConfigRepo.GetByCompanyIdAsync(companyId);

                // Check for existing Form 16
                var existingForm16 = await _form16Repo.GetByEmployeeAndFyAsync(
                    companyId, employeeId, financialYear);

                // Get payroll transactions for the FY
                var payrollTransactions = await GetPayrollTransactionsForFyAsync(
                    companyId, employeeId, financialYear);

                if (!payrollTransactions.Any())
                {
                    return Error.Validation("No payroll transactions found for the financial year");
                }

                // Build Form 16 data
                var form16 = await BuildForm16DataAsync(
                    company, employee, payrollInfo, taxDeclaration,
                    statutoryConfig, payrollTransactions, financialYear);

                // Set workflow fields
                form16.Status = Form16Status.Generated;
                form16.GeneratedAt = DateTime.UtcNow;
                form16.GeneratedBy = generatedBy;

                // Generate certificate number
                if (existingForm16 != null)
                {
                    // Update existing
                    form16.Id = existingForm16.Id;
                    form16.CertificateNumber = existingForm16.CertificateNumber;
                    form16.CreatedAt = existingForm16.CreatedAt;
                    form16.CreatedBy = existingForm16.CreatedBy;
                    form16.UpdatedAt = DateTime.UtcNow;
                    form16.UpdatedBy = generatedBy;
                    await _form16Repo.UpdateAsync(form16);
                }
                else
                {
                    // Create new
                    form16.Id = Guid.NewGuid();
                    form16.CertificateNumber = await GenerateCertificateNumberAsync(
                        companyId, financialYear, statutoryConfig?.TanNumber ?? "");
                    form16.CreatedAt = DateTime.UtcNow;
                    form16.UpdatedAt = DateTime.UtcNow;
                    form16.CreatedBy = generatedBy;
                    await _form16Repo.AddAsync(form16);
                }

                _logger.LogInformation(
                    "Form 16 generated successfully for Employee {EmployeeId} - Certificate {CertificateNumber}",
                    employeeId, form16.CertificateNumber);

                return Result<Form16GenerationResult>.Success(new Form16GenerationResult
                {
                    Form16Id = form16.Id,
                    CertificateNumber = form16.CertificateNumber,
                    EmployeeId = employeeId,
                    EmployeeName = employee.EmployeeName,
                    FinancialYear = financialYear,
                    TotalTdsDeducted = form16.TotalTdsDeducted,
                    TaxableIncome = form16.TaxableIncome,
                    IsSuccess = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Form 16 for Employee {EmployeeId}", employeeId);
                return Error.Internal($"Failed to generate Form 16: {ex.Message}");
            }
        }

        public async Task<Result<BulkForm16GenerationResult>> GenerateBulkAsync(
            Guid companyId,
            string financialYear,
            Guid? generatedBy = null,
            bool regenerateExisting = false)
        {
            _logger.LogInformation(
                "Starting bulk Form 16 generation for Company {CompanyId} FY {FinancialYear}",
                companyId, financialYear);

            var result = new BulkForm16GenerationResult();
            var results = new List<Form16GenerationResult>();

            try
            {
                // Get all employees with payroll in this FY
                var employeeIds = await GetEmployeesWithPayrollAsync(companyId, financialYear);
                result.TotalEmployees = employeeIds.Count;

                foreach (var employeeId in employeeIds)
                {
                    try
                    {
                        // Check if already exists
                        if (!regenerateExisting)
                        {
                            var exists = await _form16Repo.ExistsAsync(companyId, employeeId, financialYear);
                            if (exists)
                            {
                                result.SkippedCount++;
                                continue;
                            }
                        }

                        var genResult = await GenerateForEmployeeAsync(
                            companyId, employeeId, financialYear, generatedBy);

                        if (genResult.IsSuccess)
                        {
                            results.Add(genResult.Value!);
                            result.SuccessCount++;
                        }
                        else
                        {
                            results.Add(new Form16GenerationResult
                            {
                                EmployeeId = employeeId,
                                FinancialYear = financialYear,
                                IsSuccess = false,
                                ErrorMessage = genResult.Error?.Message
                            });
                            result.FailedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating Form 16 for Employee {EmployeeId}", employeeId);
                        result.FailedCount++;
                        result.Errors.Add($"Employee {employeeId}: {ex.Message}");
                    }
                }

                result.Results = results;

                _logger.LogInformation(
                    "Bulk Form 16 generation completed: {Success}/{Total} successful, {Failed} failed, {Skipped} skipped",
                    result.SuccessCount, result.TotalEmployees, result.FailedCount, result.SkippedCount);

                return Result<BulkForm16GenerationResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk Form 16 generation");
                return Error.Internal($"Bulk generation failed: {ex.Message}");
            }
        }

        public async Task<Result<Form16PreviewData>> PreviewAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear)
        {
            try
            {
                var company = await _companiesRepo.GetByIdAsync(companyId);
                var employee = await _employeesRepo.GetByIdAsync(employeeId);
                var payrollInfo = await _payrollInfoRepo.GetByEmployeeIdAsync(employeeId);
                var taxDeclaration = await _taxDeclarationRepo.GetByEmployeeAndYearAsync(employeeId, financialYear);
                var statutoryConfig = await _statutoryConfigRepo.GetByCompanyIdAsync(companyId);

                if (company == null || employee == null)
                {
                    return Error.NotFound("Company or Employee not found");
                }

                var payrollTransactions = await GetPayrollTransactionsForFyAsync(
                    companyId, employeeId, financialYear);

                if (!payrollTransactions.Any())
                {
                    return Error.Validation("No payroll transactions found");
                }

                var form16Entity = await BuildForm16DataAsync(
                    company, employee, payrollInfo, taxDeclaration,
                    statutoryConfig, payrollTransactions, financialYear);

                var existingForm16 = await _form16Repo.GetByEmployeeAndFyAsync(
                    companyId, employeeId, financialYear);

                var preview = new Form16PreviewData
                {
                    EmployeeId = employeeId,
                    EmployeeName = employee.EmployeeName,
                    FinancialYear = financialYear,
                    ComputedData = MapToDto(form16Entity),
                    ExistingForm16 = existingForm16 != null ? MapToDto(existingForm16) : null,
                    HasChanges = existingForm16 != null && HasDataChanges(existingForm16, form16Entity)
                };

                if (preview.HasChanges && existingForm16 != null)
                {
                    preview.Changes = GetDataChanges(existingForm16, form16Entity);
                }

                return Result<Form16PreviewData>.Success(preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing Form 16");
                return Error.Internal($"Preview failed: {ex.Message}");
            }
        }

        // ==================== Retrieval Operations ====================

        public async Task<Result<Form16Dto>> GetByIdAsync(Guid id)
        {
            var form16 = await _form16Repo.GetByIdAsync(id);
            if (form16 == null)
            {
                return Error.NotFound($"Form 16 with ID {id} not found");
            }
            return Result<Form16Dto>.Success(MapToDto(form16));
        }

        public async Task<Result<Form16Dto>> GetByEmployeeAndFyAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear)
        {
            var form16 = await _form16Repo.GetByEmployeeAndFyAsync(companyId, employeeId, financialYear);
            if (form16 == null)
            {
                return Error.NotFound($"Form 16 not found for employee {employeeId} FY {financialYear}");
            }
            return Result<Form16Dto>.Success(MapToDto(form16));
        }

        public async Task<Result<PagedResult<Form16SummaryDto>>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false)
        {
            var (items, totalCount) = await _form16Repo.GetPagedAsync(
                companyId, pageNumber, pageSize, financialYear, status, searchTerm, sortBy, sortDescending);

            var summaries = items.Select(MapToSummaryDto).ToList();

            return Result<PagedResult<Form16SummaryDto>>.Success(new PagedResult<Form16SummaryDto>
            {
                Items = summaries,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        public async Task<Result<Form16StatisticsDto>> GetStatisticsAsync(
            Guid companyId,
            string financialYear)
        {
            var stats = await _form16Repo.GetStatisticsAsync(companyId, financialYear);

            return Result<Form16StatisticsDto>.Success(new Form16StatisticsDto
            {
                FinancialYear = financialYear,
                TotalEmployees = stats.TotalEmployees,
                EligibleEmployees = stats.TotalEmployees,
                Form16Generated = stats.Form16Generated,
                Form16Verified = stats.Form16Verified,
                Form16Issued = stats.Form16Issued,
                Form16Pending = stats.Form16Pending,
                TotalTdsDeducted = stats.TotalTdsDeducted,
                TotalTdsDeposited = stats.TotalTdsDeposited
            });
        }

        // ==================== PDF Operations ====================

        public async Task<Result<Form16PdfResult>> GeneratePdfAsync(Guid form16Id, Guid? generatedBy = null)
        {
            var form16 = await _form16Repo.GetByIdAsync(form16Id);
            if (form16 == null)
            {
                return Error.NotFound($"Form 16 with ID {form16Id} not found");
            }

            // TODO: Implement PDF generation with a PDF library (QuestPDF, iTextSharp, etc.)
            // For now, return placeholder
            _logger.LogWarning("PDF generation not yet implemented - returning placeholder");

            var pdfPath = $"/forms/form16/{form16.FinancialYear}/{form16Id}.pdf";

            form16.PdfPath = pdfPath;
            form16.UpdatedAt = DateTime.UtcNow;
            form16.UpdatedBy = generatedBy;
            await _form16Repo.UpdateAsync(form16);

            return Result<Form16PdfResult>.Success(new Form16PdfResult
            {
                Form16Id = form16Id,
                PdfPath = pdfPath,
                FileSizeBytes = 0,
                GeneratedAt = DateTime.UtcNow
            });
        }

        public async Task<Result<BulkPdfGenerationResult>> GenerateBulkPdfAsync(
            Guid companyId,
            string financialYear,
            Guid? generatedBy = null)
        {
            var forms = await _form16Repo.GetByFinancialYearAsync(companyId, financialYear);
            var result = new BulkPdfGenerationResult
            {
                TotalForms = forms.Count()
            };

            foreach (var form16 in forms.Where(f => f.Status != Form16Status.Cancelled))
            {
                var pdfResult = await GeneratePdfAsync(form16.Id, generatedBy);
                if (pdfResult.IsSuccess)
                {
                    result.Results.Add(pdfResult.Value!);
                    result.SuccessCount++;
                }
                else
                {
                    result.FailedCount++;
                    result.Errors.Add($"Form 16 {form16.CertificateNumber}: {pdfResult.Error?.Message}");
                }
            }

            return Result<BulkPdfGenerationResult>.Success(result);
        }

        public async Task<Result<Stream>> DownloadPdfAsync(Guid form16Id)
        {
            var form16 = await _form16Repo.GetByIdAsync(form16Id);
            if (form16 == null)
            {
                return Error.NotFound($"Form 16 with ID {form16Id} not found");
            }

            if (string.IsNullOrEmpty(form16.PdfPath))
            {
                return Error.Validation("PDF not yet generated for this Form 16");
            }

            // TODO: Implement actual file retrieval from storage
            return Error.Internal("PDF download not yet implemented");
        }

        // ==================== Workflow Operations ====================

        public async Task<Result<Form16Dto>> VerifyAsync(Guid form16Id, VerifyForm16Request request)
        {
            var form16 = await _form16Repo.GetByIdAsync(form16Id);
            if (form16 == null)
            {
                return Error.NotFound($"Form 16 with ID {form16Id} not found");
            }

            if (form16.Status != Form16Status.Generated)
            {
                return Error.Validation($"Cannot verify Form 16 in status '{form16.Status}'");
            }

            form16.Status = Form16Status.Verified;
            form16.VerifiedAt = DateTime.UtcNow;
            form16.VerifiedBy = request.VerifiedBy;
            form16.VerifiedByName = request.VerifierName;
            form16.VerifiedByDesignation = request.VerifierDesignation;
            form16.Place = request.Place;
            form16.SignatureDate = DateOnly.FromDateTime(DateTime.Today);
            form16.UpdatedAt = DateTime.UtcNow;
            form16.UpdatedBy = request.VerifiedBy;

            await _form16Repo.UpdateAsync(form16);

            _logger.LogInformation("Form 16 {CertificateNumber} verified", form16.CertificateNumber);

            return Result<Form16Dto>.Success(MapToDto(form16));
        }

        public async Task<Result<Form16Dto>> IssueAsync(Guid form16Id, IssueForm16Request request)
        {
            var form16 = await _form16Repo.GetByIdAsync(form16Id);
            if (form16 == null)
            {
                return Error.NotFound($"Form 16 with ID {form16Id} not found");
            }

            // Allow issuing from 'generated' or 'verified' status (skip verification for simpler workflows)
            if (form16.Status != Form16Status.Generated && form16.Status != Form16Status.Verified)
            {
                return Error.Validation($"Cannot issue Form 16 in status '{form16.Status}'. Must be generated or verified first.");
            }

            form16.Status = Form16Status.Issued;
            form16.IssuedAt = DateTime.UtcNow;
            form16.IssuedBy = request.IssuedBy;
            form16.UpdatedAt = DateTime.UtcNow;
            form16.UpdatedBy = request.IssuedBy;

            await _form16Repo.UpdateAsync(form16);

            // TODO: Send email to employee if requested
            if (request.SendEmail)
            {
                _logger.LogInformation("Email notification requested for Form 16 {CertificateNumber}", form16.CertificateNumber);
            }

            _logger.LogInformation("Form 16 {CertificateNumber} issued", form16.CertificateNumber);

            return Result<Form16Dto>.Success(MapToDto(form16));
        }

        public async Task<Result> CancelAsync(Guid form16Id, string reason, Guid cancelledBy)
        {
            var form16 = await _form16Repo.GetByIdAsync(form16Id);
            if (form16 == null)
            {
                return Error.NotFound($"Form 16 with ID {form16Id} not found");
            }

            if (form16.Status == Form16Status.Issued)
            {
                return Error.Validation("Cannot cancel an issued Form 16");
            }

            form16.Status = Form16Status.Cancelled;
            form16.UpdatedAt = DateTime.UtcNow;
            form16.UpdatedBy = cancelledBy;

            await _form16Repo.UpdateAsync(form16);

            _logger.LogInformation("Form 16 {CertificateNumber} cancelled. Reason: {Reason}",
                form16.CertificateNumber, reason);

            return Result.Success();
        }

        public async Task<Result<Form16GenerationResult>> RegenerateAsync(
            Guid form16Id,
            Guid? regeneratedBy = null)
        {
            var form16 = await _form16Repo.GetByIdAsync(form16Id);
            if (form16 == null)
            {
                return Error.NotFound($"Form 16 with ID {form16Id} not found");
            }

            if (form16.Status == Form16Status.Issued)
            {
                return Error.Validation("Cannot regenerate an issued Form 16");
            }

            return await GenerateForEmployeeAsync(
                form16.CompanyId, form16.EmployeeId, form16.FinancialYear, regeneratedBy);
        }

        // ==================== Validation ====================

        public async Task<Result<Form16ValidationResult>> ValidateAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear)
        {
            var result = new Form16ValidationResult { IsValid = true };

            // Check company
            var company = await _companiesRepo.GetByIdAsync(companyId);
            if (company == null)
            {
                result.Errors.Add("Company not found");
                result.IsValid = false;
                return Result<Form16ValidationResult>.Success(result);
            }

            // Check employee
            var employee = await _employeesRepo.GetByIdAsync(employeeId);
            if (employee == null)
            {
                result.Errors.Add("Employee not found");
                result.IsValid = false;
                return Result<Form16ValidationResult>.Success(result);
            }

            // Check employee PAN
            var payrollInfo = await _payrollInfoRepo.GetByEmployeeIdAsync(employeeId);
            result.HasEmployeePan = !string.IsNullOrEmpty(payrollInfo?.PanNumber) ||
                                   !string.IsNullOrEmpty(employee.PanNumber);
            if (!result.HasEmployeePan)
            {
                result.Warnings.Add("Employee PAN not available - required for Form 16");
            }

            // Check company TAN
            var statutoryConfig = await _statutoryConfigRepo.GetByCompanyIdAsync(companyId);
            result.HasCompanyTan = !string.IsNullOrEmpty(statutoryConfig?.TanNumber);
            if (!result.HasCompanyTan)
            {
                result.Errors.Add("Company TAN not configured - required for Form 16");
                result.IsValid = false;
            }

            // Check payroll data
            var transactions = await GetPayrollTransactionsForFyAsync(companyId, employeeId, financialYear);
            result.HasPayrollData = transactions.Any();
            result.MonthsWithPayroll = transactions.Select(t => t.PayrollMonth).Distinct().Count();

            if (!result.HasPayrollData)
            {
                result.Errors.Add("No payroll transactions found for the financial year");
                result.IsValid = false;
            }

            // Check TDS data
            result.HasTdsData = transactions.Any(t => t.TdsDeducted > 0);
            if (!result.HasTdsData)
            {
                result.Warnings.Add("No TDS deducted in this financial year");
            }

            return Result<Form16ValidationResult>.Success(result);
        }

        public async Task<Result<bool>> CanGenerateAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear)
        {
            var validation = await ValidateAsync(companyId, employeeId, financialYear);
            return Result<bool>.Success(validation.IsSuccess && validation.Value!.IsValid);
        }

        // ==================== Private Helper Methods ====================

        private async Task<Form16> BuildForm16DataAsync(
            Core.Entities.Companies company,
            Core.Entities.Employees employee,
            Core.Entities.Payroll.EmployeePayrollInfo? payrollInfo,
            Core.Entities.Payroll.EmployeeTaxDeclaration? taxDeclaration,
            Core.Entities.Payroll.CompanyStatutoryConfig? statutoryConfig,
            IEnumerable<Core.Entities.Payroll.PayrollTransaction> transactions,
            string financialYear)
        {
            var txnList = transactions.ToList();
            var (startYear, endYear) = ParseFinancialYear(financialYear);

            // Determine employment period from transactions
            var periodFrom = DateOnly.FromDateTime(txnList.Min(t =>
                new DateTime(t.PayrollYear, t.PayrollMonth, 1)));
            var periodTo = DateOnly.FromDateTime(txnList.Max(t =>
                new DateTime(t.PayrollYear, t.PayrollMonth, DateTime.DaysInMonth(t.PayrollYear, t.PayrollMonth))));

            // Calculate quarterly TDS
            var q1Tds = CalculateQuarterlyTds(txnList, "Q1", startYear);
            var q2Tds = CalculateQuarterlyTds(txnList, "Q2", startYear);
            var q3Tds = CalculateQuarterlyTds(txnList, "Q3", startYear);
            var q4Tds = CalculateQuarterlyTds(txnList, "Q4", endYear);

            // Calculate salary components
            var salaryBreakdown = CalculateSalaryBreakdown(txnList);

            // Calculate deductions
            var deductionsBreakdown = CalculateDeductionsBreakdown(txnList, taxDeclaration, payrollInfo);

            // Determine tax regime
            var taxRegime = payrollInfo?.TaxRegime ?? taxDeclaration?.TaxRegime ?? "new";

            // Calculate tax
            var taxComputation = CalculateTaxComputation(
                salaryBreakdown.GrossSalary,
                deductionsBreakdown.TotalDeductions,
                taxDeclaration,
                taxRegime);

            var form16 = new Form16
            {
                CompanyId = company.Id,
                EmployeeId = employee.Id,
                FinancialYear = financialYear,

                // Deductor (Company) details
                Tan = statutoryConfig?.TanNumber ?? "",
                DeductorPan = company.PanNumber ?? "",
                DeductorName = company.Name,
                DeductorAddress = company.AddressLine1 ?? "",
                DeductorCity = company.City ?? "",
                DeductorState = company.State ?? "",
                DeductorPincode = company.ZipCode ?? "",
                DeductorEmail = company.Email,
                DeductorPhone = company.Phone,

                // Employee details
                EmployeePan = payrollInfo?.PanNumber ?? employee.PanNumber ?? "",
                EmployeeName = employee.EmployeeName,
                EmployeeAddress = employee.AddressLine1 ?? "",
                EmployeeCity = employee.City ?? "",
                EmployeeState = employee.State ?? "",
                EmployeePincode = employee.ZipCode ?? "",
                EmployeeEmail = employee.Email,

                // Period
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,

                // Quarterly TDS
                Q1TdsDeducted = q1Tds.TdsDeducted,
                Q1TdsDeposited = q1Tds.TdsDeposited,
                Q1ChallanDetails = JsonSerializer.Serialize(q1Tds.Challans),
                Q2TdsDeducted = q2Tds.TdsDeducted,
                Q2TdsDeposited = q2Tds.TdsDeposited,
                Q2ChallanDetails = JsonSerializer.Serialize(q2Tds.Challans),
                Q3TdsDeducted = q3Tds.TdsDeducted,
                Q3TdsDeposited = q3Tds.TdsDeposited,
                Q3ChallanDetails = JsonSerializer.Serialize(q3Tds.Challans),
                Q4TdsDeducted = q4Tds.TdsDeducted,
                Q4TdsDeposited = q4Tds.TdsDeposited,
                Q4ChallanDetails = JsonSerializer.Serialize(q4Tds.Challans),
                TotalTdsDeducted = q1Tds.TdsDeducted + q2Tds.TdsDeducted + q3Tds.TdsDeducted + q4Tds.TdsDeducted,
                TotalTdsDeposited = q1Tds.TdsDeposited + q2Tds.TdsDeposited + q3Tds.TdsDeposited + q4Tds.TdsDeposited,

                // Salary details
                GrossSalary = salaryBreakdown.GrossSalary,
                Perquisites = 0, // TODO: Get from perquisites tracking
                ProfitsInLieu = 0,
                TotalSalary = salaryBreakdown.GrossSalary,

                // Exemptions
                HraExemption = deductionsBreakdown.HraExemption,
                LtaExemption = 0, // TODO: Calculate LTA exemption
                OtherExemptions = 0,
                TotalExemptions = deductionsBreakdown.HraExemption,

                // Deductions
                StandardDeduction = deductionsBreakdown.StandardDeduction,
                ProfessionalTax = deductionsBreakdown.ProfessionalTax,
                Section80C = deductionsBreakdown.Section80C,
                Section80CCD1B = deductionsBreakdown.Section80CCD1B,
                Section80D = deductionsBreakdown.Section80D,
                Section80E = deductionsBreakdown.Section80E,
                Section80G = deductionsBreakdown.Section80G,
                Section80TTA = deductionsBreakdown.Section80TTA,
                Section24 = deductionsBreakdown.Section24,
                OtherDeductions = deductionsBreakdown.OtherDeductions,
                TotalDeductions = deductionsBreakdown.TotalDeductions,

                // Tax computation
                TaxRegime = taxRegime,
                TaxableIncome = taxComputation.TaxableIncome,
                TaxOnIncome = taxComputation.TaxOnIncome,
                Rebate87A = taxComputation.Rebate87A,
                TaxAfterRebate = taxComputation.TaxAfterRebate,
                Surcharge = taxComputation.Surcharge,
                Cess = taxComputation.Cess,
                TotalTaxLiability = taxComputation.TotalTax,
                Relief89 = taxComputation.Relief89,
                NetTaxPayable = taxComputation.NetTax,

                // Previous employer details
                PreviousEmployerIncome = taxDeclaration?.PrevEmployerIncome ?? 0,
                PreviousEmployerTds = taxDeclaration?.PrevEmployerTds ?? 0,
                OtherIncome = taxDeclaration?.OtherIncomeAnnual ?? 0,

                // Detailed breakdowns as JSON
                SalaryBreakdownJson = JsonSerializer.Serialize(salaryBreakdown),
                TaxComputationJson = JsonSerializer.Serialize(taxComputation)
            };

            return form16;
        }

        private QuarterlyTdsSummary CalculateQuarterlyTds(
            List<Core.Entities.Payroll.PayrollTransaction> transactions,
            string quarter,
            int year)
        {
            var months = QuarterMonths[quarter];
            var quarterTxns = transactions.Where(t =>
                t.PayrollYear == year && months.Contains(t.PayrollMonth)).ToList();

            return new QuarterlyTdsSummary
            {
                Quarter = quarter,
                TdsDeducted = quarterTxns.Sum(t => t.TdsDeducted),
                TdsDeposited = quarterTxns.Sum(t => t.TdsDeducted), // Assume deposited = deducted
                Challans = new List<ChallanDetail>() // TODO: Get actual challan details
            };
        }

        private SalaryBreakdown CalculateSalaryBreakdown(
            List<Core.Entities.Payroll.PayrollTransaction> transactions)
        {
            return new SalaryBreakdown
            {
                BasicSalary = transactions.Sum(t => t.BasicEarned),
                Hra = transactions.Sum(t => t.HraEarned),
                DearnessAllowance = transactions.Sum(t => t.DaEarned),
                ConveyanceAllowance = transactions.Sum(t => t.ConveyanceEarned),
                MedicalAllowance = transactions.Sum(t => t.MedicalEarned),
                SpecialAllowance = transactions.Sum(t => t.SpecialAllowanceEarned),
                OtherAllowances = transactions.Sum(t => t.OtherAllowancesEarned),
                Bonus = transactions.Sum(t => t.BonusPaid),
                Lta = transactions.Sum(t => t.LtaPaid),
                Arrears = transactions.Sum(t => t.Arrears),
                Reimbursements = transactions.Sum(t => t.Reimbursements),
                GrossSalary = transactions.Sum(t => t.GrossEarnings)
            };
        }

        private DeductionsBreakdown CalculateDeductionsBreakdown(
            List<Core.Entities.Payroll.PayrollTransaction> transactions,
            Core.Entities.Payroll.EmployeeTaxDeclaration? taxDeclaration,
            Core.Entities.Payroll.EmployeePayrollInfo? payrollInfo)
        {
            var taxRegime = payrollInfo?.TaxRegime ?? taxDeclaration?.TaxRegime ?? "new";
            var isOldRegime = taxRegime == "old";

            // Standard deduction
            var standardDeduction = taxRegime == "new" ? 75000m : 50000m;

            // Professional Tax from payroll
            var professionalTax = transactions.Sum(t => t.ProfessionalTax);

            // Section 80C - from declarations or estimate from PF
            var section80C = 0m;
            if (isOldRegime && taxDeclaration != null)
            {
                section80C = taxDeclaration.Sec80cPpf +
                            taxDeclaration.Sec80cElss +
                            taxDeclaration.Sec80cLifeInsurance +
                            taxDeclaration.Sec80cHomeLoanPrincipal +
                            taxDeclaration.Sec80cChildrenTuition +
                            taxDeclaration.Sec80cNsc +
                            taxDeclaration.Sec80cSukanyaSamriddhi +
                            taxDeclaration.Sec80cFixedDeposit +
                            taxDeclaration.Sec80cOthers;
                section80C = Math.Min(section80C, 150000m); // Cap at 1.5L
            }

            // Add employee PF contribution to 80C
            var employeePf = transactions.Sum(t => t.PfEmployee);
            if (isOldRegime)
            {
                section80C = Math.Min(section80C + employeePf, 150000m);
            }

            // Other deductions from declaration
            var section80CCD1B = isOldRegime && taxDeclaration != null ? Math.Min(taxDeclaration.Sec80ccdNps, 50000m) : 0;
            var section80D = isOldRegime ? CalculateSection80D(taxDeclaration) : 0;
            var section80E = isOldRegime && taxDeclaration != null ? taxDeclaration.Sec80eEducationLoan : 0;
            var section80G = isOldRegime && taxDeclaration != null ? taxDeclaration.Sec80gDonations * 0.5m : 0; // 50% deduction
            var section80TTA = isOldRegime && taxDeclaration != null ? Math.Min(taxDeclaration.Sec80ttaSavingsInterest, 10000m) : 0;
            var section24 = isOldRegime && taxDeclaration != null ? Math.Min(taxDeclaration.Sec24HomeLoanInterest, 200000m) : 0;

            // HRA Exemption
            var hraExemption = 0m;
            if (isOldRegime && taxDeclaration != null && taxDeclaration.HraRentPaidAnnual > 0)
            {
                var basicAnnual = transactions.Sum(t => t.BasicEarned);
                var hraReceived = transactions.Sum(t => t.HraEarned);
                var rentPaid = taxDeclaration.HraRentPaidAnnual;
                var isMetro = taxDeclaration.HraMetroCity;

                // HRA exemption = Min(Actual HRA, Rent - 10% Basic, 50%/40% of Basic)
                var option1 = hraReceived;
                var option2 = rentPaid - (0.1m * basicAnnual);
                var option3 = (isMetro ? 0.5m : 0.4m) * basicAnnual;

                hraExemption = Math.Max(0, Math.Min(option1, Math.Min(option2, option3)));
            }

            var totalDeductions = standardDeduction + professionalTax + section80C + section80CCD1B +
                                 section80D + section80E + section80G + section80TTA + section24 + hraExemption;

            return new DeductionsBreakdown
            {
                StandardDeduction = standardDeduction,
                ProfessionalTax = professionalTax,
                Section80C = section80C,
                Section80CCD1B = section80CCD1B,
                Section80D = section80D,
                Section80E = section80E,
                Section80G = section80G,
                Section80TTA = section80TTA,
                Section24 = section24,
                HraExemption = hraExemption,
                OtherDeductions = 0,
                TotalDeductions = totalDeductions
            };
        }

        private decimal CalculateSection80D(Core.Entities.Payroll.EmployeeTaxDeclaration? declaration)
        {
            if (declaration == null) return 0;

            var selfFamily = Math.Min(declaration.Sec80dSelfFamily,
                declaration.Sec80dSelfSeniorCitizen ? 50000m : 25000m);
            var parents = Math.Min(declaration.Sec80dParents,
                declaration.Sec80dParentsSeniorCitizen ? 50000m : 25000m);
            var preventive = Math.Min(declaration.Sec80dPreventiveCheckup, 5000m);

            return selfFamily + parents + preventive;
        }

        private TaxComputationBreakdown CalculateTaxComputation(
            decimal grossSalary,
            decimal totalDeductions,
            Core.Entities.Payroll.EmployeeTaxDeclaration? declaration,
            string taxRegime)
        {
            var grossIncome = grossSalary +
                             (declaration?.PrevEmployerIncome ?? 0) +
                             (declaration?.OtherIncomeAnnual ?? 0);

            var taxableIncome = Math.Max(0, grossIncome - totalDeductions);

            // Calculate tax based on regime
            var (taxOnIncome, slabBreakdown) = CalculateTaxOnSlabs(taxableIncome, taxRegime);

            // Section 87A Rebate
            var rebateThreshold = taxRegime == "new" ? 1200000m : 500000m;
            var maxRebate = taxRegime == "new" ? 25000m : 12500m;
            var rebate87A = taxableIncome <= rebateThreshold ? Math.Min(taxOnIncome, maxRebate) : 0;

            var taxAfterRebate = Math.Max(0, taxOnIncome - rebate87A);

            // Surcharge (simplified - applies > 50L)
            var surcharge = 0m;
            if (taxableIncome > 5000000m)
            {
                surcharge = taxAfterRebate * (taxableIncome > 10000000m ? 0.15m : 0.10m);
            }

            // Cess (4%)
            var cess = (taxAfterRebate + surcharge) * 0.04m;

            var totalTax = taxAfterRebate + surcharge + cess;

            return new TaxComputationBreakdown
            {
                GrossIncome = grossIncome,
                TotalDeductions = totalDeductions,
                TaxableIncome = taxableIncome,
                SlabBreakdown = slabBreakdown,
                TaxOnIncome = taxOnIncome,
                Rebate87A = rebate87A,
                TaxAfterRebate = taxAfterRebate,
                Surcharge = surcharge,
                SurchargeRelief = 0, // TODO: Calculate marginal relief
                Cess = cess,
                TotalTax = totalTax,
                Relief89 = 0,
                NetTax = totalTax
            };
        }

        private (decimal Tax, List<Core.Interfaces.Tax.TaxSlabBreakdown> Breakdown) CalculateTaxOnSlabs(
            decimal taxableIncome,
            string regime)
        {
            var breakdown = new List<Core.Interfaces.Tax.TaxSlabBreakdown>();
            decimal totalTax = 0;

            // New Regime FY 2024-25 slabs
            var slabs = regime == "new"
                ? new[] {
                    (0m, 300000m, 0m),
                    (300000m, 700000m, 0.05m),
                    (700000m, 1000000m, 0.10m),
                    (1000000m, 1200000m, 0.15m),
                    (1200000m, 1500000m, 0.20m),
                    (1500000m, decimal.MaxValue, 0.30m)
                }
                : new[] {
                    (0m, 250000m, 0m),
                    (250000m, 500000m, 0.05m),
                    (500000m, 1000000m, 0.20m),
                    (1000000m, decimal.MaxValue, 0.30m)
                };

            var remainingIncome = taxableIncome;

            foreach (var (from, to, rate) in slabs)
            {
                if (remainingIncome <= 0) break;

                var slabAmount = Math.Min(remainingIncome, to - from);
                var slabTax = slabAmount * rate;

                if (slabAmount > 0)
                {
                    breakdown.Add(new Core.Interfaces.Tax.TaxSlabBreakdown
                    {
                        FromAmount = from,
                        ToAmount = Math.Min(to, from + slabAmount),
                        Rate = rate * 100,
                        TaxableAmount = slabAmount,
                        Tax = slabTax
                    });
                }

                totalTax += slabTax;
                remainingIncome -= slabAmount;
            }

            return (totalTax, breakdown);
        }

        private async Task<IEnumerable<Core.Entities.Payroll.PayrollTransaction>> GetPayrollTransactionsForFyAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear)
        {
            var (startYear, endYear) = ParseFinancialYear(financialYear);

            // FY runs Apr (startYear) to Mar (endYear)
            var allTransactions = await _payrollTxnRepo.GetByEmployeeIdAsync(employeeId);

            return allTransactions.Where(t =>
                (t.PayrollYear == startYear && t.PayrollMonth >= 4) ||
                (t.PayrollYear == endYear && t.PayrollMonth <= 3));
        }

        private async Task<List<Guid>> GetEmployeesWithPayrollAsync(Guid companyId, string financialYear)
        {
            var (startYear, endYear) = ParseFinancialYear(financialYear);

            // Get all payroll runs for the FY
            var runs = await _payrollRunRepo.GetByCompanyIdAsync(companyId);
            var fyRuns = runs.Where(r =>
                r.FinancialYear == financialYear &&
                r.Status != "draft" && r.Status != "cancelled").ToList();

            var employeeIds = new HashSet<Guid>();
            foreach (var run in fyRuns)
            {
                var transactions = await _payrollTxnRepo.GetByPayrollRunIdAsync(run.Id);
                foreach (var txn in transactions)
                {
                    employeeIds.Add(txn.EmployeeId);
                }
            }

            return employeeIds.ToList();
        }

        private async Task<string> GenerateCertificateNumberAsync(
            Guid companyId,
            string financialYear,
            string tan)
        {
            var serial = await _form16Repo.GetNextCertificateSerialAsync(companyId, financialYear);
            var tanPrefix = string.IsNullOrEmpty(tan) ? "NOTANXXXX" : tan;
            return $"{tanPrefix}/{financialYear}/{serial:D5}";
        }

        private (int StartYear, int EndYear) ParseFinancialYear(string financialYear)
        {
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = 2000 + int.Parse(parts[1]);
            return (startYear, endYear);
        }

        // ==================== Mapping Methods ====================

        private Form16Dto MapToDto(Form16 entity)
        {
            return new Form16Dto
            {
                Id = entity.Id,
                CompanyId = entity.CompanyId,
                EmployeeId = entity.EmployeeId,
                FinancialYear = entity.FinancialYear,
                CertificateNumber = entity.CertificateNumber,
                Tan = entity.Tan,
                DeductorPan = entity.DeductorPan,
                DeductorName = entity.DeductorName,
                DeductorAddress = entity.DeductorAddress,
                EmployeePan = entity.EmployeePan,
                EmployeeName = entity.EmployeeName,
                EmployeeAddress = entity.EmployeeAddress,
                PeriodFrom = entity.PeriodFrom,
                PeriodTo = entity.PeriodTo,
                QuarterlyTds = new List<QuarterlyTdsSummary>
                {
                    new() { Quarter = "Q1", TdsDeducted = entity.Q1TdsDeducted, TdsDeposited = entity.Q1TdsDeposited },
                    new() { Quarter = "Q2", TdsDeducted = entity.Q2TdsDeducted, TdsDeposited = entity.Q2TdsDeposited },
                    new() { Quarter = "Q3", TdsDeducted = entity.Q3TdsDeducted, TdsDeposited = entity.Q3TdsDeposited },
                    new() { Quarter = "Q4", TdsDeducted = entity.Q4TdsDeducted, TdsDeposited = entity.Q4TdsDeposited }
                },
                TotalTdsDeducted = entity.TotalTdsDeducted,
                TotalTdsDeposited = entity.TotalTdsDeposited,
                GrossSalary = entity.GrossSalary,
                Perquisites = entity.Perquisites,
                ProfitsInLieu = entity.ProfitsInLieu,
                TotalSalary = entity.TotalSalary,
                TotalExemptions = entity.TotalExemptions,
                TotalDeductions = entity.TotalDeductions,
                TaxRegime = entity.TaxRegime,
                TaxableIncome = entity.TaxableIncome,
                TaxOnIncome = entity.TaxOnIncome,
                Rebate87A = entity.Rebate87A,
                Surcharge = entity.Surcharge,
                Cess = entity.Cess,
                TotalTaxLiability = entity.TotalTaxLiability,
                Relief89 = entity.Relief89,
                NetTaxPayable = entity.NetTaxPayable,
                Status = entity.Status,
                GeneratedAt = entity.GeneratedAt,
                VerifiedAt = entity.VerifiedAt,
                IssuedAt = entity.IssuedAt,
                PdfPath = entity.PdfPath,
                SalaryBreakdown = !string.IsNullOrEmpty(entity.SalaryBreakdownJson)
                    ? JsonSerializer.Deserialize<SalaryBreakdown>(entity.SalaryBreakdownJson)
                    : null,
                TaxComputation = !string.IsNullOrEmpty(entity.TaxComputationJson)
                    ? JsonSerializer.Deserialize<TaxComputationBreakdown>(entity.TaxComputationJson)
                    : null
            };
        }

        private Form16SummaryDto MapToSummaryDto(Form16 entity)
        {
            return new Form16SummaryDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EmployeeName = entity.EmployeeName,
                EmployeePan = entity.EmployeePan,
                FinancialYear = entity.FinancialYear,
                CertificateNumber = entity.CertificateNumber,
                GrossSalary = entity.GrossSalary,
                TotalTdsDeducted = entity.TotalTdsDeducted,
                TaxableIncome = entity.TaxableIncome,
                TaxRegime = entity.TaxRegime,
                Status = entity.Status,
                GeneratedAt = entity.GeneratedAt,
                IssuedAt = entity.IssuedAt,
                HasPdf = !string.IsNullOrEmpty(entity.PdfPath)
            };
        }

        private bool HasDataChanges(Form16 existing, Form16 computed)
        {
            return existing.GrossSalary != computed.GrossSalary ||
                   existing.TotalTdsDeducted != computed.TotalTdsDeducted ||
                   existing.TaxableIncome != computed.TaxableIncome ||
                   existing.TotalDeductions != computed.TotalDeductions;
        }

        private List<string> GetDataChanges(Form16 existing, Form16 computed)
        {
            var changes = new List<string>();

            if (existing.GrossSalary != computed.GrossSalary)
                changes.Add($"Gross Salary: {existing.GrossSalary:N2} → {computed.GrossSalary:N2}");

            if (existing.TotalTdsDeducted != computed.TotalTdsDeducted)
                changes.Add($"TDS Deducted: {existing.TotalTdsDeducted:N2} → {computed.TotalTdsDeducted:N2}");

            if (existing.TaxableIncome != computed.TaxableIncome)
                changes.Add($"Taxable Income: {existing.TaxableIncome:N2} → {computed.TaxableIncome:N2}");

            if (existing.TotalDeductions != computed.TotalDeductions)
                changes.Add($"Deductions: {existing.TotalDeductions:N2} → {computed.TotalDeductions:N2}");

            return changes;
        }
    }
}

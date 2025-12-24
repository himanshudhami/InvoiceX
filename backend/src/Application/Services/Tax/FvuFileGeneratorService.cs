using System.Text;
using Application.DTOs.Tax.Fvu;
using Core.Common;
using Core.Interfaces.Tax;
using Microsoft.Extensions.Logging;

namespace Application.Services.Tax
{
    /// <summary>
    /// Service for generating NSDL-compliant FVU (File Validation Utility) text files
    /// for TDS returns (Form 24Q and 26Q).
    ///
    /// This service orchestrates the file generation process by:
    /// 1. Fetching TDS return data from ITdsReturnService
    /// 2. Validating the data for FVU compliance
    /// 3. Using IFvuRecordBuilder to format individual records
    /// 4. Assembling the complete file
    ///
    /// Single Responsibility: This class coordinates the file generation workflow.
    /// It delegates record building to IFvuRecordBuilder and data fetching to ITdsReturnService.
    /// </summary>
    public class FvuFileGeneratorService : IFvuFileGeneratorService
    {
        private readonly ITdsReturnService _tdsReturnService;
        private readonly IFvuRecordBuilder _recordBuilder;
        private readonly ILogger<FvuFileGeneratorService> _logger;

        public FvuFileGeneratorService(
            ITdsReturnService tdsReturnService,
            IFvuRecordBuilder recordBuilder,
            ILogger<FvuFileGeneratorService> logger)
        {
            _tdsReturnService = tdsReturnService ?? throw new ArgumentNullException(nameof(tdsReturnService));
            _recordBuilder = recordBuilder ?? throw new ArgumentNullException(nameof(recordBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Result<FvuGenerationResultDto>> GenerateForm26QFileAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            bool isCorrection = false)
        {
            _logger.LogInformation(
                "Generating Form 26Q FVU file for Company {CompanyId}, FY {FinancialYear}, {Quarter}",
                companyId, financialYear, quarter);

            try
            {
                // 1. Fetch Form 26Q data
                var form26QData = await _tdsReturnService.GenerateForm26QAsync(companyId, financialYear, quarter);

                // 2. Validate data
                var validationResult = ValidateForm26QData(form26QData);
                if (!validationResult.CanGenerate)
                {
                    _logger.LogWarning(
                        "Form 26Q validation failed with {ErrorCount} errors",
                        validationResult.Errors.Count);

                    return Error.Validation(
                        $"Form 26Q data validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
                }

                // 3. Generate the file content
                var (fileContent, stats) = BuildForm26QFile(form26QData, isCorrection);

                // 4. Create result
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
                var fileName = GenerateFileName("26Q", form26QData.Deductor.Tan, financialYear, quarter, isCorrection);

                var result = new FvuGenerationResultDto
                {
                    FileStream = stream,
                    FileName = fileName,
                    TotalRecords = stats.TotalRecords,
                    TotalDeductees = stats.TotalDeductees,
                    TotalChallans = stats.TotalChallans,
                    TotalTdsAmount = stats.TotalTdsAmount,
                    TotalGrossAmount = stats.TotalGrossAmount,
                    FormType = "26Q",
                    FinancialYear = financialYear,
                    Quarter = quarter,
                    IsCorrection = isCorrection,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation(
                    "Successfully generated Form 26Q FVU file with {DeducteeCount} deductees, TDS amount {TdsAmount}",
                    stats.TotalDeductees, stats.TotalTdsAmount);

                return Result<FvuGenerationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error generating Form 26Q FVU file for Company {CompanyId}",
                    companyId);

                return Error.Internal($"Failed to generate Form 26Q file: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<FvuGenerationResultDto>> GenerateForm24QFileAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            bool isCorrection = false)
        {
            _logger.LogInformation(
                "Generating Form 24Q FVU file for Company {CompanyId}, FY {FinancialYear}, {Quarter}",
                companyId, financialYear, quarter);

            try
            {
                // 1. Fetch Form 24Q data
                var form24QData = await _tdsReturnService.GenerateForm24QAsync(companyId, financialYear, quarter);

                // 2. Validate data
                var validationResult = ValidateForm24QData(form24QData);
                if (!validationResult.CanGenerate)
                {
                    _logger.LogWarning(
                        "Form 24Q validation failed with {ErrorCount} errors",
                        validationResult.Errors.Count);

                    return Error.Validation(
                        $"Form 24Q data validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
                }

                // 3. Generate the file content
                var (fileContent, stats) = BuildForm24QFile(form24QData, isCorrection);

                // 4. Create result
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
                var fileName = GenerateFileName("24Q", form24QData.Deductor.Tan, financialYear, quarter, isCorrection);

                var result = new FvuGenerationResultDto
                {
                    FileStream = stream,
                    FileName = fileName,
                    TotalRecords = stats.TotalRecords,
                    TotalDeductees = stats.TotalDeductees,
                    TotalChallans = stats.TotalChallans,
                    TotalTdsAmount = stats.TotalTdsAmount,
                    TotalGrossAmount = stats.TotalGrossAmount,
                    FormType = "24Q",
                    FinancialYear = financialYear,
                    Quarter = quarter,
                    IsCorrection = isCorrection,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation(
                    "Successfully generated Form 24Q FVU file with {EmployeeCount} employees, TDS amount {TdsAmount}",
                    stats.TotalDeductees, stats.TotalTdsAmount);

                return Result<FvuGenerationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error generating Form 24Q FVU file for Company {CompanyId}",
                    companyId);

                return Error.Internal($"Failed to generate Form 24Q file: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<FvuValidationResultDto>> ValidateForFvuAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            string formType)
        {
            _logger.LogInformation(
                "Validating Form {FormType} data for FVU generation, Company {CompanyId}, FY {FinancialYear}, {Quarter}",
                formType, companyId, financialYear, quarter);

            try
            {
                FvuValidationResultDto validationResult;

                if (formType.Equals("26Q", StringComparison.OrdinalIgnoreCase))
                {
                    var form26QData = await _tdsReturnService.GenerateForm26QAsync(companyId, financialYear, quarter);
                    validationResult = ValidateForm26QData(form26QData);
                }
                else if (formType.Equals("24Q", StringComparison.OrdinalIgnoreCase))
                {
                    var form24QData = await _tdsReturnService.GenerateForm24QAsync(companyId, financialYear, quarter);
                    validationResult = ValidateForm24QData(form24QData);
                }
                else
                {
                    return Error.Validation($"Unsupported form type: {formType}. Supported types: 26Q, 24Q");
                }

                return Result<FvuValidationResultDto>.Success(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error validating Form {FormType} data for Company {CompanyId}",
                    formType, companyId);

                return Error.Internal($"Failed to validate form data: {ex.Message}");
            }
        }

        #region Form 26Q File Building

        private (string Content, FileStats Stats) BuildForm26QFile(Form26QData data, bool isCorrection)
        {
            var sb = new StringBuilder();
            var lineNumber = 1;
            var stats = new FileStats();

            // File Header
            sb.AppendLine(_recordBuilder.BuildFileHeader(
                lineNumber++,
                data.Deductor.Tan,
                FvuConstants.FileTypes.NonSalary,
                isCorrection ? FvuConstants.UploadTypes.Correction : FvuConstants.UploadTypes.Regular,
                1)); // One batch

            // Calculate total deposit amount
            var totalDepositAmount = data.Challans.Sum(c => c.TotalAmount);

            // Batch Header
            sb.AppendLine(_recordBuilder.BuildBatchHeader26Q(
                lineNumber++,
                1, // Batch number
                data.Challans.Count > 0 ? data.Challans.Count : 1, // At least 1 challan (NIL if no challans)
                data.Deductor,
                data.ResponsiblePerson,
                data.FinancialYear,
                data.Quarter,
                totalDepositAmount,
                null)); // Previous token (for correction returns)

            // Group deductees by challan
            var deducteesByChallan = GroupDeducteesByChallan(data.DeducteeRecords, data.Challans);
            var challanRecordNumber = 1;

            foreach (var (challan, deductees) in deducteesByChallan)
            {
                var challanTdsTotal = deductees.Sum(d => d.TdsAmount);

                // Challan Detail
                sb.AppendLine(_recordBuilder.BuildChallanDetail(
                    lineNumber++,
                    1, // Batch number
                    challanRecordNumber,
                    challan,
                    deductees.Count,
                    challanTdsTotal));

                stats.TotalChallans++;

                // Deductee Details
                var deducteeRecordNumber = 1;
                foreach (var deductee in deductees)
                {
                    sb.AppendLine(_recordBuilder.BuildDeducteeDetail26Q(
                        lineNumber++,
                        1, // Batch number
                        challanRecordNumber,
                        deducteeRecordNumber++,
                        deductee));

                    stats.TotalDeductees++;
                    stats.TotalTdsAmount += deductee.TdsAmount;
                    stats.TotalGrossAmount += deductee.GrossAmount;
                }

                challanRecordNumber++;
            }

            stats.TotalRecords = lineNumber - 1;

            return (sb.ToString(), stats);
        }

        private List<(TdsChallanDetail Challan, List<Form26QDeducteeRecord> Deductees)> GroupDeducteesByChallan(
            List<Form26QDeducteeRecord> deductees,
            List<TdsChallanDetail> challans)
        {
            var result = new List<(TdsChallanDetail, List<Form26QDeducteeRecord>)>();

            if (challans.Count == 0)
            {
                // Create NIL challan if no challans exist
                var nilChallan = new TdsChallanDetail
                {
                    ChallanNumber = string.Empty,
                    BsrCode = string.Empty,
                    DepositDate = GetQuarterEndDate(DateTime.Now), // Last day of quarter
                    TdsAmount = 0,
                    Surcharge = 0,
                    Cess = 0,
                    Interest = 0,
                    LateFee = 0,
                    TotalAmount = 0
                };

                result.Add((nilChallan, deductees.ToList()));
                return result;
            }

            // Group deductees by challan number, or assign to first challan if no match
            var groupedByChalllan = deductees
                .GroupBy(d => d.ChallanNumber ?? string.Empty)
                .ToList();

            foreach (var challan in challans)
            {
                var matchingDeductees = groupedByChalllan
                    .FirstOrDefault(g => g.Key == challan.ChallanNumber)?
                    .ToList() ?? new List<Form26QDeducteeRecord>();

                result.Add((challan, matchingDeductees));
            }

            // Handle deductees without matching challan - add to first challan
            var unmatchedDeductees = groupedByChalllan
                .Where(g => !challans.Any(c => c.ChallanNumber == g.Key))
                .SelectMany(g => g)
                .ToList();

            if (unmatchedDeductees.Count > 0 && result.Count > 0)
            {
                result[0].Item2.AddRange(unmatchedDeductees);
            }

            return result;
        }

        #endregion

        #region Form 24Q File Building

        private (string Content, FileStats Stats) BuildForm24QFile(Form24QData data, bool isCorrection)
        {
            var sb = new StringBuilder();
            var lineNumber = 1;
            var stats = new FileStats();

            // File Header
            sb.AppendLine(_recordBuilder.BuildFileHeader(
                lineNumber++,
                data.Deductor.Tan,
                FvuConstants.FileTypes.NonSalary,
                isCorrection ? FvuConstants.UploadTypes.Correction : FvuConstants.UploadTypes.Regular,
                1));

            // Calculate total deposit amount
            var totalDepositAmount = data.Challans.Sum(c => c.TotalAmount);

            // Batch Header
            sb.AppendLine(_recordBuilder.BuildBatchHeader24Q(
                lineNumber++,
                1,
                data.Challans.Count > 0 ? data.Challans.Count : 1,
                data.Deductor,
                data.ResponsiblePerson,
                data.FinancialYear,
                data.Quarter,
                totalDepositAmount,
                null));

            // Group employees by challan
            var employeesByChallan = GroupEmployeesByChallan(data.EmployeeRecords, data.Challans);
            var challanRecordNumber = 1;

            foreach (var (challan, employees) in employeesByChallan)
            {
                var challanTdsTotal = employees.Sum(e => e.TdsDeducted);

                // Challan Detail
                sb.AppendLine(_recordBuilder.BuildChallanDetail(
                    lineNumber++,
                    1,
                    challanRecordNumber,
                    challan,
                    employees.Count,
                    challanTdsTotal));

                stats.TotalChallans++;

                // Employee Details
                var employeeRecordNumber = 1;
                foreach (var employee in employees)
                {
                    sb.AppendLine(_recordBuilder.BuildDeducteeDetail24Q(
                        lineNumber++,
                        1,
                        challanRecordNumber,
                        employeeRecordNumber++,
                        employee));

                    stats.TotalDeductees++;
                    stats.TotalTdsAmount += employee.TdsDeducted;
                    stats.TotalGrossAmount += employee.GrossSalary;
                }

                challanRecordNumber++;
            }

            stats.TotalRecords = lineNumber - 1;

            return (sb.ToString(), stats);
        }

        private List<(TdsChallanDetail Challan, List<Form24QEmployeeRecord> Employees)> GroupEmployeesByChallan(
            List<Form24QEmployeeRecord> employees,
            List<TdsChallanDetail> challans)
        {
            var result = new List<(TdsChallanDetail, List<Form24QEmployeeRecord>)>();

            if (challans.Count == 0)
            {
                var nilChallan = new TdsChallanDetail
                {
                    ChallanNumber = string.Empty,
                    BsrCode = string.Empty,
                    DepositDate = GetQuarterEndDate(DateTime.Now),
                    TdsAmount = 0,
                    Surcharge = 0,
                    Cess = 0,
                    Interest = 0,
                    LateFee = 0,
                    TotalAmount = 0
                };

                result.Add((nilChallan, employees.ToList()));
                return result;
            }

            // For 24Q, typically all employees are under the same challan(s)
            // Distribute employees across challans proportionally or to first challan
            if (challans.Count == 1)
            {
                result.Add((challans[0], employees.ToList()));
            }
            else
            {
                // For multiple challans, assign all to first one (simplification)
                // In practice, this should be based on monthly breakdown
                result.Add((challans[0], employees.ToList()));
                for (int i = 1; i < challans.Count; i++)
                {
                    result.Add((challans[i], new List<Form24QEmployeeRecord>()));
                }
            }

            return result;
        }

        #endregion

        #region Validation Methods

        private FvuValidationResultDto ValidateForm26QData(Form26QData data)
        {
            var result = new FvuValidationResultDto
            {
                FormType = "26Q",
                FinancialYear = data.FinancialYear,
                Quarter = data.Quarter,
                Errors = new List<FvuValidationErrorDto>(),
                Warnings = new List<FvuValidationWarningDto>(),
                Summary = new FvuValidationSummaryDto()
            };

            // Validate deductor TAN
            if (!FvuConstants.IsValidTan(data.Deductor.Tan))
            {
                result.Errors.Add(new FvuValidationErrorDto
                {
                    Code = "ERR_TAN_INVALID",
                    Message = $"Deductor TAN '{data.Deductor.Tan}' is invalid or missing",
                    Field = "Deductor.Tan",
                    SuggestedFix = "Ensure TAN is in format: 4 letters + 5 digits + 1 letter"
                });
            }
            result.Summary.HasValidTan = FvuConstants.IsValidTan(data.Deductor.Tan);

            // Validate deductor PAN
            if (!FvuConstants.IsValidPan(data.Deductor.Pan))
            {
                result.Warnings.Add(new FvuValidationWarningDto
                {
                    Code = "WARN_PAN_INVALID",
                    Message = $"Deductor PAN '{data.Deductor.Pan}' may be invalid",
                    Field = "Deductor.Pan",
                    Impact = "May cause issues during filing"
                });
            }

            // Validate each deductee
            var invalidPanCount = 0;
            foreach (var deductee in data.DeducteeRecords)
            {
                if (!FvuConstants.IsValidPan(deductee.DeducteePan))
                {
                    invalidPanCount++;
                    result.Warnings.Add(new FvuValidationWarningDto
                    {
                        Code = "WARN_DEDUCTEE_PAN",
                        Message = $"Deductee PAN '{deductee.DeducteePan}' for '{deductee.DeducteeName}' is invalid",
                        RecordIdentifier = deductee.SerialNumber.ToString(),
                        Impact = "20% higher TDS rate may apply"
                    });
                }

                // Validate section code
                if (string.IsNullOrEmpty(deductee.TdsSection))
                {
                    result.Errors.Add(new FvuValidationErrorDto
                    {
                        Code = "ERR_SECTION_MISSING",
                        Message = $"TDS section is missing for deductee '{deductee.DeducteeName}'",
                        RecordIdentifier = deductee.SerialNumber.ToString(),
                        Field = "TdsSection"
                    });
                }

                // Validate LDC certificate
                if (!string.IsNullOrEmpty(deductee.ReasonForLowerDeduction) &&
                    deductee.ReasonForLowerDeduction == "A" &&
                    string.IsNullOrEmpty(deductee.CertificateNumber))
                {
                    result.Errors.Add(new FvuValidationErrorDto
                    {
                        Code = "ERR_LDC_CERT_MISSING",
                        Message = $"Certificate number is required for nil deduction (reason A) for '{deductee.DeducteeName}'",
                        RecordIdentifier = deductee.SerialNumber.ToString(),
                        Field = "CertificateNumber"
                    });
                }
            }

            // Summary
            result.Summary.TotalRecords = data.DeducteeRecords.Count;
            result.Summary.InvalidRecords = result.Errors.Count(e => e.RecordIdentifier != null);
            result.Summary.ValidRecords = result.Summary.TotalRecords - result.Summary.InvalidRecords;
            result.Summary.RecordsWithWarnings = result.Warnings.Count(w => w.RecordIdentifier != null);
            result.Summary.TotalTdsAmount = data.TotalTdsDeducted;
            result.Summary.TotalGrossAmount = data.DeducteeRecords.Sum(d => d.GrossAmount);
            result.Summary.UniqueDeductees = data.DeducteeRecords.Select(d => d.DeducteePan).Distinct().Count();
            result.Summary.ChallanCount = data.Challans.Count;
            result.Summary.InvalidPanCount = invalidPanCount;
            result.Summary.ChallansReconciled = Math.Abs(data.TotalTdsDeducted - data.TotalTdsDeposited) < 1;

            result.IsValid = result.Errors.Count == 0;

            return result;
        }

        private FvuValidationResultDto ValidateForm24QData(Form24QData data)
        {
            var result = new FvuValidationResultDto
            {
                FormType = "24Q",
                FinancialYear = data.FinancialYear,
                Quarter = data.Quarter,
                Errors = new List<FvuValidationErrorDto>(),
                Warnings = new List<FvuValidationWarningDto>(),
                Summary = new FvuValidationSummaryDto()
            };

            // Validate deductor TAN
            if (!FvuConstants.IsValidTan(data.Deductor.Tan))
            {
                result.Errors.Add(new FvuValidationErrorDto
                {
                    Code = "ERR_TAN_INVALID",
                    Message = $"Deductor TAN '{data.Deductor.Tan}' is invalid or missing",
                    Field = "Deductor.Tan",
                    SuggestedFix = "Ensure TAN is in format: 4 letters + 5 digits + 1 letter"
                });
            }
            result.Summary.HasValidTan = FvuConstants.IsValidTan(data.Deductor.Tan);

            // Validate each employee
            var invalidPanCount = 0;
            foreach (var employee in data.EmployeeRecords)
            {
                if (!FvuConstants.IsValidPan(employee.EmployeePan))
                {
                    invalidPanCount++;
                    result.Errors.Add(new FvuValidationErrorDto
                    {
                        Code = "ERR_EMPLOYEE_PAN",
                        Message = $"Employee PAN '{employee.EmployeePan}' for '{employee.EmployeeName}' is invalid",
                        RecordIdentifier = employee.EmployeeCode,
                        Field = "EmployeePan",
                        SuggestedFix = "Employee PAN is mandatory for Form 24Q. Update the employee record."
                    });
                }

                // Validate employee has salary data
                if (employee.GrossSalary <= 0 && employee.TdsDeducted <= 0)
                {
                    result.Warnings.Add(new FvuValidationWarningDto
                    {
                        Code = "WARN_NO_SALARY_DATA",
                        Message = $"Employee '{employee.EmployeeName}' has no salary/TDS data for this quarter",
                        RecordIdentifier = employee.EmployeeCode
                    });
                }
            }

            // Summary
            result.Summary.TotalRecords = data.EmployeeRecords.Count;
            result.Summary.InvalidRecords = result.Errors.Count(e => e.RecordIdentifier != null);
            result.Summary.ValidRecords = result.Summary.TotalRecords - result.Summary.InvalidRecords;
            result.Summary.RecordsWithWarnings = result.Warnings.Count(w => w.RecordIdentifier != null);
            result.Summary.TotalTdsAmount = data.TotalTdsDeducted;
            result.Summary.TotalGrossAmount = data.EmployeeRecords.Sum(e => e.GrossSalary);
            result.Summary.UniqueDeductees = data.EmployeeRecords.Count;
            result.Summary.ChallanCount = data.Challans.Count;
            result.Summary.InvalidPanCount = invalidPanCount;
            result.Summary.ChallansReconciled = Math.Abs(data.TotalTdsDeducted - data.TotalTdsDeposited) < 1;

            result.IsValid = result.Errors.Count == 0;

            return result;
        }

        #endregion

        #region Helper Methods

        private static string GenerateFileName(string formType, string tan, string financialYear, string quarter, bool isCorrection)
        {
            var correctionSuffix = isCorrection ? "_C" : "";
            var cleanTan = tan?.ToUpperInvariant() ?? "UNKNOWN";
            var cleanFy = financialYear?.Replace("-", "") ?? "0000";

            return $"{formType}_{cleanTan}_{cleanFy}_{quarter}{correctionSuffix}.txt";
        }

        private static DateOnly GetQuarterEndDate(DateTime referenceDate)
        {
            // Get last day of the quarter
            var month = referenceDate.Month;
            int quarterEndMonth;

            if (month >= 4 && month <= 6) quarterEndMonth = 6;      // Q1
            else if (month >= 7 && month <= 9) quarterEndMonth = 9;  // Q2
            else if (month >= 10 && month <= 12) quarterEndMonth = 12; // Q3
            else quarterEndMonth = 3; // Q4

            var year = quarterEndMonth <= 3 ? referenceDate.Year + 1 : referenceDate.Year;
            return new DateOnly(year, quarterEndMonth, DateTime.DaysInMonth(year, quarterEndMonth));
        }

        private class FileStats
        {
            public int TotalRecords { get; set; }
            public int TotalDeductees { get; set; }
            public int TotalChallans { get; set; }
            public decimal TotalTdsAmount { get; set; }
            public decimal TotalGrossAmount { get; set; }
        }

        #endregion
    }
}

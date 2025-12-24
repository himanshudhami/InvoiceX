using Core.Common;

namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Service interface for generating NSDL-compliant FVU (File Validation Utility) text files
    /// for TDS/TCS returns (Form 24Q, 26Q, 27Q, 27EQ).
    ///
    /// The generated text files can be validated using NSDL's FVU utility to produce
    /// .fvu files required for e-filing on TIN-NSDL portal.
    ///
    /// File Format:
    /// - Record delimiter: ^ (caret)
    /// - Line delimiter: LF
    /// - Date format: ddmmyyyy
    /// - Amount format: Integer (no decimals)
    ///
    /// Record Types:
    /// - FH: File Header (1 per file)
    /// - BH: Batch Header (1 per batch, typically 1 batch per file)
    /// - CD: Challan Detail (1 per challan)
    /// - DD: Deductee Detail (1 per deductee/employee)
    /// </summary>
    public interface IFvuFileGeneratorService
    {
        /// <summary>
        /// Generate Form 26Q FVU text file for non-salary TDS.
        /// Covers sections: 194A, 194C, 194H, 194I, 194J, 194M, 194N, 194O, 194Q, 195, etc.
        /// </summary>
        /// <param name="companyId">Company ID (deductor)</param>
        /// <param name="financialYear">Financial year in format YYYY-YY (e.g., "2024-25")</param>
        /// <param name="quarter">Quarter (Q1, Q2, Q3, Q4)</param>
        /// <param name="isCorrection">Whether this is a correction return</param>
        /// <returns>Result containing the generated file stream and metadata</returns>
        Task<Result<FvuGenerationResultDto>> GenerateForm26QFileAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            bool isCorrection = false);

        /// <summary>
        /// Generate Form 24Q FVU text file for salary TDS.
        /// Covers section 192 (salary payments).
        /// </summary>
        /// <param name="companyId">Company ID (employer/deductor)</param>
        /// <param name="financialYear">Financial year in format YYYY-YY</param>
        /// <param name="quarter">Quarter (Q1, Q2, Q3, Q4)</param>
        /// <param name="isCorrection">Whether this is a correction return</param>
        /// <returns>Result containing the generated file stream and metadata</returns>
        Task<Result<FvuGenerationResultDto>> GenerateForm24QFileAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            bool isCorrection = false);

        /// <summary>
        /// Validate TDS return data before FVU file generation.
        /// Checks for mandatory fields, format compliance, and business rules.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="financialYear">Financial year</param>
        /// <param name="quarter">Quarter</param>
        /// <param name="formType">Form type (26Q or 24Q)</param>
        /// <returns>Validation result with errors and warnings</returns>
        Task<Result<FvuValidationResultDto>> ValidateForFvuAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            string formType);
    }

    /// <summary>
    /// Result DTO for FVU file generation
    /// </summary>
    public class FvuGenerationResultDto
    {
        public Stream FileStream { get; set; } = Stream.Null;
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; } = "text/plain";
        public int TotalRecords { get; set; }
        public int TotalDeductees { get; set; }
        public int TotalChallans { get; set; }
        public decimal TotalTdsAmount { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public string FormType { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public bool IsCorrection { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result DTO for FVU validation
    /// </summary>
    public class FvuValidationResultDto
    {
        public bool IsValid { get; set; }
        public bool CanGenerate => IsValid || Errors.Count == 0;
        public string FormType { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public List<FvuValidationErrorDto> Errors { get; set; } = new();
        public List<FvuValidationWarningDto> Warnings { get; set; } = new();
        public FvuValidationSummaryDto Summary { get; set; } = new();
    }

    public class FvuValidationErrorDto
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
        public string? RecordIdentifier { get; set; }
        public string? RecordType { get; set; }
        public string? SuggestedFix { get; set; }
    }

    public class FvuValidationWarningDto
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
        public string? RecordIdentifier { get; set; }
        public string? Impact { get; set; }
    }

    public class FvuValidationSummaryDto
    {
        public int TotalRecords { get; set; }
        public int ValidRecords { get; set; }
        public int InvalidRecords { get; set; }
        public int RecordsWithWarnings { get; set; }
        public decimal TotalTdsAmount { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public int UniqueDeductees { get; set; }
        public int ChallanCount { get; set; }
        public bool HasValidTan { get; set; }
        public int InvalidPanCount { get; set; }
        public bool ChallansReconciled { get; set; }
    }
}

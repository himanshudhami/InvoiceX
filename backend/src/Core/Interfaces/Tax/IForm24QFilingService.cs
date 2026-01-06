using Core.Common;

namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Service interface for Form 24Q quarterly TDS filing management.
    ///
    /// Form 24Q is the quarterly TDS return filed by employers for salary TDS.
    /// It includes challan details (Annexure I) and employee-wise salary details (Annexure II for Q4).
    ///
    /// Key Operations:
    /// 1. Create draft filings from payroll data
    /// 2. Validate filings for compliance
    /// 3. Generate FVU files for NSDL upload
    /// 4. Track filing workflow (submit, acknowledge, reject)
    /// 5. Support correction returns
    /// </summary>
    public interface IForm24QFilingService
    {
        // ==================== Retrieval Operations ====================

        /// <summary>
        /// Get Form 24Q filing by ID
        /// </summary>
        Task<Result<Form24QFilingDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get active Form 24Q filing for a company/FY/quarter
        /// </summary>
        Task<Result<Form24QFilingDto>> GetByCompanyQuarterAsync(
            Guid companyId,
            string financialYear,
            string quarter);

        /// <summary>
        /// Get paged list of Form 24Q filings for a company
        /// </summary>
        Task<Result<PagedResult<Form24QFilingSummaryDto>>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? quarter = null,
            string? status = null,
            string? sortBy = null,
            bool sortDescending = false);

        /// <summary>
        /// Get Form 24Q filing statistics for a financial year
        /// </summary>
        Task<Result<Form24QFilingStatisticsDto>> GetStatisticsAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Get all filings for a financial year
        /// </summary>
        Task<Result<IEnumerable<Form24QFilingSummaryDto>>> GetByFinancialYearAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Get pending (not acknowledged) filings for a financial year
        /// </summary>
        Task<Result<IEnumerable<Form24QFilingSummaryDto>>> GetPendingFilingsAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Get overdue filings (past due date, not acknowledged)
        /// </summary>
        Task<Result<IEnumerable<Form24QFilingSummaryDto>>> GetOverdueFilingsAsync(Guid companyId);

        // ==================== Draft Operations ====================

        /// <summary>
        /// Create a draft Form 24Q filing for a quarter.
        /// Generates data from payroll transactions.
        /// </summary>
        Task<Result<Form24QFilingDto>> CreateDraftAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            Guid? createdBy = null);

        /// <summary>
        /// Refresh filing data from current payroll transactions.
        /// Only allowed for draft status.
        /// </summary>
        Task<Result<Form24QFilingDto>> RefreshDataAsync(Guid filingId, Guid? updatedBy = null);

        /// <summary>
        /// Preview Form 24Q data without saving (for verification)
        /// </summary>
        Task<Result<Form24QPreviewData>> PreviewAsync(
            Guid companyId,
            string financialYear,
            string quarter);

        // ==================== Validation Operations ====================

        /// <summary>
        /// Validate Form 24Q filing data.
        /// Checks for compliance with NSDL requirements.
        /// </summary>
        Task<Result<Form24QValidationResult>> ValidateFilingAsync(Guid filingId);

        // ==================== FVU Operations ====================

        /// <summary>
        /// Generate FVU file for NSDL upload.
        /// Filing must be validated first.
        /// </summary>
        Task<Result<Form24QFilingDto>> GenerateFvuAsync(Guid filingId, Guid? generatedBy = null);

        /// <summary>
        /// Download the generated FVU file
        /// </summary>
        Task<Result<Form24QFileDownloadResult>> DownloadFvuAsync(Guid filingId);

        // ==================== Workflow Operations ====================

        /// <summary>
        /// Mark filing as submitted to NSDL
        /// </summary>
        Task<Result<Form24QFilingDto>> MarkAsSubmittedAsync(
            Guid filingId,
            DateOnly? filingDate = null,
            Guid? submittedBy = null);

        /// <summary>
        /// Record acknowledgement from NSDL
        /// </summary>
        Task<Result<Form24QFilingDto>> RecordAcknowledgementAsync(
            Guid filingId,
            string acknowledgementNumber,
            string? tokenNumber = null,
            DateOnly? filingDate = null,
            Guid? updatedBy = null);

        /// <summary>
        /// Mark filing as rejected by NSDL
        /// </summary>
        Task<Result<Form24QFilingDto>> MarkAsRejectedAsync(
            Guid filingId,
            string rejectionReason,
            Guid? updatedBy = null);

        // ==================== Correction Returns ====================

        /// <summary>
        /// Create a correction return based on an original filing.
        /// Original filing will be marked as 'revised'.
        /// </summary>
        Task<Result<Form24QFilingDto>> CreateCorrectionReturnAsync(
            Guid originalFilingId,
            Guid? createdBy = null);

        /// <summary>
        /// Get correction returns for an original filing
        /// </summary>
        Task<Result<IEnumerable<Form24QFilingSummaryDto>>> GetCorrectionsAsync(Guid originalFilingId);

        // ==================== Delete Operations ====================

        /// <summary>
        /// Delete a draft filing. Only draft status filings can be deleted.
        /// </summary>
        Task<Result<bool>> DeleteDraftAsync(Guid filingId);
    }

    // ==================== DTOs ====================

    /// <summary>
    /// Full Form 24Q filing DTO
    /// </summary>
    public class Form24QFilingDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public string Tan { get; set; } = string.Empty;
        public string FormType { get; set; } = string.Empty;
        public Guid? OriginalFilingId { get; set; }
        public int RevisionNumber { get; set; }

        // Summary
        public int TotalEmployees { get; set; }
        public decimal TotalSalaryPaid { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal Variance { get; set; }

        // Status
        public string Status { get; set; } = string.Empty;
        public bool HasValidationErrors { get; set; }
        public int ValidationErrorCount { get; set; }
        public int ValidationWarningCount { get; set; }

        // FVU
        public bool HasFvuFile { get; set; }
        public DateTime? FvuGeneratedAt { get; set; }
        public string? FvuVersion { get; set; }

        // Filing
        public DateOnly? FilingDate { get; set; }
        public string? AcknowledgementNumber { get; set; }
        public string? TokenNumber { get; set; }
        public string? ProvisionalReceiptNumber { get; set; }

        // Dates
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Summary DTO for listing
    /// </summary>
    public class Form24QFilingSummaryDto
    {
        public Guid Id { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public string Tan { get; set; } = string.Empty;
        public string FormType { get; set; } = string.Empty;
        public int RevisionNumber { get; set; }
        public int TotalEmployees { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal Variance { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool HasFvuFile { get; set; }
        public string? AcknowledgementNumber { get; set; }
        public DateOnly? FilingDate { get; set; }
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Statistics DTO
    /// </summary>
    public class Form24QFilingStatisticsDto
    {
        public string FinancialYear { get; set; } = string.Empty;
        public int TotalFilings { get; set; }
        public int DraftCount { get; set; }
        public int ValidatedCount { get; set; }
        public int FvuGeneratedCount { get; set; }
        public int SubmittedCount { get; set; }
        public int AcknowledgedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal TotalVariance { get; set; }

        // Quarterly breakdown
        public QuarterStatusDto? Q1 { get; set; }
        public QuarterStatusDto? Q2 { get; set; }
        public QuarterStatusDto? Q3 { get; set; }
        public QuarterStatusDto? Q4 { get; set; }
    }

    /// <summary>
    /// Quarter status DTO
    /// </summary>
    public class QuarterStatusDto
    {
        public string Quarter { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool HasFiling { get; set; }
        public bool IsOverdue { get; set; }
        public DateOnly DueDate { get; set; }
        public int TotalEmployees { get; set; }
        public decimal TdsDeducted { get; set; }
        public decimal TdsDeposited { get; set; }
        public string? AcknowledgementNumber { get; set; }
    }

    /// <summary>
    /// Preview data DTO
    /// </summary>
    public class Form24QPreviewData
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
        public decimal TotalSalaryPaid { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal Variance { get; set; }
        public List<ChallanPreviewDto> Challans { get; set; } = new();
        public List<EmployeePreviewDto> Employees { get; set; } = new();
    }

    /// <summary>
    /// Challan preview DTO
    /// </summary>
    public class ChallanPreviewDto
    {
        public string BsrCode { get; set; } = string.Empty;
        public DateOnly ChallanDate { get; set; }
        public string ChallanSerial { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Cin { get; set; }
    }

    /// <summary>
    /// Employee preview DTO
    /// </summary>
    public class EmployeePreviewDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public decimal GrossSalary { get; set; }
        public decimal TdsDeducted { get; set; }
    }

    /// <summary>
    /// Validation result DTO for Form 24Q
    /// </summary>
    public class Form24QValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
    }

    /// <summary>
    /// File download result for Form 24Q
    /// </summary>
    public class Form24QFileDownloadResult
    {
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/plain";
    }
}

namespace Core.Entities.Tax
{
    /// <summary>
    /// Form 24Q Quarterly TDS Return for Salary (Section 192).
    /// Filed by employer to report TDS deducted from employee salaries.
    ///
    /// Annexure I (All Quarters): Challan/Transfer voucher details
    /// Annexure II (Q4 Only): Employee-wise annual salary and TDS details
    ///
    /// Filing Due Dates:
    /// - Q1 (Apr-Jun): July 31
    /// - Q2 (Jul-Sep): October 31
    /// - Q3 (Oct-Dec): January 31
    /// - Q4 (Jan-Mar): May 31
    /// </summary>
    public class Form24QFiling
    {
        public Guid Id { get; set; }

        // ==================== Filing Identification ====================

        public Guid CompanyId { get; set; }

        /// <summary>
        /// Indian financial year in '2024-25' format
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Quarter: Q1, Q2, Q3, Q4
        /// </summary>
        public string Quarter { get; set; } = string.Empty;

        /// <summary>
        /// Tax Deduction Account Number of employer
        /// </summary>
        public string Tan { get; set; } = string.Empty;

        // ==================== Filing Type ====================

        /// <summary>
        /// Type of filing: 'regular' or 'correction'
        /// </summary>
        public string FormType { get; set; } = "regular";

        /// <summary>
        /// Reference to original filing for correction returns
        /// </summary>
        public Guid? OriginalFilingId { get; set; }

        /// <summary>
        /// Revision number for correction returns (0 for original, 1+ for corrections)
        /// </summary>
        public int RevisionNumber { get; set; }

        // ==================== Summary Data ====================

        /// <summary>
        /// Total number of employees with TDS deducted in the quarter
        /// </summary>
        public int TotalEmployees { get; set; }

        /// <summary>
        /// Total salary paid during the quarter
        /// </summary>
        public decimal TotalSalaryPaid { get; set; }

        /// <summary>
        /// Total TDS deducted during the quarter
        /// </summary>
        public decimal TotalTdsDeducted { get; set; }

        /// <summary>
        /// Total TDS deposited to government during the quarter
        /// </summary>
        public decimal TotalTdsDeposited { get; set; }

        /// <summary>
        /// Variance between TDS deducted and deposited
        /// </summary>
        public decimal Variance { get; set; }

        // ==================== Annexure Data (JSONB) ====================

        /// <summary>
        /// Annexure I - Challan details (BSR code, date, serial, amount)
        /// JSON array of challan records
        /// </summary>
        public string? Annexure1Data { get; set; }

        /// <summary>
        /// Annexure II - Employee-wise annual salary details (Q4 only)
        /// JSON array of employee annual records
        /// </summary>
        public string? Annexure2Data { get; set; }

        /// <summary>
        /// Quarterly employee-wise TDS records
        /// JSON array of employee quarterly records
        /// </summary>
        public string? EmployeeRecords { get; set; }

        /// <summary>
        /// Linked challan records from statutory_payments
        /// JSON array of challan references
        /// </summary>
        public string? ChallanRecords { get; set; }

        // ==================== Status Workflow ====================

        /// <summary>
        /// Filing status: draft, validated, fvu_generated, submitted, acknowledged, rejected, revised
        /// </summary>
        public string Status { get; set; } = "draft";

        // ==================== Validation ====================

        /// <summary>
        /// Validation errors as JSON array
        /// </summary>
        public string? ValidationErrors { get; set; }

        /// <summary>
        /// Validation warnings as JSON array
        /// </summary>
        public string? ValidationWarnings { get; set; }

        public DateTime? ValidatedAt { get; set; }
        public Guid? ValidatedBy { get; set; }

        // ==================== FVU File ====================

        /// <summary>
        /// Path to generated FVU file
        /// </summary>
        public string? FvuFilePath { get; set; }

        public DateTime? FvuGeneratedAt { get; set; }

        /// <summary>
        /// FVU version used for generation (e.g., '7.8')
        /// </summary>
        public string? FvuVersion { get; set; }

        // ==================== Filing Details ====================

        /// <summary>
        /// Date of filing with NSDL
        /// </summary>
        public DateOnly? FilingDate { get; set; }

        /// <summary>
        /// Acknowledgement number from NSDL
        /// </summary>
        public string? AcknowledgementNumber { get; set; }

        /// <summary>
        /// Token number from NSDL
        /// </summary>
        public string? TokenNumber { get; set; }

        /// <summary>
        /// Provisional receipt number
        /// </summary>
        public string? ProvisionalReceiptNumber { get; set; }

        // ==================== Submission Tracking ====================

        public DateTime? SubmittedAt { get; set; }
        public Guid? SubmittedBy { get; set; }

        // ==================== Rejection Handling ====================

        public string? RejectionReason { get; set; }
        public DateTime? RejectedAt { get; set; }

        // ==================== Audit ====================

        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}

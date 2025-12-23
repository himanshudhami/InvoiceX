namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Service interface for TDS Return preparation (Form 26Q and Form 24Q).
    ///
    /// Form 26Q: Quarterly statement of TDS on payments other than salary
    /// - Due dates: Q1 (Jul 31), Q2 (Oct 31), Q3 (Jan 31), Q4 (May 31)
    /// - Covers: 194A (Interest), 194C (Contractors), 194H (Commission),
    ///   194I (Rent), 194J (Professional), etc.
    ///
    /// Form 24Q: Quarterly statement of TDS on salary
    /// - Due dates: Same as 26Q
    /// - Covers: Section 192 (Salary)
    /// - Annexure II required for Q4 with annual salary details
    ///
    /// Filing Requirements:
    /// - TAN (Tax Deduction Account Number) mandatory for deductors
    /// - PAN of deductees mandatory (otherwise 20% TDS rate applies)
    /// - Challan details for TDS deposits
    /// </summary>
    public interface ITdsReturnService
    {
        // ==================== Form 26Q (Non-Salary) ====================

        /// <summary>
        /// Generate Form 26Q data for non-salary TDS return.
        /// Aggregates contractor payments, professional fees, rent, interest, etc.
        /// </summary>
        Task<Form26QData> GenerateForm26QAsync(Guid companyId, string financialYear, string quarter);

        /// <summary>
        /// Validate Form 26Q data before filing
        /// </summary>
        Task<TdsReturnValidationResult> ValidateForm26QAsync(Guid companyId, string financialYear, string quarter);

        /// <summary>
        /// Get Form 26Q summary with deductee-wise breakdown
        /// </summary>
        Task<Form26QSummary> GetForm26QSummaryAsync(Guid companyId, string financialYear, string quarter);

        // ==================== Form 24Q (Salary) ====================

        /// <summary>
        /// Generate Form 24Q data for salary TDS return.
        /// Includes employee-wise salary and TDS details.
        /// </summary>
        Task<Form24QData> GenerateForm24QAsync(Guid companyId, string financialYear, string quarter);

        /// <summary>
        /// Validate Form 24Q data before filing
        /// </summary>
        Task<TdsReturnValidationResult> ValidateForm24QAsync(Guid companyId, string financialYear, string quarter);

        /// <summary>
        /// Get Form 24Q summary with employee-wise breakdown
        /// </summary>
        Task<Form24QSummary> GetForm24QSummaryAsync(Guid companyId, string financialYear, string quarter);

        /// <summary>
        /// Generate Form 24Q Annexure II (required for Q4 only)
        /// Contains full annual salary details for each employee
        /// </summary>
        Task<Form24QAnnexureII> GenerateForm24QAnnexureIIAsync(Guid companyId, string financialYear);

        // ==================== Challan Reconciliation ====================

        /// <summary>
        /// Get challan details for a quarter
        /// </summary>
        Task<IEnumerable<TdsChallanDetail>> GetChallanDetailsAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            string? formType = null);

        /// <summary>
        /// Reconcile TDS deducted with challans deposited
        /// </summary>
        Task<ChallanReconciliationResult> ReconcileChallansAsync(
            Guid companyId,
            string financialYear,
            string quarter);

        // ==================== Due Date Tracking ====================

        /// <summary>
        /// Get TDS return due dates for a financial year
        /// </summary>
        Task<IEnumerable<TdsReturnDueDate>> GetDueDatesAsync(string financialYear);

        /// <summary>
        /// Get pending/overdue TDS returns
        /// </summary>
        Task<IEnumerable<PendingTdsReturn>> GetPendingReturnsAsync(Guid companyId);

        // ==================== Filing Status ====================

        /// <summary>
        /// Mark return as filed
        /// </summary>
        Task MarkReturnFiledAsync(MarkReturnFiledRequest request);

        /// <summary>
        /// Get filing history
        /// </summary>
        Task<IEnumerable<TdsReturnFilingHistory>> GetFilingHistoryAsync(
            Guid companyId,
            string? financialYear = null);
    }

    // ==================== Form 26Q DTOs ====================

    /// <summary>
    /// Form 26Q data structure for non-salary TDS return
    /// </summary>
    public class Form26QData
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;

        // Deductor details
        public DeductorDetails Deductor { get; set; } = new();

        // Responsible person
        public ResponsiblePersonDetails ResponsiblePerson { get; set; } = new();

        // Challan details
        public List<TdsChallanDetail> Challans { get; set; } = new();

        // Deductee records (Section-wise)
        public List<Form26QDeducteeRecord> DeducteeRecords { get; set; } = new();

        // Summary
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalLateFee { get; set; }
        public int TotalDeductees { get; set; }
    }

    /// <summary>
    /// Deductee record for Form 26Q
    /// </summary>
    public class Form26QDeducteeRecord
    {
        public int SerialNumber { get; set; }
        public string DeducteePan { get; set; } = string.Empty;
        public string DeducteeName { get; set; } = string.Empty;
        public string TdsSection { get; set; } = string.Empty;
        public DateOnly PaymentDate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal TdsRate { get; set; }
        public decimal TdsAmount { get; set; }
        public DateOnly? TdsDepositDate { get; set; }
        public string? ChallanNumber { get; set; }
        public string? BsrCode { get; set; }
        public string? ReasonForLowerDeduction { get; set; }
        public string? CertificateNumber { get; set; }

        // Source tracking
        public string SourceType { get; set; } = string.Empty;
        public Guid? SourceId { get; set; }
    }

    /// <summary>
    /// Form 26Q summary
    /// </summary>
    public class Form26QSummary
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public decimal TotalGrossAmount { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal Variance { get; set; }
        public int TotalTransactions { get; set; }
        public int UniqueDeductees { get; set; }
        public List<TdsSectionSummary26Q> SectionBreakdown { get; set; } = new();
        public List<TdsMonthSummary> MonthBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Section-wise summary for Form 26Q
    /// </summary>
    public class TdsSectionSummary26Q
    {
        public string SectionCode { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal TdsAmount { get; set; }
        public int TransactionCount { get; set; }
        public int DeducteeCount { get; set; }
    }

    // ==================== Form 24Q DTOs ====================

    /// <summary>
    /// Form 24Q data structure for salary TDS return
    /// </summary>
    public class Form24QData
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;

        // Deductor details
        public DeductorDetails Deductor { get; set; } = new();

        // Responsible person
        public ResponsiblePersonDetails ResponsiblePerson { get; set; } = new();

        // Challan details
        public List<TdsChallanDetail> Challans { get; set; } = new();

        // Employee records
        public List<Form24QEmployeeRecord> EmployeeRecords { get; set; } = new();

        // Summary
        public decimal TotalSalary { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public int TotalEmployees { get; set; }
    }

    /// <summary>
    /// Employee record for Form 24Q
    /// </summary>
    public class Form24QEmployeeRecord
    {
        public int SerialNumber { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeePan { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public DateOnly DateOfJoining { get; set; }
        public DateOnly? DateOfLeaving { get; set; }

        // Quarter salary details
        public decimal GrossSalary { get; set; }
        public decimal TdsDeducted { get; set; }
        public decimal TdsDeposited { get; set; }

        // Monthly breakdown
        public List<MonthlySalaryTds> MonthlyDetails { get; set; } = new();
    }

    /// <summary>
    /// Monthly salary and TDS details
    /// </summary>
    public class MonthlySalaryTds
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal TdsDeducted { get; set; }
        public DateOnly? TdsDepositDate { get; set; }
        public string? ChallanNumber { get; set; }
    }

    /// <summary>
    /// Form 24Q summary
    /// </summary>
    public class Form24QSummary
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal Variance { get; set; }
        public int TotalEmployees { get; set; }
        public int EmployeesWithTds { get; set; }
        public List<TdsMonthSummary> MonthBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Form 24Q Annexure II - Annual salary details (Q4 only)
    /// </summary>
    public class Form24QAnnexureII
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public List<AnnexureIIEmployeeRecord> EmployeeRecords { get; set; } = new();
    }

    /// <summary>
    /// Annexure II employee record with full annual details
    /// </summary>
    public class AnnexureIIEmployeeRecord
    {
        public Guid EmployeeId { get; set; }
        public string EmployeePan { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string TaxRegime { get; set; } = string.Empty;

        // Income details
        public decimal GrossSalary { get; set; }
        public decimal Perquisites { get; set; }
        public decimal ProfitsInLieu { get; set; }
        public decimal TotalSalary { get; set; }
        public decimal OtherIncome { get; set; }
        public decimal GrossTotal { get; set; }

        // Deductions
        public decimal StandardDeduction { get; set; }
        public decimal Section80C { get; set; }
        public decimal Section80CCD1B { get; set; }
        public decimal Section80D { get; set; }
        public decimal HraExemption { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal TotalDeductions { get; set; }

        // Tax calculation
        public decimal TaxableIncome { get; set; }
        public decimal TaxOnIncome { get; set; }
        public decimal Rebate87A { get; set; }
        public decimal Surcharge { get; set; }
        public decimal Cess { get; set; }
        public decimal TotalTaxLiability { get; set; }
        public decimal TdsDeducted { get; set; }
        public decimal TaxRefund { get; set; }
    }

    // ==================== Common DTOs ====================

    /// <summary>
    /// Deductor details for TDS returns
    /// </summary>
    public class DeductorDetails
    {
        public string Tan { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string FlatNo { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string RoadName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DeductorType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Responsible person details
    /// </summary>
    public class ResponsiblePersonDetails
    {
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FlatNo { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string RoadName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
    }

    /// <summary>
    /// TDS challan details
    /// </summary>
    public class TdsChallanDetail
    {
        public Guid Id { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public string BsrCode { get; set; } = string.Empty;
        public DateOnly DepositDate { get; set; }
        public string TdsSection { get; set; } = string.Empty;
        public decimal TdsAmount { get; set; }
        public decimal Surcharge { get; set; }
        public decimal Cess { get; set; }
        public decimal Interest { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string? MinorHead { get; set; }
        public string? BookEntryFlag { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Monthly TDS summary
    /// </summary>
    public class TdsMonthSummary
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal TdsDeducted { get; set; }
        public decimal TdsDeposited { get; set; }
        public int TransactionCount { get; set; }
    }

    // ==================== Validation & Filing DTOs ====================

    /// <summary>
    /// TDS return validation result
    /// </summary>
    public class TdsReturnValidationResult
    {
        public bool IsValid { get; set; }
        public string FormType { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public ValidationSummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Validation error
    /// </summary>
    public class ValidationError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
        public string? RecordIdentifier { get; set; }
        public string Severity { get; set; } = "error";
    }

    /// <summary>
    /// Validation warning
    /// </summary>
    public class ValidationWarning
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
        public string? RecordIdentifier { get; set; }
    }

    /// <summary>
    /// Validation summary
    /// </summary>
    public class ValidationSummary
    {
        public int TotalRecords { get; set; }
        public int ValidRecords { get; set; }
        public int InvalidRecords { get; set; }
        public int RecordsWithWarnings { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal Variance { get; set; }
        public bool HasChallanMismatch { get; set; }
        public bool HasPanMismatch { get; set; }
    }

    /// <summary>
    /// Challan reconciliation result
    /// </summary>
    public class ChallanReconciliationResult
    {
        public bool IsReconciled { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal Variance { get; set; }
        public List<ChallanMismatch> Mismatches { get; set; } = new();
    }

    /// <summary>
    /// Challan mismatch details
    /// </summary>
    public class ChallanMismatch
    {
        public string MismatchType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ExpectedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public string? ChallanNumber { get; set; }
    }

    /// <summary>
    /// TDS return due date
    /// </summary>
    public class TdsReturnDueDate
    {
        public string FormType { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public DateOnly? ExtendedDueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysUntilDue { get; set; }
    }

    /// <summary>
    /// Pending TDS return
    /// </summary>
    public class PendingTdsReturn
    {
        public string FormType { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public decimal EstimatedTdsAmount { get; set; }
        public int RecordCount { get; set; }
    }

    /// <summary>
    /// Request for marking return as filed
    /// </summary>
    public class MarkReturnFiledRequest
    {
        public Guid CompanyId { get; set; }
        public string FormType { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public DateOnly FilingDate { get; set; }
        public string AcknowledgementNumber { get; set; } = string.Empty;
        public string? TokenNumber { get; set; }
        public string? OriginalOrRevised { get; set; }
        public string? Remarks { get; set; }
        public Guid? FiledBy { get; set; }
    }

    /// <summary>
    /// TDS return filing history
    /// </summary>
    public class TdsReturnFilingHistory
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string FormType { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Quarter { get; set; } = string.Empty;
        public DateOnly FilingDate { get; set; }
        public string AcknowledgementNumber { get; set; } = string.Empty;
        public string? TokenNumber { get; set; }
        public string OriginalOrRevised { get; set; } = "original";
        public decimal TotalTdsAmount { get; set; }
        public int TotalRecords { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? FiledBy { get; set; }
    }
}

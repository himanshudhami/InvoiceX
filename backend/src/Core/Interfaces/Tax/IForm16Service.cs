using Core.Common;

namespace Core.Interfaces.Tax
{
    /// <summary>
    /// Service interface for Form 16 (TDS Certificate) generation and management.
    ///
    /// Form 16 is the TDS certificate issued by employers to employees.
    /// It certifies that tax has been deducted at source from salary and deposited to the government.
    ///
    /// Key Operations:
    /// 1. Generate Form 16 data from payroll transactions
    /// 2. Generate PDF certificates
    /// 3. Bulk generation for all employees
    /// 4. Verification and issuance workflow
    /// </summary>
    public interface IForm16Service
    {
        // ==================== Generation Operations ====================

        /// <summary>
        /// Generate Form 16 data for a single employee.
        /// Aggregates salary and TDS data from payroll transactions.
        /// </summary>
        Task<Result<Form16GenerationResult>> GenerateForEmployeeAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear,
            Guid? generatedBy = null);

        /// <summary>
        /// Bulk generate Form 16 for all eligible employees in a financial year.
        /// </summary>
        Task<Result<BulkForm16GenerationResult>> GenerateBulkAsync(
            Guid companyId,
            string financialYear,
            Guid? generatedBy = null,
            bool regenerateExisting = false);

        /// <summary>
        /// Preview Form 16 data without saving (for verification before generation)
        /// </summary>
        Task<Result<Form16PreviewData>> PreviewAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear);

        // ==================== Retrieval Operations ====================

        /// <summary>
        /// Get Form 16 by ID
        /// </summary>
        Task<Result<Form16Dto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get Form 16 for an employee and financial year
        /// </summary>
        Task<Result<Form16Dto>> GetByEmployeeAndFyAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear);

        /// <summary>
        /// Get paged list of Form 16s for a company
        /// </summary>
        Task<Result<PagedResult<Form16SummaryDto>>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false);

        /// <summary>
        /// Get Form 16 statistics for a financial year
        /// </summary>
        Task<Result<Form16StatisticsDto>> GetStatisticsAsync(
            Guid companyId,
            string financialYear);

        // ==================== PDF Operations ====================

        /// <summary>
        /// Generate PDF for a Form 16
        /// </summary>
        Task<Result<Form16PdfResult>> GeneratePdfAsync(Guid form16Id, Guid? generatedBy = null);

        /// <summary>
        /// Bulk generate PDFs for all Form 16s in a financial year
        /// </summary>
        Task<Result<BulkPdfGenerationResult>> GenerateBulkPdfAsync(
            Guid companyId,
            string financialYear,
            Guid? generatedBy = null);

        /// <summary>
        /// Download PDF for a Form 16
        /// </summary>
        Task<Result<Stream>> DownloadPdfAsync(Guid form16Id);

        // ==================== Workflow Operations ====================

        /// <summary>
        /// Verify Form 16 (HR/Finance approval)
        /// </summary>
        Task<Result<Form16Dto>> VerifyAsync(Guid form16Id, VerifyForm16Request request);

        /// <summary>
        /// Mark Form 16 as issued (sent to employee)
        /// </summary>
        Task<Result<Form16Dto>> IssueAsync(Guid form16Id, IssueForm16Request request);

        /// <summary>
        /// Cancel Form 16
        /// </summary>
        Task<Result> CancelAsync(Guid form16Id, string reason, Guid cancelledBy);

        /// <summary>
        /// Regenerate Form 16 (recalculate from payroll data)
        /// </summary>
        Task<Result<Form16GenerationResult>> RegenerateAsync(
            Guid form16Id,
            Guid? regeneratedBy = null);

        // ==================== Validation ====================

        /// <summary>
        /// Validate Form 16 data before generation
        /// </summary>
        Task<Result<Form16ValidationResult>> ValidateAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear);

        /// <summary>
        /// Check if Form 16 can be generated for an employee
        /// </summary>
        Task<Result<bool>> CanGenerateAsync(
            Guid companyId,
            Guid employeeId,
            string financialYear);
    }

    // ==================== DTOs ====================

    /// <summary>
    /// Form 16 complete data
    /// </summary>
    public class Form16Dto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid EmployeeId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string CertificateNumber { get; set; } = string.Empty;

        // Deductor details
        public string Tan { get; set; } = string.Empty;
        public string DeductorPan { get; set; } = string.Empty;
        public string DeductorName { get; set; } = string.Empty;
        public string DeductorAddress { get; set; } = string.Empty;

        // Employee details
        public string EmployeePan { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeAddress { get; set; } = string.Empty;

        // Period
        public DateOnly PeriodFrom { get; set; }
        public DateOnly PeriodTo { get; set; }

        // Quarterly TDS summary
        public List<QuarterlyTdsSummary> QuarterlyTds { get; set; } = new();
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }

        // Salary details
        public decimal GrossSalary { get; set; }
        public decimal Perquisites { get; set; }
        public decimal ProfitsInLieu { get; set; }
        public decimal TotalSalary { get; set; }
        public decimal TotalExemptions { get; set; }
        public decimal TotalDeductions { get; set; }

        // Tax computation
        public string TaxRegime { get; set; } = "new";
        public decimal TaxableIncome { get; set; }
        public decimal TaxOnIncome { get; set; }
        public decimal Rebate87A { get; set; }
        public decimal Surcharge { get; set; }
        public decimal Cess { get; set; }
        public decimal TotalTaxLiability { get; set; }
        public decimal Relief89 { get; set; }
        public decimal NetTaxPayable { get; set; }

        // Status
        public string Status { get; set; } = "draft";
        public DateTime? GeneratedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? IssuedAt { get; set; }
        public string? PdfPath { get; set; }

        // Detailed breakdowns
        public SalaryBreakdown? SalaryBreakdown { get; set; }
        public DeductionsBreakdown? DeductionsBreakdown { get; set; }
        public TaxComputationBreakdown? TaxComputation { get; set; }
    }

    /// <summary>
    /// Summary DTO for list views
    /// </summary>
    public class Form16SummaryDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeePan { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string CertificateNumber { get; set; } = string.Empty;
        public decimal GrossSalary { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TaxableIncome { get; set; }
        public string TaxRegime { get; set; } = "new";
        public string Status { get; set; } = "draft";
        public DateTime? GeneratedAt { get; set; }
        public DateTime? IssuedAt { get; set; }
        public bool HasPdf { get; set; }
    }

    /// <summary>
    /// Quarterly TDS summary for Part A
    /// </summary>
    public class QuarterlyTdsSummary
    {
        public string Quarter { get; set; } = string.Empty;
        public decimal TdsDeducted { get; set; }
        public decimal TdsDeposited { get; set; }
        public List<ChallanDetail> Challans { get; set; } = new();
    }

    /// <summary>
    /// Challan details for Part A
    /// </summary>
    public class ChallanDetail
    {
        public string ChallanNumber { get; set; } = string.Empty;
        public string BsrCode { get; set; } = string.Empty;
        public DateOnly DepositDate { get; set; }
        public decimal Amount { get; set; }
        public string? MinorHead { get; set; }
    }

    /// <summary>
    /// Salary breakdown for Part B
    /// </summary>
    public class SalaryBreakdown
    {
        public decimal BasicSalary { get; set; }
        public decimal Hra { get; set; }
        public decimal DearnessAllowance { get; set; }
        public decimal ConveyanceAllowance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal SpecialAllowance { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal Bonus { get; set; }
        public decimal Lta { get; set; }
        public decimal Arrears { get; set; }
        public decimal Reimbursements { get; set; }
        public decimal GrossSalary { get; set; }
    }

    /// <summary>
    /// Deductions breakdown for Part B
    /// </summary>
    public class DeductionsBreakdown
    {
        public decimal StandardDeduction { get; set; }
        public decimal ProfessionalTax { get; set; }
        public decimal Section80C { get; set; }
        public decimal Section80CCD1B { get; set; }
        public decimal Section80D { get; set; }
        public decimal Section80E { get; set; }
        public decimal Section80G { get; set; }
        public decimal Section80TTA { get; set; }
        public decimal Section24 { get; set; }
        public decimal HraExemption { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal TotalDeductions { get; set; }
    }

    /// <summary>
    /// Tax computation breakdown
    /// </summary>
    public class TaxComputationBreakdown
    {
        public decimal GrossIncome { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TaxableIncome { get; set; }
        public List<TaxSlabBreakdown> SlabBreakdown { get; set; } = new();
        public decimal TaxOnIncome { get; set; }
        public decimal Rebate87A { get; set; }
        public decimal TaxAfterRebate { get; set; }
        public decimal Surcharge { get; set; }
        public decimal SurchargeRelief { get; set; }
        public decimal Cess { get; set; }
        public decimal TotalTax { get; set; }
        public decimal Relief89 { get; set; }
        public decimal NetTax { get; set; }
    }

    /// <summary>
    /// Tax slab breakdown
    /// </summary>
    public class TaxSlabBreakdown
    {
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal Tax { get; set; }
    }

    // ==================== Request/Response DTOs ====================

    /// <summary>
    /// Form 16 generation result
    /// </summary>
    public class Form16GenerationResult
    {
        public Guid Form16Id { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public decimal TotalTdsDeducted { get; set; }
        public decimal TaxableIncome { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Bulk generation result
    /// </summary>
    public class BulkForm16GenerationResult
    {
        public int TotalEmployees { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<Form16GenerationResult> Results { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Form 16 preview data
    /// </summary>
    public class Form16PreviewData
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public Form16Dto? ExistingForm16 { get; set; }
        public Form16Dto ComputedData { get; set; } = new();
        public bool HasChanges { get; set; }
        public List<string> Changes { get; set; } = new();
    }

    /// <summary>
    /// PDF generation result
    /// </summary>
    public class Form16PdfResult
    {
        public Guid Form16Id { get; set; }
        public string PdfPath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Bulk PDF generation result
    /// </summary>
    public class BulkPdfGenerationResult
    {
        public int TotalForms { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<Form16PdfResult> Results { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Verify Form 16 request
    /// </summary>
    public class VerifyForm16Request
    {
        public Guid VerifiedBy { get; set; }
        public string? VerifierName { get; set; }
        public string? VerifierDesignation { get; set; }
        public string? Place { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Issue Form 16 request
    /// </summary>
    public class IssueForm16Request
    {
        public Guid IssuedBy { get; set; }
        public bool SendEmail { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Form 16 validation result
    /// </summary>
    public class Form16ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool HasPayrollData { get; set; }
        public bool HasTdsData { get; set; }
        public bool HasEmployeePan { get; set; }
        public bool HasCompanyTan { get; set; }
        public int MonthsWithPayroll { get; set; }
    }

    /// <summary>
    /// Form 16 statistics
    /// </summary>
    public class Form16StatisticsDto
    {
        public string FinancialYear { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
        public int EligibleEmployees { get; set; }
        public int Form16Generated { get; set; }
        public int Form16Verified { get; set; }
        public int Form16Issued { get; set; }
        public int Form16Pending { get; set; }
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal TotalTaxableIncome { get; set; }
        public int EmployeesWithTds { get; set; }
        public int EmployeesWithoutTds { get; set; }
    }

    /// <summary>
    /// Paged result wrapper
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}

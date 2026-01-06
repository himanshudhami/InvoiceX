using Core.Common;
using Core.Entities.Payroll;

namespace Core.Interfaces.Statutory
{
    /// <summary>
    /// Service interface for ESI (Employee State Insurance) return generation and filing.
    /// ESI is managed by ESIC (Employees' State Insurance Corporation).
    ///
    /// Key Components:
    /// - ESI Code: Employer registration number
    /// - IP Number: Insured Person number for each employee
    /// - Contribution periods: April-September and October-March
    ///
    /// Due Dates:
    /// - 15th of following month for contribution
    ///
    /// Rates (FY 2024-25):
    /// - Employee: 0.75% of gross wages
    /// - Employer: 3.25% of gross wages
    /// - Ceiling: ₹21,000 gross/month
    ///
    /// Interest: 12% per annum on late payment
    /// </summary>
    public interface IEsiReturnService
    {
        // ==================== Return Generation ====================

        /// <summary>
        /// Generate ESI return data for a month.
        /// </summary>
        Task<Result<EsiReturnData>> GenerateReturnAsync(
            Guid companyId,
            int periodMonth,
            int periodYear);

        /// <summary>
        /// Generate ESI return from a specific payroll run.
        /// </summary>
        Task<Result<EsiReturnData>> GenerateReturnFromPayrollRunAsync(Guid payrollRunId);

        /// <summary>
        /// Preview return before filing.
        /// </summary>
        Task<Result<EsiReturnPreview>> PreviewReturnAsync(
            Guid companyId,
            int periodMonth,
            int periodYear);

        /// <summary>
        /// Generate ESI return file for ESIC portal upload.
        /// </summary>
        Task<Result<EsiReturnFileResult>> GenerateReturnFileAsync(
            Guid companyId,
            int periodMonth,
            int periodYear);

        // ==================== Return Operations ====================

        /// <summary>
        /// Create statutory payment record for ESI return.
        /// </summary>
        Task<Result<StatutoryPayment>> CreateReturnPaymentAsync(
            CreateEsiReturnRequest request);

        /// <summary>
        /// Record ESI payment.
        /// </summary>
        Task<Result<StatutoryPayment>> RecordPaymentAsync(
            Guid paymentId,
            RecordEsiPaymentRequest request);

        /// <summary>
        /// Update challan number after ESIC filing.
        /// </summary>
        Task<Result<StatutoryPayment>> UpdateChallanNumberAsync(
            Guid paymentId,
            string challanNumber);

        // ==================== Retrieval ====================

        /// <summary>
        /// Get paginated list of ESI returns.
        /// </summary>
        Task<Result<(IEnumerable<EsiReturnListDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null,
            string? searchTerm = null);

        /// <summary>
        /// Get pending ESI returns.
        /// </summary>
        Task<Result<IEnumerable<PendingEsiReturnDto>>> GetPendingReturnsAsync(
            Guid companyId,
            string? financialYear = null);

        /// <summary>
        /// Get filed ESI returns.
        /// </summary>
        Task<Result<IEnumerable<FiledEsiReturnDto>>> GetFiledReturnsAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Get return details by payment ID.
        /// </summary>
        Task<Result<EsiReturnDetailDto>> GetReturnByIdAsync(Guid paymentId);

        // ==================== Utility ====================

        /// <summary>
        /// Get due date for ESI payment (15th of following month).
        /// </summary>
        DateOnly GetDueDate(int periodMonth, int periodYear);

        /// <summary>
        /// Calculate interest on late ESI payment (12% per annum).
        /// </summary>
        decimal CalculateInterest(decimal esiAmount, DateOnly dueDate, DateOnly paymentDate);

        /// <summary>
        /// Get monthly ESI summary for dashboard.
        /// </summary>
        Task<Result<EsiReturnSummary>> GetMonthlySummaryAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Get contribution period (apr_sep or oct_mar).
        /// </summary>
        string GetContributionPeriod(int month, int year);

        // ==================== Reconciliation ====================

        /// <summary>
        /// Reconcile ESI deducted vs deposited.
        /// </summary>
        Task<Result<EsiReconciliation>> ReconcileAsync(
            Guid companyId,
            string financialYear);
    }

    // ==================== DTOs ====================

    /// <summary>
    /// ESI return data structure
    /// </summary>
    public class EsiReturnData
    {
        public Guid CompanyId { get; set; }
        public Guid? PayrollRunId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string ContributionPeriod { get; set; } = string.Empty; // apr_sep or oct_mar
        public string WageMonth { get; set; } = string.Empty; // Format: MMYYYY

        // Employer Details
        public string EsiCode { get; set; } = string.Empty;
        public string EmployerName { get; set; } = string.Empty;

        // Summary
        public int EmployeeCount { get; set; }
        public int CoveredEmployees { get; set; } // Employees within ESI ceiling
        public decimal TotalGrossWages { get; set; }
        public decimal TotalCoveredWages { get; set; } // Wages for ESI-covered employees

        // Contribution Summary
        public decimal TotalEmployeeContribution { get; set; }
        public decimal TotalEmployerContribution { get; set; }
        public decimal TotalContribution { get; set; }

        // Due Date
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }

        // Employee Records
        public List<EsiReturnEmployeeRecord> EmployeeRecords { get; set; } = new();
    }

    /// <summary>
    /// Individual employee record for ESI return
    /// </summary>
    public class EsiReturnEmployeeRecord
    {
        public Guid EmployeeId { get; set; }
        public Guid PayrollTransactionId { get; set; }

        // Employee Identification
        public string IpNumber { get; set; } = string.Empty; // Insured Person Number
        public string EmployeeName { get; set; } = string.Empty;

        // Wages
        public decimal GrossWages { get; set; }
        public bool IsCovered { get; set; } // Within ESI ceiling

        // Contributions
        public decimal EmployeeContribution { get; set; }
        public decimal EmployerContribution { get; set; }
        public decimal TotalContribution { get; set; }

        // Days
        public int DaysWorked { get; set; }
        public int AbsentDays { get; set; }

        // Flags
        public bool IsNewEmployee { get; set; }
        public bool HasExited { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public DateTime? DateOfExit { get; set; }

        // Reason for no contribution (if applicable)
        public string? NoContributionReason { get; set; }
    }

    /// <summary>
    /// ESI return preview
    /// </summary>
    public class EsiReturnPreview
    {
        public EsiReturnData ReturnData { get; set; } = new();
        public DateOnly ProposedPaymentDate { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalPayable { get; set; }

        public bool HasWarnings { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<EsiValidationError> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Validation error in ESI return
    /// </summary>
    public class EsiValidationError
    {
        public Guid? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Severity { get; set; } = "warning";
    }

    /// <summary>
    /// ESI return file generation result
    /// </summary>
    public class EsiReturnFileResult
    {
        public string FileName { get; set; } = string.Empty;
        public string FileContent { get; set; } = string.Empty;
        public string FileFormat { get; set; } = "txt";
        public int RecordCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string? Base64Content { get; set; }
    }

    /// <summary>
    /// Request to create ESI return payment
    /// </summary>
    public class CreateEsiReturnRequest
    {
        public Guid CompanyId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public Guid? PayrollRunId { get; set; }
        public DateOnly ProposedPaymentDate { get; set; }
        public Guid? CreatedBy { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to record ESI payment
    /// </summary>
    public class RecordEsiPaymentRequest
    {
        public DateOnly PaymentDate { get; set; }
        public string PaymentMode { get; set; } = "online";
        public string? BankName { get; set; }
        public Guid? BankAccountId { get; set; }
        public string? BankReference { get; set; }
        public decimal ActualAmountPaid { get; set; }
        public Guid? PaidBy { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Pending ESI return DTO
    /// </summary>
    public class PendingEsiReturnDto
    {
        public Guid? StatutoryPaymentId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal TotalContribution { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// ESI return list item DTO for paginated responses
    /// </summary>
    public class EsiReturnListDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string EsiCode { get; set; } = string.Empty;
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string MonthName { get; set; } = string.Empty;
        public string ContributionPeriod { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal TotalEmployeeContribution { get; set; }
        public decimal TotalEmployerContribution { get; set; }
        public decimal TotalContribution { get; set; }
        public decimal Interest { get; set; }
        public decimal TotalAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ChallanNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Filed ESI return DTO
    /// </summary>
    public class FiledEsiReturnDto
    {
        public Guid StatutoryPaymentId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal TotalContribution { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly PaymentDate { get; set; }
        public string? ChallanNumber { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed ESI return DTO
    /// </summary>
    public class EsiReturnDetailDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string EsiCode { get; set; } = string.Empty;

        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string ContributionPeriod { get; set; } = string.Empty;

        public int EmployeeCount { get; set; }
        public decimal TotalGrossWages { get; set; }
        public decimal TotalEmployeeContribution { get; set; }
        public decimal TotalEmployerContribution { get; set; }
        public decimal TotalContribution { get; set; }

        public decimal InterestAmount { get; set; }
        public decimal TotalPaid { get; set; }

        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ChallanNumber { get; set; }
        public string? PaymentMode { get; set; }
        public string? BankName { get; set; }
        public string? BankReference { get; set; }

        public List<EsiReturnEmployeeRecord> EmployeeRecords { get; set; } = new();
    }

    /// <summary>
    /// Monthly ESI summary
    /// </summary>
    public class EsiReturnSummary
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public List<MonthlyEsiStatus> MonthlyStatus { get; set; } = new();
        public decimal TotalEsiDeducted { get; set; }
        public decimal TotalEsiDeposited { get; set; }
        public decimal TotalVariance { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
    }

    /// <summary>
    /// Monthly ESI status
    /// </summary>
    public class MonthlyEsiStatus
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public string ContributionPeriod { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public decimal EsiDeducted { get; set; }
        public decimal EsiDeposited { get; set; }
        public decimal Variance { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ChallanNumber { get; set; }
        public Guid? StatutoryPaymentId { get; set; }
    }

    /// <summary>
    /// ESI reconciliation result
    /// </summary>
    public class EsiReconciliation
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;

        public decimal TotalEsiDeducted { get; set; }
        public decimal TotalEsiDeposited { get; set; }
        public decimal Variance { get; set; }
        public bool IsReconciled => Variance == 0;

        public List<EsiReconciliationItem> MonthlyReconciliation { get; set; } = new();
        public List<EsiReconciliationMismatch> Mismatches { get; set; } = new();
    }

    /// <summary>
    /// Monthly ESI reconciliation item
    /// </summary>
    public class EsiReconciliationItem
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int EmployeeCount { get; set; }
        public decimal Deducted { get; set; }
        public decimal Deposited { get; set; }
        public decimal Variance { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// ESI reconciliation mismatch
    /// </summary>
    public class EsiReconciliationMismatch
    {
        public string MismatchType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? Month { get; set; }
        public Guid? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public decimal Amount { get; set; }
        public string? SuggestedAction { get; set; }
    }
}

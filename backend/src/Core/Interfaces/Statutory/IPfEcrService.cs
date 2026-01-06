using Core.Common;
using Core.Entities.Payroll;

namespace Core.Interfaces.Statutory
{
    /// <summary>
    /// Service interface for PF ECR (Electronic Challan-cum-Return) generation.
    /// ECR is the format required by EPFO for PF contribution filing.
    ///
    /// Key Components:
    /// - Establishment Code: Company PF registration number
    /// - UAN: Universal Account Number for each employee
    /// - TRRN: Transaction Reference Return Number (generated after filing)
    ///
    /// Due Dates:
    /// - 15th of following month for both contribution and ECR filing
    ///
    /// Penalties:
    /// - Late filing: Interest at 1% per month (Section 7Q)
    /// - Default: Damages at 5% to 100% (Section 14B)
    /// </summary>
    public interface IPfEcrService
    {
        // ==================== ECR Generation ====================

        /// <summary>
        /// Generate ECR data for a payroll run.
        /// </summary>
        Task<Result<PfEcrData>> GenerateEcrAsync(
            Guid companyId,
            int periodMonth,
            int periodYear);

        /// <summary>
        /// Generate ECR data from a specific payroll run.
        /// </summary>
        Task<Result<PfEcrData>> GenerateEcrFromPayrollRunAsync(Guid payrollRunId);

        /// <summary>
        /// Preview ECR before generating file.
        /// </summary>
        Task<Result<PfEcrPreview>> PreviewEcrAsync(
            Guid companyId,
            int periodMonth,
            int periodYear);

        /// <summary>
        /// Generate ECR text file in EPFO format.
        /// </summary>
        Task<Result<PfEcrFileResult>> GenerateEcrFileAsync(
            Guid companyId,
            int periodMonth,
            int periodYear);

        // ==================== ECR Operations ====================

        /// <summary>
        /// Create statutory payment record for ECR.
        /// </summary>
        Task<Result<StatutoryPayment>> CreateEcrPaymentAsync(
            CreatePfEcrRequest request);

        /// <summary>
        /// Record ECR payment/remittance.
        /// </summary>
        Task<Result<StatutoryPayment>> RecordEcrPaymentAsync(
            Guid paymentId,
            RecordPfEcrPaymentRequest request);

        /// <summary>
        /// Update TRRN after EPFO filing.
        /// </summary>
        Task<Result<StatutoryPayment>> UpdateTrrnAsync(
            Guid paymentId,
            string trrn);

        // ==================== Retrieval ====================

        /// <summary>
        /// Get paginated list of PF ECR filings.
        /// </summary>
        Task<Result<(IEnumerable<PfEcrListDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null,
            string? searchTerm = null);

        /// <summary>
        /// Get pending ECR filings.
        /// </summary>
        Task<Result<IEnumerable<PendingEcrDto>>> GetPendingEcrAsync(
            Guid companyId,
            string? financialYear = null);

        /// <summary>
        /// Get filed ECRs for a period.
        /// </summary>
        Task<Result<IEnumerable<FiledEcrDto>>> GetFiledEcrAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Get ECR details by payment ID.
        /// </summary>
        Task<Result<PfEcrDetailDto>> GetEcrByIdAsync(Guid paymentId);

        // ==================== Utility ====================

        /// <summary>
        /// Get due date for PF payment (15th of following month).
        /// </summary>
        DateOnly GetDueDate(int periodMonth, int periodYear);

        /// <summary>
        /// Calculate interest on late PF payment (1% per month).
        /// </summary>
        decimal CalculateInterest(decimal pfAmount, DateOnly dueDate, DateOnly paymentDate);

        /// <summary>
        /// Calculate damages on late PF payment (5% to 100% based on delay).
        /// </summary>
        decimal CalculateDamages(decimal pfAmount, DateOnly dueDate, DateOnly paymentDate);

        /// <summary>
        /// Get monthly PF summary for dashboard.
        /// </summary>
        Task<Result<PfEcrSummary>> GetMonthlySummaryAsync(
            Guid companyId,
            string financialYear);

        // ==================== Reconciliation ====================

        /// <summary>
        /// Reconcile PF deducted vs deposited.
        /// </summary>
        Task<Result<PfReconciliation>> ReconcileAsync(
            Guid companyId,
            string financialYear);
    }

    // ==================== DTOs ====================

    /// <summary>
    /// ECR data structure for PF filing
    /// </summary>
    public class PfEcrData
    {
        public Guid CompanyId { get; set; }
        public Guid? PayrollRunId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string WageMonth { get; set; } = string.Empty; // Format: MMYYYY

        // Establishment Details
        public string EstablishmentCode { get; set; } = string.Empty;
        public string EstablishmentName { get; set; } = string.Empty;
        public string? Extension { get; set; } // Sub-establishment code if any

        // Summary
        public int MemberCount { get; set; }
        public decimal TotalEpfWages { get; set; }
        public decimal TotalEpsWages { get; set; }
        public decimal TotalEdliWages { get; set; }

        // Contribution Summary
        public decimal TotalEmployeeContribution { get; set; }
        public decimal TotalEmployerEpfContribution { get; set; }
        public decimal TotalEmployerEpsContribution { get; set; }
        public decimal TotalAdminCharges { get; set; }
        public decimal TotalEdliCharges { get; set; }
        public decimal TotalContribution { get; set; }

        // Due Date
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }

        // Member Records
        public List<PfEcrMemberRecord> MemberRecords { get; set; } = new();
    }

    /// <summary>
    /// Individual member record for ECR
    /// </summary>
    public class PfEcrMemberRecord
    {
        public Guid EmployeeId { get; set; }
        public Guid PayrollTransactionId { get; set; }

        // Member Identification
        public string Uan { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string? MemberId { get; set; } // PF Account Number

        // Wages
        public decimal GrossWages { get; set; }
        public decimal EpfWages { get; set; }
        public decimal EpsWages { get; set; }
        public decimal EdliWages { get; set; }

        // Contributions
        public decimal EmployeeEpfContribution { get; set; }
        public decimal EmployerEpfContribution { get; set; }
        public decimal EmployerEpsContribution { get; set; }
        public decimal TotalContribution { get; set; }

        // NCP (Non-Contributory Period) Days
        public int NcpDays { get; set; }
        public string? NcpReason { get; set; }

        // International Worker Details (if applicable)
        public bool IsInternationalWorker { get; set; }
        public string? PassportNumber { get; set; }
        public string? CountryOfOrigin { get; set; }

        // Flags
        public bool IsNewMember { get; set; }
        public bool HasExited { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public DateTime? DateOfExit { get; set; }
    }

    /// <summary>
    /// ECR preview before filing
    /// </summary>
    public class PfEcrPreview
    {
        public PfEcrData EcrData { get; set; } = new();
        public DateOnly ProposedPaymentDate { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal DamagesAmount { get; set; }
        public decimal TotalPayable { get; set; }

        public bool HasWarnings { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<EcrValidationError> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Validation error in ECR
    /// </summary>
    public class EcrValidationError
    {
        public Guid? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Severity { get; set; } = "warning"; // warning, error
    }

    /// <summary>
    /// ECR file generation result
    /// </summary>
    public class PfEcrFileResult
    {
        public string FileName { get; set; } = string.Empty;
        public string FileContent { get; set; } = string.Empty;
        public string FileFormat { get; set; } = "txt"; // txt for EPFO upload
        public int RecordCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Base64 encoded file content for download
        /// </summary>
        public string? Base64Content { get; set; }
    }

    /// <summary>
    /// Request to create ECR payment
    /// </summary>
    public class CreatePfEcrRequest
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
    /// Request to record ECR payment
    /// </summary>
    public class RecordPfEcrPaymentRequest
    {
        public DateOnly PaymentDate { get; set; }
        public string PaymentMode { get; set; } = "online"; // online, neft, rtgs
        public string? BankName { get; set; }
        public Guid? BankAccountId { get; set; }
        public string? BankReference { get; set; }
        public decimal ActualAmountPaid { get; set; }
        public Guid? PaidBy { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Pending ECR DTO
    /// </summary>
    public class PendingEcrDto
    {
        public Guid? StatutoryPaymentId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public decimal TotalContribution { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal DamagesAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// PF ECR list item DTO for paginated responses
    /// </summary>
    public class PfEcrListDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string EstablishmentCode { get; set; } = string.Empty;
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string MonthName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public decimal TotalEmployeeContribution { get; set; }
        public decimal TotalEmployerContribution { get; set; }
        public decimal TotalAdminCharges { get; set; }
        public decimal TotalContribution { get; set; }
        public decimal Interest { get; set; }
        public decimal Damages { get; set; }
        public decimal TotalAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Trrn { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Filed ECR DTO
    /// </summary>
    public class FiledEcrDto
    {
        public Guid StatutoryPaymentId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public decimal TotalContribution { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly PaymentDate { get; set; }
        public string? Trrn { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed ECR DTO
    /// </summary>
    public class PfEcrDetailDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string EstablishmentCode { get; set; } = string.Empty;

        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string FinancialYear { get; set; } = string.Empty;

        public int MemberCount { get; set; }
        public decimal TotalEpfWages { get; set; }
        public decimal TotalEpsWages { get; set; }
        public decimal TotalEmployeeContribution { get; set; }
        public decimal TotalEmployerContribution { get; set; }
        public decimal TotalAdminCharges { get; set; }
        public decimal TotalEdliCharges { get; set; }
        public decimal TotalContribution { get; set; }

        public decimal InterestAmount { get; set; }
        public decimal DamagesAmount { get; set; }
        public decimal TotalPaid { get; set; }

        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Trrn { get; set; }
        public string? PaymentMode { get; set; }
        public string? BankName { get; set; }
        public string? BankReference { get; set; }

        public List<PfEcrMemberRecord> MemberRecords { get; set; } = new();
    }

    /// <summary>
    /// Monthly PF ECR summary
    /// </summary>
    public class PfEcrSummary
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public List<MonthlyPfStatus> MonthlyStatus { get; set; } = new();
        public decimal TotalPfDeducted { get; set; }
        public decimal TotalPfDeposited { get; set; }
        public decimal TotalVariance { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
    }

    /// <summary>
    /// Monthly PF status
    /// </summary>
    public class MonthlyPfStatus
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public decimal PfDeducted { get; set; }
        public decimal PfDeposited { get; set; }
        public decimal Variance { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Trrn { get; set; }
        public Guid? StatutoryPaymentId { get; set; }
    }

    /// <summary>
    /// PF reconciliation result
    /// </summary>
    public class PfReconciliation
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;

        public decimal TotalPfDeducted { get; set; }
        public decimal TotalPfDeposited { get; set; }
        public decimal Variance { get; set; }
        public bool IsReconciled => Variance == 0;

        public List<PfReconciliationItem> MonthlyReconciliation { get; set; } = new();
        public List<PfReconciliationMismatch> Mismatches { get; set; } = new();
    }

    /// <summary>
    /// Monthly reconciliation item
    /// </summary>
    public class PfReconciliationItem
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int MemberCount { get; set; }
        public decimal Deducted { get; set; }
        public decimal Deposited { get; set; }
        public decimal Variance { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Reconciliation mismatch
    /// </summary>
    public class PfReconciliationMismatch
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

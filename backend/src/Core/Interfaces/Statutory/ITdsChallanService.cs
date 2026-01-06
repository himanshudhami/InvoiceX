using Core.Common;
using Core.Entities.Payroll;

namespace Core.Interfaces.Statutory
{
    /// <summary>
    /// Service interface for TDS Challan 281 generation and management.
    /// Challan 281 is used for depositing TDS to the government.
    ///
    /// Key Components:
    /// - BSR Code: Bank branch code
    /// - CIN: Challan Identification Number (BSR + Date + Serial)
    /// - Minor Head: 200 (Salary TDS) or 206 (Non-Salary TDS)
    ///
    /// Due Dates:
    /// - 7th of following month for regular months
    /// - 30th April for March (end of FY)
    ///
    /// Interest: 1.5% per month on late payment from due date to deposit date
    /// </summary>
    public interface ITdsChallanService
    {
        // ==================== Challan Generation ====================

        /// <summary>
        /// Generate TDS challan data for a month.
        /// Aggregates TDS from payroll and contractor payments.
        /// </summary>
        Task<Result<TdsChallanData>> GenerateChallanAsync(
            Guid companyId,
            int periodMonth,
            int periodYear,
            string challanType); // "salary" (192) or "non-salary" (26Q)

        /// <summary>
        /// Generate monthly TDS challan summary for dashboard.
        /// </summary>
        Task<Result<TdsChalllanSummary>> GetMonthlySummaryAsync(
            Guid companyId,
            string financialYear);

        /// <summary>
        /// Preview challan before creating payment record.
        /// Includes interest calculation if depositing late.
        /// </summary>
        Task<Result<TdsChallanPreview>> PreviewChallanAsync(
            Guid companyId,
            int periodMonth,
            int periodYear,
            string challanType,
            DateOnly? proposedPaymentDate = null);

        // ==================== Challan Operations ====================

        /// <summary>
        /// Create statutory payment record for challan.
        /// </summary>
        Task<Result<StatutoryPayment>> CreateChallanPaymentAsync(
            CreateTdsChallanRequest request);

        /// <summary>
        /// Record challan deposit with bank details.
        /// </summary>
        Task<Result<StatutoryPayment>> RecordChallanDepositAsync(
            Guid paymentId,
            RecordChallanDepositRequest request);

        /// <summary>
        /// Update CIN (Challan Identification Number) after bank confirmation.
        /// CIN is required for Form 24Q/26Q filing.
        /// </summary>
        Task<Result<StatutoryPayment>> UpdateCinAsync(
            Guid paymentId,
            UpdateCinRequest request);

        /// <summary>
        /// Verify challan against OLTAS/TIN-NSDL.
        /// </summary>
        Task<Result<ChallanVerificationResult>> VerifyChallanAsync(
            Guid paymentId,
            ChallanVerificationRequest request);

        // ==================== Retrieval ====================

        /// <summary>
        /// Get paginated list of TDS challans.
        /// </summary>
        Task<Result<(IEnumerable<TdsChallanListDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null,
            string? searchTerm = null);

        /// <summary>
        /// Get pending challans (unpaid).
        /// </summary>
        Task<Result<IEnumerable<PendingChallanDto>>> GetPendingChallansAsync(
            Guid companyId,
            string? financialYear = null);

        /// <summary>
        /// Get overdue challans.
        /// </summary>
        Task<Result<IEnumerable<OverdueChallanDto>>> GetOverdueChallansAsync(
            Guid companyId);

        /// <summary>
        /// Get paid challans for reconciliation.
        /// </summary>
        Task<Result<IEnumerable<PaidChallanDto>>> GetPaidChallansAsync(
            Guid companyId,
            string financialYear,
            string? quarter = null);

        /// <summary>
        /// Get challan by ID with full details.
        /// </summary>
        Task<Result<TdsChallanDetailDto>> GetChallanByIdAsync(Guid paymentId);

        // ==================== Interest & Penalty Calculation ====================

        /// <summary>
        /// Calculate interest on late TDS deposit.
        /// Rate: 1.5% per month (simple interest)
        /// </summary>
        decimal CalculateInterest(
            decimal tdsAmount,
            DateOnly dueDate,
            DateOnly actualPaymentDate);

        /// <summary>
        /// Get due date for TDS payment.
        /// 7th of following month, except March (30th April)
        /// </summary>
        DateOnly GetDueDate(int periodMonth, int periodYear);

        // ==================== Reconciliation ====================

        /// <summary>
        /// Reconcile TDS deducted vs deposited for a period.
        /// </summary>
        Task<Result<TdsChallanReconciliation>> ReconcileAsync(
            Guid companyId,
            string financialYear,
            string? quarter = null);

        /// <summary>
        /// Link challan to payroll transactions.
        /// </summary>
        Task<Result> LinkChallanToPayrollAsync(
            Guid challanPaymentId,
            IEnumerable<Guid> payrollTransactionIds);
    }

    // ==================== DTOs ====================

    /// <summary>
    /// TDS Challan 281 data structure
    /// </summary>
    public class TdsChallanData
    {
        public Guid CompanyId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string ChallanType { get; set; } = "salary"; // salary, non-salary
        public string FinancialYear { get; set; } = string.Empty;

        // Deductor Details
        public string Tan { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;

        // Challan Details
        public string MajorHead { get; set; } = "0021"; // Income Tax (other than companies)
        public string MinorHead { get; set; } = "200"; // 200 = Salary, 206 = Others
        public string AssessmentYear { get; set; } = string.Empty;

        // Amount Breakdown
        public decimal TdsAmount { get; set; }
        public decimal SurchargeAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Due Date Info
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }

        // Source Records
        public int EmployeeCount { get; set; }
        public int TransactionCount { get; set; }
        public List<TdsChallanSourceRecord> SourceRecords { get; set; } = new();
    }

    /// <summary>
    /// Source record for TDS challan
    /// </summary>
    public class TdsChallanSourceRecord
    {
        public Guid SourceId { get; set; }
        public string SourceType { get; set; } = string.Empty; // payroll_transaction, contractor_payment
        public string DeducteeName { get; set; } = string.Empty;
        public string DeducteePan { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal TdsAmount { get; set; }
        public decimal SurchargeAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalTds { get; set; }
    }

    /// <summary>
    /// Monthly TDS challan summary
    /// </summary>
    public class TdsChalllanSummary
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public List<MonthlyTdsChallanStatus> MonthlyStatus { get; set; } = new();
        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal TotalVariance { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
    }

    /// <summary>
    /// Monthly status for challan
    /// </summary>
    public class MonthlyTdsChallanStatus
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TdsDeducted { get; set; }
        public decimal TdsDeposited { get; set; }
        public decimal Variance { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty; // pending, paid, overdue
        public string? ChallanNumber { get; set; }
        public Guid? StatutoryPaymentId { get; set; }
    }

    /// <summary>
    /// Challan preview with interest calculation
    /// </summary>
    public class TdsChallanPreview
    {
        public TdsChallanData ChallanData { get; set; } = new();
        public DateOnly ProposedPaymentDate { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public string InterestCalculation { get; set; } = string.Empty;
        public bool HasWarnings { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Request to create challan payment
    /// </summary>
    public class CreateTdsChallanRequest
    {
        public Guid CompanyId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string ChallanType { get; set; } = "salary";
        public DateOnly ProposedPaymentDate { get; set; }
        public Guid? CreatedBy { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to record challan deposit
    /// </summary>
    public class RecordChallanDepositRequest
    {
        public DateOnly PaymentDate { get; set; }
        public string PaymentMode { get; set; } = "online"; // online, neft, rtgs, challan
        public string BsrCode { get; set; } = string.Empty;
        public string ChallanNumber { get; set; } = string.Empty;
        public string? ReceiptNumber { get; set; } // CIN
        public string? BankName { get; set; }
        public Guid? BankAccountId { get; set; }
        public string? BankReference { get; set; }
        public decimal ActualAmountPaid { get; set; }
        public Guid? PaidBy { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to update CIN after bank confirmation.
    /// CIN (Challan Identification Number) is a 20-digit unique number
    /// assigned by the bank after TDS deposit.
    /// Required for Form 24Q/26Q quarterly filing.
    /// </summary>
    public class UpdateCinRequest
    {
        /// <summary>
        /// BSR Code (7 digits) - Bank branch code assigned by RBI
        /// </summary>
        public string BsrCode { get; set; } = string.Empty;

        /// <summary>
        /// CIN (Challan Identification Number) - Full 20-digit number
        /// Format: BSR Code (7) + Deposit Date (DDMMYYYY) + Challan Serial (5)
        /// </summary>
        public string Cin { get; set; } = string.Empty;

        /// <summary>
        /// Challan serial number (5 digits) - Part of CIN
        /// </summary>
        public string? ChallanSerialNumber { get; set; }

        /// <summary>
        /// Date of deposit as confirmed by bank (part of CIN)
        /// </summary>
        public DateOnly? DepositDate { get; set; }

        /// <summary>
        /// Optional remarks for audit trail
        /// </summary>
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Request for challan verification
    /// </summary>
    public class ChallanVerificationRequest
    {
        public Guid VerifiedBy { get; set; }
        public string? OltasReference { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Result of challan verification
    /// </summary>
    public class ChallanVerificationResult
    {
        public bool IsVerified { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? OltasStatus { get; set; }
        public string? Remarks { get; set; }
        public DateTime VerifiedAt { get; set; }
        public Guid? VerifiedBy { get; set; }
    }

    /// <summary>
    /// TDS Challan list item DTO for paginated responses
    /// </summary>
    public class TdsChallanListDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Tan { get; set; } = string.Empty;
        public string ChallanType { get; set; } = string.Empty;
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string MonthName { get; set; } = string.Empty;
        public decimal BasicTax { get; set; }
        public decimal Surcharge { get; set; }
        public decimal EducationCess { get; set; }
        public decimal Interest { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? BsrCode { get; set; }
        public string? CinNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Pending challan DTO
    /// </summary>
    public class PendingChallanDto
    {
        public Guid? StatutoryPaymentId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public string ChallanType { get; set; } = string.Empty;
        public decimal TdsAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Overdue challan DTO
    /// </summary>
    public class OverdueChallanDto : PendingChallanDto
    {
        public decimal EstimatedInterest { get; set; }
        public decimal TotalWithInterest { get; set; }
        public string UrgencyLevel { get; set; } = string.Empty; // low, medium, high, critical
    }

    /// <summary>
    /// Paid challan DTO
    /// </summary>
    public class PaidChallanDto
    {
        public Guid StatutoryPaymentId { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public string ChallanType { get; set; } = string.Empty;
        public decimal TdsAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly PaymentDate { get; set; }
        public string BsrCode { get; set; } = string.Empty;
        public string? ChallanNumber { get; set; }
        public string? Cin { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed challan DTO
    /// </summary>
    public class TdsChallanDetailDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Tan { get; set; } = string.Empty;

        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string ChallanType { get; set; } = string.Empty;

        public decimal TdsAmount { get; set; }
        public decimal SurchargeAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;

        public string? BsrCode { get; set; }
        public string? ChallanNumber { get; set; }
        public string? Cin { get; set; }
        public string? PaymentMode { get; set; }
        public string? BankName { get; set; }
        public string? BankReference { get; set; }

        public DateTime? VerifiedAt { get; set; }
        public Guid? VerifiedBy { get; set; }

        public List<TdsChallanSourceRecord> SourceRecords { get; set; } = new();
    }

    /// <summary>
    /// TDS challan reconciliation result
    /// </summary>
    public class TdsChallanReconciliation
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string? Quarter { get; set; }

        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }
        public decimal Variance { get; set; }
        public bool IsReconciled => Variance == 0;

        public List<ReconciliationItem> SalaryTds { get; set; } = new();
        public List<ReconciliationItem> NonSalaryTds { get; set; } = new();
        public List<ReconciliationMismatch> Mismatches { get; set; } = new();
    }

    /// <summary>
    /// Reconciliation item
    /// </summary>
    public class ReconciliationItem
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Deducted { get; set; }
        public decimal Deposited { get; set; }
        public decimal Variance { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Reconciliation mismatch
    /// </summary>
    public class ReconciliationMismatch
    {
        public string MismatchType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? Month { get; set; }
        public decimal Amount { get; set; }
        public string? SuggestedAction { get; set; }
    }
}

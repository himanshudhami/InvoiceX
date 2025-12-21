namespace Application.DTOs.Forex
{
    /// <summary>
    /// DTO for creating a new FIRC entry
    /// </summary>
    public class CreateFircDto
    {
        public Guid CompanyId { get; set; }
        public string? FircNumber { get; set; }
        public DateOnly? FircDate { get; set; }
        public required string BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? BankSwiftCode { get; set; }
        public required string PurposeCode { get; set; }
        public required string ForeignCurrency { get; set; }
        public decimal ForeignAmount { get; set; }
        public decimal InrAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public string? RemitterName { get; set; }
        public string? RemitterCountry { get; set; }
        public string? RemitterBank { get; set; }
        public string BeneficiaryName { get; set; } = string.Empty;
        public string? BeneficiaryAccount { get; set; }
        public Guid? PaymentId { get; set; }
        public string? Notes { get; set; }
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// DTO for updating a FIRC entry
    /// </summary>
    public class UpdateFircDto
    {
        public string? FircNumber { get; set; }
        public DateOnly? FircDate { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? BankSwiftCode { get; set; }
        public string? PurposeCode { get; set; }
        public string? RemitterName { get; set; }
        public string? RemitterCountry { get; set; }
        public string? RemitterBank { get; set; }
        public string? BeneficiaryName { get; set; }
        public string? BeneficiaryAccount { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for allocating FIRC amount to invoices
    /// </summary>
    public class FircInvoiceAllocationDto
    {
        public Guid InvoiceId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal? AllocatedAmountInr { get; set; }
    }

    /// <summary>
    /// Result of FIRC auto-matching process
    /// </summary>
    public class FircAutoMatchResultDto
    {
        public int FircsProcessed { get; set; }
        public int FircsMatched { get; set; }
        public int FircsSkipped { get; set; }
        public decimal TotalAmountMatched { get; set; }
        public List<FircMatchDto> Matches { get; set; } = new();
    }

    /// <summary>
    /// Individual FIRC match details
    /// </summary>
    public class FircMatchDto
    {
        public Guid FircId { get; set; }
        public Guid PaymentId { get; set; }
        public string? FircNumber { get; set; }
        public decimal FircAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal AmountDifference { get; set; }
        public int DaysDifference { get; set; }
        public int MatchScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// EDPMS compliance summary
    /// </summary>
    public class EdpmsComplianceSummaryDto
    {
        public Guid CompanyId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public int TotalFircs { get; set; }
        public int ReportedCount { get; set; }
        public int PendingCount { get; set; }
        public decimal TotalReportedAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public double CompliancePercentage => TotalFircs > 0 ? Math.Round((double)ReportedCount / TotalFircs * 100, 2) : 100;
        public List<PendingEdpmsItemDto> PendingItems { get; set; } = new();
    }

    /// <summary>
    /// Individual pending EDPMS item
    /// </summary>
    public class PendingEdpmsItemDto
    {
        public Guid FircId { get; set; }
        public string? FircNumber { get; set; }
        public DateOnly? FircDate { get; set; }
        public string BankName { get; set; } = string.Empty;
        public decimal ForeignAmount { get; set; }
        public string ForeignCurrency { get; set; } = string.Empty;
        public decimal InrAmount { get; set; }
        public int DaysPending { get; set; }
    }

    /// <summary>
    /// Alert for invoices approaching realization deadline
    /// </summary>
    public class RealizationAlertDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public DateOnly DeadlineDate { get; set; }
        public int DaysRemaining { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal ForeignAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal? AmountInr { get; set; }
        public decimal AmountRealized { get; set; }
        public decimal AmountPending { get; set; }
        public string AlertLevel { get; set; } = "normal";  // normal, warning, critical, overdue
    }

    /// <summary>
    /// Realization status for an export invoice
    /// </summary>
    public class RealizationStatusDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public DateOnly DeadlineDate { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal ForeignAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal? AmountInr { get; set; }
        public decimal AmountRealized { get; set; }
        public decimal AmountPending { get; set; }
        public bool IsFullyRealized => AmountPending <= 0.01m;
        public bool IsOverdue { get; set; }
        public int DaysToDeadline { get; set; }
        public string Status { get; set; } = string.Empty;  // realized, partially_realized, pending, overdue
        public List<RealizationPaymentDto> Payments { get; set; } = new();
    }

    /// <summary>
    /// Payment details for realization
    /// </summary>
    public class RealizationPaymentDto
    {
        public Guid PaymentId { get; set; }
        public DateOnly PaymentDate { get; set; }
        public decimal ForeignAmount { get; set; }
        public decimal InrAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public Guid? FircId { get; set; }
        public string? FircNumber { get; set; }
    }

    /// <summary>
    /// Summary of realization status
    /// </summary>
    public class RealizationSummaryDto
    {
        public Guid CompanyId { get; set; }
        public DateOnly AsOfDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Invoice counts
        public int TotalExportInvoices { get; set; }
        public int FullyRealizedCount { get; set; }
        public int PartiallyRealizedCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }

        // Amounts in foreign currency (USD)
        public decimal TotalExportAmount { get; set; }
        public decimal TotalRealizedAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public decimal TotalOverdueAmount { get; set; }
        public string PrimaryCurrency { get; set; } = "USD";

        // Amounts in INR
        public decimal TotalExportAmountInr { get; set; }
        public decimal TotalRealizedAmountInr { get; set; }
        public decimal TotalPendingAmountInr { get; set; }
        public decimal TotalOverdueAmountInr { get; set; }

        // Ratios
        public double RealizationPercentage => TotalExportAmount > 0
            ? Math.Round((double)TotalRealizedAmount / (double)TotalExportAmount * 100, 2)
            : 0;

        // Breakdowns by currency
        public Dictionary<string, CurrencyRealizationDto> CurrencyBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Realization breakdown by currency
    /// </summary>
    public class CurrencyRealizationDto
    {
        public string Currency { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RealizedAmount { get; set; }
        public decimal PendingAmount { get; set; }
    }

    /// <summary>
    /// FIRC reconciliation report
    /// </summary>
    public class FircReconciliationReportDto
    {
        public Guid CompanyId { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // FIRC summary
        public int TotalFircs { get; set; }
        public int LinkedToPayments { get; set; }
        public int LinkedToInvoices { get; set; }
        public int UnlinkedFircs { get; set; }
        public decimal TotalFircAmountForeign { get; set; }
        public decimal TotalFircAmountInr { get; set; }

        // Reconciliation status
        public int FullyReconciledCount { get; set; }
        public int PartiallyReconciledCount { get; set; }
        public int UnreconciledCount { get; set; }

        // Discrepancies
        public decimal TotalDiscrepancyAmount { get; set; }
        public List<FircDiscrepancyDto> Discrepancies { get; set; } = new();

        // Details
        public List<FircReconciliationItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Individual FIRC reconciliation item
    /// </summary>
    public class FircReconciliationItemDto
    {
        public Guid FircId { get; set; }
        public string? FircNumber { get; set; }
        public DateOnly? FircDate { get; set; }
        public string BankName { get; set; } = string.Empty;
        public decimal ForeignAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal InrAmount { get; set; }
        public string Status { get; set; } = string.Empty;

        // Linked payment
        public Guid? PaymentId { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public decimal? PaymentAmount { get; set; }
        public decimal? PaymentAmountInr { get; set; }

        // Linked invoices
        public int LinkedInvoiceCount { get; set; }
        public decimal TotalAllocatedAmount { get; set; }

        // EDPMS
        public bool EdpmsReported { get; set; }
        public DateOnly? EdpmsReportDate { get; set; }

        // Reconciliation
        public string ReconciliationStatus { get; set; } = string.Empty;  // full, partial, none
        public decimal? AmountDifference { get; set; }
    }

    /// <summary>
    /// FIRC discrepancy details
    /// </summary>
    public class FircDiscrepancyDto
    {
        public Guid FircId { get; set; }
        public string? FircNumber { get; set; }
        public string DiscrepancyType { get; set; } = string.Empty;  // amount_mismatch, missing_payment, missing_invoice
        public string Description { get; set; } = string.Empty;
        public decimal? ExpectedAmount { get; set; }
        public decimal? ActualAmount { get; set; }
        public decimal? Difference { get; set; }
    }

    /// <summary>
    /// FIRC validation result
    /// </summary>
    public class FircValidationResultDto
    {
        public Guid FircId { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool HasMissingFircNumber { get; set; }
        public bool HasMissingFircDate { get; set; }
        public bool HasMissingPaymentLink { get; set; }
        public bool HasMissingInvoiceLink { get; set; }
        public bool HasEdpmsPending { get; set; }
        public bool HasAmountMismatch { get; set; }
    }
}

namespace Application.DTOs.Gst
{
    /// <summary>
    /// ITC (Input Tax Credit) refund claim
    /// </summary>
    public class ItcRefundClaimDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string RefundType { get; set; } = string.Empty;  // export_without_payment, inverted_duty, accumulated_itc
        public string ReturnPeriod { get; set; } = string.Empty;  // MMYYYY
        public DateOnly ClaimDate { get; set; }

        // Amounts
        public decimal TurnoverOfZeroRatedSupply { get; set; }
        public decimal AdjustedTotalTurnover { get; set; }
        public decimal NetItcAvailable { get; set; }
        public decimal RefundClaimAmount { get; set; }

        // Status
        public string Status { get; set; } = "pending";  // pending, filed, sanctioned, rejected
        public string? Arn { get; set; }
        public DateOnly? FilingDate { get; set; }
        public decimal? SanctionedAmount { get; set; }
        public DateOnly? SanctionDate { get; set; }
        public string? RejectionReason { get; set; }

        // Bank details for refund
        public string? BankAccountNumber { get; set; }
        public string? BankIfsc { get; set; }
        public string? BankName { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating an ITC refund claim
    /// </summary>
    public class CreateItcRefundClaimDto
    {
        public Guid CompanyId { get; set; }
        public required string RefundType { get; set; }
        public required string ReturnPeriod { get; set; }
        public decimal TurnoverOfZeroRatedSupply { get; set; }
        public decimal AdjustedTotalTurnover { get; set; }
        public decimal NetItcAvailable { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankIfsc { get; set; }
        public string? BankName { get; set; }
    }

    /// <summary>
    /// ITC refund eligibility calculation
    /// </summary>
    public class ItcRefundEligibilityDto
    {
        public Guid CompanyId { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        // Export turnover
        public decimal TotalExportTurnover { get; set; }
        public int ExportInvoiceCount { get; set; }

        // Total turnover
        public decimal TotalTurnover { get; set; }
        public int TotalInvoiceCount { get; set; }

        // ITC details
        public decimal TotalItcAvailable { get; set; }
        public decimal ItcOnInputs { get; set; }
        public decimal ItcOnCapitalGoods { get; set; }
        public decimal ItcOnInputServices { get; set; }

        // Refund calculation (Formula: Max Refund = (Exports / Total Turnover) x Net ITC)
        public decimal ExportTurnoverRatio => TotalTurnover > 0 ? TotalExportTurnover / TotalTurnover : 0;
        public decimal MaxRefundAmount => Math.Round(ExportTurnoverRatio * TotalItcAvailable, 2);

        // Eligibility
        public bool IsEligible => MaxRefundAmount > 0 && TotalExportTurnover > 0;
        public List<string> EligibilityNotes { get; set; } = new();
    }

    /// <summary>
    /// ITC refund summary for a period
    /// </summary>
    public class ItcRefundSummaryDto
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Claims summary
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int SanctionedClaims { get; set; }
        public int RejectedClaims { get; set; }

        // Amounts
        public decimal TotalClaimedAmount { get; set; }
        public decimal TotalSanctionedAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public decimal TotalRejectedAmount { get; set; }

        // Sanction rate
        public double SanctionRate => TotalClaimedAmount > 0
            ? Math.Round((double)TotalSanctionedAmount / (double)TotalClaimedAmount * 100, 2)
            : 0;

        // Average processing time
        public double? AvgProcessingDays { get; set; }

        // Recent claims
        public List<ItcRefundClaimDto> RecentClaims { get; set; } = new();
    }

    /// <summary>
    /// ITC ledger entry for tracking accumulated credits
    /// </summary>
    public class ItcLedgerEntryDto
    {
        public DateOnly Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;  // purchase, refund_claim, reversal
        public decimal IgstCredit { get; set; }
        public decimal IgstDebit { get; set; }
        public decimal CgstCredit { get; set; }
        public decimal CgstDebit { get; set; }
        public decimal SgstCredit { get; set; }
        public decimal SgstDebit { get; set; }
        public decimal CessCredit { get; set; }
        public decimal CessDebit { get; set; }
        public decimal RunningBalanceIgst { get; set; }
        public decimal RunningBalanceCgst { get; set; }
        public decimal RunningBalanceSgst { get; set; }
        public decimal RunningBalanceCess { get; set; }
    }
}

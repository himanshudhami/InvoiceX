namespace Core.Entities
{
    /// <summary>
    /// TDS Receivable - tracks TDS deducted by customers for Form 26AS reconciliation
    /// TDS credits can be claimed while filing income tax returns
    /// </summary>
    public class TdsReceivable
    {
        public Guid Id { get; set; }

        // ==================== Company Linking ====================

        /// <summary>
        /// Company that received the payment (and had TDS deducted)
        /// </summary>
        public Guid CompanyId { get; set; }

        // ==================== Financial Period ====================

        /// <summary>
        /// Indian financial year in '2024-25' format
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Quarter: Q1 (Apr-Jun), Q2 (Jul-Sep), Q3 (Oct-Dec), Q4 (Jan-Mar)
        /// </summary>
        public string Quarter { get; set; } = string.Empty;

        // ==================== Deductor (Customer) Details ====================

        /// <summary>
        /// Optional customer reference
        /// </summary>
        public Guid? PartyId { get; set; }

        /// <summary>
        /// Name of the entity that deducted TDS
        /// </summary>
        public string DeductorName { get; set; } = string.Empty;

        /// <summary>
        /// Tax Deduction Account Number of the deductor
        /// </summary>
        public string? DeductorTan { get; set; }

        /// <summary>
        /// PAN of the deductor
        /// </summary>
        public string? DeductorPan { get; set; }

        // ==================== Transaction Details ====================

        /// <summary>
        /// Date when the payment was received
        /// </summary>
        public DateOnly PaymentDate { get; set; }

        /// <summary>
        /// TDS section under which tax was deducted
        /// Common: 194J (Professional 10%), 194C (Contractor 1-2%), 194H (Commission 5%)
        /// </summary>
        public string TdsSection { get; set; } = string.Empty;

        /// <summary>
        /// Gross amount before TDS deduction
        /// </summary>
        public decimal GrossAmount { get; set; }

        /// <summary>
        /// TDS rate applied (percentage)
        /// </summary>
        public decimal TdsRate { get; set; }

        /// <summary>
        /// TDS amount deducted
        /// </summary>
        public decimal TdsAmount { get; set; }

        /// <summary>
        /// Net amount received after TDS deduction
        /// </summary>
        public decimal NetReceived { get; set; }

        // ==================== Certificate Details (Form 16A) ====================

        /// <summary>
        /// TDS certificate number from Form 16A
        /// </summary>
        public string? CertificateNumber { get; set; }

        /// <summary>
        /// Date on the TDS certificate
        /// </summary>
        public DateOnly? CertificateDate { get; set; }

        /// <summary>
        /// Whether the certificate has been downloaded from TRACES
        /// </summary>
        public bool CertificateDownloaded { get; set; }

        // ==================== Linked Records ====================

        /// <summary>
        /// Link to the payment record
        /// </summary>
        public Guid? PaymentId { get; set; }

        /// <summary>
        /// Link to the invoice if applicable
        /// </summary>
        public Guid? InvoiceId { get; set; }

        // ==================== 26AS Matching ====================

        /// <summary>
        /// Whether this entry has been matched with Form 26AS
        /// </summary>
        public bool MatchedWith26As { get; set; }

        /// <summary>
        /// Amount as per Form 26AS (may differ from TdsAmount)
        /// </summary>
        public decimal? Form26AsAmount { get; set; }

        /// <summary>
        /// Difference between our records and 26AS
        /// </summary>
        public decimal? AmountDifference { get; set; }

        /// <summary>
        /// When the 26AS matching was performed
        /// </summary>
        public DateTime? MatchedAt { get; set; }

        // ==================== Claiming Status ====================

        /// <summary>
        /// Status: pending, matched, claimed, disputed, written_off
        /// </summary>
        public string Status { get; set; } = "pending";

        /// <summary>
        /// ITR in which this credit was claimed (e.g., 'ITR-2024-25')
        /// </summary>
        public string? ClaimedInReturn { get; set; }

        // ==================== Additional Info ====================

        /// <summary>
        /// Additional notes
        /// </summary>
        public string? Notes { get; set; }

        // ==================== Timestamps ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
        public Customers? Customer { get; set; }
        public Payments? Payment { get; set; }
        public Invoices? Invoice { get; set; }
    }
}

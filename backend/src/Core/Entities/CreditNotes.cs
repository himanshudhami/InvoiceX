namespace Core.Entities
{
    /// <summary>
    /// Credit Note entity as per Section 34 of CGST Act.
    /// Must reference an original invoice and include reason for issuance.
    /// </summary>
    public class CreditNotes
    {
        // Identity
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? PartyId { get; set; }

        // Credit Note identification
        public string CreditNoteNumber { get; set; } = string.Empty;
        public DateOnly CreditNoteDate { get; set; }

        // Original invoice reference (MANDATORY as per GST)
        public Guid OriginalInvoiceId { get; set; }
        public string OriginalInvoiceNumber { get; set; } = string.Empty;
        public DateOnly OriginalInvoiceDate { get; set; }

        // Reason for credit note (required for GST compliance)
        public string Reason { get; set; } = string.Empty;  // goods_returned, post_sale_discount, deficiency_in_services, excess_amount_charged, excess_tax_charged, change_in_pos, export_refund, other
        public string? ReasonDescription { get; set; }

        // Status
        public string? Status { get; set; } = "draft";  // draft, issued, cancelled

        // Financial details
        public decimal Subtotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Currency { get; set; } = "INR";

        // Additional details
        public string? Notes { get; set; }
        public string? Terms { get; set; }

        // Timestamps
        public DateTime? IssuedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // GST Classification
        public string? InvoiceType { get; set; }
        public string? SupplyType { get; set; }
        public string? PlaceOfSupply { get; set; }
        public bool ReverseCharge { get; set; }

        // GST Totals
        public decimal TotalCgst { get; set; }
        public decimal TotalSgst { get; set; }
        public decimal TotalIgst { get; set; }
        public decimal TotalCess { get; set; }

        // E-invoicing fields
        public bool EInvoiceApplicable { get; set; }
        public string? Irn { get; set; }
        public DateTime? IrnGeneratedAt { get; set; }
        public DateTime? IrnCancelledAt { get; set; }
        public string? QrCodeData { get; set; }
        public string? EInvoiceSignedJson { get; set; }
        public string? EInvoiceStatus { get; set; } = "not_applicable";

        // Forex
        public string? ForeignCurrency { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? AmountInInr { get; set; }

        // 2025 Amendment: ITC Reversal Tracking
        public bool ItcReversalRequired { get; set; }
        public bool ItcReversalConfirmed { get; set; }
        public DateOnly? ItcReversalDate { get; set; }
        public string? ItcReversalCertificate { get; set; }

        // GSTR-1 Reporting
        public bool ReportedInGstr1 { get; set; }
        public string? Gstr1Period { get; set; }
        public DateOnly? Gstr1FilingDate { get; set; }

        // Time limit tracking
        public DateOnly? TimeBarredDate { get; set; }
        public bool IsTimeBarred { get; set; }
    }
}

namespace Core.Entities.Gst
{
    /// <summary>
    /// Individual line item in GSTR-3B tables.
    /// Supports drill-down to source documents (invoices, vendor invoices, RCM).
    /// </summary>
    public class Gstr3bLineItem
    {
        public Guid Id { get; set; }
        public Guid FilingId { get; set; }

        // ==================== Table Reference ====================

        /// <summary>
        /// Table code (e.g., '3.1(a)', '4(A)(1)', '5(a)')
        /// </summary>
        public string TableCode { get; set; } = string.Empty;

        /// <summary>
        /// Display order within the table
        /// </summary>
        public int RowOrder { get; set; }

        /// <summary>
        /// Description of the line item
        /// </summary>
        public string Description { get; set; } = string.Empty;

        // ==================== Amounts ====================

        public decimal TaxableValue { get; set; }
        public decimal Igst { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Cess { get; set; }

        /// <summary>
        /// Total GST (IGST + CGST + SGST + CESS)
        /// </summary>
        public decimal TotalGst => Igst + Cgst + Sgst + Cess;

        // ==================== Source Tracking ====================

        /// <summary>
        /// Number of source documents contributing to this line
        /// </summary>
        public int SourceCount { get; set; }

        /// <summary>
        /// Primary source type: invoice, vendor_invoice, rcm_transaction, manual
        /// </summary>
        public string? SourceType { get; set; }

        /// <summary>
        /// JSON array of source document IDs
        /// </summary>
        public string? SourceIdsJson { get; set; }

        // ==================== Notes ====================

        public string? ComputationNotes { get; set; }

        // ==================== Timestamps ====================

        public DateTime CreatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Gstr3bFiling? Filing { get; set; }
        public ICollection<Gstr3bSourceDocument> SourceDocuments { get; set; } = new List<Gstr3bSourceDocument>();
    }

    /// <summary>
    /// Source document linked to a GSTR-3B line item for drill-down.
    /// </summary>
    public class Gstr3bSourceDocument
    {
        public Guid Id { get; set; }
        public Guid LineItemId { get; set; }

        // ==================== Source Reference ====================

        /// <summary>
        /// Source type: invoice, vendor_invoice, rcm_transaction, credit_note, debit_note
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        public Guid SourceId { get; set; }
        public string? SourceNumber { get; set; }
        public DateTime? SourceDate { get; set; }

        // ==================== Amounts Contributed ====================

        public decimal TaxableValue { get; set; }
        public decimal Igst { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Cess { get; set; }

        // ==================== Party Details ====================

        public string? PartyName { get; set; }
        public string? PartyGstin { get; set; }

        // ==================== Timestamps ====================

        public DateTime CreatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Gstr3bLineItem? LineItem { get; set; }
    }

    /// <summary>
    /// Source document types for GSTR-3B
    /// </summary>
    public static class Gstr3bSourceTypes
    {
        public const string Invoice = "invoice";
        public const string VendorInvoice = "vendor_invoice";
        public const string RcmTransaction = "rcm_transaction";
        public const string CreditNote = "credit_note";
        public const string DebitNote = "debit_note";
        public const string Manual = "manual";
    }
}

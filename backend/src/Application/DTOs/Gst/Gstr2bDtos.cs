using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Gst
{
    // ==================== Import DTOs ====================

    /// <summary>
    /// DTO for GSTR-2B import record
    /// </summary>
    public class Gstr2bImportDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;
        public string Gstin { get; set; } = string.Empty;

        public string ImportSource { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string ImportStatus { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }

        public int TotalInvoices { get; set; }
        public int MatchedInvoices { get; set; }
        public int UnmatchedInvoices { get; set; }
        public int PartiallyMatchedInvoices { get; set; }

        public decimal TotalItcIgst { get; set; }
        public decimal TotalItcCgst { get; set; }
        public decimal TotalItcSgst { get; set; }
        public decimal TotalItcCess { get; set; }
        public decimal TotalItcAmount { get; set; }
        public decimal MatchedItcAmount { get; set; }

        public DateTime? ImportedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to import GSTR-2B JSON data
    /// </summary>
    public class ImportGstr2bRequestDto
    {
        [Required]
        public Guid CompanyId { get; set; }

        [Required]
        [StringLength(20)]
        public string ReturnPeriod { get; set; } = string.Empty;  // Format: 'Jan-2025'

        [Required]
        public string JsonData { get; set; } = string.Empty;

        public string? FileName { get; set; }
    }

    // ==================== Invoice DTOs ====================

    /// <summary>
    /// DTO for GSTR-2B invoice record
    /// </summary>
    public class Gstr2bInvoiceDto
    {
        public Guid Id { get; set; }
        public Guid ImportId { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;

        // Supplier
        public string SupplierGstin { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public string? SupplierTradeName { get; set; }

        // Invoice
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public string? InvoiceType { get; set; }
        public string? DocumentType { get; set; }

        // Amounts
        public decimal TaxableValue { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalGst { get; set; }
        public decimal TotalInvoiceValue { get; set; }

        // ITC
        public bool ItcEligible { get; set; }
        public decimal ItcIgst { get; set; }
        public decimal ItcCgst { get; set; }
        public decimal ItcSgst { get; set; }
        public decimal ItcCess { get; set; }
        public decimal TotalItc => ItcIgst + ItcCgst + ItcSgst + ItcCess;

        // Supply
        public string? PlaceOfSupply { get; set; }
        public string? SupplyType { get; set; }
        public bool ReverseCharge { get; set; }

        // Matching
        public string MatchStatus { get; set; } = string.Empty;
        public Guid? MatchedVendorInvoiceId { get; set; }
        public string? MatchedVendorInvoiceNumber { get; set; }
        public int? MatchConfidence { get; set; }
        public List<string>? Discrepancies { get; set; }

        // Action
        public string? ActionStatus { get; set; }
        public string? ActionNotes { get; set; }
    }

    /// <summary>
    /// Invoice list item (lighter version)
    /// </summary>
    public class Gstr2bInvoiceListItemDto
    {
        public Guid Id { get; set; }
        public string SupplierGstin { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public string? InvoiceType { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal TotalItc { get; set; }
        public string MatchStatus { get; set; } = string.Empty;
        public int? MatchConfidence { get; set; }
        public string? ActionStatus { get; set; }
    }

    // ==================== Reconciliation DTOs ====================

    /// <summary>
    /// Request to run reconciliation
    /// </summary>
    public class RunReconciliationRequestDto
    {
        [Required]
        public Guid ImportId { get; set; }

        /// <summary>
        /// Force re-run even if already reconciled
        /// </summary>
        public bool Force { get; set; }
    }

    /// <summary>
    /// Reconciliation summary
    /// </summary>
    public class Gstr2bReconciliationSummaryDto
    {
        public string ReturnPeriod { get; set; } = string.Empty;

        public int TotalInvoices { get; set; }
        public int MatchedInvoices { get; set; }
        public int PartialMatchInvoices { get; set; }
        public int UnmatchedInvoices { get; set; }
        public int AcceptedInvoices { get; set; }
        public int RejectedInvoices { get; set; }
        public int PendingReviewInvoices { get; set; }

        public decimal MatchPercentage { get; set; }

        public decimal TotalTaxableValue { get; set; }
        public decimal MatchedTaxableValue { get; set; }
        public decimal UnmatchedTaxableValue { get; set; }

        public decimal TotalItcAvailable { get; set; }
        public decimal MatchedItc { get; set; }
        public decimal UnmatchedItc { get; set; }
    }

    /// <summary>
    /// Supplier-wise summary
    /// </summary>
    public class Gstr2bSupplierSummaryDto
    {
        public string SupplierGstin { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public int InvoiceCount { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public decimal TotalTaxableValue { get; set; }
        public decimal TotalItc { get; set; }
        public decimal MatchPercentage => InvoiceCount > 0 ? Math.Round((decimal)MatchedCount / InvoiceCount * 100, 2) : 0;
    }

    /// <summary>
    /// ITC comparison summary
    /// </summary>
    public class Gstr2bItcComparisonDto
    {
        public string ReturnPeriod { get; set; } = string.Empty;

        // As per GSTR-2B
        public Gstr2bItcBreakdownDto Gstr2b { get; set; } = new();

        // As per Books (Vendor Invoices)
        public Gstr2bItcBreakdownDto Books { get; set; } = new();

        // Difference
        public Gstr2bItcBreakdownDto Difference { get; set; } = new();
    }

    public class Gstr2bItcBreakdownDto
    {
        public decimal Igst { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Cess { get; set; }
        public decimal Total { get; set; }
    }

    // ==================== Action DTOs ====================

    /// <summary>
    /// Accept mismatch request
    /// </summary>
    public class AcceptMismatchRequestDto
    {
        [Required]
        public Guid InvoiceId { get; set; }

        public string? Notes { get; set; }
    }

    /// <summary>
    /// Reject invoice request
    /// </summary>
    public class RejectInvoiceRequestDto
    {
        [Required]
        public Guid InvoiceId { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Manual match request
    /// </summary>
    public class ManualMatchRequestDto
    {
        [Required]
        public Guid Gstr2bInvoiceId { get; set; }

        [Required]
        public Guid VendorInvoiceId { get; set; }

        public string? Notes { get; set; }
    }

    // ==================== GSTR-2B JSON Structure DTOs ====================
    // These match the official GSTR-2B JSON format from GSTN

    /// <summary>
    /// Root structure of GSTR-2B JSON
    /// </summary>
    public class Gstr2bJsonRoot
    {
        public string? Gstin { get; set; }
        public string? RetPeriod { get; set; }  // Format: MMYYYY
        public Gstr2bDocbData? Docdata { get; set; }
    }

    public class Gstr2bDocbData
    {
        public List<Gstr2bB2bRecord>? B2b { get; set; }
        public List<Gstr2bB2baRecord>? B2ba { get; set; }
        public List<Gstr2bCdnRecord>? Cdnr { get; set; }
        public List<Gstr2bCdnaRecord>? Cdnra { get; set; }
        public List<Gstr2bIsdRecord>? Isd { get; set; }
        public List<Gstr2bImpgRecord>? Impg { get; set; }
    }

    public class Gstr2bB2bRecord
    {
        public string? Ctin { get; set; }  // Supplier GSTIN
        public string? Trdnm { get; set; }  // Trade name
        public string? Supfildt { get; set; }  // Supplier filing date
        public List<Gstr2bInvoiceRecord>? Inv { get; set; }
    }

    public class Gstr2bB2baRecord
    {
        public string? Ctin { get; set; }
        public string? Trdnm { get; set; }
        public List<Gstr2bAmendedInvoiceRecord>? Inv { get; set; }
    }

    public class Gstr2bCdnRecord
    {
        public string? Ctin { get; set; }
        public string? Trdnm { get; set; }
        public List<Gstr2bNoteRecord>? Nt { get; set; }
    }

    public class Gstr2bCdnaRecord
    {
        public string? Ctin { get; set; }
        public string? Trdnm { get; set; }
        public List<Gstr2bAmendedNoteRecord>? Nt { get; set; }
    }

    public class Gstr2bIsdRecord
    {
        public string? Ctin { get; set; }
        public string? Trdnm { get; set; }
        public List<Gstr2bIsdDocRecord>? Doclist { get; set; }
    }

    public class Gstr2bImpgRecord
    {
        public string? Refdt { get; set; }
        public string? Portcd { get; set; }
        public string? Benum { get; set; }  // Bill of Entry number
        public string? Bedt { get; set; }  // Bill of Entry date
        public decimal? Txval { get; set; }
        public decimal? Igst { get; set; }
        public decimal? Cess { get; set; }
    }

    public class Gstr2bInvoiceRecord
    {
        public string? Inum { get; set; }  // Invoice number
        public string? Dt { get; set; }  // Invoice date (DD-MM-YYYY)
        public decimal? Val { get; set; }  // Invoice value
        public string? Pos { get; set; }  // Place of supply
        public string? Rev { get; set; }  // Reverse charge (Y/N)
        public string? Itcavl { get; set; }  // ITC available (Y/N)
        public string? Rsn { get; set; }  // Reason if ITC not available
        public string? Diffprcnt { get; set; }  // Differential percentage
        public List<Gstr2bItemRecord>? Items { get; set; }
    }

    public class Gstr2bAmendedInvoiceRecord : Gstr2bInvoiceRecord
    {
        public string? Oinum { get; set; }  // Original invoice number
        public string? Oidt { get; set; }  // Original invoice date
    }

    public class Gstr2bNoteRecord
    {
        public string? Ntnum { get; set; }  // Note number
        public string? Dt { get; set; }
        public string? Typ { get; set; }  // C=Credit, D=Debit
        public decimal? Val { get; set; }
        public string? Pos { get; set; }
        public string? Rev { get; set; }
        public string? Itcavl { get; set; }
        public string? Rsn { get; set; }
        public string? Diffprcnt { get; set; }
        public List<Gstr2bItemRecord>? Items { get; set; }
    }

    public class Gstr2bAmendedNoteRecord : Gstr2bNoteRecord
    {
        public string? Ontnum { get; set; }  // Original note number
        public string? Ontdt { get; set; }  // Original note date
    }

    public class Gstr2bIsdDocRecord
    {
        public string? Doctyp { get; set; }
        public string? Docnum { get; set; }
        public string? Docdt { get; set; }
        public decimal? Igst { get; set; }
        public decimal? Cgst { get; set; }
        public decimal? Sgst { get; set; }
        public decimal? Cess { get; set; }
        public string? Itcelg { get; set; }
    }

    public class Gstr2bItemRecord
    {
        public int? Num { get; set; }  // Item number
        public decimal? Rt { get; set; }  // Rate
        public decimal? Txval { get; set; }  // Taxable value
        public decimal? Igst { get; set; }
        public decimal? Cgst { get; set; }
        public decimal? Sgst { get; set; }
        public decimal? Cess { get; set; }
    }
}

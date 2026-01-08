namespace Core.Entities
{
    /// <summary>
    /// Vendor Invoice entity - represents a purchase bill/invoice from a vendor
    /// For Accounts Payable tracking (Purchase voucher in Tally)
    /// Uses unified Party model (partyId references parties table with is_vendor=true)
    /// </summary>
    public class VendorInvoice
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Party ID (vendor). References parties table where is_vendor = true
        /// </summary>
        public Guid? PartyId { get; set; }

        // ==================== Invoice Details ====================

        /// <summary>
        /// Vendor's invoice number (as printed on their bill)
        /// </summary>
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Internal reference number for this purchase bill
        /// </summary>
        public string? InternalReference { get; set; }

        /// <summary>
        /// Invoice date on vendor's bill
        /// </summary>
        public DateOnly InvoiceDate { get; set; }

        /// <summary>
        /// Payment due date
        /// </summary>
        public DateOnly DueDate { get; set; }

        /// <summary>
        /// Date when bill was received
        /// </summary>
        public DateOnly? ReceivedDate { get; set; }

        /// <summary>
        /// Status: draft, pending_approval, approved, partially_paid, paid, cancelled
        /// </summary>
        public string? Status { get; set; }

        // ==================== Amounts ====================

        public decimal Subtotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public string? Currency { get; set; }
        public string? Notes { get; set; }
        public string? PoNumber { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ==================== GST Classification ====================

        /// <summary>
        /// Invoice type for ITC eligibility
        /// Values: purchase_b2b, purchase_import, purchase_rcm, purchase_sez
        /// </summary>
        public string? InvoiceType { get; set; } = "purchase_b2b";

        /// <summary>
        /// Supply type: intra_state, inter_state, import
        /// </summary>
        public string? SupplyType { get; set; }

        /// <summary>
        /// Place of supply (state code)
        /// </summary>
        public string? PlaceOfSupply { get; set; }

        /// <summary>
        /// Whether this invoice is subject to Reverse Charge Mechanism
        /// </summary>
        public bool ReverseCharge { get; set; }

        /// <summary>
        /// Whether RCM is applicable (for unregistered vendors, specific services)
        /// </summary>
        public bool RcmApplicable { get; set; }

        // ==================== GST Totals ====================

        public decimal TotalCgst { get; set; }
        public decimal TotalSgst { get; set; }
        public decimal TotalIgst { get; set; }
        public decimal TotalCess { get; set; }

        // ==================== ITC (Input Tax Credit) ====================

        /// <summary>
        /// Whether ITC is eligible for this invoice
        /// May be blocked under Section 17(5)
        /// </summary>
        public bool ItcEligible { get; set; } = true;

        /// <summary>
        /// Amount of ITC claimed/claimable
        /// </summary>
        public decimal ItcClaimedAmount { get; set; }

        /// <summary>
        /// Reason if ITC is ineligible
        /// Values: blocked_17_5, missing_gstin, late_filing, not_reflected_gstr2b
        /// </summary>
        public string? ItcIneligibleReason { get; set; }

        /// <summary>
        /// Whether matched with GSTR-2B
        /// </summary>
        public bool MatchedWithGstr2B { get; set; }

        /// <summary>
        /// GSTR-2B return period where matched
        /// </summary>
        public string? Gstr2BPeriod { get; set; }

        // ==================== TDS (Tax Deducted at Source) ====================

        /// <summary>
        /// Whether TDS is applicable on this invoice
        /// </summary>
        public bool TdsApplicable { get; set; }

        /// <summary>
        /// TDS Section: 194C, 194J, 194H, 194I, 194A, 194Q
        /// </summary>
        public string? TdsSection { get; set; }

        /// <summary>
        /// TDS Rate
        /// </summary>
        public decimal? TdsRate { get; set; }

        /// <summary>
        /// TDS Amount deducted
        /// </summary>
        public decimal? TdsAmount { get; set; }

        // ==================== Import-specific fields ====================

        /// <summary>
        /// Bill of Entry number for imports
        /// </summary>
        public string? BillOfEntryNumber { get; set; }

        /// <summary>
        /// Bill of Entry date
        /// </summary>
        public DateOnly? BillOfEntryDate { get; set; }

        /// <summary>
        /// Port code for customs
        /// </summary>
        public string? PortCode { get; set; }

        /// <summary>
        /// Foreign currency amount (before conversion)
        /// </summary>
        public decimal? ForeignCurrencyAmount { get; set; }

        /// <summary>
        /// Foreign currency code
        /// </summary>
        public string? ForeignCurrency { get; set; }

        /// <summary>
        /// Exchange rate at invoice date
        /// </summary>
        public decimal? ExchangeRate { get; set; }

        // ==================== Ledger Posting ====================

        /// <summary>
        /// Whether posted to general ledger
        /// </summary>
        public bool IsPosted { get; set; }

        /// <summary>
        /// Journal entry ID after posting
        /// </summary>
        public Guid? PostedJournalId { get; set; }

        /// <summary>
        /// When posted to ledger
        /// </summary>
        public DateTime? PostedAt { get; set; }

        /// <summary>
        /// Default expense account for this invoice
        /// </summary>
        public Guid? ExpenseAccountId { get; set; }

        // ==================== Approval Workflow ====================

        /// <summary>
        /// Who approved this invoice
        /// </summary>
        public Guid? ApprovedBy { get; set; }

        /// <summary>
        /// When approved
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Approval notes
        /// </summary>
        public string? ApprovalNotes { get; set; }

        // ==================== Tally Migration Fields ====================

        /// <summary>
        /// Original Tally Voucher GUID for migration tracking
        /// </summary>
        public string? TallyVoucherGuid { get; set; }

        /// <summary>
        /// Original Tally Voucher Number
        /// </summary>
        public string? TallyVoucherNumber { get; set; }

        /// <summary>
        /// Migration batch ID
        /// </summary>
        public Guid? TallyMigrationBatchId { get; set; }

        // ==================== Navigation Properties ====================

        /// <summary>
        /// Vendor party (is_vendor = true)
        /// </summary>
        public Party? Party { get; set; }
        public Companies? Company { get; set; }
        public ICollection<VendorInvoiceItem>? Items { get; set; }
    }
}

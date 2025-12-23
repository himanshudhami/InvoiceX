namespace Core.Entities.Gst
{
    /// <summary>
    /// Tracks Reverse Charge Mechanism (RCM) transactions per GST Act Section 9(3) and 9(4).
    /// RCM applies to: Legal services, Security services, GTA, Import of services, etc.
    /// Two-stage journal model: 1) Liability recognition 2) RCM payment + ITC claim
    /// Reference: Notification 13/2017 - Central Tax (Rate)
    /// </summary>
    public class RcmTransaction
    {
        public Guid Id { get; set; }

        // ==================== Company Linking ====================

        public Guid CompanyId { get; set; }

        // ==================== Financial Period ====================

        /// <summary>
        /// Indian financial year in '2024-25' format
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// GST return period in 'Jan-2025' format
        /// </summary>
        public string ReturnPeriod { get; set; } = string.Empty;

        // ==================== Source Document ====================

        /// <summary>
        /// Source type: expense_claim, vendor_invoice, manual
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        public Guid? SourceId { get; set; }
        public string? SourceNumber { get; set; }

        // ==================== Vendor Details ====================

        public string VendorName { get; set; } = string.Empty;

        /// <summary>
        /// GSTIN of vendor (null for unregistered persons)
        /// </summary>
        public string? VendorGstin { get; set; }

        public string? VendorPan { get; set; }
        public string? VendorStateCode { get; set; }
        public string? VendorInvoiceNumber { get; set; }
        public DateTime? VendorInvoiceDate { get; set; }

        // ==================== RCM Category ====================

        public Guid? RcmCategoryId { get; set; }

        /// <summary>
        /// Category code: LEGAL, SECURITY, GTA, IMPORT_SERVICE, etc.
        /// </summary>
        public string RcmCategoryCode { get; set; } = string.Empty;

        /// <summary>
        /// Notification reference: 'Notification 13/2017 Sr. 2'
        /// </summary>
        public string? RcmNotification { get; set; }

        // ==================== Supply Details ====================

        /// <summary>
        /// Place of supply state code
        /// </summary>
        public string PlaceOfSupply { get; set; } = string.Empty;

        /// <summary>
        /// Supply type: intra_state, inter_state, import
        /// </summary>
        public string SupplyType { get; set; } = "intra_state";

        public string? HsnSacCode { get; set; }
        public string? Description { get; set; }

        // ==================== Amounts ====================

        public decimal TaxableValue { get; set; }
        public decimal CgstRate { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstRate { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstRate { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessRate { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalRcmTax { get; set; }

        // ==================== Stage 1: RCM Liability Recognition ====================

        public bool LiabilityRecognized { get; set; }
        public DateTime? LiabilityRecognizedAt { get; set; }
        public Guid? LiabilityJournalId { get; set; }

        // ==================== Stage 2: RCM Payment ====================

        public bool RcmPaid { get; set; }
        public DateTime? RcmPaymentDate { get; set; }
        public Guid? RcmPaymentJournalId { get; set; }
        public string? RcmPaymentReference { get; set; }

        // ==================== ITC Claim (After RCM Payment) ====================

        /// <summary>
        /// Whether ITC is eligible (may be blocked per Section 17(5))
        /// </summary>
        public bool ItcEligible { get; set; } = true;

        public bool ItcClaimed { get; set; }
        public DateTime? ItcClaimDate { get; set; }
        public Guid? ItcClaimJournalId { get; set; }
        public string? ItcClaimPeriod { get; set; }

        // ==================== ITC Blocked ====================

        public bool ItcBlocked { get; set; }
        public string? ItcBlockedReason { get; set; }

        // ==================== GSTR-3B Integration ====================

        public string? Gstr3bPeriod { get; set; }
        public string? Gstr3bTable { get; set; } // '3.1(d)' for RCM
        public bool Gstr3bFiled { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Status: pending, liability_created, rcm_paid, itc_claimed, itc_blocked, cancelled
        /// </summary>
        public string Status { get; set; } = "pending";

        // ==================== Notes ====================

        public string? Notes { get; set; }

        // ==================== Timestamps ====================

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
        public Ledger.JournalEntry? LiabilityJournal { get; set; }
        public Ledger.JournalEntry? RcmPaymentJournal { get; set; }
        public Ledger.JournalEntry? ItcClaimJournal { get; set; }
    }

    /// <summary>
    /// RCM category codes per Notification 13/2017
    /// </summary>
    public static class RcmCategoryCodes
    {
        public const string Legal = "LEGAL";
        public const string Security = "SECURITY";
        public const string Gta = "GTA";
        public const string ImportService = "IMPORT_SERVICE";
        public const string RecoveryAgent = "RECOVERY_AGENT";
        public const string Director = "DIRECTOR";
        public const string InsuranceAgent = "INSURANCE_AGENT";
        public const string Author = "AUTHOR";
        public const string Sponsorship = "SPONSORSHIP";
        public const string GovernmentRent = "GOVT_RENT";
        public const string ManpowerSupply = "MANPOWER";
        public const string UnregisteredPerson = "UNREGISTERED";
    }

    /// <summary>
    /// RCM transaction status
    /// </summary>
    public static class RcmTransactionStatus
    {
        public const string Pending = "pending";
        public const string LiabilityCreated = "liability_created";
        public const string RcmPaid = "rcm_paid";
        public const string ItcClaimed = "itc_claimed";
        public const string ItcBlocked = "itc_blocked";
        public const string Cancelled = "cancelled";
    }
}

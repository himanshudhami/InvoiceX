namespace Core.Entities.Gst
{
    /// <summary>
    /// GSTR-2B Invoice entity - individual invoice records from GSTR-2B
    /// </summary>
    public class Gstr2bInvoice
    {
        public Guid Id { get; set; }
        public Guid ImportId { get; set; }
        public Guid CompanyId { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;

        // Supplier details
        public string SupplierGstin { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public string? SupplierTradeName { get; set; }

        // Invoice details
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public string? InvoiceType { get; set; }  // B2B, CDNR, ISD, IMPG
        public string? DocumentType { get; set; }  // Invoice, Credit Note, Debit Note

        // Amounts
        public decimal TaxableValue { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalInvoiceValue { get; set; }

        // Computed
        public decimal TotalGst => IgstAmount + CgstAmount + SgstAmount + CessAmount;

        // ITC eligibility
        public bool ItcEligible { get; set; } = true;
        public decimal ItcIgst { get; set; }
        public decimal ItcCgst { get; set; }
        public decimal ItcSgst { get; set; }
        public decimal ItcCess { get; set; }

        // Place of supply
        public string? PlaceOfSupply { get; set; }
        public string? SupplyType { get; set; }  // intra_state, inter_state
        public bool ReverseCharge { get; set; }

        // Matching
        public string MatchStatus { get; set; } = Gstr2bMatchStatus.Pending;
        public Guid? MatchedVendorInvoiceId { get; set; }
        public int? MatchConfidence { get; set; }  // 0-100
        public string? MatchDetails { get; set; }  // JSON
        public string? MatchDiscrepancies { get; set; }  // JSON

        // User actions
        public string? ActionStatus { get; set; }  // accepted, rejected, pending_review
        public Guid? ActionBy { get; set; }
        public DateTime? ActionAt { get; set; }
        public string? ActionNotes { get; set; }

        // Amendment tracking
        public string? OriginalInvoiceNumber { get; set; }
        public DateOnly? OriginalInvoiceDate { get; set; }
        public string? AmendmentType { get; set; }  // original, amended

        // Raw data
        public string? RawJson { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public Gstr2bImport? Import { get; set; }
    }

    /// <summary>
    /// Match status types
    /// </summary>
    public static class Gstr2bMatchStatus
    {
        public const string Pending = "pending";
        public const string Matched = "matched";
        public const string PartialMatch = "partial_match";
        public const string Unmatched = "unmatched";
        public const string Accepted = "accepted";
        public const string Rejected = "rejected";
    }

    /// <summary>
    /// Action status types
    /// </summary>
    public static class Gstr2bActionStatus
    {
        public const string Accepted = "accepted";
        public const string Rejected = "rejected";
        public const string PendingReview = "pending_review";
    }

    /// <summary>
    /// Invoice types in GSTR-2B
    /// </summary>
    public static class Gstr2bInvoiceTypes
    {
        public const string B2B = "B2B";           // Business to Business
        public const string B2BA = "B2BA";         // B2B Amended
        public const string CDNR = "CDNR";         // Credit/Debit Notes - Registered
        public const string CDNRA = "CDNRA";       // CDNR Amended
        public const string ISD = "ISD";           // Input Service Distributor
        public const string ISDA = "ISDA";         // ISD Amended
        public const string IMPG = "IMPG";         // Import of Goods
        public const string IMPGSEZ = "IMPGSEZ";   // Import from SEZ
    }

    /// <summary>
    /// Reconciliation rule entity
    /// </summary>
    public class Gstr2bReconciliationRule
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }  // NULL = global rule

        public string RuleName { get; set; } = string.Empty;
        public string RuleCode { get; set; } = string.Empty;
        public int Priority { get; set; } = 100;
        public bool IsActive { get; set; } = true;

        // Match criteria
        public bool MatchGstin { get; set; } = true;
        public bool MatchInvoiceNumber { get; set; } = true;
        public bool MatchInvoiceDate { get; set; }
        public bool MatchAmount { get; set; } = true;

        // Tolerances
        public int InvoiceNumberFuzzyThreshold { get; set; } = 2;
        public int DateToleranceDays { get; set; } = 3;
        public decimal AmountTolerancePercentage { get; set; } = 1.00m;
        public decimal AmountToleranceAbsolute { get; set; } = 100.00m;

        // Confidence
        public int ConfidenceScore { get; set; }

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Match result from reconciliation engine
    /// </summary>
    public class Gstr2bMatchResult
    {
        public bool IsMatch { get; set; }
        public int ConfidenceScore { get; set; }
        public string MatchedRuleCode { get; set; } = string.Empty;
        public Guid? MatchedVendorInvoiceId { get; set; }
        public string? MatchedInvoiceNumber { get; set; }
        public List<string> Discrepancies { get; set; } = new();
        public Dictionary<string, object> Details { get; set; } = new();
    }
}

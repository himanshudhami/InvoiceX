namespace Core.Entities.Ledger
{
    /// <summary>
    /// Represents GST Input Tax Credit (ITC) for tracking and GSTR return filing.
    /// Per GST Act 2017 Sections 16-21, ITC can be claimed on business purchases.
    /// </summary>
    public class GstInputCredit
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty; // "2024-25"
        public string ReturnPeriod { get; set; } = string.Empty; // "Jan-2025"

        // Source document
        public string SourceType { get; set; } = string.Empty; // expense_claim, subscription, contractor_payment
        public Guid SourceId { get; set; }
        public string? SourceNumber { get; set; }

        // Vendor details (for GSTR-2B matching)
        public string? VendorGstin { get; set; }
        public string? VendorName { get; set; }
        public string? VendorInvoiceNumber { get; set; }
        public DateTime? VendorInvoiceDate { get; set; }

        // GST details
        public string? PlaceOfSupply { get; set; }
        public string SupplyType { get; set; } = "intra_state";
        public string? HsnSacCode { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal CgstRate { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstRate { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstRate { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessRate { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalGst { get; set; }

        // ITC eligibility
        public bool ItcEligible { get; set; } = true;
        public string? IneligibleReason { get; set; } // Blocked credit reason

        // GSTR-2B matching status
        public bool MatchedWithGstr2B { get; set; }
        public DateTime? Gstr2BMatchDate { get; set; }
        public string? Gstr2BMismatchReason { get; set; }

        // Claim status
        public string Status { get; set; } = "pending"; // pending, claimed, reversed, rejected
        public bool ClaimedInGstr3B { get; set; }
        public string? Gstr3BFilingPeriod { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public string? ClaimedBy { get; set; }

        // Reversal (Rule 42/43)
        public bool IsReversed { get; set; }
        public decimal? ReversalAmount { get; set; }
        public string? ReversalReason { get; set; }
        public DateTime? ReversalDate { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// GST Input Credit status constants.
    /// </summary>
    public static class GstInputCreditStatus
    {
        public const string Pending = "pending";
        public const string Claimed = "claimed";
        public const string Reversed = "reversed";
        public const string Rejected = "rejected";

        public static readonly string[] All = new[]
        {
            Pending, Claimed, Reversed, Rejected
        };

        public static bool IsValid(string status) => All.Contains(status);
    }
}

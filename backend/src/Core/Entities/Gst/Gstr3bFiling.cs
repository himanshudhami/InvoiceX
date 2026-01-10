namespace Core.Entities.Gst
{
    /// <summary>
    /// GSTR-3B filing record for a return period.
    /// Contains consolidated data from GSTR-1 outward supplies, vendor invoices ITC, and RCM.
    /// Reference: Form GSTR-3B (Monthly summary return)
    /// </summary>
    public class Gstr3bFiling
    {
        public Guid Id { get; set; }

        // ==================== Company & Period ====================

        public Guid CompanyId { get; set; }

        /// <summary>
        /// Company GSTIN for this filing
        /// </summary>
        public string Gstin { get; set; } = string.Empty;

        /// <summary>
        /// Return period in 'Jan-2025' format
        /// </summary>
        public string ReturnPeriod { get; set; } = string.Empty;

        /// <summary>
        /// Indian financial year in '2024-25' format
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        // ==================== Filing Status ====================

        /// <summary>
        /// Status: draft, generated, reviewed, filed, amended
        /// </summary>
        public string Status { get; set; } = Gstr3bFilingStatus.Draft;

        public DateTime? GeneratedAt { get; set; }
        public Guid? GeneratedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedBy { get; set; }

        public DateTime? FiledAt { get; set; }
        public Guid? FiledBy { get; set; }

        // ==================== GSTN Filing Details ====================

        /// <summary>
        /// Acknowledgment Reference Number from GSTN
        /// </summary>
        public string? Arn { get; set; }

        /// <summary>
        /// Actual filing date on GSTN portal
        /// </summary>
        public DateTime? FilingDate { get; set; }

        // ==================== Table Summaries (JSONB) ====================

        /// <summary>
        /// Table 3.1: Outward supplies (taxable, zero-rated, nil, RCM, non-GST)
        /// </summary>
        public string? Table31Json { get; set; }

        /// <summary>
        /// Table 3.2: Interstate supplies to unregistered persons
        /// </summary>
        public string? Table32Json { get; set; }

        /// <summary>
        /// Table 4: ITC Available, Reversed, Ineligible
        /// </summary>
        public string? Table4Json { get; set; }

        /// <summary>
        /// Table 5: Exempt, nil-rated, non-GST inward supplies
        /// </summary>
        public string? Table5Json { get; set; }

        /// <summary>
        /// Table 6.1: Tax payment summary
        /// </summary>
        public string? Table61Json { get; set; }

        // ==================== Variance Tracking ====================

        /// <summary>
        /// Variance from previous period for review
        /// </summary>
        public string? PreviousPeriodVarianceJson { get; set; }

        // ==================== Notes ====================

        public string? Notes { get; set; }

        // ==================== Timestamps ====================

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
        public ICollection<Gstr3bLineItem> LineItems { get; set; } = new List<Gstr3bLineItem>();
    }

    /// <summary>
    /// GSTR-3B filing status constants
    /// </summary>
    public static class Gstr3bFilingStatus
    {
        public const string Draft = "draft";
        public const string Generated = "generated";
        public const string Reviewed = "reviewed";
        public const string Filed = "filed";
        public const string Amended = "amended";
    }

    /// <summary>
    /// GSTR-3B table codes for line items
    /// </summary>
    public static class Gstr3bTableCodes
    {
        // Table 3.1 - Outward Supplies
        public const string Table31a = "3.1(a)";  // Outward taxable supplies (other than zero/nil/exempt)
        public const string Table31b = "3.1(b)";  // Outward taxable supplies (zero rated)
        public const string Table31c = "3.1(c)";  // Other outward supplies (nil rated, exempted)
        public const string Table31d = "3.1(d)";  // Inward supplies (liable to reverse charge)
        public const string Table31e = "3.1(e)";  // Non-GST outward supplies

        // Table 3.2 - Interstate supplies to unregistered
        public const string Table32Unreg = "3.2(unreg)";
        public const string Table32Comp = "3.2(comp)";
        public const string Table32Uin = "3.2(uin)";

        // Table 4 - ITC
        public const string Table4A1 = "4(A)(1)";   // Import of goods
        public const string Table4A2 = "4(A)(2)";   // Import of services
        public const string Table4A3 = "4(A)(3)";   // Inward supplies liable to RCM
        public const string Table4A4 = "4(A)(4)";   // Inward supplies from ISD
        public const string Table4A5 = "4(A)(5)";   // All other ITC
        public const string Table4B1 = "4(B)(1)";   // ITC reversed as per rules 42 & 43
        public const string Table4B2 = "4(B)(2)";   // ITC reversed - others
        public const string Table4C = "4(C)";       // Net ITC available
        public const string Table4D1 = "4(D)(1)";   // Ineligible ITC - as per section 17(5)
        public const string Table4D2 = "4(D)(2)";   // Ineligible ITC - others

        // Table 5 - Exempt/nil-rated
        public const string Table5a = "5(a)";   // From registered supplier
        public const string Table5b = "5(b)";   // From unregistered supplier
        public const string Table5c = "5(c)";   // Non-GST supply
    }
}

namespace Core.Entities.Tax
{
    /// <summary>
    /// Tax Collected at Source (TCS) per Section 206C of Income Tax Act.
    /// TCS is collected by seller from buyer at the time of sale.
    /// Common scenarios: Sale of goods > 50L, scrap, motor vehicles, forest produce
    /// </summary>
    public class TcsTransaction
    {
        public Guid Id { get; set; }

        // ==================== Company Linking ====================

        public Guid CompanyId { get; set; }

        // ==================== Transaction Type ====================

        /// <summary>
        /// Transaction type: 'collected' (we sold and collected TCS) or 'paid' (we bought and TCS was collected from us)
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        // ==================== TCS Section ====================

        /// <summary>
        /// TCS section code: 206C(1H), 206C(1), 206C(1F), etc.
        /// </summary>
        public string SectionCode { get; set; } = string.Empty;

        public Guid? SectionId { get; set; }

        // ==================== Financial Period ====================

        public DateOnly TransactionDate { get; set; }

        /// <summary>
        /// Indian financial year in '2024-25' format
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Quarter: Q1, Q2, Q3, Q4
        /// </summary>
        public string Quarter { get; set; } = string.Empty;

        // ==================== Party Details (Collectee/Collector) ====================

        /// <summary>
        /// Party type: customer (when we collect), vendor (when we pay)
        /// </summary>
        public string PartyType { get; set; } = string.Empty;

        public Guid? PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string? PartyPan { get; set; }
        public string? PartyGstin { get; set; }

        // ==================== Amounts ====================

        /// <summary>
        /// Total transaction value (sale/purchase amount)
        /// </summary>
        public decimal TransactionValue { get; set; }

        /// <summary>
        /// TCS rate applied (percentage)
        /// </summary>
        public decimal TcsRate { get; set; }

        /// <summary>
        /// TCS amount collected/paid
        /// </summary>
        public decimal TcsAmount { get; set; }

        // ==================== Cumulative Tracking (for 50L threshold) ====================

        /// <summary>
        /// Total value from this party in the financial year
        /// </summary>
        public decimal? CumulativeValueFy { get; set; }

        /// <summary>
        /// Applicable threshold for this section
        /// </summary>
        public decimal? ThresholdAmount { get; set; }

        // ==================== Linked Documents ====================

        public Guid? InvoiceId { get; set; }
        public Guid? PaymentId { get; set; }
        public Guid? JournalEntryId { get; set; }

        // ==================== Status and Remittance ====================

        /// <summary>
        /// Status: pending, collected, remitted, filed, cancelled
        /// </summary>
        public string Status { get; set; } = "pending";

        public DateTime? CollectedAt { get; set; }
        public DateTime? RemittedAt { get; set; }
        public string? ChallanNumber { get; set; }
        public string? BsrCode { get; set; }

        // ==================== Form 27EQ Tracking ====================

        public string? Form27EqQuarter { get; set; }
        public bool Form27EqFiled { get; set; }
        public string? Form27EqAcknowledgement { get; set; }

        // ==================== Notes ====================

        public string? Notes { get; set; }

        // ==================== Timestamps ====================

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
        public Invoices? Invoice { get; set; }
        public Payments? Payment { get; set; }
        public Ledger.JournalEntry? JournalEntry { get; set; }
    }

    /// <summary>
    /// TCS Section codes per Section 206C
    /// </summary>
    public static class TcsSectionCodes
    {
        /// <summary>
        /// Sale of goods > 50L - 0.1%
        /// </summary>
        public const string SaleOver50L = "206C(1H)";

        /// <summary>
        /// Sale of scrap - 1%
        /// </summary>
        public const string Scrap = "206C(1)";

        /// <summary>
        /// Sale of motor vehicle > 10L - 1%
        /// </summary>
        public const string MotorVehicle = "206C(1F)";

        /// <summary>
        /// Foreign remittance (LRS) - 5%/20%
        /// </summary>
        public const string ForeignRemittance = "206C(1G)_REMIT";

        /// <summary>
        /// Overseas tour package - 5%/20%
        /// </summary>
        public const string OverseasTour = "206C(1G)_TOUR";

        /// <summary>
        /// Sale of liquor - 1%
        /// </summary>
        public const string Liquor = "206C(1)(i)";

        /// <summary>
        /// Sale of forest produce - 2.5%
        /// </summary>
        public const string ForestProduce = "206C(1)(ii)";
    }

    /// <summary>
    /// TCS transaction status
    /// </summary>
    public static class TcsTransactionStatus
    {
        public const string Pending = "pending";
        public const string Collected = "collected";
        public const string Remitted = "remitted";
        public const string Filed = "filed";
        public const string Cancelled = "cancelled";
    }
}

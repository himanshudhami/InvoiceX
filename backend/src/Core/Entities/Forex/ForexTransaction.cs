namespace Core.Entities.Forex
{
    /// <summary>
    /// Tracks foreign currency transactions for Ind AS 21 compliance
    /// Records bookings (invoice), settlements (payment), and revaluations
    /// </summary>
    public class ForexTransaction
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // Transaction reference
        public DateOnly TransactionDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;

        // Source document
        public string SourceType { get; set; } = string.Empty;  // invoice, payment, revaluation
        public Guid? SourceId { get; set; }
        public string? SourceNumber { get; set; }

        // Currency details
        public string Currency { get; set; } = string.Empty;    // USD, EUR, GBP
        public decimal ForeignAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal InrAmount { get; set; }

        // Transaction type
        public string TransactionType { get; set; } = string.Empty;  // booking, settlement, revaluation

        // Forex gain/loss
        public decimal? ForexGainLoss { get; set; }
        public string? GainLossType { get; set; }  // realized, unrealized

        // Related transaction (settlement links to booking)
        public Guid? RelatedForexId { get; set; }

        // Ledger posting
        public Guid? JournalEntryId { get; set; }
        public bool IsPosted { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        // Navigation
        public Companies? Company { get; set; }
        public ForexTransaction? RelatedForex { get; set; }
        public Ledger.JournalEntry? JournalEntry { get; set; }
    }
}

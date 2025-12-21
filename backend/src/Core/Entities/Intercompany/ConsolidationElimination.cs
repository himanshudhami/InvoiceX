namespace Core.Entities.Intercompany
{
    /// <summary>
    /// Tracks elimination entries for consolidated financial statements
    /// </summary>
    public class ConsolidationElimination
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Month-end date for consolidation
        /// </summary>
        public DateOnly ConsolidationPeriod { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public Guid ParentCompanyId { get; set; }

        /// <summary>
        /// Type: 'intercompany_revenue', 'intercompany_receivable', 'investment', 'dividend', 'unrealized_profit'
        /// </summary>
        public string EliminationType { get; set; } = string.Empty;

        public Guid? FromCompanyId { get; set; }
        public Guid? ToCompanyId { get; set; }

        public decimal EliminationAmount { get; set; }
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Array of related intercompany transaction IDs
        /// </summary>
        public Guid[]? SourceTransactionIds { get; set; }

        public Guid? JournalEntryId { get; set; }

        /// <summary>
        /// Status: 'pending', 'posted', 'reversed'
        /// </summary>
        public string Status { get; set; } = "pending";
        public DateTime? PostedAt { get; set; }
        public Guid? PostedBy { get; set; }

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
    }
}

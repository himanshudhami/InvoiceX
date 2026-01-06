namespace Core.Entities.Tags
{
    /// <summary>
    /// Associates tags with any transaction type (polymorphic tagging)
    /// Supports split allocations for cost distribution
    /// </summary>
    public class TransactionTag
    {
        public Guid Id { get; set; }

        // ==================== Transaction Reference ====================

        /// <summary>
        /// ID of the tagged transaction (invoice, payment, expense, journal entry, etc.)
        /// </summary>
        public Guid TransactionId { get; set; }

        /// <summary>
        /// Type of transaction being tagged
        /// Values: 'invoice', 'vendor_invoice', 'payment', 'vendor_payment',
        ///         'expense_claim', 'journal_entry', 'journal_line', 'bank_transaction',
        ///         'salary_transaction', 'asset', 'subscription'
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Tag being applied
        /// </summary>
        public Guid TagId { get; set; }

        // ==================== Allocation ====================

        /// <summary>
        /// Amount allocated to this tag (for split allocations)
        /// If null, entire transaction amount is allocated
        /// </summary>
        public decimal? AllocatedAmount { get; set; }

        /// <summary>
        /// Percentage allocated to this tag (alternative to amount)
        /// Used when splitting by percentage
        /// </summary>
        public decimal? AllocationPercentage { get; set; }

        /// <summary>
        /// Allocation method used
        /// Values: 'full' (100% to this tag), 'amount' (specific amount),
        ///         'percentage' (specific %), 'split_equal' (equal split with other tags)
        /// </summary>
        public string AllocationMethod { get; set; } = "full";

        // ==================== Source ====================

        /// <summary>
        /// How this tag was applied
        /// Values: 'manual', 'rule', 'ai_suggested', 'imported'
        /// </summary>
        public string Source { get; set; } = "manual";

        /// <summary>
        /// If source = 'rule', the rule ID that applied this tag
        /// </summary>
        public Guid? AttributionRuleId { get; set; }

        /// <summary>
        /// Confidence score for AI suggestions (0-100)
        /// </summary>
        public int? ConfidenceScore { get; set; }

        // ==================== Timestamps ====================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }

        // ==================== Navigation ====================

        public Tag? Tag { get; set; }
        public AttributionRule? AttributionRule { get; set; }
    }
}

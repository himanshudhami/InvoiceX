namespace Core.Entities
{
    /// <summary>
    /// Bank transaction match record - links bank transactions to payments/expenses
    /// Supports split reconciliation where one transaction can match multiple records
    /// </summary>
    public class BankTransactionMatch
    {
        public Guid Id { get; set; }

        // ==================== Linking ====================

        /// <summary>
        /// Company this match belongs to
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Bank transaction being matched
        /// </summary>
        public Guid BankTransactionId { get; set; }

        // ==================== Match Target ====================

        /// <summary>
        /// Type of record being matched: payment, expense, transfer, tax_payment, salary, contractor_payment
        /// </summary>
        public string MatchedType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the matched record (payment ID, expense ID, etc.)
        /// </summary>
        public Guid MatchedId { get; set; }

        /// <summary>
        /// Amount matched from this transaction
        /// </summary>
        public decimal MatchedAmount { get; set; }

        // ==================== Match Metadata ====================

        /// <summary>
        /// When the match was created
        /// </summary>
        public DateTime MatchedAt { get; set; }

        /// <summary>
        /// User who created the match
        /// </summary>
        public string? MatchedBy { get; set; }

        /// <summary>
        /// Method: manual, auto_reference, auto_amount, rule_based
        /// </summary>
        public string MatchMethod { get; set; } = "manual";

        /// <summary>
        /// Confidence score for auto-matches (0-100)
        /// </summary>
        public decimal? ConfidenceScore { get; set; }

        /// <summary>
        /// Additional notes about this match
        /// </summary>
        public string? Notes { get; set; }

        // ==================== Timestamps ====================

        public DateTime CreatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public BankTransaction? BankTransaction { get; set; }
        public Companies? Company { get; set; }
    }
}

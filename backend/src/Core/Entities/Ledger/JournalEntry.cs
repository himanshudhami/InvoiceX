namespace Core.Entities.Ledger
{
    /// <summary>
    /// Journal Entry - represents a balanced double-entry transaction
    /// Immutable once posted (status = 'posted')
    /// </summary>
    public class JournalEntry
    {
        public Guid Id { get; set; }

        // ==================== Company & Reference ====================

        /// <summary>
        /// Company this entry belongs to
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Unique journal number (e.g., "JV/2024-25/0001")
        /// </summary>
        public string JournalNumber { get; set; } = string.Empty;

        /// <summary>
        /// Date of the journal entry
        /// </summary>
        public DateOnly JournalDate { get; set; }

        // ==================== Period ====================

        /// <summary>
        /// Financial year (e.g., "2024-25")
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Month within the FY (1-12, April = 1)
        /// </summary>
        public int PeriodMonth { get; set; }

        // ==================== Entry Type & Source ====================

        /// <summary>
        /// Entry type: manual, auto_post, reversal, opening, adjustment
        /// </summary>
        public string EntryType { get; set; } = "manual";

        /// <summary>
        /// Source transaction type: invoice, payment, payroll_run, expense, etc.
        /// </summary>
        public string? SourceType { get; set; }

        /// <summary>
        /// ID of the source transaction (invoice_id, payment_id, etc.)
        /// </summary>
        public Guid? SourceId { get; set; }

        /// <summary>
        /// Display reference from source (invoice number, payment reference)
        /// </summary>
        public string? SourceNumber { get; set; }

        /// <summary>
        /// Description/narration for the journal entry
        /// </summary>
        public string Description { get; set; } = string.Empty;

        // ==================== Totals ====================

        /// <summary>
        /// Total debit amount (must equal total_credit)
        /// </summary>
        public decimal TotalDebit { get; set; }

        /// <summary>
        /// Total credit amount (must equal total_debit)
        /// </summary>
        public decimal TotalCredit { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Status: draft, posted, reversed
        /// </summary>
        public string Status { get; set; } = "draft";

        /// <summary>
        /// When the entry was posted
        /// </summary>
        public DateTime? PostedAt { get; set; }

        /// <summary>
        /// User who posted the entry
        /// </summary>
        public Guid? PostedBy { get; set; }

        // ==================== Reversal ====================

        /// <summary>
        /// Whether this entry has been reversed
        /// </summary>
        public bool IsReversed { get; set; }

        /// <summary>
        /// If this is a reversal entry, references the original
        /// </summary>
        public Guid? ReversalOfId { get; set; }

        /// <summary>
        /// If reversed, references the reversing entry
        /// </summary>
        public Guid? ReversedById { get; set; }

        // ==================== Posting Rule ====================

        /// <summary>
        /// For auto-posted entries, the rule pack version used
        /// </summary>
        public string? RulePackVersion { get; set; }

        /// <summary>
        /// For auto-posted entries, the rule code that generated this
        /// </summary>
        public string? RuleCode { get; set; }

        // ==================== Audit ====================

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
        public JournalEntry? ReversalOf { get; set; }
        public JournalEntry? ReversedBy { get; set; }
        public ICollection<JournalEntryLine>? Lines { get; set; }
    }
}

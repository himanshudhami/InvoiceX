namespace Core.Entities.Ledger
{
    /// <summary>
    /// Posting Rule - configuration-driven auto-posting templates
    /// Defines how business events (invoices, payments, etc.) generate journal entries
    /// </summary>
    public class PostingRule
    {
        public Guid Id { get; set; }

        // ==================== Company ====================

        /// <summary>
        /// Company this rule belongs to (null for system-wide templates)
        /// </summary>
        public Guid? CompanyId { get; set; }

        // ==================== Rule Identification ====================

        /// <summary>
        /// Unique code for this rule (e.g., "INV_DOM_INTRA_B2B")
        /// </summary>
        public string RuleCode { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name
        /// </summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>
        /// Description of when this rule applies
        /// </summary>
        public string? Description { get; set; }

        // ==================== Trigger Configuration ====================

        /// <summary>
        /// Source transaction type: invoice, payment, payroll_run, expense, etc.
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// Event that triggers this rule: on_create, on_finalize, on_approval, on_payment
        /// </summary>
        public string TriggerEvent { get; set; } = string.Empty;

        /// <summary>
        /// JSON conditions that must match for rule to apply
        /// Example: {"invoice_type": "b2b", "is_intra_state": true}
        /// </summary>
        public string? ConditionsJson { get; set; }

        // ==================== Posting Template ====================

        /// <summary>
        /// JSON template defining the journal entry structure
        /// Contains description_template and lines array with account mappings
        /// </summary>
        public string PostingTemplate { get; set; } = string.Empty;

        // ==================== Versioning ====================

        /// <summary>
        /// Financial year this rule applies to (null = all years)
        /// </summary>
        public string? FinancialYear { get; set; }

        /// <summary>
        /// Date from which this rule is effective
        /// </summary>
        public DateOnly EffectiveFrom { get; set; }

        /// <summary>
        /// Date until which this rule is effective (null = no end)
        /// </summary>
        public DateOnly? EffectiveTo { get; set; }

        // ==================== Priority & Status ====================

        /// <summary>
        /// Priority when multiple rules match (lower = higher priority)
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Whether this rule is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ==================== Audit ====================

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
        public ICollection<PostingRuleUsageLog>? UsageLogs { get; set; }
    }

    /// <summary>
    /// Audit log for posting rule usage
    /// Records each time a rule is used to generate a journal entry
    /// </summary>
    public class PostingRuleUsageLog
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Rule that was applied
        /// </summary>
        public Guid PostingRuleId { get; set; }

        /// <summary>
        /// Generated journal entry
        /// </summary>
        public Guid JournalEntryId { get; set; }

        /// <summary>
        /// Source type (copied from rule)
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// Source transaction ID
        /// </summary>
        public Guid SourceId { get; set; }

        /// <summary>
        /// Snapshot of the rule at time of posting (JSON)
        /// </summary>
        public string? RuleSnapshot { get; set; }

        /// <summary>
        /// When the posting was computed
        /// </summary>
        public DateTime ComputedAt { get; set; }

        /// <summary>
        /// User who triggered the posting
        /// </summary>
        public Guid? ComputedBy { get; set; }

        /// <summary>
        /// Whether the posting was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if posting failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        // ==================== Navigation Properties ====================

        public PostingRule? PostingRule { get; set; }
        public JournalEntry? JournalEntry { get; set; }
    }
}

namespace Core.Entities.Tags
{
    /// <summary>
    /// Rules for automatic tag attribution
    /// When a transaction matches the conditions, tags are automatically applied
    /// </summary>
    public class AttributionRule
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Basic Info ====================

        /// <summary>
        /// Human-readable rule name
        /// e.g., "Tag AWS bills to Engineering", "Split rent by department"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of what this rule does
        /// </summary>
        public string? Description { get; set; }

        // ==================== Rule Conditions ====================

        /// <summary>
        /// Primary rule type for matching
        /// Values: 'vendor', 'customer', 'account', 'product', 'keyword',
        ///         'amount_range', 'employee', 'composite'
        /// </summary>
        public string RuleType { get; set; } = string.Empty;

        /// <summary>
        /// Transaction types this rule applies to
        /// e.g., ['vendor_invoice', 'vendor_payment'] or ['*'] for all
        /// Stored as JSON array
        /// </summary>
        public string AppliesTo { get; set; } = "[\"*\"]";

        /// <summary>
        /// Conditions for matching (JSONB)
        /// Structure depends on rule_type:
        ///
        /// vendor: { "vendor_ids": ["uuid1", "uuid2"], "vendor_name_contains": "AWS" }
        /// customer: { "customer_ids": ["uuid1"], "customer_type": "b2b" }
        /// account: { "account_ids": ["uuid1"], "account_group": "expenses" }
        /// keyword: { "description_contains": ["cloud", "server"], "match_mode": "any" }
        /// amount_range: { "min_amount": 10000, "max_amount": 50000 }
        /// composite: { "and": [...], "or": [...] }
        /// </summary>
        public string Conditions { get; set; } = "{}";

        // ==================== Tag Assignment ====================

        /// <summary>
        /// Tags to apply when rule matches
        /// Stored as JSON array of tag assignments:
        /// [
        ///   { "tag_id": "uuid", "allocation_method": "full" },
        ///   { "tag_id": "uuid", "allocation_method": "percentage", "value": 60 },
        ///   { "tag_id": "uuid", "allocation_method": "percentage", "value": 40 }
        /// ]
        /// </summary>
        public string TagAssignments { get; set; } = "[]";

        /// <summary>
        /// Overall allocation method when multiple tags
        /// Values: 'single' (one tag), 'split_equal', 'split_percentage', 'split_by_metric'
        /// </summary>
        public string AllocationMethod { get; set; } = "single";

        /// <summary>
        /// For 'split_by_metric', which metric to use
        /// Values: 'headcount', 'revenue', 'sqft', 'custom'
        /// </summary>
        public string? SplitMetric { get; set; }

        // ==================== Execution Control ====================

        /// <summary>
        /// Priority order (lower = higher priority)
        /// When multiple rules match, higher priority wins
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Stop processing other rules if this one matches
        /// </summary>
        public bool StopOnMatch { get; set; } = true;

        /// <summary>
        /// Overwrite existing tags or add to them
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;

        // ==================== Scope ====================

        /// <summary>
        /// Only apply to transactions after this date
        /// </summary>
        public DateOnly? EffectiveFrom { get; set; }

        /// <summary>
        /// Stop applying after this date
        /// </summary>
        public DateOnly? EffectiveTo { get; set; }

        // ==================== Statistics ====================

        /// <summary>
        /// Number of times this rule has been applied
        /// </summary>
        public int TimesApplied { get; set; }

        /// <summary>
        /// Last time this rule was applied
        /// </summary>
        public DateTime? LastAppliedAt { get; set; }

        /// <summary>
        /// Total amount processed by this rule
        /// </summary>
        public decimal TotalAmountTagged { get; set; }

        // ==================== Status ====================

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        // ==================== Navigation ====================

        public ICollection<TransactionTag>? AppliedTags { get; set; }
    }
}

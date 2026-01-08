namespace Core.Entities.Tags
{
    /// <summary>
    /// Flexible tagging system - modern replacement for Tally's Cost Centers
    /// Tags can be hierarchical and grouped by type (department, project, client, etc.)
    /// </summary>
    public class Tag
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Basic Info ====================

        /// <summary>
        /// Tag name (e.g., "Engineering", "Project Alpha", "Client ABC")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional tag code for quick reference
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Tag group for categorization
        /// Values: 'department', 'project', 'client', 'region', 'cost_center', 'custom'
        /// </summary>
        public string TagGroup { get; set; } = "custom";

        /// <summary>
        /// Description of what this tag represents
        /// </summary>
        public string? Description { get; set; }

        // ==================== Hierarchy ====================

        /// <summary>
        /// Parent tag ID for hierarchical tags
        /// e.g., "Engineering" -> "Frontend Team" -> "React Squad"
        /// </summary>
        public Guid? ParentTagId { get; set; }

        /// <summary>
        /// Full path for display (auto-computed)
        /// e.g., "Engineering / Frontend Team / React Squad"
        /// </summary>
        public string? FullPath { get; set; }

        /// <summary>
        /// Depth level in hierarchy (0 = root)
        /// </summary>
        public int Level { get; set; }

        // ==================== UI/Display ====================

        /// <summary>
        /// Color for UI display (hex code)
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Icon name for UI (optional)
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Sort order within the same parent
        /// </summary>
        public int SortOrder { get; set; }

        // ==================== Budgeting (Optional) ====================

        /// <summary>
        /// Budget amount for this tag (annual or per-period)
        /// </summary>
        public decimal? BudgetAmount { get; set; }

        /// <summary>
        /// Budget period: 'annual', 'quarterly', 'monthly'
        /// </summary>
        public string? BudgetPeriod { get; set; }

        /// <summary>
        /// Financial year for budget (e.g., "2024-25")
        /// </summary>
        public string? BudgetYear { get; set; }

        // ==================== Tally Migration ====================

        /// <summary>
        /// Original Tally Cost Center GUID (for migration)
        /// </summary>
        public string? TallyCostCenterGuid { get; set; }

        /// <summary>
        /// Original Tally Cost Center Name
        /// </summary>
        public string? TallyCostCenterName { get; set; }

        // ==================== Status ====================

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this is a system-seeded tag (cannot be deleted)
        /// System tags include TDS sections, party types, compliance tags
        /// </summary>
        public bool IsSystem { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        // ==================== Navigation ====================

        public Tag? ParentTag { get; set; }
        public ICollection<Tag>? ChildTags { get; set; }
        public ICollection<TransactionTag>? TransactionTags { get; set; }

        /// <summary>
        /// TDS rules associated with this tag (for tds_section tags)
        /// </summary>
        public ICollection<TdsTagRule>? TdsTagRules { get; set; }
    }
}

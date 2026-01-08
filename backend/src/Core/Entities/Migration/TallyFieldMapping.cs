namespace Core.Entities.Migration
{
    /// <summary>
    /// User-configurable mapping rules for Tally data import.
    /// Maps Tally ledger groups/items to our entities and accounts.
    /// </summary>
    public class TallyFieldMapping
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Mapping Scope ====================

        /// <summary>
        /// Type of mapping.
        /// Values: ledger_group, ledger, stock_group, voucher_type, cost_category
        /// </summary>
        public string MappingType { get; set; } = "ledger_group";

        // ==================== Source (Tally) ====================

        /// <summary>
        /// Tally group name for group-level mappings (e.g., "Sundry Creditors")
        /// </summary>
        public string? TallyGroupName { get; set; }

        /// <summary>
        /// Tally item name for specific item mappings.
        /// NULL means the mapping applies to the whole group.
        /// </summary>
        public string? TallyName { get; set; }

        // ==================== Target (Our System) ====================

        /// <summary>
        /// Target entity type.
        /// Values: vendors, customers, bank_accounts, chart_of_accounts, tags,
        ///         stock_groups, suspense, skip
        /// </summary>
        public string TargetEntity { get; set; } = string.Empty;

        /// <summary>
        /// Specific target account (for ledger-to-COA mappings)
        /// </summary>
        public Guid? TargetAccountId { get; set; }

        /// <summary>
        /// Default account type for new accounts.
        /// Values: asset, liability, income, expense, equity
        /// </summary>
        public string? TargetAccountType { get; set; }

        /// <summary>
        /// More specific account classification
        /// </summary>
        public string? TargetAccountSubtype { get; set; }

        /// <summary>
        /// Default account code for new accounts created from this mapping
        /// </summary>
        public string? DefaultAccountCode { get; set; }

        /// <summary>
        /// Default account name template for new accounts
        /// </summary>
        public string? DefaultAccountName { get; set; }

        /// <summary>
        /// For cost center mappings - target tag group.
        /// Values: department, project, client, region, cost_center, custom
        /// </summary>
        public string? TargetTagGroup { get; set; }

        /// <summary>
        /// Tag names to auto-assign to parties created from this mapping.
        /// JSON array of tag names, e.g., ["TDS:194J-Professional", "Vendor:Consultant"]
        /// Used for auto-tagging vendors/customers during Tally import.
        /// </summary>
        public List<string>? TagAssignments { get; set; }

        // ==================== Priority ====================

        /// <summary>
        /// Priority for overlapping rules (lower = higher priority)
        /// </summary>
        public int Priority { get; set; } = 100;

        // ==================== Status ====================

        /// <summary>
        /// Whether this mapping is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this is a system-provided default mapping
        /// </summary>
        public bool IsSystemDefault { get; set; }

        // ==================== Audit ====================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }

        // ==================== Navigation ====================

        public Companies? Company { get; set; }
        public Ledger.ChartOfAccount? TargetAccount { get; set; }
    }
}

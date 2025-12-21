namespace Core.Entities.Intercompany
{
    /// <summary>
    /// Represents a parent-subsidiary or affiliate relationship between companies
    /// Used for consolidated financial statements
    /// </summary>
    public class CompanyRelationship
    {
        public Guid Id { get; set; }
        public Guid ParentCompanyId { get; set; }
        public Guid ChildCompanyId { get; set; }

        /// <summary>
        /// Type of relationship: 'subsidiary', 'associate', 'joint_venture', 'affiliate'
        /// </summary>
        public string RelationshipType { get; set; } = string.Empty;

        /// <summary>
        /// Ownership percentage (e.g., 100.00 for wholly owned subsidiary)
        /// </summary>
        public decimal? OwnershipPercentage { get; set; }

        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }

        /// <summary>
        /// Consolidation method: 'full', 'proportionate', 'equity_method', 'none'
        /// </summary>
        public string? ConsolidationMethod { get; set; }

        public string FunctionalCurrency { get; set; } = "INR";
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
    }
}

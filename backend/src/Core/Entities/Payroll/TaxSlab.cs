namespace Core.Entities.Payroll
{
    /// <summary>
    /// Income tax slab configuration for Old and New tax regimes
    /// </summary>
    public class TaxSlab
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Tax regime: 'old' or 'new'
        /// </summary>
        public string Regime { get; set; } = "new";

        /// <summary>
        /// Financial year e.g., '2024-25'
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Minimum income for this slab (inclusive)
        /// </summary>
        public decimal MinIncome { get; set; }

        /// <summary>
        /// Maximum income for this slab (null means no upper limit)
        /// </summary>
        public decimal? MaxIncome { get; set; }

        /// <summary>
        /// Tax rate percentage for this slab
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// Health & Education cess rate (typically 4%)
        /// </summary>
        public decimal CessRate { get; set; } = 4.00m;

        /// <summary>
        /// Income threshold above which surcharge applies
        /// </summary>
        public decimal? SurchargeThreshold { get; set; }

        /// <summary>
        /// Surcharge rate percentage
        /// </summary>
        public decimal? SurchargeRate { get; set; }

        /// <summary>
        /// Taxpayer category this slab applies to:
        /// 'all' - all taxpayers (default)
        /// 'senior' - senior citizens (60-79 years)
        /// 'super_senior' - super senior citizens (80+ years)
        /// Note: Senior citizen slabs only apply to Old Regime
        /// </summary>
        public string ApplicableToCategory { get; set; } = "all";

        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

namespace Core.Entities.Payroll
{
    /// <summary>
    /// State-wise Professional Tax slab configuration
    /// </summary>
    public class ProfessionalTaxSlab
    {
        public Guid Id { get; set; }

        /// <summary>
        /// State name (e.g., 'Karnataka', 'Maharashtra')
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Minimum monthly income for this slab (inclusive)
        /// </summary>
        public decimal MinMonthlyIncome { get; set; }

        /// <summary>
        /// Maximum monthly income for this slab (null means no upper limit)
        /// </summary>
        public decimal? MaxMonthlyIncome { get; set; }

        /// <summary>
        /// Monthly professional tax amount
        /// </summary>
        public decimal MonthlyTax { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

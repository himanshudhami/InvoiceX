namespace Core.Entities.Tax
{
    /// <summary>
    /// What-if scenario for advance tax planning.
    /// Allows modeling different business scenarios and their tax impact.
    /// </summary>
    public class AdvanceTaxScenario
    {
        public Guid Id { get; set; }

        public Guid AssessmentId { get; set; }

        public string ScenarioName { get; set; } = string.Empty;

        // ==================== Adjustments from Base ====================

        /// <summary>
        /// Revenue adjustment (+ for increase, - for decrease)
        /// </summary>
        public decimal RevenueAdjustment { get; set; }

        /// <summary>
        /// Expense adjustment (+ for increase, - for decrease)
        /// </summary>
        public decimal ExpenseAdjustment { get; set; }

        /// <summary>
        /// Impact of capital expenditure on depreciation
        /// </summary>
        public decimal CapexImpact { get; set; }

        /// <summary>
        /// Change in payroll costs
        /// </summary>
        public decimal PayrollChange { get; set; }

        /// <summary>
        /// Other adjustments (tax planning items, etc.)
        /// </summary>
        public decimal OtherAdjustments { get; set; }

        // ==================== Computed Results ====================

        public decimal AdjustedTaxableIncome { get; set; }
        public decimal AdjustedTaxLiability { get; set; }

        /// <summary>
        /// Difference from base assessment's tax liability
        /// </summary>
        public decimal VarianceFromBase { get; set; }

        // ==================== Details ====================

        public string? Assumptions { get; set; } // JSON
        public string? Notes { get; set; }

        // ==================== Audit ====================

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

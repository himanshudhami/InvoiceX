namespace Core.Entities.Tax
{
    /// <summary>
    /// Annual Advance Tax Assessment for corporates under Section 207.
    /// Tracks projected income, tax computation, and payment schedule.
    /// </summary>
    public class AdvanceTaxAssessment
    {
        public Guid Id { get; set; }

        // ==================== Company & Period ====================

        public Guid CompanyId { get; set; }

        /// <summary>
        /// Indian financial year (e.g., '2024-25')
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Assessment year (e.g., '2025-26')
        /// </summary>
        public string AssessmentYear { get; set; } = string.Empty;

        /// <summary>
        /// Status: draft, active, finalized
        /// </summary>
        public string Status { get; set; } = "draft";

        // ==================== Projected Income ====================

        public decimal ProjectedRevenue { get; set; }
        public decimal ProjectedExpenses { get; set; }
        public decimal ProjectedDepreciation { get; set; }
        public decimal ProjectedOtherIncome { get; set; }
        public decimal ProjectedProfitBeforeTax { get; set; }

        // ==================== Tax Calculation ====================

        public decimal TaxableIncome { get; set; }

        /// <summary>
        /// Tax regime: normal, 115BAA (22%), 115BAB (15%)
        /// </summary>
        public string TaxRegime { get; set; } = "normal";

        public decimal TaxRate { get; set; } = 25.00m;
        public decimal SurchargeRate { get; set; }
        public decimal CessRate { get; set; } = 4.00m;

        // ==================== Computed Tax ====================

        public decimal BaseTax { get; set; }
        public decimal Surcharge { get; set; }
        public decimal Cess { get; set; }
        public decimal TotalTaxLiability { get; set; }

        // ==================== Credits ====================

        public decimal TdsReceivable { get; set; }
        public decimal TcsCredit { get; set; }
        public decimal AdvanceTaxAlreadyPaid { get; set; }
        public decimal MatCredit { get; set; }
        public decimal NetTaxPayable { get; set; }

        // ==================== Interest Liability ====================

        /// <summary>
        /// Interest u/s 234B - shortfall in advance tax payment
        /// </summary>
        public decimal Interest234B { get; set; }

        /// <summary>
        /// Interest u/s 234C - deferment of advance tax installments
        /// </summary>
        public decimal Interest234C { get; set; }

        public decimal TotalInterest { get; set; }

        // ==================== Details ====================

        public string? ComputationDetails { get; set; } // JSON
        public string? Assumptions { get; set; } // JSON
        public string? Notes { get; set; }

        // ==================== Audit ====================

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

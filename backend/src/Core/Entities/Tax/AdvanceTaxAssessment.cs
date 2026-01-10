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

        // ==================== YTD Actuals (locked) ====================

        /// <summary>
        /// Actual revenue from ledger (Apr to YtdThroughDate)
        /// </summary>
        public decimal YtdRevenue { get; set; }

        /// <summary>
        /// Actual expenses from ledger (Apr to YtdThroughDate)
        /// </summary>
        public decimal YtdExpenses { get; set; }

        /// <summary>
        /// Last date of actuals - data is locked up to this date
        /// </summary>
        public DateOnly? YtdThroughDate { get; set; }

        // ==================== Projected Additional (editable) ====================

        /// <summary>
        /// User's projection for remaining FY revenue (after YtdThroughDate)
        /// </summary>
        public decimal ProjectedAdditionalRevenue { get; set; }

        /// <summary>
        /// User's projection for remaining FY expenses (after YtdThroughDate)
        /// </summary>
        public decimal ProjectedAdditionalExpenses { get; set; }

        // ==================== Full Year Projections (computed) ====================

        /// <summary>
        /// Full year revenue = YtdRevenue + ProjectedAdditionalRevenue
        /// </summary>
        public decimal ProjectedRevenue { get; set; }

        /// <summary>
        /// Full year expenses = YtdExpenses + ProjectedAdditionalExpenses
        /// </summary>
        public decimal ProjectedExpenses { get; set; }
        public decimal ProjectedDepreciation { get; set; }
        public decimal ProjectedOtherIncome { get; set; }
        public decimal ProjectedProfitBeforeTax { get; set; }

        // ==================== Book to Taxable Reconciliation ====================

        /// <summary>
        /// Book profit as per P&L (same as ProjectedProfitBeforeTax)
        /// </summary>
        public decimal BookProfit { get; set; }

        // Additions to book profit (expenses disallowed)

        /// <summary>
        /// Add back: Depreciation as per books
        /// </summary>
        public decimal AddBookDepreciation { get; set; }

        /// <summary>
        /// Add back: Cash payments exceeding Rs 10,000 (Section 40A(3))
        /// </summary>
        public decimal AddDisallowed40A3 { get; set; }

        /// <summary>
        /// Add back: Provision for gratuity (Section 40A(7))
        /// </summary>
        public decimal AddDisallowed40A7 { get; set; }

        /// <summary>
        /// Add back: Unpaid statutory dues (Section 43B)
        /// </summary>
        public decimal AddDisallowed43B { get; set; }

        /// <summary>
        /// Add back: Other disallowed expenses
        /// </summary>
        public decimal AddOtherDisallowances { get; set; }

        /// <summary>
        /// Total additions to book profit
        /// </summary>
        public decimal TotalAdditions { get; set; }

        // Deductions from book profit

        /// <summary>
        /// Less: Depreciation as per Income Tax Act rates
        /// </summary>
        public decimal LessItDepreciation { get; set; }

        /// <summary>
        /// Less: Deductions under Section 80C
        /// </summary>
        public decimal LessDeductions80C { get; set; }

        /// <summary>
        /// Less: Deductions under Section 80D
        /// </summary>
        public decimal LessDeductions80D { get; set; }

        /// <summary>
        /// Less: Other deductions
        /// </summary>
        public decimal LessOtherDeductions { get; set; }

        /// <summary>
        /// Total deductions from book profit
        /// </summary>
        public decimal TotalDeductions { get; set; }

        // ==================== Tax Calculation ====================

        /// <summary>
        /// Taxable Income = BookProfit + TotalAdditions - TotalDeductions
        /// </summary>
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

        // ==================== Revision Tracking ====================

        public int RevisionCount { get; set; }
        public DateOnly? LastRevisionDate { get; set; }
        public int? LastRevisionQuarter { get; set; }

        // ==================== MAT (Minimum Alternate Tax) ====================

        /// <summary>
        /// True if MAT > Normal Tax for this assessment
        /// </summary>
        public bool IsMatApplicable { get; set; }

        /// <summary>
        /// Book profit for MAT calculation (may differ from taxable income)
        /// </summary>
        public decimal MatBookProfit { get; set; }

        /// <summary>
        /// MAT rate (currently 15%)
        /// </summary>
        public decimal MatRate { get; set; } = 15.00m;

        /// <summary>
        /// MAT on book profit before surcharge and cess
        /// </summary>
        public decimal MatOnBookProfit { get; set; }

        public decimal MatSurcharge { get; set; }
        public decimal MatCess { get; set; }

        /// <summary>
        /// Total MAT = MatOnBookProfit + MatSurcharge + MatCess
        /// </summary>
        public decimal TotalMat { get; set; }

        /// <summary>
        /// Available MAT credit from previous years
        /// </summary>
        public decimal MatCreditAvailable { get; set; }

        /// <summary>
        /// MAT credit being utilized this year
        /// </summary>
        public decimal MatCreditToUtilize { get; set; }

        /// <summary>
        /// New MAT credit created this year (if MAT > Normal Tax)
        /// </summary>
        public decimal MatCreditCreatedThisYear { get; set; }

        /// <summary>
        /// Final tax payable after MAT computation
        /// </summary>
        public decimal TaxPayableAfterMat { get; set; }

        // ==================== Audit ====================

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

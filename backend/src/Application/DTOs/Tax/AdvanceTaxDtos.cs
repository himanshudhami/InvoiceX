namespace Application.DTOs.Tax
{
    // ==================== Assessment DTOs ====================

    /// <summary>
    /// Complete Advance Tax Assessment data
    /// </summary>
    public class AdvanceTaxAssessmentDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string AssessmentYear { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";

        // YTD Actuals (locked - fetched from ledger)
        public decimal YtdRevenue { get; set; }
        public decimal YtdExpenses { get; set; }
        public DateOnly? YtdThroughDate { get; set; }

        // Projected Additional (editable)
        public decimal ProjectedAdditionalRevenue { get; set; }
        public decimal ProjectedAdditionalExpenses { get; set; }

        // Full Year Projections (computed: YTD + Projected Additional)
        public decimal ProjectedRevenue { get; set; }
        public decimal ProjectedExpenses { get; set; }
        public decimal ProjectedDepreciation { get; set; }
        public decimal ProjectedOtherIncome { get; set; }
        public decimal ProjectedProfitBeforeTax { get; set; }

        // Book to Taxable Reconciliation
        public decimal BookProfit { get; set; }

        // Additions (expenses disallowed)
        public decimal AddBookDepreciation { get; set; }
        public decimal AddDisallowed40A3 { get; set; }
        public decimal AddDisallowed40A7 { get; set; }
        public decimal AddDisallowed43B { get; set; }
        public decimal AddOtherDisallowances { get; set; }
        public decimal TotalAdditions { get; set; }

        // Deductions
        public decimal LessItDepreciation { get; set; }
        public decimal LessDeductions80C { get; set; }
        public decimal LessDeductions80D { get; set; }
        public decimal LessOtherDeductions { get; set; }
        public decimal TotalDeductions { get; set; }

        // Tax Calculation
        public decimal TaxableIncome { get; set; }
        public string TaxRegime { get; set; } = "normal";
        public decimal TaxRate { get; set; }
        public decimal SurchargeRate { get; set; }
        public decimal CessRate { get; set; }

        // Computed Tax
        public decimal BaseTax { get; set; }
        public decimal Surcharge { get; set; }
        public decimal Cess { get; set; }
        public decimal TotalTaxLiability { get; set; }

        // Credits
        public decimal TdsReceivable { get; set; }
        public decimal TcsCredit { get; set; }
        public decimal AdvanceTaxAlreadyPaid { get; set; }
        public decimal MatCredit { get; set; }
        public decimal NetTaxPayable { get; set; }

        // Interest
        public decimal Interest234B { get; set; }
        public decimal Interest234C { get; set; }
        public decimal TotalInterest { get; set; }

        // Details
        public string? ComputationDetails { get; set; }
        public string? Assumptions { get; set; }
        public string? Notes { get; set; }

        // Related data
        public List<AdvanceTaxScheduleDto> Schedules { get; set; } = new();
        public List<AdvanceTaxPaymentDto> Payments { get; set; } = new();

        // Revision tracking
        public int RevisionCount { get; set; }
        public DateOnly? LastRevisionDate { get; set; }
        public int? LastRevisionQuarter { get; set; }

        // MAT (Minimum Alternate Tax)
        public bool IsMatApplicable { get; set; }
        public decimal MatBookProfit { get; set; }
        public decimal MatRate { get; set; }
        public decimal MatOnBookProfit { get; set; }
        public decimal MatSurcharge { get; set; }
        public decimal MatCess { get; set; }
        public decimal TotalMat { get; set; }
        public decimal MatCreditAvailable { get; set; }
        public decimal MatCreditToUtilize { get; set; }
        public decimal MatCreditCreatedThisYear { get; set; }
        public decimal TaxPayableAfterMat { get; set; }

        // Audit
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to create/compute advance tax assessment
    /// </summary>
    public class CreateAdvanceTaxAssessmentDto
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;

        // Optional overrides (if not provided, will be computed from ledger)
        public decimal? ProjectedRevenue { get; set; }
        public decimal? ProjectedExpenses { get; set; }
        public decimal? ProjectedDepreciation { get; set; }
        public decimal? ProjectedOtherIncome { get; set; }

        // Tax regime selection
        public string TaxRegime { get; set; } = "normal"; // normal, 115BAA, 115BAB

        // Credits
        public decimal? TdsReceivable { get; set; }
        public decimal? TcsCredit { get; set; }
        public decimal? MatCredit { get; set; }

        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to update assessment projections (only editable fields)
    /// </summary>
    public class UpdateAdvanceTaxAssessmentDto
    {
        // Projected additional values (editable - for remaining FY)
        public decimal ProjectedAdditionalRevenue { get; set; }
        public decimal ProjectedAdditionalExpenses { get; set; }
        public decimal ProjectedDepreciation { get; set; }
        public decimal ProjectedOtherIncome { get; set; }

        // Book to Taxable Reconciliation - Additions
        public decimal AddBookDepreciation { get; set; }
        public decimal AddDisallowed40A3 { get; set; }
        public decimal AddDisallowed40A7 { get; set; }
        public decimal AddDisallowed43B { get; set; }
        public decimal AddOtherDisallowances { get; set; }

        // Book to Taxable Reconciliation - Deductions
        public decimal LessItDepreciation { get; set; }
        public decimal LessDeductions80C { get; set; }
        public decimal LessDeductions80D { get; set; }
        public decimal LessOtherDeductions { get; set; }

        public string TaxRegime { get; set; } = "normal";

        public decimal TdsReceivable { get; set; }
        public decimal TcsCredit { get; set; }
        public decimal MatCredit { get; set; }

        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to refresh YTD actuals from ledger
    /// </summary>
    public class RefreshYtdRequest
    {
        public Guid AssessmentId { get; set; }

        /// <summary>
        /// If true, auto-calculate projected additional based on trend
        /// </summary>
        public bool AutoProjectFromTrend { get; set; } = false;
    }

    /// <summary>
    /// YTD financials data from ledger
    /// </summary>
    public class YtdFinancialsDto
    {
        public decimal YtdRevenue { get; set; }
        public decimal YtdExpenses { get; set; }
        public DateOnly ThroughDate { get; set; }
        public int MonthsCovered { get; set; }

        // Trend-based projections
        public decimal AvgMonthlyRevenue { get; set; }
        public decimal AvgMonthlyExpenses { get; set; }
        public int RemainingMonths { get; set; }
        public decimal SuggestedAdditionalRevenue { get; set; }
        public decimal SuggestedAdditionalExpenses { get; set; }
    }

    // ==================== Schedule DTOs ====================

    /// <summary>
    /// Quarterly payment schedule
    /// </summary>
    public class AdvanceTaxScheduleDto
    {
        public Guid Id { get; set; }
        public Guid AssessmentId { get; set; }
        public int Quarter { get; set; }
        public string QuarterLabel { get; set; } = string.Empty; // Q1, Q2, Q3, Q4
        public DateOnly DueDate { get; set; }

        public decimal CumulativePercentage { get; set; }
        public decimal CumulativeTaxDue { get; set; }
        public decimal TaxPayableThisQuarter { get; set; }

        public decimal TaxPaidThisQuarter { get; set; }
        public decimal CumulativeTaxPaid { get; set; }

        public decimal ShortfallAmount { get; set; }
        public decimal Interest234C { get; set; }

        public string PaymentStatus { get; set; } = "pending";
        public bool IsOverdue { get; set; }
        public int DaysUntilDue { get; set; }
    }

    // ==================== Payment DTOs ====================

    /// <summary>
    /// Advance tax payment record
    /// </summary>
    public class AdvanceTaxPaymentDto
    {
        public Guid Id { get; set; }
        public Guid AssessmentId { get; set; }
        public Guid? ScheduleId { get; set; }
        public int? Quarter { get; set; }

        public DateOnly PaymentDate { get; set; }
        public decimal Amount { get; set; }

        public string? ChallanNumber { get; set; }
        public string? BsrCode { get; set; }
        public string? Cin { get; set; }

        public Guid? BankAccountId { get; set; }
        public string? BankAccountName { get; set; }
        public Guid? JournalEntryId { get; set; }
        public string? JournalNumber { get; set; }

        public string Status { get; set; } = "completed";
        public string? Notes { get; set; }

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to record advance tax payment
    /// </summary>
    public class RecordAdvanceTaxPaymentDto
    {
        public Guid AssessmentId { get; set; }
        public Guid? ScheduleId { get; set; }
        public DateOnly PaymentDate { get; set; }
        public decimal Amount { get; set; }

        public string? ChallanNumber { get; set; }
        public string? BsrCode { get; set; }
        public string? Cin { get; set; }

        public Guid? BankAccountId { get; set; }
        public bool CreateJournalEntry { get; set; } = true;

        public string? Notes { get; set; }
    }

    // ==================== Scenario DTOs ====================

    /// <summary>
    /// What-if scenario
    /// </summary>
    public class AdvanceTaxScenarioDto
    {
        public Guid Id { get; set; }
        public Guid AssessmentId { get; set; }
        public string ScenarioName { get; set; } = string.Empty;

        public decimal RevenueAdjustment { get; set; }
        public decimal ExpenseAdjustment { get; set; }
        public decimal CapexImpact { get; set; }
        public decimal PayrollChange { get; set; }
        public decimal OtherAdjustments { get; set; }

        public decimal AdjustedTaxableIncome { get; set; }
        public decimal AdjustedTaxLiability { get; set; }
        public decimal VarianceFromBase { get; set; }

        public string? Assumptions { get; set; }
        public string? Notes { get; set; }

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to run a scenario
    /// </summary>
    public class RunScenarioDto
    {
        public Guid AssessmentId { get; set; }
        public string ScenarioName { get; set; } = string.Empty;

        public decimal RevenueAdjustment { get; set; }
        public decimal ExpenseAdjustment { get; set; }
        public decimal CapexImpact { get; set; }
        public decimal PayrollChange { get; set; }
        public decimal OtherAdjustments { get; set; }

        public string? Assumptions { get; set; }
        public string? Notes { get; set; }
    }

    // ==================== Summary & Tracker DTOs ====================

    /// <summary>
    /// Advance tax tracker/dashboard summary
    /// </summary>
    public class AdvanceTaxTrackerDto
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string AssessmentYear { get; set; } = string.Empty;

        // Assessment summary
        public Guid? AssessmentId { get; set; }
        public string AssessmentStatus { get; set; } = string.Empty;
        public decimal TotalTaxLiability { get; set; }
        public decimal NetTaxPayable { get; set; }

        // Payment summary
        public decimal TotalAdvanceTaxPaid { get; set; }
        public decimal RemainingTaxPayable { get; set; }
        public decimal PaymentPercentage { get; set; }

        // Current quarter status
        public int CurrentQuarter { get; set; }
        public DateOnly? NextDueDate { get; set; }
        public decimal NextQuarterAmount { get; set; }
        public int DaysUntilNextDue { get; set; }

        // Interest liability
        public decimal Interest234B { get; set; }
        public decimal Interest234C { get; set; }
        public decimal TotalInterest { get; set; }

        // Schedules
        public List<AdvanceTaxScheduleDto> Schedules { get; set; } = new();
    }

    /// <summary>
    /// Interest calculation breakdown
    /// </summary>
    public class InterestCalculationDto
    {
        // Section 234B - Shortfall in advance tax
        public decimal AssessedTax { get; set; }
        public decimal AdvanceTaxPaid { get; set; }
        public decimal ShortfallFor234B { get; set; }
        public int Months234B { get; set; }
        public decimal Interest234B { get; set; }

        // Section 234C - Deferment
        public List<Interest234CQuarterDto> QuarterlyBreakdown { get; set; } = new();
        public decimal TotalInterest234C { get; set; }

        public decimal TotalInterest { get; set; }
    }

    /// <summary>
    /// Per-quarter 234C interest breakdown
    /// </summary>
    public class Interest234CQuarterDto
    {
        public int Quarter { get; set; }
        public decimal RequiredCumulative { get; set; }
        public decimal ActualCumulative { get; set; }
        public decimal Shortfall { get; set; }
        public int Months { get; set; }
        public decimal Interest { get; set; }
    }

    /// <summary>
    /// Tax computation breakdown
    /// </summary>
    public class TaxComputationDto
    {
        public decimal TaxableIncome { get; set; }

        public string TaxRegime { get; set; } = string.Empty;
        public decimal TaxRate { get; set; }
        public decimal BaseTax { get; set; }

        public decimal SurchargeRate { get; set; }
        public decimal Surcharge { get; set; }

        public decimal CessRate { get; set; }
        public decimal Cess { get; set; }

        public decimal GrossTax { get; set; }

        public decimal TdsCredit { get; set; }
        public decimal TcsCredit { get; set; }
        public decimal MatCredit { get; set; }
        public decimal TotalCredits { get; set; }

        public decimal NetTaxPayable { get; set; }
    }

    /// <summary>
    /// Preview of TDS/TCS values from modules (before applying to assessment)
    /// </summary>
    public class TdsTcsPreviewDto
    {
        /// <summary>
        /// Total TDS receivable from tds_receivable table
        /// </summary>
        public decimal TdsReceivable { get; set; }

        /// <summary>
        /// Total TCS credit (TCS paid by company) from tcs_transactions
        /// </summary>
        public decimal TcsCredit { get; set; }

        /// <summary>
        /// Current value in assessment (for comparison)
        /// </summary>
        public decimal CurrentTdsInAssessment { get; set; }

        /// <summary>
        /// Current TCS in assessment (for comparison)
        /// </summary>
        public decimal CurrentTcsInAssessment { get; set; }

        /// <summary>
        /// Difference between fetched and current TDS
        /// </summary>
        public decimal TdsDifference { get; set; }

        /// <summary>
        /// Difference between fetched and current TCS
        /// </summary>
        public decimal TcsDifference { get; set; }
    }

    // ==================== Revision DTOs ====================

    /// <summary>
    /// Revision record for audit trail
    /// </summary>
    public class AdvanceTaxRevisionDto
    {
        public Guid Id { get; set; }
        public Guid AssessmentId { get; set; }

        public int RevisionNumber { get; set; }
        public int RevisionQuarter { get; set; }
        public DateOnly RevisionDate { get; set; }

        // Before values
        public decimal PreviousProjectedRevenue { get; set; }
        public decimal PreviousProjectedExpenses { get; set; }
        public decimal PreviousTaxableIncome { get; set; }
        public decimal PreviousTotalTaxLiability { get; set; }
        public decimal PreviousNetTaxPayable { get; set; }

        // After values
        public decimal RevisedProjectedRevenue { get; set; }
        public decimal RevisedProjectedExpenses { get; set; }
        public decimal RevisedTaxableIncome { get; set; }
        public decimal RevisedTotalTaxLiability { get; set; }
        public decimal RevisedNetTaxPayable { get; set; }

        // Variance
        public decimal RevenueVariance { get; set; }
        public decimal ExpenseVariance { get; set; }
        public decimal TaxableIncomeVariance { get; set; }
        public decimal TaxLiabilityVariance { get; set; }
        public decimal NetPayableVariance { get; set; }

        public string? RevisionReason { get; set; }
        public string? Notes { get; set; }

        public Guid? RevisedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to create a revision (with new projections)
    /// </summary>
    public class CreateRevisionDto
    {
        public Guid AssessmentId { get; set; }
        public int RevisionQuarter { get; set; }

        // New projections
        public decimal ProjectedAdditionalRevenue { get; set; }
        public decimal ProjectedAdditionalExpenses { get; set; }
        public decimal ProjectedDepreciation { get; set; }
        public decimal ProjectedOtherIncome { get; set; }

        // Reconciliation adjustments
        public decimal AddBookDepreciation { get; set; }
        public decimal AddDisallowed40A3 { get; set; }
        public decimal AddDisallowed40A7 { get; set; }
        public decimal AddDisallowed43B { get; set; }
        public decimal AddOtherDisallowances { get; set; }
        public decimal LessItDepreciation { get; set; }
        public decimal LessDeductions80C { get; set; }
        public decimal LessDeductions80D { get; set; }
        public decimal LessOtherDeductions { get; set; }

        public string TaxRegime { get; set; } = "normal";
        public decimal TdsReceivable { get; set; }
        public decimal TcsCredit { get; set; }
        public decimal MatCredit { get; set; }

        public string? RevisionReason { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Revision status/prompt for dashboard
    /// </summary>
    public class RevisionStatusDto
    {
        public int CurrentQuarter { get; set; }
        public bool RevisionRecommended { get; set; }
        public string? RevisionPrompt { get; set; }
        public DateOnly? LastRevisionDate { get; set; }
        public int TotalRevisions { get; set; }

        // Variance analysis if actuals differ from projections
        public decimal ActualVsProjectedVariance { get; set; }
        public decimal VariancePercentage { get; set; }
    }

    // ==================== MAT Credit DTOs ====================

    /// <summary>
    /// MAT Credit Register entry
    /// </summary>
    public class MatCreditRegisterDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string AssessmentYear { get; set; } = string.Empty;

        // MAT computation
        public decimal BookProfit { get; set; }
        public decimal MatRate { get; set; }
        public decimal MatOnBookProfit { get; set; }
        public decimal MatSurcharge { get; set; }
        public decimal MatCess { get; set; }
        public decimal TotalMat { get; set; }

        // Normal tax comparison
        public decimal NormalTax { get; set; }

        // Credit tracking
        public decimal MatCreditCreated { get; set; }
        public decimal MatCreditUtilized { get; set; }
        public decimal MatCreditBalance { get; set; }

        // Expiry
        public string ExpiryYear { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
        public string Status { get; set; } = "active";

        public string? Notes { get; set; }

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// MAT Credit utilization record
    /// </summary>
    public class MatCreditUtilizationDto
    {
        public Guid Id { get; set; }
        public Guid MatCreditId { get; set; }
        public string UtilizationYear { get; set; } = string.Empty;
        public Guid? AssessmentId { get; set; }
        public decimal AmountUtilized { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// MAT computation summary for an assessment
    /// </summary>
    public class MatComputationDto
    {
        public Guid AssessmentId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;

        // Book profit for MAT
        public decimal BookProfit { get; set; }

        // MAT calculation
        public decimal MatRate { get; set; }
        public decimal MatOnBookProfit { get; set; }
        public decimal MatSurcharge { get; set; }
        public decimal MatSurchargeRate { get; set; }
        public decimal MatCess { get; set; }
        public decimal MatCessRate { get; set; }
        public decimal TotalMat { get; set; }

        // Normal tax
        public decimal NormalTax { get; set; }

        // Comparison
        public bool IsMatApplicable { get; set; }
        public decimal TaxDifference { get; set; }

        // Credit implications
        public decimal MatCreditCreatedThisYear { get; set; }
        public decimal MatCreditAvailable { get; set; }
        public decimal MatCreditToUtilize { get; set; }

        // Final tax
        public decimal FinalTaxPayable { get; set; }

        // Explanation
        public string MatApplicabilityReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Available MAT credit summary
    /// </summary>
    public class MatCreditSummaryDto
    {
        public Guid CompanyId { get; set; }
        public string CurrentFinancialYear { get; set; } = string.Empty;

        public decimal TotalCreditAvailable { get; set; }
        public int YearsWithCredit { get; set; }

        public List<MatCreditRegisterDto> AvailableCredits { get; set; } = new();

        // Expiring soon (within 2 years)
        public decimal ExpiringSoonAmount { get; set; }
        public int ExpiringSoonCount { get; set; }
    }
}

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

    // ==================== Form 280 (Challan) DTOs ====================

    /// <summary>
    /// Request to generate Form 280 challan
    /// </summary>
    public class GenerateForm280Request
    {
        public Guid AssessmentId { get; set; }
        public int? Quarter { get; set; }
        public decimal Amount { get; set; }
        public DateOnly? PaymentDate { get; set; }

        /// <summary>
        /// Bank through which payment will be made
        /// </summary>
        public string? BankName { get; set; }

        /// <summary>
        /// Bank branch name
        /// </summary>
        public string? BranchName { get; set; }
    }

    /// <summary>
    /// Form 280 challan data (pre-filled)
    /// </summary>
    public class Form280ChallanDto
    {
        // Taxpayer Information
        public string CompanyName { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string Tan { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Assessment Details
        public string AssessmentYear { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;

        // Payment Type Codes
        /// <summary>
        /// Major Head: 0020 (Company) or 0021 (Other than Company)
        /// </summary>
        public string MajorHead { get; set; } = "0020";
        public string MajorHeadDescription { get; set; } = "Income Tax on Companies (Corporation Tax)";

        /// <summary>
        /// Minor Head: 100 (Advance Tax), 300 (Self Assessment), 400 (Regular Assessment)
        /// </summary>
        public string MinorHead { get; set; } = "100";
        public string MinorHeadDescription { get; set; } = "Advance Tax";

        // Payment Details
        public decimal Amount { get; set; }
        public string AmountInWords { get; set; } = string.Empty;
        public int? Quarter { get; set; }
        public string? QuarterLabel { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly? PaymentDate { get; set; }

        // Bank Details (if already paid)
        public string? BankName { get; set; }
        public string? BranchName { get; set; }
        public string? ChallanNumber { get; set; }
        public string? BsrCode { get; set; }
        public string? Cin { get; set; }

        // Status
        public bool IsPaid { get; set; }
        public string Status { get; set; } = "pending";

        // Breakdown (for information)
        public decimal TotalTaxLiability { get; set; }
        public decimal TdsCredit { get; set; }
        public decimal TcsCredit { get; set; }
        public decimal AdvanceTaxPaid { get; set; }
        public decimal NetPayable { get; set; }

        // Quarter-wise requirement (for context)
        public decimal CumulativePercentRequired { get; set; }
        public decimal CumulativeAmountRequired { get; set; }
        public decimal CumulativePaid { get; set; }

        // Generation metadata
        public DateTime GeneratedAt { get; set; }
        public string FormType { get; set; } = "ITNS 280";
    }

    /// <summary>
    /// BSR Code lookup entry
    /// </summary>
    public class BsrCodeDto
    {
        public string BsrCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    // ==================== Compliance Dashboard DTOs ====================

    /// <summary>
    /// Multi-company compliance dashboard summary
    /// </summary>
    public class ComplianceDashboardDto
    {
        public string FinancialYear { get; set; } = string.Empty;
        public int TotalCompanies { get; set; }
        public int CompaniesWithAssessments { get; set; }
        public int CompaniesWithoutAssessments { get; set; }

        // Payment Status Summary
        public int CompaniesFullyPaid { get; set; }
        public int CompaniesPartiallyPaid { get; set; }
        public int CompaniesOverdue { get; set; }

        // Financial Summary
        public decimal TotalTaxLiability { get; set; }
        public decimal TotalTaxPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalInterestLiability { get; set; }

        // Current Quarter Status
        public int CurrentQuarter { get; set; }
        public DateOnly? NextDueDate { get; set; }
        public int DaysUntilNextDue { get; set; }
        public decimal NextQuarterTotalDue { get; set; }

        // Company-wise breakdown
        public List<CompanyComplianceStatusDto> CompanyStatuses { get; set; } = new();

        // Upcoming due dates
        public List<UpcomingDueDateDto> UpcomingDueDates { get; set; } = new();

        // Alerts
        public List<ComplianceAlertDto> Alerts { get; set; } = new();
    }

    /// <summary>
    /// Individual company compliance status
    /// </summary>
    public class CompanyComplianceStatusDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Pan { get; set; }

        public Guid? AssessmentId { get; set; }
        public string AssessmentStatus { get; set; } = string.Empty;

        // Tax amounts
        public decimal TotalTaxLiability { get; set; }
        public decimal TaxPaid { get; set; }
        public decimal Outstanding { get; set; }
        public decimal PaymentPercentage { get; set; }

        // Interest
        public decimal Interest234B { get; set; }
        public decimal Interest234C { get; set; }
        public decimal TotalInterest { get; set; }

        // Current quarter status
        public int CurrentQuarter { get; set; }
        public string CurrentQuarterStatus { get; set; } = string.Empty;
        public decimal CurrentQuarterDue { get; set; }
        public decimal CurrentQuarterPaid { get; set; }
        public decimal CurrentQuarterShortfall { get; set; }

        // Next due
        public DateOnly? NextDueDate { get; set; }
        public decimal NextQuarterAmount { get; set; }
        public int DaysUntilDue { get; set; }

        // Status indicators
        public bool IsOverdue { get; set; }
        public bool HasInterestLiability { get; set; }
        public bool NeedsRevision { get; set; }
        public string OverallStatus { get; set; } = string.Empty; // on_track, at_risk, overdue, no_assessment
    }

    /// <summary>
    /// Upcoming due date entry
    /// </summary>
    public class UpcomingDueDateDto
    {
        public DateOnly DueDate { get; set; }
        public int Quarter { get; set; }
        public string QuarterLabel { get; set; } = string.Empty;
        public int DaysUntilDue { get; set; }
        public int CompaniesCount { get; set; }
        public decimal TotalAmountDue { get; set; }
        public List<CompanyDueDto> Companies { get; set; } = new();
    }

    /// <summary>
    /// Company due amount for a specific date
    /// </summary>
    public class CompanyDueDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Shortfall { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Compliance alert
    /// </summary>
    public class ComplianceAlertDto
    {
        public string AlertType { get; set; } = string.Empty; // overdue, due_soon, interest_high, revision_needed, no_assessment
        public string Severity { get; set; } = string.Empty; // critical, warning, info
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Guid? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public Guid? AssessmentId { get; set; }
        public decimal? Amount { get; set; }
        public DateOnly? DueDate { get; set; }
    }

    /// <summary>
    /// Year-on-year comparison
    /// </summary>
    public class YearOnYearComparisonDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        public List<YearlyTaxSummaryDto> YearlySummaries { get; set; } = new();

        // Variance analysis
        public decimal RevenueGrowthPercent { get; set; }
        public decimal TaxLiabilityGrowthPercent { get; set; }
        public decimal EffectiveTaxRateChange { get; set; }
    }

    /// <summary>
    /// Yearly tax summary for comparison
    /// </summary>
    public class YearlyTaxSummaryDto
    {
        public string FinancialYear { get; set; } = string.Empty;
        public string AssessmentYear { get; set; } = string.Empty;

        public decimal ProjectedRevenue { get; set; }
        public decimal ProjectedExpenses { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal TotalTaxLiability { get; set; }
        public decimal EffectiveTaxRate { get; set; }

        public decimal TaxPaid { get; set; }
        public decimal Interest234B { get; set; }
        public decimal Interest234C { get; set; }
        public decimal TotalInterest { get; set; }

        public string TaxRegime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for compliance dashboard
    /// </summary>
    public class ComplianceDashboardRequest
    {
        public string FinancialYear { get; set; } = string.Empty;
        public List<Guid>? CompanyIds { get; set; } // Filter by specific companies (optional)
    }

    /// <summary>
    /// Request for year-on-year comparison
    /// </summary>
    public class YearOnYearComparisonRequest
    {
        public Guid CompanyId { get; set; }
        public int NumberOfYears { get; set; } = 3;
    }
}

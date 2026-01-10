// Advance Tax Types (Section 207 - Corporate)

export type AdvanceTaxStatus = 'draft' | 'active' | 'finalized';
export type AdvanceTaxPaymentStatus = 'pending' | 'partial' | 'paid' | 'overdue';
export type TaxRegime = 'normal' | '115BAA' | '115BAB';

// ==================== Assessment Types ====================

export interface AdvanceTaxAssessment {
  id: string;
  companyId: string;
  companyName?: string;
  financialYear: string;
  assessmentYear: string;
  status: AdvanceTaxStatus;

  // YTD Actuals (locked - fetched from ledger)
  ytdRevenue: number;
  ytdExpenses: number;
  ytdThroughDate?: string;

  // Projected Additional (editable)
  projectedAdditionalRevenue: number;
  projectedAdditionalExpenses: number;

  // Full Year Projections (computed: YTD + Projected Additional)
  projectedRevenue: number;
  projectedExpenses: number;
  projectedDepreciation: number;
  projectedOtherIncome: number;
  projectedProfitBeforeTax: number;

  // Book to Taxable Reconciliation
  bookProfit: number;
  // Additions (expenses disallowed)
  addBookDepreciation: number;
  addDisallowed40A3: number;
  addDisallowed40A7: number;
  addDisallowed43B: number;
  addOtherDisallowances: number;
  totalAdditions: number;
  // Deductions
  lessItDepreciation: number;
  lessDeductions80C: number;
  lessDeductions80D: number;
  lessOtherDeductions: number;
  totalDeductions: number;

  // Tax Calculation
  taxableIncome: number;
  taxRegime: TaxRegime;
  taxRate: number;
  surchargeRate: number;
  cessRate: number;

  // Computed Tax
  baseTax: number;
  surcharge: number;
  cess: number;
  totalTaxLiability: number;

  // Credits
  tdsReceivable: number;
  tcsCredit: number;
  advanceTaxAlreadyPaid: number;
  matCredit: number;
  netTaxPayable: number;

  // Interest
  interest234B: number;
  interest234C: number;
  totalInterest: number;

  // Details
  computationDetails?: string;
  assumptions?: string;
  notes?: string;

  // Related data
  schedules: AdvanceTaxSchedule[];
  payments: AdvanceTaxPayment[];

  // Revision tracking
  revisionCount: number;
  lastRevisionDate?: string;
  lastRevisionQuarter?: number;

  // MAT (Minimum Alternate Tax)
  isMatApplicable: boolean;
  matBookProfit: number;
  matRate: number;
  matOnBookProfit: number;
  matSurcharge: number;
  matCess: number;
  totalMat: number;
  matCreditAvailable: number;
  matCreditToUtilize: number;
  matCreditCreatedThisYear: number;
  taxPayableAfterMat: number;

  // Audit
  createdBy?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAdvanceTaxAssessmentRequest {
  companyId: string;
  financialYear: string;

  // Optional overrides (if not provided, will be computed from ledger)
  projectedRevenue?: number;
  projectedExpenses?: number;
  projectedDepreciation?: number;
  projectedOtherIncome?: number;

  // Tax regime selection
  taxRegime?: TaxRegime;

  // Credits
  tdsReceivable?: number;
  tcsCredit?: number;
  matCredit?: number;

  notes?: string;
}

export interface UpdateAdvanceTaxAssessmentRequest {
  // Projected additional values (editable - for remaining FY)
  projectedAdditionalRevenue: number;
  projectedAdditionalExpenses: number;
  projectedDepreciation: number;
  projectedOtherIncome: number;

  // Book to Taxable Reconciliation - Additions
  addBookDepreciation: number;
  addDisallowed40A3: number;
  addDisallowed40A7: number;
  addDisallowed43B: number;
  addOtherDisallowances: number;

  // Book to Taxable Reconciliation - Deductions
  lessItDepreciation: number;
  lessDeductions80C: number;
  lessDeductions80D: number;
  lessOtherDeductions: number;

  taxRegime: TaxRegime;

  tdsReceivable: number;
  tcsCredit: number;
  matCredit: number;

  notes?: string;
}

export interface RefreshYtdRequest {
  assessmentId: string;
  autoProjectFromTrend?: boolean;
}

export interface YtdFinancials {
  ytdRevenue: number;
  ytdExpenses: number;
  throughDate: string;
  monthsCovered: number;

  // Trend-based projections
  avgMonthlyRevenue: number;
  avgMonthlyExpenses: number;
  remainingMonths: number;
  suggestedAdditionalRevenue: number;
  suggestedAdditionalExpenses: number;
}

// ==================== Schedule Types ====================

export interface AdvanceTaxSchedule {
  id: string;
  assessmentId: string;
  quarter: number;
  quarterLabel: string; // Q1, Q2, Q3, Q4
  dueDate: string;

  cumulativePercentage: number;
  cumulativeTaxDue: number;
  taxPayableThisQuarter: number;

  taxPaidThisQuarter: number;
  cumulativeTaxPaid: number;

  shortfallAmount: number;
  interest234C: number;

  paymentStatus: AdvanceTaxPaymentStatus;
  isOverdue: boolean;
  daysUntilDue: number;
}

// ==================== Payment Types ====================

export interface AdvanceTaxPayment {
  id: string;
  assessmentId: string;
  scheduleId?: string;
  quarter?: number;

  paymentDate: string;
  amount: number;

  challanNumber?: string;
  bsrCode?: string;
  cin?: string;

  bankAccountId?: string;
  bankAccountName?: string;
  journalEntryId?: string;
  journalNumber?: string;

  status: string;
  notes?: string;

  createdBy?: string;
  createdAt: string;
}

export interface RecordAdvanceTaxPaymentRequest {
  assessmentId: string;
  scheduleId?: string;
  paymentDate: string;
  amount: number;

  challanNumber?: string;
  bsrCode?: string;
  cin?: string;

  bankAccountId?: string;
  createJournalEntry?: boolean;

  notes?: string;
}

// ==================== Scenario Types ====================

export interface AdvanceTaxScenario {
  id: string;
  assessmentId: string;
  scenarioName: string;

  revenueAdjustment: number;
  expenseAdjustment: number;
  capexImpact: number;
  payrollChange: number;
  otherAdjustments: number;

  adjustedTaxableIncome: number;
  adjustedTaxLiability: number;
  varianceFromBase: number;

  assumptions?: string;
  notes?: string;

  createdBy?: string;
  createdAt: string;
}

export interface RunScenarioRequest {
  assessmentId: string;
  scenarioName: string;

  revenueAdjustment: number;
  expenseAdjustment: number;
  capexImpact: number;
  payrollChange: number;
  otherAdjustments: number;

  assumptions?: string;
  notes?: string;
}

// ==================== Summary & Tracker Types ====================

export interface AdvanceTaxTracker {
  companyId: string;
  financialYear: string;
  assessmentYear: string;

  // Assessment summary
  assessmentId?: string;
  assessmentStatus: string;
  totalTaxLiability: number;
  netTaxPayable: number;

  // Payment summary
  totalAdvanceTaxPaid: number;
  remainingTaxPayable: number;
  paymentPercentage: number;

  // Current quarter status
  currentQuarter: number;
  nextDueDate?: string;
  nextQuarterAmount: number;
  daysUntilNextDue: number;

  // Interest liability
  interest234B: number;
  interest234C: number;
  totalInterest: number;

  // Schedules
  schedules: AdvanceTaxSchedule[];
}

export interface InterestCalculation {
  // Section 234B - Shortfall in advance tax
  assessedTax: number;
  advanceTaxPaid: number;
  shortfallFor234B: number;
  months234B: number;
  interest234B: number;

  // Section 234C - Deferment
  quarterlyBreakdown: Interest234CQuarter[];
  totalInterest234C: number;

  totalInterest: number;
}

export interface Interest234CQuarter {
  quarter: number;
  requiredCumulative: number;
  actualCumulative: number;
  shortfall: number;
  months: number;
  interest: number;
}

export interface TaxComputation {
  taxableIncome: number;

  taxRegime: string;
  taxRate: number;
  baseTax: number;

  surchargeRate: number;
  surcharge: number;

  cessRate: number;
  cess: number;

  grossTax: number;

  tdsCredit: number;
  tcsCredit: number;
  matCredit: number;
  totalCredits: number;

  netTaxPayable: number;
}

export interface TdsTcsPreview {
  tdsReceivable: number;
  tcsCredit: number;
  currentTdsInAssessment: number;
  currentTcsInAssessment: number;
  tdsDifference: number;
  tcsDifference: number;
}

// ==================== Revision Types ====================

export interface AdvanceTaxRevision {
  id: string;
  assessmentId: string;
  revisionNumber: number;
  revisionQuarter: number;
  revisionDate: string;

  // Before values
  previousProjectedRevenue: number;
  previousProjectedExpenses: number;
  previousTaxableIncome: number;
  previousTotalTaxLiability: number;
  previousNetTaxPayable: number;

  // After values
  revisedProjectedRevenue: number;
  revisedProjectedExpenses: number;
  revisedTaxableIncome: number;
  revisedTotalTaxLiability: number;
  revisedNetTaxPayable: number;

  // Variance
  revenueVariance: number;
  expenseVariance: number;
  taxableIncomeVariance: number;
  taxLiabilityVariance: number;
  netPayableVariance: number;

  revisionReason?: string;
  notes?: string;
  revisedBy?: string;
  createdAt: string;
}

export interface CreateRevisionRequest {
  assessmentId: string;
  revisionQuarter: number;

  // New projections
  projectedAdditionalRevenue: number;
  projectedAdditionalExpenses: number;
  projectedDepreciation: number;
  projectedOtherIncome: number;

  // Reconciliation adjustments
  addBookDepreciation: number;
  addDisallowed40A3: number;
  addDisallowed40A7: number;
  addDisallowed43B: number;
  addOtherDisallowances: number;
  lessItDepreciation: number;
  lessDeductions80C: number;
  lessDeductions80D: number;
  lessOtherDeductions: number;

  taxRegime: TaxRegime;
  tdsReceivable: number;
  tcsCredit: number;
  matCredit: number;

  revisionReason?: string;
  notes?: string;
}

export interface RevisionStatus {
  currentQuarter: number;
  revisionRecommended: boolean;
  revisionPrompt?: string;
  lastRevisionDate?: string;
  totalRevisions: number;
  actualVsProjectedVariance: number;
  variancePercentage: number;
}

// ==================== MAT Credit Types ====================

export interface MatCreditRegister {
  id: string;
  companyId: string;
  financialYear: string;
  assessmentYear: string;

  // MAT computation
  bookProfit: number;
  matRate: number;
  matOnBookProfit: number;
  matSurcharge: number;
  matCess: number;
  totalMat: number;

  // Normal tax comparison
  normalTax: number;

  // Credit tracking
  matCreditCreated: number;
  matCreditUtilized: number;
  matCreditBalance: number;

  // Expiry
  expiryYear: string;
  isExpired: boolean;
  status: string;

  notes?: string;

  createdBy?: string;
  createdAt: string;
  updatedAt: string;
}

export interface MatCreditUtilization {
  id: string;
  matCreditId: string;
  utilizationYear: string;
  assessmentId?: string;
  amountUtilized: number;
  balanceAfter: number;
  notes?: string;
  createdAt: string;
}

export interface MatComputation {
  assessmentId: string;
  financialYear: string;

  // Book profit for MAT
  bookProfit: number;

  // MAT calculation
  matRate: number;
  matOnBookProfit: number;
  matSurcharge: number;
  matSurchargeRate: number;
  matCess: number;
  matCessRate: number;
  totalMat: number;

  // Normal tax
  normalTax: number;

  // Comparison
  isMatApplicable: boolean;
  taxDifference: number;

  // Credit implications
  matCreditCreatedThisYear: number;
  matCreditAvailable: number;
  matCreditToUtilize: number;

  // Final tax
  finalTaxPayable: number;

  // Explanation
  matApplicabilityReason: string;
}

export interface MatCreditSummary {
  companyId: string;
  currentFinancialYear: string;

  totalCreditAvailable: number;
  yearsWithCredit: number;

  availableCredits: MatCreditRegister[];

  // Expiring soon (within 2 years)
  expiringSoonAmount: number;
  expiringSoonCount: number;
}

// ==================== Form 280 (Challan) Types ====================

export interface GenerateForm280Request {
  assessmentId: string;
  quarter?: number;
  amount: number;
  paymentDate?: string;
  bankName?: string;
  branchName?: string;
}

export interface Form280Challan {
  // Taxpayer Information
  companyName: string;
  pan: string;
  tan: string;
  address: string;
  city: string;
  state: string;
  pincode: string;
  email: string;
  phone: string;

  // Assessment Details
  assessmentYear: string;
  financialYear: string;

  // Payment Type Codes
  majorHead: string;
  majorHeadDescription: string;
  minorHead: string;
  minorHeadDescription: string;

  // Payment Details
  amount: number;
  amountInWords: string;
  quarter?: number;
  quarterLabel?: string;
  dueDate: string;
  paymentDate?: string;

  // Bank Details
  bankName?: string;
  branchName?: string;
  challanNumber?: string;
  bsrCode?: string;
  cin?: string;

  // Status
  isPaid: boolean;
  status: string;

  // Breakdown
  totalTaxLiability: number;
  tdsCredit: number;
  tcsCredit: number;
  advanceTaxPaid: number;
  netPayable: number;

  // Quarter-wise requirement
  cumulativePercentRequired: number;
  cumulativeAmountRequired: number;
  cumulativePaid: number;

  // Generation metadata
  generatedAt: string;
  formType: string;
}

// ==================== Compliance Dashboard Types ====================

export interface ComplianceDashboardRequest {
  financialYear: string;
  companyIds?: string[];
}

export interface ComplianceDashboard {
  financialYear: string;
  totalCompanies: number;
  companiesWithAssessments: number;
  companiesWithoutAssessments: number;

  companiesFullyPaid: number;
  companiesPartiallyPaid: number;
  companiesOverdue: number;

  totalTaxLiability: number;
  totalTaxPaid: number;
  totalOutstanding: number;
  totalInterestLiability: number;

  currentQuarter: number;
  nextDueDate?: string;
  daysUntilNextDue: number;
  nextQuarterTotalDue: number;

  companyStatuses: CompanyComplianceStatus[];
  upcomingDueDates: UpcomingDueDate[];
  alerts: ComplianceAlert[];
}

export interface CompanyComplianceStatus {
  companyId: string;
  companyName: string;
  pan?: string;

  assessmentId?: string;
  assessmentStatus: string;

  totalTaxLiability: number;
  taxPaid: number;
  outstanding: number;
  paymentPercentage: number;

  interest234B: number;
  interest234C: number;
  totalInterest: number;

  currentQuarter: number;
  currentQuarterStatus: string;
  currentQuarterDue: number;
  currentQuarterPaid: number;
  currentQuarterShortfall: number;

  nextDueDate?: string;
  nextQuarterAmount: number;
  daysUntilDue: number;

  isOverdue: boolean;
  hasInterestLiability: boolean;
  needsRevision: boolean;
  overallStatus: string; // on_track, at_risk, overdue, no_assessment
}

export interface UpcomingDueDate {
  dueDate: string;
  quarter: number;
  quarterLabel: string;
  daysUntilDue: number;
  companiesCount: number;
  totalAmountDue: number;
  companies: CompanyDue[];
}

export interface CompanyDue {
  companyId: string;
  companyName: string;
  amountDue: number;
  amountPaid: number;
  shortfall: number;
  status: string;
}

export interface ComplianceAlert {
  alertType: string;
  severity: string;
  title: string;
  message: string;
  companyId?: string;
  companyName?: string;
  assessmentId?: string;
  amount?: number;
  dueDate?: string;
}

export interface YearOnYearComparisonRequest {
  companyId: string;
  numberOfYears?: number;
}

export interface YearOnYearComparison {
  companyId: string;
  companyName: string;
  yearlySummaries: YearlyTaxSummary[];
  revenueGrowthPercent: number;
  taxLiabilityGrowthPercent: number;
  effectiveTaxRateChange: number;
}

export interface YearlyTaxSummary {
  financialYear: string;
  assessmentYear: string;
  projectedRevenue: number;
  projectedExpenses: number;
  taxableIncome: number;
  totalTaxLiability: number;
  effectiveTaxRate: number;
  taxPaid: number;
  interest234B: number;
  interest234C: number;
  totalInterest: number;
  taxRegime: string;
  status: string;
}

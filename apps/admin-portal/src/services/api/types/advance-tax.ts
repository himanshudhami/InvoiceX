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

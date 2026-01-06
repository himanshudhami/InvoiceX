// TDS Returns Types (Form 26Q/24Q)

// Deductor Details (common)
export interface DeductorDetails {
  tan: string;
  pan: string;
  name: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  pincode?: string;
  email?: string;
  phone?: string;
}

// Challan Details (common)
export interface ChallanDetail {
  bsrCode: string;
  challanDate: string;
  challanSerialNumber: string;
  challanAmount: number;
  tdsSection: string;
  tdsAmount: number;
  surcharge: number;
  educationCess: number;
  interest: number;
  penalty: number;
  totalDeposited: number;
}

// Form 26Q Data (Non-salary TDS)
export interface Form26QData {
  companyId: string;
  financialYear: string;
  quarter: string;
  deductorDetails: DeductorDetails;
  challanDetails: ChallanDetail[];
  deducteeRecords: Form26QDeducteeRecord[];
  summary: Form26QSummary;
}

export interface Form26QDeducteeRecord {
  serialNumber: number;
  deducteePan: string;
  deducteeName: string;
  tdsSection: string;
  dateOfPayment: string;
  grossAmount: number;
  tdsRate: number;
  tdsAmount: number;
  certificateNumber?: string;
  certificateDate?: string;
  remarks?: string;
}

export interface Form26QSummary {
  financialYear?: string;
  quarter?: string;
  totalDeductees?: number;
  uniqueDeductees?: number;  // Backend property name
  totalGrossAmount: number;
  totalTdsDeducted: number;
  totalTdsDeposited: number;
  variance: number;
  totalTransactions?: number;
  sectionWiseBreakdown?: SectionBreakdown[];
  sectionBreakdown?: SectionBreakdown[];  // Backend property name
  monthWiseBreakdown?: MonthBreakdown[];
  monthBreakdown?: MonthBreakdown[];  // Backend property name
}

export interface SectionBreakdown {
  section: string;
  sectionName: string;
  deducteeCount: number;
  grossAmount: number;
  tdsAmount: number;
}

export interface MonthBreakdown {
  month: number;
  year: number;
  monthName: string;
  grossAmount: number;
  tdsAmount: number;
  transactionCount: number;
}

// Form 24Q Data (Salary TDS)
export interface Form24QData {
  companyId: string;
  financialYear: string;
  quarter: string;
  deductorDetails: DeductorDetails;
  challanDetails: ChallanDetail[];
  employeeRecords: Form24QEmployeeRecord[];
  summary: Form24QSummary;
}

export interface Form24QEmployeeRecord {
  serialNumber: number;
  employeePan: string;
  employeeName: string;
  employeeCode: string;
  dateOfJoining?: string;
  dateOfLeaving?: string;
  grossSalary: number;
  tdsDeducted: number;
  monthlyDetails: MonthlySalaryTds[];
}

export interface MonthlySalaryTds {
  month: number;
  year: number;
  grossSalary: number;
  tdsDeducted: number;
}

export interface Form24QSummary {
  financialYear?: string;
  quarter?: string;
  totalEmployees: number;
  employeesWithTds?: number;
  totalGrossSalary: number;
  totalDeductions?: number;  // Not in backend, kept for compatibility
  totalTdsDeducted: number;
  totalTdsDeposited: number;
  variance: number;
  monthWiseBreakdown?: MonthBreakdown[];
  monthBreakdown?: MonthBreakdown[];  // Backend property name
}

// Form 24Q Annexure II (Q4 Annual Salary Details)
export interface Form24QAnnexureII {
  companyId: string;
  financialYear: string;
  employeeAnnualDetails: EmployeeAnnualSalaryDetail[];
}

export interface EmployeeAnnualSalaryDetail {
  employeePan: string;
  employeeName: string;
  employeeCode: string;
  dateOfJoining?: string;
  dateOfLeaving?: string;
  totalGrossSalary: number;
  totalTdsDeducted: number;
  section10Exemptions?: number;
  standardDeduction?: number;
  professionalTax?: number;
  chapter6Deductions?: number;
  taxableIncome?: number;
  taxPayable?: number;
  rebate87A?: number;
  surcharge?: number;
  educationCess?: number;
  netTaxPayable?: number;
}

// TDS Return Validation
export interface TdsReturnValidationResult {
  isValid: boolean;
  errors: ValidationError[];
  warnings: ValidationError[];
}

export interface ValidationError {
  errorCode: string;
  errorMessage: string;
  field?: string;
  recordNumber?: number;
}

// TDS Return Due Dates
export interface TdsReturnDueDate {
  formType: '26Q' | '24Q';
  quarter: string;
  dueDate: string;
  isPastDue: boolean;
  daysUntilDue: number;
}

// Pending TDS Returns
export interface PendingTdsReturn {
  formType: '26Q' | '24Q';
  financialYear: string;
  quarter: string;
  dueDate: string;
  status: 'pending' | 'overdue' | 'filed';
  daysOverdue?: number;
}

// Mark Return Filed Request
export interface MarkReturnFiledRequest {
  companyId: string;
  formType: '26Q' | '24Q';
  financialYear: string;
  quarter: string;
  acknowledgementNumber: string;
  tokenNumber?: string;
  filingDate: string;
  notes?: string;
}

// TDS Return Filing History
export interface TdsReturnFilingHistory {
  id: string;
  companyId: string;
  formType: '26Q' | '24Q';
  financialYear: string;
  quarter: string;
  acknowledgementNumber: string;
  tokenNumber?: string;
  filingDate: string;
  filedBy?: string;
  notes?: string;
  createdAt: string;
}

// Challan Reconciliation
export interface ChallanReconciliationResult {
  isReconciled: boolean;
  totalDeducted?: number;
  totalTdsDeducted?: number;  // Backend property name
  totalDeposited?: number;
  totalTdsDeposited?: number;  // Backend property name
  totalChallansDeposited?: number;  // Legacy frontend name
  variance?: number;
  difference?: number;  // Legacy frontend name
  mismatches?: ChallanMismatch[];
}

export interface ChallanMismatch {
  section: string;
  deductedAmount: number;
  depositedAmount: number;
  variance: number;
  mismatchType: 'under_deposited' | 'over_deposited' | 'missing_challan';
}

// Combined TDS Summary
export interface CombinedTdsSummary {
  financialYear: string;
  quarter: string;
  form26Q: Form26QSummary;
  form24Q: Form24QSummary;
  totalTdsDeducted: number;
  totalTdsDeposited: number;
  totalVariance: number;
}

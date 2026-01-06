// Statutory Compliance Types - RCM, LDC, TCS, Form 16, TDS Challan, PF ECR, ESI
import type { PaginationParams } from './common';

// ==================== RCM Types ====================

export interface RcmTransaction {
  id: string;
  companyId: string;
  transactionDate: string;
  vendorName: string;
  vendorGstin?: string;
  rcmCategory: 'legal' | 'security' | 'gta' | 'import_service' | 'other';
  description?: string;
  taxableValue: number;
  cgstRate: number;
  cgstAmount: number;
  sgstRate: number;
  sgstAmount: number;
  igstRate: number;
  igstAmount: number;
  cessAmount: number;
  totalGstAmount: number;
  status: 'pending' | 'rcm_paid' | 'itc_claimed';
  rcmPaidDate?: string;
  rcmChallanNumber?: string;
  itcClaimDate?: string;
  itcClaimPeriod?: string;
  sourceDocumentType?: string;
  sourceDocumentId?: string;
  sourceDocumentNumber?: string;
  journalEntryId?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateRcmTransactionDto {
  companyId: string;
  transactionDate: string;
  vendorName: string;
  vendorGstin?: string;
  rcmCategory: string;
  description?: string;
  taxableValue: number;
  cgstRate: number;
  sgstRate: number;
  igstRate: number;
  cessAmount?: number;
  sourceDocumentType?: string;
  sourceDocumentId?: string;
  sourceDocumentNumber?: string;
  notes?: string;
}

export interface RcmPaymentRequest {
  rcmTransactionId: string;
  paymentDate: string;
  challanNumber: string;
  bankAccountId?: string;
  notes?: string;
}

export interface RcmItcClaimRequest {
  rcmTransactionId: string;
  claimDate: string;
  returnPeriod: string;
  notes?: string;
}

export interface RcmSummary {
  companyId: string;
  returnPeriod: string;
  totalTaxableValue: number;
  totalRcmLiability: number;
  rcmPaid: number;
  rcmPending: number;
  itcClaimed: number;
  itcPending: number;
  categoryBreakdown: RcmCategoryBreakdown[];
}

export interface RcmCategoryBreakdown {
  category: string;
  categoryName: string;
  transactionCount: number;
  taxableValue: number;
  gstAmount: number;
}

// ==================== Lower Deduction Certificate (LDC) Types ====================

export interface LowerDeductionCertificate {
  id: string;
  companyId: string;
  deducteePan: string;
  deducteeName: string;
  certificateNumber: string;
  certificateType: 'lower' | 'nil';
  tdsSection: string;
  normalRate: number;
  certificateRate: number;
  validFromDate: string;
  validToDate: string;
  thresholdAmount?: number;
  utilizedAmount: number;
  remainingAmount?: number;
  issuingAuthority?: string;
  status: 'active' | 'expired' | 'exhausted' | 'cancelled';
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateLdcDto {
  companyId: string;
  deducteePan: string;
  deducteeName: string;
  certificateNumber: string;
  certificateType: 'lower' | 'nil';
  tdsSection: string;
  normalRate: number;
  certificateRate: number;
  validFromDate: string;
  validToDate: string;
  thresholdAmount?: number;
  issuingAuthority?: string;
  notes?: string;
}

export interface UpdateLdcDto {
  certificateRate?: number;
  validToDate?: string;
  thresholdAmount?: number;
  status?: string;
  notes?: string;
}

export interface LdcValidationResult {
  isValid: boolean;
  certificateId?: string;
  certificateNumber?: string;
  certificateType?: string;
  normalRate: number;
  certificateRate: number;
  remainingThreshold?: number;
  validationMessage?: string;
}

export interface LdcUsageRecord {
  id: string;
  certificateId: string;
  companyId: string;
  transactionDate: string;
  transactionType: string;
  transactionId?: string;
  grossAmount: number;
  normalTdsAmount: number;
  actualTdsAmount: number;
  tdsSavings: number;
  createdAt: string;
}

// ==================== TCS Types ====================

export interface TcsTransaction {
  id: string;
  companyId: string;
  transactionDate: string;
  customerId?: string;
  customerName: string;
  customerPan?: string;
  invoiceId?: string;
  invoiceNumber?: string;
  tcsSection: string;
  netSaleValue: number;
  tcsRate: number;
  tcsAmount: number;
  status: 'collected' | 'remitted';
  remittanceDate?: string;
  challanNumber?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateTcsTransactionDto {
  companyId: string;
  transactionDate: string;
  customerId?: string;
  customerName: string;
  customerPan?: string;
  invoiceId?: string;
  invoiceNumber?: string;
  tcsSection: string;
  netSaleValue: number;
  tcsRate: number;
  notes?: string;
}

export interface TcsRemittanceRequest {
  tcsTransactionIds: string[];
  remittanceDate: string;
  challanNumber: string;
  bankAccountId?: string;
  notes?: string;
}

export interface TcsSummary {
  companyId: string;
  financialYear: string;
  quarter?: string;
  totalSaleValue: number;
  totalTcsCollected: number;
  tcsRemitted: number;
  tcsPending: number;
  transactionCount: number;
  sectionBreakdown: TcsSectionBreakdown[];
}

export interface TcsSectionBreakdown {
  section: string;
  sectionName: string;
  transactionCount: number;
  saleValue: number;
  tcsAmount: number;
}

// ==================== Form 16 Types ====================

export interface Form16Data {
  id: string;
  companyId: string;
  employeeId: string;
  financialYear: string;
  assessmentYear: string;
  employeeName: string;
  employeePan: string;
  // Part A - TDS Summary
  tan: string;
  deductorName: string;
  deductorAddress?: string;
  periodFrom: string;
  periodTo: string;
  q1TdsDeducted: number;
  q1TdsDeposited: number;
  q2TdsDeducted: number;
  q2TdsDeposited: number;
  q3TdsDeducted: number;
  q3TdsDeposited: number;
  q4TdsDeducted: number;
  q4TdsDeposited: number;
  totalTdsDeducted: number;
  totalTdsDeposited: number;
  // Part B - Salary Details
  grossSalary: number;
  exemptions: Form16Exemptions;
  deductions: Form16Deductions;
  netTaxableIncome: number;
  taxPayable: number;
  rebate87a: number;
  surcharge: number;
  educationCess: number;
  relief89: number;
  netTaxPayable: number;
  // Status
  status: 'draft' | 'generated' | 'issued' | 'revised';
  generatedAt?: string;
  issuedAt?: string;
  pdfPath?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface Form16Exemptions {
  hra?: number;
  lta?: number;
  section10Others?: number;
  standardDeduction?: number;
  professionalTax?: number;
}

export interface Form16Deductions {
  section80C?: number;
  section80CCD1B?: number;
  section80D?: number;
  section80E?: number;
  section80G?: number;
  section80TTA?: number;
  section24?: number;
  otherDeductions?: number;
}

export interface Form16Summary {
  companyId: string;
  financialYear: string;
  totalEmployees: number;
  generated: number;
  issued: number;
  pending: number;
  totalTdsDeducted: number;
  totalTdsDeposited: number;
}

export interface GenerateForm16Request {
  companyId: string;
  financialYear: string;
  employeeIds?: string[];
}

export interface Form16FilterParams extends PaginationParams {
  companyId?: string;
  financialYear?: string;
  status?: string;
  employeeId?: string;
}

// ==================== TDS Challan 281 Types ====================

export interface TdsChallan {
  id: string;
  companyId: string;
  payrollRunId?: string;
  tdsType: 'salary' | 'contractor' | 'rent' | 'professional';
  challanType: '281';
  periodMonth: number;
  periodYear: number;
  financialYear: string;
  // Company Details
  tan: string;
  pan: string;
  companyName: string;
  address?: string;
  city?: string;
  state?: string;
  pincode?: string;
  // Tax Details
  majorHead: string;
  minorHead: string;
  assessmentYear: string;
  basicTax: number;
  surcharge: number;
  educationCess: number;
  interest: number;
  lateFee: number;
  totalAmount: number;
  // Payment Details
  status: 'draft' | 'pending' | 'paid' | 'verified';
  dueDate: string;
  paymentDate?: string;
  paymentMode?: string;
  bankName?: string;
  bsrCode?: string;
  cinNumber?: string;
  bankReference?: string;
  // Audit
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  paidBy?: string;
}

export interface TdsChallanSummary {
  companyId: string;
  financialYear: string;
  totalTdsDeducted: number;
  totalTdsDeposited: number;
  totalVariance: number;
  paidCount: number;
  pendingCount: number;
  overdueCount: number;
  monthlyStatus: TdsChallanMonthlyStatus[];
}

export interface TdsChallanMonthlyStatus {
  month: number;
  year: number;
  monthName: string;
  tdsDeducted: number;
  tdsDeposited: number;
  status: string;
  dueDate: string;
  paymentDate?: string;
  challanId?: string;
  cinNumber?: string;
}

export interface CreateTdsChallanRequest {
  companyId: string;
  tdsType: 'salary' | 'contractor';
  periodMonth: number;
  periodYear: number;
  payrollRunId?: string;
  proposedPaymentDate: string;
  createdBy?: string;
}

export interface RecordTdsPaymentRequest {
  paymentDate: string;
  paymentMode: string;
  bankName?: string;
  bsrCode?: string;
  cinNumber: string;
  bankReference?: string;
  actualAmountPaid: number;
  paidBy?: string;
}

export interface UpdateCinRequest {
  bsrCode: string;
  cin: string;
  challanSerialNumber?: string;
  depositDate?: string;
  remarks?: string;
}

export interface TdsChallanFilterParams extends PaginationParams {
  companyId?: string;
  financialYear?: string;
  tdsType?: string;
  status?: string;
}

// ==================== PF ECR Types ====================

export interface PfEcrData {
  id?: string;
  companyId: string;
  payrollRunId?: string;
  periodMonth: number;
  periodYear: number;
  financialYear: string;
  wageMonth: string;
  // Establishment Details
  establishmentId: string;
  establishmentName: string;
  // Summary
  memberCount: number;
  totalEpfWages: number;
  totalEpsWages: number;
  totalEmployeeEpf: number;
  totalEmployerEpf: number;
  totalEmployerEps: number;
  totalEdliContribution: number;
  totalAdminCharges: number;
  totalContribution: number;
  // Due Date
  dueDate: string;
  isOverdue: boolean;
  daysOverdue: number;
  // Records
  memberRecords: PfEcrMemberRecord[];
}

export interface PfEcrMemberRecord {
  employeeId: string;
  payrollTransactionId: string;
  uan: string;
  memberName: string;
  epfWages: number;
  epsWages: number;
  employeeEpf: number;
  employerEpf: number;
  employerEps: number;
  edliContribution: number;
  ncp: number;
  refundOfAdvances?: number;
}

export interface PfEcrPreview {
  ecrData: PfEcrData;
  proposedPaymentDate: string;
  baseAmount: number;
  interestAmount: number;
  damagesAmount: number;
  totalPayable: number;
  hasWarnings: boolean;
  warnings: string[];
}

export interface PfEcrFileResult {
  fileName: string;
  fileContent: string;
  fileFormat: string;
  recordCount: number;
  totalAmount: number;
  generatedAt: string;
  base64Content?: string;
}

export interface PfEcrSummary {
  companyId: string;
  financialYear: string;
  totalEpfDeducted: number;
  totalEpfDeposited: number;
  totalVariance: number;
  paidCount: number;
  pendingCount: number;
  overdueCount: number;
  monthlyStatus: PfEcrMonthlyStatus[];
}

export interface PfEcrMonthlyStatus {
  month: number;
  year: number;
  monthName: string;
  memberCount: number;
  epfDeducted: number;
  epfDeposited: number;
  variance: number;
  dueDate: string;
  paymentDate?: string;
  status: string;
  trrn?: string;
  statutoryPaymentId?: string;
}

export interface CreatePfEcrPaymentRequest {
  companyId: string;
  periodMonth: number;
  periodYear: number;
  payrollRunId?: string;
  proposedPaymentDate: string;
  createdBy?: string;
}

export interface RecordPfPaymentRequest {
  paymentDate: string;
  paymentMode: string;
  bankName?: string;
  bankAccountId?: string;
  bankReference?: string;
  actualAmountPaid: number;
  paidBy?: string;
}

export interface PfEcrFilterParams extends PaginationParams {
  companyId?: string;
  financialYear?: string;
  status?: string;
}

// ==================== ESI Return Types ====================

export interface EsiReturnData {
  id?: string;
  companyId: string;
  payrollRunId?: string;
  periodMonth: number;
  periodYear: number;
  financialYear: string;
  contributionPeriod: string;
  wageMonth: string;
  // Employer Details
  esiCode: string;
  employerName: string;
  // Summary
  employeeCount: number;
  coveredEmployees: number;
  totalGrossWages: number;
  totalCoveredWages: number;
  // Contributions
  totalEmployeeContribution: number;
  totalEmployerContribution: number;
  totalContribution: number;
  // Due Date
  dueDate: string;
  isOverdue: boolean;
  daysOverdue: number;
  // Records
  employeeRecords: EsiReturnEmployeeRecord[];
}

export interface EsiReturnEmployeeRecord {
  employeeId: string;
  payrollTransactionId: string;
  ipNumber: string;
  employeeName: string;
  grossWages: number;
  isCovered: boolean;
  employeeContribution: number;
  employerContribution: number;
  totalContribution: number;
  daysWorked: number;
  absentDays: number;
  isNewEmployee: boolean;
  hasExited: boolean;
  dateOfJoining?: string;
  dateOfExit?: string;
  noContributionReason?: string;
}

export interface EsiReturnPreview {
  returnData: EsiReturnData;
  proposedPaymentDate: string;
  baseAmount: number;
  interestAmount: number;
  totalPayable: number;
  hasWarnings: boolean;
  warnings: string[];
}

export interface EsiReturnFileResult {
  fileName: string;
  fileContent: string;
  fileFormat: string;
  recordCount: number;
  totalAmount: number;
  generatedAt: string;
  base64Content?: string;
}

export interface EsiReturnSummary {
  companyId: string;
  financialYear: string;
  totalEsiDeducted: number;
  totalEsiDeposited: number;
  totalVariance: number;
  paidCount: number;
  pendingCount: number;
  overdueCount: number;
  monthlyStatus: EsiReturnMonthlyStatus[];
}

export interface EsiReturnMonthlyStatus {
  month: number;
  year: number;
  monthName: string;
  contributionPeriod: string;
  employeeCount: number;
  esiDeducted: number;
  esiDeposited: number;
  variance: number;
  dueDate: string;
  paymentDate?: string;
  status: string;
  challanNumber?: string;
  statutoryPaymentId?: string;
}

export interface CreateEsiReturnRequest {
  companyId: string;
  periodMonth: number;
  periodYear: number;
  payrollRunId?: string;
  proposedPaymentDate: string;
  createdBy?: string;
}

export interface RecordEsiPaymentRequest {
  paymentDate: string;
  paymentMode: string;
  bankName?: string;
  bankAccountId?: string;
  bankReference?: string;
  actualAmountPaid: number;
  paidBy?: string;
}

export interface EsiReturnFilterParams extends PaginationParams {
  companyId?: string;
  financialYear?: string;
  status?: string;
}

// ==================== Statutory Dashboard Types ====================

export interface StatutoryComplianceSummary {
  companyId: string;
  financialYear: string;
  tds: {
    totalDeducted: number;
    totalDeposited: number;
    pendingAmount: number;
    overdueCount: number;
  };
  pf: {
    totalDeducted: number;
    totalDeposited: number;
    pendingAmount: number;
    overdueCount: number;
  };
  esi: {
    totalDeducted: number;
    totalDeposited: number;
    pendingAmount: number;
    overdueCount: number;
  };
  upcomingDueDates: StatutoryDueDate[];
}

export interface StatutoryDueDate {
  type: 'tds' | 'pf' | 'esi';
  periodMonth: number;
  periodYear: number;
  periodName: string;
  dueDate: string;
  amount: number;
  status: string;
  daysUntilDue: number;
  isOverdue: boolean;
}

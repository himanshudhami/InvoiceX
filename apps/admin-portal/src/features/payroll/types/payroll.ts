// Payroll types matching backend DTOs

// ==================== Payroll Runs ====================
export interface PayrollRun {
  id: string;
  companyId: string;
  payrollMonth: number;
  payrollYear: number;
  financialYear: string;
  status: 'draft' | 'processing' | 'computed' | 'approved' | 'paid' | 'cancelled';
  totalEmployees: number;
  totalContractors: number;
  totalGrossSalary: number;
  totalDeductions: number;
  totalNetSalary: number;
  totalEmployerPf: number;
  totalEmployerEsi: number;
  totalEmployerCost: number;
  computedBy?: string;
  computedAt?: string;
  approvedBy?: string;
  approvedAt?: string;
  paidBy?: string;
  paidAt?: string;
  paymentReference?: string;
  paymentMode?: string;
  remarks?: string;
  createdAt?: string;
  updatedAt?: string;
  companyName?: string;
  monthName?: string;
}

export interface CreatePayrollRunDto {
  companyId: string;
  payrollMonth: number;
  payrollYear: number;
  remarks?: string;
  createdBy?: string;
}

export interface UpdatePayrollRunDto {
  status?: string;
  paymentReference?: string;
  paymentMode?: string;
  remarks?: string;
  updatedBy?: string;
  bankAccountId?: string;
}

export interface ProcessPayrollDto {
  companyId: string;
  payrollMonth: number;
  payrollYear: number;
  includeContractors?: boolean;
  processedBy?: string;
}

export interface PayrollRunSummary {
  payrollRunId: string;
  monthYear: string;
  status: string;
  totalEmployees: number;
  totalContractors: number;
  totalGross: number;
  totalDeductions: number;
  totalNet: number;
  totalEmployerCost: number;
  totalPfEmployee: number;
  totalPfEmployer: number;
  totalEsiEmployee: number;
  totalEsiEmployer: number;
  totalPt: number;
  totalTds: number;
}

export interface PayrollPreview {
  companyId: string;
  payrollMonth: number;
  payrollYear: number;
  employeeCount: number;
  totalMonthlyGross: number;
  totalPfEmployee: number;
  totalPfEmployer: number;
  totalEsiEmployee: number;
  totalEsiEmployer: number;
  totalPt: number;
  totalTds: number;
  totalDeductions: number;
  totalNetPay: number;
  employeesWithoutStructure: string[];
}

// ==================== Payroll Transactions ====================
export interface PayrollTransaction {
  id: string;
  payrollRunId: string;
  employeeId: string;
  salaryStructureId?: string;
  payrollMonth: number;
  payrollYear: number;
  payrollType: 'employee' | 'contractor';
  workingDays: number;
  presentDays: number;
  lopDays: number;
  // Earnings
  basicEarned: number;
  hraEarned: number;
  daEarned: number;
  conveyanceEarned: number;
  medicalEarned: number;
  specialAllowanceEarned: number;
  otherAllowancesEarned: number;
  ltaPaid: number;
  bonusPaid: number;
  arrears: number;
  reimbursements: number;
  incentives: number;
  otherEarnings: number;
  grossEarnings: number;
  // Deductions
  pfEmployee: number;
  esiEmployee: number;
  professionalTax: number;
  tdsDeducted: number;
  loanRecovery: number;
  advanceRecovery: number;
  otherDeductions: number;
  totalDeductions: number;
  // Net Pay
  netPayable: number;
  // Employer Contributions
  pfEmployer: number;
  pfAdminCharges: number;
  pfEdli: number;
  esiEmployer: number;
  gratuityProvision: number;
  totalEmployerCost: number;
  // TDS
  tdsCalculation?: string;
  tdsHrOverride?: number;
  tdsOverrideReason?: string;
  // Payment
  status: string;
  paymentDate?: string;
  paymentMethod?: string;
  paymentReference?: string;
  bankAccount?: string;
  remarks?: string;
  createdAt?: string;
  updatedAt?: string;
  employeeName?: string;
  companyName?: string;
  monthName?: string;
}

export interface CreatePayrollTransactionDto {
  payrollRunId: string;
  employeeId: string;
  salaryStructureId?: string;
  payrollMonth: number;
  payrollYear: number;
  payrollType?: 'employee' | 'contractor';
  workingDays?: number;
  presentDays?: number;
  lopDays?: number;
  ltaPaid?: number;
  bonusPaid?: number;
  arrears?: number;
  reimbursements?: number;
  incentives?: number;
  otherEarnings?: number;
  loanRecovery?: number;
  advanceRecovery?: number;
  otherDeductions?: number;
  remarks?: string;
}

export interface UpdatePayrollTransactionDto {
  workingDays?: number;
  presentDays?: number;
  lopDays?: number;
  ltaPaid?: number;
  bonusPaid?: number;
  arrears?: number;
  reimbursements?: number;
  incentives?: number;
  otherEarnings?: number;
  loanRecovery?: number;
  advanceRecovery?: number;
  otherDeductions?: number;
  remarks?: string;
}

export interface TdsOverrideDto {
  tdsAmount: number;
  reason: string;
}

// ==================== Salary Structures ====================
export interface EmployeeSalaryStructure {
  id: string;
  employeeId: string;
  companyId: string;
  effectiveFrom: string;
  effectiveTo?: string;
  annualCtc: number;
  basicSalary: number;
  hra: number;
  dearnessAllowance: number;
  conveyanceAllowance: number;
  medicalAllowance: number;
  specialAllowance: number;
  otherAllowances: number;
  ltaAnnual: number;
  bonusAnnual: number;
  pfEmployerMonthly: number;
  esiEmployerMonthly: number;
  gratuityMonthly: number;
  monthlyGross: number;
  isActive: boolean;
  revisionReason?: string;
  approvedBy?: string;
  approvedAt?: string;
  createdAt?: string;
  updatedAt?: string;
  employeeName?: string;
  companyName?: string;
}

export interface CreateEmployeeSalaryStructureDto {
  employeeId: string;
  companyId: string;
  effectiveFrom: string;
  annualCtc: number;
  basicSalary: number;
  hra: number;
  dearnessAllowance: number;
  conveyanceAllowance: number;
  medicalAllowance: number;
  specialAllowance: number;
  otherAllowances: number;
  ltaAnnual: number;
  bonusAnnual: number;
  pfEmployerMonthly: number;
  esiEmployerMonthly: number;
  gratuityMonthly: number;
  revisionReason?: string;
  approvedBy?: string;
  createdBy?: string;
}

export interface UpdateEmployeeSalaryStructureDto {
  effectiveFrom?: string;
  effectiveTo?: string;
  annualCtc?: number;
  basicSalary?: number;
  hra?: number;
  dearnessAllowance?: number;
  conveyanceAllowance?: number;
  medicalAllowance?: number;
  specialAllowance?: number;
  otherAllowances?: number;
  ltaAnnual?: number;
  bonusAnnual?: number;
  pfEmployerMonthly?: number;
  esiEmployerMonthly?: number;
  gratuityMonthly?: number;
  isActive?: boolean;
  revisionReason?: string;
  approvedBy?: string;
  updatedBy?: string;
}

export interface SalaryBreakdown {
  annualCtc: number;
  monthlyCtc: number;
  basicSalary: number;
  hra: number;
  dearnessAllowance: number;
  conveyanceAllowance: number;
  medicalAllowance: number;
  specialAllowance: number;
  otherAllowances: number;
  monthlyGross: number;
  pfEmployer: number;
  esiEmployer: number;
  gratuity: number;
  ltaAnnual: number;
  bonusAnnual: number;
}

// ==================== Tax Declarations ====================
export interface EmployeeTaxDeclaration {
  id: string;
  employeeId: string;
  financialYear: string;
  taxRegime: 'old' | 'new';
  // Section 80C
  sec80cPpf: number;
  sec80cElss: number;
  sec80cLifeInsurance: number;
  sec80cHomeLoanPrincipal: number;
  sec80cChildrenTuition: number;
  sec80cNsc: number;
  sec80cSukanyaSamriddhi: number;
  sec80cFixedDeposit: number;
  sec80cOthers: number;
  // Section 80CCD(1B)
  sec80ccdNps: number;
  // Section 80D
  sec80dSelfFamily: number;
  sec80dParents: number;
  sec80dPreventiveCheckup: number;
  sec80dSelfSeniorCitizen: boolean;
  sec80dParentsSeniorCitizen: boolean;
  // Other Sections
  sec80eEducationLoan: number;
  sec24HomeLoanInterest: number;
  sec80gDonations: number;
  sec80ttaSavingsInterest: number;
  // HRA
  hraRentPaidAnnual: number;
  hraMetroCity: boolean;
  hraLandlordPan?: string;
  hraLandlordName?: string;
  // Other Income
  otherIncomeAnnual: number;
  // Column 388A - Other TDS/TCS Credits (CBDT Feb 2025)
  otherTdsInterest: number;
  otherTdsDividend: number;
  otherTdsCommission: number;
  otherTdsRent: number;
  otherTdsProfessional: number;
  otherTdsOthers: number;
  tcsForeignRemittance: number;
  tcsOverseasTour: number;
  tcsVehiclePurchase: number;
  tcsOthers: number;
  otherTdsTcsDetails?: string;
  // 388A Calculated totals
  totalOtherTds: number;
  totalTcs: number;
  totalColumn388A: number;
  // Previous Employer
  prevEmployerIncome: number;
  prevEmployerTds: number;
  prevEmployerPf: number;
  prevEmployerPt: number;
  // Status
  status: 'draft' | 'submitted' | 'verified' | 'rejected' | 'locked';
  submittedAt?: string;
  verifiedBy?: string;
  verifiedAt?: string;
  lockedAt?: string;
  proofDocuments?: string;
  // Rejection workflow
  rejectedAt?: string;
  rejectedBy?: string;
  rejectionReason?: string;
  revisionCount: number;
  // Timestamps
  createdAt?: string;
  updatedAt?: string;
  // Calculated totals
  total80cDeduction: number;
  total80dDeduction: number;
  totalDeductions: number;
  employeeName?: string;
}

// Rejection workflow DTOs
export interface RejectDeclarationDto {
  reason: string;
  comments?: string;
}

export interface DeclarationHistoryEntry {
  id: string;
  declarationId: string;
  action: 'created' | 'updated' | 'submitted' | 'verified' | 'rejected' | 'locked' | 'unlocked' | 'revised';
  changedBy: string;
  changedAt: string;
  previousValues?: string;
  newValues?: string;
  rejectionReason?: string;
  rejectionComments?: string;
}

// Tax declaration summary with capped values
export interface TaxDeclarationSummary {
  declarationId?: string;
  financialYear: string;
  taxRegime: string;
  // Section 80C
  section80CTotal: number;
  section80CAllowed: number;
  section80CExcess: number;
  // Section 80CCD(1B)
  section80ccdTotal: number;
  section80ccdAllowed: number;
  // Section 80D
  section80DSelfFamilyAllowed: number;
  section80DParentsAllowed: number;
  section80DPreventiveAllowed: number;
  section80DTotal: number;
  // Other Sections
  section80ETotal: number;
  section24Allowed: number;
  section80GAllowed: number;
  section80TTAAllowed: number;
  // HRA
  hraRentDeclared: number;
  requiresPanForHra: boolean;
  hasValidLandlordPan: boolean;
  // Grand Total
  totalAllowedDeductions: number;
  // Validation
  warnings: string[];
  errors: string[];
}

// Tax deduction limits (for frontend validation)
export const TAX_LIMITS = {
  MAX_80C: 150000,
  MAX_80CCD_NPS: 50000,
  MAX_80D_SELF_FAMILY: 25000,
  MAX_80D_SELF_FAMILY_SENIOR: 50000,
  MAX_80D_PARENTS: 25000,
  MAX_80D_PARENTS_SENIOR: 50000,
  MAX_80D_PREVENTIVE: 5000,
  MAX_SECTION_24: 200000,
  MAX_80TTA: 10000,
  HRA_PAN_THRESHOLD: 100000,
} as const;

export interface CreateEmployeeTaxDeclarationDto {
  employeeId: string;
  financialYear: string;
  taxRegime?: 'old' | 'new';
  sec80cPpf?: number;
  sec80cElss?: number;
  sec80cLifeInsurance?: number;
  sec80cHomeLoanPrincipal?: number;
  sec80cChildrenTuition?: number;
  sec80cNsc?: number;
  sec80cSukanyaSamriddhi?: number;
  sec80cFixedDeposit?: number;
  sec80cOthers?: number;
  sec80ccdNps?: number;
  sec80dSelfFamily?: number;
  sec80dParents?: number;
  sec80dPreventiveCheckup?: number;
  sec80dSelfSeniorCitizen?: boolean;
  sec80dParentsSeniorCitizen?: boolean;
  sec80eEducationLoan?: number;
  sec24HomeLoanInterest?: number;
  sec80gDonations?: number;
  sec80ttaSavingsInterest?: number;
  hraRentPaidAnnual?: number;
  hraMetroCity?: boolean;
  hraLandlordPan?: string;
  hraLandlordName?: string;
  otherIncomeAnnual?: number;
  // Column 388A - Other TDS/TCS Credits
  otherTdsInterest?: number;
  otherTdsDividend?: number;
  otherTdsCommission?: number;
  otherTdsRent?: number;
  otherTdsProfessional?: number;
  otherTdsOthers?: number;
  tcsForeignRemittance?: number;
  tcsOverseasTour?: number;
  tcsVehiclePurchase?: number;
  tcsOthers?: number;
  otherTdsTcsDetails?: string;
  prevEmployerIncome?: number;
  prevEmployerTds?: number;
  prevEmployerPf?: number;
  prevEmployerPt?: number;
}

export interface UpdateEmployeeTaxDeclarationDto {
  taxRegime?: 'old' | 'new';
  sec80cPpf?: number;
  sec80cElss?: number;
  sec80cLifeInsurance?: number;
  sec80cHomeLoanPrincipal?: number;
  sec80cChildrenTuition?: number;
  sec80cNsc?: number;
  sec80cSukanyaSamriddhi?: number;
  sec80cFixedDeposit?: number;
  sec80cOthers?: number;
  sec80ccdNps?: number;
  sec80dSelfFamily?: number;
  sec80dParents?: number;
  sec80dPreventiveCheckup?: number;
  sec80dSelfSeniorCitizen?: boolean;
  sec80dParentsSeniorCitizen?: boolean;
  sec80eEducationLoan?: number;
  sec24HomeLoanInterest?: number;
  sec80gDonations?: number;
  sec80ttaSavingsInterest?: number;
  hraRentPaidAnnual?: number;
  hraMetroCity?: boolean;
  hraLandlordPan?: string;
  hraLandlordName?: string;
  otherIncomeAnnual?: number;
  // Column 388A - Other TDS/TCS Credits
  otherTdsInterest?: number;
  otherTdsDividend?: number;
  otherTdsCommission?: number;
  otherTdsRent?: number;
  otherTdsProfessional?: number;
  otherTdsOthers?: number;
  tcsForeignRemittance?: number;
  tcsOverseasTour?: number;
  tcsVehiclePurchase?: number;
  tcsOthers?: number;
  otherTdsTcsDetails?: string;
  prevEmployerIncome?: number;
  prevEmployerTds?: number;
  prevEmployerPf?: number;
  prevEmployerPt?: number;
  proofDocuments?: string;
}

// ==================== Contractor Payments ====================
// Links to parties table (unified party model) instead of employees
export interface ContractorPayment {
  id: string;
  partyId: string;
  companyId: string;
  paymentMonth: number;
  paymentYear: number;
  invoiceNumber?: string;
  contractReference?: string;
  grossAmount: number;
  tdsSection: string;
  tdsRate: number;
  tdsAmount: number;
  contractorPan?: string;
  otherDeductions: number;
  netPayable: number;
  gstApplicable: boolean;
  gstRate: number;
  gstAmount: number;
  totalInvoiceAmount?: number;
  status: 'pending' | 'approved' | 'paid' | 'cancelled';
  paymentDate?: string;
  paymentMethod?: string;
  paymentReference?: string;
  description?: string;
  remarks?: string;
  createdAt?: string;
  updatedAt?: string;
  partyName?: string;
  companyName?: string;
}

export interface CreateContractorPaymentDto {
  partyId: string;
  companyId: string;
  paymentMonth: number;
  paymentYear: number;
  grossAmount: number;
  tdsSection?: string;
  tdsRate?: number;
  otherDeductions?: number;
  gstApplicable?: boolean;
  gstRate?: number;
  invoiceNumber?: string;
  contractReference?: string;
  description?: string;
  remarks?: string;
  createdBy?: string;
}

export interface UpdateContractorPaymentDto {
  grossAmount?: number;
  tdsSection?: string;
  tdsRate?: number;
  otherDeductions?: number;
  gstApplicable?: boolean;
  gstRate?: number;
  invoiceNumber?: string;
  contractReference?: string;
  description?: string;
  remarks?: string;
  status?: string;
  paymentDate?: string;
  paymentMethod?: string;
  paymentReference?: string;
  updatedBy?: string;
  bankAccountId?: string;
}

export interface ContractorPaymentSummary {
  partyId: string;
  financialYear: string;
  totalGross: number;
  totalTds: number;
  totalGst: number;
  totalNet: number;
  paymentCount: number;
}

// ==================== Employee Payroll Info ====================
export interface EmployeePayrollInfo {
  id: string;
  employeeId: string;
  companyId: string;
  uan?: string;
  pfAccountNumber?: string;
  esiNumber?: string;
  bankAccountNumber?: string;
  bankName?: string;
  bankIfsc?: string;
  taxRegime: 'old' | 'new';
  panNumber?: string;
  payrollType: 'employee' | 'contractor';
  isPfApplicable: boolean;
  isEsiApplicable: boolean;
  isPtApplicable: boolean;
  optedForRestrictedPf: boolean;
  dateOfJoining?: string;
  dateOfLeaving?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  // Compliance Fields
  residentialStatus: 'resident' | 'non_resident' | 'rnor';
  dateOfBirth?: string;
  taxRegimeEffectiveFrom?: string;
  workState?: string;
  // Computed fields
  age?: number;
  isSeniorCitizen?: boolean;
  isSuperSeniorCitizen?: boolean;
  // Navigation
  employeeName?: string;
  companyName?: string;
}

export interface CreateEmployeePayrollInfoDto {
  employeeId: string;
  companyId: string;
  uan?: string;
  pfAccountNumber?: string;
  esiNumber?: string;
  bankAccountNumber?: string;
  bankName?: string;
  bankIfsc?: string;
  taxRegime?: 'old' | 'new';
  panNumber?: string;
  payrollType?: 'employee' | 'contractor';
  isPfApplicable?: boolean;
  isEsiApplicable?: boolean;
  isPtApplicable?: boolean;
  optedForRestrictedPf?: boolean;
  dateOfJoining?: string;
  // Compliance Fields
  residentialStatus?: 'resident' | 'non_resident' | 'rnor';
  dateOfBirth?: string;
  taxRegimeEffectiveFrom?: string;
  workState?: string;
}

export interface UpdateEmployeePayrollInfoDto {
  uan?: string;
  pfAccountNumber?: string;
  esiNumber?: string;
  bankAccountNumber?: string;
  bankName?: string;
  bankIfsc?: string;
  taxRegime?: 'old' | 'new';
  panNumber?: string;
  payrollType?: 'employee' | 'contractor';
  isPfApplicable?: boolean;
  isEsiApplicable?: boolean;
  isPtApplicable?: boolean;
  optedForRestrictedPf?: boolean;
  dateOfJoining?: string;
  dateOfLeaving?: string;
  isActive?: boolean;
  // Compliance Fields
  residentialStatus?: 'resident' | 'non_resident' | 'rnor';
  dateOfBirth?: string;
  taxRegimeEffectiveFrom?: string;
  workState?: string;
}

// ==================== PF Calculation Types ====================
export type PfCalculationMode = 'ceiling_based' | 'actual_wage' | 'restricted_pf';
export type PfTrustType = 'epfo' | 'private_trust';

// ==================== Statutory Config ====================
export interface CompanyStatutoryConfig {
  id: string;
  companyId: string;
  // PF Configuration
  pfEnabled: boolean;
  pfEstablishmentCode?: string;
  pfEmployeeRate: number;
  pfEmployerRate: number;
  pfWageCeiling: number;
  pfIncludeSpecialAllowance: boolean;
  // PF Calculation Mode (new fields)
  pfCalculationMode: PfCalculationMode;
  pfTrustType: PfTrustType;
  pfTrustName?: string;
  pfTrustRegistrationNumber?: string;
  restrictedPfMaxWage: number;
  // ESI Configuration
  esiEnabled: boolean;
  esiCode?: string;
  esiEmployeeRate: number;
  esiEmployerRate: number;
  esiWageCeiling: number;
  // PT Configuration
  ptEnabled: boolean;
  ptState?: string;
  ptRegistrationNumber?: string;
  // LWF Configuration
  lwfEnabled: boolean;
  lwfEmployeeAmount: number;
  lwfEmployerAmount: number;
  // Gratuity Configuration
  gratuityEnabled: boolean;
  gratuityRate: number;
  // Status
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  companyName?: string;
}

export interface CreateCompanyStatutoryConfigDto {
  companyId: string;
  // PF Configuration
  pfEnabled?: boolean;
  pfEstablishmentCode?: string;
  pfEmployeeRate?: number;
  pfEmployerRate?: number;
  pfWageCeiling?: number;
  pfIncludeSpecialAllowance?: boolean;
  // PF Calculation Mode
  pfCalculationMode?: PfCalculationMode;
  pfTrustType?: PfTrustType;
  pfTrustName?: string;
  pfTrustRegistrationNumber?: string;
  restrictedPfMaxWage?: number;
  // ESI Configuration
  esiEnabled?: boolean;
  esiCode?: string;
  esiEmployeeRate?: number;
  esiEmployerRate?: number;
  esiWageCeiling?: number;
  // PT Configuration
  ptEnabled?: boolean;
  ptState?: string;
  ptRegistrationNumber?: string;
  // LWF Configuration
  lwfEnabled?: boolean;
  lwfEmployeeAmount?: number;
  lwfEmployerAmount?: number;
  // Gratuity Configuration
  gratuityEnabled?: boolean;
  gratuityRate?: number;
}

export interface UpdateCompanyStatutoryConfigDto {
  // PF Configuration
  pfEnabled?: boolean;
  pfEstablishmentCode?: string;
  pfEmployeeRate?: number;
  pfEmployerRate?: number;
  pfWageCeiling?: number;
  pfIncludeSpecialAllowance?: boolean;
  // PF Calculation Mode
  pfCalculationMode?: PfCalculationMode;
  pfTrustType?: PfTrustType;
  pfTrustName?: string;
  pfTrustRegistrationNumber?: string;
  restrictedPfMaxWage?: number;
  // ESI Configuration
  esiEnabled?: boolean;
  esiCode?: string;
  esiEmployeeRate?: number;
  esiEmployerRate?: number;
  esiWageCeiling?: number;
  // PT Configuration
  ptEnabled?: boolean;
  ptState?: string;
  ptRegistrationNumber?: string;
  // LWF Configuration
  lwfEnabled?: boolean;
  lwfEmployeeAmount?: number;
  lwfEmployerAmount?: number;
  // Gratuity Configuration
  gratuityEnabled?: boolean;
  gratuityRate?: number;
  isActive?: boolean;
}

// ==================== Tax Configuration ====================
export interface TaxSlab {
  id: string;
  regime: 'old' | 'new';
  financialYear: string;
  minIncome: number;
  maxIncome?: number;
  rate: number;
  cessRate: number;
  surchargeThreshold?: number;
  surchargeRate?: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface ProfessionalTaxSlab {
  id: string;
  state: string;
  minMonthlyIncome: number;
  maxMonthlyIncome?: number | null;
  monthlyTax: number;
  februaryTax?: number | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateProfessionalTaxSlabDto {
  state: string;
  minMonthlyIncome: number;
  maxMonthlyIncome?: number | null;
  monthlyTax: number;
  februaryTax?: number | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  isActive?: boolean;
}

export interface UpdateProfessionalTaxSlabDto {
  state: string;
  minMonthlyIncome: number;
  maxMonthlyIncome?: number | null;
  monthlyTax: number;
  februaryTax?: number | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  isActive: boolean;
}

// List of Indian states for PT slab configuration
export const INDIAN_STATES = [
  'Andhra Pradesh',
  'Arunachal Pradesh',
  'Assam',
  'Bihar',
  'Chhattisgarh',
  'Delhi',
  'Goa',
  'Gujarat',
  'Haryana',
  'Himachal Pradesh',
  'Jharkhand',
  'Karnataka',
  'Kerala',
  'Madhya Pradesh',
  'Maharashtra',
  'Manipur',
  'Meghalaya',
  'Mizoram',
  'Nagaland',
  'Odisha',
  'Punjab',
  'Rajasthan',
  'Sikkim',
  'Tamil Nadu',
  'Telangana',
  'Tripura',
  'Uttar Pradesh',
  'Uttarakhand',
  'West Bengal',
] as const;

// States that do NOT levy Professional Tax
export const NO_PT_STATES = [
  'Delhi',
  'Haryana',
  'Himachal Pradesh',
  'Jammu and Kashmir',
  'Punjab',
  'Rajasthan',
  'Uttar Pradesh',
  'Uttarakhand',
] as const;

// ==================== Payslip ====================
export interface Payslip {
  transactionId: string;
  employeeId: string;
  employeeName: string;
  employeeCode: string;
  companyName: string;
  monthYear: string;
  bankAccount?: string;
  bankName?: string;
  bankIfsc?: string;
  uan?: string;
  panNumber?: string;
  esiNumber?: string;
  workingDays: number;
  presentDays: number;
  lopDays: number;
  earnings: PayslipLineItem[];
  totalEarnings: number;
  deductions: PayslipLineItem[];
  totalDeductions: number;
  netPay: number;
  netPayInWords: string;
  ytdGross: number;
  ytdPf: number;
  ytdPt: number;
  ytdTds: number;
}

export interface PayslipLineItem {
  description: string;
  amount: number;
}

// ==================== Tax Parameters ====================
export interface TaxParameter {
  id: string;
  financialYear: string;
  regime: 'old' | 'new' | 'both';
  parameterCode: string;
  parameterName: string;
  parameterValue: number;
  parameterType: 'amount' | 'percentage' | 'threshold';
  description?: string;
  legalReference?: string;
  effectiveFrom: string;
  effectiveTo?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  formattedValue?: string;
}

export interface CreateTaxParameterDto {
  financialYear: string;
  regime: 'old' | 'new' | 'both';
  parameterCode: string;
  parameterName: string;
  parameterValue: number;
  parameterType: 'amount' | 'percentage' | 'threshold';
  description?: string;
  legalReference?: string;
  effectiveFrom: string;
  createdBy?: string;
}

export interface UpdateTaxParameterDto {
  parameterName?: string;
  parameterValue?: number;
  description?: string;
  legalReference?: string;
  effectiveTo?: string;
  isActive?: boolean;
  updatedBy?: string;
}

export interface TaxParametersLookup {
  financialYear: string;
  regime: string;
  parameters: Record<string, number>;
}

// ==================== Salary Components ====================
export interface SalaryComponent {
  id: string;
  companyId?: string;
  componentCode: string;
  componentName: string;
  componentType: 'earning' | 'deduction' | 'employer_contribution';

  // Wage base flags
  isPfWage: boolean;
  isEsiWage: boolean;
  isTaxable: boolean;
  isPtWage: boolean;

  // Proration
  applyProration: boolean;
  prorationBasis: 'calendar_days' | 'working_days' | 'fixed';

  // Display
  displayOrder: number;
  showOnPayslip: boolean;
  payslipGroup?: string;

  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  wageBasesDisplay?: string;
}

export interface CreateSalaryComponentDto {
  companyId?: string;
  componentCode: string;
  componentName: string;
  componentType?: 'earning' | 'deduction' | 'employer_contribution';

  isPfWage?: boolean;
  isEsiWage?: boolean;
  isTaxable?: boolean;
  isPtWage?: boolean;

  applyProration?: boolean;
  prorationBasis?: 'calendar_days' | 'working_days' | 'fixed';

  displayOrder?: number;
  showOnPayslip?: boolean;
  payslipGroup?: string;
  createdBy?: string;
}

export interface UpdateSalaryComponentDto {
  componentName?: string;
  componentType?: 'earning' | 'deduction' | 'employer_contribution';

  isPfWage?: boolean;
  isEsiWage?: boolean;
  isTaxable?: boolean;
  isPtWage?: boolean;

  applyProration?: boolean;
  prorationBasis?: 'calendar_days' | 'working_days' | 'fixed';

  displayOrder?: number;
  showOnPayslip?: boolean;
  payslipGroup?: string;

  isActive?: boolean;
  updatedBy?: string;
}

export interface SalaryComponentWageFlags {
  componentCode: string;
  componentName: string;
  isPfWage: boolean;
  isEsiWage: boolean;
  isTaxable: boolean;
  isPtWage: boolean;
  applyProration: boolean;
}

// ==================== Payroll Calculation Lines ====================
export interface PayrollCalculationLine {
  id: string;
  transactionId: string;
  lineType: 'earning' | 'deduction' | 'employer_contribution' | 'statutory';
  lineSequence: number;

  ruleCode: string;
  description: string;

  baseAmount?: number;
  rate?: number;
  computedAmount: number;

  configVersion?: string;
  configSnapshot?: string;
  notes?: string;
  createdAt?: string;

  calculationDisplay?: string;
}

export interface CreatePayrollCalculationLineDto {
  transactionId: string;
  lineType: 'earning' | 'deduction' | 'employer_contribution' | 'statutory';
  lineSequence?: number;
  ruleCode: string;
  description: string;
  baseAmount?: number;
  rate?: number;
  computedAmount: number;
  configVersion?: string;
  configSnapshot?: string;
  notes?: string;
}

export interface PayrollCalculationSummary {
  transactionId: string;
  employeeName: string;
  monthYear: string;

  earnings: PayrollCalculationLine[];
  deductions: PayrollCalculationLine[];
  employerContributions: PayrollCalculationLine[];

  totalEarnings: number;
  totalDeductions: number;
  totalEmployerContributions: number;
  netPayable: number;
}

export interface CalculationBreakdownGroup {
  groupName: string;
  groupType: string;
  items: CalculationLineItem[];
  groupTotal: number;
}

export interface CalculationLineItem {
  ruleCode: string;
  description: string;
  baseAmount?: number;
  rate?: number;
  amount: number;
  notes?: string;
}

// ==================== Calculation Rules ====================
export interface CalculationRule {
  id: string;
  companyId: string;
  name: string;
  description?: string;
  componentType: 'earning' | 'deduction' | 'employer_contribution';
  componentCode: string;
  componentName?: string;
  ruleType: 'percentage' | 'fixed' | 'slab' | 'formula';
  formulaConfig: string; // JSON string
  priority: number;
  effectiveFrom: string;
  effectiveTo?: string;
  isActive: boolean;
  isSystem: boolean;
  isTaxable: boolean;
  affectsPfWage: boolean;
  affectsEsiWage: boolean;
  createdAt?: string;
  updatedAt?: string;
  companyName?: string;
  conditions: CalculationRuleCondition[];
}

export interface CalculationRuleCondition {
  id: string;
  ruleId: string;
  conditionGroup: number;
  field: string;
  operator: string;
  value: string; // JSON string
}

export interface CreateCalculationRuleDto {
  companyId: string;
  name: string;
  description?: string;
  componentType: 'earning' | 'deduction' | 'employer_contribution';
  componentCode: string;
  componentName?: string;
  ruleType: 'percentage' | 'fixed' | 'slab' | 'formula';
  formulaConfig: string;
  priority?: number;
  effectiveFrom?: string;
  effectiveTo?: string;
  isTaxable?: boolean;
  affectsPfWage?: boolean;
  affectsEsiWage?: boolean;
  conditions?: CreateCalculationRuleConditionDto[];
}

export interface UpdateCalculationRuleDto {
  name?: string;
  description?: string;
  componentType?: 'earning' | 'deduction' | 'employer_contribution';
  componentCode?: string;
  componentName?: string;
  ruleType?: 'percentage' | 'fixed' | 'slab' | 'formula';
  formulaConfig?: string;
  priority?: number;
  effectiveFrom?: string;
  effectiveTo?: string;
  isActive?: boolean;
  isTaxable?: boolean;
  affectsPfWage?: boolean;
  affectsEsiWage?: boolean;
  conditions?: CreateCalculationRuleConditionDto[];
}

export interface CreateCalculationRuleConditionDto {
  conditionGroup?: number;
  field: string;
  operator: string;
  value: string;
}

export interface FormulaVariable {
  id: string;
  code: string;
  displayName: string;
  description?: string;
  dataType: string;
  source: string;
  isSystem: boolean;
}

export interface CalculationRuleTemplate {
  id: string;
  name: string;
  description?: string;
  category: string;
  componentType: string;
  componentCode: string;
  ruleType: string;
  formulaConfig: string;
  defaultConditions?: string;
  displayOrder: number;
}

export interface FormulaValidationResult {
  isValid: boolean;
  errorMessage?: string;
  sampleResult?: number;
  usedVariables: string[];
  unknownVariables: string[];
}

export interface RuleCalculationPreview {
  success: boolean;
  errorMessage?: string;
  result: number;
  inputValues: Record<string, number>;
  steps: CalculationStep[];
}

export interface CalculationStep {
  description: string;
  expression: string;
  value: number;
}





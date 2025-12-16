// Backend entity types matching the .NET API
export interface Company {
  id: string;
  name: string;
  logoUrl?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  email?: string;
  phone?: string;
  website?: string;
  taxNumber?: string;
  paymentInstructions?: string;
  invoiceTemplateId?: string;
  signatureType?: string;
  signatureData?: string;
  signatureName?: string;
  signatureFont?: string;
  signatureColor?: string;
  createdAt?: string;
  updatedAt?: string;
  // Statutory identifiers for Indian compliance
  tanNumber?: string;            // Tax Account Number (for TDS)
  pfRegistrationNumber?: string; // PF Establishment Code
  esiRegistrationNumber?: string; // ESI Code
}

export interface Customer {
  id: string;
  companyId?: string;
  name: string;
  companyName?: string;
  email?: string;
  phone?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  taxNumber?: string;
  notes?: string;
  creditLimit?: number;
  paymentTerms?: number;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface Product {
  id: string;
  companyId?: string;
  name: string;
  description?: string;
  sku?: string;
  category?: string;
  type?: string;
  unitPrice: number;
  unit?: string;
  taxRate?: number;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface Invoice {
  id: string;
  companyId?: string;
  customerId?: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  status?: string;
  subtotal: number;
  taxAmount?: number;
  discountAmount?: number;
  totalAmount: number;
  paidAmount?: number;
  currency?: string;
  notes?: string;
  terms?: string;
  paymentInstructions?: string;
  poNumber?: string;
  projectName?: string;
  sentAt?: string;
  viewedAt?: string;
  paidAt?: string;
  createdAt?: string;
  updatedAt?: string;
  items?: InvoiceItem[]; // Client-side: Invoice items embedded for TanStack DB
}

export interface Quote {
  id: string;
  companyId?: string;
  customerId?: string;
  quoteNumber: string;
  quoteDate: string;
  expiryDate: string;
  status?: string;
  subtotal: number;
  discountType?: string;
  discountValue?: number;
  discountAmount?: number;
  taxAmount?: number;
  totalAmount: number;
  currency?: string;
  notes?: string;
  terms?: string;
  paymentInstructions?: string;
  poNumber?: string;
  projectName?: string;
  sentAt?: string;
  viewedAt?: string;
  acceptedAt?: string;
  rejectedAt?: string;
  rejectedReason?: string;
  createdAt?: string;
  updatedAt?: string;
  items?: QuoteItem[]; // Client-side: Quote items embedded for TanStack DB
}

export interface QuoteItem {
  id: string;
  quoteId?: string;
  productId?: string;
  description: string;
  quantity: number;
  unitPrice: number;
  taxRate?: number;
  discountRate?: number;
  lineTotal: number;
  sortOrder?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface InvoiceItem {
  id: string;
  invoiceId?: string;
  productId?: string;
  description: string;
  quantity: number;
  unitPrice: number;
  taxRate?: number;
  discountRate?: number;
  lineTotal: number;
  sortOrder?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface TaxRate {
  id: string;
  companyId?: string;
  name: string;
  rate: number;
  isDefault?: boolean;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface InvoiceTemplate {
  id: string;
  companyId?: string;
  name: string;
  templateData: string;
  templateKey?: string;
  previewUrl?: string;
  configSchema?: any;
  isDefault?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

// DTO types for create/update operations
export interface CreateCompanyDto {
  name: string;
  logoUrl?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  email?: string;
  phone?: string;
  website?: string;
  taxNumber?: string;
  paymentInstructions?: string;
  signatureType?: string;
  signatureData?: string;
  signatureName?: string;
  signatureFont?: string;
  signatureColor?: string;
}

export interface UpdateCompanyDto extends CreateCompanyDto {}

export interface CreateCustomerDto {
  companyId?: string;
  name: string;
  companyName?: string;
  email?: string;
  phone?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  taxNumber?: string;
  notes?: string;
  creditLimit?: number;
  paymentTerms?: number;
  isActive?: boolean;
}

export interface UpdateCustomerDto extends CreateCustomerDto {}

export interface CreateProductDto {
  companyId?: string;
  name: string;
  description?: string;
  sku?: string;
  category?: string;
  type?: string;
  unitPrice: number;
  unit?: string;
  taxRate?: number;
  isActive?: boolean;
}

export interface UpdateProductDto extends CreateProductDto {}

export interface CreateInvoiceDto {
  companyId?: string;
  customerId?: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  status?: string;
  subtotal: number;
  taxAmount?: number;
  discountAmount?: number;
  totalAmount: number;
  paidAmount?: number;
  currency?: string;
  notes?: string;
  terms?: string;
  paymentInstructions?: string;
  poNumber?: string;
  projectName?: string;
}

export interface UpdateInvoiceDto extends CreateInvoiceDto {
  id: string;
}

export interface CreateQuoteDto {
  companyId?: string;
  customerId?: string;
  quoteNumber: string;
  quoteDate: string;
  expiryDate: string;
  status?: string;
  subtotal: number;
  discountType?: string;
  discountValue?: number;
  discountAmount?: number;
  taxAmount?: number;
  totalAmount: number;
  currency?: string;
  notes?: string;
  terms?: string;
  paymentInstructions?: string;
  poNumber?: string;
  projectName?: string;
}

export interface UpdateQuoteDto extends CreateQuoteDto {
  id: string;
}

export interface CreateTaxRateDto {
  companyId?: string;
  name: string;
  rate: number;
  isDefault?: boolean;
  isActive?: boolean;
}

export interface UpdateTaxRateDto extends CreateTaxRateDto {}

export interface CreateInvoiceTemplateDto {
  companyId?: string;
  name: string;
  templateData: string;
  isDefault?: boolean;
}

export interface UpdateInvoiceTemplateDto extends CreateInvoiceTemplateDto {}

// Dashboard-specific types
export interface DashboardStats {
  totalRevenue: number;
  outstandingAmount: number;
  thisMonthAmount: number;
  overdueAmount: number;
  outstandingCount: number;
  thisMonthCount: number;
  overdueCount: number;
}

export interface RecentInvoice {
  id: string;
  invoiceNumber: string;
  customerName: string;
  totalAmount: number;
  status: string;
  invoiceDate: string;
  dueDate: string;
  daysOverdue?: number;
}

export interface DashboardData {
  stats: DashboardStats;
  recentInvoices: RecentInvoice[];
}

// API response types
export interface ApiError {
  type: 'Validation' | 'NotFound' | 'Conflict' | 'Internal';
  message: string;
  details?: string[];
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// Filter and pagination parameters
export interface PaginationParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface EmployeesFilterParams extends PaginationParams {
  employeeName?: string;
  employeeId?: string;
  department?: string;
  designation?: string;
  status?: string;
  city?: string;
  state?: string;
  country?: string;
  contractType?: string;
  company?: string; // Legacy field, kept for backward compatibility
  companyId?: string; // Preferred field - UUID of company
}

// Salary transactions filters for server paging
export interface SalaryTransactionsFilterParams extends PaginationParams {
  employeeId?: string;
  salaryMonth?: number;
  salaryYear?: number;
  status?: string;
  transactionType?: string; // salary, consulting, bonus, reimbursement, gift
  contractType?: string; // Contract, Fulltime, etc.
  department?: string;
  paymentMethod?: string;
  paymentDateFrom?: string;
  paymentDateTo?: string;
  company?: string; // Legacy field - company name (string)
  companyId?: string; // Preferred field - UUID of company
}

// Employee Management Types
export interface Employee {
  id: string;
  employeeName: string;
  email?: string;
  phone?: string;
  employeeId?: string;
  department?: string;
  designation?: string;
  hireDate?: string;
  status: string;
  bankAccountNumber?: string;
  bankName?: string;
  ifscCode?: string;
  panNumber?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country: string;
  contractType?: string;
  company?: string; // Legacy field, kept for backward compatibility
  companyId?: string; // Foreign key to companies table
  createdAt?: string;
  updatedAt?: string;
  // Statutory identifiers for Indian compliance (from payroll info)
  uan?: string;             // Universal Account Number for PF
  pfAccountNumber?: string; // PF Account Number
  esiNumber?: string;       // ESI IP Number
}

export interface CreateEmployeeDto {
  employeeName: string;
  email?: string;
  phone?: string;
  employeeId?: string;
  department?: string;
  designation?: string;
  hireDate?: string;
  status?: string;
  bankAccountNumber?: string;
  bankName?: string;
  ifscCode?: string;
  panNumber?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  contractType?: string;
  company?: string;
  companyId?: string;
}

export interface UpdateEmployeeDto extends CreateEmployeeDto {}

export interface EmployeeSalaryTransaction {
  id: string;
  employeeId: string;
  companyId?: string;
  salaryMonth: number;
  salaryYear: number;
  basicSalary: number;
  hra: number;
  conveyance: number;
  medicalAllowance: number;
  specialAllowance: number;
  lta: number;
  otherAllowances: number;
  grossSalary: number;
  pfEmployee: number;
  pfEmployer: number;
  pt: number;
  incomeTax: number;
  otherDeductions: number;
  netSalary: number;
  paymentDate?: string;
  paymentMethod: string;
  paymentReference?: string;
  status: string;
  remarks?: string;
  currency: string;
  transactionType?: string; // salary, consulting, bonus, reimbursement, gift
  createdAt?: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
  employee?: Employee; // For joined queries
}

export interface CreateEmployeeSalaryTransactionDto {
  employeeId: string;
  companyId?: string;
  salaryMonth: number;
  salaryYear: number;
  basicSalary: number;
  hra: number;
  conveyance: number;
  medicalAllowance: number;
  specialAllowance: number;
  lta: number;
  otherAllowances: number;
  grossSalary: number;
  pfEmployee: number;
  pfEmployer: number;
  pt: number;
  incomeTax: number;
  otherDeductions: number;
  netSalary: number;
  paymentDate?: string;
  paymentMethod?: string;
  paymentReference?: string;
  status?: string;
  remarks?: string;
  currency?: string;
  transactionType?: string; // salary, consulting, bonus, reimbursement, gift
  createdBy?: string;
}

export interface UpdateEmployeeSalaryTransactionDto {
  basicSalary: number;
  hra: number;
  conveyance: number;
  medicalAllowance: number;
  specialAllowance: number;
  lta: number;
  otherAllowances: number;
  grossSalary: number;
  pfEmployee: number;
  pfEmployer: number;
  pt: number;
  incomeTax: number;
  otherDeductions: number;
  netSalary: number;
  paymentDate?: string;
  paymentMethod?: string;
  paymentReference?: string;
  status?: string;
  remarks?: string;
  currency?: string;
  transactionType?: string; // salary, consulting, bonus, reimbursement, gift
  updatedBy?: string;
}

export interface BulkEmployeeSalaryTransactionsDto {
  salaryTransactions: CreateEmployeeSalaryTransactionDto[];
  skipValidationErrors?: boolean;
  overwriteExisting?: boolean;
  createdBy?: string;
}

export interface CopySalaryTransactionsDto {
  sourceMonth: number;
  sourceYear: number;
  targetMonth: number;
  targetYear: number;
  companyId?: string;
  duplicateHandling?: 'skip' | 'overwrite' | 'skip_and_report';
  resetPaymentInfo?: boolean;
  createdBy?: string;
}

export interface BulkEmployeesDto {
  employees: CreateEmployeeDto[];
  skipValidationErrors?: boolean;
  createdBy?: string;
}

export interface BulkUploadResult {
  successCount: number;
  failureCount: number;
  totalCount: number;
  errors: BulkUploadError[];
  createdIds: string[];
}

export interface BulkUploadError {
  rowNumber: number;
  employeeReference?: string;
  assetReference?: string;
  errorMessage: string;
  fieldName?: string;
}

export interface BulkAssetsDto {
  assets: CreateAssetDto[];
  skipValidationErrors?: boolean;
  createdBy?: string;
}

// Asset management
export interface Asset {
  id: string;
  companyId: string;
  categoryId?: string;
  modelId?: string;
  assetType: string;
  status: string;
  assetTag: string;
  serialNumber?: string;
  name: string;
  description?: string;
  location?: string;
  vendor?: string;
  purchaseType?: string;
  invoiceReference?: string;
  purchaseDate?: string;
  inServiceDate?: string;
  depreciationStartDate?: string;
  warrantyExpiration?: string;
  purchaseCost?: number;
  currency?: string;
  depreciationMethod?: string;
  usefulLifeMonths?: number;
  salvageValue?: number;
  residualBookValue?: number;
  customProperties?: any;
  notes?: string;
  // Loan-related fields
  linkedLoanId?: string;
  downPaymentAmount?: number;
  gstAmount?: number;
  gstRate?: number;
  itcEligible?: boolean;
  tdsOnInterest?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface AssetAssignment {
  id: string;
  assetId: string;
  targetType: 'employee' | 'company';
  companyId: string;
  employeeId?: string;
  assignedOn: string;
  returnedOn?: string;
  conditionOut?: string;
  conditionIn?: string;
  isActive: boolean;
  notes?: string;
}

export interface CreateAssetDto {
  companyId: string;
  categoryId?: string;
  modelId?: string;
  assetType: string;
  status?: string;
  assetTag: string;
  name: string;
  serialNumber?: string;
  description?: string;
  location?: string;
  vendor?: string;
  purchaseType?: string;
  invoiceReference?: string;
  purchaseDate?: string;
  inServiceDate?: string;
  depreciationStartDate?: string;
  warrantyExpiration?: string;
  purchaseCost?: number;
  currency?: string;
  depreciationMethod?: string;
  usefulLifeMonths?: number;
  salvageValue?: number;
  residualBookValue?: number;
  customProperties?: any;
  notes?: string;
  // Loan-related fields
  linkedLoanId?: string;
  downPaymentAmount?: number;
  gstAmount?: number;
  gstRate?: number;
  itcEligible?: boolean;
  tdsOnInterest?: number;
}

export interface UpdateAssetDto extends Partial<CreateAssetDto> {}

export interface CreateAssetAssignmentDto {
  targetType: 'employee' | 'company';
  companyId: string;
  employeeId?: string;
  assignedOn?: string;
  conditionOut?: string;
  notes?: string;
}

export interface ReturnAssetAssignmentDto {
  returnedOn?: string;
  conditionIn?: string;
}

export interface AssetMaintenance {
  id: string;
  assetId: string;
  title: string;
  description?: string;
  status: string;
  openedAt: string;
  closedAt?: string;
  vendor?: string;
  cost?: number;
  currency?: string;
  dueDate?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateAssetMaintenanceDto {
  title: string;
  description?: string;
  status?: string;
  openedAt?: string;
  dueDate?: string;
  vendor?: string;
  cost?: number;
  currency?: string;
  notes?: string;
}

export interface UpdateAssetMaintenanceDto extends Partial<CreateAssetMaintenanceDto> {
  closedAt?: string;
}

export interface AssetDocument {
  id: string;
  assetId: string;
  name: string;
  url: string;
  contentType?: string;
  uploadedAt?: string;
  notes?: string;
}

export interface CreateAssetDocumentDto {
  name: string;
  url: string;
  contentType?: string;
  notes?: string;
}

export interface AssetDisposal {
  id: string;
  assetId: string;
  disposedOn: string;
  method: string;
  proceeds?: number;
  disposalCost?: number;
  currency?: string;
  buyer?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateAssetDisposalDto {
  disposedOn?: string;
  method?: string;
  proceeds?: number;
  disposalCost?: number;
  currency?: string;
  buyer?: string;
  notes?: string;
}

export interface AssetCostSummary {
  assetId: string;
  purchaseType: string;
  currency?: string;
  purchaseCost: number;
  maintenanceCost: number;
  depreciationBase: number;
  accumulatedDepreciation: number;
  monthlyDepreciation: number;
  netBookValue: number;
  salvageValue: number;
  depreciationMethod: string;
  usefulLifeMonths?: number;
  depreciationStartDate?: string;
  ageMonths: number;
  remainingLifeMonths: number;
  disposalProceeds: number;
  disposalCost: number;
  disposalGainLoss: number;
}

export interface AssetCostReportRow {
  companyId?: string;
  categoryId?: string;
  purchaseType?: string;
  assetCount?: number;
  purchaseCost: number;
  maintenanceCost: number;
  accumulatedDepreciation: number;
  netBookValue: number;
}

export interface AssetAgingBucket {
  label: string;
  assetCount: number;
  purchaseCost: number;
  netBookValue: number;
}

export interface AssetMaintenanceSpend {
  assetId: string;
  companyId: string;
  assetTag: string;
  assetName: string;
  status: string;
  maintenanceCost: number;
}

export interface AssetCostReport {
  totalPurchaseCost: number;
  totalMaintenanceCost: number;
  totalAccumulatedDepreciation: number;
  totalNetBookValue: number;
  totalCapexPurchase: number;
  totalOpexSpend: number;
  totalDisposalProceeds: number;
  totalDisposalCosts: number;
  totalDisposalGainLoss: number;
  averageAgeMonths: number;
  byCompany: AssetCostReportRow[];
  byCategory: AssetCostReportRow[];
  byPurchaseType: AssetCostReportRow[];
  agingBuckets: AssetAgingBucket[];
  topMaintenanceSpend: AssetMaintenanceSpend[];
}

// Subscriptions
export interface Subscription {
  id: string;
  companyId: string;
  name: string;
  vendor?: string;
  planName?: string;
  category?: string;
  status: string;
  startDate?: string;
  renewalDate?: string;
  renewalPeriod?: string;
  seatsTotal?: number;
  seatsUsed?: number;
  licenseKey?: string;
  costPerPeriod?: number;
  costPerSeat?: number;
  currency?: string;
  billingCycleStart?: string;
  billingCycleEnd?: string;
  autoRenew?: boolean;
  url?: string;
  notes?: string;
  pausedOn?: string;
  resumedOn?: string;
  cancelledOn?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface SubscriptionAssignment {
  id: string;
  subscriptionId: string;
  targetType: 'employee' | 'company';
  companyId: string;
  employeeId?: string;
  seatIdentifier?: string;
  role?: string;
  assignedOn: string;
  revokedOn?: string;
  notes?: string;
}

export interface CreateSubscriptionDto {
  companyId: string;
  name: string;
  vendor?: string;
  planName?: string;
  category?: string;
  status?: string;
  startDate?: string;
  renewalDate?: string;
  renewalPeriod?: string;
  seatsTotal?: number;
  seatsUsed?: number;
  licenseKey?: string;
  costPerPeriod?: number;
  costPerSeat?: number;
  currency?: string;
  billingCycleStart?: string;
  billingCycleEnd?: string;
  autoRenew?: boolean;
  url?: string;
  notes?: string;
}

export interface UpdateSubscriptionDto extends Partial<CreateSubscriptionDto> {}

export interface CreateSubscriptionAssignmentDto {
  targetType: 'employee' | 'company';
  companyId: string;
  employeeId?: string;
  seatIdentifier?: string;
  role?: string;
  assignedOn?: string;
  notes?: string;
}

export interface RevokeSubscriptionAssignmentDto {
  revokedOn?: string;
  notes?: string;
}

export interface SubscriptionMonthlyExpense {
  year: number;
  month: number;
  totalCost: number;
  currency: string;
  totalCostInInr: number;
  activeSubscriptionCount: number;
}

export interface SubscriptionCostReport {
  totalMonthlyCost: number;
  totalYearlyCost: number;
  totalCostInInr: number;
  activeSubscriptionCount: number;
  totalSubscriptionCount: number;
  monthlyExpenses: SubscriptionMonthlyExpense[];
}

// Loans
export interface Loan {
  id: string;
  companyId: string;
  loanName: string;
  lenderName: string;
  loanType: 'secured' | 'unsecured' | 'asset_financing';
  assetId?: string;
  principalAmount: number;
  interestRate: number;
  loanStartDate: string;
  loanEndDate?: string;
  tenureMonths: number;
  emiAmount: number;
  outstandingPrincipal: number;
  interestType: 'fixed' | 'floating' | 'reducing';
  compoundingFrequency: 'monthly' | 'quarterly' | 'annually';
  status: 'active' | 'closed' | 'foreclosed' | 'defaulted';
  loanAccountNumber?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface LoanEmiSchedule {
  id: string;
  loanId: string;
  emiNumber: number;
  dueDate: string;
  principalAmount: number;
  interestAmount: number;
  totalEmi: number;
  outstandingPrincipalAfter: number;
  status: 'pending' | 'paid' | 'overdue' | 'skipped';
  paidDate?: string;
  paymentVoucherId?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface LoanTransaction {
  id: string;
  loanId: string;
  transactionType: 'disbursement' | 'emi_payment' | 'prepayment' | 'foreclosure' | 'interest_accrual' | 'interest_capitalization';
  transactionDate: string;
  amount: number;
  principalAmount: number;
  interestAmount: number;
  description?: string;
  paymentMethod?: 'bank_transfer' | 'cheque' | 'cash' | 'online' | 'other';
  bankAccountId?: string;
  voucherReference?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateLoanDto {
  companyId: string;
  loanName: string;
  lenderName: string;
  loanType?: 'secured' | 'unsecured' | 'asset_financing';
  assetId?: string;
  principalAmount: number;
  interestRate: number;
  loanStartDate: string;
  loanEndDate?: string;
  tenureMonths: number;
  interestType?: 'fixed' | 'floating' | 'reducing';
  compoundingFrequency?: 'monthly' | 'quarterly' | 'annually';
  loanAccountNumber?: string;
  notes?: string;
}

export interface UpdateLoanDto extends Partial<CreateLoanDto> {
  emiAmount?: number;
  status?: 'active' | 'closed' | 'foreclosed' | 'defaulted';
}

export interface LoanScheduleDto {
  loanId: string;
  loanName: string;
  principalAmount: number;
  interestRate: number;
  tenureMonths: number;
  emiAmount: number;
  scheduleItems: LoanEmiScheduleItemDto[];
}

export interface LoanEmiScheduleItemDto {
  id: string;
  emiNumber: number;
  dueDate: string;
  principalAmount: number;
  interestAmount: number;
  totalEmi: number;
  outstandingPrincipalAfter: number;
  status: string;
  paidDate?: string;
}

export interface CreateEmiPaymentDto {
  paymentDate: string;
  amount: number;
  principalAmount: number;
  interestAmount: number;
  paymentMethod?: 'bank_transfer' | 'cheque' | 'cash' | 'online' | 'other';
  bankAccountId?: string;
  voucherReference?: string;
  notes?: string;
  emiNumber?: number;
}

export interface PrepaymentDto {
  prepaymentDate: string;
  amount: number;
  paymentMethod?: 'bank_transfer' | 'cheque' | 'cash' | 'online' | 'other';
  bankAccountId?: string;
  voucherReference?: string;
  notes?: string;
  reduceEmi?: boolean;
}

// Payment types - Enhanced for Indian tax compliance
export interface Payment {
  id: string;
  // Linking
  invoiceId?: string;
  companyId?: string;
  customerId?: string;
  // Payment details
  paymentDate: string;
  amount: number;
  amountInInr?: number;
  currency?: string;
  paymentMethod?: string;
  referenceNumber?: string;
  notes?: string;
  description?: string;
  // Classification
  paymentType?: 'invoice_payment' | 'advance_received' | 'direct_income' | 'refund_received';
  incomeCategory?: 'export_services' | 'domestic_services' | 'product_sale' | 'interest' | 'other';
  // TDS tracking
  tdsApplicable?: boolean;
  tdsSection?: string;  // 194J, 194C, 194H, 194O
  tdsRate?: number;
  tdsAmount?: number;
  grossAmount?: number;
  // Financial year
  financialYear?: string;  // Format: 2024-25
  // Timestamps
  createdAt?: string;
  updatedAt?: string;
}

// Income summary for financial reports
export interface IncomeSummary {
  totalGross: number;
  totalTds: number;
  totalNet: number;
  totalInr: number;
}

// TDS summary for compliance reporting
export interface TdsSummary {
  customerName?: string;
  customerPan?: string;
  tdsSection?: string;
  paymentCount: number;
  totalGross: number;
  totalTds: number;
  totalNet: number;
}

// Bank Account types - Phase A: Bank Integration
export interface BankAccount {
  id: string;
  companyId?: string;
  accountName: string;
  accountNumber: string;
  bankName: string;
  ifscCode?: string;
  branchName?: string;
  accountType: string; // 'current', 'savings', 'cc', 'foreign'
  currency: string;
  openingBalance: number;
  currentBalance: number;
  asOfDate?: string;
  isPrimary: boolean;
  isActive: boolean;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateBankAccountDto {
  companyId?: string;
  accountName: string;
  accountNumber: string;
  bankName: string;
  ifscCode?: string;
  branchName?: string;
  accountType?: string;
  currency?: string;
  openingBalance?: number;
  currentBalance?: number;
  asOfDate?: string;
  isPrimary?: boolean;
  isActive?: boolean;
  notes?: string;
}

export interface UpdateBankAccountDto {
  companyId?: string;
  accountName?: string;
  accountNumber?: string;
  bankName?: string;
  ifscCode?: string;
  branchName?: string;
  accountType?: string;
  currency?: string;
  openingBalance?: number;
  currentBalance?: number;
  asOfDate?: string;
  isPrimary?: boolean;
  isActive?: boolean;
  notes?: string;
}

export interface UpdateBalanceDto {
  newBalance: number;
  asOfDate: string;
}

// Bank Transaction types
export interface BankTransaction {
  id: string;
  bankAccountId: string;
  transactionDate: string;
  valueDate?: string;
  description?: string;
  referenceNumber?: string;
  chequeNumber?: string;
  transactionType: 'credit' | 'debit';
  amount: number;
  balanceAfter?: number;
  category?: string;
  isReconciled: boolean;
  reconciledType?: string;
  reconciledId?: string;
  reconciledAt?: string;
  reconciledBy?: string;
  importSource: string; // 'manual', 'csv', 'pdf', 'api'
  importBatchId?: string;
  rawData?: string;
  transactionHash?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateBankTransactionDto {
  bankAccountId: string;
  transactionDate: string;
  valueDate?: string;
  description?: string;
  referenceNumber?: string;
  chequeNumber?: string;
  transactionType: 'credit' | 'debit';
  amount: number;
  balanceAfter?: number;
  category?: string;
}

export interface UpdateBankTransactionDto {
  transactionDate?: string;
  valueDate?: string;
  description?: string;
  referenceNumber?: string;
  chequeNumber?: string;
  transactionType?: 'credit' | 'debit';
  amount?: number;
  balanceAfter?: number;
  category?: string;
}

export interface ImportBankTransactionDto {
  transactionDate: string;
  valueDate?: string;
  description?: string;
  referenceNumber?: string;
  chequeNumber?: string;
  transactionType: 'credit' | 'debit';
  amount: number;
  balanceAfter?: number;
  rawData?: string;
}

export interface ImportBankTransactionsRequest {
  bankAccountId: string;
  transactions: ImportBankTransactionDto[];
  skipDuplicates?: boolean;
}

export interface ImportBankTransactionsResult {
  importedCount: number;
  skippedCount: number;
  failedCount: number;
  batchId: string;
  errors: string[];
}

export interface ReconcileTransactionDto {
  reconciledType: string; // 'payment', 'expense', 'payroll', 'tax_payment', 'transfer', 'contractor'
  reconciledId: string;
  reconciledBy?: string;
}

export interface ReconciliationSuggestion {
  paymentId: string;
  paymentDate: string;
  amount: number;
  customerName?: string;
  invoiceNumber?: string;
  referenceNumber?: string;
  matchScore: number;
  amountDifference: number;
  dateDifferenceInDays: number;
}

export interface BankTransactionSummary {
  totalCount: number;
  reconciledCount: number;
  unreconciledCount: number;
  totalCredits: number;
  totalDebits: number;
  netAmount: number;
  reconciliationPercentage: number;
}

export interface BankAccountFilterParams extends PaginationParams {
  companyId?: string;
  accountType?: string;
  currency?: string;
  bankName?: string;
  isActive?: boolean;
  isPrimary?: boolean;
}

export interface BankTransactionFilterParams extends PaginationParams {
  bankAccountId?: string;
  transactionType?: 'credit' | 'debit';
  category?: string;
  isReconciled?: boolean;
  reconciledType?: string;
  importSource?: string;
  importBatchId?: string;
  fromDate?: string;
  toDate?: string;
}

// Cash Flow Statement types (AS-3 compliant)
export interface OperatingActivitiesDetail {
  cashReceiptsFromCustomers: number;
  cashPaidToEmployees: number;
  cashPaidForSubscriptions: number;
  cashPaidForOpexAssets: number;
  cashPaidForMaintenance: number;
  tdsPayments: number;
  depreciationAddedBack: number;
  loanInterestAddedBack: number;
}

export interface InvestingActivitiesDetail {
  capexAssetPurchases: number;
  assetDisposals: number;
}

export interface FinancingActivitiesDetail {
  loanDisbursementsReceived: number;
  loanPrincipalRepayments: number;
  loanInterestPayments: number;
}

export interface CashFlowStatementDto {
  // Operating Activities
  netProfitBeforeTax: number;
  adjustmentsForNonCashItems: number;
  operatingCashBeforeWorkingCapital: number;
  changesInWorkingCapital: number;
  cashFromOperatingActivities: number;
  
  // Investing Activities
  purchaseOfFixedAssets: number;
  saleOfFixedAssets: number;
  cashFromInvestingActivities: number;
  
  // Financing Activities
  loanDisbursements: number;
  loanRepayments: number;
  cashFromFinancingActivities: number;
  
  // Net Cash Flow
  netIncreaseDecreaseInCash: number;
  openingCashBalance: number;
  closingCashBalance: number;
  
  // Detailed breakdowns
  operatingDetails: OperatingActivitiesDetail;
  investingDetails: InvestingActivitiesDetail;
  financingDetails: FinancingActivitiesDetail;
}

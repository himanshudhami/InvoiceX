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
  // GST Compliance fields
  gstin?: string;                // GSTIN (15 characters)
  gstStateCode?: string;         // GST State Code (first 2 digits of GSTIN)
  panNumber?: string;            // PAN Number (10 characters)
  cinNumber?: string;            // CIN (Corporate Identity Number)
  gstRegistrationType?: string;  // 'regular' | 'composition' | 'unregistered' | 'overseas'
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
  // GST Compliance fields
  gstin?: string;              // GSTIN (15 characters)
  gstStateCode?: string;       // GST State Code (2 digits)
  customerType?: string;       // 'b2b' | 'b2c' | 'overseas' | 'sez'
  isGstRegistered?: boolean;   // Whether customer is GST registered
  panNumber?: string;          // PAN Number (for TDS purposes)
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
  // GST Compliance fields
  hsnSacCode?: string;           // HSN code (goods) or SAC code (services)
  isService?: boolean;           // True for SAC code (services), false for HSN code (goods)
  defaultGstRate?: number;       // Default GST rate percentage (0, 5, 12, 18, 28)
  cessRate?: number;             // Cess rate percentage for specific goods
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
  // GST Classification
  invoiceType?: string;          // 'export' | 'domestic_b2b' | 'domestic_b2c' | 'sez' | 'deemed_export'
  supplyType?: string;           // 'intra_state' | 'inter_state' | 'export'
  placeOfSupply?: string;        // State code or 'export'
  reverseCharge?: boolean;       // Whether reverse charge mechanism applies
  // GST Totals
  totalCgst?: number;            // Total CGST amount
  totalSgst?: number;            // Total SGST amount
  totalIgst?: number;            // Total IGST amount
  totalCess?: number;            // Total Cess amount
  // E-invoicing fields
  eInvoiceApplicable?: boolean;  // Whether e-invoicing is applicable
  eInvoiceIrn?: string;          // Invoice Reference Number from e-invoice portal (legacy)
  eInvoiceAckNumber?: string;    // Acknowledgement number from e-invoice portal
  eInvoiceAckDate?: string;      // Acknowledgement date from e-invoice portal
  eInvoiceQrCode?: string;       // QR code data for e-invoice (legacy)
  // New E-invoice fields (IRP integration)
  irn?: string;                  // Invoice Reference Number
  irnGeneratedAt?: string;       // When IRN was generated
  irnCancelledAt?: string;       // When IRN was cancelled
  qrCodeData?: string;           // QR code base64 data from IRP
  eInvoiceSignedJson?: string;   // Signed invoice JSON from IRP
  eInvoiceStatus?: string;       // 'not_applicable' | 'pending' | 'generated' | 'cancelled' | 'error'
  // Export-specific fields
  exportType?: string;           // 'EXPWP' | 'EXPWOP' (with/without payment)
  portCode?: string;             // Port of export code
  shippingBillNumber?: string;   // Shipping bill number
  shippingBillDate?: string;     // Shipping bill date
  foreignCurrency?: string;      // Foreign currency code (USD, EUR, etc.)
  exchangeRate?: number;         // Exchange rate to INR
  countryOfDestination?: string; // Destination country code
  // SEZ-specific fields
  sezCategory?: string;          // 'SEZWP' | 'SEZWOP'
  // Shipping details
  shippingAddress?: string;      // Shipping/delivery address
  transporterName?: string;      // Name of transporter
  vehicleNumber?: string;        // Vehicle number
  ewayBillNumber?: string;       // E-way bill number
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
  // GST Compliance fields
  hsnSacCode?: string;           // HSN code (goods) or SAC code (services)
  isService?: boolean;           // True for SAC code (services), false for HSN code (goods)
  cgstRate?: number;             // Central GST rate percentage
  cgstAmount?: number;           // Central GST amount calculated
  sgstRate?: number;             // State GST rate percentage
  sgstAmount?: number;           // State GST amount calculated
  igstRate?: number;             // Integrated GST rate percentage
  igstAmount?: number;           // Integrated GST amount calculated
  cessRate?: number;             // Cess rate percentage
  cessAmount?: number;           // Cess amount calculated
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
  // GST Compliance fields
  gstin?: string;
  gstStateCode?: string;
  panNumber?: string;
  cinNumber?: string;
  gstRegistrationType?: string;
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
  // GST Compliance fields
  gstin?: string;
  gstStateCode?: string;
  customerType?: string;
  isGstRegistered?: boolean;
  panNumber?: string;
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
  // GST Compliance fields
  hsnSacCode?: string;
  isService?: boolean;
  defaultGstRate?: number;
  cessRate?: number;
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
  // GST Classification
  invoiceType?: string;
  supplyType?: string;
  placeOfSupply?: string;
  reverseCharge?: boolean;
  // GST Totals
  totalCgst?: number;
  totalSgst?: number;
  totalIgst?: number;
  totalCess?: number;
  // E-invoicing fields
  eInvoiceApplicable?: boolean;
  eInvoiceIrn?: string;
  eInvoiceAckNumber?: string;
  eInvoiceAckDate?: string;
  eInvoiceQrCode?: string;
  // Shipping details
  shippingAddress?: string;
  transporterName?: string;
  vehicleNumber?: string;
  ewayBillNumber?: string;
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

// Invoice filters for server-side paging
export interface InvoicesFilterParams extends PaginationParams {
  status?: string;
  invoiceNumber?: string;
  projectName?: string;
  currency?: string;
  companyId?: string;
  customerId?: string;
}

// Quote filters for server-side paging
export interface QuotesFilterParams extends PaginationParams {
  status?: string;
  quoteNumber?: string;
  projectName?: string;
  currency?: string;
  companyId?: string;
}

// Customer filters for server-side paging
export interface CustomersFilterParams extends PaginationParams {
  name?: string;
  companyName?: string;
  email?: string;
  city?: string;
  state?: string;
  isActive?: boolean;
  companyId?: string;
}

// Product filters for server-side paging
export interface ProductsFilterParams extends PaginationParams {
  searchTerm?: string;
  name?: string;
  category?: string;
  type?: string;
  isActive?: boolean;
  companyId?: string;
}

// Payment filters for server-side paging
export interface PaymentsFilterParams extends PaginationParams {
  invoiceId?: string;
  companyId?: string;
  customerId?: string;
  paymentType?: string;
  incomeCategory?: string;
  tdsApplicable?: boolean;
  tdsSection?: string;
  financialYear?: string;
  currency?: string;
  fromDate?: string;
  toDate?: string;
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
  // Hierarchy fields
  managerId?: string; // Foreign key to employees table (reporting manager)
  managerName?: string; // Denormalized manager name for display
  reportingLevel?: number; // Level in org hierarchy (0 = top)
  isManager?: boolean; // Whether employee has direct reports
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
  managerId?: string; // Reporting manager
}

export interface UpdateEmployeeDto extends CreateEmployeeDto {}

export interface ResignEmployeeDto {
  lastWorkingDay: string;
  resignationReason?: string;
}

export interface RejoinEmployeeDto {
  rejoiningDate?: string;
}

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
  // Computed/Joined fields
  invoiceNumber?: string;  // Populated from JOIN with invoices table
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

// ==================== TDS Receivable Types ====================
// TDS credits from customer payments - for Form 26AS reconciliation

export interface TdsReceivable {
  id: string;
  companyId: string;
  financialYear: string; // Format: '2024-25'
  quarter: string; // 'Q1', 'Q2', 'Q3', 'Q4'
  // Deductor details
  customerId?: string;
  deductorName: string;
  deductorTan?: string;
  deductorPan?: string;
  // Transaction details
  paymentDate: string;
  tdsSection: string; // 194J, 194C, 194H, 194O
  grossAmount: number;
  tdsRate: number;
  tdsAmount: number;
  netReceived: number;
  // Certificate details
  certificateNumber?: string;
  certificateDate?: string;
  certificateDownloaded: boolean;
  // Linked records
  paymentId?: string;
  invoiceId?: string;
  // 26AS matching
  matchedWith26As: boolean;
  form26AsAmount?: number;
  amountDifference?: number;
  matchedAt?: string;
  // Status
  status: 'pending' | 'matched' | 'claimed' | 'disputed' | 'written_off';
  claimedInReturn?: string;
  // Additional
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateTdsReceivableDto {
  companyId: string;
  financialYear: string;
  quarter: string;
  customerId?: string;
  deductorName: string;
  deductorTan?: string;
  deductorPan?: string;
  paymentDate: string;
  tdsSection: string;
  grossAmount: number;
  tdsRate: number;
  tdsAmount: number;
  netReceived: number;
  certificateNumber?: string;
  certificateDate?: string;
  certificateDownloaded?: boolean;
  paymentId?: string;
  invoiceId?: string;
  notes?: string;
}

export interface UpdateTdsReceivableDto {
  financialYear?: string;
  quarter?: string;
  customerId?: string;
  deductorName?: string;
  deductorTan?: string;
  deductorPan?: string;
  paymentDate?: string;
  tdsSection?: string;
  grossAmount?: number;
  tdsRate?: number;
  tdsAmount?: number;
  netReceived?: number;
  certificateNumber?: string;
  certificateDate?: string;
  certificateDownloaded?: boolean;
  paymentId?: string;
  invoiceId?: string;
  notes?: string;
}

export interface Match26AsDto {
  form26AsAmount: number;
}

export interface UpdateTdsStatusDto {
  status: 'pending' | 'matched' | 'claimed' | 'disputed' | 'written_off';
  claimedInReturn?: string;
}

export interface TdsReceivableSummary {
  financialYear: string;
  totalGrossAmount: number;
  totalTdsAmount: number;
  totalNetReceived: number;
  totalEntries: number;
  matchedEntries: number;
  unmatchedEntries: number;
  matchedAmount: number;
  unmatchedAmount: number;
  quarterlySummary: TdsQuarterlySummary[];
}

export interface TdsQuarterlySummary {
  quarter: string;
  tdsAmount: number;
  entryCount: number;
}

export interface TdsReceivableFilterParams extends PaginationParams {
  companyId?: string;
  financialYear?: string;
  quarter?: string;
  status?: string;
  matchedWith26As?: boolean;
}

// ==================== Leave Management Types ====================

export interface LeaveType {
  id: string;
  companyId: string;
  name: string;
  code: string;
  description?: string;
  daysPerYear: number;
  carryForwardAllowed: boolean;
  maxCarryForwardDays: number;
  encashmentAllowed: boolean;
  maxEncashmentDays?: number;
  isPaidLeave: boolean;
  requiresApproval: boolean;
  minDaysNotice?: number;
  maxConsecutiveDays?: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateLeaveTypeDto {
  companyId: string;
  name: string;
  code: string;
  description?: string;
  daysPerYear: number;
  carryForwardAllowed?: boolean;
  maxCarryForwardDays?: number;
  encashmentAllowed?: boolean;
  maxEncashmentDays?: number;
  isPaidLeave?: boolean;
  requiresApproval?: boolean;
  minDaysNotice?: number;
  maxConsecutiveDays?: number;
  isActive?: boolean;
}

export interface UpdateLeaveTypeDto extends Partial<CreateLeaveTypeDto> {}

export interface EmployeeLeaveBalance {
  id: string;
  employeeId: string;
  leaveTypeId: string;
  financialYear: string;
  openingBalance: number;
  accrued: number;
  taken: number;
  carryForwarded: number;
  adjusted: number;
  encashed: number;
  available: number;
  createdAt?: string;
  updatedAt?: string;
  // Joined fields
  employee?: Employee;
  leaveType?: LeaveType;
}

export interface CreateLeaveBalanceDto {
  employeeId: string;
  leaveTypeId: string;
  financialYear: string;
  openingBalance?: number;
  accrued?: number;
  taken?: number;
  carryForwarded?: number;
  adjusted?: number;
  encashed?: number;
}

export interface UpdateLeaveBalanceDto {
  openingBalance?: number;
  accrued?: number;
  taken?: number;
  carryForwarded?: number;
  adjusted?: number;
  encashed?: number;
}

export interface AdjustLeaveBalanceDto {
  adjustment: number;
  reason: string;
}

export interface LeaveApplication {
  id: string;
  employeeId: string;
  employeeName?: string;
  employeeCode?: string;
  leaveTypeId: string;
  leaveTypeName?: string;
  leaveTypeCode?: string;
  leaveTypeColor?: string;
  companyId: string;
  fromDate: string;
  toDate: string;
  totalDays: number;
  isHalfDay: boolean;
  halfDayType?: 'first_half' | 'second_half';
  reason?: string;
  status: 'pending' | 'approved' | 'rejected' | 'cancelled' | 'withdrawn';
  appliedAt: string;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: string;
  rejectionReason?: string;
  createdAt?: string;
  updatedAt?: string;
  // Joined fields
  employee?: Employee;
  leaveType?: LeaveType;
  approver?: Employee;
}

export interface CreateLeaveApplicationDto {
  employeeId: string;
  leaveTypeId: string;
  companyId: string;
  fromDate: string;
  toDate: string;
  totalDays: number;
  isHalfDay?: boolean;
  halfDayType?: 'first_half' | 'second_half';
  reason?: string;
}

export interface UpdateLeaveApplicationDto {
  fromDate?: string;
  toDate?: string;
  totalDays?: number;
  isHalfDay?: boolean;
  halfDayType?: 'first_half' | 'second_half';
  reason?: string;
}

export interface ApproveLeaveDto {
  comments?: string;
}

export interface RejectLeaveDto {
  reason: string;
}

export interface Holiday {
  id: string;
  companyId: string;
  name: string;
  date: string;
  year: number;
  isOptional: boolean;
  isFloating: boolean;
  description?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateHolidayDto {
  companyId: string;
  name: string;
  date: string;
  year: number;
  isOptional?: boolean;
  isFloating?: boolean;
  description?: string;
}

export interface UpdateHolidayDto extends Partial<CreateHolidayDto> {}

export interface BulkHolidaysDto {
  holidays: CreateHolidayDto[];
  skipDuplicates?: boolean;
}

export interface LeaveApplicationFilterParams extends PaginationParams {
  companyId?: string;
  employeeId?: string;
  leaveTypeId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  financialYear?: string;
}

export interface LeaveBalanceFilterParams extends PaginationParams {
  companyId?: string;
  employeeId?: string;
  leaveTypeId?: string;
  financialYear?: string;
}

export interface HolidayFilterParams extends PaginationParams {
  companyId?: string;
  year?: number;
  isOptional?: boolean;
}

export interface LeaveSummary {
  employeeId: string;
  employeeName: string;
  financialYear: string;
  balances: {
    leaveTypeName: string;
    leaveTypeCode: string;
    opening: number;
    accrued: number;
    taken: number;
    available: number;
  }[];
}

export interface LeaveCalendarEntry {
  date: string;
  type: 'holiday' | 'leave';
  name: string;
  employeeName?: string;
  leaveType?: string;
  isOptional?: boolean;
}

// Asset Request types
export type AssetRequestStatus = 'pending' | 'in_progress' | 'approved' | 'rejected' | 'fulfilled' | 'cancelled';
export type AssetRequestPriority = 'low' | 'normal' | 'high' | 'urgent';

export interface AssetRequestSummary {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeCode?: string;
  assetType: string;
  category: string;
  title: string;
  priority: AssetRequestPriority;
  status: AssetRequestStatus;
  quantity: number;
  estimatedBudget?: number;
  requestedAt: string;
  approvedAt?: string;
  fulfilledAt?: string;
}

export interface AssetRequestDetail extends AssetRequestSummary {
  companyId: string;
  department?: string;
  description?: string;
  justification?: string;
  specifications?: string;
  requestedByDate?: string;
  createdAt: string;
  updatedAt: string;
  approvedBy?: string;
  approvedByName?: string;
  rejectionReason?: string;
  cancelledAt?: string;
  cancellationReason?: string;
  assignedAssetId?: string;
  assignedAssetName?: string;
  fulfilledBy?: string;
  fulfilledByName?: string;
  fulfillmentNotes?: string;
  canEdit: boolean;
  canCancel: boolean;
  canFulfill: boolean;
  approvalRequestId?: string;
  hasApprovalWorkflow: boolean;
  currentApprovalStep?: number;
  totalApprovalSteps?: number;
}

export interface AssetRequestStats {
  totalRequests: number;
  pendingRequests: number;
  approvedRequests: number;
  rejectedRequests: number;
  fulfilledRequests: number;
  unfulfilledApproved: number;
}

export interface CreateAssetRequestDto {
  assetType: string;
  category: string;
  title: string;
  description?: string;
  justification?: string;
  specifications?: string;
  priority?: AssetRequestPriority;
  quantity?: number;
  estimatedBudget?: number;
  requestedByDate?: string;
}

export interface UpdateAssetRequestDto extends Partial<CreateAssetRequestDto> {}

export interface ApproveAssetRequestDto {
  comments?: string;
}

export interface RejectAssetRequestDto {
  reason: string;
}

export interface CancelAssetRequestDto {
  reason?: string;
}

export interface FulfillAssetRequestDto {
  assetId: string;
  notes?: string;
}

export interface AssetRequestFilterParams extends PaginationParams {
  companyId?: string;
  employeeId?: string;
  status?: string;
  priority?: string;
  category?: string;
  fromDate?: string;
  toDate?: string;
}

// ==================== General Ledger Types ====================

export type AccountType = 'asset' | 'liability' | 'equity' | 'income' | 'expense';
export type NormalBalance = 'debit' | 'credit';
export type JournalEntryStatus = 'draft' | 'posted' | 'reversed';
export type JournalEntryType = 'manual' | 'auto_post' | 'reversal' | 'opening' | 'closing';

export interface ChartOfAccount {
  id: string;
  companyId?: string;
  accountCode: string;
  accountName: string;
  accountType: AccountType;
  accountSubtype?: string;
  parentAccountId?: string;
  scheduleReference?: string;
  gstTreatment?: string;
  isControlAccount: boolean;
  isSystemAccount: boolean;
  normalBalance: NormalBalance;
  openingBalance: number;
  currentBalance: number;
  depthLevel: number;
  sortOrder: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateChartOfAccountDto {
  companyId: string;
  accountCode: string;
  accountName: string;
  accountType: AccountType;
  accountSubtype?: string;
  parentAccountId?: string;
  scheduleReference?: string;
  gstTreatment?: string;
  isControlAccount?: boolean;
  normalBalance: NormalBalance;
  openingBalance?: number;
}

export interface UpdateChartOfAccountDto {
  accountName?: string;
  accountSubtype?: string;
  scheduleReference?: string;
  gstTreatment?: string;
  isActive?: boolean;
}

export interface JournalEntry {
  id: string;
  companyId: string;
  journalNumber: string;
  journalDate: string;
  financialYear: string;
  periodMonth: number;
  entryType: JournalEntryType;
  sourceType?: string;
  sourceId?: string;
  sourceNumber?: string;
  description: string;
  totalDebit: number;
  totalCredit: number;
  status: JournalEntryStatus;
  postedAt?: string;
  postedBy?: string;
  isReversed: boolean;
  reversalOfId?: string;
  rulePackVersion?: string;
  ruleCode?: string;
  createdAt?: string;
  lines?: JournalEntryLine[];
}

export interface JournalEntryLine {
  id: string;
  journalEntryId: string;
  accountId: string;
  accountCode?: string;
  accountName?: string;
  debitAmount: number;
  creditAmount: number;
  currency: string;
  exchangeRate: number;
  subledgerType?: string;
  subledgerId?: string;
  description?: string;
  lineNumber: number;
}

export interface CreateJournalEntryDto {
  companyId: string;
  journalDate: string;
  description: string;
  lines: CreateJournalEntryLineDto[];
}

export interface CreateJournalEntryLineDto {
  accountId: string;
  debitAmount: number;
  creditAmount: number;
  description?: string;
  subledgerType?: string;
  subledgerId?: string;
}

export interface PostingRule {
  id: string;
  companyId?: string;
  ruleCode: string;
  ruleName: string;
  sourceType: string;
  triggerEvent: string;
  conditionsJson?: string;
  postingTemplate: string;
  financialYear?: string;
  effectiveFrom: string;
  isActive: boolean;
  priority: number;
  createdAt?: string;
}

// Trial Balance Report
export interface TrialBalanceRow {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountType: AccountType;
  depthLevel: number;
  openingBalance: number;
  debits: number;
  credits: number;
  closingBalance: number;
  debitBalance: number;
  creditBalance: number;
}

export interface TrialBalanceReport {
  companyId: string;
  asOfDate: string;
  financialYear: string;
  rows: TrialBalanceRow[];
  totalDebits: number;
  totalCredits: number;
  isBalanced: boolean;
}

// Income Statement Report
export interface IncomeStatementRow {
  accountId: string;
  accountCode: string;
  accountName: string;
  amount: number;
}

export interface IncomeStatementSection {
  sectionName: string;
  rows: IncomeStatementRow[];
  sectionTotal: number;
}

export interface IncomeStatementReport {
  companyId: string;
  fromDate: string;
  toDate: string;
  incomeSections: IncomeStatementSection[];
  expenseSections: IncomeStatementSection[];
  totalIncome: number;
  totalExpenses: number;
  netIncome: number;
}

// Balance Sheet Report
export interface BalanceSheetRow {
  accountId: string;
  accountCode: string;
  accountName: string;
  amount: number;
}

export interface BalanceSheetSection {
  sectionName: string;
  rows: BalanceSheetRow[];
  sectionTotal: number;
}

export interface BalanceSheetReport {
  companyId: string;
  asOfDate: string;
  assetSections: BalanceSheetSection[];
  liabilitySections: BalanceSheetSection[];
  equitySections: BalanceSheetSection[];
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  isBalanced: boolean;
}

// Account Ledger Report
export interface AccountLedgerEntry {
  date: string;
  journalNumber: string;
  journalEntryId: string;
  description: string;
  debit: number;
  credit: number;
  runningBalance: number;
}

export interface AccountLedgerReport {
  accountId: string;
  accountCode: string;
  accountName: string;
  fromDate: string;
  toDate: string;
  openingBalance: number;
  entries: AccountLedgerEntry[];
  closingBalance: number;
}

// Filter Params
export interface ChartOfAccountsFilterParams extends PaginationParams {
  companyId?: string;
  accountType?: AccountType;
  isActive?: boolean;
  searchTerm?: string;
}

export interface JournalEntriesFilterParams extends PaginationParams {
  companyId?: string;
  status?: JournalEntryStatus;
  entryType?: JournalEntryType;
  fromDate?: string;
  toDate?: string;
  financialYear?: string;
  searchTerm?: string;
}

// ==================== E-Invoice Types ====================

export interface EInvoiceCredentials {
  id: string;
  companyId: string;
  gspProvider: 'cleartax' | 'iris' | 'nic_direct';
  environment: 'sandbox' | 'production';
  clientId?: string;
  username?: string;
  autoGenerateIrn: boolean;
  autoCancelOnVoid: boolean;
  generateEwayBill: boolean;
  einvoiceThreshold: number;
  isActive: boolean;
  tokenExpiry?: string;
  createdAt: string;
  updatedAt: string;
}

export interface SaveEInvoiceCredentialsDto {
  companyId: string;
  gspProvider: string;
  environment?: string;
  clientId?: string;
  clientSecret?: string;
  username?: string;
  password?: string;
  autoGenerateIrn: boolean;
  autoCancelOnVoid: boolean;
  generateEwayBill: boolean;
  einvoiceThreshold?: number;
  isActive: boolean;
}

export interface EInvoiceGenerationResult {
  success: boolean;
  irn?: string;
  ackNumber?: string;
  ackDate?: string;
  qrCode?: string;
  ewayBillNumber?: string;
  errorCode?: string;
  errorMessage?: string;
}

export interface EInvoiceAuditLog {
  id: string;
  companyId: string;
  invoiceId?: string;
  actionType: string;
  requestTimestamp: string;
  responseStatus?: string;
  irn?: string;
  ackNumber?: string;
  ackDate?: string;
  errorCode?: string;
  errorMessage?: string;
  gspProvider?: string;
  environment?: string;
  responseTimeMs?: number;
  createdAt: string;
}

export interface EInvoiceQueueItem {
  id: string;
  companyId: string;
  invoiceId: string;
  actionType: string;
  priority: number;
  status: 'pending' | 'processing' | 'completed' | 'failed' | 'cancelled';
  retryCount: number;
  maxRetries: number;
  nextRetryAt?: string;
  startedAt?: string;
  completedAt?: string;
  errorCode?: string;
  errorMessage?: string;
  createdAt: string;
  updatedAt: string;
}

// ==================== Tax Rule Pack Types ====================

export interface TaxRulePack {
  id: string;
  packCode: string;
  packName: string;
  financialYear: string;
  version: number;
  sourceNotification?: string;
  description?: string;
  status: 'draft' | 'active' | 'superseded';
  incomeTaxSlabs?: Record<string, unknown>;
  standardDeductions?: Record<string, unknown>;
  rebateThresholds?: Record<string, unknown>;
  cessRates?: Record<string, unknown>;
  surchargeRates?: Record<string, unknown>;
  tdsRates?: Record<string, unknown>;
  pfEsiRates?: Record<string, unknown>;
  professionalTaxConfig?: Record<string, unknown>;
  gstRates?: Record<string, unknown>;
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
  activatedAt?: string;
  activatedBy?: string;
}

export interface CreateTaxRulePackDto {
  packCode: string;
  packName: string;
  financialYear: string;
  sourceNotification?: string;
  description?: string;
}

export interface TdsSectionRate {
  id: string;
  rulePackId: string;
  sectionCode: string;
  sectionName?: string;
  rateIndividual: number;
  rateCompany: number;
  rateNoPan: number;
  thresholdAmount?: number;
  thresholdType?: string;
  payeeTypes?: string[];
  isActive: boolean;
  notes?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  createdAt: string;
}

export interface TdsCalculationRequest {
  sectionCode: string;
  payeeType?: string;
  amount: number;
  hasPan?: boolean;
  transactionDate?: string;
}

export interface TdsCalculationResult {
  success: boolean;
  errorMessage?: string;
  rulePackId?: string;
  sectionCode?: string;
  applicableRate: number;
  thresholdAmount?: number;
  isExempt: boolean;
  tdsAmount: number;
}

export interface IncomeTaxCalculationRequest {
  taxableIncome: number;
  regime?: 'new' | 'old';
  ageCategory?: 'general' | 'senior' | 'super_senior';
  financialYear: string;
}

export interface IncomeTaxCalculationResult {
  success: boolean;
  regime: string;
  ageCategory?: string;
  grossIncome: number;
  taxableIncome: number;
  baseTax: number;
  rebate: number;
  surcharge: number;
  cess: number;
  totalTax: number;
  effectiveRate: number;
  slabBreakdown: IncomeTaxSlabBreakdown[];
  message?: string;
  financialYear?: string;
  rulePackVersion?: number;
}

export interface IncomeTaxSlabBreakdown {
  slabMin: number;
  slabMax?: number;
  rate: number;
  taxableAmount: number;
  taxAmount: number;
}

export interface PfEsiRates {
  employeePfRate: number;
  employerPfRate: number;
  pfWageCeiling: number;
  employeeEsiRate: number;
  employerEsiRate: number;
  esiWageCeiling: number;
  effectiveFrom?: string;
}

export interface RulePackUsageLog {
  id: string;
  rulePackId: string;
  companyId?: string;
  computationType: string;
  computationId: string;
  computationDate?: string;
  inputAmount?: number;
  computedTax?: number;
  effectiveRate?: number;
  computedAt: string;
  computedBy?: string;
}

// ==================== EXPORT & FOREX TYPES ====================

// FIRC Tracking (Foreign Inward Remittance Certificate)
export interface FircTracking {
  id: string;
  companyId: string;
  paymentId?: string;
  fircNumber: string;
  fircDate: string;
  bankName: string;
  purposeCode?: string;
  foreignCurrency: string;
  foreignAmount: number;
  inrAmount: number;
  exchangeRate: number;
  beneficiaryName?: string;
  remitterName?: string;
  remitterCountry?: string;
  invoiceIds?: string[];
  edpmsReported: boolean;
  edpmsReportDate?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateFircDto {
  companyId: string;
  fircNumber: string;
  fircDate: string;
  bankName: string;
  purposeCode?: string;
  foreignCurrency: string;
  foreignAmount: number;
  inrAmount: number;
  exchangeRate: number;
  beneficiaryName?: string;
  remitterName?: string;
  remitterCountry?: string;
}

export interface UpdateFircDto {
  fircNumber?: string;
  fircDate?: string;
  bankName?: string;
  purposeCode?: string;
  foreignCurrency?: string;
  foreignAmount?: number;
  inrAmount?: number;
  exchangeRate?: number;
  beneficiaryName?: string;
  remitterName?: string;
  remitterCountry?: string;
  edpmsReported?: boolean;
  edpmsReportDate?: string;
}

export interface FircAutoMatchResult {
  fircId: string;
  matchedPaymentId?: string;
  matchedInvoiceIds: string[];
  matchConfidence: number;
  matchReason: string;
  isAutoMatched: boolean;
}

// LUT Register (Letter of Undertaking for GST Exports)
export interface LutRegister {
  id: string;
  companyId: string;
  lutNumber: string;
  financialYear: string;
  validFrom: string;
  validTo: string;
  gstin: string;
  filingDate?: string;
  arn?: string;
  status: 'active' | 'expired' | 'superseded';
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateLutDto {
  companyId: string;
  lutNumber: string;
  financialYear: string;
  validFrom: string;
  validTo: string;
  gstin: string;
  filingDate?: string;
  arn?: string;
}

export interface UpdateLutDto {
  lutNumber?: string;
  financialYear?: string;
  validFrom?: string;
  validTo?: string;
  gstin?: string;
  filingDate?: string;
  arn?: string;
  status?: 'active' | 'expired' | 'superseded';
}

export interface LutValidationResult {
  isValid: boolean;
  lutNumber?: string;
  validFrom?: string;
  validTo?: string;
  daysRemaining?: number;
  message: string;
}

export interface LutExpiryAlert {
  lutId: string;
  lutNumber: string;
  financialYear: string;
  validTo: string;
  daysToExpiry: number;
  severity: 'info' | 'warning' | 'critical';
  message: string;
}

// Export Receivables Ageing
export interface ExportReceivablesAgeingReport {
  companyId: string;
  asOfDate: string;
  current: number;
  currentInr: number;
  currentCount: number;
  days31To60: number;
  days31To60Inr: number;
  days31To60Count: number;
  days61To90: number;
  days61To90Inr: number;
  days61To90Count: number;
  days91To180: number;
  days91To180Inr: number;
  days91To180Count: number;
  days181To270: number;
  days181To270Inr: number;
  days181To270Count: number;
  over270Days: number;
  over270DaysInr: number;
  over270DaysCount: number;
  totalReceivablesForeign: number;
  totalReceivablesInr: number;
  invoices: AgeingInvoice[];
  currencyBreakdown: Record<string, CurrencyAgeing>;
}

export interface AgeingInvoice {
  invoiceId: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  daysOutstanding: number;
  customerId: string;
  customerName: string;
  currency: string;
  invoiceAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  outstandingAmountInr: number;
  ageingBucket: string;
  femaDeadline: string;
  daysToFemaDeadline: number;
  isFemaOverdue: boolean;
}

export interface CurrencyAgeing {
  currency: string;
  totalAmount: number;
  totalAmountInr: number;
  invoiceCount: number;
  current: number;
  days31To60: number;
  days61To90: number;
  over90Days: number;
}

export interface CustomerExportReceivable {
  customerId: string;
  customerName: string;
  country?: string;
  primaryCurrency: string;
  invoiceCount: number;
  totalInvoiced: number;
  totalPaid: number;
  totalOutstanding: number;
  totalOutstandingInr: number;
  oldestInvoiceDays: number;
  femaOverdueCount: number;
  femaOverdueAmount: number;
}

// Forex Gain/Loss Report
export interface ForexGainLossReport {
  companyId: string;
  fromDate: string;
  toDate: string;
  realizedGainTotal: number;
  realizedLossTotal: number;
  netRealizedGainLoss: number;
  unrealizedGainTotal: number;
  unrealizedLossTotal: number;
  netUnrealizedGainLoss: number;
  realizedTransactionCount: number;
  realizedTransactions: ForexTransactionDetail[];
  monthlyTrend: MonthlyForexSummary[];
}

export interface ForexTransactionDetail {
  transactionId: string;
  transactionDate: string;
  transactionType: string;
  documentNumber: string;
  currency: string;
  foreignAmount: number;
  bookingRate: number;
  settlementRate: number;
  bookingAmountInr: number;
  settlementAmountInr: number;
  gainLoss: number;
}

export interface MonthlyForexSummary {
  month: number;
  year: number;
  monthName: string;
  realizedGainLoss: number;
  unrealizedGainLoss: number;
  totalGainLoss: number;
  transactionCount: number;
}

export interface UnrealizedForexPosition {
  companyId: string;
  asOfDate: string;
  currentExchangeRate: number;
  totalOpenPositionForeign: number;
  totalOpenPositionInrAtBooking: number;
  totalOpenPositionInrAtCurrent: number;
  totalUnrealizedGainLoss: number;
  currencyBreakdown: Record<string, CurrencyForexPosition>;
}

export interface CurrencyForexPosition {
  currency: string;
  openAmount: number;
  avgBookingRate: number;
  currentRate: number;
  bookingValueInr: number;
  currentValueInr: number;
  unrealizedGainLoss: number;
}

// FEMA Compliance Dashboard
export interface FemaComplianceDashboard {
  companyId: string;
  complianceScore: number;
  overallStatus: 'compliant' | 'warning' | 'critical' | 'non_compliant';
  totalOpenInvoices: number;
  totalExportReceivables: number;
  fullyRealizedCount: number;
  fullyRealizedAmount: number;
  partiallyRealizedCount: number;
  partiallyRealizedAmount: number;
  pendingRealizationCount: number;
  pendingRealizationAmount: number;
  overdueCount: number;
  overdueAmount: number;
  fircsReceived: number;
  fircsPending: number;
  fircsCoverage: number;
  edpmsReported: number;
  edpmsPending: number;
  hasActiveLut: boolean;
  activeLutNumber?: string;
  daysToLutExpiry?: number;
  criticalAlerts: number;
  warningAlerts: number;
  topAlerts: FemaViolationAlert[];
}

export interface FemaViolationAlert {
  alertType: string;
  severity: 'info' | 'warning' | 'critical';
  title: string;
  description: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  documentNumber?: string;
  amount?: number;
  currency?: string;
  daysOverdue?: number;
}

// Export Realization Report
export interface ExportRealizationReport {
  companyId: string;
  financialYear: string;
  totalExportInvoices: number;
  totalExportValue: number;
  totalRealizedValue: number;
  totalPendingValue: number;
  realizationPercentage: number;
  avgRealizationDays: number;
  byStatus: RealizationStatusSummary[];
  byCustomer: CustomerRealization[];
  monthlyBreakdown: MonthlyRealization[];
  atRiskInvoices: AtRiskInvoice[];
}

export interface RealizationStatusSummary {
  status: string;
  count: number;
  amount: number;
}

export interface CustomerRealization {
  customerId: string;
  customerName: string;
  invoiceCount: number;
  totalExportValue: number;
  realizedValue: number;
  pendingValue: number;
  realizationPercentage: number;
  avgRealizationDays: number;
}

export interface MonthlyRealization {
  month: number;
  year: number;
  monthName: string;
  invoiceCount: number;
  invoicedAmount: number;
  realizedAmount: number;
  realizationPercentage: number;
}

export interface AtRiskInvoice {
  invoiceId: string;
  invoiceNumber: string;
  invoiceDate: string;
  femaDeadline: string;
  daysToDeadline: number;
  customerName: string;
  outstandingAmount: number;
  currency: string;
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
}

export interface MonthlyRealizationTrend {
  month: number;
  year: number;
  monthName: string;
  invoiced: number;
  realized: number;
  outstanding: number;
  realizationRate: number;
  avgDaysToRealize: number;
}

// Combined Export Dashboard
export interface ExportDashboard {
  companyId: string;
  totalExportReceivables: number;
  totalExportRevenueFy: number;
  totalRealizedFy: number;
  totalInvoicesFy: number;
  overdueInvoices: number;
  totalCustomers: number;
  netForexGainLossFy: number;
  unrealizedForexPosition: number;
  avgRealizationDays: number;
  femaComplianceScore: number;
  hasActiveLut: boolean;
  pendingFircs: number;
  criticalAlerts: number;
  warningAlerts: number;
  realizationTrend: MonthlyRealizationTrend[];
  forexTrend: MonthlyForexSummary[];
  receivablesByCustomer: Record<string, number>;
  receivablesByCurrency: Record<string, number>;
}

// GSTR-1 Export Types
export interface Gstr1ExportData {
  companyId: string;
  returnPeriod: string;
  exports: Gstr1Export[];
  totalInvoices: number;
  totalTaxableValue: number;
  totalIgst: number;
}

export interface Gstr1Export {
  invoiceNumber: string;
  invoiceDate: string;
  portCode?: string;
  shippingBillNumber?: string;
  shippingBillDate?: string;
  customerName: string;
  countryCode: string;
  currency: string;
  foreignValue: number;
  exchangeRate: number;
  taxableValue: number;
  igstRate: number;
  igstAmount: number;
  exportType: 'EXPWP' | 'EXPWOP';
  irnNumber?: string;
}

// Realization Alert
export interface RealizationAlert {
  invoiceId: string;
  invoiceNumber: string;
  invoiceDate: string;
  customerId: string;
  customerName: string;
  currency: string;
  invoiceAmount: number;
  outstandingAmount: number;
  outstandingAmountInr: number;
  femaDeadline: string;
  daysToDeadline: number;
  daysOverdue?: number;
  alertLevel: 'approaching' | 'imminent' | 'overdue';
  alertMessage: string;
}

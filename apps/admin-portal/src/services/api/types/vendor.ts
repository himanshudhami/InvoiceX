// Vendor types (Accounts Payable module)
import type { PaginationParams } from './common';

// ==================== Vendor Types ====================

export interface Vendor {
  id: string;
  companyId: string;
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
  notes?: string;
  creditLimit?: number;
  paymentTerms?: number;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  // GST Compliance fields
  gstin?: string;              // GSTIN (15 characters)
  gstStateCode?: string;       // GST State Code (2 digits)
  vendorType?: string;         // 'registered' | 'unregistered' | 'composition' | 'overseas'
  isGstRegistered?: boolean;   // Whether vendor is GST registered
  panNumber?: string;          // PAN Number
  // TDS fields
  tanNumber?: string;          // TAN for vendors who deduct TDS
  defaultTdsSection?: string;  // 194C, 194J, etc.
  defaultTdsRate?: number;     // Default TDS rate
  tdsApplicable?: boolean;     // Whether TDS is applicable
  // MSME compliance
  msmeRegistered?: boolean;
  msmeRegistrationNumber?: string;
  msmeCategory?: string;       // 'micro' | 'small' | 'medium'
  // Bank details
  bankAccountNumber?: string;
  bankName?: string;
  ifscCode?: string;
  bankBranch?: string;
  // Tally migration
  tallyLedgerGuid?: string;
  tallyLedgerName?: string;
}

export interface CreateVendorDto {
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
  notes?: string;
  creditLimit?: number;
  paymentTerms?: number;
  isActive?: boolean;
  // GST
  gstin?: string;
  gstStateCode?: string;
  vendorType?: string;
  isGstRegistered?: boolean;
  panNumber?: string;
  // TDS
  tanNumber?: string;
  defaultTdsSection?: string;
  defaultTdsRate?: number;
  tdsApplicable?: boolean;
  // MSME
  msmeRegistered?: boolean;
  msmeRegistrationNumber?: string;
  msmeCategory?: string;
  // Bank
  bankAccountNumber?: string;
  bankName?: string;
  ifscCode?: string;
  bankBranch?: string;
}

export interface UpdateVendorDto extends CreateVendorDto {}

export interface VendorsFilterParams extends PaginationParams {
  name?: string;
  companyName?: string;
  email?: string;
  city?: string;
  state?: string;
  isActive?: boolean;
  companyId?: string;
  vendorType?: string;
  isGstRegistered?: boolean;
  tdsApplicable?: boolean;
  defaultTdsSection?: string;
  msmeRegistered?: boolean;
  msmeCategory?: string;
}

// ==================== Vendor Invoice Types ====================

export interface VendorInvoiceItem {
  id: string;
  vendorInvoiceId: string;
  productId?: string;
  description: string;
  quantity: number;
  unitPrice: number;
  taxRate?: number;
  discountRate?: number;
  lineTotal: number;
  sortOrder?: number;
  // GST fields
  hsnSacCode?: string;
  isService?: boolean;
  cgstRate?: number;
  cgstAmount?: number;
  sgstRate?: number;
  sgstAmount?: number;
  igstRate?: number;
  igstAmount?: number;
  cessRate?: number;
  cessAmount?: number;
  // ITC
  itcEligible?: boolean;
  itcCategory?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface VendorInvoice {
  id: string;
  companyId: string;
  partyId: string;  // References parties table (vendor)
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  receivedDate?: string;
  status?: string;           // 'draft' | 'pending_approval' | 'approved' | 'posted' | 'paid' | 'cancelled'
  invoiceType?: string;      // 'regular' | 'credit_note' | 'debit_note'
  subtotal: number;
  taxAmount?: number;
  discountAmount?: number;
  totalAmount: number;
  paidAmount?: number;
  currency?: string;
  notes?: string;
  terms?: string;
  poNumber?: string;
  // GST Classification
  supplyType?: string;       // 'intra_state' | 'inter_state' | 'import'
  placeOfSupply?: string;
  reverseCharge?: boolean;
  rcmApplicable?: boolean;
  // GST Totals
  totalCgst?: number;
  totalSgst?: number;
  totalIgst?: number;
  totalCess?: number;
  // TDS
  tdsApplicable?: boolean;
  tdsSection?: string;
  tdsRate?: number;
  tdsAmount?: number;
  // ITC
  itcEligible?: boolean;
  itcCategory?: string;      // 'inputs' | 'capital_goods' | 'input_services'
  itcClaimedAmount?: number;
  // GSTR-2B matching
  gstr2bMatched?: boolean;
  gstr2bMatchDate?: string;
  // Posting
  isPosted?: boolean;
  journalEntryId?: string;
  // Tally migration
  tallyVoucherGuid?: string;
  tallyVoucherNumber?: string;
  // Items
  items?: VendorInvoiceItem[];
  // Relations
  vendor?: Vendor;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateVendorInvoiceDto {
  companyId?: string;
  partyId: string;  // References parties table (vendor)
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  receivedDate?: string;
  status?: string;
  invoiceType?: string;
  subtotal: number;
  taxAmount?: number;
  discountAmount?: number;
  totalAmount: number;
  currency?: string;
  notes?: string;
  terms?: string;
  poNumber?: string;
  // GST
  supplyType?: string;
  placeOfSupply?: string;
  reverseCharge?: boolean;
  rcmApplicable?: boolean;
  totalCgst?: number;
  totalSgst?: number;
  totalIgst?: number;
  totalCess?: number;
  // TDS
  tdsApplicable?: boolean;
  tdsSection?: string;
  tdsRate?: number;
  tdsAmount?: number;
  // ITC
  itcEligible?: boolean;
  itcCategory?: string;
  // Items
  items?: CreateVendorInvoiceItemDto[];
}

export interface CreateVendorInvoiceItemDto {
  productId?: string;
  description: string;
  quantity: number;
  unitPrice: number;
  taxRate?: number;
  discountRate?: number;
  lineTotal: number;
  sortOrder?: number;
  hsnSacCode?: string;
  isService?: boolean;
  cgstRate?: number;
  cgstAmount?: number;
  sgstRate?: number;
  sgstAmount?: number;
  igstRate?: number;
  igstAmount?: number;
  cessRate?: number;
  cessAmount?: number;
  itcEligible?: boolean;
  itcCategory?: string;
}

export interface UpdateVendorInvoiceDto extends CreateVendorInvoiceDto {
  id: string;
}

export interface VendorInvoicesFilterParams extends PaginationParams {
  companyId?: string;
  partyId?: string;  // References parties table (vendor)
  status?: string;
  invoiceType?: string;
  invoiceNumber?: string;
  supplyType?: string;
  tdsApplicable?: boolean;
  tdsSection?: string;
  itcEligible?: boolean;
  itcCategory?: string;
  gstr2bMatched?: boolean;
  isPosted?: boolean;
  invoiceDateFrom?: string;
  invoiceDateTo?: string;
  dueDateFrom?: string;
  dueDateTo?: string;
  financialYear?: string;
}

// ==================== Vendor Payment Types ====================

export interface VendorPaymentAllocation {
  id: string;
  vendorPaymentId: string;
  vendorInvoiceId?: string;    // Null for advances
  allocatedAmount: number;
  tdsAllocated?: number;
  allocationType?: string;     // 'bill_settlement' | 'advance' | 'debit_note' | 'on_account'
  tallyBillRef?: string;
  createdAt?: string;
  updatedAt?: string;
  // Relations
  vendorInvoice?: VendorInvoice;
}

export interface VendorPayment {
  id: string;
  companyId: string;
  partyId: string;  // References parties table (vendor)
  paymentDate: string;
  amount: number;
  grossAmount?: number;
  currency?: string;
  paymentMethod?: string;      // 'bank_transfer' | 'cheque' | 'cash' | 'neft' | 'rtgs' | 'upi'
  paymentType?: string;        // 'bill_payment' | 'advance_paid' | 'expense_reimbursement' | 'refund_paid'
  referenceNumber?: string;
  chequeNumber?: string;
  chequeDate?: string;
  bankAccountId?: string;
  status?: string;             // 'draft' | 'pending_approval' | 'approved' | 'processed' | 'cancelled'
  notes?: string;
  // TDS
  tdsApplicable?: boolean;
  tdsSection?: string;
  tdsRate?: number;
  tdsAmount?: number;
  tdsDeposited?: boolean;
  tdsChallanNumber?: string;
  tdsDepositDate?: string;
  // Reconciliation
  isReconciled?: boolean;
  reconciledAt?: string;
  bankTransactionId?: string;
  // Posting
  isPosted?: boolean;
  journalEntryId?: string;
  // Tally migration
  tallyVoucherGuid?: string;
  tallyVoucherNumber?: string;
  // Relations
  vendor?: Vendor;
  allocations?: VendorPaymentAllocation[];
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateVendorPaymentDto {
  companyId?: string;
  partyId: string;  // References parties table (vendor)
  paymentDate: string;
  amount: number;
  grossAmount?: number;
  currency?: string;
  paymentMethod?: string;
  paymentType?: string;
  referenceNumber?: string;
  chequeNumber?: string;
  chequeDate?: string;
  bankAccountId?: string;
  status?: string;
  notes?: string;
  // TDS
  tdsApplicable?: boolean;
  tdsSection?: string;
  tdsRate?: number;
  tdsAmount?: number;
  // Allocations
  allocations?: CreateVendorPaymentAllocationDto[];
}

export interface CreateVendorPaymentAllocationDto {
  vendorInvoiceId?: string;
  allocatedAmount: number;
  tdsAllocated?: number;
  allocationType?: string;
}

export interface UpdateVendorPaymentDto extends Omit<CreateVendorPaymentDto, 'companyId'> {}

export interface VendorPaymentsFilterParams extends PaginationParams {
  companyId?: string;
  partyId?: string;  // References parties table (vendor)
  bankAccountId?: string;
  status?: string;
  paymentMethod?: string;
  paymentType?: string;
  tdsApplicable?: boolean;
  tdsSection?: string;
  tdsDeposited?: boolean;
  isReconciled?: boolean;
  isPosted?: boolean;
  paymentDateFrom?: string;
  paymentDateTo?: string;
  financialYear?: string;
}

// ==================== Vendor Summary Types ====================

export interface VendorOutstanding {
  vendorId: string;
  vendorName: string;
  totalInvoiced: number;
  totalPaid: number;
  totalTdsDeducted: number;
  outstandingAmount: number;
  overdueAmount: number;
  advanceBalance: number;
}

export interface VendorAgingSummary {
  vendorId: string;
  vendorName: string;
  current: number;      // 0-30 days
  days30: number;       // 31-60 days
  days60: number;       // 61-90 days
  days90: number;       // 91-120 days
  days120Plus: number;  // 120+ days
  total: number;
}

export interface VendorTdsSummary {
  vendorId: string;
  vendorName: string;
  panNumber?: string;
  tdsSection: string;
  totalDeducted: number;
  totalDeposited: number;
  pendingDeposit: number;
}

// ==================== Vendor Payment Summary Types ====================

export interface VendorPaymentSummary {
  totalPaid: number;
  vendorCount: number;
  paymentCount: number;
  vendors: VendorPaymentDetail[];
}

export interface VendorPaymentDetail {
  vendorId: string;
  vendorName: string;
  totalPaid: number;
  paymentCount: number;
  lastPaymentDate?: string;
}

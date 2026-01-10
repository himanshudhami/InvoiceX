// GST Compliance Types

// ITC Blocked Category (Section 17(5))
export interface ItcBlockedCategory {
  id?: string;
  categoryCode: string;
  categoryName: string;
  sectionReference: string;
  hsnSacCodes?: string;
  description?: string;
  isActive: boolean;
}

// ITC Blocked Check Request/Result
export interface ItcBlockedCheckRequest {
  hsnSacCode?: string;
  expenseCategory?: string;
  description?: string;
  gstAmount: number;
}

export interface ItcBlockedCheckResult {
  isBlocked: boolean;
  blockedCategoryCode?: string;
  blockedCategoryName?: string;
  sectionReference?: string;
  reason?: string;
}

// ITC Blocked Request for posting
export interface ItcBlockedRequest {
  companyId: string;
  transactionDate: string;
  sourceType?: string;
  sourceId?: string;
  sourceNumber?: string;
  blockedCategoryCode: string;
  hsnSacCode?: string;
  description?: string;
  taxableValue: number;
  cgstAmount: number;
  sgstAmount: number;
  igstAmount: number;
  cessAmount: number;
  notes?: string;
}

// Credit Note GST Request
export interface CreditNoteGstRequest {
  companyId: string;
  creditNoteDate: string;
  creditNoteNumber: string;
  originalInvoiceId?: string;
  originalInvoiceNumber?: string;
  taxableValue: number;
  cgstAmount: number;
  sgstAmount: number;
  igstAmount: number;
  cessAmount: number;
  supplyType: 'intra_state' | 'inter_state';
  partyName?: string;
  partyGstin?: string;
  reason?: string;
  notes?: string;
}

// Debit Note GST Request
export interface DebitNoteGstRequest {
  companyId: string;
  debitNoteDate: string;
  debitNoteNumber: string;
  originalInvoiceId?: string;
  originalInvoiceNumber?: string;
  taxableValue: number;
  cgstAmount: number;
  sgstAmount: number;
  igstAmount: number;
  cessAmount: number;
  supplyType: 'intra_state' | 'inter_state';
  partyName?: string;
  partyGstin?: string;
  reason?: string;
  notes?: string;
}

// ITC Reversal Calculation Request/Result
export interface ItcReversalCalculationRequest {
  companyId: string;
  financialYear: string;
  returnPeriod: string;
  reversalRule: 'Rule42' | 'Rule43';
  totalTurnover?: number;
  exemptTurnover?: number;
  totalCommonCredit?: number;
  capitalGoodsValue?: number;
  usefulLife?: number;
  exemptUsePercentage?: number;
}

export interface ItcReversalCalculation {
  reversalRule: string;
  totalItcAvailable: number;
  itcToReverse: number;
  cgstReversal: number;
  sgstReversal: number;
  igstReversal: number;
  cessReversal: number;
  calculationDetails?: string;
}

// ITC Reversal Request
export interface ItcReversalRequest {
  companyId: string;
  reversalDate: string;
  reversalRule: 'Rule42' | 'Rule43';
  returnPeriod: string;
  cgstReversal: number;
  sgstReversal: number;
  igstReversal: number;
  cessReversal: number;
  reason?: string;
  notes?: string;
}

// UTGST Request
export interface UtgstRequest {
  companyId: string;
  transactionDate: string;
  transactionType: 'input' | 'output';
  sourceType?: string;
  sourceId?: string;
  sourceNumber?: string;
  taxableValue: number;
  cgstRate: number;
  cgstAmount: number;
  utgstRate: number;
  utgstAmount: number;
  partyName?: string;
  partyGstin?: string;
  unionTerritory?: string;
  notes?: string;
}

// GST TDS Request (Section 51)
export interface GstTdsRequest {
  companyId: string;
  transactionDate: string;
  invoiceId?: string;
  invoiceNumber?: string;
  taxableValue: number;
  tdsRate: number;
  cgstTdsAmount: number;
  sgstTdsAmount: number;
  igstTdsAmount: number;
  supplyType: 'intra_state' | 'inter_state';
  deductorName: string;
  deductorGstin?: string;
  deductorTan?: string;
  notes?: string;
}

// GST TCS Request (Section 52)
export interface GstTcsRequest {
  companyId: string;
  transactionDate: string;
  netValue: number;
  tcsRate: number;
  cgstTcsAmount: number;
  sgstTcsAmount: number;
  igstTcsAmount: number;
  supplyType: 'intra_state' | 'inter_state';
  operatorName: string;
  operatorGstin?: string;
  notes?: string;
}

// GST Posting Result
export interface GstPostingResult {
  success: boolean;
  journalEntry?: {
    id: string;
    journalNumber?: string;
    totalDebit: number;
    totalCredit: number;
  };
  errorMessage?: string;
}

// ITC Blocked Summary
export interface ItcBlockedSummary {
  returnPeriod: string;
  totalBlockedCgst: number;
  totalBlockedSgst: number;
  totalBlockedIgst: number;
  totalBlockedCess: number;
  totalBlockedAmount: number;
  transactionCount: number;
  categoryBreakdown: ItcBlockedCategorySummary[];
}

export interface ItcBlockedCategorySummary {
  categoryCode: string;
  categoryName: string;
  blockedAmount: number;
  transactionCount: number;
}

// ITC Availability Report
export interface ItcAvailabilityReport {
  returnPeriod: string;
  totalItcAvailed: number;
  itcBlocked: number;
  itcReversed: number;
  netItcAvailable: number;
  itcUtilized: number;
  itcBalance: number;
}

// ==================== GSTR-3B Types ====================

export type Gstr3bFilingStatus = 'draft' | 'generated' | 'reviewed' | 'filed';

// GSTR-3B Filing
export interface Gstr3bFiling {
  id: string;
  companyId: string;
  gstin: string;
  returnPeriod: string;
  financialYear: string;
  status: Gstr3bFilingStatus;
  generatedAt?: string;
  generatedBy?: string;
  reviewedAt?: string;
  reviewedBy?: string;
  filedAt?: string;
  filedBy?: string;
  arn?: string;
  filingDate?: string;
  notes?: string;
  table31?: Gstr3bTable31;
  table4?: Gstr3bTable4;
  table5?: Gstr3bTable5;
  variance?: Gstr3bVarianceSummary;
  createdAt?: string;
  updatedAt?: string;
}

// Table 3.1 - Outward and inward supplies
export interface Gstr3bTable31 {
  outwardTaxable: Gstr3bRow;
  outwardZeroRated: Gstr3bRow;
  otherOutward: Gstr3bRow;
  inwardRcm: Gstr3bRow;
  nonGst: Gstr3bRow;
}

export interface Gstr3bRow {
  taxableValue: number;
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  sourceCount?: number;
}

// Table 4 - ITC
export interface Gstr3bTable4 {
  itcAvailable: Gstr3bItcAvailable;
  itcReversed: Gstr3bItcReversed;
  itcIneligible: Gstr3bItcIneligible;
  netItc: Gstr3bItcTotal;
}

export interface Gstr3bItcAvailable {
  importGoods: Gstr3bItcRow;
  importServices: Gstr3bItcRow;
  rcmInward: Gstr3bItcRow;
  isdInward: Gstr3bItcRow;
  allOtherItc: Gstr3bItcRow;
  total: Gstr3bItcTotal;
}

export interface Gstr3bItcReversed {
  rule42: Gstr3bItcRow;
  rule43: Gstr3bItcRow;
  others: Gstr3bItcRow;
  total: Gstr3bItcTotal;
}

export interface Gstr3bItcIneligible {
  section17_5: Gstr3bItcRow;
  others: Gstr3bItcRow;
  total: Gstr3bItcTotal;
}

export interface Gstr3bItcRow {
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  sourceCount?: number;
}

export interface Gstr3bItcTotal {
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  totalItc: number;
}

// Table 5 - Exempt supplies
export interface Gstr3bTable5 {
  interStateSupplies: Gstr3bExemptRow;
  intraStateSupplies: Gstr3bExemptRow;
}

export interface Gstr3bExemptRow {
  nilRated: number;
  exempted: number;
  nonGst: number;
  total?: number;
}

// Line Item
export interface Gstr3bLineItem {
  id: string;
  tableCode: string;
  rowOrder: number;
  description: string;
  taxableValue: number;
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  sourceCount: number;
  sourceType: string;
  computationNotes?: string;
}

// Source Document (for drill-down)
export interface Gstr3bSourceDocument {
  id: string;
  sourceType: string;
  sourceId: string;
  sourceNumber: string;
  sourceDate: string;
  taxableValue: number;
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  partyName?: string;
  partyGstin?: string;
}

// Variance
export interface Gstr3bVarianceSummary {
  previousPeriod: string;
  items: Gstr3bVarianceItem[];
}

export interface Gstr3bVarianceItem {
  field: string;
  tableCode: string;
  previousValue: number;
  currentValue?: number;
  variance?: number;
  variancePercentage?: number;
}

// Filing History
export interface Gstr3bFilingHistory {
  id: string;
  returnPeriod: string;
  financialYear: string;
  status: Gstr3bFilingStatus;
  generatedAt?: string;
  filedAt?: string;
  arn?: string;
}

// Request DTOs
export interface GenerateGstr3bRequest {
  companyId: string;
  returnPeriod: string;
  regenerate?: boolean;
}

export interface ReviewGstr3bRequest {
  notes?: string;
}

export interface FileGstr3bRequest {
  arn: string;
  filingDate: string;
}

// ==================== GSTR-2B Types ====================

export type Gstr2bImportStatus = 'pending' | 'processing' | 'completed' | 'failed';
export type Gstr2bMatchStatus = 'pending' | 'matched' | 'partial_match' | 'unmatched' | 'manual_match';
export type Gstr2bActionStatus = 'pending' | 'accepted' | 'rejected';

// GSTR-2B Import
export interface Gstr2bImport {
  id: string;
  companyId: string;
  returnPeriod: string;
  gstin: string;
  importSource: string;
  fileName?: string;
  importStatus: Gstr2bImportStatus;
  errorMessage?: string;
  totalInvoices: number;
  matchedInvoices: number;
  unmatchedInvoices: number;
  partiallyMatchedInvoices: number;
  totalItcIgst: number;
  totalItcCgst: number;
  totalItcSgst: number;
  totalItcCess: number;
  totalItcAmount: number;
  matchedItcAmount: number;
  importedAt?: string;
  processedAt?: string;
  createdAt: string;
}

// GSTR-2B Invoice
export interface Gstr2bInvoice {
  id: string;
  importId: string;
  returnPeriod: string;
  supplierGstin: string;
  supplierName?: string;
  supplierTradeName?: string;
  invoiceNumber: string;
  invoiceDate: string;
  invoiceType?: string;
  documentType?: string;
  taxableValue: number;
  igstAmount: number;
  cgstAmount: number;
  sgstAmount: number;
  cessAmount: number;
  totalGst: number;
  totalInvoiceValue: number;
  itcEligible: boolean;
  itcIgst: number;
  itcCgst: number;
  itcSgst: number;
  itcCess: number;
  placeOfSupply?: string;
  supplyType?: string;
  reverseCharge: boolean;
  matchStatus: Gstr2bMatchStatus;
  matchedVendorInvoiceId?: string;
  matchedVendorInvoiceNumber?: string;
  matchConfidence?: number;
  discrepancies?: string[];
  actionStatus?: Gstr2bActionStatus;
  actionNotes?: string;
}

// GSTR-2B Invoice List Item (lighter version)
export interface Gstr2bInvoiceListItem {
  id: string;
  supplierGstin: string;
  supplierName?: string;
  invoiceNumber: string;
  invoiceDate: string;
  invoiceType?: string;
  taxableValue: number;
  totalItc: number;
  matchStatus: Gstr2bMatchStatus;
  matchConfidence?: number;
  actionStatus?: Gstr2bActionStatus;
}

// GSTR-2B Reconciliation Summary
export interface Gstr2bReconciliationSummary {
  returnPeriod: string;
  totalInvoices: number;
  matchedInvoices: number;
  partialMatchInvoices: number;
  unmatchedInvoices: number;
  acceptedInvoices: number;
  rejectedInvoices: number;
  pendingReviewInvoices: number;
  matchPercentage: number;
  totalTaxableValue: number;
  matchedTaxableValue: number;
  unmatchedTaxableValue: number;
  totalItcAvailable: number;
  matchedItc: number;
  unmatchedItc: number;
}

// GSTR-2B Supplier Summary
export interface Gstr2bSupplierSummary {
  supplierGstin: string;
  supplierName?: string;
  invoiceCount: number;
  matchedCount: number;
  unmatchedCount: number;
  totalTaxableValue: number;
  totalItc: number;
  matchPercentage: number;
}

// GSTR-2B ITC Comparison
export interface Gstr2bItcComparison {
  returnPeriod: string;
  gstr2b: Gstr2bItcBreakdown;
  books: Gstr2bItcBreakdown;
  difference: Gstr2bItcBreakdown;
}

export interface Gstr2bItcBreakdown {
  igst: number;
  cgst: number;
  sgst: number;
  cess: number;
  total: number;
}

// GSTR-2B Request DTOs
export interface ImportGstr2bRequest {
  companyId: string;
  returnPeriod: string;
  jsonData: string;
  fileName?: string;
}

export interface RunReconciliationRequest {
  importId: string;
  force?: boolean;
}

export interface AcceptMismatchRequest {
  invoiceId: string;
  notes?: string;
}

export interface RejectInvoiceRequest {
  invoiceId: string;
  reason: string;
}

export interface ManualMatchRequest {
  gstr2bInvoiceId: string;
  vendorInvoiceId: string;
  notes?: string;
}

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

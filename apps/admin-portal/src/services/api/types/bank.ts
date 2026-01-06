// Bank Account and Transaction types
import type { PaginationParams } from './common';

// Bank Account types
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
  // Ledger integration for BRS
  linkedAccountId?: string;
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
  linkedAccountId?: string;
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
  linkedAccountId?: string;
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
  // Reconciliation difference handling
  reconciliationDifferenceAmount?: number;
  reconciliationDifferenceType?: string;
  reconciliationDifferenceNotes?: string;
  reconciliationTdsSection?: string;
  reconciliationAdjustmentJournalId?: string;
  // Journal Entry linking (hybrid reconciliation for BRS)
  reconciledJournalEntryId?: string;
  reconciledJeLineId?: string;
  // Reversal pairing fields
  pairedTransactionId?: string;
  pairType?: 'original' | 'reversal';
  reversalJournalEntryId?: string;
  isReversalTransaction: boolean;
  importSource: string; // 'manual', 'csv', 'pdf', 'api'
  importBatchId?: string;
  rawData?: string;
  transactionHash?: string;
  createdAt?: string;
  updatedAt?: string;
}

// Reversal Detection Result
export interface ReversalDetectionResult {
  isReversal: boolean;
  detectedPattern?: string;
  confidence: number;
  extractedOriginalReference?: string;
  suggestedOriginals: ReversalMatchSuggestion[];
}

// Suggested original transaction for a reversal
export interface ReversalMatchSuggestion {
  transactionId: string;
  transactionDate: string;
  description?: string;
  referenceNumber?: string;
  amount: number;
  transactionType: string;
  matchScore: number;
  matchReason: string;
  isReconciled: boolean;
  reconciledType?: string;
  reconciledId?: string;
}

// Request to pair a reversal with its original
export interface PairReversalRequest {
  reversalTransactionId: string;
  originalTransactionId: string;
  originalWasPostedToLedger: boolean;
  originalJournalEntryId?: string;
  notes?: string;
  pairedBy?: string;
}

// Result of pairing reversal
export interface PairReversalResult {
  success: boolean;
  originalTransactionId: string;
  reversalTransactionId: string;
  reversalJournalEntryId?: string;
  message: string;
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

// Classification of reconciliation difference per ICAI guidelines
export type ReconciliationDifferenceType =
  | 'bank_interest'   // Interest income credited by bank
  | 'bank_charges'    // Fees/charges deducted by bank
  | 'tds_deducted'    // TDS deducted by customer (receivable)
  | 'round_off'       // Minor rounding difference (typically <₹100)
  | 'forex_gain'      // Foreign exchange gain
  | 'forex_loss'      // Foreign exchange loss
  | 'other_income'    // Other miscellaneous income
  | 'other_expense'   // Other miscellaneous expense
  | 'suspense';       // Park for later investigation

export interface ReconcileTransactionDto {
  reconciledType: string; // 'payment', 'expense', 'payroll', 'tax_payment', 'transfer', 'contractor'
  reconciledId: string;
  reconciledBy?: string;
  // Difference handling (ICAI-compliant bank reconciliation)
  differenceAmount?: number; // Positive = bank received more, Negative = bank received less
  differenceType?: ReconciliationDifferenceType;
  differenceNotes?: string;
  tdsSection?: string; // e.g., '194C', '194J' if difference is TDS
}

export interface ReconciliationSuggestion {
  paymentId: string;
  paymentDate: string;
  amount: number;
  customerName?: string;
  invoiceNumber?: string;
  referenceNumber?: string;
  paymentMethod?: string;
  matchScore: number;
  amountDifference: number;
  dateDifferenceInDays: number;
}

// Bank Reconciliation Statement types
export interface BrsItem {
  id: string;
  date: string;
  description: string;
  referenceNumber?: string;
  amount: number;
  type: string;
}

export interface BankReconciliationStatement {
  bankAccountId: string;
  bankAccountName: string;
  asOfDate: string;
  generatedAt: string;
  bankStatementBalance: number;
  depositsInTransit: number;
  depositsInTransitItems: BrsItem[];
  outstandingCheques: number;
  outstandingChequeItems: BrsItem[];
  adjustedBankBalance: number;
  bookBalance: number;
  bankCreditsNotInBooks: number;
  bankCreditsNotInBooksItems: BrsItem[];
  bankDebitsNotInBooks: number;
  bankDebitsNotInBooksItems: BrsItem[];
  adjustedBookBalance: number;
  difference: number;
  isReconciled: boolean;
  totalTransactions: number;
  reconciledTransactions: number;
  unreconciledTransactions: number;
}

// Reconcile to Journal Entry (for manual JE reconciliation)
export interface ReconcileToJournalDto {
  journalEntryId: string;
  journalEntryLineId: string;
  reconciledBy?: string;
  notes?: string;
  differenceAmount?: number;
  differenceType?: ReconciliationDifferenceType;
  differenceNotes?: string;
  tdsSection?: string;
}

// TDS Summary Item for BRS
export interface TdsSummaryItem {
  section: string;
  description: string;
  transactionCount: number;
  totalAmount: number;
}

export interface DifferenceTypeSummary {
  differenceType: string;
  description: string;
  count: number;
  totalAmount: number;
}

// Enhanced BRS with journal entry perspective
export interface EnhancedBrsReport extends BankReconciliationStatement {
  // Ledger perspective
  ledgerBalance: number;
  hasLedgerLink: boolean;
  linkedAccountId?: string;
  linkedAccountName?: string;
  // TDS summary
  tdsSummary: TdsSummaryItem[];
  totalTdsDeducted: number;
  // Audit metrics
  unlinkedJeCount: number;
  unlinkedJeTransactionIds: string[];
  directJeReconciliationCount: number;
  // Date range
  periodStart?: string;
  periodEnd: string;
  // Differences
  bankToLedgerDifference: number;
  differenceTypeSummary: DifferenceTypeSummary[];
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

// Debit (outgoing payment) reconciliation suggestion
export interface DebitReconciliationSuggestion {
  recordId: string;
  recordType: string; // salary, contractor, expense_claim, subscription, loan_payment, asset_maintenance
  recordTypeDisplay: string;
  paymentDate: string;
  amount: number;
  payeeName?: string;
  description?: string;
  referenceNumber?: string;
  matchScore: number;
  amountDifference: number;
  tdsAmount?: number;
  tdsSection?: string;
  isReconciled: boolean;
  category?: string;
}

// Search request for reconciliation candidates
export interface ReconciliationSearchRequest {
  companyId: string;
  transactionType?: 'credit' | 'debit';
  searchTerm?: string;
  amountMin?: number;
  amountMax?: number;
  dateFrom?: string;
  dateTo?: string;
  recordTypes?: string[];
  category?: string;
  includeReconciled?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

// Unified outgoing payment view
export interface OutgoingPayment {
  id: string;
  type: string;
  typeDisplay: string;
  paymentDate: string;
  amount: number;
  payeeName?: string;
  description?: string;
  displayName: string;
  referenceNumber?: string;
  isReconciled: boolean;
  bankTransactionId?: string;
  reconciledAt?: string;
  tdsAmount?: number;
  tdsSection?: string;
  category?: string;
  status?: string;
}

// Summary of outgoing payments
export interface OutgoingPaymentsSummary {
  totalCount: number;
  reconciledCount: number;
  unreconciledCount: number;
  totalAmount: number;
  reconciledAmount: number;
  unreconciledAmount: number;
  byType: Record<string, OutgoingPaymentTypeBreakdown>;
}

export interface OutgoingPaymentTypeBreakdown {
  typeDisplay: string;
  count: number;
  amount: number;
  reconciledCount: number;
  unreconciledCount: number;
}

// Outgoing payments filter params
export interface OutgoingPaymentsFilterParams {
  pageNumber?: number;
  pageSize?: number;
  reconciled?: boolean;
  types?: string;
  fromDate?: string;
  toDate?: string;
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

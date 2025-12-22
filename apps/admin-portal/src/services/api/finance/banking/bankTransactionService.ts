import { apiClient } from '../../client';
import {
  BankTransaction,
  CreateBankTransactionDto,
  UpdateBankTransactionDto,
  ReconcileTransactionDto,
  ReconciliationSuggestion,
  DebitReconciliationSuggestion,
  ReconciliationSearchRequest,
  ImportBankTransactionsRequest,
  ImportBankTransactionsResult,
  BankTransactionSummary,
  BankTransactionFilterParams,
  PagedResponse,
  ReversalDetectionResult,
  ReversalMatchSuggestion,
  PairReversalRequest,
  PairReversalResult,
  ReconcileToJournalDto,
  BankReconciliationStatement,
  EnhancedBrsReport
} from '../../types';

export class BankTransactionService {
  private readonly endpoint = 'banktransactions';

  async getPaged(params?: BankTransactionFilterParams): Promise<PagedResponse<BankTransaction>> {
    return apiClient.getPaged<BankTransaction>(this.endpoint, params);
  }

  async getAll(): Promise<BankTransaction[]> {
    return apiClient.get<BankTransaction[]>(this.endpoint);
  }

  async getById(id: string): Promise<BankTransaction> {
    return apiClient.get<BankTransaction>(`${this.endpoint}/${id}`);
  }

  async create(data: CreateBankTransactionDto): Promise<BankTransaction> {
    return apiClient.post<BankTransaction>(this.endpoint, data);
  }

  async update(id: string, data: UpdateBankTransactionDto): Promise<BankTransaction> {
    return apiClient.put<BankTransaction>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  // Get transactions by bank account
  async getByBankAccountId(bankAccountId: string): Promise<BankTransaction[]> {
    return apiClient.get<BankTransaction[]>(`${this.endpoint}/by-account/${bankAccountId}`);
  }

  // Get transactions by bank account with date range
  async getByBankAccountIdWithDateRange(
    bankAccountId: string,
    fromDate: string,
    toDate: string
  ): Promise<BankTransaction[]> {
    const params = new URLSearchParams({ fromDate, toDate });
    return apiClient.get<BankTransaction[]>(
      `${this.endpoint}/by-account/${bankAccountId}/date-range?${params.toString()}`
    );
  }

  // Get all unreconciled transactions
  async getUnreconciled(): Promise<BankTransaction[]> {
    return apiClient.get<BankTransaction[]>(`${this.endpoint}/unreconciled`);
  }

  // Reconcile a transaction
  async reconcile(id: string, data: ReconcileTransactionDto): Promise<void> {
    return apiClient.post(`${this.endpoint}/${id}/reconcile`, data);
  }

  // Unreconcile a transaction
  async unreconcile(id: string): Promise<void> {
    return apiClient.post(`${this.endpoint}/${id}/unreconcile`, {});
  }

  // Get reconciliation suggestions for a transaction
  async getReconciliationSuggestions(
    id: string,
    tolerance?: number,
    maxResults?: number
  ): Promise<ReconciliationSuggestion[]> {
    const params = new URLSearchParams();
    if (tolerance !== undefined) params.append('tolerance', tolerance.toString());
    if (maxResults !== undefined) params.append('maxResults', maxResults.toString());
    const query = params.toString();
    return apiClient.get<ReconciliationSuggestion[]>(
      `${this.endpoint}/${id}/reconciliation-suggestions${query ? `?${query}` : ''}`
    );
  }

  // Import transactions from CSV
  async importTransactions(request: ImportBankTransactionsRequest): Promise<ImportBankTransactionsResult> {
    return apiClient.post<ImportBankTransactionsResult>(`${this.endpoint}/import`, request);
  }

  // Get transactions by import batch
  async getByBatchId(batchId: string): Promise<BankTransaction[]> {
    return apiClient.get<BankTransaction[]>(`${this.endpoint}/by-batch/${batchId}`);
  }

  // Delete all transactions in an import batch
  async deleteBatch(batchId: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/batch/${batchId}`);
  }

  // Get transaction summary for a bank account
  async getSummary(bankAccountId: string): Promise<BankTransactionSummary> {
    return apiClient.get<BankTransactionSummary>(`${this.endpoint}/summary/${bankAccountId}`);
  }

  // Get debit reconciliation suggestions for a transaction (outgoing payments)
  async getDebitReconciliationSuggestions(
    id: string,
    tolerance?: number,
    maxResults?: number
  ): Promise<DebitReconciliationSuggestion[]> {
    const params = new URLSearchParams();
    if (tolerance !== undefined) params.append('tolerance', tolerance.toString());
    if (maxResults !== undefined) params.append('maxResults', maxResults.toString());
    const query = params.toString();
    return apiClient.get<DebitReconciliationSuggestion[]>(
      `${this.endpoint}/${id}/debit-reconciliation-suggestions${query ? `?${query}` : ''}`
    );
  }

  // Search for reconciliation candidates with filters
  async searchReconciliationCandidates(
    request: ReconciliationSearchRequest
  ): Promise<PagedResponse<DebitReconciliationSuggestion>> {
    return apiClient.post<PagedResponse<DebitReconciliationSuggestion>>(
      `${this.endpoint}/search-reconciliation-candidates`,
      request
    );
  }

  // Search payments for credit reconciliation (incoming payments)
  async searchPayments(params: {
    companyId: string;
    searchTerm?: string;
    amountMin?: number;
    amountMax?: number;
    maxResults?: number;
  }): Promise<ReconciliationSuggestion[]> {
    const queryParams = new URLSearchParams();
    queryParams.append('companyId', params.companyId);
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params.amountMin !== undefined) queryParams.append('amountMin', params.amountMin.toString());
    if (params.amountMax !== undefined) queryParams.append('amountMax', params.amountMax.toString());
    if (params.maxResults !== undefined) queryParams.append('maxResults', params.maxResults.toString());

    return apiClient.get<ReconciliationSuggestion[]>(
      `${this.endpoint}/search-payments?${queryParams.toString()}`
    );
  }

  // ==================== Reversal Pairing Methods ====================

  // Detect if a transaction is a reversal and get suggested originals
  async detectReversal(id: string): Promise<ReversalDetectionResult> {
    return apiClient.get<ReversalDetectionResult>(`${this.endpoint}/${id}/detect-reversal`);
  }

  // Find potential original transactions for a reversal
  async findPotentialOriginals(
    id: string,
    maxDaysBack?: number,
    maxResults?: number
  ): Promise<ReversalMatchSuggestion[]> {
    const params = new URLSearchParams();
    if (maxDaysBack !== undefined) params.append('maxDaysBack', maxDaysBack.toString());
    if (maxResults !== undefined) params.append('maxResults', maxResults.toString());
    const query = params.toString();
    return apiClient.get<ReversalMatchSuggestion[]>(
      `${this.endpoint}/${id}/potential-originals${query ? `?${query}` : ''}`
    );
  }

  // Pair a reversal transaction with its original
  async pairReversal(request: PairReversalRequest): Promise<PairReversalResult> {
    return apiClient.post<PairReversalResult>(`${this.endpoint}/pair-reversal`, request);
  }

  // Unpair a reversal from its original
  async unpairReversal(id: string): Promise<void> {
    return apiClient.post(`${this.endpoint}/${id}/unpair-reversal`, {});
  }

  // Get all unpaired reversal transactions
  async getUnpairedReversals(bankAccountId?: string): Promise<BankTransaction[]> {
    const params = bankAccountId ? `?bankAccountId=${bankAccountId}` : '';
    return apiClient.get<BankTransaction[]>(`${this.endpoint}/unpaired-reversals${params}`);
  }

  // ==================== Journal Entry Reconciliation ====================

  // Reconcile a bank transaction directly to a journal entry
  async reconcileToJournal(id: string, data: ReconcileToJournalDto): Promise<void> {
    return apiClient.post(`${this.endpoint}/${id}/reconcile-to-journal`, data);
  }

  // ==================== Bank Reconciliation Statement (BRS) ====================

  // Generate basic BRS for a bank account
  async getBrs(bankAccountId: string, asOfDate: string): Promise<BankReconciliationStatement> {
    const params = new URLSearchParams({ asOfDate });
    return apiClient.get<BankReconciliationStatement>(
      `${this.endpoint}/brs/${bankAccountId}?${params.toString()}`
    );
  }

  // Generate enhanced BRS with ledger balance, TDS summary, and audit metrics
  async getEnhancedBrs(
    bankAccountId: string,
    asOfDate?: string,
    periodStart?: string
  ): Promise<EnhancedBrsReport> {
    const params = new URLSearchParams();
    if (asOfDate) params.append('asOfDate', asOfDate);
    if (periodStart) params.append('periodStart', periodStart);
    const query = params.toString();
    return apiClient.get<EnhancedBrsReport>(
      `${this.endpoint}/brs/${bankAccountId}/enhanced${query ? `?${query}` : ''}`
    );
  }
}

export const bankTransactionService = new BankTransactionService();

// CSV parsing utility for bank statement import
export interface ParsedBankTransaction {
  transactionDate: string;
  valueDate?: string;
  description?: string;
  referenceNumber?: string;
  chequeNumber?: string;
  transactionType: 'credit' | 'debit';
  amount: number;
  balanceAfter?: number;
  rawData: string;
}

export interface CSVParseResult {
  transactions: ParsedBankTransaction[];
  errors: string[];
  rowCount: number;
}

// Clean Excel-style escaped values like ="value" or =""value""
export function cleanExcelValue(value: string | undefined): string {
  if (!value) return '';
  // Remove ="..." wrapper and extra quotes
  let cleaned = value.trim();
  if (cleaned.startsWith('="') && cleaned.endsWith('"')) {
    cleaned = cleaned.slice(2, -1);
  }
  // Remove any remaining doubled quotes
  cleaned = cleaned.replace(/""/g, '"').replace(/^"|"$/g, '');
  return cleaned.trim();
}

// Common Indian bank CSV column mappings
export interface BankCSVMapping {
  dateColumn: string;
  descriptionColumn: string;
  chequeColumn: string;
  valueColumn?: string;
  withdrawalColumn?: string;
  depositColumn?: string;
  amountColumn?: string;      // For banks with single amount column
  drCrColumn?: string;        // For banks with Dr/Cr indicator
  balanceColumn: string;
  dateFormat: string;
  skipRows?: number;          // Number of header rows to skip
  hasExcelEscaping?: boolean; // Whether values are Excel-escaped like ="value"
}

export const BANK_CSV_MAPPINGS: Record<string, BankCSVMapping> = {
  HDFC: {
    dateColumn: 'Date',
    descriptionColumn: 'Narration',
    chequeColumn: 'Chq./Ref.No.',
    valueColumn: 'Value Dt',
    withdrawalColumn: 'Withdrawal Amt.',
    depositColumn: 'Deposit Amt.',
    balanceColumn: 'Closing Balance',
    dateFormat: 'DD/MM/YY'
  },
  ICICI: {
    dateColumn: 'Transaction Date',
    descriptionColumn: 'Transaction Remarks',
    chequeColumn: 'Cheque Number',
    valueColumn: 'Value Date',
    withdrawalColumn: 'Withdrawal Amount (INR)',
    depositColumn: 'Deposit Amount (INR)',
    balanceColumn: 'Balance (INR)',
    dateFormat: 'DD-MM-YYYY'
  },
  SBI: {
    dateColumn: 'Txn Date',
    descriptionColumn: 'Description',
    chequeColumn: 'Ref No./Cheque No.',
    valueColumn: 'Value Date',
    withdrawalColumn: 'Debit',
    depositColumn: 'Credit',
    balanceColumn: 'Balance',
    dateFormat: 'DD MMM YYYY'
  },
  AXIS: {
    dateColumn: 'Tran Date',
    descriptionColumn: 'Particulars',
    chequeColumn: 'Chq No',
    valueColumn: 'Value Date',
    withdrawalColumn: 'DR Amount',
    depositColumn: 'CR Amount',
    balanceColumn: 'Balance',
    dateFormat: 'DD-MM-YYYY'
  },
  KOTAK: {
    dateColumn: 'Date',
    descriptionColumn: 'Description',
    chequeColumn: 'Chq / Ref number',
    amountColumn: 'Amount',
    drCrColumn: 'Dr / Cr',
    balanceColumn: 'Balance',
    dateFormat: 'DD/MM/YYYY',
    hasExcelEscaping: true
  }
};

export type BankType = keyof typeof BANK_CSV_MAPPINGS;

// Parse amount string from bank CSV (handles commas, currency symbols)
export function parseAmountString(value: string | undefined): number {
  if (!value || value.trim() === '' || value === '-') return 0;
  // Remove currency symbols, commas, spaces
  const cleaned = value.replace(/[^0-9.-]/g, '');
  const parsed = parseFloat(cleaned);
  return isNaN(parsed) ? 0 : Math.abs(parsed);
}

// Parse date string based on bank format
export function parseBankDate(dateStr: string, format: string): string {
  if (!dateStr || dateStr.trim() === '') return '';

  const parts = dateStr.trim().split(/[-/\s]+/);

  if (format === 'DD/MM/YY' || format === 'DD-MM-YY') {
    const [day, month, year] = parts;
    const fullYear = parseInt(year) > 50 ? `19${year}` : `20${year}`;
    return `${fullYear}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
  }

  if (format === 'DD/MM/YYYY' || format === 'DD-MM-YYYY') {
    const [day, month, year] = parts;
    return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
  }

  if (format === 'DD MMM YYYY') {
    const [day, monthStr, year] = parts;
    const months: Record<string, string> = {
      Jan: '01', Feb: '02', Mar: '03', Apr: '04', May: '05', Jun: '06',
      Jul: '07', Aug: '08', Sep: '09', Oct: '10', Nov: '11', Dec: '12'
    };
    const month = months[monthStr] || '01';
    return `${year}-${month}-${day.padStart(2, '0')}`;
  }

  // Fallback: try ISO format
  return dateStr;
}

// Get value from row, optionally cleaning Excel escaping
function getRowValue(row: Record<string, string>, column: string | undefined, cleanExcel: boolean): string {
  if (!column) return '';
  const value = row[column] || '';
  return cleanExcel ? cleanExcelValue(value) : value;
}

// Parse a row from bank CSV
export function parseCSVRow(
  row: Record<string, string>,
  mapping: BankCSVMapping
): ParsedBankTransaction | null {
  const cleanExcel = mapping.hasExcelEscaping ?? false;

  const dateStr = getRowValue(row, mapping.dateColumn, cleanExcel);
  const date = parseBankDate(dateStr, mapping.dateFormat);
  if (!date) return null;

  let transactionType: 'credit' | 'debit';
  let amount: number;

  // Handle banks with single amount column + Dr/Cr indicator (like Kotak)
  if (mapping.amountColumn && mapping.drCrColumn) {
    const amountStr = getRowValue(row, mapping.amountColumn, cleanExcel);
    amount = parseAmountString(amountStr);

    if (amount === 0) return null;

    const drCr = getRowValue(row, mapping.drCrColumn, cleanExcel).toUpperCase();
    transactionType = drCr === 'CR' ? 'credit' : 'debit';
  } else {
    // Handle banks with separate withdrawal/deposit columns
    const withdrawal = parseAmountString(getRowValue(row, mapping.withdrawalColumn, cleanExcel));
    const deposit = parseAmountString(getRowValue(row, mapping.depositColumn, cleanExcel));

    // Skip rows with no amount
    if (withdrawal === 0 && deposit === 0) return null;

    transactionType = deposit > 0 ? 'credit' : 'debit';
    amount = deposit > 0 ? deposit : withdrawal;
  }

  const description = getRowValue(row, mapping.descriptionColumn, cleanExcel);
  const chequeNumber = getRowValue(row, mapping.chequeColumn, cleanExcel);
  const balanceStr = getRowValue(row, mapping.balanceColumn, cleanExcel);
  const valueDate = mapping.valueColumn
    ? parseBankDate(getRowValue(row, mapping.valueColumn, cleanExcel), mapping.dateFormat)
    : undefined;

  return {
    transactionDate: date,
    valueDate: valueDate || undefined,
    description: description || undefined,
    chequeNumber: chequeNumber || undefined,
    transactionType,
    amount,
    balanceAfter: parseAmountString(balanceStr) || undefined,
    rawData: JSON.stringify(row)
  };
}

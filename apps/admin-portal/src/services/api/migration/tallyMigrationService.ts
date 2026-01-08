import { apiClient } from '../index';

// Types for Tally Migration
export interface TallyUploadResponse {
  batchId: string;
  batchNumber: string;
  status: string;
  parsedData?: TallyParsedData;
  totalLedgers: number;
  totalStockItems: number;
  totalVouchers: number;
  totalCostCenters: number;
  validationIssues: TallyValidationIssue[];
  canProceed: boolean;
}

export interface TallyParsedData {
  masters: TallyMastersSummary;
  vouchers: TallyVouchersSummary;
  fileName?: string;
  fileSize?: number;
  format: string;
  parsedAt: string;
  parseDurationMs: number;
  validationIssues: TallyValidationIssue[];
  hasErrors: boolean;
  hasWarnings: boolean;
}

export interface TallyMastersSummary {
  ledgers: TallyLedger[];
  stockGroups: TallyStockGroup[];
  stockItems: TallyStockItem[];
  godowns: TallyGodown[];
  units: TallyUnit[];
  costCenters: TallyCostCenter[];
  costCategories: TallyCostCategory[];
  currencies: TallyCurrency[];
  voucherTypes: TallyVoucherType[];
  ledgerCountsByGroup: Record<string, number>;
  tallyCompanyName?: string;
  tallyCompanyGuid?: string;
  financialYearFrom?: string;
  financialYearTo?: string;
}

export interface TallyVouchersSummary {
  vouchers: TallyVoucher[];
  salesCount: number;
  purchaseCount: number;
  receiptCount: number;
  paymentCount: number;
  journalCount: number;
  contraCount: number;
  creditNoteCount: number;
  debitNoteCount: number;
  stockJournalCount: number;
  physicalStockCount: number;
  deliveryNoteCount: number;
  receiptNoteCount: number;
  otherCount: number;
  totalSalesAmount: number;
  totalPurchaseAmount: number;
  totalReceiptAmount: number;
  totalPaymentAmount: number;
  minDate?: string;
  maxDate?: string;
  countsByVoucherType: Record<string, number>;
  amountsByVoucherType: Record<string, number>;
}

export interface TallyLedger {
  guid: string;
  name: string;
  parent?: string;
  ledgerGroup?: string;
  openingBalance: number;
  closingBalance: number;
  gstin?: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  stateName?: string;
  targetEntity?: string;
  targetId?: string;
}

export interface TallyStockGroup {
  guid: string;
  name: string;
  parent?: string;
  targetId?: string;
}

export interface TallyStockItem {
  guid: string;
  name: string;
  stockGroup?: string;
  baseUnits?: string;
  hsnCode?: string;
  openingQuantity: number;
  openingValue: number;
  targetId?: string;
}

export interface TallyGodown {
  guid: string;
  name: string;
  parent?: string;
  address?: string;
  targetId?: string;
}

export interface TallyUnit {
  guid: string;
  name: string;
  symbol: string;
  isSimpleUnit: boolean;
  targetId?: string;
}

export interface TallyCostCenter {
  guid: string;
  name: string;
  parent?: string;
  category?: string;
  targetId?: string;
  targetTagGroup?: string;
}

export interface TallyCostCategory {
  guid: string;
  name: string;
  targetTagGroup?: string;
}

export interface TallyCurrency {
  guid: string;
  name: string;
  symbol: string;
  isoCode: string;
}

export interface TallyVoucherType {
  guid: string;
  name: string;
  parent?: string;
}

export interface TallyVoucher {
  guid: string;
  voucherNumber: string;
  voucherType: string;
  date: string;
  partyLedgerName?: string;
  amount: number;
  narration?: string;
  ledgerEntries: TallyLedgerEntry[];
  inventoryEntries: TallyInventoryEntry[];
  targetEntity?: string;
  targetId?: string;
}

export interface TallyLedgerEntry {
  ledgerName: string;
  amount: number;
  isDebit: boolean;
}

export interface TallyInventoryEntry {
  stockItemName: string;
  quantity: number;
  rate: number;
  amount: number;
}

export interface TallyValidationIssue {
  severity: 'error' | 'warning' | 'info';
  code: string;
  message: string;
  recordType?: string;
  recordName?: string;
  recordGuid?: string;
  field?: string;
}

export interface TallyMappingConfig {
  batchId: string;
  groupMappings: TallyGroupMapping[];
  ledgerMappings: TallyLedgerMapping[];
  costCategoryMappings: TallyCostCategoryMapping[];
  createSuspenseAccounts: boolean;
  skipUnmapped: boolean;
}

export interface TallyGroupMapping {
  tallyGroupName: string;
  targetEntity: string;
  targetAccountType?: string;
  targetAccountId?: string;
}

export interface TallyLedgerMapping {
  tallyLedgerName: string;
  tallyGroupName: string;
  targetEntity: string;
  targetAccountId?: string;
  targetCustomerId?: string;
  targetVendorId?: string;
}

export interface TallyCostCategoryMapping {
  tallyCostCategoryName: string;
  targetTagGroup: string;
}

export interface TallyImportRequest {
  batchId: string;
  recordTypes?: string[];
  fromDate?: string;
  toDate?: string;
  createJournalEntries: boolean;
  updateStockQuantities: boolean;
}

export interface TallyImportProgress {
  batchId: string;
  status: string;
  currentPhase: string;
  totalMasters: number;
  processedMasters: number;
  successfulMasters: number;
  failedMasters: number;
  totalVouchers: number;
  processedVouchers: number;
  successfulVouchers: number;
  failedVouchers: number;
  percentComplete: number;
  currentItem?: string;
  lastError?: string;
  startedAt?: string;
  elapsedSeconds: number;
  estimatedRemainingSeconds?: number;
}

export interface TallyImportResult {
  batchId: string;
  batchNumber: string;
  status: string;
  success: boolean;
  totalRecords: number;
  importedRecords: number;
  skippedRecords: number;
  failedRecords: number;
  suspenseRecords: number;
  ledgers: TallyImportCounts;
  stockItems: TallyImportCounts;
  stockGroups: TallyImportCounts;
  godowns: TallyImportCounts;
  costCenters: TallyImportCounts;
  vouchers: TallyImportCounts;
  voucherCountsByType: Record<string, number>;
  suspenseTotalAmount: number;
  suspenseItems: TallySuspenseItem[];
  errors: TallyImportError[];
  startedAt: string;
  completedAt: string;
  durationSeconds: number;
  totalDebitAmount: number;
  totalCreditAmount: number;
  imbalance: number;
}

export interface TallyImportCounts {
  total: number;
  imported: number;
  skipped: number;
  failed: number;
  suspense: number;
}

export interface TallySuspenseItem {
  recordType: string;
  tallyName: string;
  tallyGroupName?: string;
  amount: number;
  reason: string;
  suspenseAccountId?: string;
}

export interface TallyImportError {
  recordType: string;
  tallyGuid?: string;
  tallyName?: string;
  errorCode: string;
  errorMessage: string;
}

export interface TallyBatchListItem {
  id: string;
  batchNumber: string;
  importType: string;
  status: string;
  tallyCompanyName?: string;
  sourceFileName?: string;
  tallyFromDate?: string;
  tallyToDate?: string;
  importedLedgers: number;
  importedVouchers: number;
  importCompletedAt?: string;
  createdAt: string;
  errorMessage?: string;
}

export interface TallyBatchDetail {
  id: string;
  batchNumber: string;
  importType: string;
  status: string;
  sourceFileName?: string;
  sourceFileSize?: number;
  sourceFormat: string;
  tallyCompanyName?: string;
  tallyCompanyGuid?: string;
  tallyFromDate?: string;
  tallyToDate?: string;
  ledgers: TallyImportCounts;
  stockItems: TallyImportCounts;
  stockGroups: TallyImportCounts;
  godowns: TallyImportCounts;
  units: TallyImportCounts;
  costCenters: TallyImportCounts;
  vouchers: TallyImportCounts;
  suspenseEntriesCreated: number;
  suspenseTotalAmount: number;
  uploadStartedAt?: string;
  parsingCompletedAt?: string;
  importStartedAt?: string;
  importCompletedAt?: string;
  errorMessage?: string;
  createdAt: string;
  createdByName?: string;
}

export interface TallyRollbackRequest {
  batchId: string;
  deleteMasters: boolean;
  deleteTransactions: boolean;
  reason?: string;
}

export interface TallyRollbackPreview {
  batchId: string;
  canRollback: boolean;
  blockingReason?: string;
  customersCount: number;
  vendorsCount: number;
  accountsCount: number;
  stockItemsCount: number;
  invoicesCount: number;
  paymentsCount: number;
  journalEntriesCount: number;
  dependentTransactionsCount: number;
  warnings: string[];
}

export interface TallyRollbackResult {
  batchId: string;
  success: boolean;
  mastersDeleted: number;
  transactionsDeleted: number;
  journalEntriesDeleted: number;
  errors: string[];
  rolledBackAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// API functions
export const tallyMigrationApi = {
  /**
   * Upload and parse a Tally export file
   */
  upload: async (companyId: string, file: File, importType: string = 'full'): Promise<TallyUploadResponse> => {
    const formData = new FormData();
    formData.append('file', file);

    return apiClient.post<TallyUploadResponse>(
      `/migration/tally/upload?companyId=${companyId}&importType=${importType}`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
  },

  /**
   * Get preview of parsed data
   */
  getPreview: async (batchId: string): Promise<TallyParsedData> => {
    return apiClient.get<TallyParsedData>(`/migration/tally/${batchId}/preview`);
  },

  /**
   * Configure field mappings
   */
  configureMappings: async (batchId: string, config: Omit<TallyMappingConfig, 'batchId'>): Promise<void> => {
    await apiClient.put(`/migration/tally/${batchId}/mappings`, { ...config, batchId });
  },

  /**
   * Start the import process
   */
  startImport: async (batchId: string, request?: Partial<TallyImportRequest>): Promise<TallyImportResult> => {
    return apiClient.post<TallyImportResult>(
      `/migration/tally/${batchId}/import`,
      { ...request, batchId }
    );
  },

  /**
   * Get current import progress
   */
  getProgress: async (batchId: string): Promise<TallyImportProgress> => {
    return apiClient.get<TallyImportProgress>(`/migration/tally/${batchId}/progress`);
  },

  /**
   * Get import result
   */
  getResult: async (batchId: string): Promise<TallyImportResult> => {
    return apiClient.get<TallyImportResult>(`/migration/tally/${batchId}/result`);
  },

  /**
   * Preview rollback
   */
  previewRollback: async (batchId: string): Promise<TallyRollbackPreview> => {
    return apiClient.get<TallyRollbackPreview>(`/migration/tally/${batchId}/rollback/preview`);
  },

  /**
   * Rollback an import batch
   */
  rollback: async (batchId: string, request?: Partial<TallyRollbackRequest>): Promise<TallyRollbackResult> => {
    return apiClient.post<TallyRollbackResult>(
      `/migration/tally/${batchId}/rollback`,
      { ...request, batchId }
    );
  },

  /**
   * Get list of batches
   */
  getBatches: async (
    companyId: string,
    pageNumber: number = 1,
    pageSize: number = 20,
    status?: string
  ): Promise<PagedResult<TallyBatchListItem>> => {
    const params = new URLSearchParams({
      companyId,
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });
    if (status) params.append('status', status);

    return apiClient.get<PagedResult<TallyBatchListItem>>(
      `/migration/tally/batches?${params.toString()}`
    );
  },

  /**
   * Get batch details
   */
  getBatchDetail: async (batchId: string): Promise<TallyBatchDetail> => {
    return apiClient.get<TallyBatchDetail>(`/migration/tally/${batchId}`);
  },

  /**
   * Get import logs
   */
  getLogs: async (
    batchId: string,
    pageNumber: number = 1,
    pageSize: number = 50,
    recordType?: string,
    status?: string
  ): Promise<PagedResult<TallyImportError>> => {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });
    if (recordType) params.append('recordType', recordType);
    if (status) params.append('status', status);

    return apiClient.get<PagedResult<TallyImportError>>(
      `/migration/tally/${batchId}/logs?${params.toString()}`
    );
  },
};

export default tallyMigrationApi;

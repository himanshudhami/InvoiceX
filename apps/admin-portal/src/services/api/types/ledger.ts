// General Ledger types
import type { PaginationParams } from './common';

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

// Abnormal Balance Report
export interface AbnormalBalanceItem {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountType: AccountType;
  accountSubtype?: string;
  expectedBalanceSide: NormalBalance;
  actualBalanceSide: string;
  amount: number;
  category: string;
  possibleReason: string;
  recommendedAction: string;
  isContraAccount: boolean;
  severity: 'info' | 'warning';
}

export interface AbnormalBalanceCategorySummary {
  categoryName: string;
  count: number;
  totalAmount: number;
  severity: string;
}

export interface AbnormalBalanceReport {
  companyId: string;
  generatedAt: string;
  totalAbnormalAccounts: number;
  actionableIssues: number;
  totalAbnormalAmount: number;
  items: AbnormalBalanceItem[];
  categorySummary: AbnormalBalanceCategorySummary[];
}

export interface AbnormalBalanceAlertSummary {
  companyId: string;
  hasIssues: boolean;
  totalIssues: number;
  criticalIssues: number;
  totalAmount: number;
  alertMessage: string;
  alertSeverity: 'success' | 'warning' | 'error' | 'info';
  topCategories: AbnormalBalanceCategorySummary[];
}

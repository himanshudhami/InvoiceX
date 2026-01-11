import type { ChartOfAccountsFilterParams, JournalEntriesFilterParams } from '@/services/api/types'

/**
 * Query keys for ledger-related queries
 * Following TanStack Query best practices for cache management
 */
export const ledgerKeys = {
  // Base keys
  all: ['ledger'] as const,

  // Chart of Accounts
  accounts: () => [...ledgerKeys.all, 'accounts'] as const,
  accountsList: (companyId?: string) => [...ledgerKeys.accounts(), 'list', { companyId }] as const,
  accountsPaged: (params?: ChartOfAccountsFilterParams) => [...ledgerKeys.accounts(), 'paged', params] as const,
  accountDetail: (id: string) => [...ledgerKeys.accounts(), 'detail', id] as const,

  // Journal Entries
  journals: () => [...ledgerKeys.all, 'journals'] as const,
  journalsList: (companyId?: string) => [...ledgerKeys.journals(), 'list', { companyId }] as const,
  journalsPaged: (params?: JournalEntriesFilterParams) => [...ledgerKeys.journals(), 'paged', params] as const,
  journalDetail: (id: string) => [...ledgerKeys.journals(), 'detail', id] as const,

  // Posting Rules
  rules: () => [...ledgerKeys.all, 'rules'] as const,
  rulesList: (companyId?: string) => [...ledgerKeys.rules(), 'list', { companyId }] as const,
  ruleDetail: (id: string) => [...ledgerKeys.rules(), 'detail', id] as const,

  // Reports
  reports: () => [...ledgerKeys.all, 'reports'] as const,
  trialBalance: (companyId: string, asOfDate: string, includeZeroBalances?: boolean) =>
    [...ledgerKeys.reports(), 'trial-balance', { companyId, asOfDate, includeZeroBalances }] as const,
  trialBalanceByPeriod: (companyId: string, financialYear: string, periodMonth?: number) =>
    [...ledgerKeys.reports(), 'trial-balance-period', { companyId, financialYear, periodMonth }] as const,
  incomeStatement: (companyId: string, fromDate: string, toDate: string) =>
    [...ledgerKeys.reports(), 'income-statement', { companyId, fromDate, toDate }] as const,
  balanceSheet: (companyId: string, asOfDate: string) =>
    [...ledgerKeys.reports(), 'balance-sheet', { companyId, asOfDate }] as const,
  accountLedger: (accountId: string, fromDate: string, toDate: string) =>
    [...ledgerKeys.reports(), 'account-ledger', { accountId, fromDate, toDate }] as const,

  // Data Quality Reports
  dataQuality: () => [...ledgerKeys.all, 'data-quality'] as const,
  abnormalBalances: (companyId: string) =>
    [...ledgerKeys.dataQuality(), 'abnormal-balances', { companyId }] as const,
  abnormalBalanceAlert: (companyId: string) =>
    [...ledgerKeys.dataQuality(), 'abnormal-balance-alert', { companyId }] as const,
}

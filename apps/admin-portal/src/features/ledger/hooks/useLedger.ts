import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

import { ledgerService } from '@/services/api/finance/ledger/ledgerService'
import type {
  ChartOfAccount,
  CreateChartOfAccountDto,
  UpdateChartOfAccountDto,
  JournalEntry,
  CreateJournalEntryDto,
  ChartOfAccountsFilterParams,
  JournalEntriesFilterParams,
} from '@/services/api/types'
import { ledgerKeys } from './ledgerKeys'

// ==================== Chart of Accounts Hooks ====================

/**
 * Fetch all accounts for a company
 */
export const useAccounts = (companyId?: string) => {
  return useQuery({
    queryKey: ledgerKeys.accountsList(companyId),
    queryFn: () => ledgerService.getAccounts(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Fetch paginated accounts with server-side filtering
 */
export const useAccountsPaged = (params: ChartOfAccountsFilterParams = {}) => {
  return useQuery({
    queryKey: ledgerKeys.accountsPaged(params),
    queryFn: () => ledgerService.getAccountsPaged(params),
    placeholderData: (prev) => prev,
    staleTime: 30 * 1000,
  })
}

/**
 * Fetch single account by ID
 */
export const useAccount = (id: string, enabled = true) => {
  return useQuery({
    queryKey: ledgerKeys.accountDetail(id),
    queryFn: () => ledgerService.getAccountById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Create account mutation
 */
export const useCreateAccount = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateChartOfAccountDto) => ledgerService.createAccount(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ledgerKeys.accounts() })
    },
    onError: (error) => {
      console.error('Failed to create account:', error)
    },
  })
}

/**
 * Update account mutation
 */
export const useUpdateAccount = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateChartOfAccountDto }) =>
      ledgerService.updateAccount(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ledgerKeys.accountDetail(id) })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.accounts() })
    },
    onError: (error) => {
      console.error('Failed to update account:', error)
    },
  })
}

/**
 * Delete account mutation
 */
export const useDeleteAccount = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => ledgerService.deleteAccount(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ledgerKeys.accounts() })
    },
    onError: (error) => {
      console.error('Failed to delete account:', error)
    },
  })
}

/**
 * Initialize chart of accounts for a company
 */
export const useInitializeChartOfAccounts = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (companyId: string) => ledgerService.initializeChartOfAccounts(companyId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ledgerKeys.accounts() })
    },
    onError: (error) => {
      console.error('Failed to initialize chart of accounts:', error)
    },
  })
}

// ==================== Journal Entries Hooks ====================

/**
 * Fetch all journal entries for a company
 */
export const useJournalEntries = (companyId?: string) => {
  return useQuery({
    queryKey: ledgerKeys.journalsList(companyId),
    queryFn: () => ledgerService.getJournalEntries(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Fetch paginated journal entries with server-side filtering
 */
export const useJournalEntriesPaged = (params: JournalEntriesFilterParams = {}) => {
  return useQuery({
    queryKey: ledgerKeys.journalsPaged(params),
    queryFn: () => ledgerService.getJournalEntriesPaged(params),
    placeholderData: (prev) => prev,
    staleTime: 30 * 1000,
  })
}

/**
 * Fetch single journal entry by ID (includes lines)
 */
export const useJournalEntry = (id: string, enabled = true) => {
  return useQuery({
    queryKey: ledgerKeys.journalDetail(id),
    queryFn: () => ledgerService.getJournalEntryById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Create journal entry mutation
 */
export const useCreateJournalEntry = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateJournalEntryDto) => ledgerService.createJournalEntry(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ledgerKeys.journals() })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.reports() })
    },
    onError: (error) => {
      console.error('Failed to create journal entry:', error)
    },
  })
}

/**
 * Post journal entry mutation (change status from draft to posted)
 */
export const usePostJournalEntry = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => ledgerService.postJournalEntry(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ledgerKeys.journalDetail(id) })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.journals() })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.reports() })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.accounts() })
    },
    onError: (error) => {
      console.error('Failed to post journal entry:', error)
    },
  })
}

/**
 * Reverse journal entry mutation
 */
export const useReverseJournalEntry = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      ledgerService.reverseJournalEntry(id, reason),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ledgerKeys.journalDetail(id) })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.journals() })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.reports() })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.accounts() })
    },
    onError: (error) => {
      console.error('Failed to reverse journal entry:', error)
    },
  })
}

// ==================== Reports Hooks ====================

/**
 * Fetch trial balance report
 */
export const useTrialBalance = (
  companyId: string,
  asOfDate: string,
  includeZeroBalances = false,
  enabled = true
) => {
  return useQuery({
    queryKey: ledgerKeys.trialBalance(companyId, asOfDate, includeZeroBalances),
    queryFn: () => ledgerService.getTrialBalance(companyId, asOfDate, includeZeroBalances),
    enabled: enabled && !!companyId && !!asOfDate,
    staleTime: 60 * 1000,
  })
}

/**
 * Fetch trial balance by financial year and period
 */
export const useTrialBalanceByPeriod = (
  companyId: string,
  financialYear: string,
  periodMonth?: number,
  enabled = true
) => {
  return useQuery({
    queryKey: ledgerKeys.trialBalanceByPeriod(companyId, financialYear, periodMonth),
    queryFn: () => ledgerService.getTrialBalanceByPeriod(companyId, financialYear, periodMonth),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 60 * 1000,
  })
}

/**
 * Fetch income statement (P&L) report
 */
export const useIncomeStatement = (
  companyId: string,
  fromDate: string,
  toDate: string,
  enabled = true
) => {
  return useQuery({
    queryKey: ledgerKeys.incomeStatement(companyId, fromDate, toDate),
    queryFn: () => ledgerService.getIncomeStatement(companyId, fromDate, toDate),
    enabled: enabled && !!companyId && !!fromDate && !!toDate,
    staleTime: 60 * 1000,
  })
}

/**
 * Fetch balance sheet report
 */
export const useBalanceSheet = (companyId: string, asOfDate: string, enabled = true) => {
  return useQuery({
    queryKey: ledgerKeys.balanceSheet(companyId, asOfDate),
    queryFn: () => ledgerService.getBalanceSheet(companyId, asOfDate),
    enabled: enabled && !!companyId && !!asOfDate,
    staleTime: 60 * 1000,
  })
}

/**
 * Fetch account ledger report (transaction history for a single account)
 */
export const useAccountLedger = (
  accountId: string,
  fromDate: string,
  toDate: string,
  enabled = true
) => {
  return useQuery({
    queryKey: ledgerKeys.accountLedger(accountId, fromDate, toDate),
    queryFn: () => ledgerService.getAccountLedger(accountId, fromDate, toDate),
    enabled: enabled && !!accountId && !!fromDate && !!toDate,
    staleTime: 60 * 1000,
  })
}

// ==================== Data Quality Reports ====================

/**
 * Fetch abnormal balance report (accounts with balances opposite to normal)
 */
export const useAbnormalBalances = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: ledgerKeys.abnormalBalances(companyId),
    queryFn: () => ledgerService.getAbnormalBalances(companyId),
    enabled: enabled && !!companyId,
    staleTime: 60 * 1000,
  })
}

/**
 * Fetch abnormal balance alert summary for dashboard
 */
export const useAbnormalBalanceAlert = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: ledgerKeys.abnormalBalanceAlert(companyId),
    queryFn: () => ledgerService.getAbnormalBalanceAlert(companyId),
    enabled: enabled && !!companyId,
    staleTime: 60 * 1000,
  })
}

// ==================== Auto-Posting Hooks ====================

/**
 * Auto-post invoice to general ledger
 */
export const usePostInvoice = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ invoiceId, autoPost = true }: { invoiceId: string; autoPost?: boolean }) =>
      ledgerService.postInvoice(invoiceId, autoPost),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ledgerKeys.journals() })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.reports() })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.accounts() })
    },
    onError: (error) => {
      console.error('Failed to post invoice:', error)
    },
  })
}

/**
 * Auto-post payment to general ledger
 */
export const usePostPayment = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ paymentId, autoPost = true }: { paymentId: string; autoPost?: boolean }) =>
      ledgerService.postPayment(paymentId, autoPost),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ledgerKeys.journals() })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.reports() })
      queryClient.invalidateQueries({ queryKey: ledgerKeys.accounts() })
    },
    onError: (error) => {
      console.error('Failed to post payment:', error)
    },
  })
}

// ==================== Subledger Report Hooks ====================

/**
 * Fetch AP aging by vendor
 */
export const useApAging = (companyId: string, asOfDate: string, enabled = true) => {
  return useQuery({
    queryKey: ledgerKeys.apAging(companyId, asOfDate),
    queryFn: () => ledgerService.getApAging(companyId, asOfDate),
    enabled: enabled && !!companyId && !!asOfDate,
    staleTime: 60 * 1000,
  })
}

/**
 * Fetch AR aging by customer
 */
export const useArAging = (companyId: string, asOfDate: string, enabled = true) => {
  return useQuery({
    queryKey: ledgerKeys.arAging(companyId, asOfDate),
    queryFn: () => ledgerService.getArAging(companyId, asOfDate),
    enabled: enabled && !!companyId && !!asOfDate,
    staleTime: 60 * 1000,
  })
}

/**
 * Fetch party ledger (transaction history for a party)
 */
export const usePartyLedger = (
  companyId: string,
  partyType: string,
  partyId: string,
  fromDate: string,
  toDate: string,
  enabled = true
) => {
  return useQuery({
    queryKey: ledgerKeys.partyLedger(companyId, partyType, partyId, fromDate, toDate),
    queryFn: () => ledgerService.getPartyLedger(companyId, partyType, partyId, fromDate, toDate),
    enabled: enabled && !!companyId && !!partyType && !!partyId && !!fromDate && !!toDate,
    staleTime: 60 * 1000,
  })
}

/**
 * Fetch control account reconciliation
 */
export const useControlAccountReconciliation = (companyId: string, asOfDate: string, enabled = true) => {
  return useQuery({
    queryKey: ledgerKeys.controlAccountReconciliation(companyId, asOfDate),
    queryFn: () => ledgerService.getControlAccountReconciliation(companyId, asOfDate),
    enabled: enabled && !!companyId && !!asOfDate,
    staleTime: 60 * 1000,
  })
}

/**
 * Fetch subledger drilldown for a control account
 */
export const useSubledgerDrilldown = (
  companyId: string,
  controlAccountId: string,
  asOfDate: string,
  enabled = true
) => {
  return useQuery({
    queryKey: ledgerKeys.subledgerDrilldown(companyId, controlAccountId, asOfDate),
    queryFn: () => ledgerService.getSubledgerDrilldown(companyId, controlAccountId, asOfDate),
    enabled: enabled && !!companyId && !!controlAccountId && !!asOfDate,
    staleTime: 60 * 1000,
  })
}

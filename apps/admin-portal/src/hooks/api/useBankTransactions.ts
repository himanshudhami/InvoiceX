import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { bankTransactionService } from '@/services/api/finance/banking/bankTransactionService';
import {
  CreateBankTransactionDto,
  UpdateBankTransactionDto,
  ReconcileTransactionDto,
  ReconcileToJournalDto,
  ImportBankTransactionsRequest,
  BankTransactionFilterParams
} from '@/services/api/types';
import { bankAccountKeys } from './useBankAccounts';

// Query keys for React Query cache management
export const bankTransactionKeys = {
  all: ['bankTransactions'] as const,
  lists: () => [...bankTransactionKeys.all, 'list'] as const,
  list: (params: BankTransactionFilterParams) => [...bankTransactionKeys.lists(), params] as const,
  details: () => [...bankTransactionKeys.all, 'detail'] as const,
  detail: (id: string) => [...bankTransactionKeys.details(), id] as const,
  byAccount: (bankAccountId: string) => [...bankTransactionKeys.all, 'byAccount', bankAccountId] as const,
  byAccountDateRange: (bankAccountId: string, fromDate: string, toDate: string) =>
    [...bankTransactionKeys.all, 'byAccount', bankAccountId, fromDate, toDate] as const,
  unreconciled: () => [...bankTransactionKeys.all, 'unreconciled'] as const,
  byBatch: (batchId: string) => [...bankTransactionKeys.all, 'byBatch', batchId] as const,
  summary: (bankAccountId: string) => [...bankTransactionKeys.all, 'summary', bankAccountId] as const,
  suggestions: (id: string) => [...bankTransactionKeys.all, 'suggestions', id] as const,
  debitSuggestions: (id: string) => [...bankTransactionKeys.all, 'debitSuggestions', id] as const,
  // Reversal pairing keys
  reversalDetection: (id: string) => [...bankTransactionKeys.all, 'reversalDetection', id] as const,
  potentialOriginals: (id: string) => [...bankTransactionKeys.all, 'potentialOriginals', id] as const,
  unpairedReversals: (bankAccountId?: string) => [...bankTransactionKeys.all, 'unpairedReversals', bankAccountId] as const,
  // BRS keys
  brs: (bankAccountId: string, asOfDate: string) => [...bankTransactionKeys.all, 'brs', bankAccountId, asOfDate] as const,
  enhancedBrs: (bankAccountId: string, asOfDate?: string, periodStart?: string) =>
    [...bankTransactionKeys.all, 'enhancedBrs', bankAccountId, asOfDate, periodStart] as const,
};

/**
 * Hook for fetching all bank transactions
 */
export const useBankTransactions = () => {
  return useQuery({
    queryKey: bankTransactionKeys.lists(),
    queryFn: () => bankTransactionService.getAll(),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching paginated bank transactions
 */
export const useBankTransactionsPaged = (params: BankTransactionFilterParams = {}) => {
  return useQuery({
    queryKey: bankTransactionKeys.list(params),
    queryFn: () => bankTransactionService.getPaged(params),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
    placeholderData: (previousData) => previousData,
  });
};

/**
 * Hook for fetching a single bank transaction by ID
 */
export const useBankTransaction = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankTransactionKeys.detail(id),
    queryFn: () => bankTransactionService.getById(id),
    enabled: enabled && !!id,
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching transactions by bank account
 */
export const useBankTransactionsByAccount = (bankAccountId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankTransactionKeys.byAccount(bankAccountId),
    queryFn: () => bankTransactionService.getByBankAccountId(bankAccountId),
    enabled: enabled && !!bankAccountId,
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching transactions by bank account with date range
 */
export const useBankTransactionsByAccountDateRange = (
  bankAccountId: string,
  fromDate: string,
  toDate: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: bankTransactionKeys.byAccountDateRange(bankAccountId, fromDate, toDate),
    queryFn: () => bankTransactionService.getByBankAccountIdWithDateRange(bankAccountId, fromDate, toDate),
    enabled: enabled && !!bankAccountId && !!fromDate && !!toDate,
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching unreconciled transactions
 */
export const useUnreconciledTransactions = (enabled: boolean = true) => {
  return useQuery({
    queryKey: bankTransactionKeys.unreconciled(),
    queryFn: () => bankTransactionService.getUnreconciled(),
    enabled,
    staleTime: 1 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching transactions by import batch
 */
export const useBankTransactionsByBatch = (batchId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankTransactionKeys.byBatch(batchId),
    queryFn: () => bankTransactionService.getByBatchId(batchId),
    enabled: enabled && !!batchId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching transaction summary for a bank account
 */
export const useBankTransactionSummary = (bankAccountId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankTransactionKeys.summary(bankAccountId),
    queryFn: () => bankTransactionService.getSummary(bankAccountId),
    enabled: enabled && !!bankAccountId,
    staleTime: 1 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching reconciliation suggestions
 */
export const useReconciliationSuggestions = (
  id: string,
  tolerance?: number,
  maxResults?: number,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: bankTransactionKeys.suggestions(id),
    queryFn: () => bankTransactionService.getReconciliationSuggestions(id, tolerance, maxResults),
    enabled: enabled && !!id,
    staleTime: 30 * 1000,
    gcTime: 60 * 1000,
  });
};

/**
 * Hook for creating a bank transaction
 */
export const useCreateBankTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBankTransactionDto) => bankTransactionService.create(data),
    onSuccess: (newTransaction) => {
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.unreconciled() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.byAccount(newTransaction.bankAccountId) });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.summary(newTransaction.bankAccountId) });
      // Also invalidate bank account to update balance
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to create bank transaction:', error);
    },
  });
};

/**
 * Hook for updating a bank transaction
 */
export const useUpdateBankTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBankTransactionDto }) =>
      bankTransactionService.update(id, data),
    onSuccess: (updatedTransaction, variables) => {
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.byAccount(updatedTransaction.bankAccountId) });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.summary(updatedTransaction.bankAccountId) });
    },
    onError: (error) => {
      console.error('Failed to update bank transaction:', error);
    },
  });
};

/**
 * Hook for reconciling a transaction
 */
export const useReconcileTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ReconcileTransactionDto }) =>
      bankTransactionService.reconcile(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.unreconciled() });
    },
    onError: (error) => {
      console.error('Failed to reconcile transaction:', error);
    },
  });
};

/**
 * Hook for unreconciling a transaction
 */
export const useUnreconcileTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => bankTransactionService.unreconcile(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.unreconciled() });
    },
    onError: (error) => {
      console.error('Failed to unreconcile transaction:', error);
    },
  });
};

/**
 * Hook for importing bank transactions
 */
export const useImportBankTransactions = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ImportBankTransactionsRequest) =>
      bankTransactionService.importTransactions(request),
    onSuccess: (result, request) => {
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.unreconciled() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.byAccount(request.bankAccountId) });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.summary(request.bankAccountId) });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.byBatch(result.batchId) });
      // Also invalidate bank account to update balance
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to import bank transactions:', error);
    },
  });
};

/**
 * Hook for deleting a bank transaction
 */
export const useDeleteBankTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => bankTransactionService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.unreconciled() });
    },
    onError: (error) => {
      console.error('Failed to delete bank transaction:', error);
    },
  });
};

/**
 * Hook for deleting all transactions in a batch
 */
export const useDeleteBankTransactionBatch = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (batchId: string) => bankTransactionService.deleteBatch(batchId),
    onSuccess: (_, batchId) => {
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.unreconciled() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.byBatch(batchId) });
      // Invalidate bank accounts since balances might have changed
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete bank transaction batch:', error);
    },
  });
};

/**
 * Hook for fetching debit reconciliation suggestions (outgoing payments)
 */
export const useDebitReconciliationSuggestions = (
  id: string,
  tolerance?: number,
  maxResults?: number,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: bankTransactionKeys.debitSuggestions(id),
    queryFn: () => bankTransactionService.getDebitReconciliationSuggestions(id, tolerance, maxResults),
    enabled: enabled && !!id,
    staleTime: 30 * 1000,
    gcTime: 60 * 1000,
  });
};

// ==================== Reversal Pairing Hooks ====================

/**
 * Hook for detecting if a transaction is a reversal
 */
export const useReversalDetection = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankTransactionKeys.reversalDetection(id),
    queryFn: () => bankTransactionService.detectReversal(id),
    enabled: enabled && !!id,
    staleTime: 60 * 1000,
    gcTime: 2 * 60 * 1000,
  });
};

/**
 * Hook for finding potential original transactions for a reversal
 */
export const usePotentialOriginals = (
  id: string,
  maxDaysBack?: number,
  maxResults?: number,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: bankTransactionKeys.potentialOriginals(id),
    queryFn: () => bankTransactionService.findPotentialOriginals(id, maxDaysBack, maxResults),
    enabled: enabled && !!id,
    staleTime: 30 * 1000,
    gcTime: 60 * 1000,
  });
};

/**
 * Hook for getting unpaired reversal transactions
 */
export const useUnpairedReversals = (bankAccountId?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankTransactionKeys.unpairedReversals(bankAccountId),
    queryFn: () => bankTransactionService.getUnpairedReversals(bankAccountId),
    enabled,
    staleTime: 30 * 1000,
    gcTime: 60 * 1000,
  });
};

/**
 * Hook for pairing a reversal with its original transaction
 */
export const usePairReversal = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: {
      reversalTransactionId: string;
      originalTransactionId: string;
      originalWasPostedToLedger: boolean;
      originalJournalEntryId?: string;
      notes?: string;
      pairedBy?: string;
    }) => bankTransactionService.pairReversal(request),
    onSuccess: () => {
      // Invalidate all relevant queries
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.all });
    },
    onError: (error) => {
      console.error('Failed to pair reversal:', error);
    },
  });
};

/**
 * Hook for unpairing a reversal from its original
 */
export const useUnpairReversal = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => bankTransactionService.unpairReversal(id),
    onSuccess: () => {
      // Invalidate all relevant queries
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.all });
    },
    onError: (error) => {
      console.error('Failed to unpair reversal:', error);
    },
  });
};

// ==================== Journal Entry Reconciliation Hooks ====================

/**
 * Hook for reconciling a bank transaction directly to a journal entry
 */
export const useReconcileToJournal = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ReconcileToJournalDto }) =>
      bankTransactionService.reconcileToJournal(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankTransactionKeys.unreconciled() });
    },
    onError: (error) => {
      console.error('Failed to reconcile to journal:', error);
    },
  });
};

// ==================== BRS (Bank Reconciliation Statement) Hooks ====================

/**
 * Hook for fetching basic BRS
 */
export const useBrs = (bankAccountId: string, asOfDate: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankTransactionKeys.brs(bankAccountId, asOfDate),
    queryFn: () => bankTransactionService.getBrs(bankAccountId, asOfDate),
    enabled: enabled && !!bankAccountId && !!asOfDate,
    staleTime: 30 * 1000,
    gcTime: 60 * 1000,
  });
};

/**
 * Hook for fetching enhanced BRS with ledger balance, TDS summary, and audit metrics
 */
export const useEnhancedBrs = (
  bankAccountId: string,
  asOfDate?: string,
  periodStart?: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: bankTransactionKeys.enhancedBrs(bankAccountId, asOfDate, periodStart),
    queryFn: () => bankTransactionService.getEnhancedBrs(bankAccountId, asOfDate, periodStart),
    enabled: enabled && !!bankAccountId,
    staleTime: 30 * 1000,
    gcTime: 60 * 1000,
  });
};

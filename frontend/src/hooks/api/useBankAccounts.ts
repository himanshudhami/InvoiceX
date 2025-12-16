import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { bankAccountService } from '@/services/api/bankAccountService';
import {
  CreateBankAccountDto,
  UpdateBankAccountDto,
  UpdateBalanceDto,
  BankAccountFilterParams
} from '@/services/api/types';

// Query keys for React Query cache management
export const bankAccountKeys = {
  all: ['bankAccounts'] as const,
  lists: () => [...bankAccountKeys.all, 'list'] as const,
  list: (params: BankAccountFilterParams) => [...bankAccountKeys.lists(), params] as const,
  details: () => [...bankAccountKeys.all, 'detail'] as const,
  detail: (id: string) => [...bankAccountKeys.details(), id] as const,
  byCompany: (companyId: string) => [...bankAccountKeys.all, 'byCompany', companyId] as const,
  primary: (companyId: string) => [...bankAccountKeys.all, 'primary', companyId] as const,
  active: () => [...bankAccountKeys.all, 'active'] as const,
};

/**
 * Hook for fetching all bank accounts
 */
export const useBankAccounts = () => {
  return useQuery({
    queryKey: bankAccountKeys.lists(),
    queryFn: () => bankAccountService.getAll(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching paginated bank accounts
 */
export const useBankAccountsPaged = (params: BankAccountFilterParams = {}) => {
  return useQuery({
    queryKey: bankAccountKeys.list(params),
    queryFn: () => bankAccountService.getPaged(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    placeholderData: (previousData) => previousData,
  });
};

/**
 * Hook for fetching a single bank account by ID
 */
export const useBankAccount = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankAccountKeys.detail(id),
    queryFn: () => bankAccountService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching bank accounts by company
 */
export const useBankAccountsByCompany = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankAccountKeys.byCompany(companyId),
    queryFn: () => bankAccountService.getByCompanyId(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching primary bank account for a company
 */
export const usePrimaryBankAccount = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bankAccountKeys.primary(companyId),
    queryFn: () => bankAccountService.getPrimaryAccount(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching all active bank accounts
 */
export const useActiveBankAccounts = () => {
  return useQuery({
    queryKey: bankAccountKeys.active(),
    queryFn: () => bankAccountService.getActiveAccounts(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for creating a bank account
 */
export const useCreateBankAccount = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBankAccountDto) => bankAccountService.create(data),
    onSuccess: (newAccount) => {
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.active() });
      if (newAccount.companyId) {
        queryClient.invalidateQueries({ queryKey: bankAccountKeys.byCompany(newAccount.companyId) });
      }
    },
    onError: (error) => {
      console.error('Failed to create bank account:', error);
    },
  });
};

/**
 * Hook for updating a bank account
 */
export const useUpdateBankAccount = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBankAccountDto }) =>
      bankAccountService.update(id, data),
    onSuccess: (updatedAccount, variables) => {
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.active() });
      if (updatedAccount.companyId) {
        queryClient.invalidateQueries({ queryKey: bankAccountKeys.byCompany(updatedAccount.companyId) });
        queryClient.invalidateQueries({ queryKey: bankAccountKeys.primary(updatedAccount.companyId) });
      }
    },
    onError: (error) => {
      console.error('Failed to update bank account:', error);
    },
  });
};

/**
 * Hook for updating bank account balance
 */
export const useUpdateBankAccountBalance = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBalanceDto }) =>
      bankAccountService.updateBalance(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.active() });
    },
    onError: (error) => {
      console.error('Failed to update bank account balance:', error);
    },
  });
};

/**
 * Hook for setting primary bank account
 */
export const useSetPrimaryBankAccount = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ companyId, accountId }: { companyId: string; accountId: string }) =>
      bankAccountService.setPrimaryAccount(companyId, accountId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.byCompany(variables.companyId) });
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.primary(variables.companyId) });
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to set primary bank account:', error);
    },
  });
};

/**
 * Hook for deleting a bank account
 */
export const useDeleteBankAccount = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => bankAccountService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bankAccountKeys.active() });
    },
    onError: (error) => {
      console.error('Failed to delete bank account:', error);
    },
  });
};

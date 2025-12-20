import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  expenseClaimService,
  ExpenseClaim,
  ExpenseClaimFilterParams,
  RejectExpenseClaimDto,
  ReimburseExpenseClaimDto,
} from '@/services/api/expenseClaimService';
import toast from 'react-hot-toast';

// Query keys for React Query cache management
export const expenseClaimKeys = {
  all: ['expenseClaims'] as const,
  lists: () => [...expenseClaimKeys.all, 'list'] as const,
  list: (params: ExpenseClaimFilterParams) => [...expenseClaimKeys.lists(), params] as const,
  details: () => [...expenseClaimKeys.all, 'detail'] as const,
  detail: (id: string) => [...expenseClaimKeys.details(), id] as const,
  attachments: (id: string) => [...expenseClaimKeys.detail(id), 'attachments'] as const,
};

/**
 * Hook for fetching paginated expense claims
 */
export const useExpenseClaimsPaged = (params: ExpenseClaimFilterParams = {}) => {
  return useQuery({
    queryKey: expenseClaimKeys.list(params),
    queryFn: () => expenseClaimService.getPaged(params),
    staleTime: 2 * 60 * 1000, // 2 minutes - expense claims may change frequently
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching a single expense claim by ID
 */
export const useExpenseClaim = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: expenseClaimKeys.detail(id),
    queryFn: () => expenseClaimService.getById(id),
    enabled: enabled && !!id,
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching expense claim attachments
 */
export const useExpenseClaimAttachments = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: expenseClaimKeys.attachments(id),
    queryFn: () => expenseClaimService.getAttachments(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for approving an expense claim
 */
export const useApproveExpenseClaim = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => expenseClaimService.approve(id),
    onSuccess: (updatedClaim) => {
      queryClient.setQueryData(expenseClaimKeys.detail(updatedClaim.id), updatedClaim);
      queryClient.invalidateQueries({ queryKey: expenseClaimKeys.lists() });
      toast.success('Expense claim approved successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to approve expense claim';
      toast.error(message);
    },
  });
};

/**
 * Hook for rejecting an expense claim
 */
export const useRejectExpenseClaim = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: RejectExpenseClaimDto }) =>
      expenseClaimService.reject(id, data),
    onSuccess: (updatedClaim) => {
      queryClient.setQueryData(expenseClaimKeys.detail(updatedClaim.id), updatedClaim);
      queryClient.invalidateQueries({ queryKey: expenseClaimKeys.lists() });
      toast.success('Expense claim rejected');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to reject expense claim';
      toast.error(message);
    },
  });
};

/**
 * Hook for reimbursing an expense claim
 */
export const useReimburseExpenseClaim = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ReimburseExpenseClaimDto }) =>
      expenseClaimService.reimburse(id, data),
    onSuccess: (updatedClaim) => {
      queryClient.setQueryData(expenseClaimKeys.detail(updatedClaim.id), updatedClaim);
      queryClient.invalidateQueries({ queryKey: expenseClaimKeys.lists() });
      toast.success('Expense claim marked as reimbursed');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to reimburse expense claim';
      toast.error(message);
    },
  });
};

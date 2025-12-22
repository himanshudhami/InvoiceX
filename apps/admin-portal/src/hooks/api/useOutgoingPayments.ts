import { useQuery } from '@tanstack/react-query';
import { outgoingPaymentsService } from '@/services/api/finance/outgoingPaymentsService';
import { OutgoingPaymentsFilterParams } from '@/services/api/types';

// Query keys for React Query cache management
export const outgoingPaymentsKeys = {
  all: ['outgoingPayments'] as const,
  lists: () => [...outgoingPaymentsKeys.all, 'list'] as const,
  list: (companyId: string, params?: OutgoingPaymentsFilterParams) =>
    [...outgoingPaymentsKeys.lists(), companyId, params] as const,
  toReconcile: (companyId: string, params?: OutgoingPaymentsFilterParams) =>
    [...outgoingPaymentsKeys.all, 'toReconcile', companyId, params] as const,
  summary: (companyId: string, fromDate?: string, toDate?: string) =>
    [...outgoingPaymentsKeys.all, 'summary', companyId, fromDate, toDate] as const,
};

/**
 * Hook for fetching paginated outgoing payments
 */
export const useOutgoingPayments = (
  companyId: string,
  params?: OutgoingPaymentsFilterParams,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: outgoingPaymentsKeys.list(companyId, params),
    queryFn: () => outgoingPaymentsService.getOutgoingPayments(companyId, params),
    enabled: enabled && !!companyId,
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
    placeholderData: (previousData) => previousData,
  });
};

/**
 * Hook for fetching outgoing payments pending reconciliation
 */
export const useOutgoingPaymentsToReconcile = (
  companyId: string,
  params?: OutgoingPaymentsFilterParams,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: outgoingPaymentsKeys.toReconcile(companyId, params),
    queryFn: () => outgoingPaymentsService.getToReconcile(companyId, params),
    enabled: enabled && !!companyId,
    staleTime: 1 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
    placeholderData: (previousData) => previousData,
  });
};

/**
 * Hook for fetching outgoing payments summary
 */
export const useOutgoingPaymentsSummary = (
  companyId: string,
  fromDate?: string,
  toDate?: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: outgoingPaymentsKeys.summary(companyId, fromDate, toDate),
    queryFn: () => outgoingPaymentsService.getSummary(companyId, fromDate, toDate),
    enabled: enabled && !!companyId,
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

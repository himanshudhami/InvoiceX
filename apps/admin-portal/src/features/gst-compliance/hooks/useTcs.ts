import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';

import { tcsService } from '@/services/api/gst';
import type {
  CreateTcsTransactionDto,
  TcsRemittanceRequest,
  PaginationParams,
} from '@/services/api/types';
import { gstKeys } from './gstKeys';

// ==================== TCS Queries ====================

/**
 * Fetch all TCS transactions
 */
export const useTcsTransactions = (companyId?: string) => {
  return useQuery({
    queryKey: gstKeys.tcs.list(companyId),
    queryFn: () => tcsService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch paged TCS transactions
 */
export const useTcsTransactionsPaged = (
  params: PaginationParams & {
    companyId?: string;
    status?: string;
    financialYear?: string;
    quarter?: string;
    customerPan?: string;
  } = {}
) => {
  return useQuery({
    queryKey: gstKeys.tcs.paged(params),
    queryFn: () => tcsService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single TCS transaction
 */
export const useTcsTransaction = (id: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.tcs.detail(id),
    queryFn: () => tcsService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch pending TCS remittances
 */
export const usePendingTcsRemittance = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.tcs.pending(companyId),
    queryFn: () => tcsService.getPendingRemittance(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch TCS summary
 */
export const useTcsSummary = (
  companyId: string,
  financialYear: string,
  quarter?: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.tcs.summary(companyId, financialYear, quarter),
    queryFn: () => tcsService.getSummary(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch TCS liability report
 */
export const useTcsLiabilityReport = (companyId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.tcs.liability(companyId, financialYear),
    queryFn: () => tcsService.getLiabilityReport(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== TCS Mutations ====================

/**
 * Create TCS transaction (collection)
 */
export const useCreateTcsTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateTcsTransactionDto) => tcsService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: gstKeys.tcs.lists() });
      toast.success('TCS collection recorded successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to record TCS collection');
    },
  });
};

/**
 * Update TCS transaction
 */
export const useUpdateTcsTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<CreateTcsTransactionDto> }) =>
      tcsService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.tcs.detail(id) });
      queryClient.invalidateQueries({ queryKey: gstKeys.tcs.lists() });
      toast.success('TCS transaction updated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to update TCS transaction');
    },
  });
};

/**
 * Delete TCS transaction
 */
export const useDeleteTcsTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => tcsService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: gstKeys.tcs.lists() });
      toast.success('TCS transaction deleted');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to delete TCS transaction');
    },
  });
};

/**
 * Record TCS remittance (bulk)
 */
export const useRecordTcsRemittance = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: TcsRemittanceRequest) => tcsService.recordRemittance(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: gstKeys.tcs.lists() });
      queryClient.invalidateQueries({ queryKey: gstKeys.tcs.all });
      toast.success('TCS remittance recorded successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to record TCS remittance');
    },
  });
};

/**
 * Calculate TCS for a sale
 */
export const useCalculateTcs = () => {
  return useMutation({
    mutationFn: ({
      companyId,
      customerId,
      saleAmount,
      financialYear,
    }: {
      companyId: string;
      customerId: string;
      saleAmount: number;
      financialYear: string;
    }) => tcsService.calculateTcs(companyId, customerId, saleAmount, financialYear),
  });
};

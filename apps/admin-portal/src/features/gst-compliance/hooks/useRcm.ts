import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';

import { rcmService } from '@/services/api/gst';
import type {
  CreateRcmTransactionDto,
  RcmPaymentRequest,
  RcmItcClaimRequest,
  PaginationParams,
} from '@/services/api/types';
import { gstKeys } from './gstKeys';

// ==================== RCM Queries ====================

/**
 * Fetch all RCM transactions
 */
export const useRcmTransactions = (companyId?: string) => {
  return useQuery({
    queryKey: gstKeys.rcm.list(companyId),
    queryFn: () => rcmService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch paged RCM transactions
 */
export const useRcmTransactionsPaged = (
  params: PaginationParams & {
    companyId?: string;
    status?: string;
    rcmCategory?: string;
    returnPeriod?: string;
  } = {}
) => {
  return useQuery({
    queryKey: gstKeys.rcm.paged(params),
    queryFn: () => rcmService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single RCM transaction
 */
export const useRcmTransaction = (id: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.rcm.detail(id),
    queryFn: () => rcmService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch pending RCM transactions (not yet paid)
 */
export const usePendingRcmTransactions = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.rcm.pending(companyId),
    queryFn: () => rcmService.getPending(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch RCM transactions pending ITC claim
 */
export const usePendingRcmItcClaim = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.rcm.pendingItc(companyId),
    queryFn: () => rcmService.getPendingItcClaim(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch RCM summary for a return period
 */
export const useRcmSummary = (companyId: string, returnPeriod: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.rcm.summary(companyId, returnPeriod),
    queryFn: () => rcmService.getSummary(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== RCM Mutations ====================

/**
 * Create RCM transaction
 */
export const useCreateRcmTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateRcmTransactionDto) => rcmService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.lists() });
      toast.success('RCM transaction created successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to create RCM transaction');
    },
  });
};

/**
 * Update RCM transaction
 */
export const useUpdateRcmTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<CreateRcmTransactionDto> }) =>
      rcmService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.detail(id) });
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.lists() });
      toast.success('RCM transaction updated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to update RCM transaction');
    },
  });
};

/**
 * Delete RCM transaction
 */
export const useDeleteRcmTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => rcmService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.lists() });
      toast.success('RCM transaction deleted');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to delete RCM transaction');
    },
  });
};

/**
 * Record RCM payment (Stage 1)
 */
export const useRecordRcmPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RcmPaymentRequest) => rcmService.recordPayment(request),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.detail(result.id) });
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.lists() });
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.pending(result.companyId) });
      toast.success('RCM payment recorded successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to record RCM payment');
    },
  });
};

/**
 * Claim ITC on RCM (Stage 2)
 */
export const useClaimRcmItc = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RcmItcClaimRequest) => rcmService.claimItc(request),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.detail(result.id) });
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.lists() });
      queryClient.invalidateQueries({ queryKey: gstKeys.rcm.pendingItc(result.companyId) });
      toast.success('ITC claimed on RCM successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to claim ITC on RCM');
    },
  });
};

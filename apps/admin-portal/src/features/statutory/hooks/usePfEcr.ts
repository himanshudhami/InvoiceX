import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { pfEcrService } from '@/services/api/finance/statutory';
import { statutoryKeys } from './statutoryKeys';
import type { CreatePfEcrPaymentRequest, RecordPfPaymentRequest } from '@/services/api/types';

/**
 * Hook to generate PF ECR data
 */
export const usePfEcrGenerate = (
  companyId: string,
  periodYear: number,
  periodMonth: number,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.pfEcr.generate(companyId, periodYear, periodMonth),
    queryFn: () => pfEcrService.generate(companyId, periodYear, periodMonth),
    enabled: enabled && !!companyId && !!periodYear && !!periodMonth,
    staleTime: 30 * 1000,
  });
};

/**
 * Hook to preview PF ECR with interest/damages calculation
 */
export const usePfEcrPreview = (
  companyId: string,
  periodYear: number,
  periodMonth: number,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.pfEcr.preview(companyId, periodYear, periodMonth),
    queryFn: () => pfEcrService.preview(companyId, periodYear, periodMonth),
    enabled: enabled && !!companyId && !!periodYear && !!periodMonth,
    staleTime: 30 * 1000,
  });
};

/**
 * Hook to get PF ECR detail by ID
 */
export const usePfEcr = (id: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.pfEcr.detail(id),
    queryFn: () => pfEcrService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get pending PF ECRs
 */
export const usePendingPfEcrs = (companyId: string, financialYear?: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.pfEcr.pending(companyId, financialYear),
    queryFn: () => pfEcrService.getPending(companyId, financialYear),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get filed PF ECRs
 */
export const useFiledPfEcrs = (companyId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.pfEcr.filed(companyId, financialYear),
    queryFn: () => pfEcrService.getFiled(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get PF ECR summary
 */
export const usePfEcrSummary = (companyId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.pfEcr.summary(companyId, financialYear),
    queryFn: () => pfEcrService.getSummary(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get PF reconciliation
 */
export const usePfReconciliation = (companyId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.pfEcr.reconcile(companyId, financialYear),
    queryFn: () => pfEcrService.reconcile(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to create PF ECR payment
 */
export const useCreatePfEcrPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreatePfEcrPaymentRequest) => pfEcrService.createPayment(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.pfEcr.all });
      toast.success('PF ECR payment record created');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create PF ECR payment');
    },
  });
};

/**
 * Hook to record PF payment
 */
export const useRecordPfPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: RecordPfPaymentRequest }) =>
      pfEcrService.recordPayment(id, request),
    onSuccess: (data, { id }) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.pfEcr.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.pfEcr.all });
      toast.success('PF payment recorded successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to record PF payment');
    },
  });
};

/**
 * Hook to update TRRN
 */
export const useUpdatePfTrrn = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, trrn }: { id: string; trrn: string }) =>
      pfEcrService.updateTrrn(id, trrn),
    onSuccess: (data, { id }) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.pfEcr.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.pfEcr.all });
      toast.success('TRRN updated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update TRRN');
    },
  });
};

/**
 * Hook to generate PF ECR file
 */
export const useGeneratePfEcrFile = () => {
  return useMutation({
    mutationFn: ({ companyId, periodYear, periodMonth }: {
      companyId: string;
      periodYear: number;
      periodMonth: number
    }) => pfEcrService.generateFile(companyId, periodYear, periodMonth),
    onSuccess: (data) => {
      toast.success(`ECR file generated: ${data.fileName}`);
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to generate ECR file');
    },
  });
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { esiReturnService } from '@/services/api/finance/statutory';
import { statutoryKeys } from './statutoryKeys';
import type { CreateEsiReturnRequest, RecordEsiPaymentRequest } from '@/services/api/types';

/**
 * Hook to generate ESI return data
 */
export const useEsiReturnGenerate = (
  companyId: string,
  periodYear: number,
  periodMonth: number,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.esiReturn.generate(companyId, periodYear, periodMonth),
    queryFn: () => esiReturnService.generate(companyId, periodYear, periodMonth),
    enabled: enabled && !!companyId && !!periodYear && !!periodMonth,
    staleTime: 30 * 1000,
  });
};

/**
 * Hook to preview ESI return with interest calculation
 */
export const useEsiReturnPreview = (
  companyId: string,
  periodYear: number,
  periodMonth: number,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.esiReturn.preview(companyId, periodYear, periodMonth),
    queryFn: () => esiReturnService.preview(companyId, periodYear, periodMonth),
    enabled: enabled && !!companyId && !!periodYear && !!periodMonth,
    staleTime: 30 * 1000,
  });
};

/**
 * Hook to get ESI return detail by ID
 */
export const useEsiReturn = (id: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.esiReturn.detail(id),
    queryFn: () => esiReturnService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get pending ESI returns
 */
export const usePendingEsiReturns = (companyId: string, financialYear?: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.esiReturn.pending(companyId, financialYear),
    queryFn: () => esiReturnService.getPending(companyId, financialYear),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get filed ESI returns
 */
export const useFiledEsiReturns = (companyId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.esiReturn.filed(companyId, financialYear),
    queryFn: () => esiReturnService.getFiled(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get ESI return summary
 */
export const useEsiReturnSummary = (companyId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.esiReturn.summary(companyId, financialYear),
    queryFn: () => esiReturnService.getSummary(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get ESI reconciliation
 */
export const useEsiReconciliation = (companyId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.esiReturn.reconcile(companyId, financialYear),
    queryFn: () => esiReturnService.reconcile(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to create ESI return payment
 */
export const useCreateEsiReturnPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateEsiReturnRequest) => esiReturnService.createPayment(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.esiReturn.all });
      toast.success('ESI return payment record created');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create ESI return payment');
    },
  });
};

/**
 * Hook to record ESI payment
 */
export const useRecordEsiPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: RecordEsiPaymentRequest }) =>
      esiReturnService.recordPayment(id, request),
    onSuccess: (data, { id }) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.esiReturn.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.esiReturn.all });
      toast.success('ESI payment recorded successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to record ESI payment');
    },
  });
};

/**
 * Hook to update challan number
 */
export const useUpdateEsiChallanNumber = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, challanNumber }: { id: string; challanNumber: string }) =>
      esiReturnService.updateChallanNumber(id, challanNumber),
    onSuccess: (data, { id }) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.esiReturn.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.esiReturn.all });
      toast.success('Challan number updated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update challan number');
    },
  });
};

/**
 * Hook to generate ESI return file
 */
export const useGenerateEsiReturnFile = () => {
  return useMutation({
    mutationFn: ({ companyId, periodYear, periodMonth }: {
      companyId: string;
      periodYear: number;
      periodMonth: number
    }) => esiReturnService.generateFile(companyId, periodYear, periodMonth),
    onSuccess: (data) => {
      toast.success(`ESI return file generated: ${data.fileName}`);
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to generate ESI return file');
    },
  });
};

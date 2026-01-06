import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { tdsChallanService } from '@/services/api/finance/statutory';
import { statutoryKeys } from './statutoryKeys';
import type { TdsChallanFilterParams, CreateTdsChallanRequest, RecordTdsPaymentRequest, UpdateCinRequest } from '@/services/api/types';

/**
 * Hook to get paginated TDS challan list
 */
export const useTdsChallanList = (companyId: string, params: TdsChallanFilterParams = {}, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.tdsChallan.list({ companyId, ...params }),
    queryFn: () => tdsChallanService.getPaged(companyId, params),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook to get TDS challan by ID
 */
export const useTdsChallan = (id: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.tdsChallan.detail(id),
    queryFn: () => tdsChallanService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get pending TDS challans
 */
export const usePendingTdsChallans = (companyId: string, financialYear?: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.tdsChallan.pending(companyId, financialYear),
    queryFn: () => tdsChallanService.getPending(companyId, financialYear),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get TDS challan summary
 */
export const useTdsChallanSummary = (companyId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.tdsChallan.summary(companyId, financialYear),
    queryFn: () => tdsChallanService.getSummary(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to preview TDS challan
 */
export const useTdsChallanPreview = (
  companyId: string,
  tdsType: 'salary' | 'contractor',
  periodYear: number,
  periodMonth: number,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.tdsChallan.preview(companyId, tdsType, periodYear, periodMonth),
    queryFn: () => tdsChallanService.preview(companyId, tdsType, periodYear, periodMonth),
    enabled: enabled && !!companyId && !!periodYear && !!periodMonth,
    staleTime: 30 * 1000, // 30 seconds - preview data can change
  });
};

/**
 * Hook to create TDS challan
 */
export const useCreateTdsChallan = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateTdsChallanRequest) => tdsChallanService.create(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.tdsChallan.all });
      toast.success('TDS Challan created successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create TDS Challan');
    },
  });
};

/**
 * Hook to record TDS payment
 */
export const useRecordTdsPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: RecordTdsPaymentRequest }) =>
      tdsChallanService.recordDeposit(id, request),
    onSuccess: (data, { id }) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.tdsChallan.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.tdsChallan.lists() });
      toast.success('TDS payment recorded successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to record TDS payment');
    },
  });
};

/**
 * Hook to update CIN (Challan Identification Number) after bank confirmation.
 * CIN is required for Form 24Q/26Q quarterly TDS return filing.
 */
export const useUpdateTdsCin = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCinRequest }) =>
      tdsChallanService.updateCin(id, request),
    onSuccess: (data, { id }) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.tdsChallan.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.tdsChallan.lists() });
      toast.success('CIN updated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update CIN');
    },
  });
};

/**
 * Hook to generate TDS challan from payroll run
 */
export const useGenerateTdsChallanFromPayroll = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payrollRunId: string) => tdsChallanService.generateFromPayrollRun(payrollRunId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.tdsChallan.all });
      toast.success('TDS Challan generated from payroll run');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to generate TDS Challan');
    },
  });
};

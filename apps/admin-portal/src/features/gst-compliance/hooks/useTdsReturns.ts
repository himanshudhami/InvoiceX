import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';

import { tdsReturnService } from '@/services/api/gst';
import type { MarkReturnFiledRequest } from '@/services/api/types';
import { gstKeys } from './gstKeys';

// ==================== Form 26Q (Non-Salary TDS) ====================

/**
 * Fetch Form 26Q data
 */
export const useForm26Q = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.form26Q(companyId, financialYear, quarter),
    queryFn: () => tdsReturnService.getForm26Q(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch Form 26Q summary
 */
export const useForm26QSummary = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.form26QSummary(companyId, financialYear, quarter),
    queryFn: () => tdsReturnService.getForm26QSummary(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Validate Form 26Q
 */
export const useValidateForm26Q = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = false
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.form26QValidation(companyId, financialYear, quarter),
    queryFn: () => tdsReturnService.validateForm26Q(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 60 * 1000, // 1 minute
  });
};

// ==================== Form 24Q (Salary TDS) ====================

/**
 * Fetch Form 24Q data
 */
export const useForm24Q = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.form24Q(companyId, financialYear, quarter),
    queryFn: () => tdsReturnService.getForm24Q(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch Form 24Q summary
 */
export const useForm24QSummary = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.form24QSummary(companyId, financialYear, quarter),
    queryFn: () => tdsReturnService.getForm24QSummary(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Validate Form 24Q
 */
export const useValidateForm24Q = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = false
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.form24QValidation(companyId, financialYear, quarter),
    queryFn: () => tdsReturnService.validateForm24Q(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 60 * 1000,
  });
};

/**
 * Fetch Form 24Q Annexure II (Q4 annual details)
 */
export const useForm24QAnnexureII = (
  companyId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.annexureII(companyId, financialYear),
    queryFn: () => tdsReturnService.getForm24QAnnexureII(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== Challan & Reconciliation ====================

/**
 * Fetch challans for a quarter
 */
export const useChallans = (
  companyId: string,
  financialYear: string,
  quarter: string,
  formType?: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.challans(companyId, financialYear, quarter),
    queryFn: () => tdsReturnService.getChallans(companyId, financialYear, quarter, formType),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Reconcile challans with TDS deducted
 */
export const useChallanReconciliation = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.reconciliation(companyId, financialYear, quarter),
    queryFn: () => tdsReturnService.reconcileChallans(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== Due Dates & Pending ====================

/**
 * Fetch TDS return due dates
 */
export const useTdsReturnDueDates = (financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.dueDates(financialYear),
    queryFn: () => tdsReturnService.getDueDates(financialYear),
    enabled: enabled && !!financialYear,
    staleTime: 60 * 60 * 1000, // 1 hour
  });
};

/**
 * Fetch pending TDS returns
 */
export const usePendingTdsReturns = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.pending(companyId),
    queryFn: () => tdsReturnService.getPendingReturns(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== Filing Status ====================

/**
 * Fetch filing history
 */
export const useTdsFilingHistory = (companyId: string, financialYear?: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.filingHistory(companyId, financialYear),
    queryFn: () => tdsReturnService.getFilingHistory(companyId, financialYear),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Mark TDS return as filed
 */
export const useMarkReturnFiled = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: MarkReturnFiledRequest) => tdsReturnService.markReturnFiled(request),
    onSuccess: (_, request) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.tdsReturns.pending(request.companyId) });
      queryClient.invalidateQueries({
        queryKey: gstKeys.tdsReturns.filingHistory(request.companyId),
      });
      toast.success(`${request.formType} for ${request.quarter} marked as filed`);
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to mark return as filed');
    },
  });
};

// ==================== Combined Summary ====================

/**
 * Fetch combined TDS summary (26Q + 24Q)
 */
export const useCombinedTdsSummary = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.tdsReturns.combinedSummary(companyId, financialYear, quarter),
    queryFn: () => tdsReturnService.getCombinedSummary(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 5 * 60 * 1000,
  });
};

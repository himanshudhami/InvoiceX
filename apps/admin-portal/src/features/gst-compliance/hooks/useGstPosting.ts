import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';

import { gstPostingService } from '@/services/api/gst';
import type {
  ItcBlockedCheckRequest,
  ItcBlockedRequest,
  CreditNoteGstRequest,
  DebitNoteGstRequest,
  ItcReversalCalculationRequest,
  ItcReversalRequest,
  UtgstRequest,
  GstTdsRequest,
  GstTcsRequest,
} from '@/services/api/types';
import { gstKeys } from './gstKeys';

// ==================== ITC Blocked Queries ====================

/**
 * Fetch all blocked ITC categories (Section 17(5))
 */
export const useItcBlockedCategories = () => {
  return useQuery({
    queryKey: gstKeys.itcBlocked.categories(),
    queryFn: () => gstPostingService.getBlockedCategories(),
    staleTime: 24 * 60 * 60 * 1000, // Categories rarely change
  });
};

/**
 * Fetch ITC blocked summary for a return period
 */
export const useItcBlockedSummary = (companyId: string, returnPeriod: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.itcBlocked.summary(companyId, returnPeriod),
    queryFn: () => gstPostingService.getItcBlockedSummary(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch ITC availability report
 */
export const useItcAvailabilityReport = (companyId: string, returnPeriod: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.itcReports.availability(companyId, returnPeriod),
    queryFn: () => gstPostingService.getItcAvailabilityReport(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== ITC Blocked Mutations ====================

/**
 * Check if ITC is blocked for an expense
 */
export const useCheckItcBlocked = () => {
  return useMutation({
    mutationFn: (request: ItcBlockedCheckRequest) => gstPostingService.checkItcBlocked(request),
  });
};

/**
 * Post ITC blocked journal entry
 */
export const usePostItcBlocked = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ItcBlockedRequest) => gstPostingService.postItcBlocked(request),
    onSuccess: (result, request) => {
      if (result.success) {
        queryClient.invalidateQueries({ queryKey: gstKeys.itcBlocked.all });
        queryClient.invalidateQueries({ queryKey: gstKeys.itcReports.all });
        toast.success('ITC blocked entry posted successfully');
      } else {
        toast.error(result.errorMessage || 'Failed to post ITC blocked entry');
      }
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to post ITC blocked entry');
    },
  });
};

// ==================== Credit/Debit Note Mutations ====================

/**
 * Post credit note GST adjustment
 */
export const usePostCreditNoteGst = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreditNoteGstRequest) => gstPostingService.postCreditNoteGst(request),
    onSuccess: (result) => {
      if (result.success) {
        queryClient.invalidateQueries({ queryKey: gstKeys.itcReports.all });
        toast.success('Credit note GST adjustment posted');
      } else {
        toast.error(result.errorMessage || 'Failed to post credit note GST');
      }
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to post credit note GST');
    },
  });
};

/**
 * Post debit note GST adjustment
 */
export const usePostDebitNoteGst = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: DebitNoteGstRequest) => gstPostingService.postDebitNoteGst(request),
    onSuccess: (result) => {
      if (result.success) {
        queryClient.invalidateQueries({ queryKey: gstKeys.itcReports.all });
        toast.success('Debit note GST adjustment posted');
      } else {
        toast.error(result.errorMessage || 'Failed to post debit note GST');
      }
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to post debit note GST');
    },
  });
};

// ==================== ITC Reversal Mutations ====================

/**
 * Calculate ITC reversal (Rule 42/43)
 */
export const useCalculateItcReversal = () => {
  return useMutation({
    mutationFn: (request: ItcReversalCalculationRequest) =>
      gstPostingService.calculateItcReversal(request),
  });
};

/**
 * Post ITC reversal journal entry
 */
export const usePostItcReversal = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ItcReversalRequest) => gstPostingService.postItcReversal(request),
    onSuccess: (result) => {
      if (result.success) {
        queryClient.invalidateQueries({ queryKey: gstKeys.itcReversal.all });
        queryClient.invalidateQueries({ queryKey: gstKeys.itcReports.all });
        toast.success('ITC reversal posted successfully');
      } else {
        toast.error(result.errorMessage || 'Failed to post ITC reversal');
      }
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to post ITC reversal');
    },
  });
};

// ==================== UTGST Mutation ====================

/**
 * Post UTGST entry
 */
export const usePostUtgst = () => {
  return useMutation({
    mutationFn: (request: UtgstRequest) => gstPostingService.postUtgst(request),
    onSuccess: (result) => {
      if (result.success) {
        toast.success('UTGST entry posted successfully');
      } else {
        toast.error(result.errorMessage || 'Failed to post UTGST entry');
      }
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to post UTGST entry');
    },
  });
};

// ==================== GST TDS/TCS Mutations ====================

/**
 * Post GST TDS (Section 51)
 */
export const usePostGstTds = () => {
  return useMutation({
    mutationFn: (request: GstTdsRequest) => gstPostingService.postGstTds(request),
    onSuccess: (result) => {
      if (result.success) {
        toast.success('GST TDS entry posted successfully');
      } else {
        toast.error(result.errorMessage || 'Failed to post GST TDS entry');
      }
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to post GST TDS entry');
    },
  });
};

/**
 * Post GST TCS (Section 52)
 */
export const usePostGstTcs = () => {
  return useMutation({
    mutationFn: (request: GstTcsRequest) => gstPostingService.postGstTcs(request),
    onSuccess: (result) => {
      if (result.success) {
        toast.success('GST TCS entry posted successfully');
      } else {
        toast.error(result.errorMessage || 'Failed to post GST TCS entry');
      }
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to post GST TCS entry');
    },
  });
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';

import { advanceTaxService } from '@/services/api/finance/tax/advanceTaxService';
import type {
  CreateAdvanceTaxAssessmentRequest,
  UpdateAdvanceTaxAssessmentRequest,
  RecordAdvanceTaxPaymentRequest,
  RunScenarioRequest,
  RefreshYtdRequest,
  CreateRevisionRequest,
} from '@/services/api/types';
import { advanceTaxKeys } from './advanceTaxKeys';

// ==================== Assessment Queries ====================

/**
 * Fetch assessment by ID
 */
export const useAdvanceTaxAssessment = (id: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.assessments.detail(id),
    queryFn: () => advanceTaxService.getAssessmentById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch assessment for company and FY
 */
export const useAdvanceTaxAssessmentByFy = (
  companyId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: advanceTaxKeys.assessments.byCompanyFy(companyId, financialYear),
    queryFn: () => advanceTaxService.getAssessment(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
    retry: false, // Don't retry on 404
  });
};

/**
 * Fetch all assessments for a company
 */
export const useAdvanceTaxAssessments = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.assessments.listByCompany(companyId),
    queryFn: () => advanceTaxService.getAssessmentsByCompany(companyId),
    enabled: enabled && !!companyId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch assessments with pending payments
 */
export const usePendingAdvanceTaxPayments = (companyId?: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.assessments.pending(companyId),
    queryFn: () => advanceTaxService.getPendingPayments(companyId),
    enabled,
    staleTime: 2 * 60 * 1000,
  });
};

// ==================== Schedule Queries ====================

/**
 * Fetch payment schedule for an assessment
 */
export const useAdvanceTaxSchedule = (assessmentId: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.schedules.byAssessment(assessmentId),
    queryFn: () => advanceTaxService.getPaymentSchedule(assessmentId),
    enabled: enabled && !!assessmentId,
    staleTime: 2 * 60 * 1000,
  });
};

// ==================== Payment Queries ====================

/**
 * Fetch payments for an assessment
 */
export const useAdvanceTaxPayments = (assessmentId: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.payments.byAssessment(assessmentId),
    queryFn: () => advanceTaxService.getPayments(assessmentId),
    enabled: enabled && !!assessmentId,
    staleTime: 2 * 60 * 1000,
  });
};

// ==================== Interest Queries ====================

/**
 * Fetch interest breakdown
 */
export const useAdvanceTaxInterest = (assessmentId: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.interest.breakdown(assessmentId),
    queryFn: () => advanceTaxService.getInterestBreakdown(assessmentId),
    enabled: enabled && !!assessmentId,
    staleTime: 2 * 60 * 1000,
  });
};

// ==================== Scenario Queries ====================

/**
 * Fetch scenarios for an assessment
 */
export const useAdvanceTaxScenarios = (assessmentId: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.scenarios.byAssessment(assessmentId),
    queryFn: () => advanceTaxService.getScenarios(assessmentId),
    enabled: enabled && !!assessmentId,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== Tracker Queries ====================

/**
 * Fetch advance tax tracker/dashboard
 */
export const useAdvanceTaxTracker = (
  companyId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: advanceTaxKeys.tracker.byCompanyFy(companyId, financialYear),
    queryFn: () => advanceTaxService.getTracker(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch tax computation breakdown
 */
export const useAdvanceTaxComputation = (assessmentId: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.computation.byAssessment(assessmentId),
    queryFn: () => advanceTaxService.getTaxComputation(assessmentId),
    enabled: enabled && !!assessmentId,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== Assessment Mutations ====================

/**
 * Compute and create advance tax assessment
 */
export const useComputeAdvanceTax = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateAdvanceTaxAssessmentRequest) =>
      advanceTaxService.computeAssessment(request),
    onSuccess: (result, variables) => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.assessments.byCompanyFy(
          variables.companyId,
          variables.financialYear
        ),
      });
      toast.success(`Advance tax assessment computed for FY ${variables.financialYear}`);
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to compute advance tax assessment');
    },
  });
};

/**
 * Update assessment projections
 */
export const useUpdateAdvanceTaxAssessment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateAdvanceTaxAssessmentRequest }) =>
      advanceTaxService.updateAssessment(id, request),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.assessments.detail(result.id),
      });
      toast.success('Assessment updated');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to update assessment');
    },
  });
};

/**
 * Activate assessment
 */
export const useActivateAdvanceTax = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => advanceTaxService.activateAssessment(id),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      toast.success('Assessment activated');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to activate assessment');
    },
  });
};

/**
 * Finalize assessment
 */
export const useFinalizeAdvanceTax = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => advanceTaxService.finalizeAssessment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      toast.success('Assessment finalized');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to finalize assessment');
    },
  });
};

/**
 * Delete assessment
 */
export const useDeleteAdvanceTaxAssessment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => advanceTaxService.deleteAssessment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      toast.success('Assessment deleted');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to delete assessment');
    },
  });
};

/**
 * Refresh YTD actuals from ledger
 */
export const useRefreshYtd = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RefreshYtdRequest) => advanceTaxService.refreshYtd(request),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.assessments.detail(result.id),
      });
      toast.success('YTD actuals refreshed from ledger');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to refresh YTD');
    },
  });
};

/**
 * Fetch YTD financials preview
 */
export const useYtdFinancialsPreview = (
  companyId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: advanceTaxKeys.ytdPreview.byCompanyFy(companyId, financialYear),
    queryFn: () => advanceTaxService.getYtdFinancialsPreview(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 1 * 60 * 1000, // 1 minute - fresh data important
  });
};

/**
 * Refresh TDS receivable and TCS credit from modules
 */
export const useRefreshTdsTcs = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (assessmentId: string) => advanceTaxService.refreshTdsTcs(assessmentId),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.assessments.detail(result.id),
      });
      toast.success('TDS/TCS credits refreshed from modules');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to refresh TDS/TCS');
    },
  });
};

/**
 * Fetch TDS/TCS preview (current values from modules)
 */
export const useTdsTcsPreview = (
  companyId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: advanceTaxKeys.tdsTcsPreview.byCompanyFy(companyId, financialYear),
    queryFn: () => advanceTaxService.getTdsTcsPreview(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 1 * 60 * 1000, // 1 minute - fresh data important
  });
};

// ==================== Schedule Mutations ====================

/**
 * Recalculate schedules
 */
export const useRecalculateSchedules = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (assessmentId: string) => advanceTaxService.recalculateSchedules(assessmentId),
    onSuccess: (_, assessmentId) => {
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.schedules.byAssessment(assessmentId),
      });
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.assessments.detail(assessmentId),
      });
      toast.success('Schedules recalculated');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to recalculate schedules');
    },
  });
};

// ==================== Payment Mutations ====================

/**
 * Record advance tax payment
 */
export const useRecordAdvanceTaxPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RecordAdvanceTaxPaymentRequest) =>
      advanceTaxService.recordPayment(request),
    onSuccess: (result, variables) => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.payments.byAssessment(variables.assessmentId),
      });
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.schedules.byAssessment(variables.assessmentId),
      });
      toast.success(`Payment of ${variables.amount.toLocaleString()} recorded`);
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to record payment');
    },
  });
};

/**
 * Delete a payment
 */
export const useDeleteAdvanceTaxPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => advanceTaxService.deletePayment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      toast.success('Payment deleted');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to delete payment');
    },
  });
};

// ==================== Scenario Mutations ====================

/**
 * Run a what-if scenario
 */
export const useRunAdvanceTaxScenario = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RunScenarioRequest) => advanceTaxService.runScenario(request),
    onSuccess: (result, variables) => {
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.scenarios.byAssessment(variables.assessmentId),
      });
      toast.success(`Scenario "${variables.scenarioName}" created`);
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to run scenario');
    },
  });
};

/**
 * Delete a scenario
 */
export const useDeleteAdvanceTaxScenario = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => advanceTaxService.deleteScenario(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.scenarios.all() });
      toast.success('Scenario deleted');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to delete scenario');
    },
  });
};

// ==================== Revision Hooks ====================

/**
 * Get revisions for an assessment
 */
export const useAdvanceTaxRevisions = (assessmentId: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.revisions.byAssessment(assessmentId),
    queryFn: () => advanceTaxService.getRevisions(assessmentId),
    enabled: enabled && !!assessmentId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Get revision status (for dashboard prompt)
 */
export const useRevisionStatus = (assessmentId: string, enabled = true) => {
  return useQuery({
    queryKey: advanceTaxKeys.revisions.status(assessmentId),
    queryFn: () => advanceTaxService.getRevisionStatus(assessmentId),
    enabled: enabled && !!assessmentId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Create a revision
 */
export const useCreateRevision = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateRevisionRequest) => advanceTaxService.createRevision(request),
    onSuccess: (result, variables) => {
      queryClient.invalidateQueries({ queryKey: advanceTaxKeys.all });
      queryClient.invalidateQueries({
        queryKey: advanceTaxKeys.revisions.byAssessment(variables.assessmentId),
      });
      toast.success(`Revision Q${variables.revisionQuarter} created successfully`);
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to create revision');
    },
  });
};

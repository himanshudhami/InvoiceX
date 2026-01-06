import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import {
  form24QFilingService,
  type Form24QFilingFilterParams,
  type CreateForm24QFilingRequest,
  type RecordAcknowledgementRequest,
  type RejectFilingRequest,
} from '@/services/api/finance/statutory';
import { statutoryKeys } from './statutoryKeys';

/**
 * Hook to get paginated Form 24Q filing list for a company
 */
export const useForm24QFilingList = (companyId: string, params: Form24QFilingFilterParams = {}) => {
  return useQuery({
    queryKey: statutoryKeys.form24QFiling.list({ companyId, ...params }),
    queryFn: () => form24QFilingService.getPaged(companyId, params),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook to get Form 24Q filing by ID
 */
export const useForm24QFiling = (id: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.form24QFiling.detail(id),
    queryFn: () => form24QFilingService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get Form 24Q filing for company/FY/quarter
 */
export const useForm24QFilingByQuarter = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.form24QFiling.byCompanyQuarter(companyId, financialYear, quarter),
    queryFn: () => form24QFilingService.getByCompanyQuarter(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get Form 24Q filing statistics for a financial year
 */
export const useForm24QFilingStatistics = (
  companyId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.form24QFiling.statistics(companyId, financialYear),
    queryFn: () => form24QFilingService.getStatistics(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get all filings for a financial year
 */
export const useForm24QFilingsByYear = (
  companyId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.form24QFiling.byFinancialYear(companyId, financialYear),
    queryFn: () => form24QFilingService.getByFinancialYear(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get pending filings for a financial year
 */
export const usePendingForm24QFilings = (
  companyId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.form24QFiling.pending(companyId, financialYear),
    queryFn: () => form24QFilingService.getPendingFilings(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get overdue filings
 */
export const useOverdueForm24QFilings = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.form24QFiling.overdue(companyId),
    queryFn: () => form24QFilingService.getOverdueFilings(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to preview Form 24Q data
 */
export const useForm24QPreview = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.form24QFiling.preview(companyId, financialYear, quarter),
    queryFn: () => form24QFilingService.preview(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Hook to get correction returns for an original filing
 */
export const useForm24QCorrections = (id: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.form24QFiling.corrections(id),
    queryFn: () => form24QFilingService.getCorrections(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to create a draft Form 24Q filing
 */
export const useCreateForm24QFiling = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateForm24QFilingRequest) => form24QFilingService.createDraft(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.all });
      toast.success(`Form 24Q draft created for ${data.financialYear} ${data.quarter}`);
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create Form 24Q draft');
    },
  });
};

/**
 * Hook to refresh Form 24Q filing data
 */
export const useRefreshForm24QFiling = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, updatedBy }: { id: string; updatedBy?: string }) =>
      form24QFilingService.refreshData(id, updatedBy),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.detail(data.id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.lists() });
      toast.success('Form 24Q data refreshed');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to refresh Form 24Q data');
    },
  });
};

/**
 * Hook to validate Form 24Q filing
 */
export const useValidateForm24QFiling = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => form24QFilingService.validate(id),
    onSuccess: (result, id) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.detail(id) });
      if (result.isValid) {
        toast.success('Validation passed');
      } else {
        toast.error(`Validation failed: ${result.errors.length} error(s)`);
      }
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Validation failed');
    },
  });
};

/**
 * Hook to generate FVU file
 */
export const useGenerateForm24QFvu = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, generatedBy }: { id: string; generatedBy?: string }) =>
      form24QFilingService.generateFvu(id, generatedBy),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.detail(data.id) });
      toast.success('FVU file generated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to generate FVU file');
    },
  });
};

/**
 * Hook to download FVU file
 */
export const useDownloadForm24QFvu = () => {
  return useMutation({
    mutationFn: async ({ id, filename }: { id: string; filename?: string }) => {
      const blob = await form24QFilingService.downloadFvu(id);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = filename || `Form24Q_${id}.txt`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
    onSuccess: () => {
      toast.success('FVU file downloaded');
    },
    onError: () => {
      toast.error('Failed to download FVU file');
    },
  });
};

/**
 * Hook to mark filing as submitted
 */
export const useSubmitForm24QFiling = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      filingDate,
      submittedBy,
    }: {
      id: string;
      filingDate?: string;
      submittedBy?: string;
    }) => form24QFilingService.markAsSubmitted(id, filingDate, submittedBy),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.all });
      toast.success('Filing marked as submitted');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to mark as submitted');
    },
  });
};

/**
 * Hook to record acknowledgement
 */
export const useRecordForm24QAcknowledgement = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: RecordAcknowledgementRequest }) =>
      form24QFilingService.recordAcknowledgement(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.all });
      toast.success(`Acknowledgement recorded: ${data.acknowledgementNumber}`);
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to record acknowledgement');
    },
  });
};

/**
 * Hook to mark filing as rejected
 */
export const useRejectForm24QFiling = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: RejectFilingRequest }) =>
      form24QFilingService.markAsRejected(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.all });
      toast.success('Filing marked as rejected');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to mark as rejected');
    },
  });
};

/**
 * Hook to create correction return
 */
export const useCreateForm24QCorrection = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, createdBy }: { id: string; createdBy?: string }) =>
      form24QFilingService.createCorrection(id, createdBy),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.all });
      toast.success('Correction return created');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create correction return');
    },
  });
};

/**
 * Hook to delete draft filing
 */
export const useDeleteForm24QFiling = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => form24QFilingService.deleteDraft(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form24QFiling.all });
      toast.success('Draft filing deleted');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete filing');
    },
  });
};

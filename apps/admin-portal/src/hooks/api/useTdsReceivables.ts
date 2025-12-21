import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tdsReceivableService } from '@/services/api/finance/tax/tdsReceivableService';
import {
  CreateTdsReceivableDto,
  UpdateTdsReceivableDto,
  Match26AsDto,
  UpdateTdsStatusDto,
  TdsReceivableFilterParams
} from '@/services/api/types';

// Query keys for React Query cache management
export const tdsReceivableKeys = {
  all: ['tdsReceivables'] as const,
  lists: () => [...tdsReceivableKeys.all, 'list'] as const,
  list: (params: TdsReceivableFilterParams) => [...tdsReceivableKeys.lists(), params] as const,
  details: () => [...tdsReceivableKeys.all, 'detail'] as const,
  detail: (id: string) => [...tdsReceivableKeys.details(), id] as const,
  byCompanyFY: (companyId: string, financialYear: string) =>
    [...tdsReceivableKeys.all, 'byCompanyFY', companyId, financialYear] as const,
  byCompanyFYQuarter: (companyId: string, financialYear: string, quarter: string) =>
    [...tdsReceivableKeys.all, 'byCompanyFYQuarter', companyId, financialYear, quarter] as const,
  unmatched: (companyId: string) => [...tdsReceivableKeys.all, 'unmatched', companyId] as const,
  summary: (companyId: string, financialYear: string) =>
    [...tdsReceivableKeys.all, 'summary', companyId, financialYear] as const,
};

/**
 * Hook for fetching all TDS receivables
 */
export const useTdsReceivables = () => {
  return useQuery({
    queryKey: tdsReceivableKeys.lists(),
    queryFn: () => tdsReceivableService.getAll(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching paginated TDS receivables
 */
export const useTdsReceivablesPaged = (params: TdsReceivableFilterParams = {}) => {
  return useQuery({
    queryKey: tdsReceivableKeys.list(params),
    queryFn: () => tdsReceivableService.getPaged(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    placeholderData: (previousData) => previousData,
  });
};

/**
 * Hook for fetching a single TDS receivable by ID
 */
export const useTdsReceivable = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: tdsReceivableKeys.detail(id),
    queryFn: () => tdsReceivableService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching TDS receivables by company and financial year
 */
export const useTdsReceivablesByCompanyFY = (
  companyId: string,
  financialYear: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: tdsReceivableKeys.byCompanyFY(companyId, financialYear),
    queryFn: () => tdsReceivableService.getByCompanyAndFY(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching TDS receivables by company, financial year and quarter
 */
export const useTdsReceivablesByCompanyFYQuarter = (
  companyId: string,
  financialYear: string,
  quarter: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: tdsReceivableKeys.byCompanyFYQuarter(companyId, financialYear, quarter),
    queryFn: () => tdsReceivableService.getByCompanyFYQuarter(companyId, financialYear, quarter),
    enabled: enabled && !!companyId && !!financialYear && !!quarter,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching unmatched TDS receivables
 */
export const useUnmatchedTdsReceivables = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: tdsReceivableKeys.unmatched(companyId),
    queryFn: () => tdsReceivableService.getUnmatched(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching TDS receivable summary
 */
export const useTdsSummary = (
  companyId: string,
  financialYear: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: tdsReceivableKeys.summary(companyId, financialYear),
    queryFn: () => tdsReceivableService.getSummary(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for creating a TDS receivable
 */
export const useCreateTdsReceivable = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateTdsReceivableDto) => tdsReceivableService.create(data),
    onSuccess: (newEntry) => {
      queryClient.invalidateQueries({ queryKey: tdsReceivableKeys.lists() });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.byCompanyFY(newEntry.companyId, newEntry.financialYear)
      });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.summary(newEntry.companyId, newEntry.financialYear)
      });
      if (!newEntry.matchedWith26As) {
        queryClient.invalidateQueries({
          queryKey: tdsReceivableKeys.unmatched(newEntry.companyId)
        });
      }
    },
    onError: (error) => {
      console.error('Failed to create TDS receivable:', error);
    },
  });
};

/**
 * Hook for updating a TDS receivable
 */
export const useUpdateTdsReceivable = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTdsReceivableDto }) =>
      tdsReceivableService.update(id, data),
    onSuccess: (updatedEntry, variables) => {
      queryClient.invalidateQueries({ queryKey: tdsReceivableKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: tdsReceivableKeys.lists() });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.byCompanyFY(updatedEntry.companyId, updatedEntry.financialYear)
      });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.summary(updatedEntry.companyId, updatedEntry.financialYear)
      });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.unmatched(updatedEntry.companyId)
      });
    },
    onError: (error) => {
      console.error('Failed to update TDS receivable:', error);
    },
  });
};

/**
 * Hook for matching TDS entry with Form 26AS
 */
export const useMatchTdsWith26As = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Match26AsDto }) =>
      tdsReceivableService.matchWith26As(id, data),
    onSuccess: (updatedEntry, variables) => {
      queryClient.invalidateQueries({ queryKey: tdsReceivableKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: tdsReceivableKeys.lists() });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.byCompanyFY(updatedEntry.companyId, updatedEntry.financialYear)
      });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.summary(updatedEntry.companyId, updatedEntry.financialYear)
      });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.unmatched(updatedEntry.companyId)
      });
    },
    onError: (error) => {
      console.error('Failed to match TDS with 26AS:', error);
    },
  });
};

/**
 * Hook for updating TDS status
 */
export const useUpdateTdsStatus = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTdsStatusDto }) =>
      tdsReceivableService.updateStatus(id, data),
    onSuccess: (updatedEntry, variables) => {
      queryClient.invalidateQueries({ queryKey: tdsReceivableKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: tdsReceivableKeys.lists() });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.byCompanyFY(updatedEntry.companyId, updatedEntry.financialYear)
      });
      queryClient.invalidateQueries({
        queryKey: tdsReceivableKeys.summary(updatedEntry.companyId, updatedEntry.financialYear)
      });
    },
    onError: (error) => {
      console.error('Failed to update TDS status:', error);
    },
  });
};

/**
 * Hook for deleting a TDS receivable
 */
export const useDeleteTdsReceivable = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => tdsReceivableService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tdsReceivableKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete TDS receivable:', error);
    },
  });
};

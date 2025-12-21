import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fircService, FircFilterParams } from '@/services/api/exports/fircService';
import { CreateFircDto, UpdateFircDto } from '@/services/api/types';

// Query keys for React Query cache management
export const fircKeys = {
  all: ['fircs'] as const,
  lists: () => [...fircKeys.all, 'list'] as const,
  list: (params: FircFilterParams) => [...fircKeys.lists(), params] as const,
  details: () => [...fircKeys.all, 'detail'] as const,
  detail: (id: string) => [...fircKeys.details(), id] as const,
  byCompany: (companyId: string) => [...fircKeys.all, 'byCompany', companyId] as const,
  pendingReconciliation: (companyId: string) => [...fircKeys.all, 'pending', companyId] as const,
  realizationAlerts: (companyId: string) => [...fircKeys.all, 'alerts', companyId] as const,
  edpmsSummary: (companyId: string) => [...fircKeys.all, 'edpms', companyId] as const,
};

/**
 * Hook for fetching all FIRCs
 */
export const useFircs = () => {
  return useQuery({
    queryKey: fircKeys.lists(),
    queryFn: () => fircService.getAll(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching paginated FIRCs
 */
export const useFircsPaged = (params: FircFilterParams = {}) => {
  return useQuery({
    queryKey: fircKeys.list(params),
    queryFn: () => fircService.getPaged(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    placeholderData: (previousData) => previousData,
  });
};

/**
 * Hook for fetching a single FIRC by ID
 */
export const useFirc = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: fircKeys.detail(id),
    queryFn: () => fircService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching FIRCs by company
 */
export const useFircsByCompany = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: fircKeys.byCompany(companyId),
    queryFn: () => fircService.getByCompanyId(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching FIRCs pending reconciliation
 */
export const useFircsPendingReconciliation = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: fircKeys.pendingReconciliation(companyId),
    queryFn: () => fircService.getPendingReconciliation(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching realization alerts (FEMA deadline)
 */
export const useRealizationAlerts = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: fircKeys.realizationAlerts(companyId),
    queryFn: () => fircService.getRealizationAlerts(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching EDPMS compliance summary
 */
export const useEdpmsComplianceSummary = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: fircKeys.edpmsSummary(companyId),
    queryFn: () => fircService.getEdpmsComplianceSummary(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for creating a FIRC
 */
export const useCreateFirc = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateFircDto) => fircService.create(data),
    onSuccess: (newFirc) => {
      queryClient.invalidateQueries({ queryKey: fircKeys.lists() });
      if (newFirc.companyId) {
        queryClient.invalidateQueries({ queryKey: fircKeys.byCompany(newFirc.companyId) });
        queryClient.invalidateQueries({ queryKey: fircKeys.pendingReconciliation(newFirc.companyId) });
        queryClient.invalidateQueries({ queryKey: fircKeys.edpmsSummary(newFirc.companyId) });
      }
    },
    onError: (error) => {
      console.error('Failed to create FIRC:', error);
    },
  });
};

/**
 * Hook for updating a FIRC
 */
export const useUpdateFirc = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateFircDto }) =>
      fircService.update(id, data),
    onSuccess: (updatedFirc, variables) => {
      queryClient.invalidateQueries({ queryKey: fircKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: fircKeys.lists() });
      if (updatedFirc.companyId) {
        queryClient.invalidateQueries({ queryKey: fircKeys.byCompany(updatedFirc.companyId) });
        queryClient.invalidateQueries({ queryKey: fircKeys.edpmsSummary(updatedFirc.companyId) });
      }
    },
    onError: (error) => {
      console.error('Failed to update FIRC:', error);
    },
  });
};

/**
 * Hook for deleting a FIRC
 */
export const useDeleteFirc = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => fircService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fircKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete FIRC:', error);
    },
  });
};

/**
 * Hook for linking FIRC to payment
 */
export const useLinkFircToPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ fircId, paymentId }: { fircId: string; paymentId: string }) =>
      fircService.linkToPayment(fircId, paymentId),
    onSuccess: (updatedFirc, variables) => {
      queryClient.invalidateQueries({ queryKey: fircKeys.detail(variables.fircId) });
      queryClient.invalidateQueries({ queryKey: fircKeys.lists() });
      if (updatedFirc.companyId) {
        queryClient.invalidateQueries({ queryKey: fircKeys.pendingReconciliation(updatedFirc.companyId) });
      }
    },
    onError: (error) => {
      console.error('Failed to link FIRC to payment:', error);
    },
  });
};

/**
 * Hook for linking FIRC to invoices
 */
export const useLinkFircToInvoices = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ fircId, invoiceIds }: { fircId: string; invoiceIds: string[] }) =>
      fircService.linkToInvoices(fircId, invoiceIds),
    onSuccess: (updatedFirc, variables) => {
      queryClient.invalidateQueries({ queryKey: fircKeys.detail(variables.fircId) });
      queryClient.invalidateQueries({ queryKey: fircKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to link FIRC to invoices:', error);
    },
  });
};

/**
 * Hook for marking FIRC as EDPMS reported
 */
export const useMarkEdpmsReported = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ fircId, reportDate }: { fircId: string; reportDate?: string }) =>
      fircService.markEdpmsReported(fircId, reportDate),
    onSuccess: (updatedFirc, variables) => {
      queryClient.invalidateQueries({ queryKey: fircKeys.detail(variables.fircId) });
      queryClient.invalidateQueries({ queryKey: fircKeys.lists() });
      if (updatedFirc.companyId) {
        queryClient.invalidateQueries({ queryKey: fircKeys.edpmsSummary(updatedFirc.companyId) });
      }
    },
    onError: (error) => {
      console.error('Failed to mark FIRC as EDPMS reported:', error);
    },
  });
};

/**
 * Hook for auto-matching FIRCs
 */
export const useAutoMatchFircs = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (companyId: string) => fircService.autoMatch(companyId),
    onSuccess: (_, companyId) => {
      queryClient.invalidateQueries({ queryKey: fircKeys.byCompany(companyId) });
      queryClient.invalidateQueries({ queryKey: fircKeys.pendingReconciliation(companyId) });
      queryClient.invalidateQueries({ queryKey: fircKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to auto-match FIRCs:', error);
    },
  });
};

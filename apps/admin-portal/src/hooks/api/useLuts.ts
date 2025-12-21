import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { lutService, LutFilterParams } from '@/services/api/exports/lutService';
import { CreateLutDto, UpdateLutDto } from '@/services/api/types';

// Query keys for React Query cache management
export const lutKeys = {
  all: ['luts'] as const,
  lists: () => [...lutKeys.all, 'list'] as const,
  list: (params: LutFilterParams) => [...lutKeys.lists(), params] as const,
  details: () => [...lutKeys.all, 'detail'] as const,
  detail: (id: string) => [...lutKeys.details(), id] as const,
  byCompany: (companyId: string) => [...lutKeys.all, 'byCompany', companyId] as const,
  active: (companyId: string) => [...lutKeys.all, 'active', companyId] as const,
  validation: (companyId: string, date: string) => [...lutKeys.all, 'validation', companyId, date] as const,
  expiryAlerts: (companyId?: string) => [...lutKeys.all, 'expiryAlerts', companyId] as const,
  utilization: (lutId: string) => [...lutKeys.all, 'utilization', lutId] as const,
  compliance: (companyId: string) => [...lutKeys.all, 'compliance', companyId] as const,
};

/**
 * Hook for fetching all LUTs
 */
export const useLuts = () => {
  return useQuery({
    queryKey: lutKeys.lists(),
    queryFn: () => lutService.getAll(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching paginated LUTs
 */
export const useLutsPaged = (params: LutFilterParams = {}) => {
  return useQuery({
    queryKey: lutKeys.list(params),
    queryFn: () => lutService.getPaged(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    placeholderData: (previousData) => previousData,
  });
};

/**
 * Hook for fetching a single LUT by ID
 */
export const useLut = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: lutKeys.detail(id),
    queryFn: () => lutService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching LUTs by company
 */
export const useLutsByCompany = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: lutKeys.byCompany(companyId),
    queryFn: () => lutService.getByCompanyId(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching active LUT for a company
 */
export const useActiveLut = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: lutKeys.active(companyId),
    queryFn: () => lutService.getActiveLut(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for validating LUT for a specific date
 */
export const useLutValidation = (companyId: string, date: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: lutKeys.validation(companyId, date),
    queryFn: () => lutService.validateForDate(companyId, date),
    enabled: enabled && !!companyId && !!date,
    staleTime: 1 * 60 * 1000, // Shorter cache for validation
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching LUT expiry alerts
 */
export const useLutExpiryAlerts = (companyId?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: lutKeys.expiryAlerts(companyId),
    queryFn: () => lutService.getExpiryAlerts(companyId),
    enabled: enabled,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching LUT utilization report
 */
export const useLutUtilization = (lutId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: lutKeys.utilization(lutId),
    queryFn: () => lutService.getUtilizationReport(lutId),
    enabled: enabled && !!lutId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching LUT compliance summary
 */
export const useLutComplianceSummary = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: lutKeys.compliance(companyId),
    queryFn: () => lutService.getComplianceSummary(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for creating a LUT
 */
export const useCreateLut = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateLutDto) => lutService.create(data),
    onSuccess: (newLut) => {
      queryClient.invalidateQueries({ queryKey: lutKeys.lists() });
      if (newLut.companyId) {
        queryClient.invalidateQueries({ queryKey: lutKeys.byCompany(newLut.companyId) });
        queryClient.invalidateQueries({ queryKey: lutKeys.active(newLut.companyId) });
        queryClient.invalidateQueries({ queryKey: lutKeys.compliance(newLut.companyId) });
      }
      queryClient.invalidateQueries({ queryKey: lutKeys.expiryAlerts() });
    },
    onError: (error) => {
      console.error('Failed to create LUT:', error);
    },
  });
};

/**
 * Hook for updating a LUT
 */
export const useUpdateLut = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateLutDto }) =>
      lutService.update(id, data),
    onSuccess: (updatedLut, variables) => {
      queryClient.invalidateQueries({ queryKey: lutKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: lutKeys.lists() });
      if (updatedLut.companyId) {
        queryClient.invalidateQueries({ queryKey: lutKeys.byCompany(updatedLut.companyId) });
        queryClient.invalidateQueries({ queryKey: lutKeys.active(updatedLut.companyId) });
        queryClient.invalidateQueries({ queryKey: lutKeys.compliance(updatedLut.companyId) });
      }
      queryClient.invalidateQueries({ queryKey: lutKeys.expiryAlerts() });
    },
    onError: (error) => {
      console.error('Failed to update LUT:', error);
    },
  });
};

/**
 * Hook for deleting a LUT
 */
export const useDeleteLut = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => lutService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: lutKeys.lists() });
      queryClient.invalidateQueries({ queryKey: lutKeys.expiryAlerts() });
    },
    onError: (error) => {
      console.error('Failed to delete LUT:', error);
    },
  });
};

/**
 * Hook for renewing a LUT
 */
export const useRenewLut = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ lutId, newLutData }: { lutId: string; newLutData: CreateLutDto }) =>
      lutService.renewLut(lutId, newLutData),
    onSuccess: (newLut, variables) => {
      queryClient.invalidateQueries({ queryKey: lutKeys.detail(variables.lutId) });
      queryClient.invalidateQueries({ queryKey: lutKeys.lists() });
      if (newLut.companyId) {
        queryClient.invalidateQueries({ queryKey: lutKeys.byCompany(newLut.companyId) });
        queryClient.invalidateQueries({ queryKey: lutKeys.active(newLut.companyId) });
        queryClient.invalidateQueries({ queryKey: lutKeys.compliance(newLut.companyId) });
      }
      queryClient.invalidateQueries({ queryKey: lutKeys.expiryAlerts() });
    },
    onError: (error) => {
      console.error('Failed to renew LUT:', error);
    },
  });
};

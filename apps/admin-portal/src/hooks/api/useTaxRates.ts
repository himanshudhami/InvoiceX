import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { taxRateService } from '@/services/api/taxRateService';
import { TaxRate, CreateTaxRateDto, UpdateTaxRateDto, PaginationParams } from '@/services/api/types';
import toast from 'react-hot-toast';

// Query keys for React Query cache management
export const taxRateKeys = {
  all: ['taxRates'] as const,
  lists: () => [...taxRateKeys.all, 'list'] as const,
  list: (params: PaginationParams) => [...taxRateKeys.lists(), params] as const,
  details: () => [...taxRateKeys.all, 'detail'] as const,
  detail: (id: string) => [...taxRateKeys.details(), id] as const,
};

/**
 * Hook for fetching all tax rates
 */
export const useTaxRates = () => {
  return useQuery({
    queryKey: taxRateKeys.lists(),
    queryFn: () => taxRateService.getAll(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Hook for fetching paginated tax rates
 */
export const useTaxRatesPaged = (params: PaginationParams = {}) => {
  return useQuery({
    queryKey: taxRateKeys.list(params),
    queryFn: () => taxRateService.getPaged(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    keepPreviousData: true, // Keep previous data while loading new page
  });
};

/**
 * Hook for fetching a single tax rate by ID
 */
export const useTaxRate = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: taxRateKeys.detail(id),
    queryFn: () => taxRateService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Hook for creating a tax rate
 */
export const useCreateTaxRate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateTaxRateDto) => taxRateService.create(data),
    onSuccess: (newTaxRate) => {
      // Invalidate and refetch tax rates list
      queryClient.invalidateQueries({ queryKey: taxRateKeys.lists() });
      
      // Optionally add to cache
      queryClient.setQueryData(taxRateKeys.detail(newTaxRate.id), newTaxRate);
      
      toast.success('Tax rate created successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to create tax rate';
      toast.error(message);
    },
  });
};

/**
 * Hook for updating a tax rate
 */
export const useUpdateTaxRate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTaxRateDto }) =>
      taxRateService.update(id, data),
    onSuccess: (_, variables) => {
      // Invalidate specific tax rate and lists
      queryClient.invalidateQueries({ queryKey: taxRateKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: taxRateKeys.lists() });
      
      toast.success('Tax rate updated successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to update tax rate';
      toast.error(message);
    },
  });
};

/**
 * Hook for deleting a tax rate
 */
export const useDeleteTaxRate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => taxRateService.delete(id),
    onSuccess: (_, id) => {
      // Remove from cache and invalidate queries
      queryClient.removeQueries({ queryKey: taxRateKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: taxRateKeys.lists() });
      
      toast.success('Tax rate deleted successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to delete tax rate';
      toast.error(message);
    },
  });
};
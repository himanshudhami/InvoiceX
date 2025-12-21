import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { companyService } from '@/services/api/admin/companyService';
import { Company, CreateCompanyDto, UpdateCompanyDto, PaginationParams } from '@/services/api/types';

// Query keys for React Query cache management
export const companyKeys = {
  all: ['companies'] as const,
  lists: () => [...companyKeys.all, 'list'] as const,
  list: (params: PaginationParams) => [...companyKeys.lists(), params] as const,
  details: () => [...companyKeys.all, 'detail'] as const,
  detail: (id: string) => [...companyKeys.details(), id] as const,
};

/**
 * Hook for fetching all companies
 */
export const useCompanies = () => {
  return useQuery({
    queryKey: companyKeys.lists(),
    queryFn: () => companyService.getAll(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Hook for fetching paginated companies
 */
export const useCompaniesPaged = (params: PaginationParams = {}) => {
  return useQuery({
    queryKey: companyKeys.list(params),
    queryFn: () => companyService.getPaged(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    keepPreviousData: true, // Keep previous data while loading new page
  });
};

/**
 * Hook for fetching a single company by ID
 */
export const useCompany = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: companyKeys.detail(id),
    queryFn: () => companyService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Hook for creating a company
 */
export const useCreateCompany = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateCompanyDto) => companyService.create(data),
    onSuccess: () => {
      // Invalidate and refetch companies list
      queryClient.invalidateQueries({ queryKey: companyKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to create company:', error);
    },
  });
};

/**
 * Hook for updating a company
 */
export const useUpdateCompany = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCompanyDto }) =>
      companyService.update(id, data),
    onSuccess: (_, variables) => {
      // Invalidate specific company and lists
      queryClient.invalidateQueries({ queryKey: companyKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: companyKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to update company:', error);
    },
  });
};

/**
 * Hook for deleting a company
 */
export const useDeleteCompany = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => companyService.delete(id),
    onSuccess: () => {
      // Invalidate companies list
      queryClient.invalidateQueries({ queryKey: companyKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete company:', error);
    },
  });
};
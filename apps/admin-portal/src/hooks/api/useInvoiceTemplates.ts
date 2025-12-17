import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { invoiceTemplateService } from '@/services/api/invoiceTemplateService';
import { InvoiceTemplate, CreateInvoiceTemplateDto, UpdateInvoiceTemplateDto, PaginationParams } from '@/services/api/types';
import toast from 'react-hot-toast';

// Query keys for React Query cache management
export const invoiceTemplateKeys = {
  all: ['invoiceTemplates'] as const,
  lists: () => [...invoiceTemplateKeys.all, 'list'] as const,
  list: (params: PaginationParams) => [...invoiceTemplateKeys.lists(), params] as const,
  details: () => [...invoiceTemplateKeys.all, 'detail'] as const,
  detail: (id: string) => [...invoiceTemplateKeys.details(), id] as const,
};

/**
 * Hook for fetching all invoice templates
 */
export const useInvoiceTemplates = () => {
  return useQuery({
    queryKey: invoiceTemplateKeys.lists(),
    queryFn: () => invoiceTemplateService.getAll(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Hook for fetching paginated invoice templates
 */
export const useInvoiceTemplatesPaged = (params: PaginationParams = {}) => {
  return useQuery({
    queryKey: invoiceTemplateKeys.list(params),
    queryFn: () => invoiceTemplateService.getPaged(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    keepPreviousData: true,
  });
};

/**
 * Hook for fetching a single invoice template by ID
 */
export const useInvoiceTemplate = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: invoiceTemplateKeys.detail(id),
    queryFn: () => invoiceTemplateService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Hook for creating an invoice template
 */
export const useCreateInvoiceTemplate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateInvoiceTemplateDto) => invoiceTemplateService.create(data),
    onSuccess: (newTemplate) => {
      queryClient.setQueryData(invoiceTemplateKeys.detail(newTemplate.id), newTemplate);
      
      toast.success('Invoice template created successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to create invoice template';
      toast.error(message);
    },
  });
};

/**
 * Hook for updating an invoice template
 */
export const useUpdateInvoiceTemplate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateInvoiceTemplateDto }) =>
      invoiceTemplateService.update(id, data),
    onSuccess: (updated, variables) => {
      queryClient.setQueryData(invoiceTemplateKeys.detail(variables.id), updated ?? data);
      toast.success('Invoice template updated successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to update invoice template';
      toast.error(message);
    },
  });
};

/**
 * Hook for deleting an invoice template
 */
export const useDeleteInvoiceTemplate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => invoiceTemplateService.delete(id),
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: invoiceTemplateKeys.detail(id) });
      
      toast.success('Invoice template deleted successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to delete invoice template';
      toast.error(error);
    },
  });
};

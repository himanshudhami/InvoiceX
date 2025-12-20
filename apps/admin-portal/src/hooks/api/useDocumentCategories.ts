import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  documentCategoryService,
  DocumentCategory,
  CreateDocumentCategoryDto,
  UpdateDocumentCategoryDto,
} from '@/services/api/documentCategoryService';
import { PaginationParams } from '@/services/api/types';
import toast from 'react-hot-toast';

// Query keys for React Query cache management
export const documentCategoryKeys = {
  all: ['documentCategories'] as const,
  lists: () => [...documentCategoryKeys.all, 'list'] as const,
  list: (params: PaginationParams & { companyId?: string }) => [...documentCategoryKeys.lists(), params] as const,
  byCompany: (companyId: string) => [...documentCategoryKeys.all, 'company', companyId] as const,
  selectList: (companyId: string) => [...documentCategoryKeys.all, 'select', companyId] as const,
  details: () => [...documentCategoryKeys.all, 'detail'] as const,
  detail: (id: string) => [...documentCategoryKeys.details(), id] as const,
};

/**
 * Hook for fetching document categories by company
 */
export const useDocumentCategoriesByCompany = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: documentCategoryKeys.byCompany(companyId),
    queryFn: () => documentCategoryService.getByCompany(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching document categories for select dropdown
 */
export const useDocumentCategorySelectList = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: documentCategoryKeys.selectList(companyId),
    queryFn: () => documentCategoryService.getSelectList(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching paginated document categories
 */
export const useDocumentCategoriesPaged = (params: PaginationParams & { companyId?: string } = {}) => {
  return useQuery({
    queryKey: documentCategoryKeys.list(params),
    queryFn: () => documentCategoryService.getPaged(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching a single document category by ID
 */
export const useDocumentCategory = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: documentCategoryKeys.detail(id),
    queryFn: () => documentCategoryService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for creating a document category
 */
export const useCreateDocumentCategory = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateDocumentCategoryDto) => documentCategoryService.create(data),
    onSuccess: (newCategory) => {
      queryClient.invalidateQueries({ queryKey: documentCategoryKeys.lists() });
      queryClient.invalidateQueries({ queryKey: documentCategoryKeys.byCompany(newCategory.companyId) });
      queryClient.invalidateQueries({ queryKey: documentCategoryKeys.selectList(newCategory.companyId) });
      queryClient.setQueryData(documentCategoryKeys.detail(newCategory.id), newCategory);
      toast.success('Document category created successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to create document category';
      toast.error(message);
    },
  });
};

/**
 * Hook for updating a document category
 */
export const useUpdateDocumentCategory = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateDocumentCategoryDto }) =>
      documentCategoryService.update(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: documentCategoryKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: documentCategoryKeys.lists() });
      toast.success('Document category updated successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to update document category';
      toast.error(message);
    },
  });
};

/**
 * Hook for deleting a document category
 */
export const useDeleteDocumentCategory = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => documentCategoryService.delete(id),
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: documentCategoryKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: documentCategoryKeys.lists() });
      toast.success('Document category deleted successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to delete document category';
      toast.error(message);
    },
  });
};

/**
 * Hook for seeding default document categories
 */
export const useSeedDocumentCategories = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (companyId: string) => documentCategoryService.seedDefaults(companyId),
    onSuccess: (_, companyId) => {
      queryClient.invalidateQueries({ queryKey: documentCategoryKeys.byCompany(companyId) });
      queryClient.invalidateQueries({ queryKey: documentCategoryKeys.lists() });
      toast.success('Default document categories created successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to create default categories';
      toast.error(message);
    },
  });
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  expenseCategoryService,
  ExpenseCategory,
  CreateExpenseCategoryDto,
  UpdateExpenseCategoryDto,
} from '@/services/api/expenseCategoryService';
import { PaginationParams } from '@/services/api/types';
import toast from 'react-hot-toast';

// Query keys for React Query cache management
export const expenseCategoryKeys = {
  all: ['expenseCategories'] as const,
  lists: () => [...expenseCategoryKeys.all, 'list'] as const,
  list: (params: PaginationParams & { companyId?: string }) => [...expenseCategoryKeys.lists(), params] as const,
  byCompany: (companyId: string) => [...expenseCategoryKeys.all, 'company', companyId] as const,
  selectList: (companyId: string) => [...expenseCategoryKeys.all, 'select', companyId] as const,
  details: () => [...expenseCategoryKeys.all, 'detail'] as const,
  detail: (id: string) => [...expenseCategoryKeys.details(), id] as const,
};

/**
 * Hook for fetching expense categories by company
 */
export const useExpenseCategoriesByCompany = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: expenseCategoryKeys.byCompany(companyId),
    queryFn: () => expenseCategoryService.getByCompany(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching expense categories for select dropdown
 */
export const useExpenseCategorySelectList = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: expenseCategoryKeys.selectList(companyId),
    queryFn: () => expenseCategoryService.getSelectList(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching paginated expense categories
 */
export const useExpenseCategoriesPaged = (params: PaginationParams & { companyId?: string } = {}) => {
  return useQuery({
    queryKey: expenseCategoryKeys.list(params),
    queryFn: () => expenseCategoryService.getPaged(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching a single expense category by ID
 */
export const useExpenseCategory = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: expenseCategoryKeys.detail(id),
    queryFn: () => expenseCategoryService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for creating an expense category
 */
export const useCreateExpenseCategory = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateExpenseCategoryDto) => expenseCategoryService.create(data),
    onSuccess: (newCategory) => {
      queryClient.invalidateQueries({ queryKey: expenseCategoryKeys.lists() });
      queryClient.invalidateQueries({ queryKey: expenseCategoryKeys.byCompany(newCategory.companyId) });
      queryClient.invalidateQueries({ queryKey: expenseCategoryKeys.selectList(newCategory.companyId) });
      queryClient.setQueryData(expenseCategoryKeys.detail(newCategory.id), newCategory);
      toast.success('Expense category created successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to create expense category';
      toast.error(message);
    },
  });
};

/**
 * Hook for updating an expense category
 */
export const useUpdateExpenseCategory = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateExpenseCategoryDto }) =>
      expenseCategoryService.update(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: expenseCategoryKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: expenseCategoryKeys.lists() });
      toast.success('Expense category updated successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to update expense category';
      toast.error(message);
    },
  });
};

/**
 * Hook for deleting an expense category
 */
export const useDeleteExpenseCategory = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => expenseCategoryService.delete(id),
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: expenseCategoryKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: expenseCategoryKeys.lists() });
      toast.success('Expense category deleted successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to delete expense category';
      toast.error(message);
    },
  });
};

/**
 * Hook for seeding default expense categories
 */
export const useSeedExpenseCategories = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (companyId: string) => expenseCategoryService.seedDefaults(companyId),
    onSuccess: (_, companyId) => {
      queryClient.invalidateQueries({ queryKey: expenseCategoryKeys.byCompany(companyId) });
      queryClient.invalidateQueries({ queryKey: expenseCategoryKeys.lists() });
      toast.success('Default expense categories created successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to create default categories';
      toast.error(message);
    },
  });
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { stockGroupService } from '@/services/api/inventory';
import type {
  StockGroup,
  CreateStockGroupDto,
  UpdateStockGroupDto,
  StockGroupFilterParams,
} from '@/services/api/types';
import { stockGroupKeys } from './stockGroupKeys';

/**
 * Fetch all stock groups
 */
export const useStockGroups = (companyId?: string) => {
  return useQuery({
    queryKey: stockGroupKeys.list(companyId),
    queryFn: () => stockGroupService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated stock groups with server-side filtering
 */
export const useStockGroupsPaged = (params: StockGroupFilterParams = {}) => {
  return useQuery({
    queryKey: stockGroupKeys.paged(params),
    queryFn: () => stockGroupService.getPaged(params),
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch stock groups hierarchy (tree structure)
 */
export const useStockGroupsHierarchy = (companyId?: string) => {
  return useQuery({
    queryKey: stockGroupKeys.hierarchy(companyId),
    queryFn: () => stockGroupService.getHierarchy(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch active stock groups
 */
export const useActiveStockGroups = (companyId?: string) => {
  return useQuery({
    queryKey: stockGroupKeys.active(companyId),
    queryFn: () => stockGroupService.getActive(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch single stock group by ID
 */
export const useStockGroup = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: stockGroupKeys.detail(id),
    queryFn: () => stockGroupService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch stock group path
 */
export const useStockGroupPath = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: stockGroupKeys.path(id),
    queryFn: () => stockGroupService.getPath(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Create stock group mutation
 */
export const useCreateStockGroup = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateStockGroupDto) => stockGroupService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockGroupKeys.lists() });
    },
  });
};

/**
 * Update stock group mutation
 */
export const useUpdateStockGroup = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateStockGroupDto }) =>
      stockGroupService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: stockGroupKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: stockGroupKeys.lists() });
    },
  });
};

/**
 * Delete stock group mutation
 */
export const useDeleteStockGroup = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => stockGroupService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockGroupKeys.lists() });
    },
  });
};

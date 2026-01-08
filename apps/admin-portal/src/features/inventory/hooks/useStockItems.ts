import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { stockItemService } from '@/services/api/inventory';
import type {
  StockItem,
  CreateStockItemDto,
  UpdateStockItemDto,
  StockItemFilterParams,
} from '@/services/api/types';
import { stockItemKeys } from './stockItemKeys';

/**
 * Fetch all stock items
 */
export const useStockItems = (companyId?: string) => {
  return useQuery({
    queryKey: stockItemKeys.list(companyId),
    queryFn: () => stockItemService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated stock items with server-side filtering
 */
export const useStockItemsPaged = (params: StockItemFilterParams = {}) => {
  return useQuery({
    queryKey: stockItemKeys.paged(params),
    queryFn: () => stockItemService.getPaged(params),
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch active stock items
 */
export const useActiveStockItems = (companyId?: string) => {
  return useQuery({
    queryKey: stockItemKeys.active(companyId),
    queryFn: () => stockItemService.getActive(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch low stock items (below reorder level)
 */
export const useLowStockItems = (companyId?: string) => {
  return useQuery({
    queryKey: stockItemKeys.lowStock(companyId),
    queryFn: () => stockItemService.getLowStock(companyId),
    staleTime: 2 * 60 * 1000, // Refresh more frequently for alerts
  });
};

/**
 * Fetch stock items by group
 */
export const useStockItemsByGroup = (groupId: string, companyId?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: stockItemKeys.byGroup(groupId, companyId),
    queryFn: () => stockItemService.getByGroup(groupId, companyId),
    enabled: enabled && !!groupId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch single stock item by ID
 */
export const useStockItem = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: stockItemKeys.detail(id),
    queryFn: () => stockItemService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch stock item position (warehouse-wise stock)
 */
export const useStockItemPosition = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: stockItemKeys.position(id),
    queryFn: () => stockItemService.getPosition(id),
    enabled: enabled && !!id,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Create stock item mutation
 */
export const useCreateStockItem = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateStockItemDto) => stockItemService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
    },
  });
};

/**
 * Update stock item mutation
 */
export const useUpdateStockItem = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateStockItemDto }) =>
      stockItemService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: stockItemKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
    },
  });
};

/**
 * Delete stock item mutation
 */
export const useDeleteStockItem = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => stockItemService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
    },
  });
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { productionOrderService } from '@/services/api/manufacturing';
import type {
  ProductionOrder,
  CreateProductionOrderDto,
  UpdateProductionOrderDto,
  ReleaseProductionOrderDto,
  StartProductionOrderDto,
  CompleteProductionOrderDto,
  CancelProductionOrderDto,
  ConsumeItemDto,
} from '@/services/api/types';
import { productionOrderKeys } from './productionOrderKeys';
import { stockItemKeys } from '@/features/inventory/hooks/stockItemKeys';
import { stockMovementKeys } from '@/features/inventory/hooks/stockMovementKeys';
import { bomKeys } from './bomKeys';

/**
 * Fetch all production orders
 */
export const useProductionOrders = (companyId?: string) => {
  return useQuery({
    queryKey: productionOrderKeys.list(companyId),
    queryFn: () => productionOrderService.getAll(companyId),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch paginated production orders with server-side filtering
 */
export const useProductionOrdersPaged = (params: {
  pageNumber?: number;
  pageSize?: number;
  companyId?: string;
  searchTerm?: string;
  status?: string;
  bomId?: string;
  finishedGoodId?: string;
  warehouseId?: string;
  fromDate?: string;
  toDate?: string;
} = {}) => {
  return useQuery({
    queryKey: productionOrderKeys.paged(params),
    queryFn: () => productionOrderService.getPaged(params),
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch production orders by status
 */
export const useProductionOrdersByStatus = (status: string, companyId?: string) => {
  return useQuery({
    queryKey: productionOrderKeys.byStatus(status, companyId),
    queryFn: () => productionOrderService.getByStatus(status, companyId),
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch single production order by ID
 */
export const useProductionOrder = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: productionOrderKeys.detail(id),
    queryFn: () => productionOrderService.getById(id),
    enabled: enabled && !!id,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Create production order mutation
 */
export const useCreateProductionOrder = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateProductionOrderDto) => productionOrderService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.lists() });
    },
  });
};

/**
 * Update production order mutation
 */
export const useUpdateProductionOrder = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProductionOrderDto }) =>
      productionOrderService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.lists() });
    },
  });
};

/**
 * Delete production order mutation
 */
export const useDeleteProductionOrder = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => productionOrderService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.lists() });
    },
  });
};

/**
 * Release production order mutation (draft -> released)
 */
export const useReleaseProductionOrder = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data?: ReleaseProductionOrderDto }) =>
      productionOrderService.release(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.lists() });
    },
  });
};

/**
 * Start production order mutation (released -> in_progress)
 */
export const useStartProductionOrder = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data?: StartProductionOrderDto }) =>
      productionOrderService.start(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.lists() });
    },
  });
};

/**
 * Complete production order mutation (in_progress -> completed)
 */
export const useCompleteProductionOrder = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CompleteProductionOrderDto }) =>
      productionOrderService.complete(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockMovementKeys.lists() });
    },
  });
};

/**
 * Cancel production order mutation
 */
export const useCancelProductionOrder = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data?: CancelProductionOrderDto }) =>
      productionOrderService.cancel(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockMovementKeys.lists() });
    },
  });
};

/**
 * Consume item during production mutation
 */
export const useConsumeProductionItem = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ConsumeItemDto }) =>
      productionOrderService.consumeItem(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: productionOrderKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockMovementKeys.lists() });
    },
  });
};

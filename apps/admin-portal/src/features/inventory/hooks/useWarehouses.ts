import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { warehouseService } from '@/services/api/inventory';
import type {
  Warehouse,
  CreateWarehouseDto,
  UpdateWarehouseDto,
  WarehouseFilterParams,
} from '@/services/api/types';
import { warehouseKeys } from './warehouseKeys';

/**
 * Fetch all warehouses
 */
export const useWarehouses = (companyId?: string) => {
  return useQuery({
    queryKey: warehouseKeys.list(companyId),
    queryFn: () => warehouseService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated warehouses with server-side filtering
 */
export const useWarehousesPaged = (params: WarehouseFilterParams = {}) => {
  return useQuery({
    queryKey: warehouseKeys.paged(params),
    queryFn: () => warehouseService.getPaged(params),
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch active warehouses
 */
export const useActiveWarehouses = (companyId?: string) => {
  return useQuery({
    queryKey: warehouseKeys.active(companyId),
    queryFn: () => warehouseService.getActive(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch default warehouse
 */
export const useDefaultWarehouse = (companyId?: string) => {
  return useQuery({
    queryKey: warehouseKeys.default(companyId),
    queryFn: () => warehouseService.getDefault(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch single warehouse by ID
 */
export const useWarehouse = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: warehouseKeys.detail(id),
    queryFn: () => warehouseService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Create warehouse mutation
 */
export const useCreateWarehouse = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateWarehouseDto) => warehouseService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: warehouseKeys.lists() });
    },
  });
};

/**
 * Update warehouse mutation
 */
export const useUpdateWarehouse = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateWarehouseDto }) =>
      warehouseService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: warehouseKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: warehouseKeys.lists() });
    },
  });
};

/**
 * Delete warehouse mutation
 */
export const useDeleteWarehouse = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => warehouseService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: warehouseKeys.lists() });
    },
  });
};

/**
 * Set warehouse as default mutation
 */
export const useSetDefaultWarehouse = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => warehouseService.setDefault(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: warehouseKeys.all });
    },
  });
};

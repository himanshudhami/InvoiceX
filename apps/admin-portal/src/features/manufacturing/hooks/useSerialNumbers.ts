import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { serialNumberService } from '@/services/api/manufacturing';
import type {
  SerialNumber,
  CreateSerialNumberDto,
  UpdateSerialNumberDto,
  BulkCreateSerialNumberDto,
  MarkSerialAsSoldDto,
  SerialNumberFilterParams,
} from '@/services/api/types';
import { serialNumberKeys } from './serialNumberKeys';
import { stockItemKeys } from '@/features/inventory/hooks/stockItemKeys';

/**
 * Fetch all serial numbers
 */
export const useSerialNumbers = (companyId?: string) => {
  return useQuery({
    queryKey: serialNumberKeys.list(companyId),
    queryFn: () => serialNumberService.getAll(companyId),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch paginated serial numbers with server-side filtering
 */
export const useSerialNumbersPaged = (params: SerialNumberFilterParams = {}) => {
  return useQuery({
    queryKey: serialNumberKeys.paged(params),
    queryFn: () => serialNumberService.getPaged(params),
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch serial numbers by stock item
 */
export const useSerialNumbersByStockItem = (stockItemId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: serialNumberKeys.byStockItem(stockItemId),
    queryFn: () => serialNumberService.getByStockItem(stockItemId),
    enabled: enabled && !!stockItemId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch available serial numbers for a stock item
 */
export const useAvailableSerialNumbers = (stockItemId: string, warehouseId?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: serialNumberKeys.available(stockItemId, warehouseId),
    queryFn: () => serialNumberService.getAvailable(stockItemId, warehouseId),
    enabled: enabled && !!stockItemId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch single serial number by ID
 */
export const useSerialNumber = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: serialNumberKeys.detail(id),
    queryFn: () => serialNumberService.getById(id),
    enabled: enabled && !!id,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Create serial number mutation
 */
export const useCreateSerialNumber = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateSerialNumberDto) => serialNumberService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: serialNumberKeys.lists() });
    },
  });
};

/**
 * Bulk create serial numbers mutation
 */
export const useBulkCreateSerialNumbers = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BulkCreateSerialNumberDto) => serialNumberService.bulkCreate(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: serialNumberKeys.lists() });
    },
  });
};

/**
 * Update serial number mutation
 */
export const useUpdateSerialNumber = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateSerialNumberDto }) =>
      serialNumberService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: serialNumberKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: serialNumberKeys.lists() });
    },
  });
};

/**
 * Delete serial number mutation
 */
export const useDeleteSerialNumber = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => serialNumberService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: serialNumberKeys.lists() });
    },
  });
};

/**
 * Mark serial number as sold mutation
 */
export const useMarkSerialAsSold = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: MarkSerialAsSoldDto }) =>
      serialNumberService.markAsSold(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: serialNumberKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: serialNumberKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
    },
  });
};

/**
 * Update serial number status mutation
 */
export const useUpdateSerialStatus = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      serialNumberService.updateStatus(id, status),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: serialNumberKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: serialNumberKeys.lists() });
    },
  });
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { stockMovementService } from '@/services/api/inventory';
import type {
  StockMovement,
  CreateStockMovementDto,
  StockMovementFilterParams,
} from '@/services/api/types';
import { stockMovementKeys } from './stockMovementKeys';
import { stockItemKeys } from './stockItemKeys';

/**
 * Fetch all stock movements
 */
export const useStockMovements = (companyId?: string) => {
  return useQuery({
    queryKey: stockMovementKeys.list(companyId),
    queryFn: () => stockMovementService.getAll(companyId),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch paginated stock movements with server-side filtering
 */
export const useStockMovementsPaged = (params: StockMovementFilterParams = {}) => {
  return useQuery({
    queryKey: stockMovementKeys.paged(params),
    queryFn: () => stockMovementService.getPaged(params),
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single stock movement by ID
 */
export const useStockMovement = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: stockMovementKeys.detail(id),
    queryFn: () => stockMovementService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch stock ledger for an item
 */
export const useStockLedger = (
  itemId: string,
  params?: { warehouseId?: string; fromDate?: string; toDate?: string },
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: stockMovementKeys.ledger(itemId, params),
    queryFn: () => stockMovementService.getLedger(itemId, params),
    enabled: enabled && !!itemId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch current stock position for an item
 */
export const useStockPosition = (itemId: string, warehouseId?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: stockMovementKeys.position(itemId, warehouseId),
    queryFn: () => stockMovementService.getPosition(itemId, warehouseId),
    enabled: enabled && !!itemId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Create stock movement mutation
 */
export const useCreateStockMovement = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateStockMovementDto) => stockMovementService.create(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: stockMovementKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockMovementKeys.ledger(variables.stockItemId) });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.detail(variables.stockItemId) });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.position(variables.stockItemId) });
    },
  });
};

/**
 * Delete stock movement mutation
 */
export const useDeleteStockMovement = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => stockMovementService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockMovementKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
    },
  });
};

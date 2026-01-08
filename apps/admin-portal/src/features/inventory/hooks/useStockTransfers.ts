import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { stockTransferService } from '@/services/api/inventory';
import type {
  StockTransfer,
  CreateStockTransferDto,
  UpdateStockTransferDto,
  StockTransferFilterParams,
  TransferStatus,
} from '@/services/api/types';
import { stockTransferKeys } from './stockTransferKeys';
import { stockItemKeys } from './stockItemKeys';
import { stockMovementKeys } from './stockMovementKeys';

/**
 * Fetch all stock transfers
 */
export const useStockTransfers = (companyId?: string) => {
  return useQuery({
    queryKey: stockTransferKeys.list(companyId),
    queryFn: () => stockTransferService.getAll(companyId),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch paginated stock transfers with server-side filtering
 */
export const useStockTransfersPaged = (params: StockTransferFilterParams = {}) => {
  return useQuery({
    queryKey: stockTransferKeys.paged(params),
    queryFn: () => stockTransferService.getPaged(params),
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch pending stock transfers
 */
export const usePendingStockTransfers = (companyId?: string) => {
  return useQuery({
    queryKey: stockTransferKeys.pending(companyId),
    queryFn: () => stockTransferService.getPending(companyId),
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch stock transfers by status
 */
export const useStockTransfersByStatus = (status: TransferStatus, companyId?: string) => {
  return useQuery({
    queryKey: stockTransferKeys.byStatus(status, companyId),
    queryFn: () => stockTransferService.getByStatus(status, companyId),
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch single stock transfer by ID
 */
export const useStockTransfer = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: stockTransferKeys.detail(id),
    queryFn: () => stockTransferService.getById(id),
    enabled: enabled && !!id,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Create stock transfer mutation
 */
export const useCreateStockTransfer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateStockTransferDto) => stockTransferService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.lists() });
    },
  });
};

/**
 * Update stock transfer mutation
 */
export const useUpdateStockTransfer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateStockTransferDto }) =>
      stockTransferService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.lists() });
    },
  });
};

/**
 * Delete stock transfer mutation
 */
export const useDeleteStockTransfer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => stockTransferService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.lists() });
    },
  });
};

/**
 * Dispatch stock transfer mutation (draft -> in_transit)
 */
export const useDispatchStockTransfer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => stockTransferService.dispatch(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockMovementKeys.lists() });
    },
  });
};

/**
 * Complete stock transfer mutation (in_transit -> completed)
 */
export const useCompleteStockTransfer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => stockTransferService.complete(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockMovementKeys.lists() });
    },
  });
};

/**
 * Cancel stock transfer mutation
 */
export const useCancelStockTransfer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => stockTransferService.cancel(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockItemKeys.lists() });
      queryClient.invalidateQueries({ queryKey: stockMovementKeys.lists() });
    },
  });
};

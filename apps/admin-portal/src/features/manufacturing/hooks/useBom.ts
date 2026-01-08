import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { bomService } from '@/services/api/manufacturing';
import type {
  BillOfMaterials,
  CreateBomDto,
  UpdateBomDto,
  CopyBomDto,
} from '@/services/api/types';
import { bomKeys } from './bomKeys';
import { stockItemKeys } from '@/features/inventory/hooks/stockItemKeys';

/**
 * Fetch all BOMs
 */
export const useBoms = (companyId?: string) => {
  return useQuery({
    queryKey: bomKeys.list(companyId),
    queryFn: () => bomService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated BOMs with server-side filtering
 */
export const useBomsPaged = (params: {
  pageNumber?: number;
  pageSize?: number;
  companyId?: string;
  searchTerm?: string;
  finishedGoodId?: string;
  isActive?: boolean;
} = {}) => {
  return useQuery({
    queryKey: bomKeys.paged(params),
    queryFn: () => bomService.getPaged(params),
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch active BOMs
 */
export const useActiveBoms = (companyId?: string) => {
  return useQuery({
    queryKey: bomKeys.active(companyId),
    queryFn: () => bomService.getActive(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch BOMs by finished good
 */
export const useBomsByFinishedGood = (finishedGoodId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bomKeys.byFinishedGood(finishedGoodId),
    queryFn: () => bomService.getByFinishedGood(finishedGoodId),
    enabled: enabled && !!finishedGoodId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch active BOM for a specific product
 */
export const useActiveBomForProduct = (finishedGoodId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bomKeys.activeForProduct(finishedGoodId),
    queryFn: () => bomService.getActiveBomForProduct(finishedGoodId),
    enabled: enabled && !!finishedGoodId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch single BOM by ID
 */
export const useBom = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: bomKeys.detail(id),
    queryFn: () => bomService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Create BOM mutation
 */
export const useCreateBom = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBomDto) => bomService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bomKeys.lists() });
    },
  });
};

/**
 * Update BOM mutation
 */
export const useUpdateBom = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBomDto }) =>
      bomService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: bomKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: bomKeys.lists() });
    },
  });
};

/**
 * Delete BOM mutation
 */
export const useDeleteBom = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => bomService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bomKeys.lists() });
    },
  });
};

/**
 * Copy BOM mutation
 */
export const useCopyBom = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CopyBomDto }) =>
      bomService.copy(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bomKeys.lists() });
    },
  });
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { unitOfMeasureService } from '@/services/api/inventory';
import type {
  UnitOfMeasure,
  CreateUnitOfMeasureDto,
  UpdateUnitOfMeasureDto,
  UnitOfMeasureFilterParams,
} from '@/services/api/types';
import { unitOfMeasureKeys } from './unitOfMeasureKeys';

/**
 * Fetch all units of measure
 */
export const useUnitsOfMeasure = (companyId?: string) => {
  return useQuery({
    queryKey: unitOfMeasureKeys.list(companyId),
    queryFn: () => unitOfMeasureService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated units of measure with server-side filtering
 */
export const useUnitsOfMeasurePaged = (params: UnitOfMeasureFilterParams = {}) => {
  return useQuery({
    queryKey: unitOfMeasureKeys.paged(params),
    queryFn: () => unitOfMeasureService.getPaged(params),
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch system units (read-only)
 */
export const useSystemUnits = () => {
  return useQuery({
    queryKey: unitOfMeasureKeys.system(),
    queryFn: () => unitOfMeasureService.getSystemUnits(),
    staleTime: 30 * 60 * 1000, // System units rarely change
  });
};

/**
 * Fetch single unit of measure by ID
 */
export const useUnitOfMeasure = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: unitOfMeasureKeys.detail(id),
    queryFn: () => unitOfMeasureService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Create unit of measure mutation
 */
export const useCreateUnitOfMeasure = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateUnitOfMeasureDto) => unitOfMeasureService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: unitOfMeasureKeys.lists() });
    },
  });
};

/**
 * Update unit of measure mutation
 */
export const useUpdateUnitOfMeasure = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateUnitOfMeasureDto }) =>
      unitOfMeasureService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: unitOfMeasureKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: unitOfMeasureKeys.lists() });
    },
  });
};

/**
 * Delete unit of measure mutation
 */
export const useDeleteUnitOfMeasure = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => unitOfMeasureService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: unitOfMeasureKeys.lists() });
    },
  });
};

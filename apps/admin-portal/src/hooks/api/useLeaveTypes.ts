import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { leaveTypeService } from '@/services/api/hr/leave/leaveTypeService';
import { CreateLeaveTypeDto, UpdateLeaveTypeDto, PaginationParams } from '@/services/api/types';
import toast from 'react-hot-toast';

export const LEAVE_TYPE_QUERY_KEYS = {
  all: ['leaveTypes'] as const,
  lists: () => [...LEAVE_TYPE_QUERY_KEYS.all, 'list'] as const,
  list: (companyId?: string) => [...LEAVE_TYPE_QUERY_KEYS.lists(), companyId] as const,
  details: () => [...LEAVE_TYPE_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...LEAVE_TYPE_QUERY_KEYS.details(), id] as const,
  paged: (params: PaginationParams) => [...LEAVE_TYPE_QUERY_KEYS.all, 'paged', params] as const,
} as const;

export const useLeaveTypes = (companyId?: string) => {
  return useQuery({
    queryKey: LEAVE_TYPE_QUERY_KEYS.list(companyId),
    queryFn: () => leaveTypeService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

export const useLeaveTypesPaged = (params: PaginationParams & { companyId?: string } = {}) => {
  return useQuery({
    queryKey: LEAVE_TYPE_QUERY_KEYS.paged(params),
    queryFn: () => leaveTypeService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

export const useLeaveType = (id: string, enabled = true) => {
  return useQuery({
    queryKey: LEAVE_TYPE_QUERY_KEYS.detail(id),
    queryFn: () => leaveTypeService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

export const useCreateLeaveType = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateLeaveTypeDto) => leaveTypeService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: LEAVE_TYPE_QUERY_KEYS.lists() });
      toast.success('Leave type created successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to create leave type';
      toast.error(message);
    },
  });
};

export const useUpdateLeaveType = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateLeaveTypeDto }) =>
      leaveTypeService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: LEAVE_TYPE_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: LEAVE_TYPE_QUERY_KEYS.lists() });
      toast.success('Leave type updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update leave type';
      toast.error(message);
    },
  });
};

export const useDeleteLeaveType = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => leaveTypeService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: LEAVE_TYPE_QUERY_KEYS.lists() });
      toast.success('Leave type deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete leave type';
      toast.error(message);
    },
  });
};

export const useToggleLeaveTypeActive = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => leaveTypeService.toggleActive(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: LEAVE_TYPE_QUERY_KEYS.lists() });
      toast.success('Leave type status updated!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update leave type status';
      toast.error(message);
    },
  });
};

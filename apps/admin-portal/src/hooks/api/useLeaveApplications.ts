import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { leaveApplicationService } from '@/services/api/leaveApplicationService';
import {
  CreateLeaveApplicationDto,
  UpdateLeaveApplicationDto,
  ApproveLeaveDto,
  RejectLeaveDto,
  LeaveApplicationFilterParams,
} from '@/services/api/types';
import toast from 'react-hot-toast';
import { LEAVE_BALANCE_QUERY_KEYS } from './useLeaveBalances';

export const LEAVE_APPLICATION_QUERY_KEYS = {
  all: ['leaveApplications'] as const,
  lists: () => [...LEAVE_APPLICATION_QUERY_KEYS.all, 'list'] as const,
  list: (params: LeaveApplicationFilterParams) => [...LEAVE_APPLICATION_QUERY_KEYS.lists(), params] as const,
  details: () => [...LEAVE_APPLICATION_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...LEAVE_APPLICATION_QUERY_KEYS.details(), id] as const,
  pending: (companyId?: string) => [...LEAVE_APPLICATION_QUERY_KEYS.all, 'pending', companyId] as const,
  byEmployee: (employeeId: string) => [...LEAVE_APPLICATION_QUERY_KEYS.all, 'employee', employeeId] as const,
  calendar: (companyId: string, year: number, month: number) =>
    [...LEAVE_APPLICATION_QUERY_KEYS.all, 'calendar', companyId, year, month] as const,
  paged: (params: LeaveApplicationFilterParams) => [...LEAVE_APPLICATION_QUERY_KEYS.all, 'paged', params] as const,
} as const;

export const useLeaveApplications = (params: LeaveApplicationFilterParams = {}) => {
  return useQuery({
    queryKey: LEAVE_APPLICATION_QUERY_KEYS.list(params),
    queryFn: () => leaveApplicationService.getAll(params),
    staleTime: 30 * 1000,
  });
};

export const useLeaveApplicationsPaged = (params: LeaveApplicationFilterParams = {}) => {
  return useQuery({
    queryKey: LEAVE_APPLICATION_QUERY_KEYS.paged(params),
    queryFn: () => leaveApplicationService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

export const useLeaveApplication = (id: string, enabled = true) => {
  return useQuery({
    queryKey: LEAVE_APPLICATION_QUERY_KEYS.detail(id),
    queryFn: () => leaveApplicationService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

export const usePendingLeaveApprovals = (companyId?: string) => {
  return useQuery({
    queryKey: LEAVE_APPLICATION_QUERY_KEYS.pending(companyId),
    queryFn: () => leaveApplicationService.getPendingApprovals(companyId),
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000, // Refetch every minute for pending approvals
  });
};

export const useEmployeeLeaveApplications = (employeeId: string, enabled = true) => {
  return useQuery({
    queryKey: LEAVE_APPLICATION_QUERY_KEYS.byEmployee(employeeId),
    queryFn: () => leaveApplicationService.getByEmployee(employeeId),
    enabled: enabled && !!employeeId,
    staleTime: 30 * 1000,
  });
};

export const useLeaveCalendar = (companyId: string, year: number, month: number, enabled = true) => {
  return useQuery({
    queryKey: LEAVE_APPLICATION_QUERY_KEYS.calendar(companyId, year, month),
    queryFn: () => leaveApplicationService.getCalendar(companyId, year, month),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

export const useCreateLeaveApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateLeaveApplicationDto) => leaveApplicationService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.pending() });
      toast.success('Leave application submitted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to submit leave application';
      toast.error(message);
    },
  });
};

export const useUpdateLeaveApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateLeaveApplicationDto }) =>
      leaveApplicationService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.lists() });
      toast.success('Leave application updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update leave application';
      toast.error(message);
    },
  });
};

export const useApproveLeaveApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data, approvedBy }: { id: string; data: ApproveLeaveDto; approvedBy: string }) =>
      leaveApplicationService.approve(id, data, approvedBy),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.pending() });
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.lists() });
      toast.success('Leave application approved!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to approve leave application';
      toast.error(message);
    },
  });
};

export const useRejectLeaveApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data, rejectedBy }: { id: string; data: RejectLeaveDto; rejectedBy: string }) =>
      leaveApplicationService.reject(id, data, rejectedBy),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.pending() });
      toast.success('Leave application rejected.');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to reject leave application';
      toast.error(message);
    },
  });
};

export const useCancelLeaveApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => leaveApplicationService.cancel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.pending() });
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.lists() });
      toast.success('Leave application cancelled.');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to cancel leave application';
      toast.error(message);
    },
  });
};

export const useWithdrawLeaveApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => leaveApplicationService.withdraw(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.pending() });
      toast.success('Leave application withdrawn.');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to withdraw leave application';
      toast.error(message);
    },
  });
};

export const useDeleteLeaveApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => leaveApplicationService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: LEAVE_APPLICATION_QUERY_KEYS.pending() });
      toast.success('Leave application deleted.');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete leave application';
      toast.error(message);
    },
  });
};

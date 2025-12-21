import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { leaveBalanceService } from '@/services/api/hr/leave/leaveBalanceService';
import {
  CreateLeaveBalanceDto,
  UpdateLeaveBalanceDto,
  AdjustLeaveBalanceDto,
  LeaveBalanceFilterParams,
} from '@/services/api/types';
import toast from 'react-hot-toast';

export const LEAVE_BALANCE_QUERY_KEYS = {
  all: ['leaveBalances'] as const,
  lists: () => [...LEAVE_BALANCE_QUERY_KEYS.all, 'list'] as const,
  list: (params: LeaveBalanceFilterParams) => [...LEAVE_BALANCE_QUERY_KEYS.lists(), params] as const,
  details: () => [...LEAVE_BALANCE_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...LEAVE_BALANCE_QUERY_KEYS.details(), id] as const,
  byEmployee: (employeeId: string, financialYear?: string) =>
    [...LEAVE_BALANCE_QUERY_KEYS.all, 'employee', employeeId, financialYear] as const,
  summary: (employeeId: string, financialYear: string) =>
    [...LEAVE_BALANCE_QUERY_KEYS.all, 'summary', employeeId, financialYear] as const,
  paged: (params: LeaveBalanceFilterParams) => [...LEAVE_BALANCE_QUERY_KEYS.all, 'paged', params] as const,
} as const;

export const useLeaveBalances = (params: LeaveBalanceFilterParams = {}) => {
  return useQuery({
    queryKey: LEAVE_BALANCE_QUERY_KEYS.list(params),
    queryFn: () => leaveBalanceService.getAll(params),
    staleTime: 30 * 1000,
  });
};

export const useLeaveBalancesPaged = (params: LeaveBalanceFilterParams = {}) => {
  return useQuery({
    queryKey: LEAVE_BALANCE_QUERY_KEYS.paged(params),
    queryFn: () => leaveBalanceService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

export const useLeaveBalance = (id: string, enabled = true) => {
  return useQuery({
    queryKey: LEAVE_BALANCE_QUERY_KEYS.detail(id),
    queryFn: () => leaveBalanceService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

export const useEmployeeLeaveBalances = (employeeId: string, financialYear?: string, enabled = true) => {
  return useQuery({
    queryKey: LEAVE_BALANCE_QUERY_KEYS.byEmployee(employeeId, financialYear),
    queryFn: () => leaveBalanceService.getByEmployee(employeeId, financialYear),
    enabled: enabled && !!employeeId,
    staleTime: 30 * 1000,
  });
};

export const useEmployeeLeaveSummary = (employeeId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: LEAVE_BALANCE_QUERY_KEYS.summary(employeeId, financialYear),
    queryFn: () => leaveBalanceService.getEmployeeSummary(employeeId, financialYear),
    enabled: enabled && !!employeeId && !!financialYear,
    staleTime: 30 * 1000,
  });
};

export const useCreateLeaveBalance = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateLeaveBalanceDto) => leaveBalanceService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.lists() });
      toast.success('Leave balance created successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to create leave balance';
      toast.error(message);
    },
  });
};

export const useUpdateLeaveBalance = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateLeaveBalanceDto }) =>
      leaveBalanceService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.lists() });
      toast.success('Leave balance updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update leave balance';
      toast.error(message);
    },
  });
};

export const useAdjustLeaveBalance = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AdjustLeaveBalanceDto }) =>
      leaveBalanceService.adjust(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.lists() });
      toast.success('Leave balance adjusted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to adjust leave balance';
      toast.error(message);
    },
  });
};

export const useDeleteLeaveBalance = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => leaveBalanceService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.lists() });
      toast.success('Leave balance deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete leave balance';
      toast.error(message);
    },
  });
};

export const useInitializeLeaveBalances = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ companyId, financialYear }: { companyId: string; financialYear: string }) =>
      leaveBalanceService.initializeForYear(companyId, financialYear),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.lists() });
      toast.success(`Initialized leave balances for ${result.created} employees!`);
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to initialize leave balances';
      toast.error(message);
    },
  });
};

export const useCarryForwardLeaveBalances = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ companyId, fromYear, toYear }: { companyId: string; fromYear: string; toYear: string }) =>
      leaveBalanceService.carryForward(companyId, fromYear, toYear),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: LEAVE_BALANCE_QUERY_KEYS.lists() });
      toast.success(`Carried forward balances for ${result.processed} employees!`);
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to carry forward leave balances';
      toast.error(message);
    },
  });
};

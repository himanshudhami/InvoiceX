import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { subscriptionService } from '@/services/api/finance/subscriptions/subscriptionService';
import {
  Subscription,
  SubscriptionAssignment,
  CreateSubscriptionDto,
  UpdateSubscriptionDto,
  CreateSubscriptionAssignmentDto,
  RevokeSubscriptionAssignmentDto,
  SubscriptionMonthlyExpense,
  SubscriptionCostReport,
  PaginationParams,
} from '@/services/api/types';

const subsKey = (params?: PaginationParams) => ['subscriptions', params];
const subAssignmentsKey = (subId: string) => ['subscription-assignments', subId];

export const useSubscriptions = (params: PaginationParams = { pageNumber: 1, pageSize: 25 }) => {
  return useQuery({
    queryKey: subsKey(params),
    queryFn: () => subscriptionService.getPaged(params),
    staleTime: 1000 * 30,
  });
};

export const useCreateSubscription = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateSubscriptionDto) => subscriptionService.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['subscriptions'] }),
  });
};

export const useUpdateSubscription = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateSubscriptionDto }) =>
      subscriptionService.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['subscriptions'] }),
  });
};

export const useDeleteSubscription = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => subscriptionService.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['subscriptions'] }),
  });
};

export const useSubscriptionAssignments = (subscriptionId: string, enabled = true) => {
  return useQuery({
    queryKey: subAssignmentsKey(subscriptionId),
    queryFn: () => subscriptionService.getAssignments(subscriptionId),
    enabled: enabled && !!subscriptionId,
    staleTime: 1000 * 30,
  });
};

export const useAssignSubscription = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ subscriptionId, data }: { subscriptionId: string; data: CreateSubscriptionAssignmentDto }) =>
      subscriptionService.assign(subscriptionId, data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: subsKey() });
      qc.invalidateQueries({ queryKey: subAssignmentsKey(variables.subscriptionId) });
    },
  });
};

export const useRevokeSubscriptionAssignment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ assignmentId, data }: { assignmentId: string; data: RevokeSubscriptionAssignmentDto }) =>
      subscriptionService.revokeAssignment(assignmentId, data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: subsKey() });
    },
  });
};

export const usePauseSubscription = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, pausedOn }: { id: string; pausedOn?: string }) => subscriptionService.pause(id, pausedOn),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['subscriptions'] });
    },
  });
};

export const useResumeSubscription = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, resumedOn }: { id: string; resumedOn?: string }) => subscriptionService.resume(id, resumedOn),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['subscriptions'] });
    },
  });
};

export const useCancelSubscription = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, cancelledOn }: { id: string; cancelledOn?: string }) =>
      subscriptionService.cancel(id, cancelledOn),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['subscriptions'] });
    },
  });
};

export const useSubscriptionMonthlyExpenses = (
  year: number,
  month?: number,
  companyId?: string,
  enabled: boolean = true,
) => {
  return useQuery({
    queryKey: ['subscription-expenses', 'monthly', year, month, companyId],
    queryFn: () => subscriptionService.getMonthlyExpenses(year, month, companyId),
    enabled: enabled && year > 0,
    staleTime: 1000 * 60, // 1 minute
  });
};

export const useSubscriptionCostReport = (companyId?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: ['subscription-expenses', 'report', companyId],
    queryFn: () => subscriptionService.getCostReport(companyId),
    enabled,
    staleTime: 1000 * 60, // 1 minute
  });
};



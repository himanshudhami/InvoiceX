import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  approvalTemplateService,
  approvalWorkflowService,
} from '@/services/api/workflows/approvalWorkflowService';
import {
  CreateApprovalTemplateDto,
  UpdateApprovalTemplateDto,
  CreateApprovalStepDto,
  UpdateApprovalStepDto,
  ReorderStepsDto,
  ApproveRequestDto,
  RejectRequestDto,
} from 'shared-types';
import toast from 'react-hot-toast';

// Query Keys
export const APPROVAL_TEMPLATE_QUERY_KEYS = {
  all: ['approvalTemplates'] as const,
  lists: () => [...APPROVAL_TEMPLATE_QUERY_KEYS.all, 'list'] as const,
  list: (companyId: string, activityType?: string) =>
    [...APPROVAL_TEMPLATE_QUERY_KEYS.lists(), companyId, activityType] as const,
  details: () => [...APPROVAL_TEMPLATE_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...APPROVAL_TEMPLATE_QUERY_KEYS.details(), id] as const,
} as const;

export const APPROVAL_WORKFLOW_QUERY_KEYS = {
  all: ['approvalWorkflows'] as const,
  pending: (employeeId: string) => [...APPROVAL_WORKFLOW_QUERY_KEYS.all, 'pending', employeeId] as const,
  pendingCount: (employeeId: string) => [...APPROVAL_WORKFLOW_QUERY_KEYS.all, 'pendingCount', employeeId] as const,
  details: () => [...APPROVAL_WORKFLOW_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...APPROVAL_WORKFLOW_QUERY_KEYS.details(), id] as const,
  activity: (activityType: string, activityId: string) =>
    [...APPROVAL_WORKFLOW_QUERY_KEYS.all, 'activity', activityType, activityId] as const,
  byRequestor: (requestorId: string, status?: string) =>
    [...APPROVAL_WORKFLOW_QUERY_KEYS.all, 'requestor', requestorId, status] as const,
} as const;

// ==================== Template Hooks ====================

export const useApprovalTemplates = (companyId: string, activityType?: string, enabled = true) => {
  return useQuery({
    queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.list(companyId, activityType),
    queryFn: () => approvalTemplateService.getByCompany(companyId, activityType),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

export const useApprovalTemplate = (id: string, enabled = true) => {
  return useQuery({
    queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.detail(id),
    queryFn: () => approvalTemplateService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

export const useCreateApprovalTemplate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateApprovalTemplateDto) => approvalTemplateService.create(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.lists() });
      toast.success('Approval template created successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to create template';
      toast.error(message);
    },
  });
};

export const useUpdateApprovalTemplate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateApprovalTemplateDto }) =>
      approvalTemplateService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.lists() });
      toast.success('Approval template updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update template';
      toast.error(message);
    },
  });
};

export const useDeleteApprovalTemplate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => approvalTemplateService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.lists() });
      toast.success('Approval template deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete template';
      toast.error(message);
    },
  });
};

export const useSetTemplateAsDefault = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => approvalTemplateService.setAsDefault(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.lists() });
      toast.success('Template set as default!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to set template as default';
      toast.error(message);
    },
  });
};

// ==================== Step Hooks ====================

export const useAddApprovalStep = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ templateId, data }: { templateId: string; data: CreateApprovalStepDto }) =>
      approvalTemplateService.addStep(templateId, data),
    onSuccess: (_, { templateId }) => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.detail(templateId) });
      toast.success('Step added successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to add step';
      toast.error(message);
    },
  });
};

export const useUpdateApprovalStep = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ templateId, stepId, data }: { templateId: string; stepId: string; data: UpdateApprovalStepDto }) =>
      approvalTemplateService.updateStep(templateId, stepId, data),
    onSuccess: (_, { templateId }) => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.detail(templateId) });
      toast.success('Step updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update step';
      toast.error(message);
    },
  });
};

export const useDeleteApprovalStep = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ templateId, stepId }: { templateId: string; stepId: string }) =>
      approvalTemplateService.deleteStep(templateId, stepId),
    onSuccess: (_, { templateId }) => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.detail(templateId) });
      toast.success('Step deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete step';
      toast.error(message);
    },
  });
};

export const useReorderApprovalSteps = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ templateId, data }: { templateId: string; data: ReorderStepsDto }) =>
      approvalTemplateService.reorderSteps(templateId, data),
    onSuccess: (_, { templateId }) => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_TEMPLATE_QUERY_KEYS.detail(templateId) });
      toast.success('Steps reordered successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to reorder steps';
      toast.error(message);
    },
  });
};

// ==================== Workflow Hooks ====================

export const usePendingApprovals = (employeeId: string, enabled = true) => {
  return useQuery({
    queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.pending(employeeId),
    queryFn: () => approvalWorkflowService.getPendingApprovals(employeeId),
    enabled: enabled && !!employeeId,
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000, // Refetch every minute
  });
};

export const usePendingApprovalsCount = (employeeId: string, enabled = true) => {
  return useQuery({
    queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.pendingCount(employeeId),
    queryFn: () => approvalWorkflowService.getPendingApprovalsCount(employeeId),
    enabled: enabled && !!employeeId,
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000,
  });
};

export const useApprovalRequestDetails = (requestId: string, enabled = true) => {
  return useQuery({
    queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.detail(requestId),
    queryFn: () => approvalWorkflowService.getRequestDetails(requestId),
    enabled: enabled && !!requestId,
    staleTime: 30 * 1000,
  });
};

export const useActivityApprovalStatus = (activityType: string, activityId: string, enabled = true) => {
  return useQuery({
    queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.activity(activityType, activityId),
    queryFn: () => approvalWorkflowService.getActivityApprovalStatus(activityType, activityId),
    enabled: enabled && !!activityType && !!activityId,
    staleTime: 30 * 1000,
  });
};

export const useRequestsByRequestor = (requestorId: string, status?: string, enabled = true) => {
  return useQuery({
    queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.byRequestor(requestorId, status),
    queryFn: () => approvalWorkflowService.getRequestsByRequestor(requestorId, status),
    enabled: enabled && !!requestorId,
    staleTime: 30 * 1000,
  });
};

export const useApproveRequest = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ requestId, approverId, data }: { requestId: string; approverId: string; data: ApproveRequestDto }) =>
      approvalWorkflowService.approve(requestId, approverId, data),
    onSuccess: (_, { requestId }) => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.detail(requestId) });
      queryClient.invalidateQueries({ queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.all });
      toast.success('Request approved successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to approve request';
      toast.error(message);
    },
  });
};

export const useRejectRequest = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ requestId, approverId, data }: { requestId: string; approverId: string; data: RejectRequestDto }) =>
      approvalWorkflowService.reject(requestId, approverId, data),
    onSuccess: (_, { requestId }) => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.detail(requestId) });
      queryClient.invalidateQueries({ queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.all });
      toast.success('Request rejected.');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to reject request';
      toast.error(message);
    },
  });
};

export const useCancelRequest = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ requestId, requestorId }: { requestId: string; requestorId: string }) =>
      approvalWorkflowService.cancel(requestId, requestorId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: APPROVAL_WORKFLOW_QUERY_KEYS.all });
      toast.success('Request cancelled.');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to cancel request';
      toast.error(message);
    },
  });
};

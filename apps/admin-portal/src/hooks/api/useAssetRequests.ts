import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { assetRequestService } from '@/services/api/assets/assetRequestService';
import {
  AssetRequestSummary,
  AssetRequestDetail,
  AssetRequestStats,
  CreateAssetRequestDto,
  UpdateAssetRequestDto,
  ApproveAssetRequestDto,
  RejectAssetRequestDto,
  CancelAssetRequestDto,
  FulfillAssetRequestDto,
} from '@/services/api/types';

// Query keys
const assetRequestKeys = {
  all: ['asset-requests'] as const,
  byCompany: (companyId: string, status?: string) =>
    [...assetRequestKeys.all, 'company', companyId, status] as const,
  byEmployee: (employeeId: string, status?: string) =>
    [...assetRequestKeys.all, 'employee', employeeId, status] as const,
  pending: (companyId: string) => [...assetRequestKeys.all, 'pending', companyId] as const,
  unfulfilled: (companyId: string) => [...assetRequestKeys.all, 'unfulfilled', companyId] as const,
  stats: (companyId: string) => [...assetRequestKeys.all, 'stats', companyId] as const,
  detail: (id: string) => [...assetRequestKeys.all, 'detail', id] as const,
};

export const useAssetRequest = (id: string, enabled = true) => {
  return useQuery({
    queryKey: assetRequestKeys.detail(id),
    queryFn: () => assetRequestService.getById(id),
    enabled: enabled && !!id,
    staleTime: 1000 * 30,
  });
};

export const useAssetRequestsByCompany = (companyId: string, status?: string, enabled = true) => {
  return useQuery({
    queryKey: assetRequestKeys.byCompany(companyId, status),
    queryFn: () => assetRequestService.getByCompany(companyId, status),
    enabled: enabled && !!companyId,
    staleTime: 1000 * 30,
  });
};

export const useAssetRequestsByEmployee = (employeeId: string, status?: string, enabled = true) => {
  return useQuery({
    queryKey: assetRequestKeys.byEmployee(employeeId, status),
    queryFn: () => assetRequestService.getByEmployee(employeeId, status),
    enabled: enabled && !!employeeId,
    staleTime: 1000 * 30,
  });
};

export const usePendingAssetRequests = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: assetRequestKeys.pending(companyId),
    queryFn: () => assetRequestService.getPending(companyId),
    enabled: enabled && !!companyId,
    staleTime: 1000 * 30,
  });
};

export const useUnfulfilledAssetRequests = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: assetRequestKeys.unfulfilled(companyId),
    queryFn: () => assetRequestService.getUnfulfilled(companyId),
    enabled: enabled && !!companyId,
    staleTime: 1000 * 30,
  });
};

export const useAssetRequestStats = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: assetRequestKeys.stats(companyId),
    queryFn: () => assetRequestService.getStats(companyId),
    enabled: enabled && !!companyId,
    staleTime: 1000 * 30,
  });
};

export const useCreateAssetRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      employeeId,
      companyId,
      data,
    }: {
      employeeId: string;
      companyId: string;
      data: CreateAssetRequestDto;
    }) => assetRequestService.create(employeeId, companyId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: assetRequestKeys.all });
    },
  });
};

export const useUpdateAssetRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, employeeId, data }: { id: string; employeeId: string; data: UpdateAssetRequestDto }) =>
      assetRequestService.update(id, employeeId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: assetRequestKeys.all });
    },
  });
};

export const useApproveAssetRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, approvedBy, data }: { id: string; approvedBy: string; data: ApproveAssetRequestDto }) =>
      assetRequestService.approve(id, approvedBy, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: assetRequestKeys.all });
    },
  });
};

export const useRejectAssetRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, rejectedBy, data }: { id: string; rejectedBy: string; data: RejectAssetRequestDto }) =>
      assetRequestService.reject(id, rejectedBy, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: assetRequestKeys.all });
    },
  });
};

export const useCancelAssetRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, cancelledBy, data }: { id: string; cancelledBy: string; data: CancelAssetRequestDto }) =>
      assetRequestService.cancel(id, cancelledBy, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: assetRequestKeys.all });
    },
  });
};

export const useFulfillAssetRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, fulfilledBy, data }: { id: string; fulfilledBy: string; data: FulfillAssetRequestDto }) =>
      assetRequestService.fulfill(id, fulfilledBy, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: assetRequestKeys.all });
      qc.invalidateQueries({ queryKey: ['assets'] });
    },
  });
};

export const useWithdrawAssetRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, employeeId, reason }: { id: string; employeeId: string; reason?: string }) =>
      assetRequestService.withdraw(id, employeeId, reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: assetRequestKeys.all });
    },
  });
};

export const useDeleteAssetRequest = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => assetRequestService.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: assetRequestKeys.all });
    },
  });
};

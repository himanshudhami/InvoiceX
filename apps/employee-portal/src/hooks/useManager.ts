import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { managerApi } from '@/api/manager'
import type {
  ManagerDashboard,
  DirectReports,
  EmployeeHierarchy,
  TeamLeaveApplication,
  PendingApproval,
  ApprovalRequestDetail,
  ApproveRequestDto,
  RejectRequestDto,
  AssetRequestSummary,
} from '@/types'

// Query keys
export const managerKeys = {
  all: ['manager'] as const,
  dashboard: () => [...managerKeys.all, 'dashboard'] as const,
  team: () => [...managerKeys.all, 'team'] as const,
  teamAll: () => [...managerKeys.all, 'team', 'all'] as const,
  teamLeaves: (status?: string) => [...managerKeys.all, 'team', 'leaves', status] as const,
  teamAssetRequests: (status?: string) => [...managerKeys.all, 'team', 'asset-requests', status] as const,
  approvals: () => [...managerKeys.all, 'approvals'] as const,
  pendingApprovals: () => [...managerKeys.approvals(), 'pending'] as const,
  pendingAssetApprovals: () => [...managerKeys.approvals(), 'pending', 'asset'] as const,
  pendingCount: () => [...managerKeys.approvals(), 'count'] as const,
  approvalDetail: (requestId: string) => [...managerKeys.approvals(), requestId] as const,
}

// Dashboard hook
export function useManagerDashboard() {
  return useQuery<ManagerDashboard>({
    queryKey: managerKeys.dashboard(),
    queryFn: managerApi.getDashboard,
  })
}

// Team hooks
export function useMyTeam() {
  return useQuery<DirectReports>({
    queryKey: managerKeys.team(),
    queryFn: managerApi.getMyTeam,
  })
}

export function useAllSubordinates() {
  return useQuery<EmployeeHierarchy[]>({
    queryKey: managerKeys.teamAll(),
    queryFn: managerApi.getAllSubordinates,
  })
}

export function useTeamLeaves(status?: string) {
  return useQuery<TeamLeaveApplication[]>({
    queryKey: managerKeys.teamLeaves(status),
    queryFn: () => managerApi.getTeamLeaves(status),
  })
}

export function useTeamAssetRequests(status?: string) {
  return useQuery<AssetRequestSummary[]>({
    queryKey: managerKeys.teamAssetRequests(status),
    queryFn: () => managerApi.getTeamAssetRequests(status),
  })
}

// Approval hooks
export function usePendingApprovals() {
  return useQuery<PendingApproval[]>({
    queryKey: managerKeys.pendingApprovals(),
    queryFn: managerApi.getPendingApprovals,
  })
}

export function usePendingAssetApprovals() {
  return useQuery<PendingApproval[]>({
    queryKey: managerKeys.pendingAssetApprovals(),
    queryFn: managerApi.getPendingAssetApprovals,
  })
}

export function usePendingApprovalsCount() {
  return useQuery<number>({
    queryKey: managerKeys.pendingCount(),
    queryFn: managerApi.getPendingApprovalsCount,
  })
}

export function useApprovalDetail(requestId: string) {
  return useQuery<ApprovalRequestDetail>({
    queryKey: managerKeys.approvalDetail(requestId),
    queryFn: () => managerApi.getApprovalDetails(requestId),
    enabled: !!requestId,
  })
}

export function useApprove() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ requestId, dto }: { requestId: string; dto: ApproveRequestDto }) =>
      managerApi.approve(requestId, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: managerKeys.pendingApprovals() })
      queryClient.invalidateQueries({ queryKey: managerKeys.pendingAssetApprovals() })
      queryClient.invalidateQueries({ queryKey: managerKeys.pendingCount() })
      queryClient.invalidateQueries({ queryKey: managerKeys.dashboard() })
      queryClient.invalidateQueries({ queryKey: managerKeys.teamLeaves() })
      queryClient.invalidateQueries({ queryKey: managerKeys.teamAssetRequests() })
    },
  })
}

export function useReject() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ requestId, dto }: { requestId: string; dto: RejectRequestDto }) =>
      managerApi.reject(requestId, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: managerKeys.pendingApprovals() })
      queryClient.invalidateQueries({ queryKey: managerKeys.pendingAssetApprovals() })
      queryClient.invalidateQueries({ queryKey: managerKeys.pendingCount() })
      queryClient.invalidateQueries({ queryKey: managerKeys.dashboard() })
      queryClient.invalidateQueries({ queryKey: managerKeys.teamLeaves() })
      queryClient.invalidateQueries({ queryKey: managerKeys.teamAssetRequests() })
    },
  })
}

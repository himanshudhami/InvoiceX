import apiClient from './client'
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
  AssetRequestDetail,
} from '@/types'

export const managerApi = {
  // Dashboard
  getDashboard: async (): Promise<ManagerDashboard> => {
    const response = await apiClient.get<ManagerDashboard>('/portal/manager/dashboard')
    return response.data
  },

  // Team
  getMyTeam: async (): Promise<DirectReports> => {
    const response = await apiClient.get<DirectReports>('/portal/manager/team')
    return response.data
  },

  getAllSubordinates: async (): Promise<EmployeeHierarchy[]> => {
    const response = await apiClient.get<EmployeeHierarchy[]>('/portal/manager/team/all')
    return response.data
  },

  // Team Leaves
  getTeamLeaves: async (status?: string): Promise<TeamLeaveApplication[]> => {
    const params = status ? { status } : {}
    const response = await apiClient.get<TeamLeaveApplication[]>('/portal/manager/team/leaves', {
      params,
    })
    return response.data
  },

  // Approvals
  getPendingApprovals: async (): Promise<PendingApproval[]> => {
    const response = await apiClient.get<PendingApproval[]>('/portal/manager/approvals/pending')
    return response.data
  },

  getPendingApprovalsCount: async (): Promise<number> => {
    const response = await apiClient.get<number>('/portal/manager/approvals/pending/count')
    return response.data
  },

  getApprovalDetails: async (requestId: string): Promise<ApprovalRequestDetail> => {
    const response = await apiClient.get<ApprovalRequestDetail>(
      `/portal/manager/approvals/${requestId}`
    )
    return response.data
  },

  approve: async (requestId: string, dto: ApproveRequestDto): Promise<ApprovalRequestDetail> => {
    const response = await apiClient.post<ApprovalRequestDetail>(
      `/portal/manager/approvals/${requestId}/approve`,
      dto
    )
    return response.data
  },

  reject: async (requestId: string, dto: RejectRequestDto): Promise<ApprovalRequestDetail> => {
    const response = await apiClient.post<ApprovalRequestDetail>(
      `/portal/manager/approvals/${requestId}/reject`,
      dto
    )
    return response.data
  },

  // Asset Request Approvals (filtered by activity type)
  getPendingAssetApprovals: async (): Promise<PendingApproval[]> => {
    const allPending = await managerApi.getPendingApprovals()
    return allPending.filter((approval) => approval.activityType === 'asset_request')
  },

  // Get team's asset requests (for display in manager view)
  getTeamAssetRequests: async (status?: string): Promise<AssetRequestSummary[]> => {
    const params = status ? { status } : {}
    const response = await apiClient.get<AssetRequestSummary[]>('/portal/manager/team/asset-requests', {
      params,
    })
    return response.data
  },

  // Get asset request details by activity ID (for approval context)
  getAssetRequestDetails: async (activityId: string): Promise<AssetRequestDetail> => {
    const response = await apiClient.get<AssetRequestDetail>(
      `/portal/manager/activities/asset-request/${activityId}`
    )
    return response.data
  },
}

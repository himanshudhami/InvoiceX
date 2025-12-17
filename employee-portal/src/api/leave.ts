import apiClient from './client'
import type {
  LeaveDashboard,
  LeaveType,
  LeaveBalance,
  LeaveApplicationSummary,
  LeaveApplicationDetail,
  ApplyLeaveRequest,
  Holiday,
  LeaveCalendarEvent,
  LeaveCalculation,
} from '@/types'

export const leaveApi = {
  // Dashboard
  getDashboard: async (): Promise<LeaveDashboard> => {
    const response = await apiClient.get<LeaveDashboard>('/portal/leave/dashboard')
    return response.data
  },

  // Leave Types
  getLeaveTypes: async (): Promise<LeaveType[]> => {
    const response = await apiClient.get<LeaveType[]>('/portal/leave/types')
    return response.data
  },

  // Balances
  getBalances: async (financialYear?: string): Promise<LeaveBalance[]> => {
    const params = financialYear ? { financialYear } : {}
    const response = await apiClient.get<LeaveBalance[]>('/portal/leave/balances', { params })
    return response.data
  },

  // Applications
  getApplications: async (status?: string): Promise<LeaveApplicationSummary[]> => {
    const params = status ? { status } : {}
    const response = await apiClient.get<LeaveApplicationSummary[]>('/portal/leave/applications', {
      params,
    })
    return response.data
  },

  getApplicationDetail: async (id: string): Promise<LeaveApplicationDetail> => {
    const response = await apiClient.get<LeaveApplicationDetail>(`/portal/leave/applications/${id}`)
    return response.data
  },

  applyLeave: async (request: ApplyLeaveRequest): Promise<LeaveApplicationDetail> => {
    const response = await apiClient.post<LeaveApplicationDetail>('/portal/leave/apply', request)
    return response.data
  },

  withdrawApplication: async (id: string, reason?: string): Promise<void> => {
    await apiClient.post(`/portal/leave/applications/${id}/withdraw`, { reason })
  },

  // Holidays
  getHolidays: async (year?: number): Promise<Holiday[]> => {
    const params = year ? { year } : {}
    const response = await apiClient.get<Holiday[]>('/portal/leave/holidays', { params })
    return response.data
  },

  // Calendar
  getCalendarEvents: async (fromDate: string, toDate: string): Promise<LeaveCalendarEvent[]> => {
    const response = await apiClient.get<LeaveCalendarEvent[]>('/portal/leave/calendar', {
      params: { fromDate, toDate },
    })
    return response.data
  },

  // Calculate leave days
  calculateLeaveDays: async (fromDate: string, toDate: string): Promise<LeaveCalculation> => {
    const response = await apiClient.get<LeaveCalculation>('/portal/leave/calculate', {
      params: { fromDate, toDate },
    })
    return response.data
  },
}

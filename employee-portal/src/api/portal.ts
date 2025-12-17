import apiClient from './client'
import type {
  PortalDashboard,
  EmployeeProfile,
  PayslipSummary,
  PayslipDetail,
  MyAsset,
  TaxDeclarationSummary,
  TaxDeclarationDetail,
} from '@/types'

export const portalApi = {
  // Dashboard
  getDashboard: async (): Promise<PortalDashboard> => {
    const response = await apiClient.get<PortalDashboard>('/portal/dashboard')
    return response.data
  },

  // Profile
  getMyProfile: async (): Promise<EmployeeProfile> => {
    const response = await apiClient.get<EmployeeProfile>('/portal/me')
    return response.data
  },

  // Payslips
  getMyPayslips: async (year?: number): Promise<PayslipSummary[]> => {
    const params = year ? { year } : {}
    const response = await apiClient.get<PayslipSummary[]>('/portal/payslips', { params })
    return response.data
  },

  getPayslipDetail: async (id: string): Promise<PayslipDetail> => {
    const response = await apiClient.get<PayslipDetail>(`/portal/payslips/${id}`)
    return response.data
  },

  downloadPayslipPdf: async (id: string): Promise<Blob> => {
    const response = await apiClient.get(`/portal/payslips/${id}/pdf`, {
      responseType: 'blob',
    })
    return response.data
  },

  // Assets
  getMyAssets: async (): Promise<MyAsset[]> => {
    const response = await apiClient.get<MyAsset[]>('/portal/assets')
    return response.data
  },

  // Tax Declarations
  getMyTaxDeclarations: async (): Promise<TaxDeclarationSummary[]> => {
    const response = await apiClient.get<TaxDeclarationSummary[]>('/portal/tax-declarations')
    return response.data
  },

  getTaxDeclarationDetail: async (id: string): Promise<TaxDeclarationDetail> => {
    const response = await apiClient.get<TaxDeclarationDetail>(`/portal/tax-declarations/${id}`)
    return response.data
  },
}

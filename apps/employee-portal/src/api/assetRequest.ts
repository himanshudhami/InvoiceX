import apiClient from './client'
import type {
  AssetRequestSummary,
  AssetRequestDetail,
  CreateAssetRequest,
  UpdateAssetRequest,
  AssetPriority,
} from '@/types'

export const assetRequestApi = {
  // Get employee's asset requests
  getMyRequests: async (status?: string): Promise<AssetRequestSummary[]> => {
    const params = status ? { status } : {}
    const response = await apiClient.get<AssetRequestSummary[]>('/portal/asset-requests', { params })
    return response.data
  },

  // Get asset request detail by ID
  getRequestById: async (id: string): Promise<AssetRequestDetail> => {
    const response = await apiClient.get<AssetRequestDetail>(`/portal/asset-requests/${id}`)
    return response.data
  },

  // Submit a new asset request
  submitRequest: async (request: CreateAssetRequest): Promise<AssetRequestDetail> => {
    const response = await apiClient.post<AssetRequestDetail>('/portal/asset-requests', request)
    return response.data
  },

  // Update pending asset request
  updateRequest: async (id: string, request: UpdateAssetRequest): Promise<AssetRequestDetail> => {
    const response = await apiClient.put<AssetRequestDetail>(`/portal/asset-requests/${id}`, request)
    return response.data
  },

  // Withdraw pending asset request
  withdrawRequest: async (id: string, reason?: string): Promise<void> => {
    await apiClient.post(`/portal/asset-requests/${id}/withdraw`, { reason })
  },

  // Get available asset categories
  getCategories: async (): Promise<string[]> => {
    const response = await apiClient.get<string[]>('/portal/asset-requests/categories')
    return response.data
  },

  // Get available priorities
  getPriorities: async (): Promise<AssetPriority[]> => {
    const response = await apiClient.get<AssetPriority[]>('/portal/asset-requests/priorities')
    return response.data
  },
}

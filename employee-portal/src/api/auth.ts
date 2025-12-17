import apiClient, { tokenStorage } from './client'
import type { LoginRequest, LoginResponse, UserInfo } from '@/types'

export const authApi = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/auth/login', credentials)
    const data = response.data
    tokenStorage.setTokens(data.accessToken, data.refreshToken)
    return data
  },

  logout: async (): Promise<void> => {
    const refreshToken = tokenStorage.getRefreshToken()
    if (refreshToken) {
      try {
        await apiClient.post('/auth/logout', { refreshToken })
      } catch {
        // Ignore errors on logout
      }
    }
    tokenStorage.clearTokens()
  },

  getCurrentUser: async (): Promise<UserInfo> => {
    const response = await apiClient.get<UserInfo>('/auth/me')
    return response.data
  },

  changePassword: async (currentPassword: string, newPassword: string): Promise<void> => {
    await apiClient.post('/auth/change-password', {
      currentPassword,
      newPassword,
    })
  },
}

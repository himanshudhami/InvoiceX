import api, { tokenStorage } from './api'
import type { LoginRequest, LoginResponse, UserInfo, ChangePasswordRequest } from '../types/auth'

export const authApi = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await api.post<LoginResponse>('/auth/login', credentials)
    const data = response.data
    tokenStorage.setTokens(data.accessToken, data.refreshToken)
    return data
  },

  logout: async (): Promise<void> => {
    const refreshToken = tokenStorage.getRefreshToken()
    if (refreshToken) {
      try {
        await api.post('/auth/logout', { refreshToken })
      } catch {
        // Ignore errors on logout - we still want to clear tokens
      }
    }
    tokenStorage.clearTokens()
  },

  getCurrentUser: async (): Promise<UserInfo> => {
    const response = await api.get<UserInfo>('/auth/me')
    return response.data
  },

  changePassword: async (data: ChangePasswordRequest): Promise<void> => {
    await api.post('/auth/change-password', data)
  },
}

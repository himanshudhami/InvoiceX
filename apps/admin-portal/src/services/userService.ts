// User management API service
import api from './api'
import type {
  User,
  CreateUserRequest,
  UpdateUserRequest,
  ResetPasswordRequest,
  UsersListResponse,
  GetUsersParams,
} from '../types/user'

// Backend response format (uses 'data' instead of 'items')
interface BackendUsersResponse {
  data: User[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export const userApi = {
  // Get paginated list of users
  getUsers: (params?: GetUsersParams): Promise<UsersListResponse> =>
    api
      .get<BackendUsersResponse>('/auth/users', {
        params: {
          pageNumber: params?.pageNumber || 1,
          pageSize: params?.pageSize || 10,
          searchTerm: params?.searchTerm || undefined,
          role: params?.role || undefined,
          companyId: params?.companyId || undefined,
        },
      })
      .then((res) => ({
        // Map 'data' to 'items' for frontend consistency
        items: res.data.data,
        totalCount: res.data.totalCount,
        pageNumber: res.data.pageNumber,
        pageSize: res.data.pageSize,
        totalPages: res.data.totalPages,
      })),

  // Get single user by ID
  getUser: (id: string): Promise<User> =>
    api.get(`/auth/users/${id}`).then((res) => res.data),

  // Create new user
  createUser: (data: CreateUserRequest): Promise<User> =>
    api.post('/auth/register', data).then((res) => res.data),

  // Update user
  updateUser: (id: string, data: UpdateUserRequest): Promise<User> =>
    api.put(`/auth/users/${id}`, data).then((res) => res.data),

  // Reset user password (admin only)
  resetPassword: (data: ResetPasswordRequest): Promise<void> =>
    api.post('/auth/users/reset-password', data).then((res) => res.data),

  // Activate user account
  activateUser: (id: string): Promise<void> =>
    api.post(`/auth/users/${id}/activate`).then((res) => res.data),

  // Deactivate user account
  deactivateUser: (id: string): Promise<void> =>
    api.post(`/auth/users/${id}/deactivate`).then((res) => res.data),
}

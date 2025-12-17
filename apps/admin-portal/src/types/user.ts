// User management types for admin portal

import type { UserRole, UserInfo } from './auth'

// Re-export for convenience
export type { UserRole, UserInfo }

// Extended user info with additional fields for user management
export interface User extends UserInfo {
  createdAt: string
  updatedAt?: string
}

// Request to create a new user
export interface CreateUserRequest {
  email: string
  password: string
  displayName: string
  role: UserRole
  employeeId?: string
}

// Request to update an existing user
export interface UpdateUserRequest {
  displayName?: string
  role?: UserRole
  employeeId?: string | null
  isActive?: boolean
}

// Request to reset a user's password
export interface ResetPasswordRequest {
  userId: string
  newPassword: string
}

// Paginated response for user list
export interface UsersListResponse {
  items: User[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// Query parameters for fetching users
export interface GetUsersParams {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  role?: UserRole | ''
  companyId?: string
}

// All available roles
export const ALL_ROLES: UserRole[] = ['Admin', 'HR', 'Accountant', 'Manager', 'Employee']

// Role display labels
export const ROLE_LABELS: Record<UserRole, string> = {
  Admin: 'Administrator',
  HR: 'HR Manager',
  Accountant: 'Accountant',
  Manager: 'Manager',
  Employee: 'Employee',
}

// Authentication types for admin portal

export type UserRole = 'Admin' | 'HR' | 'Accountant' | 'Manager' | 'Employee'

export interface UserInfo {
  id: string
  email: string
  displayName: string
  role: UserRole
  companyId: string
  employeeId?: string
  isActive: boolean
  lastLoginAt?: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  user: UserInfo
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface RefreshTokenResponse {
  accessToken: string
  refreshToken: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

// Admin portal only allows Admin and HR roles
export const ALLOWED_ADMIN_PORTAL_ROLES: UserRole[] = ['Admin', 'HR']

export function isAllowedAdminPortalRole(role: UserRole): boolean {
  return ALLOWED_ADMIN_PORTAL_ROLES.includes(role)
}

// Authentication types for admin portal

export type UserRole = 'Admin' | 'HR' | 'Accountant' | 'Manager' | 'Employee'

export type AccessScope = 'all_companies' | 'assigned_companies' | undefined

export interface UserInfo {
  id: string
  email: string
  displayName: string
  role: UserRole
  companyId: string
  companyIds?: string[]         // Array of accessible company IDs (from JWT)
  accessScope?: AccessScope     // all_companies | assigned_companies
  isSuperAdmin?: boolean        // Super Admin flag
  employeeId?: string
  isActive: boolean
  lastLoginAt?: string
}

/**
 * Check if user has multi-company access (Super Admin or Company Admin with assigned companies)
 */
export function hasMultiCompanyAccess(user: UserInfo | null): boolean {
  if (!user) return false
  return user.isSuperAdmin ||
         user.accessScope === 'all_companies' ||
         user.accessScope === 'assigned_companies' ||
         (user.companyIds && user.companyIds.length > 1)
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

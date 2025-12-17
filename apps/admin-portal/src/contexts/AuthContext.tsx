import React, { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '../services/authService'
import { tokenStorage } from '../services/api'
import type { UserInfo, LoginRequest } from '../types/auth'
import { isAllowedAdminPortalRole } from '../types/auth'

interface AuthContextType {
  user: UserInfo | null
  isLoading: boolean
  isAuthenticated: boolean
  login: (credentials: LoginRequest) => Promise<void>
  logout: () => Promise<void>
  refreshUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const navigate = useNavigate()

  const refreshUser = useCallback(async () => {
    try {
      const userData = await authApi.getCurrentUser()
      // Verify user has admin portal access
      if (!isAllowedAdminPortalRole(userData.role)) {
        tokenStorage.clearTokens()
        setUser(null)
        return
      }
      setUser(userData)
    } catch {
      setUser(null)
      tokenStorage.clearTokens()
    }
  }, [])

  // Check for existing auth on mount
  useEffect(() => {
    const initAuth = async () => {
      const token = tokenStorage.getAccessToken()
      if (token) {
        await refreshUser()
      }
      setIsLoading(false)
    }
    initAuth()
  }, [refreshUser])

  const login = async (credentials: LoginRequest) => {
    const response = await authApi.login(credentials)

    // Verify user has admin portal access (Admin or HR only)
    if (!isAllowedAdminPortalRole(response.user.role)) {
      // Clear tokens since user doesn't have access
      tokenStorage.clearTokens()
      throw new Error('Access denied. Admin portal is restricted to Admin and HR users only.')
    }

    setUser(response.user)
    navigate('/dashboard')
  }

  const logout = async () => {
    await authApi.logout()
    setUser(null)
    navigate('/login')
  }

  const value: AuthContextType = {
    user,
    isLoading,
    isAuthenticated: !!user,
    login,
    logout,
    refreshUser,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

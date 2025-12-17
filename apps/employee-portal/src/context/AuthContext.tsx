import React, { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '@/api/auth'
import { tokenStorage } from '@/api/client'
import type { UserInfo, LoginRequest } from '@/types'

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
    setUser(response.user)
    navigate('/')
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

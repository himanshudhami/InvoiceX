import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'
import { MainLayout } from '@/components/layout'
import { FullScreenLoader } from '@/components/ui'
import {
  LoginPage,
  DashboardPage,
  ProfilePage,
  PayslipsPage,
  AssetsPage,
  LeaveIndexPage,
  ApplyLeavePage,
  LeaveApplicationDetailPage,
} from '@/pages'

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}

export default function App() {
  return (
    <Routes>
      {/* Public Routes */}
      <Route
        path="/login"
        element={
          <PublicRoute>
            <LoginPage />
          </PublicRoute>
        }
      />

      {/* Protected Routes */}
      <Route
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="payslips" element={<PayslipsPage />} />
        <Route path="assets" element={<AssetsPage />} />
        <Route path="leave" element={<LeaveIndexPage />} />
        <Route path="leave/apply" element={<ApplyLeavePage />} />
        <Route path="leave/applications/:id" element={<LeaveApplicationDetailPage />} />
      </Route>

      {/* Catch-all redirect */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

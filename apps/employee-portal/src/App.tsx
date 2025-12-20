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
  PayslipDetailPage,
  AssetsPage,
  ApplyAssetRequestPage,
  AssetRequestDetailPage,
  LeaveIndexPage,
  ApplyLeavePage,
  LeaveApplicationDetailPage,
  TaxDeclarationsPage,
  TaxDeclarationDetailPage,
  AnnouncementsPage,
  HelpDeskPage,
  DocumentsPage,
  MyTeamPage,
  PendingApprovalsPage,
  AssetApprovalsPage,
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

/**
 * ManagerProtectedRoute - Protects routes that require manager role
 * Redirects non-managers to the home page
 */
function ManagerProtectedRoute({ children }: { children: React.ReactNode }) {
  const { user, isLoading } = useAuth()

  if (isLoading) {
    return <FullScreenLoader />
  }

  // If user is not a manager, redirect to home page
  if (!user?.isManager) {
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
        <Route path="payslips/:id" element={<PayslipDetailPage />} />
        <Route path="assets" element={<AssetsPage />} />
        <Route path="assets/apply" element={<ApplyAssetRequestPage />} />
        <Route path="assets/requests/:id" element={<AssetRequestDetailPage />} />
        <Route path="leave" element={<LeaveIndexPage />} />
        <Route path="leave/apply" element={<ApplyLeavePage />} />
        <Route path="leave/applications/:id" element={<LeaveApplicationDetailPage />} />
        <Route path="tax-declarations" element={<TaxDeclarationsPage />} />
        <Route path="tax-declarations/:id" element={<TaxDeclarationDetailPage />} />
        <Route path="announcements" element={<AnnouncementsPage />} />
        <Route path="help" element={<HelpDeskPage />} />
        <Route path="documents" element={<DocumentsPage />} />

        {/* Manager Routes - Protected by ManagerProtectedRoute */}
        <Route
          path="manager/team"
          element={
            <ManagerProtectedRoute>
              <MyTeamPage />
            </ManagerProtectedRoute>
          }
        />
        <Route
          path="manager/approvals"
          element={
            <ManagerProtectedRoute>
              <PendingApprovalsPage />
            </ManagerProtectedRoute>
          }
        />
        <Route
          path="manager/asset-approvals"
          element={
            <ManagerProtectedRoute>
              <AssetApprovalsPage />
            </ManagerProtectedRoute>
          }
        />
      </Route>

      {/* Catch-all redirect */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

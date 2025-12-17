import React, { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Mail,
  Phone,
  Building2,
  Briefcase,
  Calendar,
  CreditCard,
  Building,
  LogOut,
  ChevronRight,
  Lock,
} from 'lucide-react'
import * as Dialog from '@radix-ui/react-dialog'
import { portalApi, authApi } from '@/api'
import { useAuth } from '@/context/AuthContext'
import { PageHeader } from '@/components/layout'
import { Card, Avatar, Button, Input, PageLoader } from '@/components/ui'
import { formatDate } from '@/utils/format'
import type { EmployeeProfile } from '@/types'

export function ProfilePage() {
  const { logout } = useAuth()
  const [showChangePassword, setShowChangePassword] = useState(false)
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false)

  const { data: profile, isLoading } = useQuery<EmployeeProfile>({
    queryKey: ['my-profile'],
    queryFn: portalApi.getMyProfile,
  })

  if (isLoading || !profile) {
    return <PageLoader />
  }

  const fullName = `${profile.firstName} ${profile.lastName}`

  return (
    <div className="animate-fade-in">
      <PageHeader title="Profile" />

      <div className="px-4 py-4 space-y-4">
        {/* Profile Header */}
        <Card className="p-6 text-center">
          <Avatar name={fullName} size="xl" className="mx-auto mb-3" />
          <h2 className="text-lg font-semibold text-gray-900">{fullName}</h2>
          <p className="text-sm text-gray-500">{profile.employeeCode}</p>
          <div className="flex items-center justify-center gap-2 mt-2 text-sm text-primary-600">
            <Building2 size={14} />
            <span>{profile.companyName}</span>
          </div>
        </Card>

        {/* Personal Details */}
        <Card className="divide-y divide-gray-100">
          <ProfileRow icon={<Mail size={18} />} label="Email" value={profile.email} />
          {profile.phone && (
            <ProfileRow icon={<Phone size={18} />} label="Phone" value={profile.phone} />
          )}
          {profile.department && (
            <ProfileRow icon={<Building size={18} />} label="Department" value={profile.department} />
          )}
          {profile.designation && (
            <ProfileRow icon={<Briefcase size={18} />} label="Designation" value={profile.designation} />
          )}
          <ProfileRow
            icon={<Calendar size={18} />}
            label="Date of Joining"
            value={formatDate(profile.dateOfJoining, 'dd MMMM yyyy')}
          />
        </Card>

        {/* Financial Details (Masked) */}
        <Card className="divide-y divide-gray-100">
          <div className="px-4 py-3 bg-gray-50">
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">
              Financial Information
            </p>
          </div>
          {profile.panMasked && (
            <ProfileRow icon={<CreditCard size={18} />} label="PAN" value={profile.panMasked} />
          )}
          {profile.bankAccountMasked && (
            <ProfileRow
              icon={<Building size={18} />}
              label="Bank Account"
              value={profile.bankAccountMasked}
            />
          )}
        </Card>

        {/* Actions */}
        <Card className="divide-y divide-gray-100">
          <button
            onClick={() => setShowChangePassword(true)}
            className="w-full flex items-center justify-between px-4 py-4 touch-feedback"
          >
            <div className="flex items-center gap-3">
              <div className="flex items-center justify-center w-10 h-10 rounded-full bg-gray-100">
                <Lock size={18} className="text-gray-600" />
              </div>
              <span className="text-sm font-medium text-gray-900">Change Password</span>
            </div>
            <ChevronRight size={18} className="text-gray-400" />
          </button>

          <button
            onClick={() => setShowLogoutConfirm(true)}
            className="w-full flex items-center justify-between px-4 py-4 touch-feedback"
          >
            <div className="flex items-center gap-3">
              <div className="flex items-center justify-center w-10 h-10 rounded-full bg-red-50">
                <LogOut size={18} className="text-red-600" />
              </div>
              <span className="text-sm font-medium text-red-600">Sign Out</span>
            </div>
            <ChevronRight size={18} className="text-gray-400" />
          </button>
        </Card>

        {/* App Info */}
        <p className="text-center text-xs text-gray-400 py-4">
          Employee Portal v0.1.0
        </p>
      </div>

      {/* Change Password Dialog */}
      <ChangePasswordDialog
        open={showChangePassword}
        onClose={() => setShowChangePassword(false)}
      />

      {/* Logout Confirmation Dialog */}
      <Dialog.Root open={showLogoutConfirm} onOpenChange={setShowLogoutConfirm}>
        <Dialog.Portal>
          <Dialog.Overlay className="fixed inset-0 bg-black/50 z-50" />
          <Dialog.Content className="fixed bottom-0 left-0 right-0 bg-white rounded-t-2xl p-6 z-50 animate-slide-up safe-bottom">
            <Dialog.Title className="text-lg font-semibold text-gray-900 mb-2">
              Sign Out?
            </Dialog.Title>
            <Dialog.Description className="text-sm text-gray-500 mb-4">
              Are you sure you want to sign out of your account?
            </Dialog.Description>

            <div className="flex gap-3">
              <Button
                variant="outline"
                className="flex-1"
                onClick={() => setShowLogoutConfirm(false)}
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                className="flex-1"
                onClick={() => {
                  setShowLogoutConfirm(false)
                  logout()
                }}
              >
                Sign Out
              </Button>
            </div>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>
    </div>
  )
}

interface ProfileRowProps {
  icon: React.ReactNode
  label: string
  value: string
}

function ProfileRow({ icon, label, value }: ProfileRowProps) {
  return (
    <div className="flex items-center gap-3 px-4 py-3">
      <div className="flex items-center justify-center w-10 h-10 rounded-full bg-gray-50 text-gray-500">
        {icon}
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs text-gray-500">{label}</p>
        <p className="text-sm font-medium text-gray-900 truncate">{value}</p>
      </div>
    </div>
  )
}

interface ChangePasswordDialogProps {
  open: boolean
  onClose: () => void
}

function ChangePasswordDialog({ open, onClose }: ChangePasswordDialogProps) {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [success, setSuccess] = useState(false)

  const handleSubmit = async () => {
    setError(null)

    if (newPassword.length < 8) {
      setError('New password must be at least 8 characters')
      return
    }

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match')
      return
    }

    setIsLoading(true)
    try {
      await authApi.changePassword(currentPassword, newPassword)
      setSuccess(true)
      setTimeout(() => {
        onClose()
        setSuccess(false)
        setCurrentPassword('')
        setNewPassword('')
        setConfirmPassword('')
      }, 1500)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to change password')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <Dialog.Root open={open} onOpenChange={onClose}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 z-50" />
        <Dialog.Content className="fixed bottom-0 left-0 right-0 bg-white rounded-t-2xl p-6 z-50 animate-slide-up safe-bottom max-h-[80vh] overflow-y-auto">
          <Dialog.Title className="text-lg font-semibold text-gray-900 mb-4">
            Change Password
          </Dialog.Title>

          {success ? (
            <div className="text-center py-6">
              <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-green-100 mb-4">
                <svg className="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
              </div>
              <p className="text-base font-medium text-gray-900">Password Changed!</p>
            </div>
          ) : (
            <div className="space-y-4">
              {error && (
                <div className="p-3 rounded-lg bg-red-50 border border-red-200">
                  <p className="text-sm text-red-600">{error}</p>
                </div>
              )}

              <Input
                type="password"
                label="Current Password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                autoComplete="current-password"
              />

              <Input
                type="password"
                label="New Password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                autoComplete="new-password"
                helperText="Must be at least 8 characters"
              />

              <Input
                type="password"
                label="Confirm New Password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
              />

              <div className="flex gap-3 pt-2">
                <Button variant="outline" className="flex-1" onClick={onClose}>
                  Cancel
                </Button>
                <Button
                  className="flex-1"
                  onClick={handleSubmit}
                  isLoading={isLoading}
                  disabled={!currentPassword || !newPassword || !confirmPassword}
                >
                  Update Password
                </Button>
              </div>
            </div>
          )}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}

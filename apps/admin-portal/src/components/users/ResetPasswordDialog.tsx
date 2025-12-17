import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Modal } from '@/components/ui/Modal'
import { userApi } from '@/services/userService'
import { User } from '@/types/user'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface ResetPasswordDialogProps {
  user: User | null
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

interface ResetPasswordForm {
  newPassword: string
  confirmPassword: string
}

export const ResetPasswordDialog = ({
  user,
  isOpen,
  onClose,
  onSuccess,
}: ResetPasswordDialogProps) => {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<ResetPasswordForm>({
    defaultValues: {
      newPassword: '',
      confirmPassword: '',
    },
  })

  const newPassword = watch('newPassword')

  const onSubmit = async (data: ResetPasswordForm) => {
    if (!user) return

    setIsLoading(true)
    setError(null)
    try {
      await userApi.resetPassword({
        userId: user.id,
        newPassword: data.newPassword,
      })
      setSuccess(true)
      reset()
      setTimeout(() => {
        setSuccess(false)
        onSuccess()
      }, 2000)
    } catch (err: any) {
      const message =
        err?.response?.data?.message || err?.response?.data || 'Failed to reset password'
      setError(typeof message === 'string' ? message : 'Failed to reset password')
    } finally {
      setIsLoading(false)
    }
  }

  const handleClose = () => {
    reset()
    setError(null)
    setSuccess(false)
    onClose()
  }

  if (!user) return null

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Reset Password" size="md">
      {success ? (
        <div className="text-center py-4">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg
              className="w-8 h-8 text-green-600"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M5 13l4 4L19 7"
              />
            </svg>
          </div>
          <p className="text-lg font-medium text-gray-900">Password Reset Successfully</p>
          <p className="text-sm text-gray-500 mt-1">
            The password for {user.displayName} has been reset.
          </p>
        </div>
      ) : (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          {error && (
            <div className="p-3 rounded-lg bg-red-50 border border-red-200">
              <p className="text-sm text-red-600">{error}</p>
            </div>
          )}

          <div className="p-3 rounded-lg bg-blue-50 border border-blue-200">
            <p className="text-sm text-blue-700">
              Resetting password for <span className="font-semibold">{user.displayName}</span> (
              {user.email})
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="newPassword">New Password</Label>
            <Input
              id="newPassword"
              type="password"
              placeholder="Minimum 6 characters"
              {...register('newPassword', {
                required: 'New password is required',
                minLength: {
                  value: 6,
                  message: 'Password must be at least 6 characters',
                },
              })}
            />
            {errors.newPassword && (
              <p className="text-sm text-red-500">{errors.newPassword.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="confirmPassword">Confirm Password</Label>
            <Input
              id="confirmPassword"
              type="password"
              placeholder="Re-enter password"
              {...register('confirmPassword', {
                required: 'Please confirm the password',
                validate: (value) => value === newPassword || 'Passwords do not match',
              })}
            />
            {errors.confirmPassword && (
              <p className="text-sm text-red-500">{errors.confirmPassword.message}</p>
            )}
          </div>

          <div className="flex justify-end space-x-3 pt-4">
            <button
              type="button"
              onClick={handleClose}
              className="px-4 py-2 text-gray-600 hover:text-gray-800 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isLoading}
              className="px-4 py-2 bg-orange-600 text-white rounded-lg hover:bg-orange-700 transition-colors disabled:opacity-50"
            >
              {isLoading ? 'Resetting...' : 'Reset Password'}
            </button>
          </div>
        </form>
      )}
    </Modal>
  )
}

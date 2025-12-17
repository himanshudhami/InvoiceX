import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Modal } from '@/components/ui/Modal'
import { userApi } from '@/services/userService'
import { CreateUserRequest, ALL_ROLES, ROLE_LABELS } from '@/types/user'
import type { UserRole } from '@/types/auth'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface CreateUserDialogProps {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

export const CreateUserDialog = ({ isOpen, onClose, onSuccess }: CreateUserDialogProps) => {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateUserRequest>({
    defaultValues: {
      email: '',
      password: '',
      displayName: '',
      role: 'Employee',
    },
  })

  const onSubmit = async (data: CreateUserRequest) => {
    setIsLoading(true)
    setError(null)
    try {
      await userApi.createUser(data)
      reset()
      onSuccess()
    } catch (err: any) {
      const message = err?.response?.data?.message || err?.response?.data || 'Failed to create user'
      setError(typeof message === 'string' ? message : 'Failed to create user')
    } finally {
      setIsLoading(false)
    }
  }

  const handleClose = () => {
    reset()
    setError(null)
    onClose()
  }

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Create New User" size="md">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {error && (
          <div className="p-3 rounded-lg bg-red-50 border border-red-200">
            <p className="text-sm text-red-600">{error}</p>
          </div>
        )}

        <div className="space-y-2">
          <Label htmlFor="displayName">Display Name</Label>
          <Input
            id="displayName"
            placeholder="John Doe"
            {...register('displayName', { required: 'Display name is required' })}
          />
          {errors.displayName && (
            <p className="text-sm text-red-500">{errors.displayName.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            type="email"
            placeholder="john@company.com"
            {...register('email', {
              required: 'Email is required',
              pattern: {
                value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                message: 'Invalid email address',
              },
            })}
          />
          {errors.email && <p className="text-sm text-red-500">{errors.email.message}</p>}
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <Input
            id="password"
            type="password"
            placeholder="Minimum 6 characters"
            {...register('password', {
              required: 'Password is required',
              minLength: {
                value: 6,
                message: 'Password must be at least 6 characters',
              },
            })}
          />
          {errors.password && <p className="text-sm text-red-500">{errors.password.message}</p>}
        </div>

        <div className="space-y-2">
          <Label htmlFor="role">Role</Label>
          <select
            id="role"
            {...register('role', { required: 'Role is required' })}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {ALL_ROLES.map((role) => (
              <option key={role} value={role}>
                {ROLE_LABELS[role]}
              </option>
            ))}
          </select>
          {errors.role && <p className="text-sm text-red-500">{errors.role.message}</p>}
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
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
          >
            {isLoading ? 'Creating...' : 'Create User'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

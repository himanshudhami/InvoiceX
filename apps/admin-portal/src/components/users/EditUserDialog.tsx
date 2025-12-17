import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { Modal } from '@/components/ui/Modal'
import { userApi } from '@/services/userService'
import { User, UpdateUserRequest, ALL_ROLES, ROLE_LABELS } from '@/types/user'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface EditUserDialogProps {
  user: User | null
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
  currentUserId?: string
}

export const EditUserDialog = ({
  user,
  isOpen,
  onClose,
  onSuccess,
  currentUserId,
}: EditUserDialogProps) => {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const isEditingSelf = user?.id === currentUserId

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<UpdateUserRequest>()

  // Reset form when user changes
  useEffect(() => {
    if (user) {
      reset({
        displayName: user.displayName,
        role: user.role,
      })
    }
  }, [user, reset])

  const onSubmit = async (data: UpdateUserRequest) => {
    if (!user) return

    setIsLoading(true)
    setError(null)
    try {
      await userApi.updateUser(user.id, data)
      onSuccess()
    } catch (err: any) {
      const message = err?.response?.data?.message || err?.response?.data || 'Failed to update user'
      setError(typeof message === 'string' ? message : 'Failed to update user')
    } finally {
      setIsLoading(false)
    }
  }

  const handleClose = () => {
    setError(null)
    onClose()
  }

  if (!user) return null

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Edit User" size="md">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {error && (
          <div className="p-3 rounded-lg bg-red-50 border border-red-200">
            <p className="text-sm text-red-600">{error}</p>
          </div>
        )}

        {isEditingSelf && (
          <div className="p-3 rounded-lg bg-yellow-50 border border-yellow-200">
            <p className="text-sm text-yellow-700">
              You are editing your own account. Role changes will take effect on next login.
            </p>
          </div>
        )}

        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            type="email"
            value={user.email}
            disabled
            className="bg-gray-100 cursor-not-allowed"
          />
          <p className="text-xs text-gray-500">Email cannot be changed</p>
        </div>

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
          {isEditingSelf && (
            <p className="text-xs text-orange-600">
              Warning: Changing your own role may affect your access
            </p>
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
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
          >
            {isLoading ? 'Saving...' : 'Save Changes'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

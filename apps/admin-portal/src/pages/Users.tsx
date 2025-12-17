import { useState, useEffect } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { userApi } from '@/services/userService'
import { User, UsersListResponse, ALL_ROLES, ROLE_LABELS } from '@/types/user'
import type { UserRole } from '@/types/auth'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { CreateUserDialog } from '@/components/users/CreateUserDialog'
import { EditUserDialog } from '@/components/users/EditUserDialog'
import { ResetPasswordDialog } from '@/components/users/ResetPasswordDialog'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { useAuth } from '@/contexts/AuthContext'
import { Edit, Key, UserCheck, UserX, Users as UsersIcon, Plus } from 'lucide-react'
import { format } from 'date-fns'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'

const UsersPage = () => {
  const { user: currentUser } = useAuth()
  const [data, setData] = useState<UsersListResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Filter state
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [searchTerm, setSearchTerm] = useState('')
  const [roleFilter, setRoleFilter] = useState<UserRole | ''>('')
  const [companyFilter, setCompanyFilter] = useState('')
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState('')

  // Dialog state
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<User | null>(null)
  const [resettingPasswordUser, setResettingPasswordUser] = useState<User | null>(null)
  const [togglingUser, setTogglingUser] = useState<User | null>(null)

  // Debounce search
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(searchTerm)
      setPage(1)
    }, 300)
    return () => clearTimeout(timer)
  }, [searchTerm])

  // Fetch users
  const fetchUsers = async () => {
    setIsLoading(true)
    setError(null)
    try {
      const result = await userApi.getUsers({
        pageNumber: page,
        pageSize,
        searchTerm: debouncedSearchTerm || undefined,
        role: roleFilter || undefined,
        companyId: companyFilter || undefined,
      })
      setData(result)
    } catch (err) {
      setError('Failed to load users')
      console.error('Error fetching users:', err)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    fetchUsers()
  }, [page, pageSize, debouncedSearchTerm, roleFilter, companyFilter])

  const handleToggleStatus = async () => {
    if (!togglingUser) return
    try {
      if (togglingUser.isActive) {
        await userApi.deactivateUser(togglingUser.id)
      } else {
        await userApi.activateUser(togglingUser.id)
      }
      setTogglingUser(null)
      fetchUsers()
    } catch (err) {
      console.error('Failed to toggle user status:', err)
    }
  }

  const getRoleBadge = (role: UserRole) => {
    const colors: Record<UserRole, string> = {
      Admin: 'bg-purple-100 text-purple-800',
      HR: 'bg-blue-100 text-blue-800',
      Accountant: 'bg-green-100 text-green-800',
      Manager: 'bg-yellow-100 text-yellow-800',
      Employee: 'bg-gray-100 text-gray-800',
    }
    return (
      <span className={`px-2 py-1 rounded-full text-xs font-medium ${colors[role]}`}>
        {ROLE_LABELS[role]}
      </span>
    )
  }

  const getStatusBadge = (isActive: boolean) => {
    return isActive ? (
      <span className="px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
        Active
      </span>
    ) : (
      <span className="px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800">
        Inactive
      </span>
    )
  }

  const columns: ColumnDef<User>[] = [
    {
      accessorKey: 'displayName',
      header: 'User',
      cell: ({ row }) => {
        const user = row.original
        return (
          <div className="flex items-start space-x-3">
            <div className="flex-shrink-0 w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
              <UsersIcon className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <div className="font-medium text-gray-900">{user.displayName}</div>
              <div className="text-sm text-gray-500">{user.email}</div>
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'role',
      header: 'Role',
      cell: ({ row }) => getRoleBadge(row.original.role),
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.isActive),
    },
    {
      accessorKey: 'lastLoginAt',
      header: 'Last Login',
      cell: ({ row }) => {
        const lastLogin = row.original.lastLoginAt
        if (!lastLogin) return <span className="text-gray-400">Never</span>
        try {
          return (
            <span className="text-sm text-gray-600">
              {format(new Date(lastLogin), 'MMM dd, yyyy HH:mm')}
            </span>
          )
        } catch {
          return <span className="text-gray-400">-</span>
        }
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const user = row.original
        const isCurrentUser = user.id === currentUser?.id
        return (
          <div className="flex items-center justify-end space-x-2">
            <button
              onClick={() => setEditingUser(user)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit user"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => setResettingPasswordUser(user)}
              className="text-orange-600 hover:text-orange-800 p-1 rounded hover:bg-orange-50 transition-colors"
              title="Reset password"
            >
              <Key size={16} />
            </button>
            {!isCurrentUser && (
              <button
                onClick={() => setTogglingUser(user)}
                className={`p-1 rounded transition-colors ${
                  user.isActive
                    ? 'text-red-600 hover:text-red-800 hover:bg-red-50'
                    : 'text-green-600 hover:text-green-800 hover:bg-green-50'
                }`}
                title={user.isActive ? 'Deactivate user' : 'Activate user'}
              >
                {user.isActive ? <UserX size={16} /> : <UserCheck size={16} />}
              </button>
            )}
          </div>
        )
      },
    },
  ]

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-red-600 mb-4">{error}</p>
          <button
            onClick={fetchUsers}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Try Again
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">User Management</h1>
          <p className="text-gray-600 mt-1">Manage user accounts, roles, and access</p>
        </div>
        <button
          onClick={() => setIsCreateDialogOpen(true)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center space-x-2"
        >
          <Plus className="w-4 h-4" />
          <span>Add User</span>
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow px-4 py-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Search</label>
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Search by name or email..."
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
            <CompanyFilterDropdown
              value={companyFilter}
              onChange={(val) => {
                setCompanyFilter(val)
                setPage(1)
              }}
              className="w-full text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Role</label>
            <select
              value={roleFilter}
              onChange={(e) => {
                setRoleFilter(e.target.value as UserRole | '')
                setPage(1)
              }}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Roles</option>
              {ALL_ROLES.map((role) => (
                <option key={role} value={role}>
                  {ROLE_LABELS[role]}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow overflow-x-auto">
        {isLoading ? (
          <div className="flex items-center justify-center h-64">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          </div>
        ) : (
          <DataTable
            columns={columns}
            data={data?.items || []}
            searchPlaceholder="Search users..."
            pageSizeOverride={pageSize}
            hidePaginationControls
          />
        )}
      </div>

      {/* Pagination */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 bg-white rounded-lg shadow px-4 py-3">
        <div className="text-sm text-gray-700">
          {data?.totalCount || 0} users - Page {data?.pageNumber || page} of{' '}
          {data?.totalPages || 1}
        </div>
        <div className="flex items-center space-x-3">
          <PageSizeSelect
            value={pageSize}
            onChange={(size) => {
              setPageSize(size)
              setPage(1)
            }}
          />
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page <= 1}
            className={`px-3 py-1 rounded-md text-sm ${
              page > 1
                ? 'bg-gray-200 hover:bg-gray-300 text-gray-700'
                : 'bg-gray-100 text-gray-400 cursor-not-allowed'
            }`}
          >
            Previous
          </button>
          <button
            onClick={() => setPage((p) => p + 1)}
            disabled={!data?.totalPages || page >= data.totalPages}
            className={`px-3 py-1 rounded-md text-sm ${
              data?.totalPages && page < data.totalPages
                ? 'bg-gray-200 hover:bg-gray-300 text-gray-700'
                : 'bg-gray-100 text-gray-400 cursor-not-allowed'
            }`}
          >
            Next
          </button>
        </div>
      </div>

      {/* Create User Dialog */}
      <CreateUserDialog
        isOpen={isCreateDialogOpen}
        onClose={() => setIsCreateDialogOpen(false)}
        onSuccess={() => {
          setIsCreateDialogOpen(false)
          fetchUsers()
        }}
      />

      {/* Edit User Dialog */}
      <EditUserDialog
        user={editingUser}
        isOpen={!!editingUser}
        onClose={() => setEditingUser(null)}
        onSuccess={() => {
          setEditingUser(null)
          fetchUsers()
        }}
        currentUserId={currentUser?.id}
      />

      {/* Reset Password Dialog */}
      <ResetPasswordDialog
        user={resettingPasswordUser}
        isOpen={!!resettingPasswordUser}
        onClose={() => setResettingPasswordUser(null)}
        onSuccess={() => {
          setResettingPasswordUser(null)
        }}
      />

      {/* Toggle Status Confirmation Modal */}
      <Modal
        isOpen={!!togglingUser}
        onClose={() => setTogglingUser(null)}
        title={togglingUser?.isActive ? 'Deactivate User' : 'Activate User'}
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Are you sure you want to {togglingUser?.isActive ? 'deactivate' : 'activate'}{' '}
            <span className="font-semibold">{togglingUser?.displayName}</span>?
            {togglingUser?.isActive && (
              <span className="block mt-2 text-sm text-orange-600">
                This user will no longer be able to log in.
              </span>
            )}
          </p>
          <div className="flex justify-end space-x-3">
            <button
              onClick={() => setTogglingUser(null)}
              className="px-4 py-2 text-gray-600 hover:text-gray-800 transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleToggleStatus}
              className={`px-4 py-2 text-white rounded-lg transition-colors ${
                togglingUser?.isActive
                  ? 'bg-red-600 hover:bg-red-700'
                  : 'bg-green-600 hover:bg-green-700'
              }`}
            >
              {togglingUser?.isActive ? 'Deactivate' : 'Activate'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

export default UsersPage

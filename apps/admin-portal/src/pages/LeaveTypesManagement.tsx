import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useLeaveTypes, useCreateLeaveType, useUpdateLeaveType, useDeleteLeaveType, useToggleLeaveTypeActive } from '@/hooks/api/useLeaveTypes'
import { useCompanies } from '@/hooks/api/useCompanies'
import { LeaveType, CreateLeaveTypeDto, UpdateLeaveTypeDto } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { Edit, Trash2, ToggleLeft, ToggleRight } from 'lucide-react'

const LeaveTypesManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingLeaveType, setEditingLeaveType] = useState<LeaveType | null>(null)
  const [deletingLeaveType, setDeletingLeaveType] = useState<LeaveType | null>(null)
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')

  const { data: leaveTypes = [], isLoading, error, refetch } = useLeaveTypes(selectedCompanyId || undefined)
  const { data: companies = [] } = useCompanies()
  const createLeaveType = useCreateLeaveType()
  const updateLeaveType = useUpdateLeaveType()
  const deleteLeaveType = useDeleteLeaveType()
  const toggleActive = useToggleLeaveTypeActive()

  const handleEdit = (leaveType: LeaveType) => {
    setEditingLeaveType(leaveType)
  }

  const handleDelete = (leaveType: LeaveType) => {
    setDeletingLeaveType(leaveType)
  }

  const handleDeleteConfirm = async () => {
    if (deletingLeaveType) {
      try {
        await deleteLeaveType.mutateAsync(deletingLeaveType.id)
        setDeletingLeaveType(null)
      } catch (error) {
        console.error('Failed to delete leave type:', error)
      }
    }
  }

  const handleToggleActive = async (leaveType: LeaveType) => {
    try {
      await toggleActive.mutateAsync(leaveType.id)
    } catch (error) {
      console.error('Failed to toggle leave type status:', error)
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingLeaveType(null)
    refetch()
  }

  const getCompanyName = (companyId: string) => {
    const company = companies.find(c => c.id === companyId)
    return company?.name || 'Unknown Company'
  }

  const columns: ColumnDef<LeaveType>[] = [
    {
      accessorKey: 'name',
      header: 'Leave Type',
      cell: ({ row }) => {
        const leaveType = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{leaveType.name}</div>
            <div className="text-sm text-gray-500">{leaveType.code}</div>
          </div>
        )
      },
    },
    {
      accessorKey: 'daysPerYear',
      header: 'Days/Year',
      cell: ({ row }) => (
        <div className="text-center font-medium">{row.original.daysPerYear}</div>
      ),
    },
    {
      accessorKey: 'carryForwardAllowed',
      header: 'Carry Forward',
      cell: ({ row }) => {
        const leaveType = row.original
        return (
          <div className="text-sm">
            {leaveType.carryForwardAllowed ? (
              <span className="text-green-600">Yes (max {leaveType.maxCarryForwardDays} days)</span>
            ) : (
              <span className="text-gray-500">No</span>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'encashmentAllowed',
      header: 'Encashment',
      cell: ({ row }) => {
        const leaveType = row.original
        return (
          <div className="text-sm">
            {leaveType.encashmentAllowed ? (
              <span className="text-green-600">Yes</span>
            ) : (
              <span className="text-gray-500">No</span>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'isPaidLeave',
      header: 'Type',
      cell: ({ row }) => (
        <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
          row.original.isPaidLeave
            ? 'bg-green-100 text-green-800'
            : 'bg-yellow-100 text-yellow-800'
        }`}>
          {row.original.isPaidLeave ? 'Paid' : 'Unpaid'}
        </span>
      ),
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => (
        <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
          row.original.isActive
            ? 'bg-green-100 text-green-800'
            : 'bg-gray-100 text-gray-800'
        }`}>
          {row.original.isActive ? 'Active' : 'Inactive'}
        </span>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const leaveType = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleToggleActive(leaveType)}
              className={`p-1 rounded transition-colors ${
                leaveType.isActive
                  ? 'text-green-600 hover:text-green-800 hover:bg-green-50'
                  : 'text-gray-400 hover:text-gray-600 hover:bg-gray-50'
              }`}
              title={leaveType.isActive ? 'Deactivate' : 'Activate'}
            >
              {leaveType.isActive ? <ToggleRight size={16} /> : <ToggleLeft size={16} />}
            </button>
            <button
              onClick={() => handleEdit(leaveType)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit leave type"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(leaveType)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete leave type"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load leave types</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  const activeLeaveTypes = leaveTypes.filter(lt => lt.isActive)
  const paidLeaveTypes = leaveTypes.filter(lt => lt.isPaidLeave)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Leave Types</h1>
        <p className="text-gray-600 mt-2">Configure leave types for your organization</p>
      </div>

      <div className="flex items-center gap-4">
        <label className="text-sm font-medium text-gray-700">Filter by Company:</label>
        <select
          value={selectedCompanyId}
          onChange={(e) => setSelectedCompanyId(e.target.value)}
          className="px-3 py-2 border border-gray-300 rounded-md text-sm"
        >
          <option value="">All Companies</option>
          {companies.map(company => (
            <option key={company.id} value={company.id}>{company.name}</option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Leave Types</div>
          <div className="text-2xl font-bold text-gray-900">{leaveTypes.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Active</div>
          <div className="text-2xl font-bold text-green-600">{activeLeaveTypes.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Paid Leave</div>
          <div className="text-2xl font-bold text-blue-600">{paidLeaveTypes.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Unpaid Leave</div>
          <div className="text-2xl font-bold text-yellow-600">{leaveTypes.length - paidLeaveTypes.length}</div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={leaveTypes}
            searchPlaceholder="Search leave types..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Leave Type"
          />
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Leave Type"
        size="lg"
      >
        <LeaveTypeForm
          companies={companies}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
          createMutation={createLeaveType}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingLeaveType}
        onClose={() => setEditingLeaveType(null)}
        title="Edit Leave Type"
        size="lg"
      >
        {editingLeaveType && (
          <LeaveTypeForm
            leaveType={editingLeaveType}
            companies={companies}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingLeaveType(null)}
            updateMutation={updateLeaveType}
          />
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingLeaveType}
        onClose={() => setDeletingLeaveType(null)}
        title="Delete Leave Type"
        size="sm"
      >
        {deletingLeaveType && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the leave type <strong>{deletingLeaveType.name}</strong>?
              This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingLeaveType(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteLeaveType.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteLeaveType.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

// Inline form component
interface LeaveTypeFormProps {
  leaveType?: LeaveType
  companies: { id: string; name: string }[]
  onSuccess: () => void
  onCancel: () => void
  createMutation?: ReturnType<typeof useCreateLeaveType>
  updateMutation?: ReturnType<typeof useUpdateLeaveType>
}

const LeaveTypeForm = ({ leaveType, companies, onSuccess, onCancel, createMutation, updateMutation }: LeaveTypeFormProps) => {
  const [formData, setFormData] = useState<CreateLeaveTypeDto>({
    companyId: leaveType?.companyId || companies[0]?.id || '',
    name: leaveType?.name || '',
    code: leaveType?.code || '',
    description: leaveType?.description || '',
    daysPerYear: leaveType?.daysPerYear || 12,
    carryForwardAllowed: leaveType?.carryForwardAllowed || false,
    maxCarryForwardDays: leaveType?.maxCarryForwardDays || 0,
    encashmentAllowed: leaveType?.encashmentAllowed || false,
    maxEncashmentDays: leaveType?.maxEncashmentDays || 0,
    isPaidLeave: leaveType?.isPaidLeave ?? true,
    requiresApproval: leaveType?.requiresApproval ?? true,
    minDaysNotice: leaveType?.minDaysNotice || 1,
    maxConsecutiveDays: leaveType?.maxConsecutiveDays || 30,
    isActive: leaveType?.isActive ?? true,
  })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      if (leaveType && updateMutation) {
        await updateMutation.mutateAsync({ id: leaveType.id, data: formData })
      } else if (createMutation) {
        await createMutation.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Failed to save leave type:', error)
    }
  }

  const isPending = createMutation?.isPending || updateMutation?.isPending

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Company *</label>
        <select
          value={formData.companyId}
          onChange={(e) => setFormData({ ...formData, companyId: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
          required
        >
          {companies.map(company => (
            <option key={company.id} value={company.id}>{company.name}</option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
          <input
            type="text"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            placeholder="e.g., Casual Leave"
            required
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Code *</label>
          <input
            type="text"
            value={formData.code}
            onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            placeholder="e.g., CL"
            required
          />
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
        <textarea
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
          rows={2}
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Days Per Year *</label>
          <input
            type="number"
            value={formData.daysPerYear}
            onChange={(e) => setFormData({ ...formData, daysPerYear: Number(e.target.value) })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            min="0"
            step="0.5"
            required
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Min Days Notice</label>
          <input
            type="number"
            value={formData.minDaysNotice}
            onChange={(e) => setFormData({ ...formData, minDaysNotice: Number(e.target.value) })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            min="0"
          />
        </div>
      </div>

      <div className="space-y-3">
        <label className="flex items-center">
          <input
            type="checkbox"
            checked={formData.isPaidLeave}
            onChange={(e) => setFormData({ ...formData, isPaidLeave: e.target.checked })}
            className="mr-2"
          />
          <span className="text-sm text-gray-700">Paid Leave</span>
        </label>

        <label className="flex items-center">
          <input
            type="checkbox"
            checked={formData.requiresApproval}
            onChange={(e) => setFormData({ ...formData, requiresApproval: e.target.checked })}
            className="mr-2"
          />
          <span className="text-sm text-gray-700">Requires Approval</span>
        </label>

        <label className="flex items-center">
          <input
            type="checkbox"
            checked={formData.carryForwardAllowed}
            onChange={(e) => setFormData({ ...formData, carryForwardAllowed: e.target.checked })}
            className="mr-2"
          />
          <span className="text-sm text-gray-700">Allow Carry Forward</span>
        </label>

        {formData.carryForwardAllowed && (
          <div className="ml-6">
            <label className="block text-sm font-medium text-gray-700 mb-1">Max Carry Forward Days</label>
            <input
              type="number"
              value={formData.maxCarryForwardDays}
              onChange={(e) => setFormData({ ...formData, maxCarryForwardDays: Number(e.target.value) })}
              className="w-32 px-3 py-2 border border-gray-300 rounded-md"
              min="0"
            />
          </div>
        )}

        <label className="flex items-center">
          <input
            type="checkbox"
            checked={formData.encashmentAllowed}
            onChange={(e) => setFormData({ ...formData, encashmentAllowed: e.target.checked })}
            className="mr-2"
          />
          <span className="text-sm text-gray-700">Allow Encashment</span>
        </label>

        <label className="flex items-center">
          <input
            type="checkbox"
            checked={formData.isActive}
            onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
            className="mr-2"
          />
          <span className="text-sm text-gray-700">Active</span>
        </label>
      </div>

      <div className="flex justify-end space-x-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isPending}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
        >
          {isPending ? 'Saving...' : leaveType ? 'Update' : 'Create'}
        </button>
      </div>
    </form>
  )
}

export default LeaveTypesManagement

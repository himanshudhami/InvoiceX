import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useHolidays, useCreateHoliday, useUpdateHoliday, useDeleteHoliday, useCopyHolidaysToNextYear } from '@/hooks/api/useHolidays'
import { useCompanies } from '@/hooks/api/useCompanies'
import { Holiday, CreateHolidayDto, UpdateHolidayDto } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { CompanySelect } from '@/components/ui/CompanySelect'
import { Edit, Trash2, Copy, Calendar } from 'lucide-react'
import { format } from 'date-fns'

const currentYear = new Date().getFullYear()

const HolidaysManagement = () => {
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  const [selectedYear, setSelectedYear] = useState<number>(currentYear)
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingHoliday, setEditingHoliday] = useState<Holiday | null>(null)
  const [deletingHoliday, setDeletingHoliday] = useState<Holiday | null>(null)

  const { data: holidays = [], isLoading, error, refetch } = useHolidays({
    companyId: selectedCompanyId || undefined,
    year: selectedYear,
  })
  const { data: companies = [] } = useCompanies()

  const createHoliday = useCreateHoliday()
  const updateHoliday = useUpdateHoliday()
  const deleteHoliday = useDeleteHoliday()
  const copyToNextYear = useCopyHolidaysToNextYear()

  const handleEdit = (holiday: Holiday) => {
    setEditingHoliday(holiday)
  }

  const handleDelete = (holiday: Holiday) => {
    setDeletingHoliday(holiday)
  }

  const handleDeleteConfirm = async () => {
    if (deletingHoliday) {
      try {
        await deleteHoliday.mutateAsync(deletingHoliday.id)
        setDeletingHoliday(null)
      } catch (error) {
        console.error('Failed to delete holiday:', error)
      }
    }
  }

  const handleCopyToNextYear = async () => {
    if (!selectedCompanyId) return
    if (confirm(`Copy all ${selectedYear} holidays to ${selectedYear + 1}?`)) {
      try {
        await copyToNextYear.mutateAsync({
          companyId: selectedCompanyId,
          sourceYear: selectedYear,
        })
      } catch (error) {
        console.error('Failed to copy holidays:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingHoliday(null)
    refetch()
  }

  const columns: ColumnDef<Holiday>[] = [
    {
      accessorKey: 'date',
      header: 'Date',
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <Calendar size={16} className="text-gray-400" />
          <div>
            <div className="font-medium">{format(new Date(row.original.date), 'dd MMM yyyy')}</div>
            <div className="text-xs text-gray-500">{format(new Date(row.original.date), 'EEEE')}</div>
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'name',
      header: 'Holiday',
      cell: ({ row }) => (
        <div>
          <div className="font-medium text-gray-900">{row.original.name}</div>
          {row.original.description && (
            <div className="text-sm text-gray-500">{row.original.description}</div>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'isOptional',
      header: 'Type',
      cell: ({ row }) => (
        <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
          row.original.isOptional
            ? 'bg-yellow-100 text-yellow-800'
            : 'bg-green-100 text-green-800'
        }`}>
          {row.original.isOptional ? 'Optional' : 'Mandatory'}
        </span>
      ),
    },
    {
      accessorKey: 'isFloating',
      header: 'Floating',
      cell: ({ row }) => (
        <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
          row.original.isFloating
            ? 'bg-blue-100 text-blue-800'
            : 'bg-gray-100 text-gray-800'
        }`}>
          {row.original.isFloating ? 'Yes' : 'No'}
        </span>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const holiday = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleEdit(holiday)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit holiday"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(holiday)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete holiday"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  const years = [currentYear - 1, currentYear, currentYear + 1]

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
        <div className="text-red-600 mb-4">Failed to load holidays</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  const mandatoryHolidays = holidays.filter(h => !h.isOptional)
  const optionalHolidays = holidays.filter(h => h.isOptional)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Holidays</h1>
        <p className="text-gray-600 mt-2">Manage company holiday calendar</p>
      </div>

      <div className="flex flex-wrap items-center gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
          <CompanySelect
            companies={companies}
            value={selectedCompanyId}
            onChange={setSelectedCompanyId}
            showAllOption
            allOptionLabel="All Companies"
            className="w-[250px]"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Year</label>
          <select
            value={selectedYear}
            onChange={(e) => setSelectedYear(Number(e.target.value))}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
          >
            {years.map(year => (
              <option key={year} value={year}>{year}</option>
            ))}
          </select>
        </div>
        {selectedCompanyId && (
          <button
            onClick={handleCopyToNextYear}
            disabled={copyToNextYear.isPending}
            className="mt-6 inline-flex items-center px-3 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50"
          >
            <Copy size={16} className="mr-2" />
            Copy to {selectedYear + 1}
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Holidays</div>
          <div className="text-2xl font-bold text-gray-900">{holidays.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Mandatory</div>
          <div className="text-2xl font-bold text-green-600">{mandatoryHolidays.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Optional</div>
          <div className="text-2xl font-bold text-yellow-600">{optionalHolidays.length}</div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={holidays}
            searchPlaceholder="Search holidays..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Holiday"
          />
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Add Holiday"
        size="md"
      >
        <HolidayForm
          companies={companies}
          year={selectedYear}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
          createMutation={createHoliday}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingHoliday}
        onClose={() => setEditingHoliday(null)}
        title="Edit Holiday"
        size="md"
      >
        {editingHoliday && (
          <HolidayForm
            holiday={editingHoliday}
            companies={companies}
            year={selectedYear}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingHoliday(null)}
            updateMutation={updateHoliday}
          />
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingHoliday}
        onClose={() => setDeletingHoliday(null)}
        title="Delete Holiday"
        size="sm"
      >
        {deletingHoliday && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the holiday <strong>{deletingHoliday.name}</strong> on{' '}
              <strong>{format(new Date(deletingHoliday.date), 'dd MMM yyyy')}</strong>?
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingHoliday(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteHoliday.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteHoliday.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

// Inline form component
interface HolidayFormProps {
  holiday?: Holiday
  companies: { id: string; name: string }[]
  year: number
  onSuccess: () => void
  onCancel: () => void
  createMutation?: ReturnType<typeof useCreateHoliday>
  updateMutation?: ReturnType<typeof useUpdateHoliday>
}

const HolidayForm = ({ holiday, companies, year, onSuccess, onCancel, createMutation, updateMutation }: HolidayFormProps) => {
  const [formData, setFormData] = useState<CreateHolidayDto>({
    companyId: holiday?.companyId || '',
    name: holiday?.name || '',
    date: holiday?.date || '',
    year: holiday?.year || year,
    isOptional: holiday?.isOptional || false,
    isFloating: holiday?.isFloating || false,
    description: holiday?.description || '',
  })
  const [companyError, setCompanyError] = useState<string>('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    // Validate companyId
    if (!formData.companyId) {
      setCompanyError('Please select a company')
      return
    }
    setCompanyError('')

    try {
      if (holiday && updateMutation) {
        await updateMutation.mutateAsync({ id: holiday.id, data: formData })
      } else if (createMutation) {
        await createMutation.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Failed to save holiday:', error)
    }
  }

  const isPending = createMutation?.isPending || updateMutation?.isPending

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Company *</label>
        <CompanySelect
          companies={companies}
          value={formData.companyId}
          onChange={(value) => {
            setFormData({ ...formData, companyId: value })
            if (value) setCompanyError('')
          }}
          placeholder="Select company..."
          error={companyError}
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Holiday Name *</label>
        <input
          type="text"
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
          placeholder="e.g., Republic Day"
          required
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Date *</label>
        <input
          type="date"
          value={formData.date}
          onChange={(e) => {
            const date = e.target.value
            const dateYear = new Date(date).getFullYear()
            setFormData({ ...formData, date, year: dateYear })
          }}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
          required
        />
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

      <div className="space-y-3">
        <label className="flex items-center">
          <input
            type="checkbox"
            checked={formData.isOptional}
            onChange={(e) => setFormData({ ...formData, isOptional: e.target.checked })}
            className="mr-2"
          />
          <span className="text-sm text-gray-700">Optional Holiday</span>
        </label>

        <label className="flex items-center">
          <input
            type="checkbox"
            checked={formData.isFloating}
            onChange={(e) => setFormData({ ...formData, isFloating: e.target.checked })}
            className="mr-2"
          />
          <span className="text-sm text-gray-700">Floating Holiday (employee can choose when to take)</span>
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
          {isPending ? 'Saving...' : holiday ? 'Update' : 'Create'}
        </button>
      </div>
    </form>
  )
}

export default HolidaysManagement

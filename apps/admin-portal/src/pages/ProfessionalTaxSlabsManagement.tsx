import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryStates, parseAsString } from 'nuqs'
import {
  useProfessionalTaxSlabs,
  useDeleteProfessionalTaxSlab,
  useDistinctPtStates,
} from '@/features/payroll/hooks'
import { ProfessionalTaxSlab, NO_PT_STATES } from '@/features/payroll/types/payroll'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { formatINR } from '@/lib/currency'
import { Edit, Trash2, Plus, ArrowLeft, AlertTriangle } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { Button } from '@/components/ui/button'
import { ProfessionalTaxSlabForm } from '@/components/forms/ProfessionalTaxSlabForm'

const ProfessionalTaxSlabsManagement = () => {
  const navigate = useNavigate()
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingSlab, setEditingSlab] = useState<ProfessionalTaxSlab | null>(null)
  const [deletingSlab, setDeletingSlab] = useState<ProfessionalTaxSlab | null>(null)

  const { data: configuredStates = [] } = useDistinctPtStates()
  const deleteProfessionalTaxSlab = useDeleteProfessionalTaxSlab()

  const [urlState, setUrlState] = useQueryStates(
    {
      state: parseAsString,
    },
    { history: 'push' }
  )

  const { data: slabs = [], isLoading, error } = useProfessionalTaxSlabs(
    urlState.state || undefined
  )

  const handleEdit = (slab: ProfessionalTaxSlab) => {
    setEditingSlab(slab)
  }

  const handleDelete = (slab: ProfessionalTaxSlab) => {
    setDeletingSlab(slab)
  }

  const handleDeleteConfirm = async () => {
    if (deletingSlab) {
      try {
        await deleteProfessionalTaxSlab.mutateAsync(deletingSlab.id)
        setDeletingSlab(null)
      } catch (error) {
        console.error('Failed to delete PT slab:', error)
      }
    }
  }

  const formatIncomeRange = (min: number, max: number | null | undefined) => {
    if (max === null || max === undefined) {
      return `${formatINR(min)}+`
    }
    return `${formatINR(min)} - ${formatINR(max)}`
  }

  const columns: ColumnDef<ProfessionalTaxSlab>[] = [
    {
      accessorKey: 'state',
      header: 'State',
      cell: ({ row }) => {
        const state = row.original.state
        const hasNoPT = NO_PT_STATES.includes(state as any)
        return (
          <div className="flex items-center gap-1">
            {state}
            {hasNoPT && (
              <span className="text-xs text-yellow-600" title="This state does not levy PT">
                <AlertTriangle className="w-3 h-3" />
              </span>
            )}
          </div>
        )
      },
    },
    {
      id: 'incomeRange',
      header: 'Income Range',
      cell: ({ row }) =>
        formatIncomeRange(row.original.minMonthlyIncome, row.original.maxMonthlyIncome),
    },
    {
      accessorKey: 'monthlyTax',
      header: 'Monthly Tax',
      cell: ({ row }) => formatINR(row.original.monthlyTax),
    },
    {
      accessorKey: 'februaryTax',
      header: 'February Tax',
      cell: ({ row }) =>
        row.original.februaryTax !== null && row.original.februaryTax !== undefined
          ? formatINR(row.original.februaryTax)
          : '—',
    },
    {
      id: 'annualTax',
      header: 'Annual Tax',
      cell: ({ row }) => {
        const monthlyTax = row.original.monthlyTax
        const februaryTax = row.original.februaryTax ?? monthlyTax
        const annual = (monthlyTax * 11) + februaryTax
        return formatINR(annual)
      },
    },
    {
      accessorKey: 'effectiveFrom',
      header: 'Effective From',
      cell: ({ row }) =>
        row.original.effectiveFrom
          ? format(new Date(row.original.effectiveFrom), 'MMM dd, yyyy')
          : '—',
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => (
        <span
          className={`px-2 py-1 rounded text-xs font-medium ${
            row.original.isActive
              ? 'bg-green-100 text-green-800'
              : 'bg-gray-100 text-gray-800'
          }`}
        >
          {row.original.isActive ? 'Active' : 'Inactive'}
        </span>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const slab = row.original
        return (
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => handleEdit(slab)}
              title="Edit"
            >
              <Edit className="w-4 h-4" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => handleDelete(slab)}
              title="Delete"
              className="text-red-600 hover:text-red-700"
            >
              <Trash2 className="w-4 h-4" />
            </Button>
          </div>
        )
      },
    },
  ]

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => navigate('/payroll/settings')}
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              Professional Tax Slabs
            </h1>
            <p className="text-sm text-gray-500 mt-1">
              Manage PT slabs for Indian states. PT varies by state and income level.
            </p>
          </div>
        </div>
        <Button onClick={() => setIsCreateDrawerOpen(true)}>
          <Plus className="w-4 h-4 mr-2" />
          Add PT Slab
        </Button>
      </div>

      {/* Info Banner */}
      <div className="bg-blue-50 border-l-4 border-blue-400 p-4 mb-6">
        <div className="flex">
          <div className="ml-3">
            <p className="text-sm text-blue-700">
              <strong>Note:</strong> Professional Tax is capped at Rs 2,500 per annum in
              most states. Some states like Karnataka and Maharashtra have a higher tax in
              February to reach the annual cap.
            </p>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div className="flex gap-4 mb-4">
        <div className="w-64">
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Filter by State
          </label>
          <select
            value={urlState.state || ''}
            onChange={(e) => setUrlState({ state: e.target.value || null })}
            className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
          >
            <option value="">All States</option>
            {configuredStates.map((state) => (
              <option key={state} value={state}>
                {state}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Table */}
      {error ? (
        <div className="bg-red-50 border border-red-200 rounded-md p-4">
          <p className="text-red-700">
            Error loading PT slabs: {(error as Error).message}
          </p>
        </div>
      ) : isLoading ? (
        <div className="flex items-center justify-center min-h-[200px]">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      ) : slabs.length === 0 ? (
        <div className="text-center py-12 bg-gray-50 rounded-md">
          <p className="text-gray-500">No Professional Tax slabs configured</p>
          <Button
            onClick={() => setIsCreateDrawerOpen(true)}
            className="mt-4"
          >
            <Plus className="w-4 h-4 mr-2" />
            Add First PT Slab
          </Button>
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={slabs}
          searchPlaceholder="Search by state..."
        />
      )}

      {/* Create Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Add Professional Tax Slab"
      >
        <ProfessionalTaxSlabForm
          onSuccess={() => setIsCreateDrawerOpen(false)}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Drawer */}
      <Drawer
        isOpen={!!editingSlab}
        onClose={() => setEditingSlab(null)}
        title="Edit Professional Tax Slab"
      >
        {editingSlab && (
          <ProfessionalTaxSlabForm
            slab={editingSlab}
            onSuccess={() => setEditingSlab(null)}
            onCancel={() => setEditingSlab(null)}
          />
        )}
      </Drawer>

      {/* Delete Modal */}
      <Modal
        isOpen={!!deletingSlab}
        onClose={() => setDeletingSlab(null)}
        title="Delete Professional Tax Slab"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Are you sure you want to delete the PT slab for{' '}
            <strong>{deletingSlab?.state}</strong> (
            {deletingSlab
              ? formatIncomeRange(
                  deletingSlab.minMonthlyIncome,
                  deletingSlab.maxMonthlyIncome
                )
              : ''}
            )?
          </p>
          <p className="text-sm text-red-600">This action cannot be undone.</p>
          <div className="flex justify-end gap-3 pt-4">
            <Button
              variant="outline"
              onClick={() => setDeletingSlab(null)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteConfirm}
              disabled={deleteProfessionalTaxSlab.isPending}
            >
              {deleteProfessionalTaxSlab.isPending ? 'Deleting...' : 'Delete'}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

export default ProfessionalTaxSlabsManagement

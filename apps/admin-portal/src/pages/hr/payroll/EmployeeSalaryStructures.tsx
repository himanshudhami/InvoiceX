import { useState, useMemo } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import {
  useSalaryStructures,
  useDeleteSalaryStructure,
  useCurrentSalaryStructure,
} from '@/features/payroll/hooks'
import { useEmployees } from '@/hooks/api/useEmployees'
import { useCompanies } from '@/hooks/api/useCompanies'
import { EmployeeSalaryStructure } from '@/features/payroll/types/payroll'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { formatINR } from '@/lib/currency'
import { Edit, Trash2, Plus, Eye, History, ArrowLeft } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { Button } from '@/components/ui/button'
import { SalaryStructureForm } from '@/components/forms/SalaryStructureForm'

const EmployeeSalaryStructures = () => {
  const navigate = useNavigate()
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingStructure, setEditingStructure] = useState<EmployeeSalaryStructure | null>(null)
  const [deletingStructure, setDeletingStructure] = useState<EmployeeSalaryStructure | null>(null)
  const [viewingHistory, setViewingHistory] = useState<string | null>(null)

  const { data: employees = [] } = useEmployees()
  const { data: companies = [] } = useCompanies()
  const deleteSalaryStructure = useDeleteSalaryStructure()

  const [urlState, setUrlState] = useQueryStates(
    {
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(100),
      searchTerm: parseAsString,
      companyId: parseAsString,
      employeeId: parseAsString,
      isActive: parseAsString,
    },
    { history: 'push' }
  )

  const { data, isLoading, error } = useSalaryStructures({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: urlState.searchTerm || undefined,
    companyId: urlState.companyId || undefined,
    employeeId: urlState.employeeId || undefined,
    isActive: urlState.isActive === 'true' ? true : urlState.isActive === 'false' ? false : undefined,
  })

  const handleEdit = (structure: EmployeeSalaryStructure) => {
    setEditingStructure(structure)
  }

  const handleDelete = (structure: EmployeeSalaryStructure) => {
    setDeletingStructure(structure)
  }

  const handleDeleteConfirm = async () => {
    if (deletingStructure) {
      try {
        await deleteSalaryStructure.mutateAsync(deletingStructure.id)
        setDeletingStructure(null)
      } catch (error) {
        console.error('Failed to delete salary structure:', error)
      }
    }
  }

  const handleViewHistory = (employeeId: string) => {
    setViewingHistory(employeeId)
  }

  const totals = useMemo(() => {
    const items = data?.items || []
    return items.reduce(
      (acc, item) => {
        acc.count += 1
        acc.annualCtc += item.annualCtc || 0
        acc.monthlyGross += item.monthlyGross || 0
        return acc
      },
      { count: 0, annualCtc: 0, monthlyGross: 0 }
    )
  }, [data?.items])

  const actionClasses = {
    view: 'text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors',
    edit: 'text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors',
    delete: 'text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors',
  }

  const columns: ColumnDef<EmployeeSalaryStructure>[] = [
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => row.original.employeeName || '—',
    },
    {
      accessorKey: 'annualCtc',
      header: 'Annual CTC',
      cell: ({ row }) => formatINR(row.original.annualCtc),
    },
    {
      accessorKey: 'monthlyGross',
      header: 'Monthly Gross',
      cell: ({ row }) => formatINR(row.original.monthlyGross),
    },
    {
      accessorKey: 'effectiveFrom',
      header: 'Effective From',
      cell: ({ row }) => format(new Date(row.original.effectiveFrom), 'MMM dd, yyyy'),
    },
    {
      accessorKey: 'effectiveTo',
      header: 'Effective To',
      cell: ({ row }) =>
        row.original.effectiveTo
          ? format(new Date(row.original.effectiveTo), 'MMM dd, yyyy')
          : 'Current',
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
        const structure = row.original
        return (
          <div className="flex items-center gap-2">
            <button
              className={actionClasses.view}
              onClick={() => handleViewHistory(structure.employeeId)}
              title="View history"
            >
              <History className="w-4 h-4" />
            </button>
            <button
              className={actionClasses.edit}
              onClick={() => handleEdit(structure)}
              title="Edit"
            >
              <Edit className="w-4 h-4" />
            </button>
            <button
              className={actionClasses.delete}
              onClick={() => handleDelete(structure)}
              title="Delete"
            >
              <Trash2 className="w-4 h-4" />
            </button>
          </div>
        )
      },
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate('/payroll')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Dashboard
        </Button>
      </div>
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Salary Structures</h1>
          <p className="text-gray-600 mt-1">Manage employee CTC and salary breakdowns</p>
        </div>
        <div className="flex gap-3">
          <CompanyFilterDropdown
            value={urlState.companyId || ''}
            onChange={(value) => setUrlState({ companyId: value || null })}
          />
          <Button onClick={() => setIsCreateDrawerOpen(true)}>
            <Plus className="w-4 h-4 mr-2" />
            Add Salary Structure
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-600">Failed to load salary structures</p>
        </div>
      )}

      <DataTable
        columns={columns}
        data={data?.items || []}
        searchPlaceholder="Search by employee name..."
        onSearchChange={(value) => setUrlState({ searchTerm: value || null, page: 1 })}
        pagination={{
          pageIndex: (data?.pageNumber || urlState.page) - 1,
          pageSize: data?.pageSize || urlState.pageSize,
          totalCount: data?.totalCount || 0,
          onPageChange: (pageIndex) => setUrlState({ page: pageIndex + 1 }),
          onPageSizeChange: (size) => setUrlState({ pageSize: size, page: 1 }),
        }}
        footer={() => {
          const colCount = columns.length
          return (
            <tfoot className="bg-gray-100 border-t-2 border-gray-300 text-sm font-semibold text-gray-900">
              <tr>
                <td className="px-6 py-4">
                  Totals ({totals.count} structures)
                </td>
                <td className="px-6 py-4 text-blue-700">
                  {formatINR(totals.annualCtc)}
                </td>
                <td className="px-6 py-4 text-green-700">
                  {formatINR(totals.monthlyGross)}
                </td>
                {Array.from({ length: colCount - 3 }).map((_, idx) => (
                  <td key={idx} className="px-6 py-4"></td>
                ))}
              </tr>
            </tfoot>
          )
        }}
      />

      {/* Create/Edit Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen || !!editingStructure}
        onClose={() => {
          setIsCreateDrawerOpen(false)
          setEditingStructure(null)
        }}
        title={editingStructure ? 'Edit Salary Structure' : 'Add Salary Structure'}
      >
        <SalaryStructureForm
          structure={editingStructure || undefined}
          onSuccess={() => {
            setIsCreateDrawerOpen(false)
            setEditingStructure(null)
          }}
          onCancel={() => {
            setIsCreateDrawerOpen(false)
            setEditingStructure(null)
          }}
        />
      </Drawer>

      {/* Delete Modal */}
      <Modal
        isOpen={!!deletingStructure}
        onClose={() => setDeletingStructure(null)}
        title="Delete Salary Structure"
      >
        <div className="space-y-4">
          <p>
            Are you sure you want to delete the salary structure for{' '}
            <span className="font-semibold">{deletingStructure?.employeeName}</span>?
          </p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setDeletingStructure(null)}>
              Cancel
            </Button>
            <Button
              onClick={handleDeleteConfirm}
              disabled={deleteSalaryStructure.isPending}
            >
              {deleteSalaryStructure.isPending ? 'Deleting...' : 'Delete'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* History Modal */}
      <Modal
        isOpen={!!viewingHistory}
        onClose={() => setViewingHistory(null)}
        title="Salary Structure History"
      >
        <div className="p-4">
          <p className="text-gray-500">History view will be implemented here</p>
          {/* TODO: Add history component */}
        </div>
      </Modal>
    </div>
  )
}

export default EmployeeSalaryStructures

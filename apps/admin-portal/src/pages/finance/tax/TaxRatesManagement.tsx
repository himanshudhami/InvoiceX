import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useTaxRates, useDeleteTaxRate } from '@/hooks/api/useTaxRates'
import { useCompanies } from '@/hooks/api/useCompanies'
import { TaxRate } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { TaxRateForm } from '@/components/forms/TaxRateForm'
import { Edit, Trash2 } from 'lucide-react'

const TaxRatesManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingTaxRate, setEditingTaxRate] = useState<TaxRate | null>(null)
  const [deletingTaxRate, setDeletingTaxRate] = useState<TaxRate | null>(null)

  const { data: taxRates = [], isLoading, error, refetch } = useTaxRates()
  const { data: companies = [] } = useCompanies()
  const deleteTaxRate = useDeleteTaxRate()

  const handleEdit = (taxRate: TaxRate) => {
    setEditingTaxRate(taxRate)
  }

  const handleDelete = (taxRate: TaxRate) => {
    setDeletingTaxRate(taxRate)
  }

  const handleDeleteConfirm = async () => {
    if (deletingTaxRate) {
      try {
        await deleteTaxRate.mutateAsync(deletingTaxRate.id)
        setDeletingTaxRate(null)
      } catch (error) {
        console.error('Failed to delete tax rate:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingTaxRate(null)
    refetch()
  }

  // Helper function to get company name
  const getCompanyName = (companyId?: string) => {
    if (!companyId) return 'Global'
    const company = companies.find(c => c.id === companyId)
    return company?.name || 'Unknown Company'
  }

  const columns: ColumnDef<TaxRate>[] = [
    {
      accessorKey: 'name',
      header: 'Tax Rate',
      cell: ({ row }) => {
        const taxRate = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{taxRate.name}</div>
            <div className="text-sm text-gray-500">
              {taxRate.rate}% tax rate
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'rate',
      header: 'Rate',
      cell: ({ row }) => {
        const rate = row.getValue('rate') as number
        return (
          <div className="text-right">
            <div className="font-medium text-gray-900">{rate}%</div>
            <div className="text-sm text-gray-500">
              ${(100 * (rate / 100)).toFixed(2)} on $100
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'companyId',
      header: 'Scope',
      cell: ({ row }) => {
        const companyName = getCompanyName(row.original.companyId)
        const isGlobal = !row.original.companyId
        return (
          <div>
            <div className={`text-sm font-medium ${isGlobal ? 'text-blue-600' : 'text-gray-900'}`}>
              {companyName}
            </div>
            <div className="text-xs text-gray-500">
              {isGlobal ? 'Available to all companies' : 'Company-specific'}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'isDefault',
      header: 'Status',
      cell: ({ row }) => {
        const taxRate = row.original
        const isDefault = taxRate.isDefault
        const isActive = taxRate.isActive
        
        return (
          <div className="space-y-1">
            {isDefault && (
              <div className="inline-flex px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800">
                Default
              </div>
            )}
            <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
              isActive
                ? 'bg-green-100 text-green-800'
                : 'bg-gray-100 text-gray-800'
            }`}>
              {isActive ? 'Active' : 'Inactive'}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'createdAt',
      header: 'Created',
      cell: ({ row }) => {
        const createdAt = row.getValue('createdAt') as string
        return createdAt ? (
          <div className="text-sm text-gray-500">
            {new Date(createdAt).toLocaleDateString()}
          </div>
        ) : (
          <div className="text-sm text-gray-500">—</div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const taxRate = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleEdit(taxRate)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit tax rate"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(taxRate)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete tax rate"
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
        <div className="text-red-600 mb-4">Failed to load tax rates</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  const activeTaxRates = taxRates.filter(tr => tr.isActive)
  const defaultTaxRates = taxRates.filter(tr => tr.isDefault)
  const globalTaxRates = taxRates.filter(tr => !tr.companyId)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Tax Rates</h1>
        <p className="text-gray-600 mt-2">Manage tax rates for invoices and products</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Tax Rates</div>
          <div className="text-2xl font-bold text-gray-900">{taxRates.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Active</div>
          <div className="text-2xl font-bold text-green-600">{activeTaxRates.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Default</div>
          <div className="text-2xl font-bold text-blue-600">{defaultTaxRates.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Global</div>
          <div className="text-2xl font-bold text-purple-600">{globalTaxRates.length}</div>
        </div>
      </div>

      {/* Quick Info */}
      {defaultTaxRates.length > 0 && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-blue-400" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-blue-800">Default Tax Rates</h3>
              <div className="mt-1 text-sm text-blue-700">
                {defaultTaxRates.map(tr => `${tr.name} (${tr.rate}%)`).join(', ')} 
                {defaultTaxRates.length === 1 ? ' is' : ' are'} set as default.
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={taxRates}
            searchPlaceholder="Search tax rates..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Tax Rate"
          />
        </div>
      </div>

      {/* Create Tax Rate Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create New Tax Rate"
        size="lg"
      >
        <TaxRateForm
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Tax Rate Drawer */}
      <Drawer
        isOpen={!!editingTaxRate}
        onClose={() => setEditingTaxRate(null)}
        title="Edit Tax Rate"
        size="lg"
      >
        {editingTaxRate && (
          <TaxRateForm
            taxRate={editingTaxRate}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingTaxRate(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingTaxRate}
        onClose={() => setDeletingTaxRate(null)}
        title="Delete Tax Rate"
        size="sm"
      >
        {deletingTaxRate && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the tax rate <strong>{deletingTaxRate.name}</strong> ({deletingTaxRate.rate}%)? 
              This action cannot be undone and may affect related products and invoices.
            </p>
            {deletingTaxRate.isDefault && (
              <div className="p-3 bg-amber-50 border border-amber-200 rounded-md">
                <div className="text-amber-800 text-sm">
                  <strong>Warning:</strong> This is a default tax rate. Deleting it may affect automatic tax calculations.
                </div>
              </div>
            )}
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingTaxRate(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteTaxRate.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteTaxRate.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default TaxRatesManagement
import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useCompanies, useDeleteCompany } from '@/hooks/api/useCompanies'
import { Company } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { CompanyForm } from '@/components/forms/CompanyForm'
import { Edit, Trash2 } from 'lucide-react'

const CompaniesManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingCompany, setEditingCompany] = useState<Company | null>(null)
  const [deletingCompany, setDeletingCompany] = useState<Company | null>(null)

  const { data: companies = [], isLoading, error, refetch } = useCompanies()
  const deleteCompany = useDeleteCompany()

  const handleEdit = (company: Company) => {
    setEditingCompany(company)
  }

  const handleDelete = (company: Company) => {
    setDeletingCompany(company)
  }

  const handleDeleteConfirm = async () => {
    if (deletingCompany) {
      try {
        await deleteCompany.mutateAsync(deletingCompany.id)
        setDeletingCompany(null)
      } catch (error) {
        console.error('Failed to delete company:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingCompany(null)
    refetch()
  }

  const columns: ColumnDef<Company>[] = [
    {
      accessorKey: 'name',
      header: 'Company Name',
      cell: ({ row }) => {
        const company = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{company.name}</div>
            {company.website && (
              <div className="text-sm text-gray-500">{company.website}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'email',
      header: 'Contact',
      cell: ({ row }) => {
        const company = row.original
        return (
          <div>
            {company.email && (
              <div className="text-sm text-gray-900">{company.email}</div>
            )}
            {company.phone && (
              <div className="text-sm text-gray-500">{company.phone}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'city',
      header: 'Location',
      cell: ({ row }) => {
        const company = row.original
        const location = [company.city, company.state].filter(Boolean).join(', ')
        return location ? (
          <div className="text-sm text-gray-900">{location}</div>
        ) : (
          <div className="text-sm text-gray-500">—</div>
        )
      },
    },
    {
      accessorKey: 'taxNumber',
      header: 'Tax Number',
      cell: ({ row }) => {
        const taxNumber = row.getValue('taxNumber') as string
        return taxNumber ? (
          <div className="text-sm text-gray-900">{taxNumber}</div>
        ) : (
          <div className="text-sm text-gray-500">—</div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const company = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleEdit(company)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit company"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(company)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete company"
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
        <div className="text-red-600 mb-4">Failed to load companies</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Companies</h1>
        <p className="text-gray-600 mt-2">Manage your company information and details</p>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={companies}
            searchPlaceholder="Search companies..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Company"
          />
        </div>
      </div>

      {/* Create Company Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create New Company"
        size="lg"
      >
        <CompanyForm
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Company Drawer */}
      <Drawer
        isOpen={!!editingCompany}
        onClose={() => setEditingCompany(null)}
        title="Edit Company"
        size="lg"
      >
        {editingCompany && (
          <CompanyForm
            company={editingCompany}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingCompany(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingCompany}
        onClose={() => setDeletingCompany(null)}
        title="Delete Company"
        size="sm"
      >
        {deletingCompany && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingCompany.name}</strong>? 
              This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingCompany(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteCompany.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteCompany.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default CompaniesManagement
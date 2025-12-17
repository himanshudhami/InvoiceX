import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useCustomers, useDeleteCustomer } from '@/features/customers/hooks'
import { Customer } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { useNavigate } from 'react-router-dom'
import { Eye, Edit, Trash2 } from 'lucide-react'
import { useQueryState, parseAsString, parseAsInteger } from 'nuqs'

const CustomerList = () => {
  const navigate = useNavigate()
  const [deletingCustomer, setDeletingCustomer] = useState<Customer | null>(null)

  // URL-backed search state
  const [search, setSearch] = useQueryState('search', parseAsString.withDefault(''))
  const [page, setPage] = useQueryState('page', parseAsInteger.withDefault(1))

  const { data: customers = [], isLoading, error, refetch } = useCustomers()
  const deleteCustomer = useDeleteCustomer()

  const handleDelete = (customer: Customer) => {
    setDeletingCustomer(customer)
  }

  const handleDeleteConfirm = async () => {
    if (deletingCustomer) {
      try {
        await deleteCustomer.mutateAsync(deletingCustomer.id)
        setDeletingCustomer(null)
      } catch (error) {
        console.error('Failed to delete customer:', error)
      }
    }
  }

  const columns: ColumnDef<Customer>[] = [
    {
      accessorKey: 'name',
      header: 'Customer Name',
      cell: ({ row }) => {
        const customer = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{customer.name}</div>
            {customer.companyName && (
              <div className="text-sm text-gray-500">{customer.companyName}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'email',
      header: 'Contact',
      cell: ({ row }) => {
        const customer = row.original
        return (
          <div>
            {customer.email && (
              <div className="text-sm text-gray-900">{customer.email}</div>
            )}
            {customer.phone && (
              <div className="text-sm text-gray-500">{customer.phone}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'city',
      header: 'Location',
      cell: ({ row }) => {
        const customer = row.original
        const location = [customer.city, customer.state].filter(Boolean).join(', ')
        return location ? (
          <div className="text-sm text-gray-900">{location}</div>
        ) : (
          <div className="text-sm text-gray-500">—</div>
        )
      },
    },
    {
      accessorKey: 'paymentTerms',
      header: 'Payment Terms',
      cell: ({ row }) => {
        const paymentTerms = row.getValue('paymentTerms') as number
        return paymentTerms ? (
          <div className="text-sm text-gray-900">{paymentTerms} days</div>
        ) : (
          <div className="text-sm text-gray-500">—</div>
        )
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => {
        const isActive = row.getValue('isActive') as boolean
        return (
          <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
            isActive
              ? 'bg-green-100 text-green-800'
              : 'bg-gray-100 text-gray-800'
          }`}>
            {isActive ? 'Active' : 'Inactive'}
          </div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const customer = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => navigate(`/customers/${customer.id}`)}
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="View customer"
            >
              <Eye size={16} />
            </button>
            <button
              onClick={() => navigate(`/customers/${customer.id}/edit`)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit customer"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(customer)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete customer"
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
        <div className="text-red-600 mb-4">Failed to load customers</div>
        <button
          onClick={() => refetch?.()}
          className="px-4 py-2 border rounded-md hover:bg-gray-50"
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Customers</h1>
          <p className="text-gray-600 mt-2">Manage your customer database</p>
        </div>
        <button
          onClick={() => navigate('/customers/new')}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          Add Customer
        </button>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={customers}
            searchPlaceholder="Search customers..."
            initialSearch={search}
            onSearchChange={(value) => setSearch(value || null)}
          />
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingCustomer}
        onClose={() => setDeletingCustomer(null)}
        title="Delete Customer"
        size="sm"
      >
        {deletingCustomer && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingCustomer.name}</strong>? 
              This action cannot be undone and may affect related invoices.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingCustomer(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteCustomer.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteCustomer.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default CustomerList

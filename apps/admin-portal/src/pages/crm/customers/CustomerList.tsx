import { useMemo, useState, useEffect } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useCustomersPaged, useDeleteCustomer } from '@/features/customers/hooks'
import { useCompanyContext } from '@/contexts/CompanyContext'
import { Customer } from '@/services/api/types'
import { Modal } from '@/components/ui/Modal'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { useNavigate } from 'react-router-dom'
import { Eye, Edit, Trash2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'

const CustomerList = () => {
  const navigate = useNavigate()
  const [deletingCustomer, setDeletingCustomer] = useState<Customer | null>(null)

  // Get selected company from context (for multi-company users)
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext()

  // URL-backed filter state with nuqs - persists on refresh
  const [urlState, setUrlState] = useQueryStates(
    {
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(100),
      search: parseAsString.withDefault(''),
      company: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  // Determine effective company ID: URL filter takes precedence, then context selection
  const effectiveCompanyId = urlState.company || (hasMultiCompanyAccess ? selectedCompanyId : undefined)

  // Debounced search term
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState(urlState.search)

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(urlState.search)
    }, 300)
    return () => clearTimeout(timer)
  }, [urlState.search])

  // Server-side paginated data
  const { data, isLoading, error, refetch } = useCustomersPaged({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: debouncedSearchTerm || undefined,
    companyId: effectiveCompanyId || undefined,
  })

  const deleteCustomer = useDeleteCustomer()

  // Extract items and pagination info from response
  const customers = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

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

  const columns = useMemo<ColumnDef<Customer>[]>(() => [
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
  ], [navigate])

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

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Customers</div>
          <div className="text-2xl font-bold text-gray-900">{totalCount}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Active</div>
          <div className="text-2xl font-bold text-green-600">
            {customers.filter(c => c.isActive).length}
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Inactive</div>
          <div className="text-2xl font-bold text-gray-600">
            {customers.filter(c => !c.isActive).length}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          {/* Search and Filters */}
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between mb-4">
            <div className="flex flex-wrap items-center gap-3">
              <CompanyFilterDropdown
                value={urlState.company}
                onChange={(val) => {
                  setUrlState({ company: val || '', page: 1 })
                }}
                className="min-w-[180px]"
              />

              {urlState.company && (
                <button
                  onClick={() => setUrlState({ company: '', page: 1 })}
                  className="text-sm px-3 py-2 rounded-md border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  Clear filters
                </button>
              )}
            </div>

            <input
              placeholder="Search customers..."
              value={urlState.search}
              onChange={(event) => setUrlState({ search: event.target.value, page: 1 })}
              className="w-full md:w-auto max-w-sm px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>

          {/* Table */}
          <div className="rounded-md border overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  {columns.map((column) => (
                    <th
                      key={column.id || (column as any).accessorKey}
                      className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                    >
                      {typeof column.header === 'string' ? column.header : ''}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {customers.length > 0 ? (
                  customers.map((customer) => (
                    <tr key={customer.id} className="hover:bg-gray-50 transition-colors">
                      {columns.map((column) => (
                        <td
                          key={`${customer.id}-${column.id || (column as any).accessorKey}`}
                          className="px-6 py-4 whitespace-nowrap text-sm text-gray-900"
                        >
                          {column.cell
                            ? (column.cell as any)({ row: { original: customer, getValue: (key: string) => (customer as any)[key] } })
                            : (customer as any)[(column as any).accessorKey]}
                        </td>
                      ))}
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={columns.length} className="px-6 py-12 text-center text-gray-500">
                      No customers found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between mt-4">
            <div className="flex items-center space-x-2">
              <span className="text-sm text-gray-700">
                Page {urlState.page} of {totalPages}
              </span>
              <span className="text-sm text-gray-500">
                ({totalCount} total customers)
              </span>
              <PageSizeSelect
                value={urlState.pageSize}
                onChange={(size) => setUrlState({ pageSize: size, page: 1 })}
              />
            </div>

            <div className="flex items-center space-x-2">
              <button
                onClick={() => setUrlState({ page: urlState.page - 1 })}
                disabled={urlState.page <= 1}
                className={cn(
                  "px-3 py-1 rounded-md text-sm transition-colors",
                  urlState.page > 1
                    ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                    : "bg-gray-100 text-gray-400 cursor-not-allowed"
                )}
              >
                Previous
              </button>

              <div className="flex items-center space-x-1">
                {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                  const startPage = Math.max(1, urlState.page - 2)
                  const page = startPage + i

                  if (page > totalPages) return null

                  return (
                    <button
                      key={page}
                      onClick={() => setUrlState({ page })}
                      className={cn(
                        "w-8 h-8 rounded text-sm transition-colors",
                        page === urlState.page
                          ? "bg-primary text-primary-foreground"
                          : "bg-gray-200 hover:bg-gray-300 text-gray-700"
                      )}
                    >
                      {page}
                    </button>
                  )
                })}
              </div>

              <button
                onClick={() => setUrlState({ page: urlState.page + 1 })}
                disabled={urlState.page >= totalPages}
                className={cn(
                  "px-3 py-1 rounded-md text-sm transition-colors",
                  urlState.page < totalPages
                    ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                    : "bg-gray-100 text-gray-400 cursor-not-allowed"
                )}
              >
                Next
              </button>
            </div>
          </div>
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

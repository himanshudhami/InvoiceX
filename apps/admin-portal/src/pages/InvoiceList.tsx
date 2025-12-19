import { useMemo, useState, useEffect } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useInvoicesPaged, useDeleteInvoice, useDuplicateInvoice } from '@/features/invoices/hooks'
import { useCustomers } from '@/features/customers/hooks'
import { useCompanyContext } from '@/contexts/CompanyContext'
import { Invoice, PagedResponse } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { CustomerSelect } from '@/components/ui/CustomerSelect'
import { useNavigate } from 'react-router-dom'
import { Eye, Edit, Trash2, Copy } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'

const InvoiceList = () => {
  const navigate = useNavigate()
  const [deletingInvoice, setDeletingInvoice] = useState<Invoice | null>(null)

  // Get selected company from context (for multi-company users)
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext()

  // URL-backed filter state with nuqs - persists on refresh
  const [urlState, setUrlState] = useQueryStates(
    {
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(100),
      search: parseAsString.withDefault(''),
      status: parseAsString.withDefault(''),
      company: parseAsString.withDefault(''),
      customer: parseAsString.withDefault(''),
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
  const { data, isLoading, error, refetch } = useInvoicesPaged({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: debouncedSearchTerm || undefined,
    status: urlState.status || undefined,
    companyId: effectiveCompanyId || undefined,
    customerId: urlState.customer || undefined,
  })

  const deleteInvoice = useDeleteInvoice()
  const duplicateInvoice = useDuplicateInvoice()
  // Fetch customers scoped to the same company as invoices to keep lookups consistent
  const { data: customers = [] } = useCustomers(effectiveCompanyId)

  // Extract items and pagination info from response
  const invoices = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

  const handleDelete = (invoice: Invoice) => {
    setDeletingInvoice(invoice)
  }

  const handleDeleteConfirm = async () => {
    if (deletingInvoice) {
      try {
        await deleteInvoice.mutateAsync(deletingInvoice.id)
        setDeletingInvoice(null)
      } catch (error) {
        console.error('Failed to delete invoice:', error)
      }
    }
  }

  const handleDuplicate = async (invoice: Invoice) => {
    try {
      const newInvoice = await duplicateInvoice.mutateAsync(invoice.id)
      navigate(`/invoices/${newInvoice.id}/edit`)
    } catch (error) {
      console.error('Failed to duplicate invoice:', error)
    }
  }

  const getCustomerName = (customerId?: string) => {
    if (!customerId) return '—'
    const customer = customers.find(c => c.id === customerId)
    return customer ? `${customer.name}${customer.companyName ? ` (${customer.companyName})` : ''}` : '—'
  }

  const getStatusBadgeColor = (status?: string) => {
    switch (status?.toLowerCase()) {
      case 'paid':
        return 'bg-green-100 text-green-800'
      case 'sent':
        return 'bg-blue-100 text-blue-800'
      case 'viewed':
        return 'bg-purple-100 text-purple-800'
      case 'overdue':
        return 'bg-red-100 text-red-800'
      case 'cancelled':
        return 'bg-gray-100 text-gray-800'
      case 'draft':
      default:
        return 'bg-yellow-100 text-yellow-800'
    }
  }

  const formatCurrency = (amount: number, currency?: string) => {
    const currencySymbol = currency === 'EUR' ? '€' : currency === 'GBP' ? '£' : '$'
    return `${currencySymbol}${amount.toFixed(2)}`
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString()
  }

  const getPaymentStatus = (totalAmount: number, paidAmount?: number) => {
    if (!paidAmount || paidAmount === 0) return 'Unpaid'
    if (paidAmount >= totalAmount) return 'Fully Paid'
    return 'Partially Paid'
  }

  // Calculate totals from current page data
  const totals = useMemo(() => {
    const result = {
      subtotal: 0,
      taxAmount: 0,
      discountAmount: 0,
      totalAmount: 0,
      paidAmount: 0,
      outstanding: 0
    }

    invoices.forEach(invoice => {
      result.subtotal += invoice.subtotal || 0
      result.taxAmount += invoice.taxAmount || 0
      result.discountAmount += invoice.discountAmount || 0
      result.totalAmount += invoice.totalAmount || 0
      result.paidAmount += invoice.paidAmount || 0
    })

    result.outstanding = result.totalAmount - result.paidAmount
    return result
  }, [invoices])

  const columns = useMemo<ColumnDef<Invoice>[]>(() => [
    {
      accessorKey: 'invoiceNumber',
      header: 'Invoice',
      cell: ({ row }) => {
        const invoice = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{invoice.invoiceNumber}</div>
            <div className="text-sm text-gray-500">
              {formatDate(invoice.invoiceDate)}
            </div>
            {invoice.projectName && (
              <div className="text-sm text-blue-600">{invoice.projectName}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'customerId',
      header: 'Customer',
      cell: ({ row }) => {
        const customerName = getCustomerName(row.original.customerId)
        return <div className="text-sm text-gray-900">{customerName}</div>
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const invoice = row.original
        const status = invoice.status || 'draft'
        return (
          <div>
            <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${getStatusBadgeColor(status)}`}>
              {status.charAt(0).toUpperCase() + status.slice(1)}
            </div>
            <div className="text-xs text-gray-500 mt-1">
              {getPaymentStatus(invoice.totalAmount, invoice.paidAmount)}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'dueDate',
      header: 'Due Date',
      cell: ({ row }) => {
        const invoice = row.original
        const dueDate = new Date(invoice.dueDate)
        const isOverdue = dueDate < new Date() && invoice.status !== 'paid'

        return (
          <div>
            <div className={`text-sm ${isOverdue ? 'text-red-600 font-medium' : 'text-gray-900'}`}>
              {formatDate(invoice.dueDate)}
            </div>
            {isOverdue && (
              <div className="text-xs text-red-500">Overdue</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'totalAmount',
      header: 'Amount',
      cell: ({ row }) => {
        const invoice = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">
              {formatCurrency(invoice.totalAmount, invoice.currency)}
            </div>
            {invoice.paidAmount !== undefined && invoice.paidAmount > 0 && (
              <div className="text-sm text-green-600">
                Paid: {formatCurrency(invoice.paidAmount, invoice.currency)}
              </div>
            )}
          </div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const invoice = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => navigate(`/invoices/${invoice.id}`)}
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="View invoice"
            >
              <Eye size={16} />
            </button>
            <button
              onClick={() => navigate(`/invoices/${invoice.id}/edit`)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit invoice"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDuplicate(invoice)}
              className="text-purple-600 hover:text-purple-800 p-1 rounded hover:bg-purple-50 transition-colors"
              title="Duplicate invoice"
              disabled={duplicateInvoice.isPending}
            >
              <Copy size={16} />
            </button>
            <button
              onClick={() => handleDelete(invoice)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete invoice"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ], [navigate, duplicateInvoice.isPending, customers])

  const statusOptions = ['draft', 'sent', 'viewed', 'overdue', 'paid', 'cancelled']

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
        <div className="text-red-600 mb-4">Failed to load invoices</div>
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
          <h1 className="text-3xl font-bold text-gray-900">Invoices</h1>
          <p className="text-gray-600 mt-2">Manage all your invoices</p>
        </div>
        <button
          onClick={() => navigate('/invoices/new')}
          className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          Create Invoice
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Invoices</div>
          <div className="text-2xl font-bold text-gray-900">{totalCount}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">This Page Total</div>
          <div className="text-2xl font-bold text-gray-900">{formatCurrency(totals.totalAmount)}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">This Page Paid</div>
          <div className="text-2xl font-bold text-green-600">{formatCurrency(totals.paidAmount)}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">This Page Outstanding</div>
          <div className="text-2xl font-bold text-red-600">{formatCurrency(totals.outstanding)}</div>
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

              <div className="flex items-center gap-2">
                <label className="text-sm font-medium text-gray-700">Status</label>
                <select
                  value={urlState.status}
                  onChange={(e) => setUrlState({ status: e.target.value, page: 1 })}
                  className="px-3 py-2 border border-gray-200 rounded-md bg-white text-sm shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent min-w-[150px]"
                >
                  <option value="">All Statuses</option>
                  {statusOptions.map((status) => (
                    <option key={status} value={status}>
                      {status.charAt(0).toUpperCase() + status.slice(1)}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex items-center gap-2">
                <label className="text-sm font-medium text-gray-700">Customer</label>
                <CustomerSelect
                  customers={customers}
                  value={urlState.customer}
                  onChange={(val) => setUrlState({ customer: val, page: 1 })}
                  placeholder="All customers"
                  className="min-w-[220px]"
                  disabled={!effectiveCompanyId}
                  showAllOption
                  allOptionLabel="All customers"
                />
              </div>

              {(urlState.status || urlState.company) && (
                <button
                  onClick={() => setUrlState({ status: '', company: '', customer: '', page: 1 })}
                  className="text-sm px-3 py-2 rounded-md border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  Clear filters
                </button>
              )}
            </div>

            <input
              placeholder="Search invoices..."
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
                {invoices.length > 0 ? (
                  invoices.map((invoice) => (
                    <tr key={invoice.id} className="hover:bg-gray-50 transition-colors">
                      {columns.map((column) => (
                        <td
                          key={`${invoice.id}-${column.id || (column as any).accessorKey}`}
                          className="px-6 py-4 whitespace-nowrap text-sm text-gray-900"
                        >
                          {column.cell
                            ? (column.cell as any)({ row: { original: invoice } })
                            : (invoice as any)[(column as any).accessorKey]}
                        </td>
                      ))}
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={columns.length} className="px-6 py-12 text-center text-gray-500">
                      No invoices found.
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
                ({totalCount} total invoices)
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
        isOpen={!!deletingInvoice}
        onClose={() => setDeletingInvoice(null)}
        title="Delete Invoice"
        size="sm"
      >
        {deletingInvoice && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete invoice <strong>{deletingInvoice.invoiceNumber}</strong>?
              This action cannot be undone and will also delete all associated invoice items and payments.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingInvoice(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteInvoice.isPending}
                className="px-4 py-2 text-sm font-medium text-primary-foreground bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteInvoice.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default InvoiceList

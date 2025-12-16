import { useState, useMemo } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import { useQuotes, useDeleteQuote, useDuplicateQuote, useSendQuote, useAcceptQuote, useRejectQuote, useConvertQuoteToInvoice } from '@/hooks/api/useQuotes'
import { useCustomers } from '@/hooks/api/useCustomers'
import { useCompanies } from '@/hooks/api/useCompanies'
import { Quote } from '@/services/api/types'
import { Modal } from '@/components/ui/Modal'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { useNavigate } from 'react-router-dom'
import { Eye, Edit, Trash2, Copy, Send, CheckCircle, XCircle, FileText } from 'lucide-react'
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  flexRender,
  SortingState,
  ColumnFiltersState,
  VisibilityState,
} from '@tanstack/react-table'
import { cn } from '@/lib/utils'

const QuoteList = () => {
  const navigate = useNavigate()
  const [deletingQuote, setDeletingQuote] = useState<Quote | null>(null)
  const [sorting, setSorting] = useState<SortingState>([])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({ companyId: false })

  // URL-backed filter state with nuqs - persists on refresh
  const [urlState, setUrlState] = useQueryStates(
    {
      search: parseAsString.withDefault(''),
      company: parseAsString.withDefault(''),
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(20),
    },
    { history: 'replace' }
  )

  const { data: quotes = [], isLoading, error, refetch } = useQuotes()
  const deleteQuote = useDeleteQuote()
  const duplicateQuote = useDuplicateQuote()
  const sendQuote = useSendQuote()
  const acceptQuote = useAcceptQuote()
  const rejectQuote = useRejectQuote()
  const convertQuoteToInvoice = useConvertQuoteToInvoice()
  const { data: customers = [] } = useCustomers()
  const { data: companies = [] } = useCompanies()
  
  // Companies is used in columnFilters for table filtering (setColumnFilters uses it)
  void companies

  const handleDelete = (quote: Quote) => {
    setDeletingQuote(quote)
  }

  const handleDeleteConfirm = async () => {
    if (deletingQuote) {
      try {
        await deleteQuote.mutateAsync(deletingQuote.id)
        setDeletingQuote(null)
      } catch (error) {
        console.error('Failed to delete quote:', error)
      }
    }
  }

  const handleDuplicate = async (quote: Quote) => {
    try {
      const newQuote = await duplicateQuote.mutateAsync(quote.id)
      // Navigate to edit page for the duplicated quote
      navigate(`/quotes/${newQuote.id}/edit`)
    } catch (error) {
      console.error('Failed to duplicate quote:', error)
    }
  }

  const handleSend = async (quote: Quote) => {
    try {
      await sendQuote.mutateAsync(quote.id)
    } catch (error) {
      console.error('Failed to send quote:', error)
    }
  }

  const handleAccept = async (quote: Quote) => {
    try {
      await acceptQuote.mutateAsync(quote.id)
    } catch (error) {
      console.error('Failed to accept quote:', error)
    }
  }

  const handleReject = async (quote: Quote) => {
    try {
      await rejectQuote.mutateAsync({ id: quote.id })
    } catch (error) {
      console.error('Failed to reject quote:', error)
    }
  }

  const handleConvertToInvoice = async (quote: Quote) => {
    try {
      await convertQuoteToInvoice.mutateAsync(quote.id)
    } catch (error) {
      console.error('Failed to convert quote to invoice:', error)
    }
  }

  // Helper functions to get related entity names
  const getCustomerName = (customerId?: string) => {
    if (!customerId) return '—'
    const customer = customers.find(c => c.id === customerId)
    return customer ? `${customer.name}${customer.companyName ? ` (${customer.companyName})` : ''}` : '—'
  }


  const getStatusBadgeColor = (status?: string) => {
    switch (status?.toLowerCase()) {
      case 'accepted':
        return 'bg-green-100 text-green-800'
      case 'sent':
        return 'bg-blue-100 text-blue-800'
      case 'viewed':
        return 'bg-purple-100 text-purple-800'
      case 'rejected':
        return 'bg-red-100 text-red-800'
      case 'expired':
        return 'bg-gray-100 text-gray-800'
      case 'cancelled':
        return 'bg-orange-100 text-orange-800'
      case 'draft':
      default:
        return 'bg-yellow-100 text-yellow-800'
    }
  }

  const formatCurrency = (amount: number, currency?: string) => {
    const currencySymbol = currency === 'EUR' ? '€' : currency === 'GBP' ? '£' : currency === 'INR' ? '₹' : '$'
    return `${currencySymbol}${amount.toFixed(2)}`
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString()
  }

  const getExpiryStatus = (expiryDate: string, status?: string) => {
    if (status === 'accepted' || status === 'rejected' || status === 'cancelled') {
      return null
    }
    const expiry = new Date(expiryDate)
    const now = new Date()
    const daysUntilExpiry = Math.ceil((expiry.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

    if (daysUntilExpiry < 0) return 'Expired'
    if (daysUntilExpiry <= 7) return `${daysUntilExpiry} days left`
    return null
  }

  const columns = useMemo<ColumnDef<Quote>[]>(() => [
    {
      accessorKey: 'quoteNumber',
      header: 'Quote',
      cell: ({ row }) => {
        const quote = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{quote.quoteNumber}</div>
            <div className="text-sm text-gray-500">
              {formatDate(quote.quoteDate)}
            </div>
            {quote.projectName && (
              <div className="text-sm text-blue-600">{quote.projectName}</div>
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
        return (
          <div className="text-sm text-gray-900">{customerName}</div>
        )
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const quote = row.original
        const status = quote.status || 'draft'
        const expiryStatus = getExpiryStatus(quote.expiryDate, status)

        return (
          <div>
            <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${getStatusBadgeColor(status)}`}>
              {status.charAt(0).toUpperCase() + status.slice(1)}
            </div>
            {expiryStatus && (
              <div className="text-xs text-orange-600 mt-1">{expiryStatus}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'expiryDate',
      header: 'Expiry Date',
      cell: ({ row }) => {
        const quote = row.original
        const expiryDate = new Date(quote.expiryDate)
        const isExpired = expiryDate < new Date() && quote.status !== 'accepted' && quote.status !== 'rejected'

        return (
          <div>
            <div className={`text-sm ${isExpired ? 'text-red-600 font-medium' : 'text-gray-900'}`}>
              {formatDate(quote.expiryDate)}
            </div>
            {isExpired && (
              <div className="text-xs text-red-500">Expired</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'totalAmount',
      header: 'Amount',
      cell: ({ row }) => {
        const quote = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">
              {formatCurrency(quote.totalAmount, quote.currency)}
            </div>
            {quote.discountAmount && quote.discountAmount > 0 && (
              <div className="text-sm text-green-600">
                Discount: {formatCurrency(quote.discountAmount, quote.currency)}
              </div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'companyId',
      header: 'Company (hidden)',
      cell: () => null,
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const quote = row.original
        const canSend = quote.status === 'draft'
        const canAccept = quote.status === 'sent' || quote.status === 'viewed'
        const canReject = quote.status === 'sent' || quote.status === 'viewed'
        const canConvert = quote.status === 'accepted'

        return (
          <div className="flex space-x-1">
            <button
              onClick={() => navigate(`/quotes/${quote.id}`)}
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="View quote"
            >
              <Eye size={16} />
            </button>
            <button
              onClick={() => navigate(`/quotes/${quote.id}/edit`)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit quote"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDuplicate(quote)}
              className="text-purple-600 hover:text-purple-800 p-1 rounded hover:bg-purple-50 transition-colors"
              title="Duplicate quote"
              disabled={duplicateQuote.isPending}
            >
              <Copy size={16} />
            </button>
            {canSend && (
              <button
                onClick={() => handleSend(quote)}
                className="text-indigo-600 hover:text-indigo-800 p-1 rounded hover:bg-indigo-50 transition-colors"
                title="Send quote"
                disabled={sendQuote.isPending}
              >
                <Send size={16} />
              </button>
            )}
            {canAccept && (
              <button
                onClick={() => handleAccept(quote)}
                className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                title="Accept quote"
                disabled={acceptQuote.isPending}
              >
                <CheckCircle size={16} />
              </button>
            )}
            {canReject && (
              <button
                onClick={() => handleReject(quote)}
                className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                title="Reject quote"
                disabled={rejectQuote.isPending}
              >
                <XCircle size={16} />
              </button>
            )}
            {canConvert && (
              <button
                onClick={() => handleConvertToInvoice(quote)}
                className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                title="Convert to invoice"
                disabled={convertQuoteToInvoice.isPending}
              >
                <FileText size={16} />
              </button>
            )}
            <button
              onClick={() => handleDelete(quote)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete quote"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ], [])

  const table = useReactTable({
    data: quotes,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onColumnVisibilityChange: setColumnVisibility,
    onGlobalFilterChange: (value) => setUrlState({ search: value || '', page: 1 }),
    globalFilterFn: 'includesString',
    state: {
      sorting,
      columnFilters,
      columnVisibility,
      globalFilter: urlState.search,
      pagination: {
        pageIndex: urlState.page - 1,
        pageSize: urlState.pageSize,
      },
    },
    onPaginationChange: (updater) => {
      const current = { pageIndex: urlState.page - 1, pageSize: urlState.pageSize }
      const next = typeof updater === 'function' ? updater(current) : updater
      setUrlState({ page: next.pageIndex + 1, pageSize: next.pageSize })
    },
  })

  const totals = useMemo(() => {
    const rows = table.getFilteredRowModel().rows
    const totalAmount = rows.reduce((sum, r) => sum + (r.original.totalAmount || 0), 0)
    const totalDiscount = rows.reduce((sum, r) => sum + (r.original.discountAmount || 0), 0)
    return { count: rows.length, totalAmount, totalDiscount }
  }, [table])

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
        <div className="text-red-600 mb-4">Failed to load quotes</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
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
          <h1 className="text-3xl font-bold text-gray-900">Quotes</h1>
          <p className="text-gray-600 mt-2">Manage all your quotes and estimates</p>
        </div>
        <button
          onClick={() => navigate('/quotes/new')}
          className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          Create Quote
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Quotes</div>
          <div className="text-2xl font-bold text-gray-900">{quotes.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Accepted</div>
          <div className="text-2xl font-bold text-green-600">
            {quotes.filter(q => q.status === 'accepted').length}
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Pending</div>
          <div className="text-2xl font-bold text-yellow-600">
            {quotes.filter(q => ['sent', 'viewed'].includes(q.status || '')).length}
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Value</div>
          <div className="text-2xl font-bold text-gray-900">
            ${quotes.reduce((sum, q) => sum + q.totalAmount, 0).toFixed(2)}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          {/* Search and Add Button */}
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center space-x-4">
              <CompanyFilterDropdown
                value={urlState.company}
                onChange={(val) => {
                  setUrlState({ company: val || '', page: 1 })
                  setColumnFilters((prev) => {
                    const rest = prev.filter(f => f.id !== 'companyId')
                    return val ? [...rest, { id: 'companyId', value: val }] : rest
                  })
                }}
              />

              <input
                placeholder="Search quotes..."
                value={urlState.search ?? ''}
                onChange={(event) => setUrlState({ search: event.target.value, page: 1 })}
                className="max-w-sm px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
          </div>

          {/* Table */}
          <div className="rounded-md border overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50">
                {table.getHeaderGroups().map((headerGroup) => (
                  <tr key={headerGroup.id}>
                    {headerGroup.headers.map((header) => (
                      <th
                        key={header.id}
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                        onClick={header.column.getToggleSortingHandler()}
                      >
                        <div className="flex items-center space-x-1">
                          <span>
                            {header.isPlaceholder
                              ? null
                              : flexRender(header.column.columnDef.header, header.getContext())}
                          </span>
                          <span className="text-gray-400">
                            {header.column.getIsSorted() === 'desc' ? '↓' :
                             header.column.getIsSorted() === 'asc' ? '↑' :
                             header.column.getCanSort() ? '↕' : null}
                          </span>
                        </div>
                      </th>
                    ))}
                  </tr>
                ))}
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {table.getRowModel().rows?.length ? (
                  table.getRowModel().rows.map((row) => (
                    <tr
                      key={row.id}
                      className="hover:bg-gray-50 transition-colors"
                    >
                      {row.getVisibleCells().map((cell) => (
                        <td key={cell.id} className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {flexRender(cell.column.columnDef.cell, cell.getContext())}
                        </td>
                      ))}
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={columns.length} className="px-6 py-12 text-center text-gray-500">
                      No results found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Totals */}
          <div className="mt-3 bg-gray-50 border border-gray-200 rounded-lg px-4 py-3 text-sm text-gray-800">
            <div className="flex flex-wrap gap-4">
              <div>Rows: <span className="font-semibold">{totals.count}</span></div>
              <div>Total Amount: <span className="font-semibold">{formatCurrency(totals.totalAmount)}</span></div>
              <div>Total Discount: <span className="font-semibold text-green-700">{formatCurrency(totals.totalDiscount)}</span></div>
            </div>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between mt-4">
            <div className="flex items-center space-x-2">
              <span className="text-sm text-gray-700">
                Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount()}
              </span>
              <span className="text-sm text-gray-500">
                ({table.getFilteredRowModel().rows.length} total rows)
              </span>
              <PageSizeSelect
                value={table.getState().pagination.pageSize}
                onChange={(size) => setUrlState({ pageSize: size, page: 1 })}
              />
            </div>

            <div className="flex items-center space-x-2">
              <button
                onClick={() => table.previousPage()}
                disabled={!table.getCanPreviousPage()}
                className={cn(
                  "px-3 py-1 rounded-md text-sm transition-colors",
                  table.getCanPreviousPage()
                    ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                    : "bg-gray-100 text-gray-400 cursor-not-allowed"
                )}
              >
                Previous
              </button>

              <div className="flex items-center space-x-1">
                {Array.from({ length: Math.min(5, table.getPageCount()) }, (_, i) => {
                  const pageIndex = table.getState().pagination.pageIndex;
                  const startPage = Math.max(0, pageIndex - 2);
                  const page = startPage + i;

                  if (page >= table.getPageCount()) return null;

                  return (
                    <button
                      key={page}
                      onClick={() => setUrlState({ page: page + 1 })}
                      className={cn(
                        "w-8 h-8 rounded text-sm transition-colors",
                        page === pageIndex
                          ? "bg-primary text-primary-foreground"
                          : "bg-gray-200 hover:bg-gray-300 text-gray-700"
                      )}
                    >
                      {page + 1}
                    </button>
                  );
                })}
              </div>

              <button
                onClick={() => table.nextPage()}
                disabled={!table.getCanNextPage()}
                className={cn(
                  "px-3 py-1 rounded-md text-sm transition-colors",
                  table.getCanNextPage()
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
        isOpen={!!deletingQuote}
        onClose={() => setDeletingQuote(null)}
        title="Delete Quote"
        size="sm"
      >
        {deletingQuote && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete quote <strong>{deletingQuote.quoteNumber}</strong>?
              This action cannot be undone and will also delete all associated quote items.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingQuote(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteQuote.isPending}
                className="px-4 py-2 text-sm font-medium text-primary-foreground bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteQuote.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default QuoteList

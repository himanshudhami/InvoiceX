import { useMemo, useState, useEffect } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQuotesPaged, useDeleteQuote, useDuplicateQuote, useSendQuote, useAcceptQuote, useRejectQuote, useConvertQuoteToInvoice } from '@/features/quotes/hooks'
import { useCustomers } from '@/features/customers/hooks'
import { useCompanyContext } from '@/contexts/CompanyContext'
import { Quote } from '@/services/api/types'
import { Modal } from '@/components/ui/Modal'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { CustomerSelect } from '@/components/ui/CustomerSelect'
import { useNavigate } from 'react-router-dom'
import { Eye, Edit, Trash2, Copy, Send, CheckCircle, XCircle, FileText } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'

const QuoteList = () => {
  const navigate = useNavigate()
  const [deletingQuote, setDeletingQuote] = useState<Quote | null>(null)

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
      party: parseAsString.withDefault(''),
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
  const { data, isLoading, error, refetch } = useQuotesPaged({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: debouncedSearchTerm || undefined,
    status: urlState.status || undefined,
    companyId: effectiveCompanyId || undefined,
    partyId: urlState.party || undefined,
  })

  const deleteQuote = useDeleteQuote()
  const duplicateQuote = useDuplicateQuote()
  const sendQuote = useSendQuote()
  const acceptQuote = useAcceptQuote()
  const rejectQuote = useRejectQuote()
  const convertQuoteToInvoice = useConvertQuoteToInvoice()
  // Scope customers to the same company filter used for quotes
  const { data: customers = [] } = useCustomers(effectiveCompanyId)

  // Extract items and pagination info from response
  const quotes = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

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

  const getCustomerName = (partyId?: string) => {
    if (!partyId) return '—'
    const customer = customers.find(c => c.id === partyId)
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

  const getExpiryStatus = (validUntil: string, status?: string) => {
    if (status === 'accepted' || status === 'rejected' || status === 'cancelled') {
      return null
    }
    const expiry = new Date(validUntil)
    const now = new Date()
    const daysUntilExpiry = Math.ceil((expiry.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

    if (daysUntilExpiry < 0) return 'Expired'
    if (daysUntilExpiry <= 7) return `${daysUntilExpiry} days left`
    return null
  }

  // Calculate totals from current page data
  const totals = useMemo(() => {
    const result = {
      totalAmount: 0,
      totalDiscount: 0,
    }

    quotes.forEach(quote => {
      result.totalAmount += quote.totalAmount || 0
      result.totalDiscount += quote.discountAmount || 0
    })

    return result
  }, [quotes])

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
          </div>
        )
      },
    },
    {
      accessorKey: 'partyId',
      header: 'Customer',
      cell: ({ row }) => {
        const customerName = getCustomerName(row.original.partyId)
        return <div className="text-sm text-gray-900">{customerName}</div>
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const quote = row.original
        const status = quote.status || 'draft'
        const expiryStatus = quote.validUntil ? getExpiryStatus(quote.validUntil, status) : null

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
      accessorKey: 'validUntil',
      header: 'Valid Until',
      cell: ({ row }) => {
        const quote = row.original
        const validUntil = quote.validUntil ? new Date(quote.validUntil) : null
        const isExpired = validUntil && validUntil < new Date() && quote.status !== 'accepted' && quote.status !== 'rejected'

        return (
          <div>
            <div className={`text-sm ${isExpired ? 'text-red-600 font-medium' : 'text-gray-900'}`}>
              {quote.validUntil ? formatDate(quote.validUntil) : '—'}
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
  ], [navigate, duplicateQuote.isPending, sendQuote.isPending, acceptQuote.isPending, rejectQuote.isPending, convertQuoteToInvoice.isPending, customers])

  const statusOptions = ['draft', 'sent', 'viewed', 'accepted', 'rejected', 'expired', 'cancelled']

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
          <div className="text-2xl font-bold text-gray-900">{totalCount}</div>
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
          <div className="text-sm font-medium text-gray-500">This Page Value</div>
          <div className="text-2xl font-bold text-gray-900">
            ${totals.totalAmount.toFixed(2)}
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

              <div className="flex items-center gap-2">
                <label className="text-sm font-medium text-gray-700">Customer</label>
                <CustomerSelect
                  customers={customers}
                  value={urlState.party}
                  onChange={(val) => setUrlState({ party: val, page: 1 })}
                  placeholder="All customers"
                  className="min-w-[220px]"
                  disabled={!effectiveCompanyId}
                  showAllOption
                  allOptionLabel="All customers"
                />
              </div>

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

              {(urlState.status || urlState.company || urlState.party) && (
                <button
                  onClick={() => setUrlState({ status: '', company: '', party: '', page: 1 })}
                  className="text-sm px-3 py-2 rounded-md border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  Clear filters
                </button>
              )}
            </div>

            <input
              placeholder="Search quotes..."
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
                {quotes.length > 0 ? (
                  quotes.map((quote) => (
                    <tr key={quote.id} className="hover:bg-gray-50 transition-colors">
                      {columns.map((column) => (
                        <td
                          key={`${quote.id}-${column.id || (column as any).accessorKey}`}
                          className="px-6 py-4 whitespace-nowrap text-sm text-gray-900"
                        >
                          {column.cell
                            ? (column.cell as any)({ row: { original: quote } })
                            : (quote as any)[(column as any).accessorKey]}
                        </td>
                      ))}
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={columns.length} className="px-6 py-12 text-center text-gray-500">
                      No quotes found.
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
                ({totalCount} total quotes)
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

import { useMemo, useEffect } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useInvoices, useDeleteInvoice, useDuplicateInvoice } from '@/features/invoices/hooks'
import { useCustomers } from '@/features/customers/hooks'
import { Invoice } from '@/services/api/types'
import { Modal } from '@/components/ui/Modal'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { useNavigate } from 'react-router-dom'
import { Eye, Edit, Trash2, Copy } from 'lucide-react'
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getPaginationRowModel,
  flexRender,
  SortingState,
  ColumnFiltersState,
  VisibilityState,
} from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { useQueryStates, parseAsString, parseAsArrayOf, parseAsInteger } from 'nuqs'
import { useState } from 'react'

const InvoiceList = () => {
  const navigate = useNavigate()
  const [deletingInvoice, setDeletingInvoice] = useState<Invoice | null>(null)
  const [sorting, setSorting] = useState<SortingState>([])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({ companyId: false })

  // URL-backed state with nuqs - persists filters on refresh
  const [urlState, setUrlState] = useQueryStates(
    {
      search: parseAsString.withDefault(''),
      companyId: parseAsString.withDefault(''),
      status: parseAsArrayOf(parseAsString).withDefault([]),
      created: parseAsString.withDefault(''),
      updated: parseAsString.withDefault(''),
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(20),
    },
    { history: 'replace' }
  )

  const { data: invoices = [], isLoading, error, refetch } = useInvoices()
  const deleteInvoice = useDeleteInvoice()
  const duplicateInvoice = useDuplicateInvoice()
  const { data: customers = [] } = useCustomers()
  // CompanyFilterDropdown uses useCompanies internally, no need to call it here

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

  const getMonthYearKey = (dateString?: string | null) => {
    if (!dateString) return null
    const date = new Date(dateString)
    if (Number.isNaN(date.getTime())) return null
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const year = date.getFullYear()
    return `${year}-${month}`
  }

  const formatMonthYearLabel = (value: string) => {
    const [year, month] = value.split('-').map(Number)
    if (!year || !month) return value
    return new Date(year, month - 1, 1).toLocaleString('default', {
      month: 'short',
      year: 'numeric',
    })
  }

  const monthYearOptions = useMemo(() => {
    const created = new Set<string>()
    const updated = new Set<string>()

    invoices.forEach((inv) => {
      const createdKey = getMonthYearKey(inv.createdAt)
      const updatedKey = getMonthYearKey(inv.updatedAt)
      if (createdKey) created.add(createdKey)
      if (updatedKey) updated.add(updatedKey)
    })

    const toOptions = (values: Set<string>) =>
      Array.from(values)
        .sort((a, b) => b.localeCompare(a))
        .map((value) => ({
          value,
          label: formatMonthYearLabel(value),
        }))

    return {
      created: toOptions(created),
      updated: toOptions(updated),
    }
  }, [invoices])

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString()
  }

  const getPaymentStatus = (totalAmount: number, paidAmount?: number) => {
    if (!paidAmount || paidAmount === 0) return 'Unpaid'
    if (paidAmount >= totalAmount) return 'Fully Paid'
    return 'Partially Paid'
  }

  // Toggle status filter using URL state
  const toggleStatusFilter = (status: string) => {
    const currentFilters = new Set(urlState.status)
    if (currentFilters.has(status)) {
      currentFilters.delete(status)
    } else {
      currentFilters.add(status)
    }
    setUrlState({ status: Array.from(currentFilters), page: 1 })
  }

  // Memoize statusFilters to prevent unnecessary recalculations
  const statusFilters = useMemo(() => new Set(urlState.status), [urlState.status])

  // Apply all filters manually (status, created, updated, companyId, search)
  const filteredInvoices = useMemo(() => {
    return invoices.filter((inv) => {
      // Status filter
      const invoiceStatus = (inv.status || 'draft').toLowerCase()
      const statusMatches = statusFilters.size === 0 || statusFilters.has(invoiceStatus)
      
      // Date filters
      const createdMatches =
        !urlState.created || getMonthYearKey(inv.createdAt) === urlState.created
      const updatedMatches =
        !urlState.updated || getMonthYearKey(inv.updatedAt) === urlState.updated
      
      // Company filter
      const companyMatches = !urlState.companyId || inv.companyId === urlState.companyId
      
      // Search filter
      const searchLower = urlState.search.toLowerCase()
      const searchMatches = !searchLower || 
        inv.invoiceNumber?.toLowerCase().includes(searchLower) ||
        inv.projectName?.toLowerCase().includes(searchLower) ||
        getCustomerName(inv.customerId).toLowerCase().includes(searchLower)
      
      return statusMatches && createdMatches && updatedMatches && companyMatches && searchMatches
    })
  }, [invoices, statusFilters, urlState.created, urlState.updated, urlState.companyId, urlState.search, customers])

  type InvoiceRow = Invoice & { customerName?: string }
  const tableData: InvoiceRow[] = useMemo(
    () =>
      filteredInvoices.map((inv) => ({
        ...inv,
        customerName: getCustomerName(inv.customerId),
      })),
    [filteredInvoices, customers],
  )

  const columns = useMemo<ColumnDef<InvoiceRow>[]>(() => [
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
      accessorKey: 'customerName',
      header: 'Customer',
      cell: ({ row }) => {
        const customerName = row.original.customerName || getCustomerName(row.original.customerId)
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
      accessorKey: 'companyId',
      header: 'Company (hidden)',
      cell: () => null,
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
  ], [navigate, duplicateInvoice.isPending])

  const table = useReactTable({
    data: tableData,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    // Removed getFilteredRowModel - we're doing all filtering manually in filteredInvoices
    getPaginationRowModel: getPaginationRowModel(),
    onSortingChange: setSorting,
    // Removed onColumnFiltersChange to prevent render loop - columnFilters is synced from URL state via useEffect
    onColumnVisibilityChange: setColumnVisibility,
    // Removed onGlobalFilterChange - search is handled manually in filteredInvoices
    state: {
      sorting,
      // columnFilters kept for React Table internal use but not used for filtering
      columnFilters,
      columnVisibility,
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
    manualPagination: false,
  })

  const visibleInvoices = table.getFilteredRowModel().rows.map((row) => row.original)

  const totals = useMemo(() => {
    const result = {
      subtotal: 0,
      taxAmount: 0,
      discountAmount: 0,
      totalAmount: 0,
      paidAmount: 0,
      outstanding: 0
    }

    visibleInvoices.forEach(invoice => {
      result.subtotal += invoice.subtotal || 0
      result.taxAmount += invoice.taxAmount || 0
      result.discountAmount += invoice.discountAmount || 0
      result.totalAmount += invoice.totalAmount || 0
      result.paidAmount += invoice.paidAmount || 0
    })

    result.outstanding = result.totalAmount - result.paidAmount

    return result
  }, [visibleInvoices])

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

      {/* Status filters */}
      <div className="flex flex-wrap gap-2 items-center">
        <span className="text-sm font-medium text-gray-700 mr-2">Filter by status:</span>
        {[
          { key: 'draft', label: 'Draft' },
          { key: 'sent', label: 'Sent' },
          { key: 'viewed', label: 'Viewed' },
          { key: 'overdue', label: 'Overdue' },
          { key: 'paid', label: 'Paid' },
          { key: 'cancelled', label: 'Cancelled' },
        ].map((option) => (
          <button
            key={option.key}
            onClick={() => toggleStatusFilter(option.key)}
            className={`px-3 py-1 rounded-full text-sm border transition-colors ${
              statusFilters.has(option.key)
                ? 'bg-primary text-primary-foreground border-primary shadow-sm'
                : 'bg-white text-gray-700 border-gray-200 hover:bg-gray-50 hover:border-gray-300'
            }`}
          >
            {option.label}
          </button>
        ))}

        {statusFilters.size > 0 && (
          <button
            onClick={() => setUrlState({ status: [], page: 1 })}
            className="px-3 py-1 rounded-full text-sm border border-red-200 bg-red-50 text-red-700 hover:bg-red-100 transition-colors ml-2"
          >
            Clear all ({statusFilters.size})
          </button>
        )}
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Invoices</div>
          <div className="text-2xl font-bold text-gray-900">{visibleInvoices.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Paid</div>
          <div className="text-2xl font-bold text-green-600">
            {visibleInvoices.filter(inv => (inv.status || '').toLowerCase() === 'paid').length}
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Overdue</div>
          <div className="text-2xl font-bold text-red-600">
            {visibleInvoices.filter(inv =>
              new Date(inv.dueDate) < new Date() && inv.status !== 'paid'
            ).length}
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Amount</div>
          <div className="text-2xl font-bold text-gray-900">
            ${visibleInvoices.reduce((sum, inv) => sum + inv.totalAmount, 0).toFixed(2)}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          {/* Search and Filters */}
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between mb-4">
            <div className="flex flex-wrap items-center gap-3">
              <CompanyFilterDropdown
                value={urlState.companyId}
                onChange={(val) => {
                  setUrlState({ companyId: val || '', page: 1 })
                }}
                className="min-w-[180px]"
              />

              <div className="flex items-center gap-2">
                <span className="text-sm font-medium text-gray-700">Created</span>
                <select
                  value={urlState.created}
                  onChange={(e) => setUrlState({ created: e.target.value || '', page: 1 })}
                  className="px-3 py-2 border border-gray-200 rounded-md bg-white text-sm shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent min-w-[180px]"
                >
                  <option value="">Any month</option>
                  {monthYearOptions.created.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex items-center gap-2">
                <span className="text-sm font-medium text-gray-700">Updated</span>
                <select
                  value={urlState.updated}
                  onChange={(e) => setUrlState({ updated: e.target.value || '', page: 1 })}
                  className="px-3 py-2 border border-gray-200 rounded-md bg-white text-sm shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent min-w-[180px]"
                >
                  <option value="">Any month</option>
                  {monthYearOptions.updated.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>

              {(urlState.created || urlState.updated) && (
                <button
                  onClick={() => setUrlState({ created: '', updated: '', page: 1 })}
                  className="text-sm px-3 py-2 rounded-md border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  Clear date filters
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
              {/* Totals Row */}
              {table.getFilteredRowModel().rows.length > 0 && (
                <tfoot className="bg-gray-100 border-t-2 border-gray-300">
                  <tr className="font-semibold">
                    <td className="px-6 py-4 text-sm text-gray-900">
                      Totals ({table.getFilteredRowModel().rows.length} invoices)
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900"></td>
                    <td className="px-6 py-4 text-sm text-gray-900">
                      <div className="text-xs text-gray-600">Paid: {formatCurrency(totals.paidAmount)}</div>
                      <div className="text-xs text-red-600">Outstanding: {formatCurrency(totals.outstanding)}</div>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900"></td>
                    <td className="px-6 py-4 text-sm text-gray-900">
                      <div className="font-bold">{formatCurrency(totals.totalAmount)}</div>
                      {totals.taxAmount > 0 && (
                        <div className="text-xs text-gray-600">Tax: {formatCurrency(totals.taxAmount)}</div>
                      )}
                      {totals.discountAmount > 0 && (
                        <div className="text-xs text-gray-600">Discount: {formatCurrency(totals.discountAmount)}</div>
                      )}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900"></td>
                  </tr>
                </tfoot>
              )}
            </table>
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

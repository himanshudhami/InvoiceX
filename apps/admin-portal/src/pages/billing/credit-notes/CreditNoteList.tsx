import { useMemo, useState, useEffect } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useCreditNotesPaged, useDeleteCreditNote, useIssueCreditNote, useCancelCreditNote } from '@/features/credit-notes/hooks'
import { useCompanyContext } from '@/contexts/CompanyContext'
import { CreditNote } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { useNavigate } from 'react-router-dom'
import { Eye, Trash2, CheckCircle, XCircle, ExternalLink } from 'lucide-react'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import { formatINR } from '@/lib/financialUtils'

const CreditNoteList = () => {
  const navigate = useNavigate()
  const [deletingCreditNote, setDeletingCreditNote] = useState<CreditNote | null>(null)
  const [issuingCreditNote, setIssuingCreditNote] = useState<CreditNote | null>(null)
  const [cancellingCreditNote, setCancellingCreditNote] = useState<CreditNote | null>(null)
  const [cancelReason, setCancelReason] = useState('')

  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext()

  const [urlState, setUrlState] = useQueryStates(
    {
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(100),
      search: parseAsString.withDefault(''),
      status: parseAsString.withDefault(''),
      company: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  const effectiveCompanyId = urlState.company || (hasMultiCompanyAccess ? selectedCompanyId : undefined)

  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState(urlState.search)

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(urlState.search)
    }, 300)
    return () => clearTimeout(timer)
  }, [urlState.search])

  const { data, isLoading, error, refetch } = useCreditNotesPaged({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: debouncedSearchTerm || undefined,
    status: urlState.status || undefined,
    companyId: effectiveCompanyId || undefined,
  })

  const deleteCreditNote = useDeleteCreditNote()
  const issueCreditNote = useIssueCreditNote()
  const cancelCreditNote = useCancelCreditNote()

  const creditNotes = data?.items ?? []
  const totalCount = data?.totalCount ?? 0

  const handleDelete = (creditNote: CreditNote) => {
    setDeletingCreditNote(creditNote)
  }

  const handleDeleteConfirm = async () => {
    if (deletingCreditNote) {
      try {
        await deleteCreditNote.mutateAsync(deletingCreditNote.id)
        setDeletingCreditNote(null)
      } catch (error) {
        console.error('Failed to delete credit note:', error)
      }
    }
  }

  const handleIssueClick = (creditNote: CreditNote) => {
    setIssuingCreditNote(creditNote)
  }

  const handleIssueConfirm = async () => {
    if (issuingCreditNote) {
      try {
        await issueCreditNote.mutateAsync(issuingCreditNote.id)
        setIssuingCreditNote(null)
      } catch (error) {
        console.error('Failed to issue credit note:', error)
      }
    }
  }

  const handleCancelClick = (creditNote: CreditNote) => {
    setCancellingCreditNote(creditNote)
    setCancelReason('')
  }

  const handleCancelConfirm = async () => {
    if (cancellingCreditNote && cancelReason) {
      try {
        await cancelCreditNote.mutateAsync({ id: cancellingCreditNote.id, reason: cancelReason })
        setCancellingCreditNote(null)
        setCancelReason('')
      } catch (error) {
        console.error('Failed to cancel credit note:', error)
      }
    }
  }

  const getStatusBadgeColor = (status?: string) => {
    switch (status?.toLowerCase()) {
      case 'issued':
        return 'bg-green-100 text-green-800'
      case 'cancelled':
        return 'bg-red-100 text-red-800'
      case 'draft':
      default:
        return 'bg-yellow-100 text-yellow-800'
    }
  }

  const formatDate = (dateString?: string) => {
    if (!dateString) return '—'
    return new Date(dateString).toLocaleDateString()
  }

  const totals = useMemo(() => {
    return creditNotes.reduce((acc, cn) => acc + (cn.totalAmount || 0), 0)
  }, [creditNotes])

  const columns = useMemo<ColumnDef<CreditNote>[]>(() => [
    {
      accessorKey: 'creditNoteNumber',
      header: 'Credit Note',
      cell: ({ row }) => {
        const cn = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{cn.creditNoteNumber}</div>
            <div className="text-sm text-gray-500">{formatDate(cn.creditNoteDate)}</div>
          </div>
        )
      },
    },
    {
      accessorKey: 'originalInvoiceNumber',
      header: 'Original Invoice',
      cell: ({ row }) => {
        const cn = row.original
        return (
          <button
            onClick={(e) => {
              e.stopPropagation()
              navigate(`/invoices/${cn.originalInvoiceId}`)
            }}
            className="text-blue-600 hover:text-blue-800 hover:underline flex items-center gap-1"
          >
            {cn.originalInvoiceNumber}
            <ExternalLink size={12} />
          </button>
        )
      },
    },
    {
      accessorKey: 'reason',
      header: 'Reason',
      cell: ({ row }) => {
        const reason = row.original.reason
        return (
          <div className="text-sm text-gray-900 capitalize">
            {reason?.replace(/_/g, ' ') || '—'}
          </div>
        )
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.original.status || 'draft'
        return (
          <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${getStatusBadgeColor(status)}`}>
            {status.charAt(0).toUpperCase() + status.slice(1)}
          </div>
        )
      },
    },
    {
      accessorKey: 'totalAmount',
      header: 'Amount',
      cell: ({ row }) => (
        <div className="font-medium text-gray-900">
          {formatINR(row.original.totalAmount)}
        </div>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const cn = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => navigate(`/credit-notes/${cn.id}`)}
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="View credit note"
            >
              <Eye size={16} />
            </button>
            {cn.status === 'draft' && (
              <>
                <button
                  onClick={() => handleIssueClick(cn)}
                  className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                  title="Issue credit note"
                  disabled={issueCreditNote.isPending}
                >
                  <CheckCircle size={16} />
                </button>
                <button
                  onClick={() => handleDelete(cn)}
                  className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                  title="Delete credit note"
                >
                  <Trash2 size={16} />
                </button>
              </>
            )}
            {cn.status === 'issued' && (
              <button
                onClick={() => handleCancelClick(cn)}
                className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                title="Cancel credit note"
              >
                <XCircle size={16} />
              </button>
            )}
          </div>
        )
      },
    },
  ], [navigate, issueCreditNote.isPending])

  const statusOptions = ['draft', 'issued', 'cancelled']

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load credit notes</div>
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
          <h1 className="text-3xl font-bold text-gray-900">Credit Notes</h1>
          <p className="text-gray-600 mt-2">Manage GST-compliant credit notes</p>
        </div>
        <button
          onClick={() => navigate('/invoices')}
          className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          Create from Invoice
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Credit Notes</div>
          <div className="text-2xl font-bold text-gray-900">{totalCount}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">This Page Total</div>
          <div className="text-2xl font-bold text-gray-900">{formatINR(totals)}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Page Count</div>
          <div className="text-2xl font-bold text-gray-900">{creditNotes.length}</div>
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

              {(urlState.status || urlState.company) && (
                <button
                  onClick={() => setUrlState({ status: '', company: '', page: 1 })}
                  className="text-sm px-3 py-2 rounded-md border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  Clear filters
                </button>
              )}
            </div>

            <input
              placeholder="Search credit notes..."
              value={urlState.search}
              onChange={(event) => setUrlState({ search: event.target.value, page: 1 })}
              className="w-full md:w-auto max-w-sm px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>

          {/* DataTable with server-side pagination */}
          <DataTable
            columns={columns}
            data={creditNotes}
            isLoading={isLoading}
            showToolbar={false}
            pagination={{
              pageIndex: urlState.page - 1,
              pageSize: urlState.pageSize,
              totalCount: totalCount,
              onPageChange: (pageIndex) => setUrlState({ page: pageIndex + 1 }),
              onPageSizeChange: (size) => setUrlState({ pageSize: size, page: 1 }),
            }}
            footer={() => (
              <tfoot className="bg-gray-100 border-t-2 border-gray-300 text-sm font-semibold text-gray-900">
                <tr>
                  <td className="px-6 py-4">
                    Page Totals ({creditNotes.length} credit notes)
                  </td>
                  <td className="px-6 py-4" colSpan={3}></td>
                  <td className="px-6 py-4 text-right">
                    {formatINR(totals)}
                  </td>
                  <td className="px-6 py-4"></td>
                </tr>
              </tfoot>
            )}
          />
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingCreditNote}
        onClose={() => setDeletingCreditNote(null)}
        title="Delete Credit Note"
        size="sm"
      >
        {deletingCreditNote && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete credit note <strong>{deletingCreditNote.creditNoteNumber}</strong>?
              This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingCreditNote(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteCreditNote.isPending}
                className="px-4 py-2 text-sm font-medium text-primary-foreground bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteCreditNote.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Issue Confirmation Modal */}
      <Modal
        isOpen={!!issuingCreditNote}
        onClose={() => setIssuingCreditNote(null)}
        title="Issue Credit Note"
        size="sm"
      >
        {issuingCreditNote && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to issue credit note <strong>{issuingCreditNote.creditNoteNumber}</strong>?
              This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setIssuingCreditNote(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleIssueConfirm}
                disabled={issueCreditNote.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
              >
                {issueCreditNote.isPending ? 'Issuing...' : 'Issue Credit Note'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Cancel Confirmation Modal */}
      <Modal
        isOpen={!!cancellingCreditNote}
        onClose={() => setCancellingCreditNote(null)}
        title="Cancel Credit Note"
        size="sm"
      >
        {cancellingCreditNote && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to cancel credit note <strong>{cancellingCreditNote.creditNoteNumber}</strong>?
            </p>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Cancellation Reason *
              </label>
              <input
                type="text"
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                placeholder="Enter reason for cancellation"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setCancellingCreditNote(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Close
              </button>
              <button
                onClick={handleCancelConfirm}
                disabled={cancelCreditNote.isPending || !cancelReason}
                className="px-4 py-2 text-sm font-medium text-primary-foreground bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {cancelCreditNote.isPending ? 'Cancelling...' : 'Cancel Credit Note'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default CreditNoteList

import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import {
  Eye,
  CheckCircle,
  XCircle,
  Wallet,
  Receipt,
  Clock,
  FileText,
  Download,
  User,
  Calendar,
  IndianRupee,
  Paperclip,
} from 'lucide-react'
import { useCompanyContext } from '@/contexts/CompanyContext'
import {
  useExpenseClaimsPaged,
  useExpenseClaim,
  useExpenseClaimAttachments,
  useApproveExpenseClaim,
  useRejectExpenseClaim,
  useReimburseExpenseClaim,
} from '@/hooks/api/useExpenseClaims'
import {
  ExpenseClaim,
  ExpenseClaimStatus,
  ExpenseClaimFilterParams,
  ExpenseAttachment,
} from '@/services/api/expenseClaimService'
import { fileService } from '@/services/api/fileService'
import { cn } from '@/lib/utils'

const statusColors: Record<string, { bg: string; text: string; icon: React.ReactNode }> = {
  draft: { bg: 'bg-gray-100', text: 'text-gray-800', icon: <FileText className="w-4 h-4" /> },
  submitted: { bg: 'bg-blue-100', text: 'text-blue-800', icon: <Clock className="w-4 h-4" /> },
  pending_approval: { bg: 'bg-yellow-100', text: 'text-yellow-800', icon: <Clock className="w-4 h-4" /> },
  approved: { bg: 'bg-green-100', text: 'text-green-800', icon: <CheckCircle className="w-4 h-4" /> },
  rejected: { bg: 'bg-red-100', text: 'text-red-800', icon: <XCircle className="w-4 h-4" /> },
  reimbursed: { bg: 'bg-purple-100', text: 'text-purple-800', icon: <Wallet className="w-4 h-4" /> },
  cancelled: { bg: 'bg-gray-100', text: 'text-gray-600', icon: <XCircle className="w-4 h-4" /> },
}

const statusLabels: Record<string, string> = {
  draft: 'Draft',
  submitted: 'Submitted',
  pending_approval: 'Pending Approval',
  approved: 'Approved',
  rejected: 'Rejected',
  reimbursed: 'Reimbursed',
  cancelled: 'Cancelled',
}

const ExpenseClaimsManagement = () => {
  const { selectedCompanyId, selectedCompany } = useCompanyContext()
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [viewingClaim, setViewingClaim] = useState<ExpenseClaim | null>(null)
  const [rejectingClaim, setRejectingClaim] = useState<ExpenseClaim | null>(null)
  const [reimbursingClaim, setReimbursingClaim] = useState<ExpenseClaim | null>(null)
  const [rejectionReason, setRejectionReason] = useState('')
  const [reimbursementReference, setReimbursementReference] = useState('')

  const filters: ExpenseClaimFilterParams = {
    companyId: selectedCompanyId || undefined,
    pageNumber,
    pageSize,
    searchTerm: searchTerm || undefined,
    status: statusFilter || undefined,
  }

  const { data, isLoading, error, refetch } = useExpenseClaimsPaged(filters)
  const approveClaim = useApproveExpenseClaim()
  const rejectClaim = useRejectExpenseClaim()
  const reimburseClaim = useReimburseExpenseClaim()

  const claims = data?.items || []
  const totalCount = data?.totalCount || 0

  const formatCurrency = (amount: number, currency = 'INR') => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency,
      maximumFractionDigits: 0,
    }).format(amount)
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '—'
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    })
  }

  const handleApprove = async (claim: ExpenseClaim) => {
    try {
      await approveClaim.mutateAsync(claim.id)
      refetch()
    } catch (error) {
      console.error('Failed to approve claim:', error)
    }
  }

  const handleReject = async () => {
    if (!rejectingClaim || !rejectionReason.trim()) return
    try {
      await rejectClaim.mutateAsync({
        id: rejectingClaim.id,
        data: { reason: rejectionReason },
      })
      setRejectingClaim(null)
      setRejectionReason('')
      refetch()
    } catch (error) {
      console.error('Failed to reject claim:', error)
    }
  }

  const handleReimburse = async () => {
    if (!reimbursingClaim) return
    try {
      await reimburseClaim.mutateAsync({
        id: reimbursingClaim.id,
        data: { reimbursementReference: reimbursementReference || undefined },
      })
      setReimbursingClaim(null)
      setReimbursementReference('')
      refetch()
    } catch (error) {
      console.error('Failed to reimburse claim:', error)
    }
  }

  const columns: ColumnDef<ExpenseClaim>[] = [
    {
      accessorKey: 'claimNumber',
      header: 'Claim',
      cell: ({ row }) => {
        const claim = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{claim.claimNumber}</div>
            <div className="text-sm text-gray-500 truncate max-w-[200px]">{claim.title}</div>
          </div>
        )
      },
    },
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => {
        const claim = row.original
        return (
          <div className="flex items-center space-x-2">
            <div className="p-1.5 bg-gray-100 rounded-full">
              <User className="w-4 h-4 text-gray-600" />
            </div>
            <span className="text-sm">{claim.employeeName || 'Unknown'}</span>
          </div>
        )
      },
    },
    {
      accessorKey: 'categoryName',
      header: 'Category',
      cell: ({ row }) => (
        <div className="flex items-center space-x-2">
          <Receipt className="w-4 h-4 text-gray-400" />
          <span className="text-sm">{row.original.categoryName || '—'}</span>
        </div>
      ),
    },
    {
      accessorKey: 'expenseDate',
      header: 'Date',
      cell: ({ row }) => (
        <div className="text-sm text-gray-600">
          {formatDate(row.original.expenseDate)}
        </div>
      ),
    },
    {
      accessorKey: 'amount',
      header: 'Amount',
      cell: ({ row }) => {
        const claim = row.original
        return (
          <div className="font-medium text-gray-900">
            {formatCurrency(claim.amount, claim.currency)}
          </div>
        )
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.original.status
        const config = statusColors[status] || statusColors.draft
        return (
          <span
            className={cn(
              'inline-flex items-center px-2.5 py-1 text-xs font-medium rounded-full gap-1',
              config.bg,
              config.text
            )}
          >
            {config.icon}
            {statusLabels[status] || status}
          </span>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const claim = row.original
        const canApprove = claim.status === ExpenseClaimStatus.Submitted ||
                          claim.status === ExpenseClaimStatus.PendingApproval
        const canReimburse = claim.status === ExpenseClaimStatus.Approved

        return (
          <div className="flex space-x-1">
            <button
              onClick={() => setViewingClaim(claim)}
              className="text-gray-600 hover:text-gray-800 p-1 rounded hover:bg-gray-100 transition-colors"
              title="View details"
            >
              <Eye size={16} />
            </button>
            {canApprove && (
              <>
                <button
                  onClick={() => handleApprove(claim)}
                  disabled={approveClaim.isPending}
                  className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors disabled:opacity-50"
                  title="Approve"
                >
                  <CheckCircle size={16} />
                </button>
                <button
                  onClick={() => setRejectingClaim(claim)}
                  className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                  title="Reject"
                >
                  <XCircle size={16} />
                </button>
              </>
            )}
            {canReimburse && (
              <button
                onClick={() => setReimbursingClaim(claim)}
                className="text-purple-600 hover:text-purple-800 p-1 rounded hover:bg-purple-50 transition-colors"
                title="Mark as reimbursed"
              >
                <Wallet size={16} />
              </button>
            )}
          </div>
        )
      },
    },
  ]

  if (!selectedCompanyId) {
    return (
      <div className="text-center py-12">
        <div className="text-gray-500">Please select a company to manage expense claims.</div>
      </div>
    )
  }

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
        <div className="text-red-600 mb-4">Failed to load expense claims</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  // Calculate stats
  const stats = {
    pending: claims.filter(c =>
      c.status === ExpenseClaimStatus.Submitted ||
      c.status === ExpenseClaimStatus.PendingApproval
    ).length,
    approved: claims.filter(c => c.status === ExpenseClaimStatus.Approved).length,
    totalPending: claims
      .filter(c =>
        c.status === ExpenseClaimStatus.Submitted ||
        c.status === ExpenseClaimStatus.PendingApproval
      )
      .reduce((sum, c) => sum + c.amount, 0),
    totalApproved: claims
      .filter(c => c.status === ExpenseClaimStatus.Approved)
      .reduce((sum, c) => sum + c.amount, 0),
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Expense Claims</h1>
        <p className="text-gray-600 mt-2">
          Review and manage expense claims for {selectedCompany?.name || 'your company'}
        </p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Claims</div>
          <div className="text-2xl font-bold text-gray-900">{totalCount}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Pending Approval</div>
          <div className="text-2xl font-bold text-yellow-600">{stats.pending}</div>
          <div className="text-sm text-gray-500">{formatCurrency(stats.totalPending)}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Approved (Awaiting Reimbursement)</div>
          <div className="text-2xl font-bold text-green-600">{stats.approved}</div>
          <div className="text-sm text-gray-500">{formatCurrency(stats.totalApproved)}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Page</div>
          <div className="text-2xl font-bold text-blue-600">{pageNumber}</div>
          <div className="text-sm text-gray-500">of {Math.ceil(totalCount / pageSize) || 1}</div>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="flex flex-wrap gap-4">
          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Search
            </label>
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value)
                setPageNumber(1)
              }}
              placeholder="Search by claim number or title..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div className="w-48">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Status
            </label>
            <select
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value)
                setPageNumber(1)
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">All Statuses</option>
              <option value="submitted">Submitted</option>
              <option value="pending_approval">Pending Approval</option>
              <option value="approved">Approved</option>
              <option value="rejected">Rejected</option>
              <option value="reimbursed">Reimbursed</option>
            </select>
          </div>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={claims}
            showToolbar={false}
            pagination={{
              pageIndex: pageNumber - 1,
              pageSize,
              totalCount,
              onPageChange: (page) => setPageNumber(page + 1),
              onPageSizeChange: (size) => {
                setPageSize(size)
                setPageNumber(1)
              },
            }}
          />
        </div>
      </div>

      {/* View Claim Drawer */}
      <Drawer
        isOpen={!!viewingClaim}
        onClose={() => setViewingClaim(null)}
        title="Expense Claim Details"
        size="lg"
      >
        {viewingClaim && (
          <ExpenseClaimDetail
            claimId={viewingClaim.id}
            onClose={() => setViewingClaim(null)}
            onApprove={() => {
              handleApprove(viewingClaim)
              setViewingClaim(null)
            }}
            onReject={() => {
              setRejectingClaim(viewingClaim)
              setViewingClaim(null)
            }}
            onReimburse={() => {
              setReimbursingClaim(viewingClaim)
              setViewingClaim(null)
            }}
          />
        )}
      </Drawer>

      {/* Reject Modal */}
      <Modal
        isOpen={!!rejectingClaim}
        onClose={() => {
          setRejectingClaim(null)
          setRejectionReason('')
        }}
        title="Reject Expense Claim"
        size="sm"
      >
        {rejectingClaim && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to reject claim{' '}
              <strong>{rejectingClaim.claimNumber}</strong>?
            </p>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Rejection Reason *
              </label>
              <textarea
                value={rejectionReason}
                onChange={(e) => setRejectionReason(e.target.value)}
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="Please provide a reason for rejection..."
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => {
                  setRejectingClaim(null)
                  setRejectionReason('')
                }}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleReject}
                disabled={!rejectionReason.trim() || rejectClaim.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {rejectClaim.isPending ? 'Rejecting...' : 'Reject Claim'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Reimburse Modal */}
      <Modal
        isOpen={!!reimbursingClaim}
        onClose={() => {
          setReimbursingClaim(null)
          setReimbursementReference('')
        }}
        title="Mark as Reimbursed"
        size="sm"
      >
        {reimbursingClaim && (
          <div className="space-y-4">
            <div className="bg-green-50 p-4 rounded-lg">
              <div className="text-sm text-green-800">
                Claim: <strong>{reimbursingClaim.claimNumber}</strong>
              </div>
              <div className="text-lg font-bold text-green-900 mt-1">
                {formatCurrency(reimbursingClaim.amount, reimbursingClaim.currency)}
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Payment Reference (Optional)
              </label>
              <input
                type="text"
                value={reimbursementReference}
                onChange={(e) => setReimbursementReference(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="e.g., Bank transaction ID, cheque number"
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => {
                  setReimbursingClaim(null)
                  setReimbursementReference('')
                }}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleReimburse}
                disabled={reimburseClaim.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-purple-600 border border-transparent rounded-md hover:bg-purple-700 disabled:opacity-50"
              >
                {reimburseClaim.isPending ? 'Processing...' : 'Confirm Reimbursement'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

// Detail Component
interface ExpenseClaimDetailProps {
  claimId: string
  onClose: () => void
  onApprove: () => void
  onReject: () => void
  onReimburse: () => void
}

const ExpenseClaimDetail = ({
  claimId,
  onClose,
  onApprove,
  onReject,
  onReimburse,
}: ExpenseClaimDetailProps) => {
  const { data: claim, isLoading } = useExpenseClaim(claimId)
  const { data: attachments = [] } = useExpenseClaimAttachments(claimId)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (!claim) {
    return <div className="text-center py-12 text-gray-500">Claim not found</div>
  }

  const formatCurrency = (amount: number, currency = 'INR') => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency,
      maximumFractionDigits: 0,
    }).format(amount)
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '—'
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    })
  }

  const canApprove = claim.status === ExpenseClaimStatus.Submitted ||
                    claim.status === ExpenseClaimStatus.PendingApproval
  const canReimburse = claim.status === ExpenseClaimStatus.Approved
  const statusConfig = statusColors[claim.status] || statusColors.draft

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">{claim.claimNumber}</h3>
          <p className="text-gray-600">{claim.title}</p>
        </div>
        <span
          className={cn(
            'inline-flex items-center px-3 py-1.5 text-sm font-medium rounded-full gap-1',
            statusConfig.bg,
            statusConfig.text
          )}
        >
          {statusConfig.icon}
          {statusLabels[claim.status] || claim.status}
        </span>
      </div>

      {/* Amount */}
      <div className="bg-gray-50 p-4 rounded-lg">
        <div className="text-sm text-gray-500">Amount</div>
        <div className="text-2xl font-bold text-gray-900">
          {formatCurrency(claim.amount, claim.currency)}
        </div>
      </div>

      {/* Details */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <div className="text-sm text-gray-500">Employee</div>
          <div className="font-medium">{claim.employeeName || 'Unknown'}</div>
        </div>
        <div>
          <div className="text-sm text-gray-500">Category</div>
          <div className="font-medium">{claim.categoryName || '—'}</div>
        </div>
        <div>
          <div className="text-sm text-gray-500">Expense Date</div>
          <div className="font-medium">{formatDate(claim.expenseDate)}</div>
        </div>
        <div>
          <div className="text-sm text-gray-500">Submitted</div>
          <div className="font-medium">{formatDate(claim.submittedAt)}</div>
        </div>
      </div>

      {/* Description */}
      {claim.description && (
        <div>
          <div className="text-sm text-gray-500 mb-1">Description</div>
          <p className="text-gray-700">{claim.description}</p>
        </div>
      )}

      {/* Rejection Reason */}
      {claim.status === ExpenseClaimStatus.Rejected && claim.rejectionReason && (
        <div className="bg-red-50 border border-red-200 p-4 rounded-lg">
          <div className="text-sm font-medium text-red-800">Rejection Reason</div>
          <p className="text-red-700 mt-1">{claim.rejectionReason}</p>
        </div>
      )}

      {/* Attachments */}
      {attachments.length > 0 && (
        <div>
          <div className="text-sm text-gray-500 mb-2">
            Attachments ({attachments.length})
          </div>
          <div className="space-y-2">
            {attachments.map((attachment) => (
              <div
                key={attachment.id}
                className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
              >
                <div className="flex items-center space-x-3">
                  <Paperclip className="w-4 h-4 text-gray-400" />
                  <span className="text-sm font-medium">{attachment.originalFilename}</span>
                </div>
                <a
                  href={fileService.getDownloadUrl(attachment.storagePath)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 hover:text-blue-800"
                >
                  <Download className="w-4 h-4" />
                </a>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex justify-end space-x-3 pt-4 border-t">
        <button
          onClick={onClose}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
        >
          Close
        </button>
        {canApprove && (
          <>
            <button
              onClick={onReject}
              className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-md hover:bg-red-700"
            >
              Reject
            </button>
            <button
              onClick={onApprove}
              className="px-4 py-2 text-sm font-medium text-white bg-green-600 rounded-md hover:bg-green-700"
            >
              Approve
            </button>
          </>
        )}
        {canReimburse && (
          <button
            onClick={onReimburse}
            className="px-4 py-2 text-sm font-medium text-white bg-purple-600 rounded-md hover:bg-purple-700"
          >
            Mark as Reimbursed
          </button>
        )}
      </div>
    </div>
  )
}

export default ExpenseClaimsManagement

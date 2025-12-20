import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  CheckCircle2,
  XCircle,
  Clock,
  Receipt,
  ChevronDown,
  ChevronUp,
  AlertCircle,
  Download,
  FileText,
  Image,
  File,
  Calendar,
  Tag,
  IndianRupee,
} from 'lucide-react'
import { managerExpenseApi, ExpenseClaim, ExpenseAttachment } from '@/api/expense'
import { PageHeader, EmptyState } from '@/components/layout'
import { Card, Badge, Button, PageLoader, Textarea } from '@/components/ui'
import { formatDate, formatCurrency } from '@/utils/format'

export function ExpenseApprovalsPage() {
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const { data: expenses, isLoading } = useQuery({
    queryKey: ['manager-pending-expenses'],
    queryFn: managerExpenseApi.getPendingExpenses,
  })

  if (isLoading) {
    return <PageLoader />
  }

  const pendingExpenses = expenses || []

  return (
    <div className="animate-fade-in">
      <PageHeader title="Expense Approvals" showBack />

      <div className="px-4 pb-4">
        {pendingExpenses.length === 0 ? (
          <EmptyState
            icon={<Receipt className="text-gray-400" size={48} />}
            title="No Pending Expenses"
            description="You have no expense claims pending your approval."
          />
        ) : (
          <div className="space-y-3">
            {pendingExpenses.map((expense) => (
              <ExpenseApprovalCard
                key={expense.id}
                expense={expense}
                isExpanded={expandedId === expense.id}
                onToggle={() => {
                  setExpandedId(expandedId === expense.id ? null : expense.id)
                }}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

interface ExpenseApprovalCardProps {
  expense: ExpenseClaim
  isExpanded: boolean
  onToggle: () => void
}

function ExpenseApprovalCard({ expense, isExpanded, onToggle }: ExpenseApprovalCardProps) {
  return (
    <Card className="overflow-hidden">
      {/* Header */}
      <button onClick={onToggle} className="w-full p-4 flex items-start justify-between text-left">
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-yellow-100">
            <Receipt className="w-5 h-5 text-yellow-600" />
          </div>
          <div>
            <h3 className="font-medium text-gray-900">{expense.title}</h3>
            <p className="text-sm text-gray-500">{expense.claimNumber}</p>
            <div className="flex items-center gap-2 text-xs text-gray-400 mt-1">
              <span>{expense.categoryName}</span>
              <span>•</span>
              <span>{formatDate(expense.expenseDate)}</span>
            </div>
          </div>
        </div>
        <div className="flex flex-col items-end gap-2">
          <span className="text-lg font-bold text-gray-900">
            {formatCurrency(expense.amount, expense.currency)}
          </span>
          <Badge variant="warning">Pending</Badge>
          {isExpanded ? (
            <ChevronUp size={20} className="text-gray-400" />
          ) : (
            <ChevronDown size={20} className="text-gray-400" />
          )}
        </div>
      </button>

      {/* Expanded Details */}
      {isExpanded && <ExpenseDetailView expense={expense} />}
    </Card>
  )
}

interface ExpenseDetailViewProps {
  expense: ExpenseClaim
}

function ExpenseDetailView({ expense }: ExpenseDetailViewProps) {
  const [comments, setComments] = useState('')
  const [rejectionReason, setRejectionReason] = useState('')
  const [showRejectForm, setShowRejectForm] = useState(false)
  const queryClient = useQueryClient()

  const { data: attachments, isLoading: isLoadingAttachments } = useQuery({
    queryKey: ['manager-expense-attachments', expense.id],
    queryFn: () => managerExpenseApi.getAttachments(expense.id),
  })

  const approveMutation = useMutation({
    mutationFn: () => managerExpenseApi.approve(expense.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['manager-pending-expenses'] })
      setComments('')
    },
  })

  const rejectMutation = useMutation({
    mutationFn: (reason: string) => managerExpenseApi.reject(expense.id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['manager-pending-expenses'] })
      setRejectionReason('')
      setShowRejectForm(false)
    },
  })

  const handleApprove = () => {
    approveMutation.mutate()
  }

  const handleReject = () => {
    if (!rejectionReason.trim()) {
      return
    }
    rejectMutation.mutate(rejectionReason)
  }

  const handleDownload = (attachment: ExpenseAttachment) => {
    const token = localStorage.getItem('portal_access_token')
    const baseUrl = import.meta.env.VITE_API_URL || '/api'
    // downloadUrl from backend is like '/api/files/download/...' - we need full URL with token
    const url = attachment.downloadUrl.startsWith('/api')
      ? `${baseUrl}${attachment.downloadUrl.substring(4)}?token=${token}`
      : `${baseUrl}${attachment.downloadUrl}?token=${token}`
    window.open(url, '_blank')
  }

  const getFileIcon = (mimeType: string) => {
    if (mimeType.startsWith('image/')) return <Image size={16} className="text-blue-500" />
    if (mimeType === 'application/pdf') return <FileText size={16} className="text-red-500" />
    return <File size={16} className="text-gray-500" />
  }

  return (
    <div className="border-t border-gray-100 p-4 space-y-4 bg-gray-50">
      {/* Expense Details */}
      <div className="bg-white rounded-lg p-3 space-y-3">
        <h4 className="text-sm font-medium text-gray-700">Expense Details</h4>

        {expense.description && (
          <p className="text-sm text-gray-600">{expense.description}</p>
        )}

        <div className="grid grid-cols-2 gap-3">
          <div className="flex items-center gap-2">
            <Tag size={14} className="text-gray-400" />
            <div>
              <p className="text-xs text-gray-500">Category</p>
              <p className="text-sm font-medium">{expense.categoryName}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Calendar size={14} className="text-gray-400" />
            <div>
              <p className="text-xs text-gray-500">Expense Date</p>
              <p className="text-sm font-medium">{formatDate(expense.expenseDate)}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <IndianRupee size={14} className="text-gray-400" />
            <div>
              <p className="text-xs text-gray-500">Amount</p>
              <p className="text-sm font-medium">
                {formatCurrency(expense.amount, expense.currency)}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Clock size={14} className="text-gray-400" />
            <div>
              <p className="text-xs text-gray-500">Submitted</p>
              <p className="text-sm font-medium">
                {expense.submittedAt ? formatDate(expense.submittedAt) : '-'}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Attachments */}
      <div className="bg-white rounded-lg p-3">
        <h4 className="text-sm font-medium text-gray-700 mb-2">Attachments</h4>
        {isLoadingAttachments ? (
          <div className="flex items-center justify-center py-4">
            <Clock className="animate-spin text-gray-400" size={20} />
          </div>
        ) : attachments && attachments.length > 0 ? (
          <div className="space-y-2">
            {attachments.map((attachment) => (
              <div
                key={attachment.id}
                className="flex items-center justify-between p-2 rounded-lg bg-gray-50 border border-gray-100"
              >
                <div className="flex items-center gap-2">
                  {getFileIcon(attachment.mimeType)}
                  <div>
                    <p className="text-sm text-gray-900 truncate max-w-[160px]">
                      {attachment.originalFilename}
                    </p>
                    <p className="text-xs text-gray-500">
                      {(attachment.fileSize / 1024).toFixed(1)} KB
                    </p>
                  </div>
                </div>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => handleDownload(attachment)}
                  className="text-primary-600"
                >
                  <Download size={14} />
                </Button>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-gray-500 text-center py-2">No attachments</p>
        )}
      </div>

      {/* Actions */}
      <div className="bg-white rounded-lg p-3 space-y-3">
        {!showRejectForm ? (
          <>
            <Textarea
              placeholder="Add a comment (optional)"
              value={comments}
              onChange={(e) => setComments(e.target.value)}
              rows={2}
            />
            <div className="flex gap-2">
              <Button
                onClick={handleApprove}
                disabled={approveMutation.isPending}
                className="flex-1 bg-green-600 hover:bg-green-700"
              >
                <CheckCircle2 size={18} className="mr-2" />
                {approveMutation.isPending ? 'Approving...' : 'Approve'}
              </Button>
              <Button
                variant="outline"
                onClick={() => setShowRejectForm(true)}
                className="flex-1 border-red-300 text-red-600 hover:bg-red-50"
              >
                <XCircle size={18} className="mr-2" />
                Reject
              </Button>
            </div>
          </>
        ) : (
          <>
            <div className="flex items-center gap-2 text-sm text-red-600 mb-2">
              <AlertCircle size={16} />
              <span>Please provide a reason for rejection</span>
            </div>
            <Textarea
              placeholder="Reason for rejection (required)"
              value={rejectionReason}
              onChange={(e) => setRejectionReason(e.target.value)}
              rows={3}
              className="border-red-200 focus:border-red-400"
            />
            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => {
                  setShowRejectForm(false)
                  setRejectionReason('')
                }}
                className="flex-1"
              >
                Cancel
              </Button>
              <Button
                onClick={handleReject}
                disabled={rejectMutation.isPending || !rejectionReason.trim()}
                className="flex-1 bg-red-600 hover:bg-red-700"
              >
                <XCircle size={18} className="mr-2" />
                {rejectMutation.isPending ? 'Rejecting...' : 'Confirm Reject'}
              </Button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}

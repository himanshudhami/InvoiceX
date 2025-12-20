import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  AlertCircle,
  Clock,
  CheckCircle,
  XCircle,
  Wallet,
  FileText,
  Image,
  File,
  Download,
  Trash2,
  Calendar,
  Tag,
} from 'lucide-react'
import { expenseApi, ExpenseAttachment } from '@/api/expense'
import { PageHeader } from '@/components/layout'
import { Card, Badge, Button, PageLoader } from '@/components/ui'
import { formatDate, formatCurrency } from '@/utils/format'

const statusConfig: Record<
  string,
  { color: string; icon: React.ReactNode; label: string }
> = {
  draft: { color: 'secondary', icon: <FileText size={14} />, label: 'Draft' },
  submitted: { color: 'info', icon: <Clock size={14} />, label: 'Submitted' },
  pending_approval: { color: 'warning', icon: <Clock size={14} />, label: 'Pending Approval' },
  approved: { color: 'success', icon: <CheckCircle size={14} />, label: 'Approved' },
  rejected: { color: 'destructive', icon: <XCircle size={14} />, label: 'Rejected' },
  reimbursed: { color: 'default', icon: <Wallet size={14} />, label: 'Reimbursed' },
  cancelled: { color: 'secondary', icon: <XCircle size={14} />, label: 'Cancelled' },
}

export function ExpenseDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const {
    data: expense,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['expense', id],
    queryFn: () => expenseApi.getById(id!),
    enabled: !!id,
  })

  const { data: attachments } = useQuery({
    queryKey: ['expense-attachments', id],
    queryFn: () => expenseApi.getAttachments(id!),
    enabled: !!id,
  })

  const cancelMutation = useMutation({
    mutationFn: () => expenseApi.cancel(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['expense', id] })
      queryClient.invalidateQueries({ queryKey: ['my-expenses'] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => expenseApi.delete(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-expenses'] })
      navigate('/expenses')
    },
  })

  if (isLoading) {
    return <PageLoader />
  }

  if (error || !expense) {
    return (
      <div className="animate-fade-in">
        <PageHeader title="Expense Detail" showBack />
        <div className="px-4 py-8 text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-500 mb-4" />
          <p className="text-red-600">Failed to load expense. Please try again.</p>
        </div>
      </div>
    )
  }

  const config = statusConfig[expense.status] || statusConfig.draft
  const canCancel = ['submitted', 'pending_approval'].includes(expense.status)
  const canDelete = expense.status === 'draft'

  const handleCancel = () => {
    if (confirm('Are you sure you want to cancel this expense claim?')) {
      cancelMutation.mutate()
    }
  }

  const handleDelete = () => {
    if (confirm('Are you sure you want to delete this expense claim? This cannot be undone.')) {
      deleteMutation.mutate()
    }
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
    if (mimeType.startsWith('image/')) return <Image size={20} className="text-blue-500" />
    if (mimeType === 'application/pdf') return <FileText size={20} className="text-red-500" />
    return <File size={20} className="text-gray-500" />
  }

  return (
    <div className="animate-fade-in">
      <PageHeader title="Expense Detail" showBack />

      <div className="px-4 py-4 space-y-4">
        {/* Status Card */}
        <Card className="p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-sm text-gray-500">{expense.claimNumber}</span>
            <Badge variant={config.color as any} className="flex items-center gap-1">
              {config.icon}
              {config.label}
            </Badge>
          </div>
          <h2 className="text-lg font-semibold text-gray-900">{expense.title}</h2>
          {expense.description && (
            <p className="text-sm text-gray-600 mt-1">{expense.description}</p>
          )}
        </Card>

        {/* Amount Card */}
        <Card className="p-4 bg-green-50 border-green-100">
          <div className="flex items-center justify-between">
            <span className="text-sm text-green-700">Amount</span>
            <span className="text-2xl font-bold text-green-800">
              {formatCurrency(expense.amount, expense.currency)}
            </span>
          </div>
        </Card>

        {/* Details Card */}
        <Card className="p-4">
          <h3 className="text-sm font-medium text-gray-700 mb-3">Details</h3>
          <div className="space-y-3">
            <div className="flex items-center gap-3">
              <Tag size={16} className="text-gray-400" />
              <div>
                <p className="text-xs text-gray-500">Category</p>
                <p className="text-sm font-medium">{expense.categoryName}</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <Calendar size={16} className="text-gray-400" />
              <div>
                <p className="text-xs text-gray-500">Expense Date</p>
                <p className="text-sm font-medium">{formatDate(expense.expenseDate)}</p>
              </div>
            </div>
            {expense.submittedAt && (
              <div className="flex items-center gap-3">
                <Clock size={16} className="text-gray-400" />
                <div>
                  <p className="text-xs text-gray-500">Submitted</p>
                  <p className="text-sm font-medium">{formatDate(expense.submittedAt)}</p>
                </div>
              </div>
            )}
          </div>
        </Card>

        {/* Approval Info */}
        {expense.status === 'approved' && expense.approvedAt && (
          <Card className="p-4 bg-green-50 border-green-100">
            <div className="flex items-center gap-2 mb-2">
              <CheckCircle size={16} className="text-green-600" />
              <span className="text-sm font-medium text-green-700">Approved</span>
            </div>
            <p className="text-sm text-green-600">
              Approved by {expense.approvedByName} on {formatDate(expense.approvedAt)}
            </p>
          </Card>
        )}

        {expense.status === 'rejected' && expense.rejectedAt && (
          <Card className="p-4 bg-red-50 border-red-100">
            <div className="flex items-center gap-2 mb-2">
              <XCircle size={16} className="text-red-600" />
              <span className="text-sm font-medium text-red-700">Rejected</span>
            </div>
            <p className="text-sm text-red-600">Rejected on {formatDate(expense.rejectedAt)}</p>
            {expense.rejectionReason && (
              <p className="mt-2 text-sm text-red-700 italic">"{expense.rejectionReason}"</p>
            )}
          </Card>
        )}

        {expense.status === 'reimbursed' && expense.reimbursedAt && (
          <Card className="p-4 bg-blue-50 border-blue-100">
            <div className="flex items-center gap-2 mb-2">
              <Wallet size={16} className="text-blue-600" />
              <span className="text-sm font-medium text-blue-700">Reimbursed</span>
            </div>
            <p className="text-sm text-blue-600">
              Reimbursed on {formatDate(expense.reimbursedAt)}
            </p>
            {expense.reimbursementReference && (
              <p className="text-sm text-blue-600 mt-1">
                Reference: {expense.reimbursementReference}
              </p>
            )}
          </Card>
        )}

        {/* Attachments */}
        {attachments && attachments.length > 0 && (
          <Card className="p-4">
            <h3 className="text-sm font-medium text-gray-700 mb-3">Attachments</h3>
            <div className="space-y-2">
              {attachments.map((attachment) => (
                <div
                  key={attachment.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-gray-50 border border-gray-100"
                >
                  <div className="flex items-center gap-3">
                    {getFileIcon(attachment.mimeType)}
                    <div>
                      <p className="text-sm font-medium text-gray-900 truncate max-w-[180px]">
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
                    <Download size={16} />
                  </Button>
                </div>
              ))}
            </div>
          </Card>
        )}

        {/* Actions */}
        {(canCancel || canDelete) && (
          <div className="pt-4 space-y-3">
            {canCancel && (
              <Button
                variant="outline"
                className="w-full border-red-300 text-red-600 hover:bg-red-50"
                onClick={handleCancel}
                disabled={cancelMutation.isPending}
              >
                {cancelMutation.isPending ? 'Cancelling...' : 'Cancel Expense'}
              </Button>
            )}
            {canDelete && (
              <Button
                variant="outline"
                className="w-full border-red-300 text-red-600 hover:bg-red-50"
                onClick={handleDelete}
                disabled={deleteMutation.isPending}
              >
                <Trash2 size={16} className="mr-2" />
                {deleteMutation.isPending ? 'Deleting...' : 'Delete Draft'}
              </Button>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

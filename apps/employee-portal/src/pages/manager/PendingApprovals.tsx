import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  CheckCircle2,
  XCircle,
  Clock,
  FileText,
  ChevronDown,
  ChevronUp,
  AlertCircle,
} from 'lucide-react'
import { usePendingApprovals, useApprovalDetail, useApprove, useReject } from '@/hooks'
import { PageHeader, EmptyState } from '@/components/layout'
import { Card, Badge, Button, PageLoader, getStatusBadgeVariant, Textarea } from '@/components/ui'
import { formatDate } from '@/utils/format'
import type { PendingApproval, ApprovalRequestStep } from '@/types'

export function PendingApprovalsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const selectedRequestId = searchParams.get('request')

  const { data: pendingApprovals, isLoading } = usePendingApprovals()

  if (isLoading) {
    return <PageLoader />
  }

  const approvals = pendingApprovals || []

  return (
    <div className="animate-fade-in">
      <PageHeader title="Pending Approvals" />

      <div className="px-4 pb-4">
        {approvals.length === 0 ? (
          <EmptyState
            icon={<CheckCircle2 className="text-gray-400" size={48} />}
            title="All Caught Up!"
            description="You have no pending approvals at the moment."
          />
        ) : (
          <div className="space-y-3">
            {approvals.map((approval) => (
              <ApprovalCard
                key={approval.requestId}
                approval={approval}
                isExpanded={selectedRequestId === approval.requestId}
                onToggle={() => {
                  if (selectedRequestId === approval.requestId) {
                    setSearchParams({})
                  } else {
                    setSearchParams({ request: approval.requestId })
                  }
                }}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

function ApprovalCard({
  approval,
  isExpanded,
  onToggle,
}: {
  approval: PendingApproval
  isExpanded: boolean
  onToggle: () => void
}) {
  return (
    <Card className="overflow-hidden">
      {/* Header */}
      <button
        onClick={onToggle}
        className="w-full p-4 flex items-start justify-between text-left"
      >
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-yellow-100">
            <Clock className="w-5 h-5 text-yellow-600" />
          </div>
          <div>
            <h3 className="font-medium text-gray-900">{approval.requestorName}</h3>
            <p className="text-sm text-gray-500">{approval.activityTitle}</p>
            <div className="flex items-center gap-2 text-xs text-gray-400 mt-1">
              <span className="capitalize">{approval.activityType}</span>
              <span>•</span>
              <span>Step {approval.stepOrder} of {approval.totalSteps}</span>
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant="warning">Pending</Badge>
          {isExpanded ? (
            <ChevronUp size={20} className="text-gray-400" />
          ) : (
            <ChevronDown size={20} className="text-gray-400" />
          )}
        </div>
      </button>

      {/* Expanded Details */}
      {isExpanded && (
        <ApprovalDetailView requestId={approval.requestId} />
      )}
    </Card>
  )
}

function ApprovalDetailView({ requestId }: { requestId: string }) {
  const [comments, setComments] = useState('')
  const [rejectionReason, setRejectionReason] = useState('')
  const [showRejectForm, setShowRejectForm] = useState(false)

  const { data: detail, isLoading } = useApprovalDetail(requestId)
  const approveMutation = useApprove()
  const rejectMutation = useReject()

  if (isLoading) {
    return (
      <div className="p-4 border-t border-gray-100 flex justify-center">
        <Clock className="animate-spin text-gray-400" size={24} />
      </div>
    )
  }

  if (!detail) {
    return null
  }

  const handleApprove = () => {
    approveMutation.mutate(
      { requestId, dto: { comments: comments || undefined } },
      {
        onSuccess: () => {
          setComments('')
        },
      }
    )
  }

  const handleReject = () => {
    if (!rejectionReason.trim()) {
      return
    }
    rejectMutation.mutate(
      { requestId, dto: { reason: rejectionReason } },
      {
        onSuccess: () => {
          setRejectionReason('')
          setShowRejectForm(false)
        },
      }
    )
  }

  return (
    <div className="border-t border-gray-100 p-4 space-y-4 bg-gray-50">
      {/* Activity Details */}
      <div className="bg-white rounded-lg p-3 space-y-2">
        <div className="flex items-center gap-2">
          <FileText size={16} className="text-gray-400" />
          <span className="text-sm font-medium text-gray-700">Request Details</span>
        </div>
        <div className="grid grid-cols-2 gap-2 text-sm">
          <div>
            <span className="text-gray-500">Requested by:</span>
            <p className="font-medium">{detail.requestorName}</p>
          </div>
          <div>
            <span className="text-gray-500">Type:</span>
            <p className="font-medium capitalize">{detail.activityType}</p>
          </div>
          <div>
            <span className="text-gray-500">Submitted:</span>
            <p className="font-medium">{formatDate(detail.createdAt)}</p>
          </div>
          <div>
            <span className="text-gray-500">Status:</span>
            <Badge variant={getStatusBadgeVariant(detail.status as any)} className="mt-1">
              {detail.status}
            </Badge>
          </div>
        </div>
      </div>

      {/* Approval Timeline */}
      <div className="bg-white rounded-lg p-3">
        <div className="flex items-center gap-2 mb-3">
          <Clock size={16} className="text-gray-400" />
          <span className="text-sm font-medium text-gray-700">Approval Progress</span>
        </div>
        <ApprovalTimeline steps={detail.steps} currentStep={detail.currentStep} />
      </div>

      {/* Actions */}
      {detail.status === 'in_progress' && (
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
      )}
    </div>
  )
}

function ApprovalTimeline({ steps, currentStep }: { steps: ApprovalRequestStep[]; currentStep: number }) {
  return (
    <div className="space-y-2">
      {steps.map((step, index) => {
        const isCompleted = step.status === 'approved'
        const isRejected = step.status === 'rejected'
        const isCurrent = step.stepOrder === currentStep && step.status === 'pending'

        return (
          <div key={step.id} className="flex items-start gap-3">
            <div className="flex flex-col items-center">
              <div
                className={`w-6 h-6 rounded-full flex items-center justify-center ${
                  isCompleted
                    ? 'bg-green-100 text-green-600'
                    : isRejected
                    ? 'bg-red-100 text-red-600'
                    : isCurrent
                    ? 'bg-yellow-100 text-yellow-600'
                    : 'bg-gray-100 text-gray-400'
                }`}
              >
                {isCompleted ? (
                  <CheckCircle2 size={14} />
                ) : isRejected ? (
                  <XCircle size={14} />
                ) : isCurrent ? (
                  <Clock size={14} />
                ) : (
                  <span className="text-xs">{step.stepOrder}</span>
                )}
              </div>
              {index < steps.length - 1 && (
                <div
                  className={`w-0.5 h-6 mt-1 ${
                    isCompleted ? 'bg-green-200' : 'bg-gray-200'
                  }`}
                />
              )}
            </div>
            <div className="flex-1 pb-3">
              <div className="flex items-center justify-between">
                <span
                  className={`text-sm font-medium ${
                    isCurrent ? 'text-yellow-700' : isCompleted ? 'text-green-700' : 'text-gray-600'
                  }`}
                >
                  {step.stepName}
                </span>
                {step.actionAt && (
                  <span className="text-xs text-gray-400">
                    {formatDate(step.actionAt)}
                  </span>
                )}
              </div>
              {step.assignedToName && (
                <p className="text-xs text-gray-500 mt-0.5">
                  {step.actionByName ? `${step.actionByName}` : `Assigned to: ${step.assignedToName}`}
                </p>
              )}
              {step.comments && (
                <p className="text-xs text-gray-500 mt-1 italic">"{step.comments}"</p>
              )}
            </div>
          </div>
        )
      })}
    </div>
  )
}

import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  CheckCircle2,
  XCircle,
  Clock,
  FileText,
  ChevronDown,
  ChevronUp,
  AlertCircle,
  Laptop,
  Package,
  Tag,
  MessageSquare,
  IndianRupee,
  Calendar,
} from 'lucide-react'
import { usePendingAssetApprovals, useApprovalDetail, useApprove, useReject } from '@/hooks'
import { managerApi } from '@/api'
import { PageHeader, EmptyState } from '@/components/layout'
import { Card, Badge, Button, PageLoader, getStatusBadgeVariant, Textarea } from '@/components/ui'
import { formatDate } from '@/utils/format'
import type { PendingApproval, ApprovalRequestStep, AssetRequestDetail } from '@/types'

export function AssetApprovalsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const selectedRequestId = searchParams.get('request')

  const { data: pendingApprovals, isLoading } = usePendingAssetApprovals()

  if (isLoading) {
    return <PageLoader />
  }

  const approvals = pendingApprovals || []

  return (
    <div className="animate-fade-in">
      <PageHeader title="Asset Request Approvals" showBack />

      <div className="px-4 pb-4">
        {approvals.length === 0 ? (
          <EmptyState
            icon={<Package className="text-gray-400" size={48} />}
            title="No Pending Asset Requests"
            description="You have no asset requests waiting for approval."
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
      <button
        onClick={onToggle}
        className="w-full p-4 flex items-start justify-between text-left"
      >
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-blue-100">
            <Laptop className="w-5 h-5 text-blue-600" />
          </div>
          <div>
            <h3 className="font-medium text-gray-900">{approval.requestorName}</h3>
            <p className="text-sm text-gray-500">{approval.activityTitle}</p>
            <div className="flex items-center gap-2 text-xs text-gray-400 mt-1">
              {approval.requestorDepartment && (
                <>
                  <span>{approval.requestorDepartment}</span>
                  <span>•</span>
                </>
              )}
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

      {isExpanded && (
        <ApprovalDetailView requestId={approval.requestId} />
      )}
    </Card>
  )
}

function getPriorityVariant(priority: string): 'success' | 'warning' | 'error' | 'default' {
  switch (priority) {
    case 'urgent':
      return 'error'
    case 'high':
      return 'warning'
    case 'normal':
      return 'default'
    case 'low':
      return 'success'
    default:
      return 'default'
  }
}

function ApprovalDetailView({ requestId }: { requestId: string }) {
  const [comments, setComments] = useState('')
  const [rejectionReason, setRejectionReason] = useState('')
  const [showRejectForm, setShowRejectForm] = useState(false)

  const { data: detail, isLoading } = useApprovalDetail(requestId)
  const approveMutation = useApprove()
  const rejectMutation = useReject()

  // Fetch asset request details using the activityId
  const { data: assetRequest, isLoading: isLoadingAsset } = useQuery<AssetRequestDetail>({
    queryKey: ['manager-asset-request', detail?.activityId],
    queryFn: () => managerApi.getAssetRequestDetails(detail!.activityId),
    enabled: !!detail?.activityId && detail?.activityType === 'asset_request',
  })

  if (isLoading || isLoadingAsset) {
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
      {/* Asset Request Details */}
      {assetRequest && (
        <div className="bg-white rounded-lg p-3 space-y-3">
          <div className="flex items-center gap-2">
            <Package size={16} className="text-blue-500" />
            <span className="text-sm font-medium text-gray-700">Asset Request Details</span>
          </div>

          {/* Title and Type */}
          <div className="border-b border-gray-100 pb-3">
            <h4 className="font-medium text-gray-900">{assetRequest.title}</h4>
            <div className="flex items-center gap-2 mt-1">
              <Tag size={14} className="text-gray-400" />
              <span className="text-sm text-gray-600">
                {assetRequest.assetType} • {assetRequest.category}
              </span>
            </div>
            <div className="flex items-center gap-2 mt-2">
              <Badge variant={getPriorityVariant(assetRequest.priority)} className="text-xs">
                {assetRequest.priority} priority
              </Badge>
              {assetRequest.quantity > 1 && (
                <Badge variant="default" className="text-xs">
                  Qty: {assetRequest.quantity}
                </Badge>
              )}
            </div>
          </div>

          {/* Description */}
          {assetRequest.description && (
            <div>
              <div className="flex items-center gap-2 mb-1">
                <FileText size={14} className="text-gray-400" />
                <span className="text-xs text-gray-500">Description</span>
              </div>
              <p className="text-sm text-gray-700">{assetRequest.description}</p>
            </div>
          )}

          {/* Justification */}
          {assetRequest.justification && (
            <div>
              <div className="flex items-center gap-2 mb-1">
                <MessageSquare size={14} className="text-gray-400" />
                <span className="text-xs text-gray-500">Justification</span>
              </div>
              <p className="text-sm text-gray-700">{assetRequest.justification}</p>
            </div>
          )}

          {/* Specifications */}
          {assetRequest.specifications && (
            <div>
              <div className="flex items-center gap-2 mb-1">
                <Laptop size={14} className="text-gray-400" />
                <span className="text-xs text-gray-500">Specifications</span>
              </div>
              <p className="text-sm text-gray-700">{assetRequest.specifications}</p>
            </div>
          )}

          {/* Budget and Date */}
          <div className="grid grid-cols-2 gap-3 pt-2 border-t border-gray-100">
            {assetRequest.estimatedBudget && (
              <div>
                <div className="flex items-center gap-1 mb-1">
                  <IndianRupee size={14} className="text-gray-400" />
                  <span className="text-xs text-gray-500">Est. Budget</span>
                </div>
                <p className="text-sm font-medium text-gray-900">
                  ₹{assetRequest.estimatedBudget.toLocaleString()}
                </p>
              </div>
            )}
            {assetRequest.requestedByDate && (
              <div>
                <div className="flex items-center gap-1 mb-1">
                  <Calendar size={14} className="text-gray-400" />
                  <span className="text-xs text-gray-500">Needed By</span>
                </div>
                <p className="text-sm font-medium text-gray-900">
                  {formatDate(assetRequest.requestedByDate)}
                </p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Request Info */}
      <div className="bg-white rounded-lg p-3 space-y-2">
        <div className="flex items-center gap-2">
          <FileText size={16} className="text-gray-400" />
          <span className="text-sm font-medium text-gray-700">Request Info</span>
        </div>
        <div className="grid grid-cols-2 gap-2 text-sm">
          <div>
            <span className="text-gray-500">Requested by:</span>
            <p className="font-medium">{detail.requestorName}</p>
          </div>
          <div>
            <span className="text-gray-500">Submitted:</span>
            <p className="font-medium">{formatDate(detail.createdAt)}</p>
          </div>
          <div className="col-span-2">
            <span className="text-gray-500">Status:</span>
            <Badge variant={getStatusBadgeVariant(detail.status as any)} className="ml-2">
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

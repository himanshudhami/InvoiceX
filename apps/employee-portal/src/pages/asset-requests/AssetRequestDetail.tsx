import React, { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Calendar,
  Clock,
  User,
  MessageSquare,
  AlertTriangle,
  Laptop,
  Tag,
  FileText,
  CheckCircle,
  Package,
  IndianRupee,
} from 'lucide-react'
import * as Dialog from '@radix-ui/react-dialog'
import { assetRequestApi } from '@/api'
import { PageHeader } from '@/components/layout'
import { Card, Badge, Button, Textarea, PageLoader } from '@/components/ui'
import { formatDate, formatDateTime } from '@/utils/format'
import type { AssetRequestDetail, AssetRequestStatus } from '@/types'

function getRequestStatusVariant(status: AssetRequestStatus): 'success' | 'warning' | 'error' | 'default' | 'info' {
  switch (status) {
    case 'pending':
    case 'in_progress':
      return 'warning'
    case 'approved':
      return 'info'
    case 'fulfilled':
      return 'success'
    case 'rejected':
    case 'cancelled':
      return 'error'
    default:
      return 'default'
  }
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

export function AssetRequestDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [showWithdrawDialog, setShowWithdrawDialog] = useState(false)
  const [withdrawReason, setWithdrawReason] = useState('')

  const { data: request, isLoading } = useQuery<AssetRequestDetail>({
    queryKey: ['asset-request', id],
    queryFn: () => assetRequestApi.getRequestById(id!),
    enabled: !!id,
  })

  const withdrawMutation = useMutation({
    mutationFn: () => assetRequestApi.withdrawRequest(id!, withdrawReason || undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['asset-request', id] })
      queryClient.invalidateQueries({ queryKey: ['my-asset-requests'] })
      queryClient.invalidateQueries({ queryKey: ['portal-dashboard'] })
      setShowWithdrawDialog(false)
      navigate('/assets')
    },
  })

  if (isLoading || !request) {
    return <PageLoader />
  }

  const canWithdraw = request.canCancel
  const isRejected = request.status === 'rejected'
  const isFulfilled = request.status === 'fulfilled'
  const isApproved = request.status === 'approved'

  return (
    <div className="animate-fade-in">
      <PageHeader title="Asset Request" showBack />

      <div className="px-4 py-4 space-y-4">
        {/* Status Banner */}
        <Card className="p-4">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold text-gray-900">{request.title}</h2>
            <Badge variant={getRequestStatusVariant(request.status)} className="text-sm">
              {request.status.replace('_', ' ')}
            </Badge>
          </div>
          <div className="flex items-center gap-2 text-sm text-gray-600">
            <Tag size={16} />
            <span>{request.assetType} • {request.category}</span>
          </div>
          <div className="flex items-center gap-2 mt-2">
            <Badge variant={getPriorityVariant(request.priority)} className="text-xs">
              {request.priority} priority
            </Badge>
            {request.quantity > 1 && (
              <Badge variant="default" className="text-xs">
                Qty: {request.quantity}
              </Badge>
            )}
          </div>
        </Card>

        {/* Approval Progress */}
        {request.hasApprovalWorkflow && request.totalApprovalSteps && (
          <Card className="p-4">
            <p className="text-sm font-medium text-gray-700 mb-2">Approval Progress</p>
            <div className="flex items-center gap-2">
              <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
                <div
                  className="h-full bg-primary-500 rounded-full transition-all"
                  style={{
                    width: `${((request.currentApprovalStep || 0) / request.totalApprovalSteps) * 100}%`,
                  }}
                />
              </div>
              <span className="text-xs text-gray-500">
                Step {request.currentApprovalStep || 0} of {request.totalApprovalSteps}
              </span>
            </div>
          </Card>
        )}

        {/* Rejection Reason */}
        {isRejected && request.rejectionReason && (
          <Card className="p-4 bg-red-50 border-red-200">
            <div className="flex items-start gap-3">
              <AlertTriangle className="text-red-500 flex-shrink-0 mt-0.5" size={18} />
              <div>
                <p className="text-sm font-medium text-red-700">Rejection Reason</p>
                <p className="text-sm text-red-600 mt-1">{request.rejectionReason}</p>
              </div>
            </div>
          </Card>
        )}

        {/* Fulfillment Info */}
        {isFulfilled && request.assignedAssetName && (
          <Card className="p-4 bg-green-50 border-green-200">
            <div className="flex items-start gap-3">
              <CheckCircle className="text-green-500 flex-shrink-0 mt-0.5" size={18} />
              <div>
                <p className="text-sm font-medium text-green-700">Asset Assigned</p>
                <p className="text-sm text-green-600 mt-1">{request.assignedAssetName}</p>
                {request.fulfilledByName && request.fulfilledAt && (
                  <p className="text-xs text-green-500 mt-1">
                    By {request.fulfilledByName} on {formatDateTime(request.fulfilledAt)}
                  </p>
                )}
                {request.fulfillmentNotes && (
                  <p className="text-sm text-green-600 mt-2 pt-2 border-t border-green-200">
                    {request.fulfillmentNotes}
                  </p>
                )}
              </div>
            </div>
          </Card>
        )}

        {/* Pending Fulfillment */}
        {isApproved && !isFulfilled && (
          <Card className="p-4 bg-blue-50 border-blue-200">
            <div className="flex items-start gap-3">
              <Package className="text-blue-500 flex-shrink-0 mt-0.5" size={18} />
              <div>
                <p className="text-sm font-medium text-blue-700">Approved - Pending Fulfillment</p>
                <p className="text-sm text-blue-600 mt-1">
                  Your request has been approved and is waiting to be fulfilled.
                </p>
              </div>
            </div>
          </Card>
        )}

        {/* Details */}
        <Card className="divide-y divide-gray-100">
          <DetailRow
            icon={<Calendar size={16} />}
            label="Requested On"
            value={formatDateTime(request.requestedAt)}
          />

          {request.description && (
            <DetailRow icon={<FileText size={16} />} label="Description" value={request.description} />
          )}

          {request.justification && (
            <DetailRow icon={<MessageSquare size={16} />} label="Justification" value={request.justification} />
          )}

          {request.specifications && (
            <DetailRow icon={<Laptop size={16} />} label="Specifications" value={request.specifications} />
          )}

          {request.estimatedBudget && (
            <DetailRow
              icon={<IndianRupee size={16} />}
              label="Estimated Budget"
              value={`₹${request.estimatedBudget.toLocaleString()}`}
            />
          )}

          {request.requestedByDate && (
            <DetailRow
              icon={<Clock size={16} />}
              label="Needed By"
              value={formatDate(request.requestedByDate)}
            />
          )}

          {request.approvedByName && (
            <DetailRow
              icon={<User size={16} />}
              label={request.status === 'approved' || request.status === 'fulfilled' ? 'Approved By' : 'Reviewed By'}
              value={`${request.approvedByName}${request.approvedAt ? ` on ${formatDateTime(request.approvedAt)}` : ''}`}
            />
          )}

          {request.cancellationReason && (
            <DetailRow
              icon={<MessageSquare size={16} />}
              label="Cancellation Reason"
              value={request.cancellationReason}
            />
          )}
        </Card>

        {/* Actions */}
        {canWithdraw && (
          <div className="pt-2">
            <Button
              variant="outline"
              className="w-full text-red-600 border-red-200 hover:bg-red-50"
              onClick={() => setShowWithdrawDialog(true)}
            >
              Withdraw Request
            </Button>
          </div>
        )}
      </div>

      {/* Withdraw Dialog */}
      <Dialog.Root open={showWithdrawDialog} onOpenChange={setShowWithdrawDialog}>
        <Dialog.Portal>
          <Dialog.Overlay className="fixed inset-0 bg-black/50 z-50" />
          <Dialog.Content className="fixed bottom-0 left-0 right-0 bg-white rounded-t-2xl p-6 z-50 animate-slide-up safe-bottom">
            <Dialog.Title className="text-lg font-semibold text-gray-900 mb-2">
              Withdraw Request?
            </Dialog.Title>
            <Dialog.Description className="text-sm text-gray-500 mb-4">
              Are you sure you want to withdraw this asset request?
            </Dialog.Description>

            <Textarea
              label="Reason (Optional)"
              placeholder="Enter reason..."
              value={withdrawReason}
              onChange={(e) => setWithdrawReason(e.target.value)}
              rows={2}
            />

            <div className="flex gap-3 mt-4">
              <Button
                variant="outline"
                className="flex-1"
                onClick={() => setShowWithdrawDialog(false)}
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                className="flex-1"
                isLoading={withdrawMutation.isPending}
                onClick={() => withdrawMutation.mutate()}
              >
                Withdraw
              </Button>
            </div>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>
    </div>
  )
}

interface DetailRowProps {
  icon: React.ReactNode
  label: string
  value: string
}

function DetailRow({ icon, label, value }: DetailRowProps) {
  return (
    <div className="flex items-start gap-3 py-3 px-4">
      <div className="flex items-center justify-center w-8 h-8 rounded-full bg-gray-50 flex-shrink-0 text-gray-500">
        {icon}
      </div>
      <div>
        <p className="text-xs text-gray-500">{label}</p>
        <p className="text-sm text-gray-900 mt-0.5">{value}</p>
      </div>
    </div>
  )
}

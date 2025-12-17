import React, { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Calendar, Clock, User, MessageSquare, AlertTriangle } from 'lucide-react'
import * as Dialog from '@radix-ui/react-dialog'
import { leaveApi } from '@/api'
import { PageHeader } from '@/components/layout'
import { Card, Badge, Button, Textarea, PageLoader, getStatusBadgeVariant } from '@/components/ui'
import { formatDate, formatDateTime, formatDays } from '@/utils/format'
import type { LeaveApplicationDetail } from '@/types'

export function LeaveApplicationDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [showWithdrawDialog, setShowWithdrawDialog] = useState(false)
  const [withdrawReason, setWithdrawReason] = useState('')

  const { data: application, isLoading } = useQuery<LeaveApplicationDetail>({
    queryKey: ['leave-application', id],
    queryFn: () => leaveApi.getApplicationDetail(id!),
    enabled: !!id,
  })

  const withdrawMutation = useMutation({
    mutationFn: () => leaveApi.withdrawApplication(id!, withdrawReason || undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leave-application', id] })
      queryClient.invalidateQueries({ queryKey: ['leave-dashboard'] })
      queryClient.invalidateQueries({ queryKey: ['leave-applications'] })
      setShowWithdrawDialog(false)
      navigate('/leave')
    },
  })

  if (isLoading || !application) {
    return <PageLoader />
  }

  const canWithdraw = application.status === 'pending' || application.status === 'approved'
  const isPending = application.status === 'pending'
  const isRejected = application.status === 'rejected'

  return (
    <div className="animate-fade-in">
      <PageHeader title="Leave Application" showBack />

      <div className="px-4 py-4 space-y-4">
        {/* Status Banner */}
        <Card className="p-4">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold text-gray-900">{application.leaveTypeName}</h2>
            <Badge variant={getStatusBadgeVariant(application.status)} className="text-sm">
              {application.status}
            </Badge>
          </div>
          <div className="flex items-center gap-2 text-sm text-gray-600">
            <Calendar size={16} />
            <span>
              {formatDate(application.fromDate)} - {formatDate(application.toDate)}
            </span>
          </div>
          <div className="flex items-center gap-2 text-sm text-gray-600 mt-1">
            <Clock size={16} />
            <span>{formatDays(application.totalDays)}</span>
            {(application.isHalfDayStart || application.isHalfDayEnd) && (
              <span className="text-xs text-gray-400">
                ({application.isHalfDayStart && 'Half day start'}
                {application.isHalfDayStart && application.isHalfDayEnd && ', '}
                {application.isHalfDayEnd && 'Half day end'})
              </span>
            )}
          </div>
        </Card>

        {/* Rejection Reason */}
        {isRejected && application.rejectionReason && (
          <Card className="p-4 bg-red-50 border-red-200">
            <div className="flex items-start gap-3">
              <AlertTriangle className="text-red-500 flex-shrink-0 mt-0.5" size={18} />
              <div>
                <p className="text-sm font-medium text-red-700">Rejection Reason</p>
                <p className="text-sm text-red-600 mt-1">{application.rejectionReason}</p>
              </div>
            </div>
          </Card>
        )}

        {/* Details */}
        <Card className="divide-y divide-gray-100">
          <DetailRow icon={<Calendar size={16} />} label="Applied On" value={formatDateTime(application.appliedAt)} />

          {application.reason && (
            <DetailRow icon={<MessageSquare size={16} />} label="Reason" value={application.reason} />
          )}

          {application.approvedByName && (
            <DetailRow
              icon={<User size={16} />}
              label={application.status === 'approved' ? 'Approved By' : 'Reviewed By'}
              value={`${application.approvedByName}${application.approvedAt ? ` on ${formatDateTime(application.approvedAt)}` : ''}`}
            />
          )}

          {application.cancellationReason && (
            <DetailRow
              icon={<MessageSquare size={16} />}
              label="Cancellation Reason"
              value={application.cancellationReason}
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
              {isPending ? 'Withdraw Application' : 'Cancel Leave'}
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
              {isPending ? 'Withdraw Application?' : 'Cancel Leave?'}
            </Dialog.Title>
            <Dialog.Description className="text-sm text-gray-500 mb-4">
              {isPending
                ? 'Are you sure you want to withdraw this leave application?'
                : 'Are you sure you want to cancel this approved leave?'}
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
                {isPending ? 'Withdraw' : 'Cancel Leave'}
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

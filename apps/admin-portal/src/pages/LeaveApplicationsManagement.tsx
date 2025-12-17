import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { usePendingLeaveApprovals, useLeaveApplications, useApproveLeaveApplication, useRejectLeaveApplication, useCancelLeaveApplication } from '@/hooks/api/useLeaveApplications'
import { useCompanies } from '@/hooks/api/useCompanies'
import { LeaveApplication, ApproveLeaveDto, RejectLeaveDto } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Check, X, Clock, AlertTriangle } from 'lucide-react'
import { format } from 'date-fns'

const LeaveApplicationsManagement = () => {
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  const [selectedStatus, setSelectedStatus] = useState<string>('pending')
  const [approvingApplication, setApprovingApplication] = useState<LeaveApplication | null>(null)
  const [rejectingApplication, setRejectingApplication] = useState<LeaveApplication | null>(null)
  const [rejectionReason, setRejectionReason] = useState('')

  const { data: pendingApprovals = [], isLoading: loadingPending, refetch: refetchPending } = usePendingLeaveApprovals(selectedCompanyId || undefined)
  const { data: allApplications = [], isLoading: loadingAll, refetch: refetchAll } = useLeaveApplications({
    companyId: selectedCompanyId || undefined,
    status: selectedStatus || undefined,
  })
  const { data: companies = [] } = useCompanies()

  const approveApplication = useApproveLeaveApplication()
  const rejectApplication = useRejectLeaveApplication()
  const cancelApplication = useCancelLeaveApplication()

  const isLoading = loadingPending || loadingAll

  const handleApprove = (application: LeaveApplication) => {
    setApprovingApplication(application)
  }

  const handleReject = (application: LeaveApplication) => {
    setRejectingApplication(application)
    setRejectionReason('')
  }

  const handleApproveConfirm = async () => {
    if (approvingApplication) {
      try {
        await approveApplication.mutateAsync({
          id: approvingApplication.id,
          data: { approvedBy: 'admin' } as ApproveLeaveDto, // In real app, use current user ID
        })
        setApprovingApplication(null)
        refetchPending()
        refetchAll()
      } catch (error) {
        console.error('Failed to approve application:', error)
      }
    }
  }

  const handleRejectConfirm = async () => {
    if (rejectingApplication && rejectionReason) {
      try {
        await rejectApplication.mutateAsync({
          id: rejectingApplication.id,
          data: {
            rejectedBy: 'admin',
            rejectionReason,
          } as RejectLeaveDto,
        })
        setRejectingApplication(null)
        refetchPending()
        refetchAll()
      } catch (error) {
        console.error('Failed to reject application:', error)
      }
    }
  }

  const handleCancel = async (application: LeaveApplication) => {
    if (confirm('Are you sure you want to cancel this leave application?')) {
      try {
        await cancelApplication.mutateAsync(application.id)
        refetchPending()
        refetchAll()
      } catch (error) {
        console.error('Failed to cancel application:', error)
      }
    }
  }

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { bg: string; text: string; icon: JSX.Element }> = {
      pending: { bg: 'bg-yellow-100', text: 'text-yellow-800', icon: <Clock size={14} /> },
      approved: { bg: 'bg-green-100', text: 'text-green-800', icon: <Check size={14} /> },
      rejected: { bg: 'bg-red-100', text: 'text-red-800', icon: <X size={14} /> },
      cancelled: { bg: 'bg-gray-100', text: 'text-gray-800', icon: <X size={14} /> },
      withdrawn: { bg: 'bg-gray-100', text: 'text-gray-800', icon: <X size={14} /> },
    }
    const config = statusConfig[status] || statusConfig.pending
    return (
      <span className={`inline-flex items-center gap-1 px-2 py-1 text-xs font-medium rounded-full ${config.bg} ${config.text}`}>
        {config.icon}
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </span>
    )
  }

  const columns: ColumnDef<LeaveApplication>[] = [
    {
      accessorKey: 'employee',
      header: 'Employee',
      cell: ({ row }) => (
        <div>
          <div className="font-medium text-gray-900">{row.original.employee?.employeeName || 'Unknown'}</div>
          <div className="text-sm text-gray-500">{row.original.employee?.department}</div>
        </div>
      ),
    },
    {
      accessorKey: 'leaveType',
      header: 'Leave Type',
      cell: ({ row }) => (
        <div className="text-gray-900">{row.original.leaveType?.name || 'Unknown'}</div>
      ),
    },
    {
      accessorKey: 'fromDate',
      header: 'Period',
      cell: ({ row }) => (
        <div className="text-sm">
          <div>{format(new Date(row.original.fromDate), 'dd MMM yyyy')}</div>
          <div className="text-gray-500">to {format(new Date(row.original.toDate), 'dd MMM yyyy')}</div>
        </div>
      ),
    },
    {
      accessorKey: 'totalDays',
      header: 'Days',
      cell: ({ row }) => (
        <div className="text-center">
          <span className="font-medium">{row.original.totalDays}</span>
          {row.original.isHalfDay && (
            <span className="text-xs text-gray-500 ml-1">(Half)</span>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'reason',
      header: 'Reason',
      cell: ({ row }) => (
        <div className="max-w-xs truncate text-sm text-gray-600">
          {row.original.reason || '-'}
        </div>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status),
    },
    {
      accessorKey: 'appliedAt',
      header: 'Applied On',
      cell: ({ row }) => (
        <div className="text-sm text-gray-500">
          {format(new Date(row.original.appliedAt), 'dd MMM yyyy')}
        </div>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const application = row.original
        if (application.status !== 'pending') {
          return null
        }
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleApprove(application)}
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="Approve"
            >
              <Check size={16} />
            </button>
            <button
              onClick={() => handleReject(application)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Reject"
            >
              <X size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Leave Applications</h1>
        <p className="text-gray-600 mt-2">Manage and approve leave applications</p>
      </div>

      {/* Pending Approvals Alert */}
      {pendingApprovals.length > 0 && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div className="flex items-center">
            <AlertTriangle className="h-5 w-5 text-yellow-600 mr-2" />
            <span className="font-medium text-yellow-800">
              {pendingApprovals.length} pending leave application{pendingApprovals.length > 1 ? 's' : ''} awaiting approval
            </span>
          </div>
        </div>
      )}

      <div className="flex flex-wrap items-center gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
          <select
            value={selectedCompanyId}
            onChange={(e) => setSelectedCompanyId(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
          >
            <option value="">All Companies</option>
            {companies.map(company => (
              <option key={company.id} value={company.id}>{company.name}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
          <select
            value={selectedStatus}
            onChange={(e) => setSelectedStatus(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
          >
            <option value="">All Status</option>
            <option value="pending">Pending</option>
            <option value="approved">Approved</option>
            <option value="rejected">Rejected</option>
            <option value="cancelled">Cancelled</option>
          </select>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Applications</div>
          <div className="text-2xl font-bold text-gray-900">{allApplications.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Pending</div>
          <div className="text-2xl font-bold text-yellow-600">
            {allApplications.filter(a => a.status === 'pending').length}
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Approved</div>
          <div className="text-2xl font-bold text-green-600">
            {allApplications.filter(a => a.status === 'approved').length}
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Rejected</div>
          <div className="text-2xl font-bold text-red-600">
            {allApplications.filter(a => a.status === 'rejected').length}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={allApplications}
            searchPlaceholder="Search applications..."
          />
        </div>
      </div>

      {/* Approve Modal */}
      <Modal
        isOpen={!!approvingApplication}
        onClose={() => setApprovingApplication(null)}
        title="Approve Leave Application"
        size="sm"
      >
        {approvingApplication && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-md space-y-2">
              <p><strong>Employee:</strong> {approvingApplication.employee?.employeeName}</p>
              <p><strong>Leave Type:</strong> {approvingApplication.leaveType?.name}</p>
              <p><strong>Period:</strong> {format(new Date(approvingApplication.fromDate), 'dd MMM yyyy')} - {format(new Date(approvingApplication.toDate), 'dd MMM yyyy')}</p>
              <p><strong>Days:</strong> {approvingApplication.totalDays}</p>
              {approvingApplication.reason && <p><strong>Reason:</strong> {approvingApplication.reason}</p>}
            </div>
            <p className="text-gray-700">Are you sure you want to approve this leave application?</p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setApprovingApplication(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleApproveConfirm}
                disabled={approveApplication.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-green-600 border border-transparent rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {approveApplication.isPending ? 'Approving...' : 'Approve'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Reject Modal */}
      <Modal
        isOpen={!!rejectingApplication}
        onClose={() => setRejectingApplication(null)}
        title="Reject Leave Application"
        size="sm"
      >
        {rejectingApplication && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-md space-y-2">
              <p><strong>Employee:</strong> {rejectingApplication.employee?.employeeName}</p>
              <p><strong>Leave Type:</strong> {rejectingApplication.leaveType?.name}</p>
              <p><strong>Days:</strong> {rejectingApplication.totalDays}</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Reason for Rejection *</label>
              <textarea
                value={rejectionReason}
                onChange={(e) => setRejectionReason(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
                rows={3}
                placeholder="Please provide a reason for rejecting this leave application..."
                required
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setRejectingApplication(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleRejectConfirm}
                disabled={rejectApplication.isPending || !rejectionReason}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {rejectApplication.isPending ? 'Rejecting...' : 'Reject'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default LeaveApplicationsManagement

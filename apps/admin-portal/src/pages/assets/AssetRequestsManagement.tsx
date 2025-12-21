import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import {
  useAssetRequestsByCompany,
  usePendingAssetRequests,
  useUnfulfilledAssetRequests,
  useAssetRequestStats,
  useApproveAssetRequest,
  useRejectAssetRequest,
  useFulfillAssetRequest,
} from '@/hooks/api/useAssetRequests'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useAssets } from '@/hooks/api/useAssets'
import { useAuth } from '@/contexts/AuthContext'
import { AssetRequestSummary, ApproveAssetRequestDto, RejectAssetRequestDto, FulfillAssetRequestDto } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { CompanySelect } from '@/components/ui/CompanySelect'
import { Check, X, Clock, AlertTriangle, Package, Laptop } from 'lucide-react'
import { format } from 'date-fns'

const AssetRequestsManagement = () => {
  const { user } = useAuth()
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  const [selectedStatus, setSelectedStatus] = useState<string>('')
  const [approvingRequest, setApprovingRequest] = useState<AssetRequestSummary | null>(null)
  const [rejectingRequest, setRejectingRequest] = useState<AssetRequestSummary | null>(null)
  const [fulfillingRequest, setFulfillingRequest] = useState<AssetRequestSummary | null>(null)
  const [rejectionReason, setRejectionReason] = useState('')
  const [selectedAssetId, setSelectedAssetId] = useState('')
  const [fulfillmentNotes, setFulfillmentNotes] = useState('')

  const { data: allRequests = [], isLoading: loadingAll, refetch: refetchAll } = useAssetRequestsByCompany(
    selectedCompanyId,
    selectedStatus || undefined,
    !!selectedCompanyId
  )
  const { data: pendingRequests = [], isLoading: loadingPending, refetch: refetchPending } = usePendingAssetRequests(
    selectedCompanyId,
    !!selectedCompanyId
  )
  const { data: unfulfilledRequests = [], refetch: refetchUnfulfilled } = useUnfulfilledAssetRequests(
    selectedCompanyId,
    !!selectedCompanyId
  )
  const { data: stats } = useAssetRequestStats(selectedCompanyId, !!selectedCompanyId)
  const { data: companies = [] } = useCompanies()
  const { data: assetsData } = useAssets({ pageNumber: 1, pageSize: 100 })
  const availableAssets = assetsData?.items?.filter(a => a.status === 'available') || []

  const approveRequest = useApproveAssetRequest()
  const rejectRequest = useRejectAssetRequest()
  const fulfillRequest = useFulfillAssetRequest()

  const isLoading = loadingPending || loadingAll

  const handleApprove = (request: AssetRequestSummary) => {
    setApprovingRequest(request)
  }

  const handleReject = (request: AssetRequestSummary) => {
    setRejectingRequest(request)
    setRejectionReason('')
  }

  const handleFulfill = (request: AssetRequestSummary) => {
    setFulfillingRequest(request)
    setSelectedAssetId('')
    setFulfillmentNotes('')
  }

  const handleApproveConfirm = async () => {
    if (approvingRequest && user) {
      if (!user.employeeId) {
        alert('Your admin account does not have an associated employee record. Please contact support.')
        return
      }
      try {
        await approveRequest.mutateAsync({
          id: approvingRequest.id,
          approvedBy: user.employeeId,
          data: {} as ApproveAssetRequestDto,
        })
        setApprovingRequest(null)
        refetchPending()
        refetchAll()
        refetchUnfulfilled()
      } catch (error) {
        console.error('Failed to approve request:', error)
      }
    }
  }

  const handleRejectConfirm = async () => {
    if (rejectingRequest && rejectionReason && user) {
      if (!user.employeeId) {
        alert('Your admin account does not have an associated employee record. Please contact support.')
        return
      }
      try {
        await rejectRequest.mutateAsync({
          id: rejectingRequest.id,
          rejectedBy: user.employeeId,
          data: { reason: rejectionReason } as RejectAssetRequestDto,
        })
        setRejectingRequest(null)
        refetchPending()
        refetchAll()
      } catch (error) {
        console.error('Failed to reject request:', error)
      }
    }
  }

  const handleFulfillConfirm = async () => {
    if (fulfillingRequest && selectedAssetId && user) {
      if (!user.employeeId) {
        alert('Your admin account does not have an associated employee record. Please contact support.')
        return
      }
      try {
        await fulfillRequest.mutateAsync({
          id: fulfillingRequest.id,
          fulfilledBy: user.employeeId,
          data: {
            assetId: selectedAssetId,
            notes: fulfillmentNotes || undefined,
          } as FulfillAssetRequestDto,
        })
        setFulfillingRequest(null)
        refetchAll()
        refetchUnfulfilled()
      } catch (error) {
        console.error('Failed to fulfill request:', error)
      }
    }
  }

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { bg: string; text: string; icon: JSX.Element }> = {
      pending: { bg: 'bg-yellow-100', text: 'text-yellow-800', icon: <Clock size={14} /> },
      in_progress: { bg: 'bg-blue-100', text: 'text-blue-800', icon: <Clock size={14} /> },
      approved: { bg: 'bg-green-100', text: 'text-green-800', icon: <Check size={14} /> },
      fulfilled: { bg: 'bg-emerald-100', text: 'text-emerald-800', icon: <Package size={14} /> },
      rejected: { bg: 'bg-red-100', text: 'text-red-800', icon: <X size={14} /> },
      cancelled: { bg: 'bg-gray-100', text: 'text-gray-800', icon: <X size={14} /> },
    }
    const config = statusConfig[status] || statusConfig.pending
    return (
      <span className={`inline-flex items-center gap-1 px-2 py-1 text-xs font-medium rounded-full ${config.bg} ${config.text}`}>
        {config.icon}
        {status.replace('_', ' ').charAt(0).toUpperCase() + status.replace('_', ' ').slice(1)}
      </span>
    )
  }

  const getPriorityBadge = (priority: string) => {
    const priorityConfig: Record<string, { bg: string; text: string }> = {
      urgent: { bg: 'bg-red-100', text: 'text-red-800' },
      high: { bg: 'bg-orange-100', text: 'text-orange-800' },
      normal: { bg: 'bg-gray-100', text: 'text-gray-800' },
      low: { bg: 'bg-green-100', text: 'text-green-800' },
    }
    const config = priorityConfig[priority] || priorityConfig.normal
    return (
      <span className={`inline-flex items-center px-2 py-0.5 text-xs font-medium rounded ${config.bg} ${config.text}`}>
        {priority.charAt(0).toUpperCase() + priority.slice(1)}
      </span>
    )
  }

  const columns: ColumnDef<AssetRequestSummary>[] = [
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => (
        <div>
          <div className="font-medium text-gray-900">{row.original.employeeName}</div>
          {row.original.employeeCode && (
            <div className="text-sm text-gray-500">{row.original.employeeCode}</div>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'title',
      header: 'Request',
      cell: ({ row }) => (
        <div>
          <div className="font-medium text-gray-900">{row.original.title}</div>
          <div className="text-sm text-gray-500">{row.original.assetType} • {row.original.category}</div>
        </div>
      ),
    },
    {
      accessorKey: 'quantity',
      header: 'Qty',
      cell: ({ row }) => (
        <div className="text-center font-medium">{row.original.quantity}</div>
      ),
    },
    {
      accessorKey: 'priority',
      header: 'Priority',
      cell: ({ row }) => getPriorityBadge(row.original.priority),
    },
    {
      accessorKey: 'estimatedBudget',
      header: 'Budget',
      cell: ({ row }) => (
        <div className="text-sm text-gray-600">
          {row.original.estimatedBudget ? `₹${row.original.estimatedBudget.toLocaleString()}` : '-'}
        </div>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status),
    },
    {
      accessorKey: 'requestedAt',
      header: 'Requested',
      cell: ({ row }) => (
        <div className="text-sm text-gray-500">
          {format(new Date(row.original.requestedAt), 'dd MMM yyyy')}
        </div>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const request = row.original
        return (
          <div className="flex space-x-2">
            {request.status === 'pending' && (
              <>
                <button
                  onClick={() => handleApprove(request)}
                  className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                  title="Approve"
                >
                  <Check size={16} />
                </button>
                <button
                  onClick={() => handleReject(request)}
                  className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                  title="Reject"
                >
                  <X size={16} />
                </button>
              </>
            )}
            {request.status === 'approved' && (
              <button
                onClick={() => handleFulfill(request)}
                className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                title="Fulfill"
              >
                <Package size={16} />
              </button>
            )}
          </div>
        )
      },
    },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Asset Requests</h1>
        <p className="text-gray-600 mt-2">Manage employee asset requests and fulfillment</p>
      </div>

      {/* Alerts */}
      {selectedCompanyId && pendingRequests.length > 0 && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div className="flex items-center">
            <AlertTriangle className="h-5 w-5 text-yellow-600 mr-2" />
            <span className="font-medium text-yellow-800">
              {pendingRequests.length} pending asset request{pendingRequests.length > 1 ? 's' : ''} awaiting approval
            </span>
          </div>
        </div>
      )}

      {selectedCompanyId && unfulfilledRequests.length > 0 && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-center">
            <Package className="h-5 w-5 text-blue-600 mr-2" />
            <span className="font-medium text-blue-800">
              {unfulfilledRequests.length} approved request{unfulfilledRequests.length > 1 ? 's' : ''} awaiting fulfillment
            </span>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
          <CompanySelect
            companies={companies}
            value={selectedCompanyId}
            onChange={setSelectedCompanyId}
            showAllOption={false}
            className="w-[250px]"
          />
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
            <option value="fulfilled">Fulfilled</option>
            <option value="rejected">Rejected</option>
            <option value="cancelled">Cancelled</option>
          </select>
        </div>
      </div>

      {/* Stats Cards */}
      {selectedCompanyId && stats && (
        <div className="grid grid-cols-2 md:grid-cols-6 gap-4">
          <div className="bg-white rounded-lg shadow p-4">
            <div className="text-sm font-medium text-gray-500">Total</div>
            <div className="text-2xl font-bold text-gray-900">{stats.totalRequests}</div>
          </div>
          <div className="bg-white rounded-lg shadow p-4">
            <div className="text-sm font-medium text-gray-500">Pending</div>
            <div className="text-2xl font-bold text-yellow-600">{stats.pendingRequests}</div>
          </div>
          <div className="bg-white rounded-lg shadow p-4">
            <div className="text-sm font-medium text-gray-500">Approved</div>
            <div className="text-2xl font-bold text-green-600">{stats.approvedRequests}</div>
          </div>
          <div className="bg-white rounded-lg shadow p-4">
            <div className="text-sm font-medium text-gray-500">Fulfilled</div>
            <div className="text-2xl font-bold text-emerald-600">{stats.fulfilledRequests}</div>
          </div>
          <div className="bg-white rounded-lg shadow p-4">
            <div className="text-sm font-medium text-gray-500">Unfulfilled</div>
            <div className="text-2xl font-bold text-blue-600">{stats.unfulfilledApproved}</div>
          </div>
          <div className="bg-white rounded-lg shadow p-4">
            <div className="text-sm font-medium text-gray-500">Rejected</div>
            <div className="text-2xl font-bold text-red-600">{stats.rejectedRequests}</div>
          </div>
        </div>
      )}

      {/* Data Table */}
      {!selectedCompanyId ? (
        <div className="bg-white rounded-lg shadow p-12 text-center">
          <Laptop className="mx-auto h-12 w-12 text-gray-400" />
          <h3 className="mt-4 text-lg font-medium text-gray-900">Select a Company</h3>
          <p className="mt-2 text-gray-500">Choose a company to view asset requests</p>
        </div>
      ) : isLoading ? (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      ) : (
        <div className="bg-white rounded-lg shadow">
          <div className="p-6">
            <DataTable
              columns={columns}
              data={allRequests}
              searchPlaceholder="Search requests..."
            />
          </div>
        </div>
      )}

      {/* Approve Modal */}
      <Modal
        isOpen={!!approvingRequest}
        onClose={() => setApprovingRequest(null)}
        title="Approve Asset Request"
        size="sm"
      >
        {approvingRequest && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-md space-y-2">
              <p><strong>Employee:</strong> {approvingRequest.employeeName}</p>
              <p><strong>Request:</strong> {approvingRequest.title}</p>
              <p><strong>Type:</strong> {approvingRequest.assetType} - {approvingRequest.category}</p>
              <p><strong>Quantity:</strong> {approvingRequest.quantity}</p>
              {approvingRequest.estimatedBudget && (
                <p><strong>Est. Budget:</strong> ₹{approvingRequest.estimatedBudget.toLocaleString()}</p>
              )}
            </div>
            <p className="text-gray-700">Are you sure you want to approve this asset request?</p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setApprovingRequest(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleApproveConfirm}
                disabled={approveRequest.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-green-600 border border-transparent rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {approveRequest.isPending ? 'Approving...' : 'Approve'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Reject Modal */}
      <Modal
        isOpen={!!rejectingRequest}
        onClose={() => setRejectingRequest(null)}
        title="Reject Asset Request"
        size="sm"
      >
        {rejectingRequest && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-md space-y-2">
              <p><strong>Employee:</strong> {rejectingRequest.employeeName}</p>
              <p><strong>Request:</strong> {rejectingRequest.title}</p>
              <p><strong>Quantity:</strong> {rejectingRequest.quantity}</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Reason for Rejection *</label>
              <textarea
                value={rejectionReason}
                onChange={(e) => setRejectionReason(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
                rows={3}
                placeholder="Please provide a reason for rejecting this request..."
                required
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setRejectingRequest(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleRejectConfirm}
                disabled={rejectRequest.isPending || !rejectionReason}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {rejectRequest.isPending ? 'Rejecting...' : 'Reject'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Fulfill Modal */}
      <Modal
        isOpen={!!fulfillingRequest}
        onClose={() => setFulfillingRequest(null)}
        title="Fulfill Asset Request"
        size="md"
      >
        {fulfillingRequest && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-md space-y-2">
              <p><strong>Employee:</strong> {fulfillingRequest.employeeName}</p>
              <p><strong>Request:</strong> {fulfillingRequest.title}</p>
              <p><strong>Type:</strong> {fulfillingRequest.assetType} - {fulfillingRequest.category}</p>
              <p><strong>Quantity:</strong> {fulfillingRequest.quantity}</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Select Asset to Assign *</label>
              <select
                value={selectedAssetId}
                onChange={(e) => setSelectedAssetId(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
                required
              >
                <option value="">Select an available asset...</option>
                {availableAssets.map((asset) => (
                  <option key={asset.id} value={asset.id}>
                    {asset.name} ({asset.assetTag}) - {asset.assetType}
                  </option>
                ))}
              </select>
              {availableAssets.length === 0 && (
                <p className="mt-1 text-sm text-yellow-600">No available assets found. Please add assets to inventory first.</p>
              )}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Notes (Optional)</label>
              <textarea
                value={fulfillmentNotes}
                onChange={(e) => setFulfillmentNotes(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
                rows={2}
                placeholder="Any notes about fulfillment..."
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setFulfillingRequest(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleFulfillConfirm}
                disabled={fulfillRequest.isPending || !selectedAssetId}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
              >
                {fulfillRequest.isPending ? 'Fulfilling...' : 'Fulfill Request'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default AssetRequestsManagement

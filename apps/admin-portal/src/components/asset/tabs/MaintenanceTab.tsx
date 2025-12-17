import { FC, useState } from 'react'
import { useAssetMaintenanceHistory } from '@/features/assets/hooks'
import type { AssetMaintenance } from '@/services/api/types'
import { formatDate } from '@/lib/date'
import { formatCurrency } from '@/lib/currency'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Wrench,
  Plus,
  Calendar,
  AlertCircle,
  Loader2,
  DollarSign,
  Building2,
  CheckCircle2,
  Clock,
  AlertTriangle,
} from 'lucide-react'

interface MaintenanceTabProps {
  assetId: string
  onAddMaintenance?: () => void
}

const getStatusBadge = (status: string) => {
  switch (status.toLowerCase()) {
    case 'open':
    case 'in_progress':
      return <Badge variant="outline" className="border-blue-300 text-blue-600">In Progress</Badge>
    case 'scheduled':
      return <Badge variant="outline" className="border-yellow-300 text-yellow-600">Scheduled</Badge>
    case 'completed':
    case 'closed':
      return <Badge variant="outline" className="border-green-300 text-green-600">Completed</Badge>
    case 'cancelled':
      return <Badge variant="outline" className="border-gray-300 text-gray-500">Cancelled</Badge>
    default:
      return <Badge variant="outline">{status}</Badge>
  }
}

const getStatusIcon = (status: string) => {
  switch (status.toLowerCase()) {
    case 'open':
    case 'in_progress':
      return <Wrench className="w-4 h-4 text-blue-500" />
    case 'scheduled':
      return <Clock className="w-4 h-4 text-yellow-500" />
    case 'completed':
    case 'closed':
      return <CheckCircle2 className="w-4 h-4 text-green-500" />
    case 'cancelled':
      return <AlertTriangle className="w-4 h-4 text-gray-400" />
    default:
      return <Wrench className="w-4 h-4 text-gray-400" />
  }
}

const MaintenanceCard: FC<{ record: AssetMaintenance }> = ({ record }) => {
  const isOverdue = record.dueDate && new Date(record.dueDate) < new Date() && !record.closedAt

  return (
    <div className={`rounded-lg border p-3 ${isOverdue ? 'border-red-200 bg-red-50' : 'border-gray-200 bg-white'}`}>
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0 mt-0.5">
          {getStatusIcon(record.status)}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between gap-2">
            <h4 className="text-sm font-medium text-gray-900 truncate">{record.title}</h4>
            {getStatusBadge(record.status)}
          </div>

          {record.description && (
            <p className="text-xs text-gray-600 mt-1 line-clamp-2">{record.description}</p>
          )}

          <div className="flex flex-wrap items-center gap-3 mt-2 text-xs text-gray-500">
            <span className="flex items-center gap-1">
              <Calendar className="w-3 h-3" />
              Opened: {formatDate(record.openedAt)}
            </span>
            {record.closedAt && (
              <span className="flex items-center gap-1">
                <CheckCircle2 className="w-3 h-3 text-green-500" />
                Closed: {formatDate(record.closedAt)}
              </span>
            )}
            {record.dueDate && !record.closedAt && (
              <span className={`flex items-center gap-1 ${isOverdue ? 'text-red-600 font-medium' : ''}`}>
                <Clock className="w-3 h-3" />
                Due: {formatDate(record.dueDate)}
                {isOverdue && ' (Overdue)'}
              </span>
            )}
          </div>

          <div className="flex flex-wrap items-center gap-3 mt-1 text-xs text-gray-500">
            {record.vendor && (
              <span className="flex items-center gap-1">
                <Building2 className="w-3 h-3" />
                {record.vendor}
              </span>
            )}
            {record.cost != null && record.cost > 0 && (
              <span className="flex items-center gap-1">
                <DollarSign className="w-3 h-3" />
                {formatCurrency(record.cost, record.currency)}
              </span>
            )}
          </div>

          {record.notes && (
            <p className="text-xs text-gray-500 italic mt-2">{record.notes}</p>
          )}
        </div>
      </div>
    </div>
  )
}

export const MaintenanceTab: FC<MaintenanceTabProps> = ({
  assetId,
  onAddMaintenance,
}) => {
  const { data: records, isLoading, isError } = useAssetMaintenanceHistory(assetId)
  const [showCompleted, setShowCompleted] = useState(false)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-5 h-5 animate-spin text-gray-400" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-gray-500">
        <AlertCircle className="w-8 h-8 mb-2" />
        <p className="text-sm">Failed to load maintenance records</p>
      </div>
    )
  }

  const activeRecords = records?.filter(
    (r) => !['completed', 'closed', 'cancelled'].includes(r.status.toLowerCase())
  ) || []
  const completedRecords = records?.filter(
    (r) => ['completed', 'closed', 'cancelled'].includes(r.status.toLowerCase())
  ) || []

  const totalCost = records?.reduce((sum, r) => sum + (r.cost || 0), 0) || 0

  return (
    <div className="p-4 space-y-4">
      {/* Add Maintenance Action */}
      {onAddMaintenance && (
        <Button onClick={onAddMaintenance} className="w-full">
          <Plus className="w-4 h-4 mr-2" />
          Add Maintenance Record
        </Button>
      )}

      {/* Summary */}
      {records && records.length > 0 && (
        <div className="grid grid-cols-3 gap-2 p-3 bg-gray-50 rounded-lg">
          <div className="text-center">
            <div className="text-lg font-semibold text-gray-900">{records.length}</div>
            <div className="text-xs text-gray-500">Total Records</div>
          </div>
          <div className="text-center">
            <div className="text-lg font-semibold text-blue-600">{activeRecords.length}</div>
            <div className="text-xs text-gray-500">Active</div>
          </div>
          <div className="text-center">
            <div className="text-lg font-semibold text-gray-900">
              {formatCurrency(totalCost)}
            </div>
            <div className="text-xs text-gray-500">Total Cost</div>
          </div>
        </div>
      )}

      {/* Active Maintenance */}
      {activeRecords.length > 0 && (
        <div>
          <h3 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
            Active Maintenance ({activeRecords.length})
          </h3>
          <div className="space-y-2">
            {activeRecords.map((record) => (
              <MaintenanceCard key={record.id} record={record} />
            ))}
          </div>
        </div>
      )}

      {/* Completed Maintenance */}
      {completedRecords.length > 0 && (
        <div>
          <button
            onClick={() => setShowCompleted(!showCompleted)}
            className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2 flex items-center gap-1 hover:text-gray-700"
          >
            {showCompleted ? '▼' : '▶'} Completed ({completedRecords.length})
          </button>
          {showCompleted && (
            <div className="space-y-2">
              {completedRecords.map((record) => (
                <MaintenanceCard key={record.id} record={record} />
              ))}
            </div>
          )}
        </div>
      )}

      {/* Empty State */}
      {(!records || records.length === 0) && (
        <div className="flex flex-col items-center justify-center py-8 text-gray-500">
          <Wrench className="w-10 h-10 mb-3 text-gray-300" />
          <p className="text-sm font-medium">No maintenance records</p>
          <p className="text-xs mt-1">Add a record to track maintenance</p>
        </div>
      )}
    </div>
  )
}

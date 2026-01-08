import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  Plus,
  RefreshCw,
  CheckCircle,
  XCircle,
  Clock,
  AlertTriangle,
  Loader2,
  ChevronLeft,
  ChevronRight,
  Eye,
  Undo2
} from 'lucide-react'
import { useCompanyContext } from '@/contexts/CompanyContext'
import { tallyMigrationApi } from '@/services/api/migration/tallyMigrationService'

const STATUS_CONFIGS: Record<string, { icon: React.ComponentType<{ className?: string }>; colorClass: string; label: string }> = {
  completed: { icon: CheckCircle, colorClass: 'text-green-500', label: 'Completed' },
  completed_with_errors: { icon: AlertTriangle, colorClass: 'text-yellow-500', label: 'Completed with Errors' },
  failed: { icon: XCircle, colorClass: 'text-red-500', label: 'Failed' },
  importing: { icon: Loader2, colorClass: 'text-blue-500 animate-spin', label: 'Importing' },
  parsing: { icon: Clock, colorClass: 'text-blue-500', label: 'Parsing' },
  pending: { icon: Clock, colorClass: 'text-gray-500', label: 'Pending' },
  rolled_back: { icon: Undo2, colorClass: 'text-orange-500', label: 'Rolled Back' },
}

const formatDate = (dateStr: string) => {
  try {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch {
    return dateStr
  }
}

const TallyMigrationHistory = () => {
  const navigate = useNavigate()
  const { selectedCompany } = useCompanyContext()
  const [page, setPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState<string>('')
  const pageSize = 10

  const { data, isLoading, refetch, isFetching } = useQuery({
    queryKey: ['tally-batches', selectedCompany?.id, page, statusFilter],
    queryFn: () => tallyMigrationApi.getBatches(
      selectedCompany!.id,
      page,
      pageSize,
      statusFilter || undefined
    ),
    enabled: !!selectedCompany?.id
  })

  const batches = data?.items || []
  const totalPages = data?.totalPages || 1

  const StatusBadge = ({ status }: { status: string }) => {
    const config = STATUS_CONFIGS[status] || STATUS_CONFIGS.pending
    const Icon = config.icon
    return (
      <span className={`inline-flex items-center gap-1.5 ${config.colorClass}`}>
        <Icon className="h-4 w-4" />
        <span className="text-sm">{config.label}</span>
      </span>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link
            to="/settings"
            className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg dark:text-gray-400 dark:hover:text-gray-200 dark:hover:bg-gray-800"
          >
            <ArrowLeft className="h-5 w-5" />
          </Link>
          <div>
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Migration History</h1>
            <p className="text-gray-600 dark:text-gray-400 mt-1">
              View and manage your Tally import batches
            </p>
          </div>
        </div>
        <Link
          to="/settings/migration/tally"
          className="inline-flex items-center px-4 py-2 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700"
        >
          <Plus className="h-4 w-4 mr-2" />
          New Import
        </Link>
      </div>

      {/* Filters */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <label className="text-sm text-gray-600 dark:text-gray-400">Status:</label>
            <select
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value)
                setPage(1)
              }}
              className="px-3 py-1.5 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
            >
              <option value="">All</option>
              <option value="completed">Completed</option>
              <option value="completed_with_errors">Completed with Errors</option>
              <option value="failed">Failed</option>
              <option value="importing">In Progress</option>
              <option value="rolled_back">Rolled Back</option>
            </select>
          </div>
          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="inline-flex items-center px-3 py-1.5 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white"
          >
            <RefreshCw className={`h-4 w-4 mr-2 ${isFetching ? 'animate-spin' : ''}`} />
            Refresh
          </button>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center h-64">
            <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
          </div>
        ) : batches.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-64 text-gray-500 dark:text-gray-400">
            <p className="text-lg">No import batches found</p>
            <Link
              to="/settings/migration/tally"
              className="mt-4 text-blue-600 hover:text-blue-700 dark:text-blue-400"
            >
              Start your first import
            </Link>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 dark:bg-gray-900/50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Batch
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Tally Company
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Records
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Date
                    </th>
                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {batches.map((batch) => (
                    <tr key={batch.id} className="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div>
                          <p className="text-sm font-medium text-gray-900 dark:text-white">
                            {batch.batchNumber}
                          </p>
                          <p className="text-xs text-gray-500 dark:text-gray-400">
                            {batch.importType === 'full' ? 'Full Migration' : 'Incremental'}
                          </p>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <div>
                          <p className="text-sm text-gray-900 dark:text-white">
                            {batch.tallyCompanyName || '-'}
                          </p>
                          {batch.sourceFileName && (
                            <p className="text-xs text-gray-500 dark:text-gray-400 truncate max-w-[200px]">
                              {batch.sourceFileName}
                            </p>
                          )}
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <StatusBadge status={batch.status} />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm">
                          <p className="text-gray-900 dark:text-white">
                            {batch.importedLedgers} ledgers
                          </p>
                          <p className="text-gray-500 dark:text-gray-400">
                            {batch.importedVouchers} vouchers
                          </p>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm">
                          <p className="text-gray-900 dark:text-white">
                            {formatDate(batch.createdAt)}
                          </p>
                          {batch.importCompletedAt && (
                            <p className="text-xs text-gray-500 dark:text-gray-400">
                              Completed: {formatDate(batch.importCompletedAt)}
                            </p>
                          )}
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <button
                          onClick={() => navigate(`/settings/migration/history/${batch.id}`)}
                          className="inline-flex items-center px-3 py-1.5 text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
                        >
                          <Eye className="h-4 w-4 mr-1" />
                          View
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between px-6 py-4 border-t border-gray-200 dark:border-gray-700">
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  Page {page} of {totalPages}
                </p>
                <div className="flex gap-2">
                  <button
                    onClick={() => setPage(p => Math.max(1, p - 1))}
                    disabled={page === 1}
                    className="inline-flex items-center px-3 py-1.5 border border-gray-300 dark:border-gray-600 rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700"
                  >
                    <ChevronLeft className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                    disabled={page === totalPages}
                    className="inline-flex items-center px-3 py-1.5 border border-gray-300 dark:border-gray-600 rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700"
                  >
                    <ChevronRight className="h-4 w-4" />
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}

export default TallyMigrationHistory

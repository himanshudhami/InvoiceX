import { useState } from 'react'
import { Link } from 'react-router-dom'
import {
  CheckCircle,
  XCircle,
  AlertTriangle,
  Download,
  RefreshCw,
  Users,
  Package,
  FileText,
  Warehouse,
  Tag,
  Receipt,
  CreditCard,
  ChevronDown,
  ChevronUp,
  ExternalLink,
  AlertCircle
} from 'lucide-react'
import { TallyImportResult, TallyImportCounts } from '@/services/api/migration/tallyMigrationService'

interface TallyImportSummaryProps {
  result: TallyImportResult
  onNewImport: () => void
}

interface CountCardProps {
  title: string
  icon: React.ComponentType<{ className?: string }>
  counts: TallyImportCounts
  colorClass: string
}

const CountCard = ({ title, icon: Icon, counts, colorClass }: CountCardProps) => (
  <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4">
    <div className="flex items-center gap-2 mb-3">
      <div className={`p-1.5 rounded ${colorClass}`}>
        <Icon className="h-4 w-4" />
      </div>
      <span className="font-medium text-gray-900 dark:text-white">{title}</span>
    </div>
    <div className="grid grid-cols-2 gap-2 text-sm">
      <div>
        <span className="text-gray-500 dark:text-gray-400">Total:</span>
        <span className="ml-1 text-gray-900 dark:text-white">{counts.total}</span>
      </div>
      <div>
        <span className="text-green-600 dark:text-green-400">Imported:</span>
        <span className="ml-1">{counts.imported}</span>
      </div>
      {counts.skipped > 0 && (
        <div>
          <span className="text-yellow-600 dark:text-yellow-400">Skipped:</span>
          <span className="ml-1">{counts.skipped}</span>
        </div>
      )}
      {counts.failed > 0 && (
        <div>
          <span className="text-red-600 dark:text-red-400">Failed:</span>
          <span className="ml-1">{counts.failed}</span>
        </div>
      )}
      {counts.suspense > 0 && (
        <div>
          <span className="text-orange-600 dark:text-orange-400">Suspense:</span>
          <span className="ml-1">{counts.suspense}</span>
        </div>
      )}
    </div>
  </div>
)

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
  }).format(amount)
}

const formatDuration = (seconds: number) => {
  if (seconds < 60) return `${Math.round(seconds)} seconds`
  const mins = Math.floor(seconds / 60)
  const secs = Math.round(seconds % 60)
  return `${mins}m ${secs}s`
}

const TallyImportSummary = ({ result, onNewImport }: TallyImportSummaryProps) => {
  const [showErrors, setShowErrors] = useState(false)
  const [showSuspense, setShowSuspense] = useState(false)

  const isSuccess = result.success && result.failedRecords === 0
  const hasWarnings = result.suspenseRecords > 0 || result.skippedRecords > 0

  const downloadReport = () => {
    const report = {
      batchId: result.batchId,
      batchNumber: result.batchNumber,
      status: result.status,
      importedAt: result.completedAt,
      duration: `${result.durationSeconds} seconds`,
      summary: {
        totalRecords: result.totalRecords,
        imported: result.importedRecords,
        skipped: result.skippedRecords,
        failed: result.failedRecords,
        suspense: result.suspenseRecords
      },
      ledgers: result.ledgers,
      stockItems: result.stockItems,
      stockGroups: result.stockGroups,
      godowns: result.godowns,
      costCenters: result.costCenters,
      vouchers: result.vouchers,
      voucherCountsByType: result.voucherCountsByType,
      errors: result.errors,
      suspenseItems: result.suspenseItems
    }

    const blob = new Blob([JSON.stringify(report, null, 2)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `tally-import-${result.batchNumber}-report.json`
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="text-center">
        <div className="flex justify-center mb-4">
          {isSuccess ? (
            <CheckCircle className="h-16 w-16 text-green-500" />
          ) : result.failedRecords > 0 ? (
            <XCircle className="h-16 w-16 text-red-500" />
          ) : (
            <AlertTriangle className="h-16 w-16 text-yellow-500" />
          )}
        </div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
          {isSuccess
            ? 'Import Completed Successfully!'
            : hasWarnings
              ? 'Import Completed with Warnings'
              : 'Import Failed'
          }
        </h2>
        <p className="text-gray-500 dark:text-gray-400 mt-2">
          Batch: {result.batchNumber} | Duration: {formatDuration(result.durationSeconds)}
        </p>
      </div>

      {/* Overall Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4 text-center">
          <p className="text-3xl font-bold text-blue-700 dark:text-blue-300">
            {result.totalRecords}
          </p>
          <p className="text-sm text-blue-600 dark:text-blue-400">Total Records</p>
        </div>
        <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-4 text-center">
          <p className="text-3xl font-bold text-green-700 dark:text-green-300">
            {result.importedRecords}
          </p>
          <p className="text-sm text-green-600 dark:text-green-400">Imported</p>
        </div>
        <div className="bg-yellow-50 dark:bg-yellow-900/20 rounded-lg p-4 text-center">
          <p className="text-3xl font-bold text-yellow-700 dark:text-yellow-300">
            {result.skippedRecords}
          </p>
          <p className="text-sm text-yellow-600 dark:text-yellow-400">Skipped</p>
        </div>
        <div className="bg-red-50 dark:bg-red-900/20 rounded-lg p-4 text-center">
          <p className="text-3xl font-bold text-red-700 dark:text-red-300">
            {result.failedRecords}
          </p>
          <p className="text-sm text-red-600 dark:text-red-400">Failed</p>
        </div>
      </div>

      {/* Detailed Counts */}
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
        <CountCard
          title="Ledgers"
          icon={Users}
          counts={result.ledgers}
          colorClass="bg-purple-100 text-purple-600 dark:bg-purple-900/50 dark:text-purple-400"
        />
        <CountCard
          title="Stock Items"
          icon={Package}
          counts={result.stockItems}
          colorClass="bg-cyan-100 text-cyan-600 dark:bg-cyan-900/50 dark:text-cyan-400"
        />
        <CountCard
          title="Stock Groups"
          icon={Package}
          counts={result.stockGroups}
          colorClass="bg-indigo-100 text-indigo-600 dark:bg-indigo-900/50 dark:text-indigo-400"
        />
        <CountCard
          title="Warehouses"
          icon={Warehouse}
          counts={result.godowns}
          colorClass="bg-teal-100 text-teal-600 dark:bg-teal-900/50 dark:text-teal-400"
        />
        <CountCard
          title="Cost Centers"
          icon={Tag}
          counts={result.costCenters}
          colorClass="bg-orange-100 text-orange-600 dark:bg-orange-900/50 dark:text-orange-400"
        />
        <CountCard
          title="Vouchers"
          icon={FileText}
          counts={result.vouchers}
          colorClass="bg-blue-100 text-blue-600 dark:bg-blue-900/50 dark:text-blue-400"
        />
      </div>

      {/* Voucher Types Breakdown */}
      {Object.keys(result.voucherCountsByType).length > 0 && (
        <div className="bg-gray-50 dark:bg-gray-900/50 rounded-lg p-4">
          <h3 className="font-medium text-gray-900 dark:text-white mb-3">Vouchers by Type</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            {Object.entries(result.voucherCountsByType)
              .sort((a, b) => b[1] - a[1])
              .map(([type, count]) => (
                <div
                  key={type}
                  className="flex items-center justify-between p-2 bg-white dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700"
                >
                  <span className="text-sm text-gray-700 dark:text-gray-300">{type}</span>
                  <span className="text-sm font-medium text-gray-900 dark:text-white">{count}</span>
                </div>
              ))}
          </div>
        </div>
      )}

      {/* Suspense Items */}
      {result.suspenseItems.length > 0 && (
        <div className="border border-orange-200 dark:border-orange-800 rounded-lg overflow-hidden">
          <button
            onClick={() => setShowSuspense(!showSuspense)}
            className="w-full flex items-center justify-between p-4 bg-orange-50 dark:bg-orange-900/20 hover:bg-orange-100 dark:hover:bg-orange-900/30 transition-colors"
          >
            <div className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-orange-500" />
              <span className="font-medium text-orange-800 dark:text-orange-200">
                {result.suspenseItems.length} items sent to suspense
              </span>
              <span className="text-sm text-orange-600 dark:text-orange-400">
                ({formatCurrency(result.suspenseTotalAmount)})
              </span>
            </div>
            {showSuspense ? (
              <ChevronUp className="h-5 w-5 text-orange-400" />
            ) : (
              <ChevronDown className="h-5 w-5 text-orange-400" />
            )}
          </button>
          {showSuspense && (
            <div className="max-h-64 overflow-y-auto">
              <table className="w-full text-sm">
                <thead className="bg-orange-50 dark:bg-orange-900/30 sticky top-0">
                  <tr>
                    <th className="px-4 py-2 text-left font-medium text-orange-700 dark:text-orange-300">Type</th>
                    <th className="px-4 py-2 text-left font-medium text-orange-700 dark:text-orange-300">Name</th>
                    <th className="px-4 py-2 text-right font-medium text-orange-700 dark:text-orange-300">Amount</th>
                    <th className="px-4 py-2 text-left font-medium text-orange-700 dark:text-orange-300">Reason</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-orange-100 dark:divide-orange-900">
                  {result.suspenseItems.map((item, idx) => (
                    <tr key={idx} className="hover:bg-orange-50 dark:hover:bg-orange-900/10">
                      <td className="px-4 py-2 text-gray-700 dark:text-gray-300">{item.recordType}</td>
                      <td className="px-4 py-2 text-gray-900 dark:text-white">{item.tallyName}</td>
                      <td className="px-4 py-2 text-right text-gray-900 dark:text-white">
                        {formatCurrency(item.amount)}
                      </td>
                      <td className="px-4 py-2 text-gray-500 dark:text-gray-400">{item.reason}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Errors */}
      {result.errors.length > 0 && (
        <div className="border border-red-200 dark:border-red-800 rounded-lg overflow-hidden">
          <button
            onClick={() => setShowErrors(!showErrors)}
            className="w-full flex items-center justify-between p-4 bg-red-50 dark:bg-red-900/20 hover:bg-red-100 dark:hover:bg-red-900/30 transition-colors"
          >
            <div className="flex items-center gap-2">
              <AlertCircle className="h-5 w-5 text-red-500" />
              <span className="font-medium text-red-800 dark:text-red-200">
                {result.errors.length} errors occurred
              </span>
            </div>
            {showErrors ? (
              <ChevronUp className="h-5 w-5 text-red-400" />
            ) : (
              <ChevronDown className="h-5 w-5 text-red-400" />
            )}
          </button>
          {showErrors && (
            <div className="max-h-64 overflow-y-auto">
              <table className="w-full text-sm">
                <thead className="bg-red-50 dark:bg-red-900/30 sticky top-0">
                  <tr>
                    <th className="px-4 py-2 text-left font-medium text-red-700 dark:text-red-300">Type</th>
                    <th className="px-4 py-2 text-left font-medium text-red-700 dark:text-red-300">Name</th>
                    <th className="px-4 py-2 text-left font-medium text-red-700 dark:text-red-300">Error</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-red-100 dark:divide-red-900">
                  {result.errors.map((err, idx) => (
                    <tr key={idx} className="hover:bg-red-50 dark:hover:bg-red-900/10">
                      <td className="px-4 py-2 text-gray-700 dark:text-gray-300">{err.recordType}</td>
                      <td className="px-4 py-2 text-gray-900 dark:text-white">{err.tallyName || '-'}</td>
                      <td className="px-4 py-2 text-red-600 dark:text-red-400">{err.errorMessage}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Balance Check */}
      {result.imbalance !== 0 && (
        <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
          <div className="flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-yellow-500 flex-shrink-0 mt-0.5" />
            <div>
              <p className="font-medium text-yellow-800 dark:text-yellow-200">Balance Check Warning</p>
              <p className="text-sm text-yellow-700 dark:text-yellow-300 mt-1">
                Total Debits: {formatCurrency(result.totalDebitAmount)} |
                Total Credits: {formatCurrency(result.totalCreditAmount)} |
                Imbalance: {formatCurrency(result.imbalance)}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex flex-col sm:flex-row justify-between items-center gap-4 pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="flex gap-3">
          <button
            onClick={downloadReport}
            className="inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
          >
            <Download className="h-4 w-4 mr-2" />
            Download Report
          </button>
          <Link
            to={`/settings/migration/history/${result.batchId}`}
            className="inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
          >
            <ExternalLink className="h-4 w-4 mr-2" />
            View Details
          </Link>
        </div>
        <button
          onClick={onNewImport}
          className="inline-flex items-center px-6 py-2 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700"
        >
          <RefreshCw className="h-4 w-4 mr-2" />
          Start New Import
        </button>
      </div>

      {/* Quick Links */}
      <div className="bg-gray-50 dark:bg-gray-900/50 rounded-lg p-4">
        <h3 className="font-medium text-gray-900 dark:text-white mb-3">What's Next?</h3>
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
          <Link
            to="/ledger/accounts"
            className="flex items-center gap-2 p-3 bg-white dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 transition-colors"
          >
            <Receipt className="h-5 w-5 text-blue-500" />
            <span className="text-sm text-gray-700 dark:text-gray-300">Review Chart of Accounts</span>
          </Link>
          <Link
            to="/customers"
            className="flex items-center gap-2 p-3 bg-white dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 transition-colors"
          >
            <Users className="h-5 w-5 text-blue-500" />
            <span className="text-sm text-gray-700 dark:text-gray-300">Review Customers</span>
          </Link>
          <Link
            to="/finance/ap/vendors"
            className="flex items-center gap-2 p-3 bg-white dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 transition-colors"
          >
            <Users className="h-5 w-5 text-blue-500" />
            <span className="text-sm text-gray-700 dark:text-gray-300">Review Vendors</span>
          </Link>
          <Link
            to="/inventory/items"
            className="flex items-center gap-2 p-3 bg-white dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 transition-colors"
          >
            <Package className="h-5 w-5 text-blue-500" />
            <span className="text-sm text-gray-700 dark:text-gray-300">Review Stock Items</span>
          </Link>
          <Link
            to="/ledger/journals"
            className="flex items-center gap-2 p-3 bg-white dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 transition-colors"
          >
            <FileText className="h-5 w-5 text-blue-500" />
            <span className="text-sm text-gray-700 dark:text-gray-300">Review Journal Entries</span>
          </Link>
          <Link
            to="/ledger/trial-balance"
            className="flex items-center gap-2 p-3 bg-white dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 transition-colors"
          >
            <CreditCard className="h-5 w-5 text-blue-500" />
            <span className="text-sm text-gray-700 dark:text-gray-300">View Trial Balance</span>
          </Link>
        </div>
      </div>
    </div>
  )
}

export default TallyImportSummary

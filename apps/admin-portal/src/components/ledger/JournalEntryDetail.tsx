import { useJournalEntry } from '@/features/ledger/hooks'
import { JournalEntryStatus, JournalEntryType } from '@/services/api/types'
import { format, parseISO } from 'date-fns'
import { CheckCircle, Clock, RotateCcw, FileText, Zap, AlertCircle } from 'lucide-react'

interface JournalEntryDetailProps {
  entryId: string
  onClose: () => void
}

const statusConfig: Record<JournalEntryStatus, { label: string; color: string; icon: React.ElementType }> = {
  draft: { label: 'Draft', color: 'bg-yellow-100 text-yellow-800', icon: Clock },
  posted: { label: 'Posted', color: 'bg-green-100 text-green-800', icon: CheckCircle },
  reversed: { label: 'Reversed', color: 'bg-red-100 text-red-800', icon: RotateCcw },
}

const entryTypeConfig: Record<JournalEntryType, { label: string; description: string }> = {
  manual: { label: 'Manual Entry', description: 'Manually created journal entry' },
  auto_post: { label: 'Auto-Posted', description: 'Automatically created from business transaction' },
  reversal: { label: 'Reversal Entry', description: 'Reversal of a previous entry' },
  opening: { label: 'Opening Entry', description: 'Opening balance entry' },
  closing: { label: 'Closing Entry', description: 'Period closing entry' },
}

export const JournalEntryDetail = ({ entryId, onClose }: JournalEntryDetailProps) => {
  const { data: entry, isLoading, error } = useJournalEntry(entryId)

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(amount)
  }

  const formatDate = (dateStr: string) => {
    try {
      return format(parseISO(dateStr), 'dd MMM yyyy')
    } catch {
      return dateStr
    }
  }

  const formatDateTime = (dateStr: string) => {
    try {
      return format(parseISO(dateStr), 'dd MMM yyyy HH:mm')
    } catch {
      return dateStr
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error || !entry) {
    return (
      <div className="text-center py-12">
        <AlertCircle className="mx-auto h-12 w-12 text-red-400" />
        <h3 className="mt-2 text-lg font-medium text-gray-900">Failed to load entry</h3>
        <p className="mt-1 text-sm text-gray-500">Please try again later</p>
      </div>
    )
  }

  const statusCfg = statusConfig[entry.status] || statusConfig.draft
  const typeCfg = entryTypeConfig[entry.entryType] || entryTypeConfig.manual
  const StatusIcon = statusCfg.icon
  const periodNames = ['Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec', 'Jan', 'Feb', 'Mar']

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h2 className="text-xl font-bold text-gray-900">{entry.journalNumber}</h2>
          <p className="text-sm text-gray-500">{formatDate(entry.journalDate)}</p>
        </div>
        <div className="flex gap-2">
          <span className={`inline-flex items-center gap-1 px-3 py-1 text-sm font-medium rounded-full ${statusCfg.color}`}>
            <StatusIcon size={14} />
            {statusCfg.label}
          </span>
        </div>
      </div>

      {/* Entry Info */}
      <div className="bg-gray-50 rounded-lg p-4 space-y-3">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <span className="text-xs text-gray-500 uppercase">Entry Type</span>
            <div className="flex items-center gap-1 mt-1">
              {entry.entryType === 'auto_post' && <Zap size={14} className="text-purple-500" />}
              <span className="text-sm font-medium text-gray-900">{typeCfg.label}</span>
            </div>
          </div>
          <div>
            <span className="text-xs text-gray-500 uppercase">Period</span>
            <div className="text-sm font-medium text-gray-900 mt-1">
              {periodNames[entry.periodMonth - 1]} {entry.financialYear}
            </div>
          </div>
        </div>

        {entry.sourceType && (
          <div>
            <span className="text-xs text-gray-500 uppercase">Source</span>
            <div className="text-sm font-medium text-gray-900 mt-1">
              {entry.sourceType}
              {entry.sourceNumber && <span className="text-gray-500"> #{entry.sourceNumber}</span>}
            </div>
          </div>
        )}

        {entry.ruleCode && (
          <div>
            <span className="text-xs text-gray-500 uppercase">Posting Rule</span>
            <div className="text-sm font-medium text-gray-900 mt-1">{entry.ruleCode}</div>
          </div>
        )}
      </div>

      {/* Description */}
      <div>
        <span className="text-xs text-gray-500 uppercase">Description</span>
        <p className="text-sm text-gray-900 mt-1">{entry.description}</p>
      </div>

      {/* Journal Lines */}
      <div>
        <span className="text-xs text-gray-500 uppercase mb-2 block">Journal Lines</span>
        <div className="border rounded-md overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Account</th>
                <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase w-28">Debit</th>
                <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase w-28">Credit</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {entry.lines?.map((line, index) => (
                <tr key={line.id || index} className="hover:bg-gray-50">
                  <td className="px-3 py-2">
                    <div className="font-mono text-sm text-gray-600">{line.accountCode}</div>
                    <div className="text-sm text-gray-900">{line.accountName}</div>
                    {line.description && (
                      <div className="text-xs text-gray-500 mt-1">{line.description}</div>
                    )}
                  </td>
                  <td className="px-3 py-2 text-right">
                    {line.debitAmount > 0 && (
                      <span className="text-blue-600 font-medium">
                        {formatCurrency(line.debitAmount)}
                      </span>
                    )}
                  </td>
                  <td className="px-3 py-2 text-right">
                    {line.creditAmount > 0 && (
                      <span className="text-green-600 font-medium">
                        {formatCurrency(line.creditAmount)}
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot className="bg-gray-50 font-medium">
              <tr>
                <td className="px-3 py-2 text-right">Totals:</td>
                <td className="px-3 py-2 text-right text-blue-600">{formatCurrency(entry.totalDebit)}</td>
                <td className="px-3 py-2 text-right text-green-600">{formatCurrency(entry.totalCredit)}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

      {/* Audit Info */}
      {(entry.postedAt || entry.createdAt) && (
        <div className="border-t pt-4">
          <span className="text-xs text-gray-500 uppercase mb-2 block">Audit Trail</span>
          <div className="space-y-2 text-sm text-gray-600">
            {entry.createdAt && (
              <div>Created: {formatDateTime(entry.createdAt)}</div>
            )}
            {entry.postedAt && (
              <div>Posted: {formatDateTime(entry.postedAt)}</div>
            )}
            {entry.isReversed && (
              <div className="text-red-600">This entry has been reversed</div>
            )}
            {entry.reversalOfId && (
              <div className="text-orange-600">This is a reversal entry</div>
            )}
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex justify-end pt-4 border-t">
        <button
          onClick={onClose}
          className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
        >
          Close
        </button>
      </div>
    </div>
  )
}

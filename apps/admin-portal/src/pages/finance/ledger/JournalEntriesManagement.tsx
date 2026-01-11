import { useState, useMemo } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useJournalEntries, useJournalEntry, usePostJournalEntry, useReverseJournalEntry } from '@/features/ledger/hooks'
import { useCompanyContext } from '@/contexts/CompanyContext'
import { JournalEntry, JournalEntryStatus, JournalEntryType } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { DateRangeFilter } from '@/components/filters/DateRangeFilter'
import { AmountRangeFilter } from '@/components/filters/AmountRangeFilter'
import {
  FileText,
  Eye,
  CheckCircle,
  RotateCcw,
  Clock,
  Zap,
  AlertTriangle,
  BookOpen,
  Filter,
  ChevronDown,
  ChevronUp,
  XCircle,
} from 'lucide-react'
import toast from 'react-hot-toast'
import { format, parseISO } from 'date-fns'
import { JournalEntryForm } from '@/components/forms/JournalEntryForm'
import { JournalEntryDetail } from '@/components/ledger/JournalEntryDetail'
import { useQueryStates, parseAsString } from 'nuqs'

const statusConfig: Record<JournalEntryStatus, { label: string; color: string; icon: React.ElementType }> = {
  draft: { label: 'Draft', color: 'bg-yellow-100 text-yellow-800', icon: Clock },
  posted: { label: 'Posted', color: 'bg-green-100 text-green-800', icon: CheckCircle },
  reversed: { label: 'Reversed', color: 'bg-red-100 text-red-800', icon: RotateCcw },
}

const entryTypeConfig: Record<JournalEntryType, { label: string; color: string }> = {
  manual: { label: 'Manual', color: 'bg-blue-100 text-blue-800' },
  auto_post: { label: 'Auto', color: 'bg-purple-100 text-purple-800' },
  reversal: { label: 'Reversal', color: 'bg-red-100 text-red-800' },
  opening: { label: 'Opening', color: 'bg-gray-100 text-gray-800' },
  closing: { label: 'Closing', color: 'bg-gray-100 text-gray-800' },
}

const JournalEntriesManagement = () => {
  // Get selected company from context (for multi-company users)
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext()

  // URL-backed filter state with nuqs
  const [urlState, setUrlState] = useQueryStates({
    company: parseAsString.withDefault(''),
    status: parseAsString.withDefault(''),
    type: parseAsString.withDefault(''),
    fromDate: parseAsString.withDefault(''),
    toDate: parseAsString.withDefault(''),
    minAmount: parseAsString.withDefault(''),
    maxAmount: parseAsString.withDefault(''),
  })

  const {
    company: companyFilter,
    status: statusFilter,
    type: entryTypeFilter,
    fromDate,
    toDate,
    minAmount,
    maxAmount,
  } = urlState

  // Setters for URL state
  const setCompanyFilter = (value: string) => setUrlState({ company: value })
  const setStatusFilter = (value: string) => setUrlState({ status: value })
  const setEntryTypeFilter = (value: string) => setUrlState({ type: value })
  const setFromDate = (value: string) => setUrlState({ fromDate: value })
  const setToDate = (value: string) => setUrlState({ toDate: value })
  const setMinAmount = (value: string) => setUrlState({ minAmount: value })
  const setMaxAmount = (value: string) => setUrlState({ maxAmount: value })

  // State for expanded filters
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(() => {
    return !!(fromDate || toDate || minAmount || maxAmount)
  })

  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [viewingEntry, setViewingEntry] = useState<JournalEntry | null>(null)
  const [postingEntry, setPostingEntry] = useState<JournalEntry | null>(null)
  const [reversingEntry, setReversingEntry] = useState<JournalEntry | null>(null)
  const [reversalReason, setReversalReason] = useState('')

  // Determine effective company ID: URL filter takes precedence, then context selection
  const effectiveCompanyId = companyFilter || (hasMultiCompanyAccess ? selectedCompanyId : undefined)

  const { data: allEntries = [], isLoading, error, refetch } = useJournalEntries(effectiveCompanyId || undefined)
  const postEntry = usePostJournalEntry()
  const reverseEntry = useReverseJournalEntry()

  // Filter entries
  const entries = useMemo(() => {
    let filtered = allEntries

    // Status filter
    if (statusFilter) {
      filtered = filtered.filter(e => e.status === statusFilter)
    }

    // Entry type filter
    if (entryTypeFilter) {
      filtered = filtered.filter(e => e.entryType === entryTypeFilter)
    }

    // Date range filter
    if (fromDate) {
      filtered = filtered.filter(e => e.journalDate >= fromDate)
    }
    if (toDate) {
      filtered = filtered.filter(e => e.journalDate <= toDate)
    }

    // Amount filter (using totalDebit as the amount)
    if (minAmount) {
      const min = parseFloat(minAmount)
      filtered = filtered.filter(e => e.totalDebit >= min)
    }
    if (maxAmount) {
      const max = parseFloat(maxAmount)
      filtered = filtered.filter(e => e.totalDebit <= max)
    }

    // Sort by date descending, then by journal number
    return [...filtered].sort((a, b) => {
      const dateCompare = b.journalDate.localeCompare(a.journalDate)
      if (dateCompare !== 0) return dateCompare
      return b.journalNumber.localeCompare(a.journalNumber)
    })
  }, [allEntries, statusFilter, entryTypeFilter, fromDate, toDate, minAmount, maxAmount])

  // Count active filters for badge
  const activeFilterCount = useMemo(() => {
    let count = 0
    if (statusFilter) count++
    if (entryTypeFilter) count++
    if (fromDate && toDate) count++
    if (minAmount || maxAmount) count++
    return count
  }, [statusFilter, entryTypeFilter, fromDate, toDate, minAmount, maxAmount])

  // Clear all filters
  const handleClearAllFilters = () => {
    setUrlState({
      status: '',
      type: '',
      fromDate: '',
      toDate: '',
      minAmount: '',
      maxAmount: '',
    })
  }

  const handleView = (entry: JournalEntry) => {
    setViewingEntry(entry)
  }

  const handlePost = async () => {
    if (postingEntry) {
      try {
        await postEntry.mutateAsync(postingEntry.id)
        setPostingEntry(null)
        toast.success('Journal entry posted successfully')
        refetch()
      } catch (error) {
        console.error('Failed to post entry:', error)
        toast.error('Failed to post journal entry')
      }
    }
  }

  const handleReverse = async () => {
    if (reversingEntry) {
      try {
        await reverseEntry.mutateAsync({ id: reversingEntry.id, reason: reversalReason })
        setReversingEntry(null)
        setReversalReason('')
        toast.success('Journal entry reversed successfully')
        refetch()
      } catch (error) {
        console.error('Failed to reverse entry:', error)
        toast.error('Failed to reverse journal entry')
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    refetch()
  }

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

  const columns: ColumnDef<JournalEntry>[] = [
    {
      accessorKey: 'journalNumber',
      header: 'Journal #',
      cell: ({ row }) => {
        const entry = row.original
        return (
          <div>
            <div className="font-mono font-medium text-gray-900">{entry.journalNumber}</div>
            <div className="text-xs text-gray-500">{formatDate(entry.journalDate)}</div>
          </div>
        )
      },
    },
    {
      accessorKey: 'description',
      header: 'Description',
      cell: ({ row }) => {
        const entry = row.original
        return (
          <div className="max-w-md">
            <div className="text-sm text-gray-900 truncate">{entry.description}</div>
            {entry.sourceType && (
              <div className="text-xs text-gray-500">
                Source: {entry.sourceType} {entry.sourceNumber ? `#${entry.sourceNumber}` : ''}
              </div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'entryType',
      header: 'Type',
      cell: ({ row }) => {
        const entryType = row.getValue('entryType') as JournalEntryType
        const config = entryTypeConfig[entryType] || entryTypeConfig.manual
        return (
          <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${config.color}`}>
            {config.label}
          </span>
        )
      },
    },
    {
      accessorKey: 'totalDebit',
      header: 'Amount',
      cell: ({ row }) => {
        const debit = row.getValue('totalDebit') as number
        return (
          <div className="text-right font-medium text-gray-900">
            {formatCurrency(debit)}
          </div>
        )
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.getValue('status') as JournalEntryStatus
        const config = statusConfig[status] || statusConfig.draft
        const Icon = config.icon
        return (
          <div className={`inline-flex items-center gap-1 px-2 py-1 text-xs font-medium rounded-full ${config.color}`}>
            <Icon size={12} />
            {config.label}
          </div>
        )
      },
    },
    {
      accessorKey: 'financialYear',
      header: 'Period',
      cell: ({ row }) => {
        const fy = row.getValue('financialYear') as string
        const period = row.original.periodMonth
        const periodNames = ['Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec', 'Jan', 'Feb', 'Mar']
        return (
          <div className="text-sm text-gray-600">
            {periodNames[period - 1]} {fy}
          </div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const entry = row.original
        const canPost = entry.status === 'draft'
        const canReverse = entry.status === 'posted' && !entry.isReversed
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleView(entry)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="View details"
            >
              <Eye size={16} />
            </button>
            {canPost && (
              <button
                onClick={() => setPostingEntry(entry)}
                className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                title="Post entry"
              >
                <CheckCircle size={16} />
              </button>
            )}
            {canReverse && (
              <button
                onClick={() => setReversingEntry(entry)}
                className="text-orange-600 hover:text-orange-800 p-1 rounded hover:bg-orange-50 transition-colors"
                title="Reverse entry"
              >
                <RotateCcw size={16} />
              </button>
            )}
          </div>
        )
      },
    },
  ]

  if (!companyFilter) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Journal Entries</h1>
          <p className="text-gray-600 mt-2">View and manage general ledger journal entries</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <FileText className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">Select a Company</h3>
            <p className="mt-1 text-sm text-gray-500">Please select a company to view its journal entries</p>
            <div className="mt-6 flex justify-center">
              <CompanyFilterDropdown
                value={companyFilter ?? ''}
                onChange={(value) => setCompanyFilter(value || null)}
              />
            </div>
          </div>
        </div>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load journal entries</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  // Calculate summary stats
  const draftCount = entries.filter(e => e.status === 'draft').length
  const postedCount = entries.filter(e => e.status === 'posted').length
  const autoPostCount = entries.filter(e => e.entryType === 'auto_post').length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Journal Entries</h1>
        <p className="text-gray-600 mt-2">View and manage general ledger journal entries</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center gap-2 mb-2">
            <FileText className="h-5 w-5 text-gray-400" />
            <span className="text-sm text-gray-600">Total Entries</span>
          </div>
          <div className="text-2xl font-bold text-gray-900">{entries.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center gap-2 mb-2">
            <Clock className="h-5 w-5 text-yellow-500" />
            <span className="text-sm text-gray-600">Drafts</span>
          </div>
          <div className="text-2xl font-bold text-yellow-600">{draftCount}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center gap-2 mb-2">
            <CheckCircle className="h-5 w-5 text-green-500" />
            <span className="text-sm text-gray-600">Posted</span>
          </div>
          <div className="text-2xl font-bold text-green-600">{postedCount}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center gap-2 mb-2">
            <Zap className="h-5 w-5 text-purple-500" />
            <span className="text-sm text-gray-600">Auto-Posted</span>
          </div>
          <div className="text-2xl font-bold text-purple-600">{autoPostCount}</div>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4 space-y-4">
        {/* Primary Filters Row */}
        <div className="flex flex-wrap gap-4 items-center">
          <div className="flex items-center gap-2">
            <Filter className="h-4 w-4 text-gray-400" />
            <span className="text-sm font-medium text-gray-700">Filters:</span>
            {activeFilterCount > 0 && (
              <span className="inline-flex items-center justify-center px-2 py-0.5 text-xs font-medium bg-blue-100 text-blue-700 rounded-full">
                {activeFilterCount}
              </span>
            )}
          </div>

          <CompanyFilterDropdown
            value={companyFilter ?? ''}
            onChange={(value) => setCompanyFilter(value || '')}
          />

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="px-3 py-1.5 text-sm border border-gray-300 rounded-md"
          >
            <option value="">All Statuses</option>
            <option value="draft">Draft</option>
            <option value="posted">Posted</option>
            <option value="reversed">Reversed</option>
          </select>

          <select
            value={entryTypeFilter}
            onChange={(e) => setEntryTypeFilter(e.target.value)}
            className="px-3 py-1.5 text-sm border border-gray-300 rounded-md"
          >
            <option value="">All Types</option>
            <option value="manual">Manual</option>
            <option value="auto_post">Auto-Posted</option>
            <option value="reversal">Reversal</option>
            <option value="opening">Opening</option>
            <option value="closing">Closing</option>
          </select>

          <button
            onClick={() => setShowAdvancedFilters(!showAdvancedFilters)}
            className={`flex items-center gap-1 px-3 py-1.5 text-sm font-medium rounded-md border transition-colors ${
              showAdvancedFilters || fromDate || toDate || minAmount || maxAmount
                ? 'border-blue-300 bg-blue-50 text-blue-700'
                : 'border-gray-300 text-gray-600 hover:bg-gray-50'
            }`}
          >
            {showAdvancedFilters ? (
              <ChevronUp className="h-4 w-4" />
            ) : (
              <ChevronDown className="h-4 w-4" />
            )}
            More Filters
          </button>

          {activeFilterCount > 0 && (
            <button
              onClick={handleClearAllFilters}
              className="px-3 py-1.5 text-sm text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-md"
            >
              Clear All
            </button>
          )}

          <span className="text-sm text-gray-500 ml-auto">
            {entries.length} of {allEntries.length} entries
          </span>
        </div>

        {/* Advanced Filters (Date & Amount) */}
        {showAdvancedFilters && (
          <div className="pt-4 border-t border-gray-200 space-y-4">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <DateRangeFilter
                fromDate={fromDate}
                toDate={toDate}
                onFromDateChange={setFromDate}
                onToDateChange={setToDate}
                showQuickOptions={true}
              />
              <AmountRangeFilter
                minAmount={minAmount}
                maxAmount={maxAmount}
                onMinAmountChange={setMinAmount}
                onMaxAmountChange={setMaxAmount}
                currency="INR"
                showQuickOptions={true}
              />
            </div>
          </div>
        )}

        {/* Active Filter Tags */}
        {activeFilterCount > 0 && (
          <div className="flex flex-wrap gap-2 pt-2">
            {statusFilter && (
              <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium bg-gray-100 text-gray-700 rounded-full">
                Status: {statusConfig[statusFilter as JournalEntryStatus]?.label || statusFilter}
                <button
                  onClick={() => setStatusFilter('')}
                  className="hover:text-gray-900"
                >
                  <XCircle className="h-3 w-3" />
                </button>
              </span>
            )}
            {entryTypeFilter && (
              <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium bg-gray-100 text-gray-700 rounded-full">
                Type: {entryTypeConfig[entryTypeFilter as JournalEntryType]?.label || entryTypeFilter}
                <button
                  onClick={() => setEntryTypeFilter('')}
                  className="hover:text-gray-900"
                >
                  <XCircle className="h-3 w-3" />
                </button>
              </span>
            )}
            {fromDate && toDate && (
              <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium bg-blue-100 text-blue-700 rounded-full">
                Date: {fromDate} to {toDate}
                <button
                  onClick={() => {
                    setFromDate('')
                    setToDate('')
                  }}
                  className="hover:text-blue-900"
                >
                  <XCircle className="h-3 w-3" />
                </button>
              </span>
            )}
            {(minAmount || maxAmount) && (
              <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium bg-green-100 text-green-700 rounded-full">
                Amount: {minAmount ? `₹${Number(minAmount).toLocaleString('en-IN')}` : '₹0'} - {maxAmount ? `₹${Number(maxAmount).toLocaleString('en-IN')}` : 'Any'}
                <button
                  onClick={() => {
                    setMinAmount('')
                    setMaxAmount('')
                  }}
                  className="hover:text-green-900"
                >
                  <XCircle className="h-3 w-3" />
                </button>
              </span>
            )}
          </div>
        )}
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">

          {entries.length === 0 ? (
            <div className="text-center py-12">
              <FileText className="mx-auto h-12 w-12 text-gray-400" />
              <h3 className="mt-2 text-lg font-medium text-gray-900">No Journal Entries</h3>
              <p className="mt-1 text-sm text-gray-500">
                Create a manual journal entry or wait for auto-posting from business transactions.
              </p>
              <div className="mt-6">
                <button
                  onClick={() => setIsCreateDrawerOpen(true)}
                  className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
                >
                  Create Journal Entry
                </button>
              </div>
            </div>
          ) : (
            <DataTable
              columns={columns}
              data={entries}
              searchPlaceholder="Search entries..."
              onAdd={() => setIsCreateDrawerOpen(true)}
              addButtonText="New Entry"
            />
          )}
        </div>
      </div>

      {/* Create Journal Entry Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Journal Entry"
        size="xl"
      >
        <JournalEntryForm
          companyId={companyFilter}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* View Journal Entry Drawer */}
      <Drawer
        isOpen={!!viewingEntry}
        onClose={() => setViewingEntry(null)}
        title={`Journal Entry ${viewingEntry?.journalNumber || ''}`}
        size="lg"
      >
        {viewingEntry && (
          <JournalEntryDetail
            entryId={viewingEntry.id}
            onClose={() => setViewingEntry(null)}
          />
        )}
      </Drawer>

      {/* Post Confirmation Modal */}
      <Modal
        isOpen={!!postingEntry}
        onClose={() => setPostingEntry(null)}
        title="Post Journal Entry"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Are you sure you want to post journal entry{' '}
            <span className="font-medium text-gray-900">{postingEntry?.journalNumber}</span>?
          </p>
          <div className="bg-yellow-50 border border-yellow-200 rounded-md p-3">
            <div className="flex items-start gap-2">
              <AlertTriangle className="h-5 w-5 text-yellow-600 mt-0.5" />
              <div className="text-sm text-yellow-700">
                Once posted, this entry cannot be edited. It can only be reversed.
              </div>
            </div>
          </div>
          <div className="flex justify-end gap-3">
            <button
              onClick={() => setPostingEntry(null)}
              className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
            >
              Cancel
            </button>
            <button
              onClick={handlePost}
              disabled={postEntry.isPending}
              className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
            >
              {postEntry.isPending ? 'Posting...' : 'Post Entry'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Reverse Confirmation Modal */}
      <Modal
        isOpen={!!reversingEntry}
        onClose={() => {
          setReversingEntry(null)
          setReversalReason('')
        }}
        title="Reverse Journal Entry"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            This will create a new reversing entry for{' '}
            <span className="font-medium text-gray-900">{reversingEntry?.journalNumber}</span>.
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Reason for Reversal
            </label>
            <textarea
              value={reversalReason}
              onChange={(e) => setReversalReason(e.target.value)}
              rows={3}
              placeholder="Enter reason for reversal..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-primary focus:border-primary"
            />
          </div>
          <div className="flex justify-end gap-3">
            <button
              onClick={() => {
                setReversingEntry(null)
                setReversalReason('')
              }}
              className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
            >
              Cancel
            </button>
            <button
              onClick={handleReverse}
              disabled={reverseEntry.isPending}
              className="px-4 py-2 bg-orange-600 text-white rounded-md hover:bg-orange-700 disabled:opacity-50"
            >
              {reverseEntry.isPending ? 'Reversing...' : 'Reverse Entry'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

export default JournalEntriesManagement

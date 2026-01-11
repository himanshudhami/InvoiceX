import { useState, useMemo, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQueryStates, parseAsString } from 'nuqs'
import { ColumnDef } from '@tanstack/react-table'
import {
  useBankTransactionsByAccount,
  useBankTransactionsByAccountDateRange,
  useBankTransactionSummary,
  useUnreconcileTransaction,
  useBankTransaction,
} from '@/hooks/api/useBankTransactions'
import { useBankAccounts } from '@/hooks/api/useBankAccounts'
import { useCompanies } from '@/hooks/api/useCompanies'
import { BankTransaction } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { ReconciliationDrawer } from '@/components/banking/ReconciliationDrawer'
import { ReversalPairingDrawer } from '@/components/banking/ReversalPairingDrawer'
import { ReconcileToJournalDialog } from '@/components/banking/ReconcileToJournalDialog'
import { CompanySelect } from '@/components/ui/CompanySelect'
import { DateRangeFilter } from '@/components/filters/DateRangeFilter'
import { AmountRangeFilter } from '@/components/filters/AmountRangeFilter'
import {
  ArrowLeft,
  CheckCircle,
  XCircle,
  Link2,
  Unlink,
  TrendingUp,
  TrendingDown,
  RefreshCw,
  Filter,
  Search,
  AlertTriangle,
  FileText,
  BarChart3,
  Receipt,
  ChevronDown,
  ChevronUp,
} from 'lucide-react'

// Simple reversal detection for UI (matches backend ReversalDetector patterns)
const isLikelyReversal = (transaction: BankTransaction): boolean => {
  if (transaction.transactionType !== 'credit') return false
  if (transaction.isReversalTransaction) return true

  const desc = (transaction.description || '').toUpperCase()
  const reversalPatterns = [
    /^REV[-/\s]/,
    /^REVERSAL/,
    /^R[-/]/,
    /^RV[-/]/,
    /NEFT[-\s]REV/,
    /RTGS[-\s]REV/,
    /REV[-/]UPI/,
    /UPI[-\s]REV/,
    /CHQ[-\s]?RET/,
    /CHEQUE[-\s]RETURN/,
    /NACH[-\s]RETURN/,
    /CHARGEBACK/,
    /^REFUND/,
  ]

  return reversalPatterns.some(pattern => pattern.test(desc))
}

// Helper to get navigation URL for reconciled source
const getReconciledSourceUrl = (reconciledType?: string, reconciledId?: string): string | null => {
  if (!reconciledType || !reconciledId) return null

  const typeRoutes: Record<string, string> = {
    contractor: '/finance/ap/contractor-payments',
    vendor: '/finance/ap/vendor-payments',
    payment: '/finance/ar/customer-receipts',
    expense: '/finance/expenses',
    payroll: '/hr/payroll/transactions',
    tax_payment: '/finance/statutory/tds-payments',
    transfer: '/finance/bank/transfers',
  }

  const route = typeRoutes[reconciledType.toLowerCase()]
  if (!route) return null

  return `${route}?highlight=${reconciledId}`
}

// Display label for reconciled type
const getReconciledTypeLabel = (reconciledType?: string): string => {
  const labels: Record<string, string> = {
    contractor: 'Contractor Payment',
    vendor: 'Vendor Payment',
    payment: 'Customer Receipt',
    expense: 'Expense',
    payroll: 'Payroll',
    tax_payment: 'Tax Payment',
    transfer: 'Bank Transfer',
  }
  return labels[reconciledType?.toLowerCase() || ''] || 'Source'
}

const BankTransactionsPage = () => {
  const navigate = useNavigate()

  // URL state management with nuqs
  const [urlState, setUrlState] = useQueryStates({
    accountId: parseAsString.withDefault(''),
    companyId: parseAsString.withDefault(''),
    status: parseAsString.withDefault('all'),
    type: parseAsString.withDefault('all'),
    search: parseAsString.withDefault(''),
    highlight: parseAsString.withDefault(''),
    fromDate: parseAsString.withDefault(''),
    toDate: parseAsString.withDefault(''),
    minAmount: parseAsString.withDefault(''),
    maxAmount: parseAsString.withDefault(''),
  })

  const {
    accountId: selectedAccountId,
    companyId: selectedCompanyId,
    status: filterStatus,
    type: filterType,
    search: searchTerm,
    highlight: highlightId,
    fromDate,
    toDate,
    minAmount,
    maxAmount,
  } = urlState

  // Setters that update URL
  const setSelectedAccountId = (id: string) => setUrlState({ accountId: id })
  const setSelectedCompanyId = (id: string) => setUrlState({ companyId: id })
  const setFilterStatus = (status: string) => setUrlState({ status })
  const setFilterType = (type: string) => setUrlState({ type })
  const setSearchTerm = (search: string) => setUrlState({ search })
  const setFromDate = (date: string) => setUrlState({ fromDate: date })
  const setToDate = (date: string) => setUrlState({ toDate: date })
  const setMinAmount = (amount: string) => setUrlState({ minAmount: amount })
  const setMaxAmount = (amount: string) => setUrlState({ maxAmount: amount })

  // State for expanded filters - auto-expand if URL has date/amount filters
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(() => {
    return !!(fromDate || toDate || minAmount || maxAmount)
  })

  // Fetch highlighted transaction to get its bank account (when navigating from other pages)
  const { data: highlightedTransaction } = useBankTransaction(
    highlightId || '',
    !!highlightId && !selectedAccountId
  )

  // Auto-select account from highlighted transaction
  useEffect(() => {
    if (highlightedTransaction?.bankAccountId && !selectedAccountId) {
      setSelectedAccountId(highlightedTransaction.bankAccountId)
    }
  }, [highlightedTransaction?.bankAccountId, selectedAccountId])

  const [reconcilingTransaction, setReconcilingTransaction] = useState<BankTransaction | null>(null)
  const [reversalTransaction, setReversalTransaction] = useState<BankTransaction | null>(null)
  const [journalReconcilingTransaction, setJournalReconcilingTransaction] = useState<BankTransaction | null>(null)
  const [showBrsReport, setShowBrsReport] = useState(false)

  // Handle reconciliation click - detect if it's a reversal
  const handleReconcileClick = (transaction: BankTransaction) => {
    if (isLikelyReversal(transaction)) {
      setReversalTransaction(transaction)
    } else {
      setReconcilingTransaction(transaction)
    }
  }

  const { data: bankAccounts = [], isLoading: accountsLoading } = useBankAccounts()
  const { data: companies = [] } = useCompanies()

  // Use date range query when both dates are provided, otherwise fetch all
  const hasDateFilter = fromDate && toDate
  const {
    data: allTransactions = [],
    isLoading: allTransactionsLoading,
    refetch: refetchAll,
  } = useBankTransactionsByAccount(
    selectedAccountId,
    !!selectedAccountId && !hasDateFilter
  )
  const {
    data: dateFilteredTransactions = [],
    isLoading: dateFilteredLoading,
    refetch: refetchDateFiltered,
  } = useBankTransactionsByAccountDateRange(
    selectedAccountId,
    fromDate,
    toDate,
    !!selectedAccountId && !!hasDateFilter
  )

  // Combine results based on which query is active
  const transactions = hasDateFilter ? dateFilteredTransactions : allTransactions
  const transactionsLoading = hasDateFilter ? dateFilteredLoading : allTransactionsLoading
  const refetch = hasDateFilter ? refetchDateFiltered : refetchAll

  const { data: summary } = useBankTransactionSummary(selectedAccountId, !!selectedAccountId)

  const unreconcileTransaction = useUnreconcileTransaction()

  const selectedAccount = bankAccounts.find(a => a.id === selectedAccountId)
  const availableCompanies = useMemo(() => {
    const ids = new Set(bankAccounts.map((a) => a.companyId).filter(Boolean))
    return companies.filter((c) => ids.has(c.id))
  }, [bankAccounts, companies])
  const filteredAccounts = useMemo(
    () => (selectedCompanyId ? bankAccounts.filter((a) => a.companyId === selectedCompanyId) : bankAccounts),
    [bankAccounts, selectedCompanyId]
  )

  // When an account is preselected, align the company filter to match
  useEffect(() => {
    if (selectedAccount && selectedAccount.companyId && selectedCompanyId !== selectedAccount.companyId) {
      setSelectedCompanyId(selectedAccount.companyId)
    }
  }, [selectedAccount, selectedCompanyId])

  // Filter transactions
  const filteredTransactions = useMemo(() => {
    return transactions.filter(t => {
      // Status filter
      if (filterStatus === 'reconciled' && !t.isReconciled) return false
      if (filterStatus === 'unreconciled' && t.isReconciled) return false

      // Type filter
      if (filterType === 'credit' && t.transactionType !== 'credit') return false
      if (filterType === 'debit' && t.transactionType !== 'debit') return false

      // Amount filter
      const amount = Number(t.amount || 0)
      if (minAmount && amount < parseFloat(minAmount)) return false
      if (maxAmount && amount > parseFloat(maxAmount)) return false

      // Search filter
      if (searchTerm) {
        const search = searchTerm.toLowerCase()
        return (
          t.description?.toLowerCase().includes(search) ||
          t.referenceNumber?.toLowerCase().includes(search) ||
          t.chequeNumber?.toLowerCase().includes(search)
        )
      }

      return true
    })
  }, [transactions, filterStatus, filterType, searchTerm, minAmount, maxAmount])

  // Count active filters for badge
  const activeFilterCount = useMemo(() => {
    let count = 0
    if (filterStatus !== 'all') count++
    if (filterType !== 'all') count++
    if (searchTerm) count++
    if (fromDate && toDate) count++
    if (minAmount || maxAmount) count++
    return count
  }, [filterStatus, filterType, searchTerm, fromDate, toDate, minAmount, maxAmount])

  // Clear all filters
  const handleClearAllFilters = () => {
    setUrlState({
      status: 'all',
      type: 'all',
      search: '',
      fromDate: '',
      toDate: '',
      minAmount: '',
      maxAmount: '',
    })
  }

  const { reconciledAmount, unreconciledAmount } = useMemo(() => {
    return transactions.reduce(
      (totals, transaction) => {
        const amount = Number(transaction.amount || 0)
        if (transaction.isReconciled) {
          totals.reconciledAmount += amount
        } else {
          totals.unreconciledAmount += amount
        }
        return totals
      },
      { reconciledAmount: 0, unreconciledAmount: 0 }
    )
  }, [transactions])

  // Compute totals for footer (from filtered transactions)
  const totals = useMemo(() => {
    if (!filteredTransactions.length) return null
    return filteredTransactions.reduce(
      (acc, t) => {
        const amount = Number(t.amount || 0)
        if (t.transactionType === 'credit') {
          acc.totalCredits += amount
        } else {
          acc.totalDebits += amount
        }
        return acc
      },
      { totalCredits: 0, totalDebits: 0 }
    )
  }, [filteredTransactions])

  const handleUnreconcile = async (transaction: BankTransaction) => {
    try {
      await unreconcileTransaction.mutateAsync(transaction.id)
      refetch()
    } catch (error) {
      console.error('Failed to unreconcile:', error)
    }
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: selectedAccount?.currency || 'INR',
      maximumFractionDigits: 2,
    }).format(amount)
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    })
  }

  // Define columns for DataTable
  const columns: ColumnDef<BankTransaction>[] = useMemo(() => [
    {
      accessorKey: 'transactionDate',
      header: 'Date',
      cell: ({ row }) => formatDate(row.original.transactionDate),
    },
    {
      accessorKey: 'description',
      header: 'Description',
      cell: ({ row }) => (
        <div className="max-w-xs truncate" title={row.original.description || ''}>
          {row.original.description || '-'}
        </div>
      ),
    },
    {
      accessorKey: 'referenceNumber',
      header: 'Reference',
      cell: ({ row }) => (
        <span className="text-gray-500 font-mono text-xs">
          {row.original.referenceNumber || row.original.chequeNumber || '-'}
        </span>
      ),
    },
    {
      accessorKey: 'transactionType',
      header: 'Type',
      meta: { align: 'center' as const },
      cell: ({ row }) => (
        <span className={`inline-flex px-2 py-0.5 text-xs font-medium rounded ${
          row.original.transactionType === 'credit'
            ? 'bg-green-100 text-green-700'
            : 'bg-red-100 text-red-700'
        }`}>
          {row.original.transactionType === 'credit' ? 'CR' : 'DR'}
        </span>
      ),
    },
    {
      accessorKey: 'amount',
      header: 'Amount',
      meta: { align: 'right' as const, numeric: true },
      cell: ({ row }) => (
        <span className={`font-medium ${
          row.original.transactionType === 'credit' ? 'text-green-600' : 'text-red-600'
        }`}>
          {row.original.transactionType === 'credit' ? '+' : '-'}
          {formatCurrency(row.original.amount)}
        </span>
      ),
    },
    {
      accessorKey: 'balanceAfter',
      header: 'Balance',
      meta: { align: 'right' as const, numeric: true },
      cell: ({ row }) => (
        <span className="text-gray-500">
          {row.original.balanceAfter ? formatCurrency(row.original.balanceAfter) : '-'}
        </span>
      ),
    },
    {
      accessorKey: 'isReconciled',
      header: 'Status',
      meta: { align: 'center' as const },
      cell: ({ row }) => row.original.isReconciled ? (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded bg-green-100 text-green-700">
          <CheckCircle className="h-3 w-3" />
          Reconciled
        </span>
      ) : (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded bg-yellow-100 text-yellow-700">
          <XCircle className="h-3 w-3" />
          Pending
        </span>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      meta: { align: 'center' as const },
      cell: ({ row }) => {
        const transaction = row.original
        const sourceUrl = getReconciledSourceUrl(transaction.reconciledType, transaction.reconciledId)

        return transaction.isReconciled ? (
          <div className="flex items-center justify-center gap-1">
            {transaction.reconciledJournalEntryId && (
              <span className="text-xs text-purple-600" title="Linked to Journal Entry">
                <FileText className="h-3 w-3" />
              </span>
            )}
            {sourceUrl && (
              <button
                onClick={() => navigate(sourceUrl)}
                className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50"
                title={`View ${getReconciledTypeLabel(transaction.reconciledType)}`}
              >
                <Receipt className="h-4 w-4" />
              </button>
            )}
            <button
              onClick={() => handleUnreconcile(transaction)}
              disabled={unreconcileTransaction.isPending}
              className="text-gray-500 hover:text-red-600 p-1 rounded hover:bg-red-50"
              title="Unreconcile"
            >
              <Unlink className="h-4 w-4" />
            </button>
          </div>
        ) : (
          <div className="flex items-center justify-center gap-1">
            <button
              onClick={() => handleReconcileClick(transaction)}
              className={`p-1 rounded ${
                isLikelyReversal(transaction)
                  ? 'text-amber-600 hover:text-amber-800 hover:bg-amber-50'
                  : 'text-blue-600 hover:text-blue-800 hover:bg-blue-50'
              }`}
              title={isLikelyReversal(transaction) ? 'Pair Reversal' : 'Reconcile to Source'}
            >
              {isLikelyReversal(transaction) ? (
                <AlertTriangle className="h-4 w-4" />
              ) : (
                <Link2 className="h-4 w-4" />
              )}
            </button>
            <button
              onClick={() => setJournalReconcilingTransaction(transaction)}
              className="p-1 rounded text-purple-600 hover:text-purple-800 hover:bg-purple-50"
              title="Reconcile to Journal Entry"
            >
              <FileText className="h-4 w-4" />
            </button>
          </div>
        )
      },
    },
  ], [formatCurrency, navigate, unreconcileTransaction.isPending])

  if (accountsLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to="/bank/accounts"
          className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg"
        >
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Bank Transactions</h1>
          <p className="text-gray-600 mt-1">View and reconcile bank transactions</p>
        </div>
      </div>

      {/* Account Selector */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex flex-wrap gap-4 items-end">
          <div className="flex-1 min-w-[220px]">
            <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
            <CompanySelect
              companies={availableCompanies}
              value={selectedCompanyId}
              onChange={(value) => {
                setSelectedCompanyId(value)
                setSelectedAccountId('')
              }}
              placeholder="Filter by company"
              showAllOption
              allOptionLabel="All companies"
              className="w-full"
            />
          </div>
          <div className="flex-1 min-w-[280px]">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Select Bank Account
            </label>
            <select
              value={selectedAccountId}
              onChange={(e) => setSelectedAccountId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">Choose an account...</option>
              {filteredAccounts
                .filter((a) => a.isActive)
                .map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.accountName} - {account.bankName} ({account.accountNumber.slice(-4)})
                  </option>
                ))}
            </select>
          </div>
          {selectedAccountId && (
            <>
              <button
                onClick={() => refetch()}
                className="px-4 py-2 text-gray-600 hover:text-gray-800 hover:bg-gray-100 rounded-md flex items-center gap-2"
              >
                <RefreshCw className="h-4 w-4" />
                Refresh
              </button>
              <Link
                to={`/bank/brs/${selectedAccountId}`}
                className="px-4 py-2 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded-md flex items-center gap-2"
              >
                <BarChart3 className="h-4 w-4" />
                View BRS
              </Link>
            </>
          )}
        </div>
      </div>

      {selectedAccountId && (
        <>
          {/* Summary Cards */}
          {summary && (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-5 gap-4">
              <div className="bg-white rounded-lg shadow p-4">
                <p className="text-sm text-gray-500">Total Transactions</p>
                <p className="text-2xl font-bold text-gray-900">{summary.totalCount}</p>
                <p className="text-xs text-gray-400 mt-1">
                  {summary.reconciledCount} reconciled
                </p>
              </div>
              <div className="bg-white rounded-lg shadow p-4">
                <div className="flex items-center gap-2">
                  <TrendingUp className="h-4 w-4 text-green-500" />
                  <p className="text-sm text-gray-500">Total Credits</p>
                </div>
                <p className="text-2xl font-bold text-green-600">{formatCurrency(summary.totalCredits)}</p>
              </div>
              <div className="bg-white rounded-lg shadow p-4">
                <div className="flex items-center gap-2">
                  <TrendingDown className="h-4 w-4 text-red-500" />
                  <p className="text-sm text-gray-500">Total Debits</p>
                </div>
                <p className="text-2xl font-bold text-red-600">{formatCurrency(summary.totalDebits)}</p>
              </div>
              <div className="bg-white rounded-lg shadow p-4">
                <p className="text-sm text-gray-500">Reconciliation Amounts</p>
                <div className="mt-2 space-y-2">
                  <div className="flex items-center justify-between text-sm">
                    <span className="flex items-center gap-1 text-gray-500">
                      <CheckCircle className="h-3.5 w-3.5 text-emerald-500" />
                      Reconciled
                    </span>
                    <span className="font-semibold text-emerald-600">
                      {formatCurrency(reconciledAmount)}
                    </span>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="flex items-center gap-1 text-gray-500">
                      <XCircle className="h-3.5 w-3.5 text-amber-500" />
                      Unreconciled
                    </span>
                    <span className="font-semibold text-amber-600">
                      {formatCurrency(unreconciledAmount)}
                    </span>
                  </div>
                </div>
              </div>
              <div className="bg-white rounded-lg shadow p-4">
                <p className="text-sm text-gray-500">Reconciliation</p>
                <p className="text-2xl font-bold text-blue-600">{summary.reconciliationPercentage.toFixed(1)}%</p>
                <div className="w-full bg-gray-200 rounded-full h-2 mt-2">
                  <div
                    className="bg-blue-600 h-2 rounded-full"
                    style={{ width: `${summary.reconciliationPercentage}%` }}
                  />
                </div>
              </div>
            </div>
          )}

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

              <select
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value as typeof filterStatus)}
                className="px-3 py-1.5 text-sm border border-gray-300 rounded-md"
              >
                <option value="all">All Status</option>
                <option value="reconciled">Reconciled</option>
                <option value="unreconciled">Unreconciled</option>
              </select>

              <select
                value={filterType}
                onChange={(e) => setFilterType(e.target.value as typeof filterType)}
                className="px-3 py-1.5 text-sm border border-gray-300 rounded-md"
              >
                <option value="all">All Types</option>
                <option value="credit">Credits Only</option>
                <option value="debit">Debits Only</option>
              </select>

              <div className="flex-1 min-w-[200px] relative">
                <Search className="h-4 w-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
                <input
                  type="text"
                  placeholder="Search description, reference..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full pl-9 pr-3 py-1.5 text-sm border border-gray-300 rounded-md"
                />
              </div>

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
                {filteredTransactions.length} of {transactions.length} transactions
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
                    currency={selectedAccount?.currency || 'INR'}
                    showQuickOptions={true}
                  />
                </div>
              </div>
            )}

            {/* Active Filter Tags */}
            {activeFilterCount > 0 && (
              <div className="flex flex-wrap gap-2 pt-2">
                {filterStatus !== 'all' && (
                  <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium bg-gray-100 text-gray-700 rounded-full">
                    Status: {filterStatus}
                    <button
                      onClick={() => setFilterStatus('all')}
                      className="hover:text-gray-900"
                    >
                      <XCircle className="h-3 w-3" />
                    </button>
                  </span>
                )}
                {filterType !== 'all' && (
                  <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium bg-gray-100 text-gray-700 rounded-full">
                    Type: {filterType}
                    <button
                      onClick={() => setFilterType('all')}
                      className="hover:text-gray-900"
                    >
                      <XCircle className="h-3 w-3" />
                    </button>
                  </span>
                )}
                {searchTerm && (
                  <span className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium bg-gray-100 text-gray-700 rounded-full">
                    Search: "{searchTerm}"
                    <button
                      onClick={() => setSearchTerm('')}
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

          {/* Transactions Table */}
          <div className="bg-white rounded-lg shadow p-6">
            <DataTable
              columns={columns}
              data={filteredTransactions}
              showToolbar={false}
              isLoading={transactionsLoading}
              highlightedRowId={highlightId || undefined}
              totalsFooter={totals ? {
                label: 'Total',
                values: [
                  { label: 'Credits', value: <span className="text-green-600">+{formatCurrency(totals.totalCredits)}</span> },
                  { label: 'Debits', value: <span className="text-red-600">-{formatCurrency(totals.totalDebits)}</span> },
                ]
              } : undefined}
            />
          </div>
        </>
      )}

      {/* Reconciliation Drawer */}
      <ReconciliationDrawer
        transaction={reconcilingTransaction}
        companyId={selectedAccount?.companyId || selectedCompanyId}
        onClose={() => setReconcilingTransaction(null)}
        onReconciled={() => {
          setReconcilingTransaction(null)
          refetch()
        }}
        formatCurrency={formatCurrency}
      />

      {/* Reversal Pairing Drawer */}
      <ReversalPairingDrawer
        transaction={reversalTransaction}
        onClose={() => setReversalTransaction(null)}
        onPaired={() => {
          setReversalTransaction(null)
          refetch()
        }}
        formatCurrency={formatCurrency}
      />

      {/* Reconcile to Journal Entry Dialog */}
      <ReconcileToJournalDialog
        transaction={journalReconcilingTransaction}
        companyId={selectedAccount?.companyId || selectedCompanyId}
        linkedAccountId={selectedAccount?.linkedAccountId}
        onClose={() => setJournalReconcilingTransaction(null)}
        onReconciled={() => {
          setJournalReconcilingTransaction(null)
          refetch()
        }}
        formatCurrency={formatCurrency}
      />
    </div>
  )
}

export default BankTransactionsPage

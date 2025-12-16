import { useState, useMemo, useEffect } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import {
  useBankTransactionsByAccount,
  useBankTransactionSummary,
  useUnreconcileTransaction,
  useReconcileTransaction,
  useReconciliationSuggestions
} from '@/hooks/api/useBankTransactions'
import { useBankAccounts } from '@/hooks/api/useBankAccounts'
import { BankTransaction, ReconciliationSuggestion } from '@/services/api/types'
import { Modal } from '@/components/ui/Modal'
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
  Search
} from 'lucide-react'

const BankTransactionsPage = () => {
  const [searchParams] = useSearchParams()
  const preselectedAccountId = searchParams.get('accountId')

  const [selectedAccountId, setSelectedAccountId] = useState<string>(preselectedAccountId || '')
  const [filterStatus, setFilterStatus] = useState<'all' | 'reconciled' | 'unreconciled'>('all')
  const [filterType, setFilterType] = useState<'all' | 'credit' | 'debit'>('all')
  const [searchTerm, setSearchTerm] = useState('')

  // Auto-select account from URL parameter
  useEffect(() => {
    if (preselectedAccountId && !selectedAccountId) {
      setSelectedAccountId(preselectedAccountId)
    }
  }, [preselectedAccountId, selectedAccountId])
  const [reconcilingTransaction, setReconcilingTransaction] = useState<BankTransaction | null>(null)

  const { data: bankAccounts = [], isLoading: accountsLoading } = useBankAccounts()
  const { data: transactions = [], isLoading: transactionsLoading, refetch } = useBankTransactionsByAccount(
    selectedAccountId,
    !!selectedAccountId
  )
  const { data: summary } = useBankTransactionSummary(selectedAccountId, !!selectedAccountId)

  const unreconcileTransaction = useUnreconcileTransaction()

  const selectedAccount = bankAccounts.find(a => a.id === selectedAccountId)

  // Filter transactions
  const filteredTransactions = useMemo(() => {
    return transactions.filter(t => {
      // Status filter
      if (filterStatus === 'reconciled' && !t.isReconciled) return false
      if (filterStatus === 'unreconciled' && t.isReconciled) return false

      // Type filter
      if (filterType === 'credit' && t.transactionType !== 'credit') return false
      if (filterType === 'debit' && t.transactionType !== 'debit') return false

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
  }, [transactions, filterStatus, filterType, searchTerm])

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
          <div className="flex-1 min-w-[250px]">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Select Bank Account
            </label>
            <select
              value={selectedAccountId}
              onChange={(e) => setSelectedAccountId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">Choose an account...</option>
              {bankAccounts.filter(a => a.isActive).map((account) => (
                <option key={account.id} value={account.id}>
                  {account.accountName} - {account.bankName} ({account.accountNumber.slice(-4)})
                </option>
              ))}
            </select>
          </div>
          {selectedAccountId && (
            <button
              onClick={() => refetch()}
              className="px-4 py-2 text-gray-600 hover:text-gray-800 hover:bg-gray-100 rounded-md flex items-center gap-2"
            >
              <RefreshCw className="h-4 w-4" />
              Refresh
            </button>
          )}
        </div>
      </div>

      {selectedAccountId && (
        <>
          {/* Summary Cards */}
          {summary && (
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
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
          <div className="bg-white rounded-lg shadow p-4">
            <div className="flex flex-wrap gap-4 items-center">
              <div className="flex items-center gap-2">
                <Filter className="h-4 w-4 text-gray-400" />
                <span className="text-sm font-medium text-gray-700">Filters:</span>
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

              <span className="text-sm text-gray-500">
                {filteredTransactions.length} of {transactions.length} transactions
              </span>
            </div>
          </div>

          {/* Transactions Table */}
          <div className="bg-white rounded-lg shadow overflow-hidden">
            {transactionsLoading ? (
              <div className="flex items-center justify-center h-64">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
              </div>
            ) : filteredTransactions.length === 0 ? (
              <div className="text-center py-12 text-gray-500">
                {transactions.length === 0
                  ? 'No transactions found. Import a bank statement to get started.'
                  : 'No transactions match your filters.'
                }
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50 border-b">
                    <tr>
                      <th className="px-4 py-3 text-left font-medium text-gray-500">Date</th>
                      <th className="px-4 py-3 text-left font-medium text-gray-500">Description</th>
                      <th className="px-4 py-3 text-left font-medium text-gray-500">Reference</th>
                      <th className="px-4 py-3 text-center font-medium text-gray-500">Type</th>
                      <th className="px-4 py-3 text-right font-medium text-gray-500">Amount</th>
                      <th className="px-4 py-3 text-right font-medium text-gray-500">Balance</th>
                      <th className="px-4 py-3 text-center font-medium text-gray-500">Status</th>
                      <th className="px-4 py-3 text-center font-medium text-gray-500">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {filteredTransactions.map((transaction) => (
                      <tr key={transaction.id} className="hover:bg-gray-50">
                        <td className="px-4 py-3 whitespace-nowrap">
                          {formatDate(transaction.transactionDate)}
                        </td>
                        <td className="px-4 py-3 max-w-xs">
                          <div className="truncate" title={transaction.description}>
                            {transaction.description || '-'}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-gray-500 font-mono text-xs">
                          {transaction.referenceNumber || transaction.chequeNumber || '-'}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <span className={`inline-flex px-2 py-0.5 text-xs font-medium rounded ${
                            transaction.transactionType === 'credit'
                              ? 'bg-green-100 text-green-700'
                              : 'bg-red-100 text-red-700'
                          }`}>
                            {transaction.transactionType === 'credit' ? 'CR' : 'DR'}
                          </span>
                        </td>
                        <td className={`px-4 py-3 text-right font-medium ${
                          transaction.transactionType === 'credit' ? 'text-green-600' : 'text-red-600'
                        }`}>
                          {transaction.transactionType === 'credit' ? '+' : '-'}
                          {formatCurrency(transaction.amount)}
                        </td>
                        <td className="px-4 py-3 text-right text-gray-500">
                          {transaction.balanceAfter ? formatCurrency(transaction.balanceAfter) : '-'}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {transaction.isReconciled ? (
                            <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded bg-green-100 text-green-700">
                              <CheckCircle className="h-3 w-3" />
                              Reconciled
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded bg-yellow-100 text-yellow-700">
                              <XCircle className="h-3 w-3" />
                              Pending
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {transaction.isReconciled ? (
                            <button
                              onClick={() => handleUnreconcile(transaction)}
                              disabled={unreconcileTransaction.isPending}
                              className="text-gray-500 hover:text-red-600 p-1 rounded hover:bg-red-50"
                              title="Unreconcile"
                            >
                              <Unlink className="h-4 w-4" />
                            </button>
                          ) : (
                            <button
                              onClick={() => setReconcilingTransaction(transaction)}
                              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50"
                              title="Reconcile"
                            >
                              <Link2 className="h-4 w-4" />
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}

      {/* Reconciliation Modal */}
      <ReconciliationModal
        transaction={reconcilingTransaction}
        onClose={() => setReconcilingTransaction(null)}
        onReconciled={() => {
          setReconcilingTransaction(null)
          refetch()
        }}
        formatCurrency={formatCurrency}
      />
    </div>
  )
}

// Reconciliation Modal Component
interface ReconciliationModalProps {
  transaction: BankTransaction | null
  onClose: () => void
  onReconciled: () => void
  formatCurrency: (amount: number) => string
}

const ReconciliationModal = ({ transaction, onClose, onReconciled, formatCurrency }: ReconciliationModalProps) => {
  const [manualType, setManualType] = useState<string>('payment')
  const [manualId, setManualId] = useState<string>('')

  const { data: suggestions = [], isLoading: suggestionsLoading } = useReconciliationSuggestions(
    transaction?.id || '',
    100, // tolerance in amount
    10,  // max results
    !!transaction && transaction.transactionType === 'credit'
  )

  const reconcileTransaction = useReconcileTransaction()

  const handleReconcileWithSuggestion = async (suggestion: ReconciliationSuggestion) => {
    if (!transaction) return

    try {
      await reconcileTransaction.mutateAsync({
        id: transaction.id,
        data: {
          reconciledType: 'payment',
          reconciledId: suggestion.paymentId,
          reconciledBy: 'user'
        }
      })
      onReconciled()
    } catch (error) {
      console.error('Failed to reconcile:', error)
    }
  }

  const handleManualReconcile = async () => {
    if (!transaction || !manualId) return

    try {
      await reconcileTransaction.mutateAsync({
        id: transaction.id,
        data: {
          reconciledType: manualType,
          reconciledId: manualId,
          reconciledBy: 'user'
        }
      })
      onReconciled()
    } catch (error) {
      console.error('Failed to reconcile:', error)
    }
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    })
  }

  if (!transaction) return null

  return (
    <Modal
      isOpen={!!transaction}
      onClose={onClose}
      title="Reconcile Transaction"
      size="lg"
    >
      <div className="space-y-6">
        {/* Transaction Details */}
        <div className="bg-gray-50 rounded-lg p-4">
          <h4 className="text-sm font-medium text-gray-700 mb-2">Transaction Details</h4>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-500">Date:</span>
              <span className="ml-2 font-medium">{formatDate(transaction.transactionDate)}</span>
            </div>
            <div>
              <span className="text-gray-500">Amount:</span>
              <span className={`ml-2 font-medium ${
                transaction.transactionType === 'credit' ? 'text-green-600' : 'text-red-600'
              }`}>
                {formatCurrency(transaction.amount)}
              </span>
            </div>
            <div className="col-span-2">
              <span className="text-gray-500">Description:</span>
              <span className="ml-2">{transaction.description || '-'}</span>
            </div>
            {transaction.referenceNumber && (
              <div className="col-span-2">
                <span className="text-gray-500">Reference:</span>
                <span className="ml-2 font-mono text-xs">{transaction.referenceNumber}</span>
              </div>
            )}
          </div>
        </div>

        {/* Suggestions (only for credits/receipts) */}
        {transaction.transactionType === 'credit' && (
          <div>
            <h4 className="text-sm font-medium text-gray-700 mb-3">
              Matching Payments
              {suggestionsLoading && <span className="ml-2 text-gray-400">(Loading...)</span>}
            </h4>

            {suggestions.length === 0 && !suggestionsLoading ? (
              <p className="text-sm text-gray-500 italic">No matching payments found</p>
            ) : (
              <div className="space-y-2 max-h-64 overflow-y-auto">
                {suggestions.map((suggestion) => (
                  <div
                    key={suggestion.paymentId}
                    className="border rounded-lg p-3 hover:border-blue-300 hover:bg-blue-50 cursor-pointer transition-colors"
                    onClick={() => handleReconcileWithSuggestion(suggestion)}
                  >
                    <div className="flex justify-between items-start">
                      <div>
                        <div className="font-medium text-gray-900">
                          {suggestion.customerName || 'Unknown Customer'}
                        </div>
                        <div className="text-sm text-gray-500">
                          {suggestion.invoiceNumber && `Invoice: ${suggestion.invoiceNumber} • `}
                          {formatDate(suggestion.paymentDate)}
                        </div>
                        {suggestion.referenceNumber && (
                          <div className="text-xs text-gray-400 font-mono mt-1">
                            Ref: {suggestion.referenceNumber}
                          </div>
                        )}
                      </div>
                      <div className="text-right">
                        <div className="font-medium text-green-600">
                          {formatCurrency(suggestion.amount)}
                        </div>
                        <div className={`text-xs ${
                          suggestion.matchScore >= 80 ? 'text-green-600' :
                          suggestion.matchScore >= 50 ? 'text-yellow-600' : 'text-red-600'
                        }`}>
                          {suggestion.matchScore}% match
                        </div>
                      </div>
                    </div>
                    {suggestion.amountDifference !== 0 && (
                      <div className="text-xs text-orange-600 mt-1">
                        Amount difference: {formatCurrency(Math.abs(suggestion.amountDifference))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Manual Reconciliation */}
        <div className="border-t pt-4">
          <h4 className="text-sm font-medium text-gray-700 mb-3">Manual Reconciliation</h4>
          <div className="flex gap-3">
            <select
              value={manualType}
              onChange={(e) => setManualType(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-md text-sm"
            >
              <option value="payment">Payment</option>
              <option value="expense">Expense</option>
              <option value="payroll">Payroll</option>
              <option value="tax_payment">Tax Payment</option>
              <option value="transfer">Transfer</option>
              <option value="contractor">Contractor Payment</option>
              <option value="other">Other</option>
            </select>
            <input
              type="text"
              placeholder="Enter ID or reference"
              value={manualId}
              onChange={(e) => setManualId(e.target.value)}
              className="flex-1 px-3 py-2 border border-gray-300 rounded-md text-sm"
            />
            <button
              onClick={handleManualReconcile}
              disabled={!manualId || reconcileTransaction.isPending}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 text-sm"
            >
              {reconcileTransaction.isPending ? 'Saving...' : 'Reconcile'}
            </button>
          </div>
        </div>

        {/* Close Button */}
        <div className="flex justify-end pt-2">
          <button
            onClick={onClose}
            className="px-4 py-2 text-gray-600 hover:text-gray-800"
          >
            Cancel
          </button>
        </div>
      </div>
    </Modal>
  )
}

export default BankTransactionsPage

import { useState, useMemo } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { useEnhancedBrs } from '@/hooks/api/useBankTransactions'
import { useBankAccounts } from '@/hooks/api/useBankAccounts'
import {
  ArrowLeft,
  RefreshCw,
  Download,
  CheckCircle,
  AlertTriangle,
  TrendingUp,
  TrendingDown,
  FileText,
  Scale,
  Receipt,
  AlertCircle
} from 'lucide-react'

const BankReconciliationStatement = () => {
  const { bankAccountId } = useParams<{ bankAccountId: string }>()
  const [searchParams] = useSearchParams()

  // Get date params or default to current date
  const defaultDate = new Date().toISOString().split('T')[0]
  const [asOfDate, setAsOfDate] = useState(searchParams.get('asOfDate') || defaultDate)
  const [periodStart, setPeriodStart] = useState(searchParams.get('periodStart') || '')

  const { data: bankAccounts = [] } = useBankAccounts()
  const selectedAccount = bankAccounts.find(a => a.id === bankAccountId)

  const { data: brsReport, isLoading, refetch, error } = useEnhancedBrs(
    bankAccountId || '',
    asOfDate,
    periodStart || undefined,
    !!bankAccountId
  )

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

  // Calculate if the BRS is balanced
  const isBalanced = useMemo(() => {
    if (!brsReport) return false
    return Math.abs(brsReport.adjustedBankBalance - brsReport.adjustedBookBalance) < 0.01
  }, [brsReport])

  if (!bankAccountId) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <AlertCircle className="h-12 w-12 text-gray-300 mx-auto mb-3" />
          <p className="text-gray-500">No bank account selected</p>
          <Link to="/bank/accounts" className="text-blue-600 hover:underline mt-2 inline-block">
            Go to Bank Accounts
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to={`/bank/transactions?accountId=${bankAccountId}`}
          className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg"
        >
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div className="flex-1">
          <h1 className="text-3xl font-bold text-gray-900">Bank Reconciliation Statement</h1>
          <p className="text-gray-600 mt-1">
            {selectedAccount?.accountName || 'Loading...'} - {selectedAccount?.bankName}
          </p>
        </div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 text-gray-600 hover:text-gray-800 hover:bg-gray-100 rounded-md flex items-center gap-2"
        >
          <RefreshCw className="h-4 w-4" />
          Refresh
        </button>
        <button
          className="px-4 py-2 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded-md flex items-center gap-2"
          disabled
        >
          <Download className="h-4 w-4" />
          Export PDF
        </button>
      </div>

      {/* Date Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="flex flex-wrap gap-4 items-end">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">As Of Date</label>
            <input
              type="date"
              value={asOfDate}
              onChange={(e) => setAsOfDate(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Period Start (Optional)</label>
            <input
              type="date"
              value={periodStart}
              onChange={(e) => setPeriodStart(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
          >
            Generate Report
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center h-64">
          <RefreshCw className="w-8 h-8 animate-spin text-blue-600" />
          <span className="ml-3 text-gray-600">Generating BRS...</span>
        </div>
      ) : error ? (
        <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
          <AlertCircle className="h-12 w-12 text-red-400 mx-auto mb-3" />
          <p className="text-red-700">Failed to generate BRS</p>
          <p className="text-sm text-red-500 mt-1">Please try again or check if the bank account has linked ledger account.</p>
        </div>
      ) : brsReport ? (
        <div className="space-y-6">
          {/* Reconciliation Status Banner */}
          <div className={`rounded-lg p-4 flex items-center gap-4 ${
            isBalanced ? 'bg-green-50 border border-green-200' : 'bg-amber-50 border border-amber-200'
          }`}>
            {isBalanced ? (
              <>
                <CheckCircle className="h-8 w-8 text-green-600" />
                <div>
                  <h2 className="font-semibold text-green-800">Reconciliation Complete</h2>
                  <p className="text-sm text-green-600">Bank and book balances match.</p>
                </div>
              </>
            ) : (
              <>
                <AlertTriangle className="h-8 w-8 text-amber-600" />
                <div>
                  <h2 className="font-semibold text-amber-800">Reconciliation Pending</h2>
                  <p className="text-sm text-amber-600">
                    Difference: {formatCurrency(Math.abs(brsReport.adjustedBankBalance - brsReport.adjustedBookBalance))}
                  </p>
                </div>
              </>
            )}
          </div>

          {/* Balance Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center gap-2 mb-2">
                <Scale className="h-5 w-5 text-blue-500" />
                <h3 className="font-medium text-gray-700">Bank Statement Balance</h3>
              </div>
              <p className="text-2xl font-bold text-gray-900">{formatCurrency(brsReport.bankStatementBalance)}</p>
              <p className="text-xs text-gray-500 mt-1">As per bank statement</p>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center gap-2 mb-2">
                <FileText className="h-5 w-5 text-purple-500" />
                <h3 className="font-medium text-gray-700">Book Balance (Ledger)</h3>
              </div>
              <p className="text-2xl font-bold text-gray-900">
                {brsReport.hasLedgerLink ? formatCurrency(brsReport.ledgerBalance) : 'Not linked'}
              </p>
              <p className="text-xs text-gray-500 mt-1">
                {brsReport.linkedAccountName || 'Link bank account to ledger'}
              </p>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center gap-2 mb-2">
                <Receipt className="h-5 w-5 text-green-500" />
                <h3 className="font-medium text-gray-700">Reconciled Transactions</h3>
              </div>
              <p className="text-2xl font-bold text-gray-900">
                {brsReport.reconciledTransactions} / {brsReport.totalTransactions}
              </p>
              <div className="w-full bg-gray-200 rounded-full h-2 mt-2">
                <div
                  className="bg-green-500 h-2 rounded-full"
                  style={{
                    width: `${brsReport.totalTransactions > 0
                      ? (brsReport.reconciledTransactions / brsReport.totalTransactions) * 100
                      : 0}%`
                  }}
                />
              </div>
            </div>
          </div>

          {/* Reconciliation Details */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Bank Statement Perspective */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="font-semibold text-gray-800 mb-4 flex items-center gap-2">
                <Scale className="h-5 w-5 text-blue-500" />
                Bank Statement Perspective
              </h3>
              <table className="w-full text-sm">
                <tbody className="divide-y">
                  <tr>
                    <td className="py-2 text-gray-600">Balance as per Bank Statement</td>
                    <td className="py-2 text-right font-medium">{formatCurrency(brsReport.bankStatementBalance)}</td>
                  </tr>
                  <tr>
                    <td className="py-2 text-gray-600 flex items-center gap-2">
                      <TrendingUp className="h-4 w-4 text-green-500" />
                      Add: Deposits in Transit
                    </td>
                    <td className="py-2 text-right font-medium text-green-600">
                      {formatCurrency(brsReport.depositsInTransit)}
                    </td>
                  </tr>
                  <tr>
                    <td className="py-2 text-gray-600 flex items-center gap-2">
                      <TrendingDown className="h-4 w-4 text-red-500" />
                      Less: Outstanding Cheques
                    </td>
                    <td className="py-2 text-right font-medium text-red-600">
                      ({formatCurrency(brsReport.outstandingCheques)})
                    </td>
                  </tr>
                  <tr className="border-t-2">
                    <td className="py-2 font-medium text-gray-800">Adjusted Bank Balance</td>
                    <td className="py-2 text-right font-bold text-gray-900">
                      {formatCurrency(brsReport.adjustedBankBalance)}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            {/* Book Perspective */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="font-semibold text-gray-800 mb-4 flex items-center gap-2">
                <FileText className="h-5 w-5 text-purple-500" />
                Book (Ledger) Perspective
              </h3>
              <table className="w-full text-sm">
                <tbody className="divide-y">
                  <tr>
                    <td className="py-2 text-gray-600">Balance as per Books</td>
                    <td className="py-2 text-right font-medium">{formatCurrency(brsReport.bookBalance)}</td>
                  </tr>
                  <tr>
                    <td className="py-2 text-gray-600 flex items-center gap-2">
                      <TrendingUp className="h-4 w-4 text-green-500" />
                      Add: Bank Credits not in Books
                    </td>
                    <td className="py-2 text-right font-medium text-green-600">
                      {formatCurrency(brsReport.bankCreditsNotInBooks)}
                    </td>
                  </tr>
                  <tr>
                    <td className="py-2 text-gray-600 flex items-center gap-2">
                      <TrendingDown className="h-4 w-4 text-red-500" />
                      Less: Bank Debits not in Books
                    </td>
                    <td className="py-2 text-right font-medium text-red-600">
                      ({formatCurrency(brsReport.bankDebitsNotInBooks)})
                    </td>
                  </tr>
                  <tr className="border-t-2">
                    <td className="py-2 font-medium text-gray-800">Adjusted Book Balance</td>
                    <td className="py-2 text-right font-bold text-gray-900">
                      {formatCurrency(brsReport.adjustedBookBalance)}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* Unreconciled Items */}
          {(brsReport.bankCreditsNotInBooksItems?.length > 0 || brsReport.bankDebitsNotInBooksItems?.length > 0) && (
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="font-semibold text-gray-800 mb-4">Unreconciled Items</h3>
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Credits not in Books */}
                {brsReport.bankCreditsNotInBooksItems?.length > 0 && (
                  <div>
                    <h4 className="text-sm font-medium text-gray-700 mb-2 flex items-center gap-2">
                      <TrendingUp className="h-4 w-4 text-green-500" />
                      Credits in Bank, Not in Books ({brsReport.bankCreditsNotInBooksItems.length})
                    </h4>
                    <div className="max-h-48 overflow-y-auto border rounded">
                      <table className="w-full text-xs">
                        <thead className="bg-gray-50 sticky top-0">
                          <tr>
                            <th className="py-2 px-3 text-left">Date</th>
                            <th className="py-2 px-3 text-left">Description</th>
                            <th className="py-2 px-3 text-right">Amount</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y">
                          {brsReport.bankCreditsNotInBooksItems.map((item) => (
                            <tr key={item.id} className="hover:bg-gray-50">
                              <td className="py-2 px-3">{formatDate(item.date)}</td>
                              <td className="py-2 px-3 truncate max-w-[200px]" title={item.description}>
                                {item.description}
                              </td>
                              <td className="py-2 px-3 text-right text-green-600">
                                {formatCurrency(item.amount)}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                {/* Debits not in Books */}
                {brsReport.bankDebitsNotInBooksItems?.length > 0 && (
                  <div>
                    <h4 className="text-sm font-medium text-gray-700 mb-2 flex items-center gap-2">
                      <TrendingDown className="h-4 w-4 text-red-500" />
                      Debits in Bank, Not in Books ({brsReport.bankDebitsNotInBooksItems.length})
                    </h4>
                    <div className="max-h-48 overflow-y-auto border rounded">
                      <table className="w-full text-xs">
                        <thead className="bg-gray-50 sticky top-0">
                          <tr>
                            <th className="py-2 px-3 text-left">Date</th>
                            <th className="py-2 px-3 text-left">Description</th>
                            <th className="py-2 px-3 text-right">Amount</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y">
                          {brsReport.bankDebitsNotInBooksItems.map((item) => (
                            <tr key={item.id} className="hover:bg-gray-50">
                              <td className="py-2 px-3">{formatDate(item.date)}</td>
                              <td className="py-2 px-3 truncate max-w-[200px]" title={item.description}>
                                {item.description}
                              </td>
                              <td className="py-2 px-3 text-right text-red-600">
                                {formatCurrency(item.amount)}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* TDS Summary */}
          {brsReport.tdsSummary && brsReport.tdsSummary.length > 0 && (
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="font-semibold text-gray-800 mb-4">TDS Summary by Section</h3>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="py-2 px-4 text-left">Section</th>
                      <th className="py-2 px-4 text-left">Description</th>
                      <th className="py-2 px-4 text-right">Transactions</th>
                      <th className="py-2 px-4 text-right">Total Amount</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {brsReport.tdsSummary.map((tds) => (
                      <tr key={tds.section} className="hover:bg-gray-50">
                        <td className="py-2 px-4 font-medium">{tds.section}</td>
                        <td className="py-2 px-4 text-gray-600">{tds.description}</td>
                        <td className="py-2 px-4 text-right">{tds.transactionCount}</td>
                        <td className="py-2 px-4 text-right font-medium">{formatCurrency(tds.totalAmount)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot className="bg-purple-50">
                    <tr>
                      <td colSpan={3} className="py-2 px-4 font-medium text-purple-800">Total TDS</td>
                      <td className="py-2 px-4 text-right font-bold text-purple-800">
                        {formatCurrency(brsReport.totalTdsDeducted)}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </div>
          )}

          {/* Difference Type Summary */}
          {brsReport.differenceTypeSummary && brsReport.differenceTypeSummary.length > 0 && (
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="font-semibold text-gray-800 mb-4">Reconciliation Differences by Type</h3>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="py-2 px-4 text-left">Type</th>
                      <th className="py-2 px-4 text-left">Description</th>
                      <th className="py-2 px-4 text-right">Count</th>
                      <th className="py-2 px-4 text-right">Total Amount</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {brsReport.differenceTypeSummary.map((diff) => (
                      <tr key={diff.differenceType} className="hover:bg-gray-50">
                        <td className="py-2 px-4 font-medium capitalize">{diff.differenceType.replace(/_/g, ' ')}</td>
                        <td className="py-2 px-4 text-gray-600">{diff.description}</td>
                        <td className="py-2 px-4 text-right">{diff.count}</td>
                        <td className="py-2 px-4 text-right font-medium">{formatCurrency(diff.totalAmount)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Audit Metrics */}
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="font-semibold text-gray-800 mb-4">Audit Metrics</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className={`p-4 rounded-lg ${
                brsReport.unlinkedJeCount === 0 ? 'bg-green-50' : 'bg-amber-50'
              }`}>
                <div className="flex items-center gap-2 mb-1">
                  {brsReport.unlinkedJeCount === 0 ? (
                    <CheckCircle className="h-5 w-5 text-green-600" />
                  ) : (
                    <AlertTriangle className="h-5 w-5 text-amber-600" />
                  )}
                  <span className="text-sm font-medium text-gray-700">Unlinked JE Count</span>
                </div>
                <p className={`text-2xl font-bold ${
                  brsReport.unlinkedJeCount === 0 ? 'text-green-600' : 'text-amber-600'
                }`}>
                  {brsReport.unlinkedJeCount}
                </p>
                <p className="text-xs text-gray-500 mt-1">
                  Reconciled transactions without journal entry link
                </p>
              </div>

              <div className="p-4 rounded-lg bg-blue-50">
                <div className="flex items-center gap-2 mb-1">
                  <FileText className="h-5 w-5 text-blue-600" />
                  <span className="text-sm font-medium text-gray-700">Direct JE Reconciliations</span>
                </div>
                <p className="text-2xl font-bold text-blue-600">
                  {brsReport.directJeReconciliationCount}
                </p>
                <p className="text-xs text-gray-500 mt-1">
                  Transactions reconciled directly to journal entries
                </p>
              </div>

              <div className={`p-4 rounded-lg ${
                brsReport.hasLedgerLink ? 'bg-green-50' : 'bg-gray-50'
              }`}>
                <div className="flex items-center gap-2 mb-1">
                  <Scale className="h-5 w-5 text-gray-600" />
                  <span className="text-sm font-medium text-gray-700">Ledger Link Status</span>
                </div>
                <p className={`text-lg font-bold ${
                  brsReport.hasLedgerLink ? 'text-green-600' : 'text-gray-500'
                }`}>
                  {brsReport.hasLedgerLink ? 'Linked' : 'Not Linked'}
                </p>
                <p className="text-xs text-gray-500 mt-1">
                  {brsReport.linkedAccountName || 'Link bank account to Chart of Accounts'}
                </p>
              </div>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  )
}

export default BankReconciliationStatement

import { useState, useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useOutgoingPayments, useOutgoingPaymentsSummary } from '@/hooks/api/useOutgoingPayments'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useUnreconcileTransaction } from '@/hooks/api/useBankTransactions'
import { OutgoingPayment, OutgoingPaymentsFilterParams } from '@/services/api/types'
import { CompanySelect } from '@/components/ui/CompanySelect'
import { ReconcilePaymentDialog } from '@/components/banking/ReconcilePaymentDialog'
import {
  ArrowLeft,
  CheckCircle,
  XCircle,
  Filter,
  RefreshCw,
  ExternalLink,
  Calendar,
  DollarSign,
  Building,
  Users,
  CreditCard,
  FileText,
  Briefcase,
  Wrench,
  Link2,
  Unlink
} from 'lucide-react'

const OutgoingPaymentsReconciliation = () => {
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  const [filterType, setFilterType] = useState<string>('all')
  const [filterReconciled, setFilterReconciled] = useState<'all' | 'reconciled' | 'unreconciled'>('unreconciled')
  const [currentPage, setCurrentPage] = useState(1)
  const [reconcilingPayment, setReconcilingPayment] = useState<OutgoingPayment | null>(null)
  const pageSize = 20

  const { data: companies = [] } = useCompanies()
  const unreconcileTransaction = useUnreconcileTransaction()

  const filterParams: OutgoingPaymentsFilterParams = useMemo(() => ({
    pageNumber: currentPage,
    pageSize,
    reconciled: filterReconciled === 'all' ? undefined : filterReconciled === 'reconciled',
    types: filterType === 'all' ? undefined : filterType
  }), [currentPage, filterReconciled, filterType])

  const { data: paymentsData, isLoading, refetch } = useOutgoingPayments(
    selectedCompanyId,
    filterParams,
    !!selectedCompanyId
  )

  const { data: summary, isLoading: summaryLoading } = useOutgoingPaymentsSummary(
    selectedCompanyId,
    undefined,
    undefined,
    !!selectedCompanyId
  )

  const payments = paymentsData?.items || []
  const totalCount = paymentsData?.totalCount || 0
  const totalPages = Math.ceil(totalCount / pageSize)

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(amount)
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    })
  }

  const handleUnreconcile = async (payment: OutgoingPayment) => {
    if (!payment.bankTransactionId) return
    try {
      await unreconcileTransaction.mutateAsync(payment.bankTransactionId)
      refetch()
    } catch (error) {
      console.error('Failed to unreconcile payment:', error)
    }
  }

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'salary': return <Users className="w-4 h-4" />
      case 'contractor': return <Briefcase className="w-4 h-4" />
      case 'vendor_payment': return <Building className="w-4 h-4" />
      case 'expense_claim': return <FileText className="w-4 h-4" />
      case 'subscription': return <CreditCard className="w-4 h-4" />
      case 'loan_payment': return <DollarSign className="w-4 h-4" />
      case 'asset_maintenance': return <Wrench className="w-4 h-4" />
      default: return <DollarSign className="w-4 h-4" />
    }
  }

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'salary': return 'bg-blue-100 text-blue-700'
      case 'contractor': return 'bg-purple-100 text-purple-700'
      case 'vendor_payment': return 'bg-emerald-100 text-emerald-700'
      case 'expense_claim': return 'bg-orange-100 text-orange-700'
      case 'subscription': return 'bg-cyan-100 text-cyan-700'
      case 'loan_payment': return 'bg-red-100 text-red-700'
      case 'asset_maintenance': return 'bg-gray-100 text-gray-700'
      default: return 'bg-gray-100 text-gray-700'
    }
  }

  const paymentTypes = [
    { value: 'all', label: 'All Types' },
    { value: 'salary', label: 'Salary' },
    { value: 'contractor', label: 'Contractor' },
    { value: 'vendor_payment', label: 'Vendor Payments' },
    { value: 'expense_claim', label: 'Expense Claims' },
    { value: 'subscription', label: 'Subscriptions' },
    { value: 'loan_payment', label: 'Loan Payments' },
    { value: 'asset_maintenance', label: 'Asset Maintenance' }
  ]

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex items-center gap-4 mb-6">
        <Link to="/finance/banking" className="text-gray-500 hover:text-gray-700">
          <ArrowLeft className="w-5 h-5" />
        </Link>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Outgoing Payments Reconciliation</h1>
          <p className="text-gray-500 text-sm mt-1">
            Unified view of all outgoing payments for bank reconciliation
          </p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-sm border p-4 mb-6">
        <div className="flex flex-wrap gap-4 items-end">
          <div className="w-64">
            <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
            <CompanySelect
              value={selectedCompanyId}
              onChange={setSelectedCompanyId}
              companies={companies}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Type</label>
            <select
              value={filterType}
              onChange={(e) => { setFilterType(e.target.value); setCurrentPage(1); }}
              className="px-3 py-2 border border-gray-300 rounded-md text-sm min-w-[160px]"
            >
              {paymentTypes.map(type => (
                <option key={type.value} value={type.value}>{type.label}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
            <select
              value={filterReconciled}
              onChange={(e) => { setFilterReconciled(e.target.value as any); setCurrentPage(1); }}
              className="px-3 py-2 border border-gray-300 rounded-md text-sm min-w-[140px]"
            >
              <option value="all">All</option>
              <option value="unreconciled">Pending</option>
              <option value="reconciled">Reconciled</option>
            </select>
          </div>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 text-gray-600 hover:text-gray-800 flex items-center gap-2"
          >
            <RefreshCw className="w-4 h-4" />
            Refresh
          </button>
        </div>
      </div>

      {/* Summary Cards */}
      {selectedCompanyId && !summaryLoading && summary && (
        <div className="grid grid-cols-4 gap-4 mb-6">
          <div className="bg-white rounded-lg shadow-sm border p-4">
            <div className="text-sm text-gray-500">Total Payments</div>
            <div className="text-2xl font-bold text-gray-900">{summary.totalCount}</div>
            <div className="text-sm text-gray-600">{formatCurrency(summary.totalAmount)}</div>
          </div>
          <div className="bg-white rounded-lg shadow-sm border p-4">
            <div className="text-sm text-gray-500">Reconciled</div>
            <div className="text-2xl font-bold text-green-600">{summary.reconciledCount}</div>
            <div className="text-sm text-green-600">{formatCurrency(summary.reconciledAmount)}</div>
          </div>
          <div className="bg-white rounded-lg shadow-sm border p-4">
            <div className="text-sm text-gray-500">Pending</div>
            <div className="text-2xl font-bold text-orange-600">{summary.unreconciledCount}</div>
            <div className="text-sm text-orange-600">{formatCurrency(summary.unreconciledAmount)}</div>
          </div>
          <div className="bg-white rounded-lg shadow-sm border p-4">
            <div className="text-sm text-gray-500">Reconciliation Rate</div>
            <div className="text-2xl font-bold text-blue-600">
              {summary.totalCount > 0 ? Math.round((summary.reconciledCount / summary.totalCount) * 100) : 0}%
            </div>
            <div className="mt-2 bg-gray-200 rounded-full h-2">
              <div
                className="bg-blue-600 rounded-full h-2"
                style={{ width: `${summary.totalCount > 0 ? (summary.reconciledCount / summary.totalCount) * 100 : 0}%` }}
              />
            </div>
          </div>
        </div>
      )}

      {/* Type Breakdown Pills */}
      {selectedCompanyId && summary && Object.keys(summary.byType).length > 0 && (
        <div className="bg-white rounded-lg shadow-sm border p-4 mb-6">
          <h3 className="text-sm font-medium text-gray-700 mb-3">By Type</h3>
          <div className="flex flex-wrap gap-3">
            {Object.entries(summary.byType).map(([type, breakdown]) => (
              <div
                key={type}
                className={`px-4 py-2 rounded-lg cursor-pointer transition-colors ${
                  filterType === type ? 'ring-2 ring-blue-500' : ''
                } ${getTypeColor(type)}`}
                onClick={() => setFilterType(filterType === type ? 'all' : type)}
              >
                <div className="flex items-center gap-2">
                  {getTypeIcon(type)}
                  <span className="font-medium">{breakdown.typeDisplay}</span>
                </div>
                <div className="text-xs mt-1 opacity-80">
                  {breakdown.count} payments • {formatCurrency(breakdown.amount)}
                </div>
                <div className="text-xs opacity-60">
                  {breakdown.unreconciledCount} pending
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Payments List */}
      {!selectedCompanyId ? (
        <div className="bg-white rounded-lg shadow-sm border p-8 text-center">
          <Filter className="w-12 h-12 text-gray-300 mx-auto mb-4" />
          <p className="text-gray-500">Select a company to view outgoing payments</p>
        </div>
      ) : isLoading ? (
        <div className="bg-white rounded-lg shadow-sm border p-8 text-center">
          <RefreshCw className="w-8 h-8 text-gray-400 mx-auto mb-4 animate-spin" />
          <p className="text-gray-500">Loading payments...</p>
        </div>
      ) : payments.length === 0 ? (
        <div className="bg-white rounded-lg shadow-sm border p-8 text-center">
          <CheckCircle className="w-12 h-12 text-green-300 mx-auto mb-4" />
          <p className="text-gray-500">No payments found with the current filters</p>
        </div>
      ) : (
        <div className="bg-white rounded-lg shadow-sm border overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Payee</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Amount</th>
                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Status</th>
                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {payments.map((payment: OutgoingPayment) => (
                <tr key={payment.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center gap-1.5 px-2 py-1 rounded text-xs font-medium ${getTypeColor(payment.type)}`}>
                      {getTypeIcon(payment.type)}
                      {payment.typeDisplay}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="font-medium text-gray-900">{payment.payeeName || '-'}</div>
                    {payment.referenceNumber && (
                      <div className="text-xs text-gray-400 font-mono">{payment.referenceNumber}</div>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-600 max-w-xs truncate">
                    {payment.description || payment.category || '-'}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-600">
                    {formatDate(payment.paymentDate)}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="font-medium text-gray-900">{formatCurrency(payment.amount)}</div>
                    {payment.tdsAmount && payment.tdsAmount > 0 && (
                      <div className="text-xs text-purple-600">
                        TDS: {formatCurrency(payment.tdsAmount)}
                      </div>
                    )}
                  </td>
                  <td className="px-4 py-3 text-center">
                    {payment.isReconciled ? (
                      <span className="inline-flex items-center gap-1 text-green-600 text-sm">
                        <CheckCircle className="w-4 h-4" />
                        Reconciled
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 text-orange-600 text-sm">
                        <XCircle className="w-4 h-4" />
                        Pending
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-center">
                    {payment.isReconciled || payment.bankTransactionId ? (
                      <div className="inline-flex items-center gap-2">
                        {payment.bankTransactionId && (
                          <Link
                            to={`/finance/banking?accountId=${payment.bankTransactionId}`}
                            className="text-blue-600 hover:text-blue-800 text-sm inline-flex items-center gap-1"
                          >
                            View <ExternalLink className="w-3 h-3" />
                          </Link>
                        )}
                        {payment.bankTransactionId && (
                          <button
                            onClick={() => handleUnreconcile(payment)}
                            disabled={unreconcileTransaction.isPending}
                            className="text-orange-600 hover:text-orange-800 text-sm inline-flex items-center gap-1 disabled:opacity-50"
                          >
                            <Unlink className="w-3 h-3" />
                            Unreconcile
                          </button>
                        )}
                      </div>
                    ) : (
                      <button
                        onClick={() => setReconcilingPayment(payment)}
                        className="text-blue-600 hover:text-blue-800 text-sm inline-flex items-center gap-1"
                      >
                        <Link2 className="w-3 h-3" />
                        Reconcile
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="px-4 py-3 border-t bg-gray-50 flex items-center justify-between">
              <div className="text-sm text-gray-500">
                Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, totalCount)} of {totalCount} payments
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                  disabled={currentPage === 1}
                  className="px-3 py-1 text-sm border rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Previous
                </button>
                <span className="px-3 py-1 text-sm">
                  Page {currentPage} of {totalPages}
                </span>
                <button
                  onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                  disabled={currentPage === totalPages}
                  className="px-3 py-1 text-sm border rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      <ReconcilePaymentDialog
        isOpen={!!reconcilingPayment}
        reconciledType={reconcilingPayment?.type || ''}
        transactionType="debit"
        payment={
          reconcilingPayment
            ? {
                id: reconcilingPayment.id,
                amount: reconcilingPayment.amount,
                paymentDate: reconcilingPayment.paymentDate,
                payeeName: reconcilingPayment.payeeName,
                description: reconcilingPayment.description || reconcilingPayment.category,
                referenceNumber: reconcilingPayment.referenceNumber,
              }
            : null
        }
        onClose={() => setReconcilingPayment(null)}
        onReconciled={() => {
          setReconcilingPayment(null)
          refetch()
        }}
      />
    </div>
  )
}

export default OutgoingPaymentsReconciliation

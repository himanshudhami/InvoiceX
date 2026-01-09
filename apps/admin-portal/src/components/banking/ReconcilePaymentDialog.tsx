import { useMemo, useState } from 'react'
import { Drawer } from '@/components/ui/Drawer'
import { useReconcileTransaction, useUnreconciledTransactions } from '@/hooks/api/useBankTransactions'
import { BankTransaction, ReconciliationDifferenceType } from '@/services/api/types'
import { AlertTriangle, ArrowLeft, CheckCircle, RefreshCw, Search } from 'lucide-react'

const DIFFERENCE_THRESHOLD = 10

const DIFFERENCE_TYPE_OPTIONS: { value: ReconciliationDifferenceType; label: string; description: string }[] = [
  { value: 'bank_interest', label: 'Bank Interest', description: 'Interest income credited by bank' },
  { value: 'bank_charges', label: 'Bank Charges', description: 'Fees/charges deducted by bank' },
  { value: 'tds_deducted', label: 'TDS Deducted', description: 'TDS deducted by customer' },
  { value: 'round_off', label: 'Round Off', description: 'Minor rounding difference' },
  { value: 'forex_gain', label: 'Forex Gain', description: 'Foreign exchange gain' },
  { value: 'forex_loss', label: 'Forex Loss', description: 'Foreign exchange loss' },
  { value: 'other_income', label: 'Other Income', description: 'Miscellaneous income' },
  { value: 'other_expense', label: 'Other Expense', description: 'Miscellaneous expense' },
  { value: 'suspense', label: 'Investigate Later', description: 'Park for investigation' },
]

interface ReconcilePaymentDialogProps {
  isOpen: boolean
  reconciledType: string
  transactionType?: 'debit' | 'credit'
  payment: {
    id: string
    amount: number
    paymentDate?: string
    payeeName?: string
    description?: string
    referenceNumber?: string
  } | null
  onClose: () => void
  onReconciled: () => void
}

export const ReconcilePaymentDialog = ({
  isOpen,
  reconciledType,
  transactionType = 'debit',
  payment,
  onClose,
  onReconciled
}: ReconcilePaymentDialogProps) => {
  const [searchTerm, setSearchTerm] = useState('')
  const [pendingReconciliation, setPendingReconciliation] = useState<{
    transaction: BankTransaction
    differenceAmount: number
  } | null>(null)
  const [differenceType, setDifferenceType] = useState<ReconciliationDifferenceType>('round_off')
  const [differenceNotes, setDifferenceNotes] = useState('')
  const [tdsSection, setTdsSection] = useState('')

  const reconcileTransaction = useReconcileTransaction()
  const { data: transactions = [], isLoading, refetch } = useUnreconciledTransactions(isOpen)

  const filteredTransactions = useMemo(() => {
    if (!payment) return []

    const search = searchTerm.trim().toLowerCase()
    const paymentDate = payment.paymentDate ? new Date(payment.paymentDate) : null

    return transactions
      .filter(txn => txn.transactionType === transactionType)
      .filter(txn => {
        if (!search) return true
        const description = txn.description?.toLowerCase() || ''
        const reference = txn.referenceNumber?.toLowerCase() || ''
        const cheque = txn.chequeNumber?.toLowerCase() || ''
        return description.includes(search) || reference.includes(search) || cheque.includes(search)
      })
      .map(txn => {
        const amountDiff = Math.abs(txn.amount - payment.amount)
        const dateDiff = paymentDate
          ? Math.abs(new Date(txn.transactionDate).getTime() - paymentDate.getTime())
          : Number.MAX_SAFE_INTEGER
        return { txn, amountDiff, dateDiff }
      })
      .sort((a, b) => {
        if (a.amountDiff !== b.amountDiff) return a.amountDiff - b.amountDiff
        return a.dateDiff - b.dateDiff
      })
      .slice(0, 20)
      .map(item => item.txn)
  }, [transactions, payment, searchTerm, transactionType])

  const resetDifferenceState = () => {
    setPendingReconciliation(null)
    setDifferenceType('round_off')
    setDifferenceNotes('')
    setTdsSection('')
  }

  const resetDialogState = () => {
    resetDifferenceState()
    setSearchTerm('')
  }

  const performReconciliation = async (
    transaction: BankTransaction,
    differenceAmount: number,
    diffType?: ReconciliationDifferenceType,
    diffNotes?: string,
    tdsSec?: string
  ) => {
    if (!payment || !reconciledType) return

    await reconcileTransaction.mutateAsync({
      id: transaction.id,
      data: {
        reconciledType,
        reconciledId: payment.id,
        reconciledBy: 'user',
        differenceAmount: Math.abs(differenceAmount) > DIFFERENCE_THRESHOLD ? differenceAmount : undefined,
        differenceType: diffType,
        differenceNotes: diffNotes,
        tdsSection: diffType === 'tds_deducted' ? tdsSec : undefined
      }
    })

    resetDialogState()
    onReconciled()
  }

  const handleSelectTransaction = (transaction: BankTransaction) => {
    if (!payment) return
    const difference = transaction.amount - payment.amount

    if (Math.abs(difference) > DIFFERENCE_THRESHOLD) {
      setPendingReconciliation({ transaction, differenceAmount: difference })
      if (transactionType === 'debit') {
        setDifferenceType(difference > 0 ? 'bank_charges' : 'round_off')
      } else {
        setDifferenceType(difference > 0 ? 'bank_interest' : 'tds_deducted')
      }
      return
    }

    void performReconciliation(transaction, difference)
  }

  const handleConfirmReconciliation = () => {
    if (!pendingReconciliation) return
    void performReconciliation(
      pendingReconciliation.transaction,
      pendingReconciliation.differenceAmount,
      differenceType,
      differenceNotes || undefined,
      tdsSection || undefined
    )
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '-'
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    })
  }

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(amount)

  return (
    <Drawer
      isOpen={isOpen}
      onClose={() => {
        resetDialogState()
        onClose()
      }}
      title="Reconcile Payment"
      size="lg"
    >
      {!payment ? (
        <div className="text-sm text-gray-500">Select a payment to reconcile.</div>
      ) : (
        <div className="flex flex-col h-full">
          <div className="bg-gray-50 rounded-lg p-4 mb-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-sm font-medium text-gray-900">
                  {payment.payeeName || 'Payment'}
                </div>
                <div className="text-xs text-gray-500">
                  {formatDate(payment.paymentDate)}
                  {payment.referenceNumber ? ` • Ref: ${payment.referenceNumber}` : ''}
                </div>
              </div>
              <div className="text-lg font-semibold text-red-600">
                {formatCurrency(payment.amount)}
              </div>
            </div>
            {payment.description && (
              <div className="mt-2 text-sm text-gray-600">{payment.description}</div>
            )}
          </div>

          {pendingReconciliation ? (
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 mb-4">
              <div className="flex items-center gap-2 mb-3">
                <button
                  onClick={resetDifferenceState}
                  className="p-1 hover:bg-amber-100 rounded"
                >
                  <ArrowLeft className="w-4 h-4 text-amber-700" />
                </button>
                <AlertTriangle className="w-5 h-5 text-amber-600" />
                <h3 className="font-medium text-amber-800">Amount Difference Detected</h3>
              </div>

              <div className="bg-white rounded-lg p-3 mb-4 space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Bank Transaction:</span>
                  <span className="font-medium">{formatCurrency(pendingReconciliation.transaction.amount)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Payment Amount:</span>
                  <span className="font-medium">{formatCurrency(payment.amount)}</span>
                </div>
                <div className="border-t pt-2 flex justify-between text-sm">
                  <span className="font-medium text-amber-700">Difference:</span>
                  <span
                    className={`font-bold ${
                      pendingReconciliation.differenceAmount > 0 ? 'text-green-600' : 'text-red-600'
                    }`}
                  >
                    {pendingReconciliation.differenceAmount > 0 ? '+' : ''}
                    {formatCurrency(pendingReconciliation.differenceAmount)}
                  </span>
                </div>
              </div>

              <div className="space-y-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Classify this difference as:
                  </label>
                  <select
                    value={differenceType}
                    onChange={(e) => setDifferenceType(e.target.value as ReconciliationDifferenceType)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm bg-white"
                  >
                    {DIFFERENCE_TYPE_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label} - {option.description}
                      </option>
                    ))}
                  </select>
                </div>

                {differenceType === 'tds_deducted' && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      TDS Section (optional)
                    </label>
                    <select
                      value={tdsSection}
                      onChange={(e) => setTdsSection(e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm bg-white"
                    >
                      <option value="">Select section...</option>
                      <option value="194A">194A - Interest other than interest on securities</option>
                      <option value="194C">194C - Contractor payments</option>
                      <option value="194H">194H - Commission or brokerage</option>
                      <option value="194I">194I - Rent</option>
                      <option value="194J">194J - Professional/Technical fees</option>
                      <option value="194Q">194Q - Purchase of goods</option>
                      <option value="other">Other</option>
                    </select>
                  </div>
                )}

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Notes (optional)
                  </label>
                  <textarea
                    value={differenceNotes}
                    onChange={(e) => setDifferenceNotes(e.target.value)}
                    placeholder="Add notes about this difference..."
                    className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm bg-white"
                    rows={3}
                  />
                </div>

                <div className="flex gap-2 pt-2">
                  <button
                    onClick={resetDifferenceState}
                    className="flex-1 px-4 py-2 border border-gray-300 rounded-md text-sm text-gray-700 hover:bg-gray-50"
                  >
                    Back
                  </button>
                  <button
                    onClick={handleConfirmReconciliation}
                    className="flex-1 px-4 py-2 bg-amber-600 text-white rounded-md text-sm hover:bg-amber-700 disabled:opacity-50"
                    disabled={reconcileTransaction.isPending}
                  >
                    {reconcileTransaction.isPending ? 'Saving...' : 'Confirm & Reconcile'}
                  </button>
                </div>
              </div>
            </div>
          ) : (
            <>
              <div className="flex items-center gap-2 mb-4">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-2.5 w-4 h-4 text-gray-400" />
                  <input
                    type="text"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    placeholder="Search unreconciled bank transactions..."
                    className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-md text-sm"
                  />
                </div>
                <button
                  onClick={() => refetch()}
                  className="p-2 border border-gray-300 rounded-md hover:bg-gray-50"
                  title="Refresh"
                >
                  <RefreshCw className="w-4 h-4 text-gray-500" />
                </button>
              </div>

              <div className="flex-1 overflow-auto">
                {isLoading ? (
                  <div className="text-sm text-gray-500">Loading transactions...</div>
                ) : filteredTransactions.length === 0 ? (
                  <div className="text-sm text-gray-500">No matching unreconciled transactions.</div>
                ) : (
                  <div className="space-y-2">
                    {filteredTransactions.map((txn) => (
                      <button
                        key={txn.id}
                        type="button"
                        onClick={() => handleSelectTransaction(txn)}
                        className="w-full text-left border rounded-md p-3 hover:bg-gray-50"
                      >
                        <div className="flex items-center justify-between">
                          <div>
                            <div className="text-sm font-medium text-gray-900">
                              {txn.description || 'Bank Transaction'}
                            </div>
                            <div className="text-xs text-gray-500">
                              {formatDate(txn.transactionDate)}
                              {txn.referenceNumber ? ` • Ref: ${txn.referenceNumber}` : ''}
                            </div>
                          </div>
                          <div className="text-sm font-semibold text-gray-900">
                            {formatCurrency(txn.amount)}
                          </div>
                        </div>
                      </button>
                    ))}
                  </div>
                )}
              </div>

              <div className="pt-4 border-t mt-4">
                <div className="text-xs text-gray-500 flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-green-500" />
                  Showing unreconciled {transactionType} transactions
                </div>
              </div>
            </>
          )}
        </div>
      )}
    </Drawer>
  )
}

export default ReconcilePaymentDialog

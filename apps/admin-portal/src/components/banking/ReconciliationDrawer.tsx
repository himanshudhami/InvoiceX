import { useState } from 'react'
import { Drawer } from '@/components/ui/Drawer'
import {
  useReconciliationSuggestions,
  useDebitReconciliationSuggestions,
  useReconcileTransaction
} from '@/hooks/api/useBankTransactions'
import {
  BankTransaction,
  ReconciliationSuggestion,
  DebitReconciliationSuggestion,
  ReconciliationDifferenceType
} from '@/services/api/types'
import { OutgoingPaymentTypeahead } from '@/components/banking/OutgoingPaymentTypeahead'
import { PaymentTypeahead } from '@/components/banking/PaymentTypeahead'
import { RefreshCw, Search, CheckCircle, AlertTriangle, ArrowLeft } from 'lucide-react'

// Difference types with labels for UI
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

interface ReconciliationDrawerProps {
  transaction: BankTransaction | null
  companyId: string
  onClose: () => void
  onReconciled: () => void
  formatCurrency: (amount: number) => string
}

type ReconciliationTab = 'suggestions' | 'search' | 'manual'

export const ReconciliationDrawer = ({
  transaction,
  companyId,
  onClose,
  onReconciled,
  formatCurrency
}: ReconciliationDrawerProps) => {
  const [activeTab, setActiveTab] = useState<ReconciliationTab>('suggestions')
  const [manualType, setManualType] = useState<string>('payment')
  const [manualId, setManualId] = useState<string>('')

  // Confirmation step state (when there's a difference)
  const [pendingReconciliation, setPendingReconciliation] = useState<{
    type: 'credit' | 'debit'
    suggestion: ReconciliationSuggestion | DebitReconciliationSuggestion
    differenceAmount: number
  } | null>(null)
  const [differenceType, setDifferenceType] = useState<ReconciliationDifferenceType>('round_off')
  const [differenceNotes, setDifferenceNotes] = useState('')
  const [tdsSection, setTdsSection] = useState('')

  const isDebit = transaction?.transactionType === 'debit'
  const isCredit = transaction?.transactionType === 'credit'

  // Threshold for showing confirmation (₹10)
  const DIFFERENCE_THRESHOLD = 10

  // Calculate tolerance as 1% of transaction amount (min ₹100, max ₹10000)
  // This handles TDS deductions, bank charges, and rounding differences
  const tolerance = transaction
    ? Math.min(Math.max(transaction.amount * 0.01, 100), 10000)
    : 100

  // Credit suggestions (incoming payments)
  const { data: creditSuggestions = [], isLoading: creditSuggestionsLoading } = useReconciliationSuggestions(
    transaction?.id || '',
    tolerance,
    10,
    !!transaction && isCredit
  )

  // Debit suggestions (outgoing payments)
  const { data: debitSuggestions = [], isLoading: debitSuggestionsLoading } = useDebitReconciliationSuggestions(
    transaction?.id || '',
    tolerance,
    10,
    !!transaction && isDebit
  )

  const suggestions = isCredit ? creditSuggestions : debitSuggestions
  const suggestionsLoading = isCredit ? creditSuggestionsLoading : debitSuggestionsLoading

  const reconcileTransaction = useReconcileTransaction()

  const handleReconcileWithCreditSuggestion = (suggestion: ReconciliationSuggestion) => {
    if (!transaction) return

    // Calculate difference: Bank amount - Payment amount
    const difference = transaction.amount - suggestion.amount

    // If difference is significant, show confirmation step
    if (Math.abs(difference) > DIFFERENCE_THRESHOLD) {
      setPendingReconciliation({
        type: 'credit',
        suggestion,
        differenceAmount: difference
      })
      // Pre-select likely difference type
      if (difference > 0) {
        setDifferenceType('bank_interest') // Bank received more (interest income)
      } else {
        setDifferenceType('tds_deducted') // Bank received less (TDS deducted)
      }
      return
    }

    // No significant difference, reconcile directly
    performReconciliation('payment', suggestion.paymentId, difference)
  }

  const handleReconcileWithDebitSuggestion = (suggestion: DebitReconciliationSuggestion) => {
    if (!transaction) return

    // Calculate difference: Bank amount - Payment amount
    const difference = transaction.amount - suggestion.amount

    // If difference is significant, show confirmation step
    if (Math.abs(difference) > DIFFERENCE_THRESHOLD) {
      setPendingReconciliation({
        type: 'debit',
        suggestion,
        differenceAmount: difference
      })
      // Pre-select likely difference type
      if (difference > 0) {
        setDifferenceType('bank_charges') // Bank debited more (bank charges)
      } else {
        setDifferenceType('round_off') // Less than expected
      }
      return
    }

    // No significant difference, reconcile directly
    performReconciliation(suggestion.recordType, suggestion.recordId, difference)
  }

  const performReconciliation = async (
    reconciledType: string,
    reconciledId: string,
    differenceAmount: number,
    diffType?: ReconciliationDifferenceType,
    diffNotes?: string,
    tdsSec?: string
  ) => {
    if (!transaction) return
    try {
      await reconcileTransaction.mutateAsync({
        id: transaction.id,
        data: {
          reconciledType,
          reconciledId,
          reconciledBy: 'user',
          differenceAmount: Math.abs(differenceAmount) > DIFFERENCE_THRESHOLD ? differenceAmount : undefined,
          differenceType: diffType,
          differenceNotes: diffNotes,
          tdsSection: tdsSec
        }
      })
      // Reset state
      setPendingReconciliation(null)
      setDifferenceType('round_off')
      setDifferenceNotes('')
      setTdsSection('')
      onReconciled()
    } catch (error) {
      console.error('Failed to reconcile:', error)
    }
  }

  const handleConfirmReconciliation = () => {
    if (!pendingReconciliation) return

    const { type, suggestion, differenceAmount } = pendingReconciliation

    if (type === 'credit') {
      const creditSuggestion = suggestion as ReconciliationSuggestion
      performReconciliation(
        'payment',
        creditSuggestion.paymentId,
        differenceAmount,
        differenceType,
        differenceNotes || undefined,
        differenceType === 'tds_deducted' ? tdsSection || undefined : undefined
      )
    } else {
      const debitSuggestion = suggestion as DebitReconciliationSuggestion
      performReconciliation(
        debitSuggestion.recordType,
        debitSuggestion.recordId,
        differenceAmount,
        differenceType,
        differenceNotes || undefined,
        differenceType === 'tds_deducted' ? tdsSection || undefined : undefined
      )
    }
  }

  const handleCancelConfirmation = () => {
    setPendingReconciliation(null)
    setDifferenceType('round_off')
    setDifferenceNotes('')
    setTdsSection('')
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

  const getMatchScoreColor = (score: number) => {
    if (score >= 80) return 'text-green-600 bg-green-50'
    if (score >= 50) return 'text-yellow-600 bg-yellow-50'
    return 'text-red-600 bg-red-50'
  }

  return (
    <Drawer
      isOpen={!!transaction}
      onClose={onClose}
      title="Reconcile Transaction"
      size="lg"
    >
      {transaction && (
        <div className="flex flex-col h-full">
          {/* Transaction Details */}
          <div className="bg-gray-50 rounded-lg p-4 mb-4">
            <div className="flex justify-between items-center">
              <div>
                <span className={`inline-flex items-center px-2 py-1 rounded text-xs font-medium ${
                  isCredit ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                }`}>
                  {isCredit ? 'Credit (Incoming)' : 'Debit (Outgoing)'}
                </span>
                <span className="ml-2 text-sm text-gray-500">{formatDate(transaction.transactionDate)}</span>
              </div>
              <div className={`text-lg font-semibold ${isCredit ? 'text-green-600' : 'text-red-600'}`}>
                {formatCurrency(transaction.amount)}
              </div>
            </div>
            <div className="mt-2 text-sm text-gray-600">{transaction.description || 'No description'}</div>
            {transaction.referenceNumber && (
              <div className="mt-1 text-xs text-gray-400 font-mono">Ref: {transaction.referenceNumber}</div>
            )}
          </div>

          {/* Confirmation View - When there's a difference */}
          {pendingReconciliation && (
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 mb-4">
              <div className="flex items-center gap-2 mb-3">
                <button
                  onClick={handleCancelConfirmation}
                  className="p-1 hover:bg-amber-100 rounded"
                >
                  <ArrowLeft className="w-4 h-4 text-amber-700" />
                </button>
                <AlertTriangle className="w-5 h-5 text-amber-600" />
                <h3 className="font-medium text-amber-800">Amount Difference Detected</h3>
              </div>

              {/* Amount Comparison */}
              <div className="bg-white rounded-lg p-3 mb-4 space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Bank Transaction:</span>
                  <span className="font-medium">{formatCurrency(transaction.amount)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">
                    {pendingReconciliation.type === 'credit' ? 'Payment Amount:' : 'Expense Amount:'}
                  </span>
                  <span className="font-medium">{formatCurrency(pendingReconciliation.suggestion.amount)}</span>
                </div>
                <div className="border-t pt-2 flex justify-between text-sm">
                  <span className="font-medium text-amber-700">Difference:</span>
                  <span className={`font-bold ${pendingReconciliation.differenceAmount > 0 ? 'text-green-600' : 'text-red-600'}`}>
                    {pendingReconciliation.differenceAmount > 0 ? '+' : ''}{formatCurrency(pendingReconciliation.differenceAmount)}
                  </span>
                </div>
              </div>

              {/* Difference Classification */}
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

                {/* TDS Section (only when TDS selected) */}
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

                {/* Notes */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Notes (optional)
                  </label>
                  <textarea
                    value={differenceNotes}
                    onChange={(e) => setDifferenceNotes(e.target.value)}
                    placeholder="Add any notes about this difference..."
                    rows={2}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm"
                  />
                </div>

                {/* Action Buttons */}
                <div className="flex gap-3 pt-2">
                  <button
                    onClick={handleCancelConfirmation}
                    className="flex-1 px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleConfirmReconciliation}
                    disabled={reconcileTransaction.isPending}
                    className="flex-1 px-4 py-2 bg-amber-600 text-white rounded-md hover:bg-amber-700 disabled:opacity-50"
                  >
                    {reconcileTransaction.isPending ? 'Saving...' : 'Confirm & Reconcile'}
                  </button>
                </div>
              </div>

              <p className="text-xs text-amber-600 mt-3">
                This will create an adjustment journal entry to account for the difference.
              </p>
            </div>
          )}

          {/* Tabs - Hidden when confirmation is showing */}
          {!pendingReconciliation && (
          <div className="border-b border-gray-200 mb-4">
            <nav className="-mb-px flex space-x-6">
              <button
                onClick={() => setActiveTab('suggestions')}
                className={`py-2 px-1 border-b-2 font-medium text-sm ${
                  activeTab === 'suggestions'
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                Suggestions {suggestions.length > 0 && `(${suggestions.length})`}
              </button>
              <button
                onClick={() => setActiveTab('search')}
                className={`py-2 px-1 border-b-2 font-medium text-sm ${
                  activeTab === 'search'
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                Search
              </button>
              <button
                onClick={() => setActiveTab('manual')}
                className={`py-2 px-1 border-b-2 font-medium text-sm ${
                  activeTab === 'manual'
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                Manual
              </button>
            </nav>
          </div>
          )}

          {/* Tab Content - Scrollable Area (hidden when confirmation is showing) */}
          {!pendingReconciliation && (
          <div className="flex-1 overflow-y-auto">
            {/* Suggestions Tab */}
            {activeTab === 'suggestions' && (
              <SuggestionsTab
                isLoading={suggestionsLoading}
                isCredit={isCredit}
                creditSuggestions={creditSuggestions}
                debitSuggestions={debitSuggestions}
                onReconcileCredit={handleReconcileWithCreditSuggestion}
                onReconcileDebit={handleReconcileWithDebitSuggestion}
                formatCurrency={formatCurrency}
                formatDate={formatDate}
                getMatchScoreColor={getMatchScoreColor}
              />
            )}

            {/* Search Tab */}
            {activeTab === 'search' && (
              <SearchTab
                isDebit={isDebit}
                isCredit={isCredit}
                companyId={companyId}
                transactionAmount={transaction?.amount}
                onReconcileDebit={handleReconcileWithDebitSuggestion}
                onReconcileCredit={handleReconcileWithCreditSuggestion}
              />
            )}

            {/* Manual Tab */}
            {activeTab === 'manual' && (
              <ManualTab
                manualType={manualType}
                setManualType={setManualType}
                manualId={manualId}
                setManualId={setManualId}
                isLoading={reconcileTransaction.isPending}
                onReconcile={handleManualReconcile}
              />
            )}
          </div>
          )}

          {/* Footer Actions (hidden when confirmation is showing) */}
          {!pendingReconciliation && (
          <div className="pt-4 mt-4 border-t flex justify-end">
            <button
              onClick={onClose}
              className="px-4 py-2 text-gray-600 hover:text-gray-800 border border-gray-300 rounded-md"
            >
              Cancel
            </button>
          </div>
          )}
        </div>
      )}
    </Drawer>
  )
}

// Suggestions Tab Component (SRP)
interface SuggestionsTabProps {
  isLoading: boolean
  isCredit: boolean
  creditSuggestions: ReconciliationSuggestion[]
  debitSuggestions: DebitReconciliationSuggestion[]
  onReconcileCredit: (suggestion: ReconciliationSuggestion) => void
  onReconcileDebit: (suggestion: DebitReconciliationSuggestion) => void
  formatCurrency: (amount: number) => string
  formatDate: (dateStr: string) => string
  getMatchScoreColor: (score: number) => string
}

const SuggestionsTab = ({
  isLoading,
  isCredit,
  creditSuggestions,
  debitSuggestions,
  onReconcileCredit,
  onReconcileDebit,
  formatCurrency,
  formatDate,
  getMatchScoreColor
}: SuggestionsTabProps) => {
  const suggestions = isCredit ? creditSuggestions : debitSuggestions

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <RefreshCw className="w-5 h-5 animate-spin text-gray-400" />
        <span className="ml-2 text-gray-500">Finding matches...</span>
      </div>
    )
  }

  if (suggestions.length === 0) {
    return (
      <div className="text-center py-12">
        <CheckCircle className="w-12 h-12 text-gray-300 mx-auto mb-3" />
        <p className="text-gray-500">No matching {isCredit ? 'incoming' : 'outgoing'} payments found</p>
        <p className="text-sm text-gray-400 mt-1">Try the Search tab to find records manually</p>
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {isCredit ? (
        creditSuggestions.map((suggestion) => (
          <CreditSuggestionCard
            key={suggestion.paymentId}
            suggestion={suggestion}
            onClick={() => onReconcileCredit(suggestion)}
            formatCurrency={formatCurrency}
            formatDate={formatDate}
            getMatchScoreColor={getMatchScoreColor}
          />
        ))
      ) : (
        debitSuggestions.map((suggestion) => (
          <DebitSuggestionCard
            key={suggestion.recordId}
            suggestion={suggestion}
            onClick={() => onReconcileDebit(suggestion)}
            formatCurrency={formatCurrency}
            formatDate={formatDate}
            getMatchScoreColor={getMatchScoreColor}
          />
        ))
      )}
    </div>
  )
}

// Credit Suggestion Card (SRP)
interface CreditSuggestionCardProps {
  suggestion: ReconciliationSuggestion
  onClick: () => void
  formatCurrency: (amount: number) => string
  formatDate: (dateStr: string) => string
  getMatchScoreColor: (score: number) => string
}

const CreditSuggestionCard = ({
  suggestion,
  onClick,
  formatCurrency,
  formatDate,
  getMatchScoreColor
}: CreditSuggestionCardProps) => (
  <div
    className="border rounded-lg p-4 hover:border-blue-300 hover:bg-blue-50 cursor-pointer transition-colors"
    onClick={onClick}
  >
    <div className="flex justify-between items-start">
      <div>
        <div className="font-medium text-gray-900">
          {suggestion.customerName || 'Unknown Customer'}
        </div>
        <div className="text-sm text-gray-500 mt-1">
          {suggestion.invoiceNumber && `Invoice: ${suggestion.invoiceNumber} • `}
          {formatDate(suggestion.paymentDate)}
        </div>
      </div>
      <div className="text-right">
        <div className="font-medium text-green-600">
          {formatCurrency(suggestion.amount)}
        </div>
        <span className={`inline-flex px-2 py-0.5 rounded text-xs font-medium mt-1 ${getMatchScoreColor(suggestion.matchScore)}`}>
          {suggestion.matchScore}% match
        </span>
      </div>
    </div>
  </div>
)

// Debit Suggestion Card (SRP)
interface DebitSuggestionCardProps {
  suggestion: DebitReconciliationSuggestion
  onClick: () => void
  formatCurrency: (amount: number) => string
  formatDate: (dateStr: string) => string
  getMatchScoreColor: (score: number) => string
}

const DebitSuggestionCard = ({
  suggestion,
  onClick,
  formatCurrency,
  formatDate,
  getMatchScoreColor
}: DebitSuggestionCardProps) => (
  <div
    className="border rounded-lg p-4 hover:border-blue-300 hover:bg-blue-50 cursor-pointer transition-colors"
    onClick={onClick}
  >
    <div className="flex justify-between items-start">
      <div>
        <div className="flex items-center gap-2">
          <span className="inline-flex px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700">
            {suggestion.recordTypeDisplay}
          </span>
          <span className="font-medium text-gray-900">
            {suggestion.payeeName || 'Unknown'}
          </span>
        </div>
        <div className="text-sm text-gray-500 mt-1">
          {suggestion.description || suggestion.category}
          {suggestion.paymentDate && ` • ${formatDate(suggestion.paymentDate)}`}
        </div>
        {suggestion.tdsAmount && suggestion.tdsAmount > 0 && (
          <div className="text-xs text-purple-600 mt-1">
            TDS {suggestion.tdsSection}: {formatCurrency(suggestion.tdsAmount)}
          </div>
        )}
      </div>
      <div className="text-right">
        <div className="font-medium text-red-600">
          {formatCurrency(suggestion.amount)}
        </div>
        <span className={`inline-flex px-2 py-0.5 rounded text-xs font-medium mt-1 ${getMatchScoreColor(suggestion.matchScore)}`}>
          {suggestion.matchScore}% match
        </span>
        {suggestion.amountDifference > 0 && (
          <div className="text-xs text-orange-500 mt-1">
            Diff: {formatCurrency(suggestion.amountDifference)}
          </div>
        )}
      </div>
    </div>
  </div>
)

// Search Tab Component (SRP) - Uses typeahead for live search
interface SearchTabProps {
  isDebit: boolean
  isCredit: boolean
  companyId: string
  transactionAmount?: number
  onReconcileDebit: (suggestion: DebitReconciliationSuggestion) => void
  onReconcileCredit: (suggestion: ReconciliationSuggestion) => void
}

const SearchTab = ({
  isDebit,
  isCredit,
  companyId,
  transactionAmount,
  onReconcileDebit,
  onReconcileCredit
}: SearchTabProps) => {
  // Search for credit (incoming) transactions
  if (isCredit) {
    return (
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Search Incoming Payments
          </label>
          <p className="text-xs text-gray-500 mb-3">
            Start typing to search by customer name, invoice number, or reference.
            Results are filtered to match payments near the transaction amount.
          </p>
          <PaymentTypeahead
            companyId={companyId}
            onSelect={onReconcileCredit}
            placeholder="Type to search payments..."
            amountHint={transactionAmount}
          />
        </div>
        <div className="border-t pt-4">
          <p className="text-xs text-gray-400">
            Click on a search result to reconcile this bank transaction with the selected payment.
          </p>
        </div>
      </div>
    )
  }

  // Search for debit (outgoing) transactions
  if (isDebit) {
    return (
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Search Outgoing Payments
          </label>
          <p className="text-xs text-gray-500 mb-3">
            Start typing to search by payee name, description, or reference number.
            Results are filtered to match payments near the transaction amount.
          </p>
          <OutgoingPaymentTypeahead
            companyId={companyId}
            onSelect={onReconcileDebit}
            placeholder="Type to search payments..."
            amountHint={transactionAmount}
          />
        </div>
        <div className="border-t pt-4">
          <p className="text-xs text-gray-400">
            Click on a search result to reconcile this bank transaction with the selected payment.
          </p>
        </div>
      </div>
    )
  }

  // Fallback (shouldn't happen)
  return (
    <div className="text-center py-12">
      <Search className="w-12 h-12 text-gray-300 mx-auto mb-3" />
      <p className="text-gray-500">Unable to determine transaction type</p>
    </div>
  )
}

// Manual Tab Component (SRP)
interface ManualTabProps {
  manualType: string
  setManualType: (value: string) => void
  manualId: string
  setManualId: (value: string) => void
  isLoading: boolean
  onReconcile: () => void
}

const ManualTab = ({
  manualType,
  setManualType,
  manualId,
  setManualId,
  isLoading,
  onReconcile
}: ManualTabProps) => (
  <div className="space-y-4">
    <p className="text-sm text-gray-500">
      If you can't find the matching record, enter the details manually.
    </p>
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Type</label>
        <select
          value={manualType}
          onChange={(e) => setManualType(e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm"
        >
          <option value="payment">Payment</option>
          <option value="salary">Salary</option>
          <option value="contractor">Contractor Payment</option>
          <option value="vendor_payment">Vendor Payment</option>
          <option value="expense_claim">Expense Claim</option>
          <option value="subscription">Subscription</option>
          <option value="loan_payment">Loan Payment</option>
          <option value="asset_maintenance">Asset Maintenance</option>
          <option value="tax_payment">Tax Payment</option>
          <option value="transfer">Bank Transfer</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Record ID or Reference</label>
        <input
          type="text"
          placeholder="Enter the ID or reference number"
          value={manualId}
          onChange={(e) => setManualId(e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm"
        />
      </div>
      <button
        onClick={onReconcile}
        disabled={!manualId || isLoading}
        className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 text-sm"
      >
        {isLoading ? 'Saving...' : 'Reconcile'}
      </button>
    </div>
  </div>
)

export default ReconciliationDrawer

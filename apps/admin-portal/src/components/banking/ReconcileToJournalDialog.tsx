import { useState, useMemo } from 'react'
import { Drawer } from '@/components/ui/Drawer'
import { useReconcileToJournal } from '@/hooks/api/useBankTransactions'
import { useJournalEntries, useJournalEntry } from '@/features/ledger/hooks/useLedger'
import {
  BankTransaction,
  JournalEntry,
  JournalEntryLine,
  ReconciliationDifferenceType
} from '@/services/api/types'
import { Search, RefreshCw, FileText, ArrowRight, AlertTriangle, CheckCircle, ArrowLeft } from 'lucide-react'

// Difference types for handling amount mismatches
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

interface ReconcileToJournalDialogProps {
  transaction: BankTransaction | null
  companyId: string
  linkedAccountId?: string
  onClose: () => void
  onReconciled: () => void
  formatCurrency: (amount: number) => string
}

export const ReconcileToJournalDialog = ({
  transaction,
  companyId,
  linkedAccountId,
  onClose,
  onReconciled,
  formatCurrency
}: ReconcileToJournalDialogProps) => {
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedJournalId, setSelectedJournalId] = useState<string | null>(null)
  const [selectedLineId, setSelectedLineId] = useState<string | null>(null)

  // Confirmation step state (when there's a difference)
  const [pendingReconciliation, setPendingReconciliation] = useState<{
    journalEntry: JournalEntry
    journalLine: JournalEntryLine
    differenceAmount: number
  } | null>(null)
  const [differenceType, setDifferenceType] = useState<ReconciliationDifferenceType>('round_off')
  const [differenceNotes, setDifferenceNotes] = useState('')
  const [tdsSection, setTdsSection] = useState('')

  const isDebit = transaction?.transactionType === 'debit'
  const DIFFERENCE_THRESHOLD = 10

  // Fetch journal entries for the company
  const { data: journalEntries = [], isLoading: entriesLoading } = useJournalEntries(companyId, !!companyId)

  // Fetch selected journal entry with lines
  const { data: selectedJournal, isLoading: journalLoading } = useJournalEntry(
    selectedJournalId || '',
    !!selectedJournalId
  )

  const reconcileToJournal = useReconcileToJournal()

  // Filter journal entries based on search term
  const filteredEntries = useMemo(() => {
    if (!searchTerm) return journalEntries.slice(0, 20) // Show first 20 by default

    const search = searchTerm.toLowerCase()
    return journalEntries.filter(entry => {
      return (
        entry.journalNumber?.toLowerCase().includes(search) ||
        entry.description?.toLowerCase().includes(search) ||
        entry.sourceNumber?.toLowerCase().includes(search)
      )
    }).slice(0, 20)
  }, [journalEntries, searchTerm])

  // Filter journal lines to show only relevant ones (matching bank account)
  const relevantLines = useMemo(() => {
    if (!selectedJournal?.lines) return []

    // If we have a linked account ID, filter for that account
    if (linkedAccountId) {
      return selectedJournal.lines.filter(line => line.accountId === linkedAccountId)
    }

    // Otherwise, show all lines and let user select
    return selectedJournal.lines
  }, [selectedJournal, linkedAccountId])

  const handleSelectJournalLine = (journalEntry: JournalEntry, line: JournalEntryLine) => {
    if (!transaction) return

    // Calculate line amount based on debit/credit
    const lineAmount = isDebit ? line.creditAmount : line.debitAmount
    const difference = transaction.amount - lineAmount

    // If there's a significant difference, show confirmation
    if (Math.abs(difference) > DIFFERENCE_THRESHOLD) {
      setPendingReconciliation({
        journalEntry,
        journalLine: line,
        differenceAmount: difference
      })
      // Pre-select likely difference type
      if (difference > 0) {
        setDifferenceType(isDebit ? 'bank_charges' : 'bank_interest')
      } else {
        setDifferenceType(isDebit ? 'round_off' : 'tds_deducted')
      }
      return
    }

    // No significant difference, reconcile directly
    performReconciliation(journalEntry.id, line.id)
  }

  const performReconciliation = async (
    journalEntryId: string,
    journalEntryLineId: string,
    diffType?: ReconciliationDifferenceType,
    diffNotes?: string,
    tdsSec?: string
  ) => {
    if (!transaction) return

    try {
      await reconcileToJournal.mutateAsync({
        id: transaction.id,
        data: {
          journalEntryId,
          journalEntryLineId,
          reconciledBy: 'user',
          notes: diffNotes,
          differenceAmount: pendingReconciliation ? pendingReconciliation.differenceAmount : undefined,
          differenceType: diffType,
          differenceNotes: diffNotes,
          tdsSection: diffType === 'tds_deducted' ? tdsSec : undefined
        }
      })

      // Reset state
      setPendingReconciliation(null)
      setSelectedJournalId(null)
      setSelectedLineId(null)
      setDifferenceType('round_off')
      setDifferenceNotes('')
      setTdsSection('')
      onReconciled()
    } catch (error) {
      console.error('Failed to reconcile to journal:', error)
    }
  }

  const handleConfirmReconciliation = () => {
    if (!pendingReconciliation) return

    performReconciliation(
      pendingReconciliation.journalEntry.id,
      pendingReconciliation.journalLine.id,
      differenceType,
      differenceNotes || undefined,
      tdsSection || undefined
    )
  }

  const handleCancelConfirmation = () => {
    setPendingReconciliation(null)
    setDifferenceType('round_off')
    setDifferenceNotes('')
    setTdsSection('')
  }

  const handleBack = () => {
    setSelectedJournalId(null)
    setSelectedLineId(null)
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    })
  }

  return (
    <Drawer
      isOpen={!!transaction}
      onClose={onClose}
      title="Reconcile to Journal Entry"
      size="lg"
    >
      {transaction && (
        <div className="flex flex-col h-full">
          {/* Transaction Details */}
          <div className="bg-gray-50 rounded-lg p-4 mb-4">
            <div className="flex justify-between items-center">
              <div>
                <span className={`inline-flex items-center px-2 py-1 rounded text-xs font-medium ${
                  isDebit ? 'bg-red-100 text-red-800' : 'bg-green-100 text-green-800'
                }`}>
                  {isDebit ? 'Debit (Outgoing)' : 'Credit (Incoming)'}
                </span>
                <span className="ml-2 text-sm text-gray-500">{formatDate(transaction.transactionDate)}</span>
              </div>
              <div className={`text-lg font-semibold ${isDebit ? 'text-red-600' : 'text-green-600'}`}>
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
                  <span className="text-gray-600">Journal Entry Line:</span>
                  <span className="font-medium">
                    {formatCurrency(isDebit
                      ? pendingReconciliation.journalLine.creditAmount
                      : pendingReconciliation.journalLine.debitAmount
                    )}
                  </span>
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
                    placeholder="Add any notes about this reconciliation..."
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
                    disabled={reconcileToJournal.isPending}
                    className="flex-1 px-4 py-2 bg-amber-600 text-white rounded-md hover:bg-amber-700 disabled:opacity-50"
                  >
                    {reconcileToJournal.isPending ? 'Saving...' : 'Confirm & Reconcile'}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Main Content - Hidden when confirmation is showing */}
          {!pendingReconciliation && (
            <div className="flex-1 overflow-y-auto">
              {/* Journal Entry Selection */}
              {!selectedJournalId && (
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Search Journal Entries
                    </label>
                    <div className="relative">
                      <Search className="h-4 w-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
                      <input
                        type="text"
                        placeholder="Search by number, description, reference..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-md text-sm"
                      />
                    </div>
                  </div>

                  {entriesLoading ? (
                    <div className="flex items-center justify-center py-12">
                      <RefreshCw className="w-5 h-5 animate-spin text-gray-400" />
                      <span className="ml-2 text-gray-500">Loading journal entries...</span>
                    </div>
                  ) : filteredEntries.length === 0 ? (
                    <div className="text-center py-12">
                      <FileText className="w-12 h-12 text-gray-300 mx-auto mb-3" />
                      <p className="text-gray-500">No journal entries found</p>
                      <p className="text-sm text-gray-400 mt-1">Try a different search term</p>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      {filteredEntries.map((entry) => (
                        <JournalEntryCard
                          key={entry.id}
                          entry={entry}
                          onClick={() => setSelectedJournalId(entry.id)}
                          formatCurrency={formatCurrency}
                          formatDate={formatDate}
                        />
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* Journal Entry Line Selection */}
              {selectedJournalId && (
                <div className="space-y-4">
                  <button
                    onClick={handleBack}
                    className="flex items-center gap-2 text-sm text-gray-600 hover:text-gray-800"
                  >
                    <ArrowLeft className="w-4 h-4" />
                    Back to journal entries
                  </button>

                  {journalLoading ? (
                    <div className="flex items-center justify-center py-12">
                      <RefreshCw className="w-5 h-5 animate-spin text-gray-400" />
                      <span className="ml-2 text-gray-500">Loading journal entry...</span>
                    </div>
                  ) : selectedJournal ? (
                    <div className="space-y-4">
                      {/* Journal Header */}
                      <div className="bg-blue-50 rounded-lg p-4">
                        <div className="flex items-center gap-2 mb-2">
                          <FileText className="w-5 h-5 text-blue-600" />
                          <span className="font-medium text-blue-900">
                            {selectedJournal.journalNumber}
                          </span>
                          <span className={`ml-auto px-2 py-0.5 rounded text-xs font-medium ${
                            selectedJournal.status === 'posted'
                              ? 'bg-green-100 text-green-700'
                              : 'bg-yellow-100 text-yellow-700'
                          }`}>
                            {selectedJournal.status}
                          </span>
                        </div>
                        <div className="text-sm text-gray-600">{selectedJournal.description}</div>
                        <div className="text-xs text-gray-500 mt-1">{formatDate(selectedJournal.journalDate)}</div>
                      </div>

                      {/* Line Selection Instructions */}
                      <div className="text-sm text-gray-600">
                        <p className="font-medium">Select the line to reconcile with:</p>
                        <p className="text-xs text-gray-500 mt-1">
                          {isDebit
                            ? 'For a bank debit (outgoing), select the credit line (money leaving).'
                            : 'For a bank credit (incoming), select the debit line (money entering).'}
                        </p>
                      </div>

                      {/* Journal Lines */}
                      {relevantLines.length > 0 ? (
                        <div className="space-y-2">
                          {relevantLines.map((line) => (
                            <JournalLineCard
                              key={line.id}
                              line={line}
                              isDebit={isDebit}
                              selectedLineId={selectedLineId}
                              onClick={() => handleSelectJournalLine(selectedJournal, line)}
                              formatCurrency={formatCurrency}
                            />
                          ))}
                        </div>
                      ) : (
                        <div className="text-center py-8 text-gray-500">
                          <p>No matching lines found for this bank account.</p>
                          <p className="text-xs mt-1">Make sure the bank account is linked to a ledger account.</p>
                        </div>
                      )}

                      {/* Show all lines if no linked account filter */}
                      {!linkedAccountId && selectedJournal.lines && selectedJournal.lines.length > 0 && (
                        <div className="space-y-2">
                          <p className="text-sm font-medium text-gray-700">All Lines:</p>
                          {selectedJournal.lines.map((line) => (
                            <JournalLineCard
                              key={line.id}
                              line={line}
                              isDebit={isDebit}
                              selectedLineId={selectedLineId}
                              onClick={() => handleSelectJournalLine(selectedJournal, line)}
                              formatCurrency={formatCurrency}
                            />
                          ))}
                        </div>
                      )}
                    </div>
                  ) : (
                    <div className="text-center py-12 text-gray-500">
                      Failed to load journal entry
                    </div>
                  )}
                </div>
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

// Journal Entry Card Component
interface JournalEntryCardProps {
  entry: JournalEntry
  onClick: () => void
  formatCurrency: (amount: number) => string
  formatDate: (dateStr: string) => string
}

const JournalEntryCard = ({ entry, onClick, formatCurrency, formatDate }: JournalEntryCardProps) => (
  <div
    className="border rounded-lg p-4 hover:border-blue-300 hover:bg-blue-50 cursor-pointer transition-colors"
    onClick={onClick}
  >
    <div className="flex justify-between items-start">
      <div>
        <div className="flex items-center gap-2">
          <FileText className="w-4 h-4 text-gray-400" />
          <span className="font-medium text-gray-900">{entry.journalNumber}</span>
          <span className={`px-2 py-0.5 rounded text-xs font-medium ${
            entry.status === 'posted' ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'
          }`}>
            {entry.status}
          </span>
        </div>
        <div className="text-sm text-gray-600 mt-1">{entry.description || 'No description'}</div>
        <div className="text-xs text-gray-400 mt-1">{formatDate(entry.journalDate)}</div>
      </div>
      <div className="text-right">
        <div className="font-medium text-gray-900">{formatCurrency(entry.totalDebit || 0)}</div>
        <ArrowRight className="w-4 h-4 text-gray-400 mt-1 ml-auto" />
      </div>
    </div>
  </div>
)

// Journal Line Card Component
interface JournalLineCardProps {
  line: JournalEntryLine
  isDebit: boolean
  selectedLineId: string | null
  onClick: () => void
  formatCurrency: (amount: number) => string
}

const JournalLineCard = ({ line, isDebit, selectedLineId, onClick, formatCurrency }: JournalLineCardProps) => {
  const isSelected = selectedLineId === line.id
  const hasDebit = line.debitAmount > 0
  const hasCredit = line.creditAmount > 0

  // Highlight the relevant amount based on bank transaction type
  const relevantAmount = isDebit ? line.creditAmount : line.debitAmount
  const isRelevant = relevantAmount > 0

  return (
    <div
      className={`border rounded-lg p-3 cursor-pointer transition-colors ${
        isSelected
          ? 'border-blue-500 bg-blue-50'
          : isRelevant
            ? 'hover:border-blue-300 hover:bg-blue-50'
            : 'opacity-60 hover:opacity-100'
      }`}
      onClick={onClick}
    >
      <div className="flex justify-between items-center">
        <div className="flex-1">
          <div className="font-medium text-gray-900 text-sm">
            {line.accountCode} - {line.accountName}
          </div>
          {line.description && (
            <div className="text-xs text-gray-500 mt-0.5">{line.description}</div>
          )}
        </div>
        <div className="flex gap-4 text-sm">
          <div className={`text-right ${hasDebit ? 'font-medium' : 'text-gray-400'}`}>
            <div className="text-xs text-gray-500">Debit</div>
            <div className={hasDebit ? (isDebit ? 'text-gray-600' : 'text-blue-600') : ''}>
              {formatCurrency(line.debitAmount)}
            </div>
          </div>
          <div className={`text-right ${hasCredit ? 'font-medium' : 'text-gray-400'}`}>
            <div className="text-xs text-gray-500">Credit</div>
            <div className={hasCredit ? (isDebit ? 'text-blue-600' : 'text-gray-600') : ''}>
              {formatCurrency(line.creditAmount)}
            </div>
          </div>
        </div>
      </div>
      {isRelevant && (
        <div className="mt-2 flex items-center gap-1 text-xs text-blue-600">
          <CheckCircle className="w-3 h-3" />
          Recommended for {isDebit ? 'debit' : 'credit'} reconciliation
        </div>
      )}
    </div>
  )
}

export default ReconcileToJournalDialog

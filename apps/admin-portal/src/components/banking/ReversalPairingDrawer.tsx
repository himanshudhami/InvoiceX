import { useState } from 'react'
import { Drawer } from '@/components/ui/Drawer'
import {
  useReversalDetection,
  usePairReversal
} from '@/hooks/api/useBankTransactions'
import { BankTransaction, ReversalMatchSuggestion } from '@/services/api/types'
import { AlertTriangle, Link2, RefreshCw, CheckCircle, AlertCircle, BookOpen } from 'lucide-react'

interface ReversalPairingDrawerProps {
  transaction: BankTransaction | null
  onClose: () => void
  onPaired: () => void
  formatCurrency: (amount: number) => string
}

export const ReversalPairingDrawer = ({
  transaction,
  onClose,
  onPaired,
  formatCurrency
}: ReversalPairingDrawerProps) => {
  const [selectedOriginal, setSelectedOriginal] = useState<ReversalMatchSuggestion | null>(null)
  const [originalWasPosted, setOriginalWasPosted] = useState<boolean | null>(null)

  // Detect reversal and get suggestions
  const { data: detectionResult, isLoading: detectingReversal } = useReversalDetection(
    transaction?.id || '',
    !!transaction
  )

  const pairReversal = usePairReversal()

  const handlePair = async () => {
    if (!transaction || !selectedOriginal || originalWasPosted === null) return

    try {
      await pairReversal.mutateAsync({
        reversalTransactionId: transaction.id,
        originalTransactionId: selectedOriginal.transactionId,
        originalWasPostedToLedger: originalWasPosted,
        pairedBy: 'user'
      })
      onPaired()
    } catch (error) {
      console.error('Failed to pair reversal:', error)
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
      title="Pair Reversal Transaction"
      size="lg"
    >
      {transaction && (
        <div className="flex flex-col h-full">
          {/* Reversal Alert Banner */}
          <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 mb-4">
            <div className="flex items-start gap-3">
              <AlertTriangle className="w-5 h-5 text-amber-500 mt-0.5" />
              <div>
                <h4 className="font-medium text-amber-800">Reversal Transaction Detected</h4>
                <p className="text-sm text-amber-700 mt-1">
                  This appears to be a reversal of a failed transaction.
                  {detectionResult?.detectedPattern && (
                    <span className="ml-1">
                      Pattern: <code className="bg-amber-100 px-1 rounded">{detectionResult.detectedPattern}</code>
                    </span>
                  )}
                </p>
              </div>
            </div>
          </div>

          {/* Transaction Details */}
          <div className="bg-gray-50 rounded-lg p-4 mb-4">
            <div className="flex justify-between items-center">
              <div>
                <span className="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-green-100 text-green-800">
                  Credit (Reversal)
                </span>
                <span className="ml-2 text-sm text-gray-500">{formatDate(transaction.transactionDate)}</span>
              </div>
              <div className="text-lg font-semibold text-green-600">
                +{formatCurrency(transaction.amount)}
              </div>
            </div>
            <div className="mt-2 text-sm text-gray-600">{transaction.description || 'No description'}</div>
            {transaction.referenceNumber && (
              <div className="mt-1 text-xs text-gray-400 font-mono">Ref: {transaction.referenceNumber}</div>
            )}
          </div>

          {/* Loading State */}
          {detectingReversal && (
            <div className="flex items-center justify-center py-12">
              <RefreshCw className="w-5 h-5 animate-spin text-gray-400" />
              <span className="ml-2 text-gray-500">Finding matching original transaction...</span>
            </div>
          )}

          {/* Suggested Originals */}
          {!detectingReversal && detectionResult && (
            <div className="flex-1 overflow-y-auto">
              <h3 className="text-sm font-medium text-gray-700 mb-3">
                <Link2 className="w-4 h-4 inline mr-1" />
                Select Original Transaction to Pair
              </h3>

              {detectionResult.suggestedOriginals.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  <AlertCircle className="w-12 h-12 text-gray-300 mx-auto mb-3" />
                  <p>No matching original transactions found</p>
                  <p className="text-sm text-gray-400 mt-1">
                    The original transaction may have already been paired or deleted
                  </p>
                </div>
              ) : (
                <div className="space-y-3">
                  {detectionResult.suggestedOriginals.map((suggestion) => (
                    <div
                      key={suggestion.transactionId}
                      className={`border rounded-lg p-4 cursor-pointer transition-colors ${
                        selectedOriginal?.transactionId === suggestion.transactionId
                          ? 'border-blue-500 bg-blue-50'
                          : 'hover:border-gray-300 hover:bg-gray-50'
                      }`}
                      onClick={() => setSelectedOriginal(suggestion)}
                    >
                      <div className="flex justify-between items-start">
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <span className={`inline-flex px-2 py-0.5 rounded text-xs font-medium ${getMatchScoreColor(suggestion.matchScore)}`}>
                              {suggestion.matchScore}% match
                            </span>
                            <span className="text-sm text-gray-500">{formatDate(suggestion.transactionDate)}</span>
                          </div>
                          <div className="mt-2 text-sm text-gray-600">
                            {suggestion.description || 'No description'}
                          </div>
                          <div className="mt-1 text-xs text-gray-400">
                            {suggestion.matchReason}
                          </div>
                          {suggestion.isReconciled && (
                            <div className="mt-2 flex items-center gap-1 text-xs text-purple-600">
                              <BookOpen className="w-3 h-3" />
                              Already reconciled with {suggestion.reconciledType}
                            </div>
                          )}
                        </div>
                        <div className="text-right">
                          <div className="font-medium text-red-600">
                            -{formatCurrency(suggestion.amount)}
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {/* Ledger Question */}
              {selectedOriginal && (
                <div className="mt-6 border-t pt-4">
                  <h4 className="text-sm font-medium text-gray-700 mb-3">
                    Was the original transaction posted to ledger?
                  </h4>
                  <div className="space-y-2">
                    <label className={`flex items-start gap-3 p-3 border rounded-lg cursor-pointer ${
                      originalWasPosted === false ? 'border-blue-500 bg-blue-50' : 'hover:bg-gray-50'
                    }`}>
                      <input
                        type="radio"
                        name="posted"
                        checked={originalWasPosted === false}
                        onChange={() => setOriginalWasPosted(false)}
                        className="mt-0.5"
                      />
                      <div>
                        <div className="font-medium text-gray-700">No, not posted to books</div>
                        <div className="text-sm text-gray-500">
                          Both transactions will be marked as cancelled. No ledger entry needed.
                        </div>
                      </div>
                    </label>
                    <label className={`flex items-start gap-3 p-3 border rounded-lg cursor-pointer ${
                      originalWasPosted === true ? 'border-blue-500 bg-blue-50' : 'hover:bg-gray-50'
                    }`}>
                      <input
                        type="radio"
                        name="posted"
                        checked={originalWasPosted === true}
                        onChange={() => setOriginalWasPosted(true)}
                        className="mt-0.5"
                      />
                      <div>
                        <div className="font-medium text-gray-700">Yes, already posted to ledger</div>
                        <div className="text-sm text-gray-500">
                          A reversal journal entry will be created to correct the books.
                        </div>
                        {selectedOriginal.isReconciled && (
                          <div className="mt-1 text-xs text-amber-600">
                            Note: This was reconciled with {selectedOriginal.reconciledType}
                          </div>
                        )}
                      </div>
                    </label>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Footer Actions */}
          <div className="pt-4 mt-4 border-t flex justify-between items-center">
            <button
              onClick={onClose}
              className="px-4 py-2 text-gray-600 hover:text-gray-800 border border-gray-300 rounded-md"
            >
              Cancel
            </button>
            <button
              onClick={handlePair}
              disabled={!selectedOriginal || originalWasPosted === null || pairReversal.isPending}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
            >
              {pairReversal.isPending ? (
                <>
                  <RefreshCw className="w-4 h-4 animate-spin" />
                  Pairing...
                </>
              ) : (
                <>
                  <CheckCircle className="w-4 h-4" />
                  Pair Transactions
                </>
              )}
            </button>
          </div>
        </div>
      )}
    </Drawer>
  )
}

export default ReversalPairingDrawer

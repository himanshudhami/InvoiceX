import { AsyncTypeahead, TypeaheadOption } from '@/components/ui/AsyncTypeahead'
import { bankTransactionService } from '@/services/api/finance/banking/bankTransactionService'
import { ReconciliationSuggestion } from '@/services/api/types'
import { Wallet, FileText, User } from 'lucide-react'
import { cn } from '@/lib/utils'

interface PaymentTypeaheadProps {
  companyId: string
  onSelect: (payment: ReconciliationSuggestion) => void
  placeholder?: string
  className?: string
  amountHint?: number
}

/**
 * PaymentTypeahead - Typeahead for searching incoming payments (credit reconciliation)
 * Follows SRP: Only handles incoming payment search and selection
 */
export function PaymentTypeahead({
  companyId,
  onSelect,
  placeholder = "Search by customer, invoice, or reference...",
  className,
  amountHint,
}: PaymentTypeaheadProps) {

  const handleSearch = async (query: string): Promise<TypeaheadOption<ReconciliationSuggestion>[]> => {
    if (!companyId) return []

    try {
      const results = await bankTransactionService.searchPayments({
        companyId,
        searchTerm: query,
        amountMin: amountHint ? amountHint * 0.8 : undefined,
        amountMax: amountHint ? amountHint * 1.2 : undefined,
        maxResults: 10
      })

      return results.map(item => ({
        id: item.paymentId,
        label: item.customerName || 'Unknown Customer',
        description: `${item.invoiceNumber || 'No Invoice'} - ${formatCurrency(item.amount)}`,
        subLabel: item.referenceNumber || item.paymentMethod,
        data: item
      }))
    } catch (error) {
      console.error('Failed to search payments:', error)
      return []
    }
  }

  const handleSelect = (option: TypeaheadOption<ReconciliationSuggestion>) => {
    onSelect(option.data)
  }

  const renderOption = (
    option: TypeaheadOption<ReconciliationSuggestion>,
    active: boolean,
    _selected: boolean
  ) => {
    const payment = option.data

    return (
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0 mt-0.5">
          <Wallet className="size-4 text-green-500" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-2 min-w-0">
              <User className="size-3 text-gray-400" />
              <span className={cn("font-medium truncate", active && "text-blue-900")}>
                {payment.customerName || 'Unknown Customer'}
              </span>
            </div>
            <span className={cn(
              "flex-shrink-0 text-sm font-medium",
              active ? "text-blue-700" : "text-gray-900"
            )}>
              {formatCurrency(payment.amount)}
            </span>
          </div>
          <div className="flex items-center gap-2 mt-0.5">
            {payment.invoiceNumber && (
              <span className={cn(
                "inline-flex items-center gap-1 px-1.5 py-0.5 text-xs rounded",
                active ? "bg-blue-100 text-blue-700" : "bg-gray-100 text-gray-600"
              )}>
                <FileText className="size-3" />
                {payment.invoiceNumber}
              </span>
            )}
            {payment.paymentDate && (
              <span className="text-xs text-gray-500">
                {formatDate(payment.paymentDate)}
              </span>
            )}
          </div>
          {(payment.referenceNumber || payment.paymentMethod) && (
            <p className="text-xs text-gray-500 truncate mt-0.5">
              {[payment.paymentMethod, payment.referenceNumber].filter(Boolean).join(' - ')}
            </p>
          )}
          {payment.matchScore !== undefined && payment.matchScore > 0 && (
            <div className="flex items-center gap-1 mt-1">
              <span className={cn(
                "text-xs font-medium",
                payment.matchScore >= 80 ? "text-green-600" :
                payment.matchScore >= 50 ? "text-yellow-600" : "text-gray-500"
              )}>
                {payment.matchScore}% match
              </span>
            </div>
          )}
        </div>
      </div>
    )
  }

  return (
    <AsyncTypeahead
      placeholder={placeholder}
      className={className}
      minChars={2}
      debounceMs={300}
      onSearch={handleSearch}
      onSelect={handleSelect}
      renderOption={renderOption}
      emptyMessage="No matching payments found"
      loadingMessage="Searching payments..."
    />
  )
}

// Helper functions
function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 0,
  }).format(amount)
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  })
}

export default PaymentTypeahead

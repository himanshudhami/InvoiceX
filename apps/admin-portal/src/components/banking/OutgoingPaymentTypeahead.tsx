import { AsyncTypeahead, TypeaheadOption } from '@/components/ui/AsyncTypeahead'
import { bankTransactionService } from '@/services/api/finance/banking/bankTransactionService'
import { DebitReconciliationSuggestion } from '@/services/api/types'
import { Banknote, User, Receipt, CreditCard, Building, Wrench } from 'lucide-react'
import { cn } from '@/lib/utils'

interface OutgoingPaymentTypeaheadProps {
  companyId: string
  onSelect: (payment: DebitReconciliationSuggestion) => void
  placeholder?: string
  className?: string
  amountHint?: number
  selectedTypes?: string[]
}

// Map payment types to icons (SRP: Icon mapping responsibility)
const typeIcons: Record<string, React.ReactNode> = {
  salary: <User className="size-4 text-blue-500" />,
  contractor: <User className="size-4 text-purple-500" />,
  expense_claim: <Receipt className="size-4 text-orange-500" />,
  subscription: <CreditCard className="size-4 text-green-500" />,
  loan_payment: <Building className="size-4 text-red-500" />,
  asset_maintenance: <Wrench className="size-4 text-gray-500" />,
}

// Map payment types to display names (SRP: Display name mapping)
const typeDisplayNames: Record<string, string> = {
  salary: 'Salary',
  contractor: 'Contractor',
  expense_claim: 'Expense',
  subscription: 'Subscription',
  loan_payment: 'Loan',
  asset_maintenance: 'Maintenance',
}

/**
 * OutgoingPaymentTypeahead - Typeahead for searching outgoing payments
 * Follows SRP: Only handles outgoing payment search and selection
 */
export function OutgoingPaymentTypeahead({
  companyId,
  onSelect,
  placeholder = "Search by name, description, or reference...",
  className,
  amountHint,
  selectedTypes,
}: OutgoingPaymentTypeaheadProps) {

  const handleSearch = async (query: string): Promise<TypeaheadOption<DebitReconciliationSuggestion>[]> => {
    if (!companyId) return []

    try {
      const response = await bankTransactionService.searchReconciliationCandidates({
        companyId,
        transactionType: 'debit',
        searchTerm: query,
        amountMin: amountHint ? amountHint * 0.8 : undefined,
        amountMax: amountHint ? amountHint * 1.2 : undefined,
        recordTypes: selectedTypes?.length ? selectedTypes : undefined,
        includeReconciled: false,
        pageNumber: 1,
        pageSize: 10
      })

      return response.items.map(item => ({
        id: item.recordId,
        label: item.payeeName || 'Unknown',
        description: `${typeDisplayNames[item.recordType] || item.recordType} - ${formatCurrency(item.amount)}`,
        subLabel: item.description || item.referenceNumber,
        data: item
      }))
    } catch (error) {
      console.error('Failed to search outgoing payments:', error)
      return []
    }
  }

  const handleSelect = (option: TypeaheadOption<DebitReconciliationSuggestion>) => {
    onSelect(option.data)
  }

  const renderOption = (
    option: TypeaheadOption<DebitReconciliationSuggestion>,
    active: boolean,
    _selected: boolean
  ) => {
    const payment = option.data
    const icon = typeIcons[payment.recordType] || <Banknote className="size-4 text-gray-400" />

    return (
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0 mt-0.5">
          {icon}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between gap-2">
            <span className={cn("font-medium truncate", active && "text-blue-900")}>
              {payment.payeeName || 'Unknown'}
            </span>
            <span className={cn(
              "flex-shrink-0 text-sm font-medium",
              active ? "text-blue-700" : "text-gray-900"
            )}>
              {formatCurrency(payment.amount)}
            </span>
          </div>
          <div className="flex items-center gap-2 mt-0.5">
            <span className={cn(
              "inline-flex px-1.5 py-0.5 text-xs rounded",
              active ? "bg-blue-100 text-blue-700" : "bg-gray-100 text-gray-600"
            )}>
              {typeDisplayNames[payment.recordType] || payment.recordType}
            </span>
            {payment.paymentDate && (
              <span className="text-xs text-gray-500">
                {formatDate(payment.paymentDate)}
              </span>
            )}
          </div>
          {payment.description && (
            <p className="text-xs text-gray-500 truncate mt-0.5">
              {payment.description}
            </p>
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

// Helper functions (could be moved to utils if used elsewhere)
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

export default OutgoingPaymentTypeahead

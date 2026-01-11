import { FC, useState, useEffect, useCallback } from 'react'
import { IndianRupee, X } from 'lucide-react'

interface QuickAmountOption {
  label: string
  min?: number
  max?: number
}

const QUICK_AMOUNT_OPTIONS: QuickAmountOption[] = [
  { label: 'Any', min: undefined, max: undefined },
  { label: '< 10K', min: undefined, max: 10000 },
  { label: '10K - 50K', min: 10000, max: 50000 },
  { label: '50K - 1L', min: 50000, max: 100000 },
  { label: '1L - 5L', min: 100000, max: 500000 },
  { label: '> 5L', min: 500000, max: undefined },
]

interface AmountRangeFilterProps {
  minAmount: string
  maxAmount: string
  onMinAmountChange: (amount: string) => void
  onMaxAmountChange: (amount: string) => void
  onClear?: () => void
  showQuickOptions?: boolean
  className?: string
  compact?: boolean
  currency?: string
  debounceMs?: number
}

export const AmountRangeFilter: FC<AmountRangeFilterProps> = ({
  minAmount,
  maxAmount,
  onMinAmountChange,
  onMaxAmountChange,
  onClear,
  showQuickOptions = true,
  className = '',
  compact = false,
  currency = 'INR',
  debounceMs = 300,
}) => {
  // Local state for debounced input
  const [localMin, setLocalMin] = useState(minAmount)
  const [localMax, setLocalMax] = useState(maxAmount)

  // Sync local state with props
  useEffect(() => {
    setLocalMin(minAmount)
  }, [minAmount])

  useEffect(() => {
    setLocalMax(maxAmount)
  }, [maxAmount])

  // Debounced update for min
  useEffect(() => {
    const timer = setTimeout(() => {
      if (localMin !== minAmount) {
        onMinAmountChange(localMin)
      }
    }, debounceMs)
    return () => clearTimeout(timer)
  }, [localMin, minAmount, onMinAmountChange, debounceMs])

  // Debounced update for max
  useEffect(() => {
    const timer = setTimeout(() => {
      if (localMax !== maxAmount) {
        onMaxAmountChange(localMax)
      }
    }, debounceMs)
    return () => clearTimeout(timer)
  }, [localMax, maxAmount, onMaxAmountChange, debounceMs])

  const hasAmountFilter = minAmount || maxAmount

  const handleQuickSelect = useCallback((option: QuickAmountOption) => {
    const min = option.min?.toString() ?? ''
    const max = option.max?.toString() ?? ''
    setLocalMin(min)
    setLocalMax(max)
    onMinAmountChange(min)
    onMaxAmountChange(max)
  }, [onMinAmountChange, onMaxAmountChange])

  const handleClear = useCallback(() => {
    setLocalMin('')
    setLocalMax('')
    onMinAmountChange('')
    onMaxAmountChange('')
    onClear?.()
  }, [onMinAmountChange, onMaxAmountChange, onClear])

  const formatDisplayValue = (value: string): string => {
    if (!value) return ''
    const num = parseFloat(value)
    if (isNaN(num)) return value
    if (num >= 100000) return `${(num / 100000).toFixed(1)}L`
    if (num >= 1000) return `${(num / 1000).toFixed(0)}K`
    return value
  }

  const getActivePreset = (): string | null => {
    const min = minAmount ? parseFloat(minAmount) : undefined
    const max = maxAmount ? parseFloat(maxAmount) : undefined

    for (const option of QUICK_AMOUNT_OPTIONS) {
      if (option.min === min && option.max === max) {
        return option.label
      }
    }
    return null
  }

  const activePreset = getActivePreset()

  if (compact) {
    return (
      <div className={`flex items-center gap-2 ${className}`}>
        <IndianRupee className="h-4 w-4 text-gray-400" />
        <input
          type="number"
          value={localMin}
          onChange={(e) => setLocalMin(e.target.value)}
          placeholder="Min"
          min="0"
          className="px-2 py-1.5 text-sm border border-gray-300 rounded-md w-24"
        />
        <span className="text-gray-400">-</span>
        <input
          type="number"
          value={localMax}
          onChange={(e) => setLocalMax(e.target.value)}
          placeholder="Max"
          min={localMin || '0'}
          className="px-2 py-1.5 text-sm border border-gray-300 rounded-md w-24"
        />
        {hasAmountFilter && (
          <button
            onClick={handleClear}
            className="p-1 text-gray-400 hover:text-gray-600 rounded"
            title="Clear amount filter"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </div>
    )
  }

  return (
    <div className={`space-y-3 ${className}`}>
      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2">
          <IndianRupee className="h-4 w-4 text-gray-400" />
          <span className="text-sm font-medium text-gray-700">Amount:</span>
        </div>
        <div className="relative">
          <span className="absolute left-2 top-1/2 -translate-y-1/2 text-gray-400 text-xs">
            {currency === 'INR' ? '₹' : currency}
          </span>
          <input
            type="number"
            value={localMin}
            onChange={(e) => setLocalMin(e.target.value)}
            placeholder="Min"
            min="0"
            className="pl-6 pr-3 py-1.5 text-sm border border-gray-300 rounded-md w-28"
          />
        </div>
        <span className="text-gray-400">to</span>
        <div className="relative">
          <span className="absolute left-2 top-1/2 -translate-y-1/2 text-gray-400 text-xs">
            {currency === 'INR' ? '₹' : currency}
          </span>
          <input
            type="number"
            value={localMax}
            onChange={(e) => setLocalMax(e.target.value)}
            placeholder="Max"
            min={localMin || '0'}
            className="pl-6 pr-3 py-1.5 text-sm border border-gray-300 rounded-md w-28"
          />
        </div>
        {hasAmountFilter && (
          <button
            onClick={handleClear}
            className="p-1 text-gray-400 hover:text-gray-600 rounded"
            title="Clear amount filter"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </div>

      {showQuickOptions && (
        <div className="flex flex-wrap gap-1">
          {QUICK_AMOUNT_OPTIONS.map((option) => {
            const isActive = activePreset === option.label
            return (
              <button
                key={option.label}
                onClick={() => handleQuickSelect(option)}
                className={`px-2 py-1 text-xs font-medium rounded transition-colors ${
                  isActive
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-600 bg-gray-100 hover:bg-gray-200'
                }`}
              >
                {option.label}
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}

export default AmountRangeFilter

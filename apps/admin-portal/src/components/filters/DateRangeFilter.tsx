import { FC, useMemo } from 'react'
import { format, startOfMonth, endOfMonth, subMonths, subDays } from 'date-fns'
import { Calendar, X } from 'lucide-react'

export interface DateRange {
  fromDate: string
  toDate: string
}

interface QuickDateOption {
  label: string
  getRange: () => DateRange
}

const getQuickDateOptions = (): QuickDateOption[] => {
  const now = new Date()

  return [
    {
      label: 'Today',
      getRange: () => ({
        fromDate: format(now, 'yyyy-MM-dd'),
        toDate: format(now, 'yyyy-MM-dd'),
      }),
    },
    {
      label: 'Last 7 days',
      getRange: () => ({
        fromDate: format(subDays(now, 6), 'yyyy-MM-dd'),
        toDate: format(now, 'yyyy-MM-dd'),
      }),
    },
    {
      label: 'Last 30 days',
      getRange: () => ({
        fromDate: format(subDays(now, 29), 'yyyy-MM-dd'),
        toDate: format(now, 'yyyy-MM-dd'),
      }),
    },
    {
      label: 'MTD',
      getRange: () => ({
        fromDate: format(startOfMonth(now), 'yyyy-MM-dd'),
        toDate: format(now, 'yyyy-MM-dd'),
      }),
    },
    {
      label: 'Last Month',
      getRange: () => ({
        fromDate: format(startOfMonth(subMonths(now, 1)), 'yyyy-MM-dd'),
        toDate: format(endOfMonth(subMonths(now, 1)), 'yyyy-MM-dd'),
      }),
    },
    {
      label: 'Last 3 Months',
      getRange: () => ({
        fromDate: format(startOfMonth(subMonths(now, 2)), 'yyyy-MM-dd'),
        toDate: format(endOfMonth(now), 'yyyy-MM-dd'),
      }),
    },
    {
      label: 'YTD',
      getRange: () => {
        const fyStart = now.getMonth() >= 3
          ? new Date(now.getFullYear(), 3, 1)
          : new Date(now.getFullYear() - 1, 3, 1)
        return {
          fromDate: format(fyStart, 'yyyy-MM-dd'),
          toDate: format(now, 'yyyy-MM-dd'),
        }
      },
    },
  ]
}

interface DateRangeFilterProps {
  fromDate: string
  toDate: string
  onFromDateChange: (date: string) => void
  onToDateChange: (date: string) => void
  onClear?: () => void
  showQuickOptions?: boolean
  className?: string
  compact?: boolean
}

export const DateRangeFilter: FC<DateRangeFilterProps> = ({
  fromDate,
  toDate,
  onFromDateChange,
  onToDateChange,
  onClear,
  showQuickOptions = true,
  className = '',
  compact = false,
}) => {
  const quickOptions = useMemo(() => getQuickDateOptions(), [])

  const hasDateFilter = fromDate || toDate

  const handleQuickSelect = (option: QuickDateOption) => {
    const range = option.getRange()
    onFromDateChange(range.fromDate)
    onToDateChange(range.toDate)
  }

  const handleClear = () => {
    onFromDateChange('')
    onToDateChange('')
    onClear?.()
  }

  if (compact) {
    return (
      <div className={`flex items-center gap-2 ${className}`}>
        <Calendar className="h-4 w-4 text-gray-400" />
        <input
          type="date"
          value={fromDate}
          onChange={(e) => onFromDateChange(e.target.value)}
          className="px-2 py-1.5 text-sm border border-gray-300 rounded-md w-36"
          placeholder="From"
        />
        <span className="text-gray-400">-</span>
        <input
          type="date"
          value={toDate}
          onChange={(e) => onToDateChange(e.target.value)}
          min={fromDate || undefined}
          className="px-2 py-1.5 text-sm border border-gray-300 rounded-md w-36"
          placeholder="To"
        />
        {hasDateFilter && (
          <button
            onClick={handleClear}
            className="p-1 text-gray-400 hover:text-gray-600 rounded"
            title="Clear date filter"
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
          <Calendar className="h-4 w-4 text-gray-400" />
          <span className="text-sm font-medium text-gray-700">Date Range:</span>
        </div>
        <input
          type="date"
          value={fromDate}
          onChange={(e) => onFromDateChange(e.target.value)}
          className="px-3 py-1.5 text-sm border border-gray-300 rounded-md"
        />
        <span className="text-gray-400">to</span>
        <input
          type="date"
          value={toDate}
          onChange={(e) => onToDateChange(e.target.value)}
          min={fromDate || undefined}
          className="px-3 py-1.5 text-sm border border-gray-300 rounded-md"
        />
        {hasDateFilter && (
          <button
            onClick={handleClear}
            className="p-1 text-gray-400 hover:text-gray-600 rounded"
            title="Clear date filter"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </div>

      {showQuickOptions && (
        <div className="flex flex-wrap gap-1">
          {quickOptions.map((option) => (
            <button
              key={option.label}
              onClick={() => handleQuickSelect(option)}
              className="px-2 py-1 text-xs font-medium text-gray-600 bg-gray-100 rounded hover:bg-gray-200 transition-colors"
            >
              {option.label}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

export default DateRangeFilter

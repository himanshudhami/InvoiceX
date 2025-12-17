import { CURRENCIES } from '@/lib/constants'

interface CurrencySelectProps {
  value: string
  onChange: (value: string) => void
  className?: string
  disabled?: boolean
}

export const CurrencySelect = ({
  value,
  onChange,
  className = '',
  disabled = false
}: CurrencySelectProps) => {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className={`w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed ${className}`}
    >
      {CURRENCIES.map((currency) => (
        <option key={currency.value} value={currency.value}>
          {currency.label}
        </option>
      ))}
    </select>
  )
}

import { INVOICE_STATUSES, QUOTE_STATUSES, InvoiceStatus, QuoteStatus } from '@/lib/constants'

interface InvoiceStatusSelectProps {
  value: string
  onChange: (value: string) => void
  className?: string
  disabled?: boolean
}

interface QuoteStatusSelectProps {
  value: string
  onChange: (value: string) => void
  className?: string
  disabled?: boolean
}

export const InvoiceStatusSelect = ({
  value,
  onChange,
  className = '',
  disabled = false
}: InvoiceStatusSelectProps) => {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className={`w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed ${className}`}
    >
      {INVOICE_STATUSES.map((status) => (
        <option key={status.value} value={status.value}>
          {status.label}
        </option>
      ))}
    </select>
  )
}

export const QuoteStatusSelect = ({
  value,
  onChange,
  className = '',
  disabled = false
}: QuoteStatusSelectProps) => {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className={`w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed ${className}`}
    >
      {QUOTE_STATUSES.map((status) => (
        <option key={status.value} value={status.value}>
          {status.label}
        </option>
      ))}
    </select>
  )
}

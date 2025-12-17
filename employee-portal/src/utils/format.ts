import { format, parseISO, isValid } from 'date-fns'

export function formatDate(date: string | Date, formatStr = 'dd MMM yyyy'): string {
  const d = typeof date === 'string' ? parseISO(date) : date
  if (!isValid(d)) return '-'
  return format(d, formatStr)
}

export function formatDateTime(date: string | Date): string {
  return formatDate(date, 'dd MMM yyyy, hh:mm a')
}

export function formatCurrency(amount: number, currency = 'INR'): string {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(amount)
}

export function formatNumber(num: number): string {
  return new Intl.NumberFormat('en-IN').format(num)
}

export function formatDays(days: number): string {
  if (days === 1) return '1 day'
  if (days === 0.5) return '0.5 day'
  return `${days} days`
}

export function getStatusColor(status: string): string {
  const statusMap: Record<string, string> = {
    pending: 'badge-pending',
    approved: 'badge-approved',
    rejected: 'badge-rejected',
    cancelled: 'badge-cancelled',
    withdrawn: 'badge-cancelled',
    draft: 'badge-pending',
    submitted: 'badge-pending',
    verified: 'badge-approved',
  }
  return statusMap[status.toLowerCase()] || 'badge-pending'
}

export function getInitials(name: string): string {
  return name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

export function getFinancialYear(date: Date = new Date()): string {
  const year = date.getFullYear()
  const month = date.getMonth()
  // Financial year starts in April (month = 3)
  if (month >= 3) {
    return `${year}-${(year + 1).toString().slice(-2)}`
  }
  return `${year - 1}-${year.toString().slice(-2)}`
}

export function getMonthName(month: number): string {
  const months = [
    'January',
    'February',
    'March',
    'April',
    'May',
    'June',
    'July',
    'August',
    'September',
    'October',
    'November',
    'December',
  ]
  return months[month - 1] || ''
}

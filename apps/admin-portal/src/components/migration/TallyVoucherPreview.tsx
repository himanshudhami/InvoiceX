import { useState, useMemo } from 'react'
import {
  Receipt,
  ShoppingCart,
  CreditCard,
  Banknote,
  FileText,
  ArrowLeftRight,
  Package,
  ChevronDown,
  ChevronUp,
  Calendar,
  TrendingUp
} from 'lucide-react'
import { TallyParsedData, TallyVoucher } from '@/services/api/migration/tallyMigrationService'

interface TallyVoucherPreviewProps {
  parsedData: TallyParsedData
  onNext: () => void
  onBack: () => void
}

interface VoucherTypeCardProps {
  title: string
  icon: React.ComponentType<{ className?: string }>
  count: number
  amount: number
  vouchers: TallyVoucher[]
  expanded: boolean
  onToggle: () => void
  colorClass: string
}

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 0,
  }).format(Math.abs(amount))
}

const formatDate = (dateStr: string) => {
  try {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    })
  } catch {
    return dateStr
  }
}

const VoucherTypeCard = ({
  title,
  icon: Icon,
  count,
  amount,
  vouchers,
  expanded,
  onToggle,
  colorClass
}: VoucherTypeCardProps) => (
  <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
    <button
      onClick={onToggle}
      className="w-full flex items-center justify-between p-4 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
    >
      <div className="flex items-center gap-3">
        <div className={`p-2 rounded-lg ${colorClass}`}>
          <Icon className="h-5 w-5" />
        </div>
        <div className="text-left">
          <span className="font-medium text-gray-900 dark:text-white">{title}</span>
          <div className="flex items-center gap-3 mt-1">
            <span className="text-sm text-gray-500 dark:text-gray-400">
              {count} voucher{count !== 1 ? 's' : ''}
            </span>
            {amount > 0 && (
              <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                {formatCurrency(amount)}
              </span>
            )}
          </div>
        </div>
      </div>
      {count > 0 && (
        expanded ? (
          <ChevronUp className="h-5 w-5 text-gray-400" />
        ) : (
          <ChevronDown className="h-5 w-5 text-gray-400" />
        )
      )}
    </button>
    {expanded && vouchers.length > 0 && (
      <div className="border-t border-gray-200 dark:border-gray-700 max-h-64 overflow-y-auto">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 dark:bg-gray-800 sticky top-0">
            <tr>
              <th className="px-4 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Date</th>
              <th className="px-4 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Number</th>
              <th className="px-4 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Party</th>
              <th className="px-4 py-2 text-right font-medium text-gray-500 dark:text-gray-400">Amount</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {vouchers.slice(0, 50).map((voucher) => (
              <tr key={voucher.guid} className="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                <td className="px-4 py-2 text-gray-900 dark:text-white whitespace-nowrap">
                  {formatDate(voucher.date)}
                </td>
                <td className="px-4 py-2 text-gray-500 dark:text-gray-400">
                  {voucher.voucherNumber || '-'}
                </td>
                <td className="px-4 py-2 text-gray-700 dark:text-gray-300 truncate max-w-[200px]">
                  {voucher.partyLedgerName || '-'}
                </td>
                <td className="px-4 py-2 text-right font-medium text-gray-900 dark:text-white">
                  {formatCurrency(voucher.amount)}
                </td>
              </tr>
            ))}
            {vouchers.length > 50 && (
              <tr>
                <td colSpan={4} className="px-4 py-2 text-center text-gray-500 dark:text-gray-400">
                  ... and {vouchers.length - 50} more
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    )}
  </div>
)

const TallyVoucherPreview = ({ parsedData, onNext, onBack }: TallyVoucherPreviewProps) => {
  const [expandedTypes, setExpandedTypes] = useState<Set<string>>(new Set(['Sales']))

  const toggleType = (type: string) => {
    setExpandedTypes(prev => {
      const next = new Set(prev)
      if (next.has(type)) {
        next.delete(type)
      } else {
        next.add(type)
      }
      return next
    })
  }

  const { vouchers } = parsedData

  // Group vouchers by type
  const vouchersByType = useMemo(() => {
    const groups: Record<string, TallyVoucher[]> = {}
    vouchers.vouchers.forEach(v => {
      const type = v.voucherType || 'Other'
      if (!groups[type]) groups[type] = []
      groups[type].push(v)
    })
    return groups
  }, [vouchers])

  const voucherTypeConfigs: Record<string, { icon: React.ComponentType<{ className?: string }>; colorClass: string }> = {
    'Sales': { icon: Receipt, colorClass: 'bg-green-100 text-green-600 dark:bg-green-900/50 dark:text-green-400' },
    'Purchase': { icon: ShoppingCart, colorClass: 'bg-orange-100 text-orange-600 dark:bg-orange-900/50 dark:text-orange-400' },
    'Receipt': { icon: CreditCard, colorClass: 'bg-blue-100 text-blue-600 dark:bg-blue-900/50 dark:text-blue-400' },
    'Payment': { icon: Banknote, colorClass: 'bg-red-100 text-red-600 dark:bg-red-900/50 dark:text-red-400' },
    'Journal': { icon: FileText, colorClass: 'bg-purple-100 text-purple-600 dark:bg-purple-900/50 dark:text-purple-400' },
    'Contra': { icon: ArrowLeftRight, colorClass: 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400' },
    'Credit Note': { icon: Receipt, colorClass: 'bg-yellow-100 text-yellow-600 dark:bg-yellow-900/50 dark:text-yellow-400' },
    'Debit Note': { icon: Receipt, colorClass: 'bg-yellow-100 text-yellow-600 dark:bg-yellow-900/50 dark:text-yellow-400' },
    'Stock Journal': { icon: Package, colorClass: 'bg-cyan-100 text-cyan-600 dark:bg-cyan-900/50 dark:text-cyan-400' },
    'Delivery Note': { icon: Package, colorClass: 'bg-indigo-100 text-indigo-600 dark:bg-indigo-900/50 dark:text-indigo-400' },
    'Receipt Note': { icon: Package, colorClass: 'bg-indigo-100 text-indigo-600 dark:bg-indigo-900/50 dark:text-indigo-400' },
    'Physical Stock': { icon: Package, colorClass: 'bg-teal-100 text-teal-600 dark:bg-teal-900/50 dark:text-teal-400' },
  }

  const defaultConfig = { icon: FileText, colorClass: 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400' }

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
          Preview Vouchers
        </h2>
        <p className="text-gray-600 dark:text-gray-400 mt-1">
          Review the transactions that will be imported
        </p>
      </div>

      {/* Summary Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-gray-50 dark:bg-gray-900/50 rounded-lg p-4">
          <div className="flex items-center gap-2 text-gray-500 dark:text-gray-400 mb-1">
            <Receipt className="h-4 w-4" />
            <span className="text-sm">Total Vouchers</span>
          </div>
          <p className="text-2xl font-bold text-gray-900 dark:text-white">
            {vouchers.vouchers.length}
          </p>
        </div>
        <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-4">
          <div className="flex items-center gap-2 text-green-600 dark:text-green-400 mb-1">
            <TrendingUp className="h-4 w-4" />
            <span className="text-sm">Sales</span>
          </div>
          <p className="text-2xl font-bold text-green-700 dark:text-green-300">
            {formatCurrency(vouchers.totalSalesAmount)}
          </p>
        </div>
        <div className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-4">
          <div className="flex items-center gap-2 text-orange-600 dark:text-orange-400 mb-1">
            <ShoppingCart className="h-4 w-4" />
            <span className="text-sm">Purchases</span>
          </div>
          <p className="text-2xl font-bold text-orange-700 dark:text-orange-300">
            {formatCurrency(vouchers.totalPurchaseAmount)}
          </p>
        </div>
        <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4">
          <div className="flex items-center gap-2 text-blue-600 dark:text-blue-400 mb-1">
            <Calendar className="h-4 w-4" />
            <span className="text-sm">Date Range</span>
          </div>
          <p className="text-sm font-medium text-blue-700 dark:text-blue-300">
            {vouchers.minDate ? formatDate(vouchers.minDate) : '-'}
            <br />
            to {vouchers.maxDate ? formatDate(vouchers.maxDate) : '-'}
          </p>
        </div>
      </div>

      {/* Vouchers by Type */}
      <div className="space-y-3">
        <h3 className="font-medium text-gray-900 dark:text-white">Vouchers by Type</h3>

        {/* Primary transaction types */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-3">
          {['Sales', 'Purchase', 'Receipt', 'Payment'].map(type => {
            const typeVouchers = vouchersByType[type] || []
            const config = voucherTypeConfigs[type] || defaultConfig
            const amount = typeVouchers.reduce((sum, v) => sum + Math.abs(v.amount), 0)
            return (
              <VoucherTypeCard
                key={type}
                title={type}
                icon={config.icon}
                count={typeVouchers.length}
                amount={amount}
                vouchers={typeVouchers}
                expanded={expandedTypes.has(type)}
                onToggle={() => toggleType(type)}
                colorClass={config.colorClass}
              />
            )
          })}
        </div>

        {/* Secondary transaction types */}
        {Object.keys(vouchersByType)
          .filter(type => !['Sales', 'Purchase', 'Receipt', 'Payment'].includes(type))
          .length > 0 && (
          <div className="mt-4">
            <h4 className="text-sm font-medium text-gray-500 dark:text-gray-400 mb-3">
              Other Voucher Types
            </h4>
            <div className="space-y-2">
              {Object.entries(vouchersByType)
                .filter(([type]) => !['Sales', 'Purchase', 'Receipt', 'Payment'].includes(type))
                .sort((a, b) => b[1].length - a[1].length)
                .map(([type, typeVouchers]) => {
                  const config = voucherTypeConfigs[type] || defaultConfig
                  const amount = typeVouchers.reduce((sum, v) => sum + Math.abs(v.amount), 0)
                  return (
                    <VoucherTypeCard
                      key={type}
                      title={type}
                      icon={config.icon}
                      count={typeVouchers.length}
                      amount={amount}
                      vouchers={typeVouchers}
                      expanded={expandedTypes.has(type)}
                      onToggle={() => toggleType(type)}
                      colorClass={config.colorClass}
                    />
                  )
                })}
            </div>
          </div>
        )}
      </div>

      {/* Mapping Info */}
      <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4">
        <h4 className="font-medium text-blue-900 dark:text-blue-200 mb-2">
          How vouchers will be imported
        </h4>
        <ul className="text-sm text-blue-700 dark:text-blue-300 space-y-1">
          <li>• <strong>Sales</strong> → Invoices with inventory updates</li>
          <li>• <strong>Purchase</strong> → Vendor Invoices with inventory updates</li>
          <li>• <strong>Receipt</strong> → Customer Payments with bill allocation</li>
          <li>• <strong>Payment</strong> → Vendor Payments with bill allocation</li>
          <li>• <strong>Journal/Contra</strong> → Journal Entries</li>
          <li>• <strong>Credit/Debit Notes</strong> → Credit/Debit Memos</li>
          <li>• <strong>Stock Journals</strong> → Stock Transfers/Adjustments</li>
        </ul>
      </div>

      {/* Actions */}
      <div className="flex justify-between pt-4 border-t border-gray-200 dark:border-gray-700">
        <button
          onClick={onBack}
          className="px-4 py-2 text-gray-600 hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200"
        >
          Back
        </button>
        <button
          onClick={onNext}
          className="px-6 py-2 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700"
        >
          Start Import
        </button>
      </div>
    </div>
  )
}

export default TallyVoucherPreview

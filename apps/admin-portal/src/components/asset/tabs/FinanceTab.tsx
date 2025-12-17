import { FC } from 'react'
import { useAssetCost } from '@/features/assets/hooks'
import type { AssetCostSummary } from '@/services/api/types'
import { formatCurrency } from '@/lib/currency'
import { formatDate } from '@/lib/date'
import { Badge } from '@/components/ui/badge'
import {
  DollarSign,
  TrendingDown,
  Calendar,
  Clock,
  AlertCircle,
  Loader2,
  PieChart,
  Wrench,
  Archive,
} from 'lucide-react'

interface FinanceTabProps {
  assetId: string
}

const MetricCard: FC<{
  icon: React.ElementType
  label: string
  value: string | number
  subtext?: string
  variant?: 'default' | 'success' | 'warning' | 'danger'
}> = ({ icon: Icon, label, value, subtext, variant = 'default' }) => {
  const variantStyles = {
    default: 'bg-gray-50 border-gray-200',
    success: 'bg-green-50 border-green-200',
    warning: 'bg-yellow-50 border-yellow-200',
    danger: 'bg-red-50 border-red-200',
  }

  const iconStyles = {
    default: 'text-gray-500',
    success: 'text-green-600',
    warning: 'text-yellow-600',
    danger: 'text-red-600',
  }

  return (
    <div className={`rounded-lg border p-3 ${variantStyles[variant]}`}>
      <div className="flex items-center gap-2 mb-1">
        <Icon className={`w-4 h-4 ${iconStyles[variant]}`} />
        <span className="text-xs text-gray-500">{label}</span>
      </div>
      <div className="text-lg font-semibold text-gray-900">{value}</div>
      {subtext && <div className="text-xs text-gray-500 mt-0.5">{subtext}</div>}
    </div>
  )
}

const DepreciationChart: FC<{ costSummary: AssetCostSummary }> = ({ costSummary }) => {
  const depreciatedPercent = costSummary.depreciationBase > 0
    ? Math.min(100, (costSummary.accumulatedDepreciation / costSummary.depreciationBase) * 100)
    : 0

  return (
    <div className="p-3 bg-gray-50 rounded-lg">
      <h4 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-3">
        Depreciation Progress
      </h4>
      <div className="relative h-4 bg-gray-200 rounded-full overflow-hidden">
        <div
          className="absolute h-full bg-gradient-to-r from-blue-500 to-blue-600 rounded-full transition-all"
          style={{ width: `${depreciatedPercent}%` }}
        />
      </div>
      <div className="flex justify-between mt-2 text-xs">
        <span className="text-gray-600">
          Depreciated: {formatCurrency(costSummary.accumulatedDepreciation, costSummary.currency)}
          <span className="text-gray-400 ml-1">({depreciatedPercent.toFixed(1)}%)</span>
        </span>
        <span className="text-gray-600">
          Book Value: {formatCurrency(costSummary.netBookValue, costSummary.currency)}
        </span>
      </div>
    </div>
  )
}

export const FinanceTab: FC<FinanceTabProps> = ({ assetId }) => {
  const { data: costSummary, isLoading, isError } = useAssetCost(assetId)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-5 h-5 animate-spin text-gray-400" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-gray-500">
        <AlertCircle className="w-8 h-8 mb-2" />
        <p className="text-sm">Failed to load financial data</p>
      </div>
    )
  }

  if (!costSummary) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-gray-500">
        <PieChart className="w-10 h-10 mb-3 text-gray-300" />
        <p className="text-sm font-medium">No financial data available</p>
        <p className="text-xs mt-1">Add purchase cost to enable tracking</p>
      </div>
    )
  }

  const lifeProgress = costSummary.usefulLifeMonths
    ? Math.min(100, (costSummary.ageMonths / costSummary.usefulLifeMonths) * 100)
    : 0

  const isEndOfLife = costSummary.remainingLifeMonths <= 0

  return (
    <div className="p-4 space-y-4">
      {/* Purchase Type Badge */}
      <div className="flex items-center gap-2">
        <Badge variant="outline" className="text-sm">
          {costSummary.purchaseType || 'Purchase'}
        </Badge>
        {costSummary.depreciationMethod && (
          <Badge variant="outline" className="text-sm">
            {costSummary.depreciationMethod} Depreciation
          </Badge>
        )}
      </div>

      {/* Key Metrics Grid */}
      <div className="grid grid-cols-2 gap-2">
        <MetricCard
          icon={DollarSign}
          label="Purchase Cost"
          value={formatCurrency(costSummary.purchaseCost, costSummary.currency)}
        />
        <MetricCard
          icon={DollarSign}
          label="Net Book Value"
          value={formatCurrency(costSummary.netBookValue, costSummary.currency)}
          variant={costSummary.netBookValue > 0 ? 'success' : 'warning'}
        />
        <MetricCard
          icon={Wrench}
          label="Maintenance Cost"
          value={formatCurrency(costSummary.maintenanceCost, costSummary.currency)}
          variant={costSummary.maintenanceCost > costSummary.purchaseCost * 0.5 ? 'warning' : 'default'}
        />
        <MetricCard
          icon={TrendingDown}
          label="Monthly Depreciation"
          value={formatCurrency(costSummary.monthlyDepreciation, costSummary.currency)}
        />
      </div>

      {/* Depreciation Progress */}
      {costSummary.depreciationBase > 0 && (
        <DepreciationChart costSummary={costSummary} />
      )}

      {/* Asset Life */}
      {costSummary.usefulLifeMonths && (
        <div className="p-3 bg-gray-50 rounded-lg">
          <h4 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-3">
            Asset Life
          </h4>
          <div className="relative h-4 bg-gray-200 rounded-full overflow-hidden">
            <div
              className={`absolute h-full rounded-full transition-all ${
                isEndOfLife ? 'bg-red-500' : 'bg-green-500'
              }`}
              style={{ width: `${lifeProgress}%` }}
            />
          </div>
          <div className="flex justify-between mt-2 text-xs">
            <span className="flex items-center gap-1 text-gray-600">
              <Clock className="w-3 h-3" />
              Age: {costSummary.ageMonths} months
            </span>
            <span className={`flex items-center gap-1 ${isEndOfLife ? 'text-red-600' : 'text-gray-600'}`}>
              {isEndOfLife ? (
                <>
                  <AlertCircle className="w-3 h-3" />
                  Past useful life
                </>
              ) : (
                <>
                  Remaining: {costSummary.remainingLifeMonths} months
                </>
              )}
            </span>
          </div>
          {costSummary.depreciationStartDate && (
            <div className="text-xs text-gray-500 mt-2 flex items-center gap-1">
              <Calendar className="w-3 h-3" />
              Depreciation started: {formatDate(costSummary.depreciationStartDate)}
            </div>
          )}
        </div>
      )}

      {/* Disposal Information */}
      {(costSummary.disposalProceeds > 0 || costSummary.disposalCost > 0) && (
        <div className="p-3 bg-orange-50 rounded-lg border border-orange-200">
          <h4 className="text-xs font-medium text-orange-700 uppercase tracking-wider mb-2 flex items-center gap-1">
            <Archive className="w-3 h-3" />
            Disposal
          </h4>
          <div className="grid grid-cols-2 gap-3 text-sm">
            <div>
              <span className="text-xs text-gray-500">Proceeds</span>
              <div className="font-medium text-gray-900">
                {formatCurrency(costSummary.disposalProceeds, costSummary.currency)}
              </div>
            </div>
            <div>
              <span className="text-xs text-gray-500">Disposal Cost</span>
              <div className="font-medium text-gray-900">
                {formatCurrency(costSummary.disposalCost, costSummary.currency)}
              </div>
            </div>
          </div>
          <div className="mt-2 pt-2 border-t border-orange-200">
            <span className="text-xs text-gray-500">Gain/Loss</span>
            <div className={`font-medium ${costSummary.disposalGainLoss >= 0 ? 'text-green-600' : 'text-red-600'}`}>
              {costSummary.disposalGainLoss >= 0 ? '+' : ''}
              {formatCurrency(costSummary.disposalGainLoss, costSummary.currency)}
            </div>
          </div>
        </div>
      )}

      {/* Salvage Value */}
      {costSummary.salvageValue > 0 && (
        <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg text-sm">
          <span className="text-gray-600">Salvage Value</span>
          <span className="font-medium text-gray-900">
            {formatCurrency(costSummary.salvageValue, costSummary.currency)}
          </span>
        </div>
      )}
    </div>
  )
}

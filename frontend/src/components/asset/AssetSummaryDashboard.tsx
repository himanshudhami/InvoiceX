import { FC, useState } from 'react'
import { useAssetCostReport, useAssets } from '@/hooks/api/useAssets'
import { formatCurrency } from '@/lib/currency'
import {
  ChevronDown,
  ChevronUp,
  Package,
  CheckCircle2,
  UserCheck,
  Wrench,
  DollarSign,
  TrendingDown,
  AlertTriangle,
  Loader2,
} from 'lucide-react'

interface AssetSummaryDashboardProps {
  companyId?: string
}

interface MetricCardProps {
  icon: React.ElementType
  label: string
  value: string | number
  subtext?: string
  variant?: 'default' | 'success' | 'warning' | 'danger' | 'info'
  loading?: boolean
}

const MetricCard: FC<MetricCardProps> = ({
  icon: Icon,
  label,
  value,
  subtext,
  variant = 'default',
  loading,
}) => {
  const variantStyles = {
    default: {
      bg: 'bg-white',
      icon: 'bg-gray-100 text-gray-600',
      text: 'text-gray-900',
    },
    success: {
      bg: 'bg-white',
      icon: 'bg-green-100 text-green-600',
      text: 'text-green-700',
    },
    warning: {
      bg: 'bg-white',
      icon: 'bg-yellow-100 text-yellow-600',
      text: 'text-yellow-700',
    },
    danger: {
      bg: 'bg-white',
      icon: 'bg-red-100 text-red-600',
      text: 'text-red-700',
    },
    info: {
      bg: 'bg-white',
      icon: 'bg-blue-100 text-blue-600',
      text: 'text-blue-700',
    },
  }

  const styles = variantStyles[variant]

  return (
    <div className={`${styles.bg} rounded-lg border border-gray-200 p-4`}>
      <div className="flex items-start gap-3">
        <div className={`flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center ${styles.icon}`}>
          <Icon className="w-5 h-5" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-sm text-gray-500">{label}</p>
          {loading ? (
            <div className="h-7 flex items-center">
              <Loader2 className="w-4 h-4 animate-spin text-gray-400" />
            </div>
          ) : (
            <p className={`text-xl font-semibold ${styles.text}`}>{value}</p>
          )}
          {subtext && <p className="text-xs text-gray-400 mt-0.5">{subtext}</p>}
        </div>
      </div>
    </div>
  )
}

export const AssetSummaryDashboard: FC<AssetSummaryDashboardProps> = ({
  companyId,
}) => {
  const [isExpanded, setIsExpanded] = useState(false)

  // Fetch data
  const { data: costReport, isLoading: isLoadingReport } = useAssetCostReport(companyId)
  const { data: assetsData, isLoading: isLoadingAssets } = useAssets({ pageSize: 100 })

  // Calculate counts from assets data
  const assetCounts = assetsData?.items?.reduce(
    (acc, asset) => {
      acc.total++
      switch (asset.status.toLowerCase()) {
        case 'available':
          acc.available++
          break
        case 'assigned':
          acc.assigned++
          break
        case 'maintenance':
          acc.maintenance++
          break
        case 'retired':
        case 'disposed':
          acc.retired++
          break
      }
      return acc
    },
    { total: 0, available: 0, assigned: 0, maintenance: 0, retired: 0 }
  ) || { total: 0, available: 0, assigned: 0, maintenance: 0, retired: 0 }

  const isLoading = isLoadingReport || isLoadingAssets

  return (
    <div className="bg-gray-50 border-b border-gray-200">
      {/* Header - Always Visible */}
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full px-6 py-3 flex items-center justify-between hover:bg-gray-100 transition-colors"
      >
        <div className="flex items-center gap-4">
          <h2 className="text-sm font-medium text-gray-700">Asset Summary</h2>
          {/* Compact summary when collapsed */}
          {!isExpanded && !isLoading && (
            <div className="flex items-center gap-4 text-sm text-gray-500">
              <span className="flex items-center gap-1">
                <Package className="w-4 h-4" />
                {assetCounts.total} total
              </span>
              <span className="flex items-center gap-1 text-green-600">
                <CheckCircle2 className="w-4 h-4" />
                {assetCounts.available} available
              </span>
              <span className="flex items-center gap-1 text-blue-600">
                <UserCheck className="w-4 h-4" />
                {assetCounts.assigned} assigned
              </span>
              {assetCounts.maintenance > 0 && (
                <span className="flex items-center gap-1 text-yellow-600">
                  <Wrench className="w-4 h-4" />
                  {assetCounts.maintenance} maintenance
                </span>
              )}
            </div>
          )}
        </div>
        <div className="flex items-center gap-2 text-gray-400">
          <span className="text-xs">{isExpanded ? 'Collapse' : 'Expand'}</span>
          {isExpanded ? (
            <ChevronUp className="w-4 h-4" />
          ) : (
            <ChevronDown className="w-4 h-4" />
          )}
        </div>
      </button>

      {/* Expanded Content */}
      {isExpanded && (
        <div className="px-6 pb-4 space-y-4">
          {/* Asset Status Counts */}
          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-3">
            <MetricCard
              icon={Package}
              label="Total Assets"
              value={assetCounts.total}
              variant="default"
              loading={isLoadingAssets}
            />
            <MetricCard
              icon={CheckCircle2}
              label="Available"
              value={assetCounts.available}
              variant="success"
              loading={isLoadingAssets}
            />
            <MetricCard
              icon={UserCheck}
              label="Assigned"
              value={assetCounts.assigned}
              variant="info"
              loading={isLoadingAssets}
            />
            <MetricCard
              icon={Wrench}
              label="In Maintenance"
              value={assetCounts.maintenance}
              variant="warning"
              loading={isLoadingAssets}
            />
            <MetricCard
              icon={AlertTriangle}
              label="Retired/Disposed"
              value={assetCounts.retired}
              variant="danger"
              loading={isLoadingAssets}
            />
          </div>

          {/* Financial Summary */}
          {costReport && (
            <div>
              <h3 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
                Financial Summary
              </h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <MetricCard
                  icon={DollarSign}
                  label="Total Value"
                  value={formatCurrency(costReport.totalPurchaseCost)}
                  subtext="Purchase cost"
                  variant="default"
                  loading={isLoadingReport}
                />
                <MetricCard
                  icon={DollarSign}
                  label="Net Book Value"
                  value={formatCurrency(costReport.totalNetBookValue)}
                  subtext="After depreciation"
                  variant="info"
                  loading={isLoadingReport}
                />
                <MetricCard
                  icon={TrendingDown}
                  label="Depreciation"
                  value={formatCurrency(costReport.totalAccumulatedDepreciation)}
                  subtext="Accumulated"
                  variant="default"
                  loading={isLoadingReport}
                />
                <MetricCard
                  icon={Wrench}
                  label="Maintenance"
                  value={formatCurrency(costReport.totalMaintenanceCost)}
                  subtext="Total spent"
                  variant={costReport.totalMaintenanceCost > costReport.totalPurchaseCost * 0.2 ? 'warning' : 'default'}
                  loading={isLoadingReport}
                />
              </div>
            </div>
          )}

          {/* Quick Stats */}
          {costReport && costReport.averageAgeMonths > 0 && (
            <div className="flex items-center gap-6 text-sm text-gray-500 pt-2 border-t border-gray-200">
              <span>
                Average asset age: <strong className="text-gray-700">{Math.round(costReport.averageAgeMonths)} months</strong>
              </span>
              {costReport.totalDisposalProceeds > 0 && (
                <span>
                  Disposal proceeds: <strong className="text-gray-700">{formatCurrency(costReport.totalDisposalProceeds)}</strong>
                </span>
              )}
              {costReport.totalDisposalGainLoss !== 0 && (
                <span className={costReport.totalDisposalGainLoss >= 0 ? 'text-green-600' : 'text-red-600'}>
                  Disposal {costReport.totalDisposalGainLoss >= 0 ? 'gain' : 'loss'}: <strong>{formatCurrency(Math.abs(costReport.totalDisposalGainLoss))}</strong>
                </span>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

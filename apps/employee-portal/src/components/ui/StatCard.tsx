import * as React from 'react'
import { TrendingUp, TrendingDown } from 'lucide-react'
import { cn } from '@/utils/cn'

interface StatCardProps {
  title: string
  value: string | number
  subtitle?: string
  icon?: React.ReactNode
  trend?: {
    value: number
    label?: string
  }
  iconBgColor?: string
  className?: string
}

export function StatCard({
  title,
  value,
  subtitle,
  icon,
  trend,
  iconBgColor = 'bg-primary-100',
  className,
}: StatCardProps) {
  const isPositiveTrend = trend && trend.value > 0
  const isNegativeTrend = trend && trend.value < 0

  return (
    <div className={cn('stat-card', className)}>
      <div className="relative z-10 flex items-start justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium text-gray-500">{title}</p>
          <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
          {subtitle && (
            <p className="mt-1 text-xs text-gray-400">{subtitle}</p>
          )}
          {trend && (
            <div className="mt-2 flex items-center gap-1">
              {isPositiveTrend ? (
                <TrendingUp size={14} className="text-green-500" />
              ) : isNegativeTrend ? (
                <TrendingDown size={14} className="text-red-500" />
              ) : null}
              <span
                className={cn(
                  'text-xs font-medium',
                  isPositiveTrend && 'text-green-600',
                  isNegativeTrend && 'text-red-600',
                  !isPositiveTrend && !isNegativeTrend && 'text-gray-500'
                )}
              >
                {trend.value > 0 ? '+' : ''}
                {trend.value}%
              </span>
              {trend.label && (
                <span className="text-xs text-gray-400">{trend.label}</span>
              )}
            </div>
          )}
        </div>
        {icon && (
          <div
            className={cn(
              'flex items-center justify-center w-12 h-12 rounded-xl',
              iconBgColor
            )}
          >
            {icon}
          </div>
        )}
      </div>
    </div>
  )
}

// Quick stat variant for smaller displays
interface QuickStatProps {
  label: string
  value: string | number
  color?: 'primary' | 'success' | 'warning' | 'error'
  icon?: React.ReactNode
  className?: string
  onClick?: () => void
}

const colorClasses = {
  primary: 'bg-primary-50 text-primary-700',
  success: 'bg-green-50 text-green-700',
  warning: 'bg-yellow-50 text-yellow-700',
  error: 'bg-red-50 text-red-700',
}

export function QuickStat({
  label,
  value,
  color = 'primary',
  icon,
  className,
  onClick,
}: QuickStatProps) {
  return (
    <div
      className={cn(
        'flex items-center gap-3 p-3 rounded-xl',
        colorClasses[color],
        onClick ? 'cursor-pointer transition hover:opacity-90 active:opacity-80' : '',
        className
      )}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onClick={onClick}
      onKeyDown={(e) => {
        if (onClick && (e.key === 'Enter' || e.key === ' ')) {
          e.preventDefault()
          onClick()
        }
      }}
    >
      {icon && <div className="flex-shrink-0">{icon}</div>}
      <div className="min-w-0">
        <p className="text-xs font-medium opacity-70 truncate">{label}</p>
        <p className="text-lg font-bold truncate">{value}</p>
      </div>
    </div>
  )
}

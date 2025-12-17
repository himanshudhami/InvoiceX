import React from 'react'
import { useQuery } from '@tanstack/react-query'
import { Laptop, Monitor, Smartphone, Keyboard, HardDrive, Calendar, AlertCircle } from 'lucide-react'
import { portalApi } from '@/api'
import { PageHeader, EmptyState } from '@/components/layout'
import { Card, Badge, PageLoader } from '@/components/ui'
import { formatDate } from '@/utils/format'
import type { MyAsset } from '@/types'

const categoryIcons: Record<string, React.ReactNode> = {
  laptop: <Laptop size={20} />,
  desktop: <Monitor size={20} />,
  monitor: <Monitor size={20} />,
  phone: <Smartphone size={20} />,
  mobile: <Smartphone size={20} />,
  keyboard: <Keyboard size={20} />,
  storage: <HardDrive size={20} />,
}

function getCategoryIcon(category: string | undefined | null): React.ReactNode {
  if (!category) return <Laptop size={20} />
  const key = category.toLowerCase()
  return categoryIcons[key] || <Laptop size={20} />
}

function getConditionVariant(condition: string | undefined | null): 'success' | 'warning' | 'error' | 'default' {
  if (!condition) return 'default'
  const c = condition.toLowerCase()
  if (c === 'excellent' || c === 'good') return 'success'
  if (c === 'fair') return 'warning'
  if (c === 'poor' || c === 'damaged') return 'error'
  return 'default'
}

export function AssetsPage() {
  const { data: assets, isLoading } = useQuery<MyAsset[]>({
    queryKey: ['my-assets'],
    queryFn: portalApi.getMyAssets,
  })

  if (isLoading) {
    return <PageLoader />
  }

  return (
    <div className="animate-fade-in">
      <PageHeader title="My Assets" />

      <div className="px-4 py-4">
        {!assets || assets.length === 0 ? (
          <EmptyState
            icon={<Laptop className="text-gray-400" size={24} />}
            title="No assets assigned"
            description="Assets assigned to you will appear here"
          />
        ) : (
          <div className="space-y-3">
            {assets.map((asset, index) => (
              <AssetCard key={asset.id || `asset-${index}`} asset={asset} />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

function AssetCard({ asset }: { asset: MyAsset }) {
  const isReturnDueSoon =
    asset.expectedReturnDate &&
    new Date(asset.expectedReturnDate) <= new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)

  return (
    <Card className="p-4">
      <div className="flex items-start gap-3">
        <div className="flex items-center justify-center w-12 h-12 rounded-xl bg-primary-50 text-primary-600">
          {getCategoryIcon(asset.category)}
        </div>

        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between">
            <div>
              <p className="text-sm font-semibold text-gray-900">{asset.assetName}</p>
              <p className="text-xs text-gray-500">{asset.assetCode}</p>
            </div>
            <Badge
              variant={getConditionVariant(asset.condition)}
              className="text-[10px]"
            >
              {asset.condition}
            </Badge>
          </div>

          <div className="mt-2 space-y-1">
            <div className="flex items-center gap-2 text-xs text-gray-500">
              <span className="font-medium">Category:</span>
              <span>{asset.category}</span>
            </div>
            {asset.serialNumber && (
              <div className="flex items-center gap-2 text-xs text-gray-500">
                <span className="font-medium">Serial:</span>
                <span className="font-mono">{asset.serialNumber}</span>
              </div>
            )}
            <div className="flex items-center gap-2 text-xs text-gray-500">
              <Calendar size={12} />
              <span>Assigned: {formatDate(asset.assignedDate)}</span>
            </div>
          </div>

          {isReturnDueSoon && asset.expectedReturnDate && (
            <div className="flex items-center gap-2 mt-2 p-2 rounded-lg bg-yellow-50 text-yellow-700">
              <AlertCircle size={14} />
              <span className="text-xs font-medium">
                Return due: {formatDate(asset.expectedReturnDate)}
              </span>
            </div>
          )}
        </div>
      </div>
    </Card>
  )
}

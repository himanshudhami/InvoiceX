import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import * as Tabs from '@radix-ui/react-tabs'
import {
  Laptop,
  Monitor,
  Smartphone,
  Keyboard,
  HardDrive,
  Calendar,
  AlertCircle,
  Plus,
  Clock,
  CheckCircle,
  ChevronRight,
  FileText,
} from 'lucide-react'
import { portalApi, assetRequestApi } from '@/api'
import { PageHeader, EmptyState } from '@/components/layout'
import { Card, Badge, Button, PageLoader } from '@/components/ui'
import { formatDate } from '@/utils/format'
import { cn } from '@/utils/cn'
import type { MyAsset, AssetRequestSummary, AssetRequestStatus } from '@/types'

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

function getRequestStatusVariant(status: AssetRequestStatus): 'success' | 'warning' | 'error' | 'default' | 'info' {
  switch (status) {
    case 'pending':
    case 'in_progress':
      return 'warning'
    case 'approved':
      return 'info'
    case 'fulfilled':
      return 'success'
    case 'rejected':
    case 'cancelled':
      return 'error'
    default:
      return 'default'
  }
}

function getPriorityVariant(priority: string): 'success' | 'warning' | 'error' | 'default' {
  switch (priority) {
    case 'urgent':
      return 'error'
    case 'high':
      return 'warning'
    case 'normal':
      return 'default'
    case 'low':
      return 'success'
    default:
      return 'default'
  }
}

export function AssetsPage() {
  const [activeTab, setActiveTab] = useState('assets')

  const { data: assets, isLoading: assetsLoading } = useQuery<MyAsset[]>({
    queryKey: ['my-assets'],
    queryFn: portalApi.getMyAssets,
  })

  const { data: requests, isLoading: requestsLoading } = useQuery<AssetRequestSummary[]>({
    queryKey: ['my-asset-requests'],
    queryFn: () => assetRequestApi.getMyRequests(),
  })

  const pendingRequests = requests?.filter((r) => r.status === 'pending' || r.status === 'in_progress') || []

  if (assetsLoading) {
    return <PageLoader />
  }

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Assets"
        rightContent={
          <Link to="/assets/apply">
            <Button size="sm">
              <Plus size={18} className="mr-1" />
              Request
            </Button>
          </Link>
        }
      />

      {/* Quick Stats */}
      <div className="px-4 py-4 grid grid-cols-2 gap-3">
        <Card className="p-3 flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-blue-50">
            <Laptop className="text-blue-600" size={20} />
          </div>
          <div>
            <p className="text-lg font-semibold text-gray-900">{assets?.length || 0}</p>
            <p className="text-xs text-gray-500">Assigned</p>
          </div>
        </Card>
        <Card className="p-3 flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-yellow-50">
            <Clock className="text-yellow-600" size={20} />
          </div>
          <div>
            <p className="text-lg font-semibold text-gray-900">{pendingRequests.length}</p>
            <p className="text-xs text-gray-500">Pending</p>
          </div>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs.Root value={activeTab} onValueChange={setActiveTab}>
        <Tabs.List className="flex border-b border-gray-200 px-4">
          <TabTrigger value="assets" label="My Assets" />
          <TabTrigger value="requests" label="My Requests" />
        </Tabs.List>

        <div className="px-4 py-4">
          <Tabs.Content value="assets">
            <MyAssetsTab assets={assets || []} />
          </Tabs.Content>

          <Tabs.Content value="requests">
            <MyRequestsTab requests={requests || []} isLoading={requestsLoading} />
          </Tabs.Content>
        </div>
      </Tabs.Root>
    </div>
  )
}

function TabTrigger({ value, label }: { value: string; label: string }) {
  return (
    <Tabs.Trigger
      value={value}
      className={cn(
        'flex-1 py-3 text-sm font-medium border-b-2 -mb-px transition-colors',
        'data-[state=active]:border-primary-600 data-[state=active]:text-primary-600',
        'data-[state=inactive]:border-transparent data-[state=inactive]:text-gray-500'
      )}
    >
      {label}
    </Tabs.Trigger>
  )
}

function MyAssetsTab({ assets }: { assets: MyAsset[] }) {
  if (assets.length === 0) {
    return (
      <EmptyState
        icon={<Laptop className="text-gray-400" size={24} />}
        title="No assets assigned"
        description="Assets assigned to you will appear here"
      />
    )
  }

  return (
    <div className="space-y-3">
      {assets.map((asset, index) => (
        <AssetCard key={asset.id || `asset-${index}`} asset={asset} />
      ))}
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
            <Badge variant={getConditionVariant(asset.condition)} className="text-[10px]">
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
              <span className="text-xs font-medium">Return due: {formatDate(asset.expectedReturnDate)}</span>
            </div>
          )}
        </div>
      </div>
    </Card>
  )
}

function MyRequestsTab({ requests, isLoading }: { requests: AssetRequestSummary[]; isLoading: boolean }) {
  if (isLoading) {
    return <PageLoader />
  }

  if (requests.length === 0) {
    return (
      <EmptyState
        icon={<FileText className="text-gray-400" size={24} />}
        title="No asset requests"
        description="Your asset requests will appear here"
        action={
          <Link to="/assets/apply">
            <Button size="sm">Request an Asset</Button>
          </Link>
        }
      />
    )
  }

  return (
    <div className="space-y-3">
      {requests.map((request) => (
        <Link key={request.id} to={`/assets/requests/${request.id}`}>
          <Card className="p-4 touch-feedback">
            <div className="flex items-start justify-between mb-2">
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-gray-900">{request.title}</p>
                <p className="text-xs text-gray-500 mt-0.5">
                  {request.category} - {request.assetType}
                </p>
              </div>
              <div className="flex flex-col items-end gap-1">
                <Badge variant={getRequestStatusVariant(request.status)}>{request.status.replace('_', ' ')}</Badge>
                {request.priority !== 'normal' && (
                  <Badge variant={getPriorityVariant(request.priority)} className="text-[10px]">
                    {request.priority}
                  </Badge>
                )}
              </div>
            </div>

            <div className="flex items-center justify-between text-xs text-gray-500">
              <span>Qty: {request.quantity}</span>
              <div className="flex items-center gap-1">
                <span>Requested {formatDate(request.requestedAt, 'dd MMM')}</span>
                <ChevronRight size={14} className="text-gray-400" />
              </div>
            </div>

            {request.status === 'fulfilled' && request.fulfilledAt && (
              <div className="flex items-center gap-2 mt-2 p-2 rounded-lg bg-green-50 text-green-700">
                <CheckCircle size={14} />
                <span className="text-xs font-medium">Fulfilled on {formatDate(request.fulfilledAt)}</span>
              </div>
            )}
          </Card>
        </Link>
      ))}
    </div>
  )
}

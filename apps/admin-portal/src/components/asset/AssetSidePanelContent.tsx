"use client"

import { FC } from 'react'
import { useAsset } from '@/hooks/api/useAssets'
import { format } from 'date-fns'
import { AlertCircle, Loader2, Monitor, DollarSign, MapPin, User, Calendar, Edit, Trash2, Share2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'

interface AssetSidePanelContentProps {
  assetId: string
  onClose: () => void
  onEdit?: (assetId: string) => void
  onAssign?: (assetId: string) => void
  onDelete?: (assetId: string) => void
}

export const AssetSidePanelContent: FC<AssetSidePanelContentProps> = ({
  assetId,
  onClose,
  onEdit,
  onAssign,
  onDelete,
}) => {
  const { data: asset, isLoading, isError } = useAsset(assetId, !!assetId)

  const statusBadge = (status?: string) => {
    const colors: Record<string, string> = {
      available: 'bg-green-100 text-green-800',
      assigned: 'bg-blue-100 text-blue-800',
      retired: 'bg-gray-100 text-gray-800',
      maintenance: 'bg-yellow-100 text-yellow-800',
    }
    const cls = status ? colors[status.toLowerCase()] || colors.available : colors.available
    return <Badge className={cls}>{status || 'Available'}</Badge>
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
      </div>
    )
  }

  if (isError || !asset) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-gray-500">
        <AlertCircle className="w-10 h-10 mb-3" />
        <p>Unable to load asset details</p>
        <Button variant="outline" size="sm" className="mt-3" onClick={onClose}>
          Close
        </Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
        <div className="flex items-start gap-3">
          <div className="flex-shrink-0 w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
            <Monitor className="w-6 h-6 text-blue-600" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-semibold text-gray-900 truncate">{asset.name}</h2>
              {statusBadge(asset.status)}
            </div>
            <div className="text-sm text-gray-500">
              {asset.category} • {asset.assetTag || 'No tag'}
            </div>
            {asset.serialNumber && (
              <div className="text-xs text-gray-400 mt-0.5">Serial: {asset.serialNumber}</div>
            )}
          </div>
          <Button variant="ghost" size="icon" onClick={onClose} className="text-gray-400 hover:text-gray-500">
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </Button>
        </div>

        <div className="flex gap-2 mt-3">
          {onEdit && (
            <Button variant="outline" size="sm" onClick={() => onEdit(asset.id)}>
              <Edit className="w-3 h-3 mr-1" />
              Edit
            </Button>
          )}
          {onAssign && (
            <Button variant="outline" size="sm" onClick={() => onAssign(asset.id)}>
              <Share2 className="w-3 h-3 mr-1" />
              Assign
            </Button>
          )}
          {onDelete && (
            <Button variant="outline" size="sm" className="text-red-600 hover:text-red-700" onClick={() => onDelete(asset.id)}>
              <Trash2 className="w-3 h-3 mr-1" />
              Delete
            </Button>
          )}
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div className="space-y-1">
            <div className="text-gray-500 text-xs uppercase">Location</div>
            <div className="flex items-center gap-1 text-gray-800">
              <MapPin className="w-4 h-4 text-gray-400" />
              <span>{asset.location || 'Not set'}</span>
            </div>
          </div>
          <div className="space-y-1">
            <div className="text-gray-500 text-xs uppercase">Assigned To</div>
            <div className="flex items-center gap-1 text-gray-800">
              <User className="w-4 h-4 text-gray-400" />
              <span>{asset.assignedToName || 'Unassigned'}</span>
            </div>
          </div>
          <div className="space-y-1">
            <div className="text-gray-500 text-xs uppercase">Purchase Date</div>
            <div className="flex items-center gap-1 text-gray-800">
              <Calendar className="w-4 h-4 text-gray-400" />
              <span>{asset.purchaseDate ? format(new Date(asset.purchaseDate), 'MMM dd, yyyy') : '—'}</span>
            </div>
          </div>
          <div className="space-y-1">
            <div className="text-gray-500 text-xs uppercase">Value</div>
            <div className="flex items-center gap-1 text-gray-800">
              <DollarSign className="w-4 h-4 text-gray-400" />
              <span>{asset.purchasePrice ?? '—'}</span>
            </div>
          </div>
        </div>

        <div className="space-y-1 text-sm">
          <div className="text-gray-500 text-xs uppercase">Notes</div>
          <div className="text-gray-800">{asset.notes || 'No notes added'}</div>
        </div>
      </div>
    </div>
  )
}

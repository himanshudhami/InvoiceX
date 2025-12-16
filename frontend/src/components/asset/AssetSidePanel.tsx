import { FC, useState } from 'react'
import { SidePanel } from '@/components/ui/SidePanel'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useAssetDetails } from '@/features/assets/hooks'
import {
  OverviewTab,
  AssignmentsTab,
  MaintenanceTab,
  DocumentsTab,
  FinanceTab,
} from './tabs'
import {
  Laptop,
  Edit,
  Link2,
  Loader2,
  AlertCircle,
  MoreHorizontal,
  Trash2,
  Archive,
} from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

interface AssetSidePanelProps {
  assetId: string | null
  onClose: () => void
  onEdit?: (assetId: string) => void
  onAssign?: (assetId: string) => void
  onDispose?: (assetId: string) => void
  onDelete?: (assetId: string) => void
}

const getStatusBadgeVariant = (status: string) => {
  switch (status) {
    case 'available':
      return 'default'
    case 'assigned':
      return 'secondary'
    case 'maintenance':
      return 'outline'
    case 'retired':
    case 'disposed':
      return 'destructive'
    case 'reserved':
      return 'outline'
    default:
      return 'outline'
  }
}

const getAssetTypeIcon = (_type: string) => {
  // Could expand this to return different icons based on type
  return Laptop
}

export const AssetSidePanel: FC<AssetSidePanelProps> = ({
  assetId,
  onClose,
  onEdit,
  onAssign,
  onDispose,
  onDelete,
}) => {
  const [activeTab, setActiveTab] = useState('overview')
  const { asset, currentAssignment, isLoading, isError } = useAssetDetails(assetId || '', !!assetId)

  const handleEdit = () => {
    if (assetId && onEdit) {
      onEdit(assetId)
    }
  }

  const handleAssign = () => {
    if (assetId && onAssign) {
      onAssign(assetId)
    }
  }

  const handleDispose = () => {
    if (assetId && onDispose) {
      onDispose(assetId)
    }
  }

  const handleDelete = () => {
    if (assetId && onDelete) {
      onDelete(assetId)
    }
  }

  const AssetIcon = asset ? getAssetTypeIcon(asset.assetType) : Laptop

  const renderHeader = () => {
    if (isLoading) {
      return (
        <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 bg-gray-200 rounded-lg animate-pulse" />
            <div className="flex-1 space-y-2">
              <div className="h-5 bg-gray-200 rounded w-32 animate-pulse" />
              <div className="h-4 bg-gray-200 rounded w-24 animate-pulse" />
            </div>
          </div>
        </div>
      )
    }

    if (isError || !asset) {
      return (
        <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
          <div className="flex items-center gap-3 text-red-600">
            <AlertCircle className="w-5 h-5" />
            <span>Failed to load asset</span>
          </div>
        </div>
      )
    }

    return (
      <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
        <div className="flex items-start gap-3">
          <div className="flex-shrink-0 w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
            <AssetIcon className="w-6 h-6 text-blue-600" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-semibold text-gray-900 truncate">{asset.name}</h2>
              <Badge variant={getStatusBadgeVariant(asset.status)}>
                {asset.status}
              </Badge>
            </div>
            <div className="text-sm text-gray-500">
              {asset.assetTag} {asset.assetType && `• ${asset.assetType.replace(/_/g, ' ')}`}
            </div>
            {currentAssignment && (
              <div className="text-xs text-blue-600 mt-0.5">
                Assigned to: {currentAssignment.employeeId ? 'Employee' : 'Company'}
              </div>
            )}
          </div>
          <Button variant="ghost" size="icon" onClick={onClose} className="text-gray-400 hover:text-gray-500">
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </Button>
        </div>

        {/* Quick Actions */}
        <div className="flex gap-2 mt-3">
          <Button variant="outline" size="sm" onClick={handleEdit}>
            <Edit className="w-3 h-3 mr-1" />
            Edit
          </Button>
          {asset.status === 'available' && (
            <Button variant="outline" size="sm" onClick={handleAssign} className="text-blue-600 hover:text-blue-700">
              <Link2 className="w-3 h-3 mr-1" />
              Assign
            </Button>
          )}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm">
                <MoreHorizontal className="w-3 h-3" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {asset.status !== 'disposed' && asset.status !== 'retired' && (
                <DropdownMenuItem onClick={handleDispose} className="text-orange-600">
                  <Archive className="w-4 h-4 mr-2" />
                  Dispose Asset
                </DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={handleDelete} className="text-red-600">
                <Trash2 className="w-4 h-4 mr-2" />
                Delete Asset
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    )
  }

  return (
    <SidePanel
      isOpen={!!assetId}
      onClose={onClose}
      width="xl"
      header={renderHeader()}
    >
      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
        </div>
      ) : isError || !asset ? (
        <div className="flex flex-col items-center justify-center py-12 text-gray-500">
          <AlertCircle className="w-10 h-10 mb-3" />
          <p>Unable to load asset details</p>
          <Button variant="outline" size="sm" className="mt-3" onClick={onClose}>
            Close
          </Button>
        </div>
      ) : (
        <Tabs value={activeTab} onValueChange={setActiveTab} className="h-full flex flex-col">
          <TabsList className="px-4 pt-2 pb-0 bg-white border-b rounded-none justify-start gap-1">
            <TabsTrigger value="overview" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Overview
            </TabsTrigger>
            <TabsTrigger value="assignments" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Assign
            </TabsTrigger>
            <TabsTrigger value="maintenance" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Maint.
            </TabsTrigger>
            <TabsTrigger value="documents" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Docs
            </TabsTrigger>
            <TabsTrigger value="finance" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Finance
            </TabsTrigger>
          </TabsList>

          <div className="flex-1 overflow-y-auto">
            <TabsContent value="overview" className="mt-0 h-full">
              <OverviewTab asset={asset} currentAssignment={currentAssignment} />
            </TabsContent>
            <TabsContent value="assignments" className="mt-0 h-full">
              <AssignmentsTab assetId={asset.id} currentAssignment={currentAssignment} onAssign={handleAssign} />
            </TabsContent>
            <TabsContent value="maintenance" className="mt-0 h-full">
              <MaintenanceTab assetId={asset.id} />
            </TabsContent>
            <TabsContent value="documents" className="mt-0 h-full">
              <DocumentsTab assetId={asset.id} />
            </TabsContent>
            <TabsContent value="finance" className="mt-0 h-full">
              <FinanceTab assetId={asset.id} />
            </TabsContent>
          </div>
        </Tabs>
      )}
    </SidePanel>
  )
}

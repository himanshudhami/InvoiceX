import { useState, useMemo } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryStates, parseAsString } from 'nuqs'
import { Asset, PagedResponse } from '@/services/api/types'
import { formatCurrency } from '@/lib/currency'
import { formatDate } from '@/lib/date'
import {
  useAssets,
  useDeleteAsset,
} from '@/hooks/api/useAssets'
import { useCompanies } from '@/hooks/api/useCompanies'
import { DataTable } from '@/components/ui/DataTable'
import { Drawer } from '@/components/ui/Drawer'
import { AssetSidePanelContent } from '@/components/asset/AssetSidePanelContent'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { AssetForm } from '@/components/forms/AssetForm'
import { AssetBulkUploadModal } from '@/components/forms/AssetBulkUploadModal'
import { AssetSummaryDashboard } from '@/components/asset/AssetSummaryDashboard'
import { AssetQuickFilters, AssetFilters } from '@/components/asset/AssetQuickFilters'
import { AssignAssetModal } from '@/components/modals/AssignAssetModal'
import {
  Plus,
  Upload,
  Edit,
  Trash2,
  AlertTriangle,
  Eye,
} from 'lucide-react'

const getStatusBadgeColor = (status: string) => {
  switch (status?.toLowerCase()) {
    case 'available':
      return 'bg-green-100 text-green-800'
    case 'assigned':
      return 'bg-blue-100 text-blue-800'
    case 'maintenance':
      return 'bg-yellow-100 text-yellow-800'
    case 'retired':
    case 'disposed':
      return 'bg-gray-100 text-gray-800'
    case 'reserved':
      return 'bg-purple-100 text-purple-800'
    default:
      return 'bg-gray-100 text-gray-800'
  }
}

const AssetsManagement = () => {
  // URL State
  const [urlState, setUrlState] = useQueryStates(
    {
      asset: parseAsString.withDefault(''),
      search: parseAsString.withDefault(''),
      status: parseAsString.withDefault(''),
      type: parseAsString.withDefault(''),
      company: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  // Data fetching - max 100 assets per page (backend limit)
  const { data: assetsData, refetch } = useAssets({ pageSize: 100 })
  const deleteAsset = useDeleteAsset()
  const { data: companies = [] } = useCompanies()

  // Local state
  const [isDrawerOpen, setIsDrawerOpen] = useState(false)
  const [isBulkUploadOpen, setIsBulkUploadOpen] = useState(false)
  const [editingAsset, setEditingAsset] = useState<Asset | null>(null)
  const [deleteConfirm, setDeleteConfirm] = useState<Asset | null>(null)
  const [assigningAssetId, setAssigningAssetId] = useState<string | null>(null)

  // Filter state from URL
  const filters: AssetFilters = {
    search: urlState.search,
    status: urlState.status,
    assetType: urlState.type,
    company: urlState.company,
  }

  const handleFilterChange = (newFilters: Partial<AssetFilters>) => {
    setUrlState({
      search: newFilters.search ?? urlState.search,
      status: newFilters.status ?? urlState.status,
      type: newFilters.assetType ?? urlState.type,
      company: newFilters.company ?? urlState.company,
    })
  }

  // Filter assets
  const assets = (assetsData as PagedResponse<Asset> | undefined)?.items || []
  const filteredAssets = useMemo(() => {
    let result = assets

    if (urlState.search) {
      const term = urlState.search.toLowerCase()
      result = result.filter(
        (a) =>
          a.name.toLowerCase().includes(term) ||
          a.assetTag.toLowerCase().includes(term) ||
          a.serialNumber?.toLowerCase().includes(term)
      )
    }

    if (urlState.status) {
      result = result.filter(
        (a) => a.status.toLowerCase() === urlState.status.toLowerCase()
      )
    }

    if (urlState.type) {
      result = result.filter((a) => a.assetType === urlState.type)
    }

    if (urlState.company) {
      result = result.filter((a) => a.companyId === urlState.company)
    }

    return result
  }, [assets, urlState])

  // Handlers
  const handleView = (assetId: string) => {
    setUrlState({ asset: assetId })
  }

  const handleClosePanel = () => {
    setUrlState({ asset: '' })
  }

  const openEditDrawer = (asset: Asset) => {
    setEditingAsset(asset)
    setIsDrawerOpen(true)
  }

  const handleEdit = (assetId: string) => {
    const asset = assets.find((a) => a.id === assetId)
    if (asset) {
      handleClosePanel()
      openEditDrawer(asset)
    }
  }

  const handleDelete = async () => {
    if (!deleteConfirm) return
    try {
      await deleteAsset.mutateAsync(deleteConfirm.id)
      setDeleteConfirm(null)
      if (urlState.asset === deleteConfirm.id) {
        setUrlState({ asset: '' })
      }
    } catch (err) {
      console.error('Failed to delete asset:', err)
    }
  }

  const handleCreateSuccess = () => {
    setIsDrawerOpen(false)
    setEditingAsset(null)
    refetch()
  }

  const handleAssignFromPanel = (assetId: string) => {
    handleClosePanel()
    setAssigningAssetId(assetId)
  }

  const handleDeleteFromPanel = (assetId: string) => {
    const asset = assets.find((a) => a.id === assetId)
    if (asset) setDeleteConfirm(asset)
  }

  // Get company name helper
  const getCompanyName = (companyId?: string) => {
    if (!companyId) return ''
    const company = companies.find((c) => c.id === companyId)
    return company?.name || ''
  }

  // Table columns
  const columns: ColumnDef<Asset>[] = useMemo(
    () => [
      {
        header: 'Asset',
        accessorKey: 'name',
        cell: ({ row }) => (
          <div>
            <div className="font-medium text-gray-900">{row.original.name}</div>
            <div className="text-sm text-gray-500">{row.original.assetTag}</div>
          </div>
        ),
      },
      {
        header: 'Type',
        accessorKey: 'assetType',
        cell: ({ row }) => (
          <Badge variant="outline">
            {row.original.assetType?.replace(/_/g, ' ')}
          </Badge>
        ),
      },
      {
        header: 'Status',
        accessorKey: 'status',
        cell: ({ row }) => (
          <span
            className={`px-2 py-1 text-xs font-medium rounded-full ${getStatusBadgeColor(
              row.original.status
            )}`}
          >
            {row.original.status}
          </span>
        ),
      },
      {
        header: 'Company',
        accessorKey: 'companyId',
        cell: ({ row }) => (
          <span className="text-sm text-gray-600">
            {getCompanyName(row.original.companyId)}
          </span>
        ),
      },
      {
        header: 'Value',
        accessorKey: 'purchaseCost',
        cell: ({ row }) =>
          row.original.purchaseCost
            ? formatCurrency(row.original.purchaseCost, row.original.currency)
            : '—',
      },
      {
        header: 'Purchased',
        accessorKey: 'purchaseDate',
        cell: ({ row }) =>
          row.original.purchaseDate ? formatDate(row.original.purchaseDate) : '—',
      },
      {
        header: '',
        id: 'actions',
        cell: ({ row }) => (
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => handleView(row.original.id)}
              title="View details"
            >
              <Eye className="w-4 h-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              onClick={() => handleEdit(row.original.id)}
              title="Edit"
            >
              <Edit className="w-4 h-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="text-red-600 hover:text-red-700 hover:bg-red-50"
              onClick={() => setDeleteConfirm(row.original)}
              title="Delete"
            >
              <Trash2 className="w-4 h-4" />
            </Button>
          </div>
        ),
      },
    ],
    [companies]
  )

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b bg-white">
        <h1 className="text-2xl font-semibold text-gray-900">Assets Management</h1>
        <div className="flex items-center gap-3">
          <Button variant="outline" onClick={() => setIsBulkUploadOpen(true)}>
            <Upload className="w-4 h-4 mr-2" />
            Bulk Upload
          </Button>
          <Button onClick={() => setIsDrawerOpen(true)}>
            <Plus className="w-4 h-4 mr-2" />
            Add Asset
          </Button>
        </div>
      </div>

      {/* Summary Dashboard */}
      <AssetSummaryDashboard companyId={urlState.company || undefined} />

      {/* Quick Filters */}
      <AssetQuickFilters filters={filters} onFilterChange={handleFilterChange} />

      {/* Main Content */}
      <div className="flex-1 overflow-hidden flex">
        {/* Asset Table */}
        <div className={`flex-1 overflow-auto ${urlState.asset ? 'hidden lg:block' : ''}`}>
          <DataTable
            columns={columns}
            data={filteredAssets}
            searchPlaceholder="Search assets..."
          />
        </div>

        {/* Side Panel */}
        <Drawer
          isOpen={!!urlState.asset}
          onClose={handleClosePanel}
          title={null as any}
          size="xl"
          showCloseButton={false}
        >
          {urlState.asset && (
            <AssetSidePanelContent
              assetId={urlState.asset}
              onClose={handleClosePanel}
              onEdit={handleEdit}
              onAssign={handleAssignFromPanel}
              onDelete={handleDeleteFromPanel}
            />
          )}
        </Drawer>
      </div>

      {/* Create/Edit Drawer */}
      <Drawer
        isOpen={isDrawerOpen}
        onClose={() => {
          setIsDrawerOpen(false)
          setEditingAsset(null)
        }}
        title={editingAsset ? 'Edit Asset' : 'Add Asset'}
        size="lg"
      >
        <AssetForm
          asset={editingAsset || undefined}
          onSuccess={handleCreateSuccess}
          onCancel={() => {
            setIsDrawerOpen(false)
            setEditingAsset(null)
          }}
        />
      </Drawer>

      {/* Bulk Upload Modal */}
      <AssetBulkUploadModal
        isOpen={isBulkUploadOpen}
        onClose={() => setIsBulkUploadOpen(false)}
        onSuccess={() => {
          setIsBulkUploadOpen(false)
          refetch()
        }}
      />

      {/* Assign Modal */}
      {assigningAssetId && (
        <AssignAssetModal
          isOpen={!!assigningAssetId}
          onClose={() => setAssigningAssetId(null)}
          assetId={assigningAssetId}
          companyId={
            assets.find((a) => a.id === assigningAssetId)?.companyId || ''
          }
          onSuccess={() => {
            setAssigningAssetId(null)
            refetch()
          }}
        />
      )}

      {/* Delete Confirmation */}
      <Modal
        isOpen={!!deleteConfirm}
        onClose={() => setDeleteConfirm(null)}
        title="Delete Asset"
        size="sm"
      >
        <div className="space-y-4">
          <div className="flex items-start gap-3">
            <div className="flex-shrink-0 w-10 h-10 bg-red-100 rounded-full flex items-center justify-center">
              <AlertTriangle className="w-5 h-5 text-red-600" />
            </div>
            <div>
              <p className="text-gray-600">
                Are you sure you want to delete{' '}
                <strong>{deleteConfirm?.name}</strong>?
              </p>
              <p className="text-sm text-gray-500 mt-1">
                This action cannot be undone.
              </p>
            </div>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setDeleteConfirm(null)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteAsset.isPending}
            >
              {deleteAsset.isPending ? 'Deleting...' : 'Delete'}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

export default AssetsManagement

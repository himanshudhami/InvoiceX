import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useStockItems,
  useLowStockItems,
  useDeleteStockItem,
} from '@/features/inventory/hooks';
import type { StockItem } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import { StockItemForm } from '@/components/forms/StockItemForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { Edit, Trash2, AlertTriangle, Package, Eye } from 'lucide-react';

const StockItemsManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<StockItem | null>(null);
  const [deletingItem, setDeletingItem] = useState<StockItem | null>(null);
  const [viewingItem, setViewingItem] = useState<StockItem | null>(null);
  const [companyFilter, setCompanyFilter] = useState<string>('');
  const [showLowStockOnly, setShowLowStockOnly] = useState(false);

  const { data: allItems = [], isLoading, error, refetch } = useStockItems(companyFilter || undefined);
  const { data: lowStockItems = [] } = useLowStockItems(companyFilter || undefined);
  const deleteItem = useDeleteStockItem();

  const lowStockIds = useMemo(() => new Set(lowStockItems.map((i) => i.id)), [lowStockItems]);

  const items = useMemo(() => {
    let filtered = companyFilter
      ? allItems.filter((i) => i.companyId === companyFilter)
      : allItems;
    if (showLowStockOnly) {
      filtered = filtered.filter((i) => lowStockIds.has(i.id));
    }
    return filtered;
  }, [allItems, companyFilter, showLowStockOnly, lowStockIds]);

  const handleEdit = (item: StockItem) => {
    setEditingItem(item);
  };

  const handleDelete = (item: StockItem) => {
    setDeletingItem(item);
  };

  const handleView = (item: StockItem) => {
    setViewingItem(item);
  };

  const handleDeleteConfirm = async () => {
    if (deletingItem) {
      try {
        await deleteItem.mutateAsync(deletingItem.id);
        setDeletingItem(null);
      } catch (error) {
        console.error('Failed to delete stock item:', error);
      }
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingItem(null);
    refetch();
  };

  const formatCurrency = (value: number | undefined) => {
    if (value === undefined) return '-';
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2,
    }).format(value);
  };

  const columns: ColumnDef<StockItem>[] = [
    {
      accessorKey: 'name',
      header: 'Item',
      cell: ({ row }) => {
        const item = row.original;
        const isLowStock = lowStockIds.has(item.id);
        return (
          <div className="flex items-start gap-2">
            {isLowStock && (
              <AlertTriangle className="h-4 w-4 text-red-500 mt-0.5 flex-shrink-0" />
            )}
            <div>
              <div className="font-medium text-gray-900">{item.name}</div>
              {item.sku && (
                <div className="text-sm text-gray-500">SKU: {item.sku}</div>
              )}
              {item.stockGroupName && (
                <div className="text-xs text-gray-400">{item.stockGroupName}</div>
              )}
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'currentQuantity',
      header: 'Current Stock',
      cell: ({ row }) => {
        const item = row.original;
        const isLowStock = lowStockIds.has(item.id);
        return (
          <div>
            <div
              className={`font-medium ${isLowStock ? 'text-red-600' : 'text-gray-900'}`}
            >
              {item.currentQuantity.toFixed(item.baseUnitSymbol === 'pcs' ? 0 : 2)}{' '}
              {item.baseUnitSymbol}
            </div>
            {item.reorderLevel && (
              <div className="text-xs text-gray-500">
                Reorder at: {item.reorderLevel}
              </div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'currentValue',
      header: 'Stock Value',
      cell: ({ row }) => (
        <div className="font-medium text-gray-900">
          {formatCurrency(row.original.currentValue)}
        </div>
      ),
    },
    {
      accessorKey: 'sellingPrice',
      header: 'Price',
      cell: ({ row }) => {
        const item = row.original;
        return (
          <div>
            {item.sellingPrice && (
              <div className="text-sm text-gray-900">
                Sell: {formatCurrency(item.sellingPrice)}
              </div>
            )}
            {item.costPrice && (
              <div className="text-xs text-gray-500">
                Cost: {formatCurrency(item.costPrice)}
              </div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'gstRate',
      header: 'GST',
      cell: ({ row }) => (
        <div className="text-sm text-gray-900">{row.getValue('gstRate')}%</div>
      ),
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => {
        const isActive = row.getValue('isActive') as boolean;
        return (
          <div
            className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
              isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
            }`}
          >
            {isActive ? 'Active' : 'Inactive'}
          </div>
        );
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const item = row.original;
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleView(item)}
              className="text-gray-600 hover:text-gray-800 p-1 rounded hover:bg-gray-50 transition-colors"
              title="View details"
            >
              <Eye size={16} />
            </button>
            <button
              onClick={() => handleEdit(item)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit item"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(item)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete item"
            >
              <Trash2 size={16} />
            </button>
          </div>
        );
      },
    },
  ];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load stock items</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Stock Items</h1>
        <p className="text-gray-600 mt-2">Manage your inventory products and materials</p>
      </div>

      {lowStockItems.length > 0 && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-red-600" />
            <span className="font-medium text-red-800">
              {lowStockItems.length} item{lowStockItems.length > 1 ? 's' : ''} below reorder
              level
            </span>
            <button
              onClick={() => setShowLowStockOnly(!showLowStockOnly)}
              className="ml-auto text-sm text-red-600 hover:text-red-800 underline"
            >
              {showLowStockOnly ? 'Show all items' : 'Show only low stock'}
            </button>
          </div>
        </div>
      )}

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="mb-4 flex items-center gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
              <CompanyFilterDropdown value={companyFilter} onChange={setCompanyFilter} />
            </div>
          </div>
          <DataTable
            columns={columns}
            data={items}
            searchPlaceholder="Search items..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Stock Item"
          />
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Stock Item"
        size="xl"
      >
        <StockItemForm
          companyId={companyFilter || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingItem}
        onClose={() => setEditingItem(null)}
        title="Edit Stock Item"
        size="xl"
      >
        {editingItem && (
          <StockItemForm
            stockItem={editingItem}
            companyId={editingItem.companyId}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingItem(null)}
          />
        )}
      </Drawer>

      <Drawer
        isOpen={!!viewingItem}
        onClose={() => setViewingItem(null)}
        title="Stock Item Details"
        size="lg"
      >
        {viewingItem && (
          <div className="space-y-6">
            <div className="flex items-center gap-4">
              <div className="h-16 w-16 rounded-lg bg-blue-50 flex items-center justify-center">
                <Package className="h-8 w-8 text-blue-600" />
              </div>
              <div>
                <h3 className="text-xl font-semibold text-gray-900">{viewingItem.name}</h3>
                {viewingItem.sku && (
                  <p className="text-sm text-gray-500">SKU: {viewingItem.sku}</p>
                )}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="text-sm text-gray-500">Current Stock</div>
                <div className="text-2xl font-bold text-gray-900">
                  {viewingItem.currentQuantity} {viewingItem.baseUnitSymbol}
                </div>
              </div>
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="text-sm text-gray-500">Stock Value</div>
                <div className="text-2xl font-bold text-gray-900">
                  {formatCurrency(viewingItem.currentValue)}
                </div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-gray-500">Stock Group:</span>
                <span className="ml-2 text-gray-900">
                  {viewingItem.stockGroupName || '-'}
                </span>
              </div>
              <div>
                <span className="text-gray-500">Base Unit:</span>
                <span className="ml-2 text-gray-900">
                  {viewingItem.baseUnitName} ({viewingItem.baseUnitSymbol})
                </span>
              </div>
              <div>
                <span className="text-gray-500">HSN/SAC:</span>
                <span className="ml-2 text-gray-900">{viewingItem.hsnSacCode || '-'}</span>
              </div>
              <div>
                <span className="text-gray-500">GST Rate:</span>
                <span className="ml-2 text-gray-900">{viewingItem.gstRate}%</span>
              </div>
              <div>
                <span className="text-gray-500">Selling Price:</span>
                <span className="ml-2 text-gray-900">
                  {formatCurrency(viewingItem.sellingPrice)}
                </span>
              </div>
              <div>
                <span className="text-gray-500">Cost Price:</span>
                <span className="ml-2 text-gray-900">
                  {formatCurrency(viewingItem.costPrice)}
                </span>
              </div>
              <div>
                <span className="text-gray-500">Reorder Level:</span>
                <span className="ml-2 text-gray-900">{viewingItem.reorderLevel || '-'}</span>
              </div>
              <div>
                <span className="text-gray-500">Valuation:</span>
                <span className="ml-2 text-gray-900 capitalize">
                  {viewingItem.valuationMethod.replace('_', ' ')}
                </span>
              </div>
            </div>

            <div className="flex justify-end pt-4 border-t">
              <button
                onClick={() => setViewingItem(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Close
              </button>
            </div>
          </div>
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingItem}
        onClose={() => setDeletingItem(null)}
        title="Delete Stock Item"
        size="sm"
      >
        {deletingItem && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingItem.name}</strong>? This action
              cannot be undone and will affect stock history.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingItem(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteItem.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteItem.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default StockItemsManagement;

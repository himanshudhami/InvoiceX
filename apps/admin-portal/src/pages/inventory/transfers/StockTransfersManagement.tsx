import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useStockTransfersPaged,
  useDeleteStockTransfer,
  useDispatchStockTransfer,
  useCompleteStockTransfer,
  useCancelStockTransfer,
} from '@/features/inventory/hooks';
import type { StockTransfer, TransferStatus, StockTransferFilterParams } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import { StockTransferForm } from '@/components/forms/StockTransferForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { cn } from '@/lib/utils';
import {
  Edit,
  Trash2,
  Truck,
  CheckCircle,
  XCircle,
  Eye,
  ArrowRight,
  Package,
} from 'lucide-react';

const statusTabs: { value: TransferStatus | 'all'; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'draft', label: 'Draft' },
  { value: 'in_transit', label: 'In Transit' },
  { value: 'completed', label: 'Completed' },
  { value: 'cancelled', label: 'Cancelled' },
];

const StockTransfersManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingTransfer, setEditingTransfer] = useState<StockTransfer | null>(null);
  const [viewingTransfer, setViewingTransfer] = useState<StockTransfer | null>(null);
  const [deletingTransfer, setDeletingTransfer] = useState<StockTransfer | null>(null);
  const [companyFilter, setCompanyFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<TransferStatus | 'all'>('all');
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 20;

  const params: StockTransferFilterParams = useMemo(
    () => ({
      companyId: companyFilter || undefined,
      status: statusFilter === 'all' ? undefined : statusFilter,
      pageNumber,
      pageSize,
    }),
    [companyFilter, statusFilter, pageNumber]
  );

  const { data: pagedData, isLoading, error, refetch } = useStockTransfersPaged(params);
  const deleteTransfer = useDeleteStockTransfer();
  const dispatchTransfer = useDispatchStockTransfer();
  const completeTransfer = useCompleteStockTransfer();
  const cancelTransfer = useCancelStockTransfer();

  const transfers = pagedData?.items ?? [];
  const totalCount = pagedData?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingTransfer(null);
    refetch();
  };

  const handleDispatch = async (transfer: StockTransfer) => {
    try {
      await dispatchTransfer.mutateAsync(transfer.id);
    } catch (error) {
      console.error('Failed to dispatch transfer:', error);
    }
  };

  const handleComplete = async (transfer: StockTransfer) => {
    try {
      await completeTransfer.mutateAsync(transfer.id);
    } catch (error) {
      console.error('Failed to complete transfer:', error);
    }
  };

  const handleCancel = async (transfer: StockTransfer) => {
    try {
      await cancelTransfer.mutateAsync(transfer.id);
    } catch (error) {
      console.error('Failed to cancel transfer:', error);
    }
  };

  const handleDeleteConfirm = async () => {
    if (deletingTransfer) {
      try {
        await deleteTransfer.mutateAsync(deletingTransfer.id);
        setDeletingTransfer(null);
      } catch (error) {
        console.error('Failed to delete transfer:', error);
      }
    }
  };

  const getStatusConfig = (status: TransferStatus) => {
    const configs = {
      draft: { label: 'Draft', color: 'bg-gray-100 text-gray-800', icon: Edit },
      in_transit: { label: 'In Transit', color: 'bg-blue-100 text-blue-800', icon: Truck },
      completed: { label: 'Completed', color: 'bg-green-100 text-green-800', icon: CheckCircle },
      cancelled: { label: 'Cancelled', color: 'bg-red-100 text-red-800', icon: XCircle },
    };
    return configs[status];
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2,
    }).format(value);
  };

  const columns: ColumnDef<StockTransfer>[] = [
    {
      accessorKey: 'transferNumber',
      header: 'Transfer #',
      cell: ({ row }) => {
        const transfer = row.original;
        return (
          <div>
            <div className="font-medium text-gray-900">{transfer.transferNumber}</div>
            <div className="text-sm text-gray-500">
              {new Date(transfer.transferDate).toLocaleDateString('en-IN', {
                day: '2-digit',
                month: 'short',
                year: 'numeric',
              })}
            </div>
          </div>
        );
      },
    },
    {
      id: 'route',
      header: 'Route',
      cell: ({ row }) => {
        const transfer = row.original;
        return (
          <div className="flex items-center gap-2 text-sm">
            <span className="font-medium text-gray-900">{transfer.fromWarehouseName}</span>
            <ArrowRight className="h-4 w-4 text-gray-400" />
            <span className="font-medium text-gray-900">{transfer.toWarehouseName}</span>
          </div>
        );
      },
    },
    {
      accessorKey: 'totalQuantity',
      header: 'Qty',
      cell: ({ row }) => (
        <div className="text-sm text-gray-900">{row.getValue('totalQuantity')}</div>
      ),
    },
    {
      accessorKey: 'totalValue',
      header: 'Value',
      cell: ({ row }) => (
        <div className="font-medium text-gray-900">
          {formatCurrency(row.getValue('totalValue'))}
        </div>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.getValue('status') as TransferStatus;
        const config = getStatusConfig(status);
        const Icon = config.icon;
        return (
          <div className={`inline-flex items-center gap-1 px-2 py-1 text-xs font-medium rounded-full ${config.color}`}>
            <Icon className="h-3 w-3" />
            {config.label}
          </div>
        );
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const transfer = row.original;
        const status = transfer.status;

        return (
          <div className="flex space-x-1">
            <button
              onClick={() => setViewingTransfer(transfer)}
              className="text-gray-600 hover:text-gray-800 p-1 rounded hover:bg-gray-50 transition-colors"
              title="View details"
            >
              <Eye size={16} />
            </button>

            {status === 'draft' && (
              <>
                <button
                  onClick={() => setEditingTransfer(transfer)}
                  className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                  title="Edit"
                >
                  <Edit size={16} />
                </button>
                <button
                  onClick={() => handleDispatch(transfer)}
                  className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                  title="Dispatch"
                >
                  <Truck size={16} />
                </button>
                <button
                  onClick={() => setDeletingTransfer(transfer)}
                  className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                  title="Delete"
                >
                  <Trash2 size={16} />
                </button>
              </>
            )}

            {status === 'in_transit' && (
              <>
                <button
                  onClick={() => handleComplete(transfer)}
                  className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                  title="Complete"
                >
                  <CheckCircle size={16} />
                </button>
                <button
                  onClick={() => handleCancel(transfer)}
                  className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                  title="Cancel"
                >
                  <XCircle size={16} />
                </button>
              </>
            )}
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
        <div className="text-red-600 mb-4">Failed to load stock transfers</div>
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
        <h1 className="text-3xl font-bold text-gray-900">Stock Transfers</h1>
        <p className="text-gray-600 mt-2">Manage inter-warehouse stock movements</p>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="mb-4 flex flex-wrap items-end gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
              <CompanyFilterDropdown value={companyFilter} onChange={setCompanyFilter} />
            </div>
          </div>

          {/* Status Tabs */}
          <div className="mb-4 border-b border-gray-200">
            <nav className="-mb-px flex space-x-4">
              {statusTabs.map((tab) => (
                <button
                  key={tab.value}
                  onClick={() => {
                    setStatusFilter(tab.value);
                    setPageNumber(1);
                  }}
                  className={cn(
                    'px-3 py-2 text-sm font-medium border-b-2 transition-colors',
                    statusFilter === tab.value
                      ? 'border-primary text-primary'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  )}
                >
                  {tab.label}
                </button>
              ))}
            </nav>
          </div>

          <DataTable
            columns={columns}
            data={transfers}
            searchPlaceholder="Search transfers..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="New Transfer"
          />

          {totalPages > 1 && (
            <div className="mt-4 flex items-center justify-between border-t pt-4">
              <div className="text-sm text-gray-500">
                Showing {(pageNumber - 1) * pageSize + 1} to{' '}
                {Math.min(pageNumber * pageSize, totalCount)} of {totalCount} entries
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                  disabled={pageNumber === 1}
                  className="px-3 py-1 text-sm border rounded-md disabled:opacity-50 hover:bg-gray-50"
                >
                  Previous
                </button>
                <span className="px-3 py-1 text-sm">
                  Page {pageNumber} of {totalPages}
                </span>
                <button
                  onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
                  disabled={pageNumber === totalPages}
                  className="px-3 py-1 text-sm border rounded-md disabled:opacity-50 hover:bg-gray-50"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="New Stock Transfer"
        size="xl"
      >
        <StockTransferForm
          companyId={companyFilter || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingTransfer}
        onClose={() => setEditingTransfer(null)}
        title="Edit Stock Transfer"
        size="xl"
      >
        {editingTransfer && (
          <StockTransferForm
            transfer={editingTransfer}
            companyId={editingTransfer.companyId}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingTransfer(null)}
          />
        )}
      </Drawer>

      <Drawer
        isOpen={!!viewingTransfer}
        onClose={() => setViewingTransfer(null)}
        title="Transfer Details"
        size="lg"
      >
        {viewingTransfer && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-xl font-semibold text-gray-900">
                  {viewingTransfer.transferNumber}
                </h3>
                <p className="text-sm text-gray-500">
                  {new Date(viewingTransfer.transferDate).toLocaleDateString('en-IN', {
                    weekday: 'long',
                    day: '2-digit',
                    month: 'long',
                    year: 'numeric',
                  })}
                </p>
              </div>
              <div
                className={`px-3 py-1 text-sm font-medium rounded-full ${
                  getStatusConfig(viewingTransfer.status).color
                }`}
              >
                {getStatusConfig(viewingTransfer.status).label}
              </div>
            </div>

            <div className="flex items-center gap-4 p-4 bg-gray-50 rounded-lg">
              <div className="flex-1 text-center">
                <div className="text-sm text-gray-500">From</div>
                <div className="font-medium text-gray-900">
                  {viewingTransfer.fromWarehouseName}
                </div>
              </div>
              <ArrowRight className="h-6 w-6 text-gray-400" />
              <div className="flex-1 text-center">
                <div className="text-sm text-gray-500">To</div>
                <div className="font-medium text-gray-900">
                  {viewingTransfer.toWarehouseName}
                </div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="text-sm text-gray-500">Total Quantity</div>
                <div className="text-xl font-bold text-gray-900">
                  {viewingTransfer.totalQuantity}
                </div>
              </div>
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="text-sm text-gray-500">Total Value</div>
                <div className="text-xl font-bold text-gray-900">
                  {formatCurrency(viewingTransfer.totalValue)}
                </div>
              </div>
            </div>

            {viewingTransfer.items && viewingTransfer.items.length > 0 && (
              <div>
                <h4 className="font-medium text-gray-900 mb-3">Items</h4>
                <div className="border rounded-lg divide-y">
                  {viewingTransfer.items.map((item, index) => (
                    <div key={index} className="p-3 flex items-center gap-3">
                      <div className="h-8 w-8 rounded bg-blue-50 flex items-center justify-center">
                        <Package className="h-4 w-4 text-blue-600" />
                      </div>
                      <div className="flex-1">
                        <div className="font-medium text-gray-900">{item.stockItemName}</div>
                        {item.stockItemSku && (
                          <div className="text-xs text-gray-500">{item.stockItemSku}</div>
                        )}
                      </div>
                      <div className="text-right">
                        <div className="font-medium text-gray-900">
                          {item.quantity} {item.unitSymbol}
                        </div>
                        {item.rate && (
                          <div className="text-xs text-gray-500">
                            @ {formatCurrency(item.rate)}
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {viewingTransfer.notes && (
              <div>
                <h4 className="font-medium text-gray-900 mb-2">Notes</h4>
                <p className="text-sm text-gray-600">{viewingTransfer.notes}</p>
              </div>
            )}

            <div className="flex justify-end pt-4 border-t">
              <button
                onClick={() => setViewingTransfer(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Close
              </button>
            </div>
          </div>
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingTransfer}
        onClose={() => setDeletingTransfer(null)}
        title="Delete Transfer"
        size="sm"
      >
        {deletingTransfer && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete transfer{' '}
              <strong>{deletingTransfer.transferNumber}</strong>? This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingTransfer(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteTransfer.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteTransfer.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default StockTransfersManagement;

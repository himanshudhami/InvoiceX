import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useProductionOrders,
  useDeleteProductionOrder,
  useReleaseProductionOrder,
  useStartProductionOrder,
  useCompleteProductionOrder,
  useCancelProductionOrder,
} from '@/features/manufacturing/hooks';
import type { ProductionOrder } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import { ProductionOrderForm } from '@/components/forms/ProductionOrderForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { Edit, Trash2, Eye, Play, CheckCircle, XCircle, Send, Factory } from 'lucide-react';

const ProductionOrdersManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingOrder, setEditingOrder] = useState<ProductionOrder | null>(null);
  const [deletingOrder, setDeletingOrder] = useState<ProductionOrder | null>(null);
  const [viewingOrder, setViewingOrder] = useState<ProductionOrder | null>(null);
  const [companyFilter, setCompanyFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [completeQty, setCompleteQty] = useState<number>(0);
  const [completingOrder, setCompletingOrder] = useState<ProductionOrder | null>(null);

  const { data: allOrders = [], isLoading, error, refetch } = useProductionOrders(companyFilter || undefined);
  const deleteOrder = useDeleteProductionOrder();
  const releaseOrder = useReleaseProductionOrder();
  const startOrder = useStartProductionOrder();
  const completeOrder = useCompleteProductionOrder();
  const cancelOrder = useCancelProductionOrder();

  const orders = useMemo(() => {
    let filtered = companyFilter
      ? allOrders.filter((o) => o.companyId === companyFilter)
      : allOrders;
    if (statusFilter) {
      filtered = filtered.filter((o) => o.status === statusFilter);
    }
    return filtered;
  }, [allOrders, companyFilter, statusFilter]);

  const handleEdit = (order: ProductionOrder) => {
    if (order.status === 'draft') {
      setEditingOrder(order);
    }
  };

  const handleDelete = (order: ProductionOrder) => {
    if (order.status === 'draft') {
      setDeletingOrder(order);
    }
  };

  const handleView = (order: ProductionOrder) => {
    setViewingOrder(order);
  };

  const handleRelease = async (order: ProductionOrder) => {
    try {
      await releaseOrder.mutateAsync({ id: order.id });
      refetch();
    } catch (error) {
      console.error('Failed to release order:', error);
    }
  };

  const handleStart = async (order: ProductionOrder) => {
    try {
      await startOrder.mutateAsync({ id: order.id });
      refetch();
    } catch (error) {
      console.error('Failed to start order:', error);
    }
  };

  const handleCompleteClick = (order: ProductionOrder) => {
    setCompletingOrder(order);
    setCompleteQty(order.plannedQuantity);
  };

  const handleCompleteConfirm = async () => {
    if (completingOrder && completeQty > 0) {
      try {
        await completeOrder.mutateAsync({
          id: completingOrder.id,
          data: { producedQuantity: completeQty },
        });
        setCompletingOrder(null);
        refetch();
      } catch (error) {
        console.error('Failed to complete order:', error);
      }
    }
  };

  const handleCancel = async (order: ProductionOrder) => {
    try {
      await cancelOrder.mutateAsync({ id: order.id });
      refetch();
    } catch (error) {
      console.error('Failed to cancel order:', error);
    }
  };

  const handleDeleteConfirm = async () => {
    if (deletingOrder) {
      try {
        await deleteOrder.mutateAsync(deletingOrder.id);
        setDeletingOrder(null);
      } catch (error) {
        console.error('Failed to delete order:', error);
      }
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingOrder(null);
    refetch();
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'draft':
        return 'bg-gray-100 text-gray-800';
      case 'released':
        return 'bg-blue-100 text-blue-800';
      case 'in_progress':
        return 'bg-yellow-100 text-yellow-800';
      case 'completed':
        return 'bg-green-100 text-green-800';
      case 'cancelled':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const columns: ColumnDef<ProductionOrder>[] = [
    {
      accessorKey: 'orderNumber',
      header: 'Order',
      cell: ({ row }) => {
        const order = row.original;
        return (
          <div>
            <div className="font-medium text-gray-900">{order.orderNumber}</div>
            <div className="text-sm text-gray-500">{order.bomName}</div>
          </div>
        );
      },
    },
    {
      accessorKey: 'finishedGoodName',
      header: 'Product',
      cell: ({ row }) => (
        <div className="text-gray-900">{row.original.finishedGoodName || '-'}</div>
      ),
    },
    {
      accessorKey: 'plannedQuantity',
      header: 'Quantity',
      cell: ({ row }) => {
        const order = row.original;
        return (
          <div>
            <div className="text-gray-900">Planned: {order.plannedQuantity}</div>
            {order.producedQuantity > 0 && (
              <div className="text-sm text-green-600">Produced: {order.producedQuantity}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'warehouseName',
      header: 'Warehouse',
      cell: ({ row }) => (
        <div className="text-gray-900">{row.original.warehouseName || '-'}</div>
      ),
    },
    {
      accessorKey: 'plannedStartDate',
      header: 'Schedule',
      cell: ({ row }) => {
        const order = row.original;
        return (
          <div className="text-sm">
            <div className="text-gray-900">
              Start: {order.plannedStartDate ? new Date(order.plannedStartDate).toLocaleDateString() : '-'}
            </div>
            {order.plannedEndDate && (
              <div className="text-gray-500">
                End: {new Date(order.plannedEndDate).toLocaleDateString()}
              </div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.getValue('status') as string;
        return (
          <div
            className={`inline-flex px-2 py-1 text-xs font-medium rounded-full capitalize ${getStatusColor(status)}`}
          >
            {status.replace('_', ' ')}
          </div>
        );
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const order = row.original;
        return (
          <div className="flex space-x-1">
            <button
              onClick={() => handleView(order)}
              className="text-gray-600 hover:text-gray-800 p-1 rounded hover:bg-gray-50 transition-colors"
              title="View details"
            >
              <Eye size={16} />
            </button>

            {order.status === 'draft' && (
              <>
                <button
                  onClick={() => handleRelease(order)}
                  className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                  title="Release order"
                >
                  <Send size={16} />
                </button>
                <button
                  onClick={() => handleEdit(order)}
                  className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                  title="Edit order"
                >
                  <Edit size={16} />
                </button>
                <button
                  onClick={() => handleDelete(order)}
                  className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                  title="Delete order"
                >
                  <Trash2 size={16} />
                </button>
              </>
            )}

            {order.status === 'released' && (
              <>
                <button
                  onClick={() => handleStart(order)}
                  className="text-yellow-600 hover:text-yellow-800 p-1 rounded hover:bg-yellow-50 transition-colors"
                  title="Start production"
                >
                  <Play size={16} />
                </button>
                <button
                  onClick={() => handleCancel(order)}
                  className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                  title="Cancel order"
                >
                  <XCircle size={16} />
                </button>
              </>
            )}

            {order.status === 'in_progress' && (
              <>
                <button
                  onClick={() => handleCompleteClick(order)}
                  className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                  title="Complete production"
                >
                  <CheckCircle size={16} />
                </button>
                <button
                  onClick={() => handleCancel(order)}
                  className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                  title="Cancel order"
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

  const statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'draft', label: 'Draft' },
    { value: 'released', label: 'Released' },
    { value: 'in_progress', label: 'In Progress' },
    { value: 'completed', label: 'Completed' },
    { value: 'cancelled', label: 'Cancelled' },
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
        <div className="text-red-600 mb-4">Failed to load production orders</div>
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
        <h1 className="text-3xl font-bold text-gray-900">Production Orders</h1>
        <p className="text-gray-600 mt-2">Manage manufacturing production orders</p>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="mb-4 flex items-center gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
              <CompanyFilterDropdown value={companyFilter} onChange={setCompanyFilter} />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Status</label>
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                {statusOptions.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <DataTable
            columns={columns}
            data={orders}
            searchPlaceholder="Search orders..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Create Order"
          />
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Production Order"
        size="xl"
      >
        <ProductionOrderForm
          companyId={companyFilter || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingOrder}
        onClose={() => setEditingOrder(null)}
        title="Edit Production Order"
        size="xl"
      >
        {editingOrder && (
          <ProductionOrderForm
            order={editingOrder}
            companyId={editingOrder.companyId}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingOrder(null)}
          />
        )}
      </Drawer>

      <Drawer
        isOpen={!!viewingOrder}
        onClose={() => setViewingOrder(null)}
        title="Production Order Details"
        size="lg"
      >
        {viewingOrder && (
          <div className="space-y-6">
            <div className="flex items-center gap-4">
              <div className="h-16 w-16 rounded-lg bg-blue-50 flex items-center justify-center">
                <Factory className="h-8 w-8 text-blue-600" />
              </div>
              <div>
                <h3 className="text-xl font-semibold text-gray-900">{viewingOrder.orderNumber}</h3>
                <p className="text-sm text-gray-500">{viewingOrder.finishedGoodName}</p>
                <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full capitalize mt-1 ${getStatusColor(viewingOrder.status)}`}>
                  {viewingOrder.status.replace('_', ' ')}
                </div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="text-sm text-gray-500">Planned Quantity</div>
                <div className="text-2xl font-bold text-gray-900">{viewingOrder.plannedQuantity}</div>
              </div>
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="text-sm text-gray-500">Produced Quantity</div>
                <div className="text-2xl font-bold text-green-600">{viewingOrder.producedQuantity || 0}</div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-gray-500">BOM:</span>
                <span className="ml-2 text-gray-900">{viewingOrder.bomName || '-'}</span>
              </div>
              <div>
                <span className="text-gray-500">Warehouse:</span>
                <span className="ml-2 text-gray-900">{viewingOrder.warehouseName || '-'}</span>
              </div>
              <div>
                <span className="text-gray-500">Planned Start:</span>
                <span className="ml-2 text-gray-900">
                  {viewingOrder.plannedStartDate ? new Date(viewingOrder.plannedStartDate).toLocaleDateString() : '-'}
                </span>
              </div>
              <div>
                <span className="text-gray-500">Planned End:</span>
                <span className="ml-2 text-gray-900">
                  {viewingOrder.plannedEndDate ? new Date(viewingOrder.plannedEndDate).toLocaleDateString() : '-'}
                </span>
              </div>
              {viewingOrder.actualStartDate && (
                <div>
                  <span className="text-gray-500">Actual Start:</span>
                  <span className="ml-2 text-gray-900">
                    {new Date(viewingOrder.actualStartDate).toLocaleDateString()}
                  </span>
                </div>
              )}
              {viewingOrder.actualEndDate && (
                <div>
                  <span className="text-gray-500">Actual End:</span>
                  <span className="ml-2 text-gray-900">
                    {new Date(viewingOrder.actualEndDate).toLocaleDateString()}
                  </span>
                </div>
              )}
            </div>

            {viewingOrder.items && viewingOrder.items.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-gray-700 mb-3">Components</h4>
                <div className="border rounded-lg divide-y">
                  {viewingOrder.items.map((item, index) => (
                    <div key={index} className="p-3 flex justify-between items-center">
                      <div className="font-medium text-gray-900">
                        {item.stockItemName || item.stockItemId}
                      </div>
                      <div className="text-right">
                        <div className="text-sm">
                          Required: <span className="font-medium">{item.requiredQuantity}</span>
                        </div>
                        <div className="text-sm text-green-600">
                          Consumed: <span className="font-medium">{item.consumedQuantity || 0}</span>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {viewingOrder.notes && (
              <div>
                <h4 className="text-sm font-medium text-gray-700 mb-1">Notes</h4>
                <p className="text-gray-600">{viewingOrder.notes}</p>
              </div>
            )}

            <div className="flex justify-end pt-4 border-t">
              <button
                onClick={() => setViewingOrder(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Close
              </button>
            </div>
          </div>
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingOrder}
        onClose={() => setDeletingOrder(null)}
        title="Delete Production Order"
        size="sm"
      >
        {deletingOrder && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete order <strong>{deletingOrder.orderNumber}</strong>?
              This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingOrder(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteOrder.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteOrder.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        isOpen={!!completingOrder}
        onClose={() => setCompletingOrder(null)}
        title="Complete Production Order"
        size="sm"
      >
        {completingOrder && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Complete order <strong>{completingOrder.orderNumber}</strong>
            </p>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Produced Quantity
              </label>
              <input
                type="number"
                step="0.01"
                min="0.01"
                value={completeQty}
                onChange={(e) => setCompleteQty(parseFloat(e.target.value) || 0)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              />
              <p className="text-sm text-gray-500 mt-1">
                Planned: {completingOrder.plannedQuantity}
              </p>
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setCompletingOrder(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleCompleteConfirm}
                disabled={completeOrder.isPending || completeQty <= 0}
                className="px-4 py-2 text-sm font-medium text-white bg-green-600 border border-transparent rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {completeOrder.isPending ? 'Completing...' : 'Complete Order'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default ProductionOrdersManagement;

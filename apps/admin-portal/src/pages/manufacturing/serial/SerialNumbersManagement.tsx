import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useSerialNumbers,
  useDeleteSerialNumber,
  useUpdateSerialStatus,
} from '@/features/manufacturing/hooks';
import type { SerialNumber } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import { SerialNumberForm } from '@/components/forms/SerialNumberForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { Edit, Trash2, Eye, Hash } from 'lucide-react';

const SerialNumbersManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingSerial, setEditingSerial] = useState<SerialNumber | null>(null);
  const [deletingSerial, setDeletingSerial] = useState<SerialNumber | null>(null);
  const [viewingSerial, setViewingSerial] = useState<SerialNumber | null>(null);
  const [companyFilter, setCompanyFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');

  const { data: allSerials = [], isLoading, error, refetch } = useSerialNumbers(companyFilter || undefined);
  const deleteSerial = useDeleteSerialNumber();
  const updateStatus = useUpdateSerialStatus();

  const serials = useMemo(() => {
    let filtered = companyFilter
      ? allSerials.filter((s) => s.companyId === companyFilter)
      : allSerials;
    if (statusFilter) {
      filtered = filtered.filter((s) => s.status === statusFilter);
    }
    return filtered;
  }, [allSerials, companyFilter, statusFilter]);

  const handleEdit = (serial: SerialNumber) => {
    setEditingSerial(serial);
  };

  const handleDelete = (serial: SerialNumber) => {
    if (serial.status === 'available') {
      setDeletingSerial(serial);
    }
  };

  const handleView = (serial: SerialNumber) => {
    setViewingSerial(serial);
  };

  const handleStatusChange = async (serial: SerialNumber, newStatus: string) => {
    try {
      await updateStatus.mutateAsync({ id: serial.id, status: newStatus });
      refetch();
    } catch (error) {
      console.error('Failed to update status:', error);
    }
  };

  const handleDeleteConfirm = async () => {
    if (deletingSerial) {
      try {
        await deleteSerial.mutateAsync(deletingSerial.id);
        setDeletingSerial(null);
      } catch (error) {
        console.error('Failed to delete serial:', error);
      }
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingSerial(null);
    refetch();
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'available':
        return 'bg-green-100 text-green-800';
      case 'reserved':
        return 'bg-yellow-100 text-yellow-800';
      case 'sold':
        return 'bg-blue-100 text-blue-800';
      case 'damaged':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const columns: ColumnDef<SerialNumber>[] = [
    {
      accessorKey: 'serialCode',
      header: 'Serial Number',
      cell: ({ row }) => {
        const serial = row.original;
        return (
          <div>
            <div className="font-mono font-medium text-gray-900">{serial.serialCode}</div>
            {serial.stockItemName && (
              <div className="text-sm text-gray-500">{serial.stockItemName}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'stockItemName',
      header: 'Item',
      cell: ({ row }) => (
        <div className="text-gray-900">{row.original.stockItemName || '-'}</div>
      ),
    },
    {
      accessorKey: 'warehouseName',
      header: 'Warehouse',
      cell: ({ row }) => (
        <div className="text-gray-900">{row.original.warehouseName || '-'}</div>
      ),
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
            {status}
          </div>
        );
      },
    },
    {
      accessorKey: 'createdAt',
      header: 'Created',
      cell: ({ row }) => {
        const date = row.original.createdAt;
        return (
          <div className="text-sm text-gray-500">
            {date ? new Date(date).toLocaleDateString() : '-'}
          </div>
        );
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const serial = row.original;
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleView(serial)}
              className="text-gray-600 hover:text-gray-800 p-1 rounded hover:bg-gray-50 transition-colors"
              title="View details"
            >
              <Eye size={16} />
            </button>
            <button
              onClick={() => handleEdit(serial)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit serial"
            >
              <Edit size={16} />
            </button>
            {serial.status === 'available' && (
              <button
                onClick={() => handleDelete(serial)}
                className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                title="Delete serial"
              >
                <Trash2 size={16} />
              </button>
            )}
          </div>
        );
      },
    },
  ];

  const statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'available', label: 'Available' },
    { value: 'reserved', label: 'Reserved' },
    { value: 'sold', label: 'Sold' },
    { value: 'damaged', label: 'Damaged' },
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
        <div className="text-red-600 mb-4">Failed to load serial numbers</div>
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
        <h1 className="text-3xl font-bold text-gray-900">Serial Numbers</h1>
        <p className="text-gray-600 mt-2">Track individual item serial numbers</p>
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
            data={serials}
            searchPlaceholder="Search serials..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Serial Number"
          />
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Add Serial Number"
        size="lg"
      >
        <SerialNumberForm
          companyId={companyFilter || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingSerial}
        onClose={() => setEditingSerial(null)}
        title="Edit Serial Number"
        size="lg"
      >
        {editingSerial && (
          <SerialNumberForm
            serialNumber={editingSerial}
            companyId={editingSerial.companyId}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingSerial(null)}
          />
        )}
      </Drawer>

      <Drawer
        isOpen={!!viewingSerial}
        onClose={() => setViewingSerial(null)}
        title="Serial Number Details"
        size="md"
      >
        {viewingSerial && (
          <div className="space-y-6">
            <div className="flex items-center gap-4">
              <div className="h-16 w-16 rounded-lg bg-blue-50 flex items-center justify-center">
                <Hash className="h-8 w-8 text-blue-600" />
              </div>
              <div>
                <h3 className="text-xl font-mono font-semibold text-gray-900">
                  {viewingSerial.serialCode}
                </h3>
                <p className="text-sm text-gray-500">{viewingSerial.stockItemName}</p>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-gray-500">Status:</span>
                <span className={`ml-2 px-2 py-1 text-xs font-medium rounded-full capitalize ${getStatusColor(viewingSerial.status)}`}>
                  {viewingSerial.status}
                </span>
              </div>
              <div>
                <span className="text-gray-500">Warehouse:</span>
                <span className="ml-2 text-gray-900">{viewingSerial.warehouseName || '-'}</span>
              </div>
              <div>
                <span className="text-gray-500">Created:</span>
                <span className="ml-2 text-gray-900">
                  {viewingSerial.createdAt ? new Date(viewingSerial.createdAt).toLocaleDateString() : '-'}
                </span>
              </div>
              {viewingSerial.soldAt && (
                <div>
                  <span className="text-gray-500">Sold At:</span>
                  <span className="ml-2 text-gray-900">
                    {new Date(viewingSerial.soldAt).toLocaleDateString()}
                  </span>
                </div>
              )}
              {viewingSerial.soldToPartyName && (
                <div className="col-span-2">
                  <span className="text-gray-500">Sold To:</span>
                  <span className="ml-2 text-gray-900">{viewingSerial.soldToPartyName}</span>
                </div>
              )}
            </div>

            {viewingSerial.notes && (
              <div>
                <h4 className="text-sm font-medium text-gray-700 mb-1">Notes</h4>
                <p className="text-gray-600">{viewingSerial.notes}</p>
              </div>
            )}

            {viewingSerial.status === 'available' && (
              <div className="border-t pt-4">
                <h4 className="text-sm font-medium text-gray-700 mb-2">Quick Actions</h4>
                <div className="flex gap-2">
                  <button
                    onClick={() => handleStatusChange(viewingSerial, 'reserved')}
                    className="px-3 py-1.5 text-sm font-medium text-yellow-700 bg-yellow-100 rounded-md hover:bg-yellow-200"
                  >
                    Mark Reserved
                  </button>
                  <button
                    onClick={() => handleStatusChange(viewingSerial, 'damaged')}
                    className="px-3 py-1.5 text-sm font-medium text-red-700 bg-red-100 rounded-md hover:bg-red-200"
                  >
                    Mark Damaged
                  </button>
                </div>
              </div>
            )}

            {viewingSerial.status === 'reserved' && (
              <div className="border-t pt-4">
                <h4 className="text-sm font-medium text-gray-700 mb-2">Quick Actions</h4>
                <div className="flex gap-2">
                  <button
                    onClick={() => handleStatusChange(viewingSerial, 'available')}
                    className="px-3 py-1.5 text-sm font-medium text-green-700 bg-green-100 rounded-md hover:bg-green-200"
                  >
                    Make Available
                  </button>
                </div>
              </div>
            )}

            <div className="flex justify-end pt-4 border-t">
              <button
                onClick={() => setViewingSerial(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Close
              </button>
            </div>
          </div>
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingSerial}
        onClose={() => setDeletingSerial(null)}
        title="Delete Serial Number"
        size="sm"
      >
        {deletingSerial && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete serial <strong className="font-mono">{deletingSerial.serialCode}</strong>?
              This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingSerial(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteSerial.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteSerial.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default SerialNumbersManagement;

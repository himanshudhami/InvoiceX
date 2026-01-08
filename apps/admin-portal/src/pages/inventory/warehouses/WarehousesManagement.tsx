import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useWarehouses,
  useDeleteWarehouse,
  useSetDefaultWarehouse,
} from '@/features/inventory/hooks';
import type { Warehouse } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import { WarehouseForm } from '@/components/forms/WarehouseForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { Edit, Trash2, Star, MapPin } from 'lucide-react';

const WarehousesManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingWarehouse, setEditingWarehouse] = useState<Warehouse | null>(null);
  const [deletingWarehouse, setDeletingWarehouse] = useState<Warehouse | null>(null);
  const [companyFilter, setCompanyFilter] = useState<string>('');

  const { data: allWarehouses = [], isLoading, error, refetch } = useWarehouses(companyFilter || undefined);
  const deleteWarehouse = useDeleteWarehouse();
  const setDefaultWarehouse = useSetDefaultWarehouse();

  const warehouses = useMemo(() => {
    if (!companyFilter) return allWarehouses;
    return allWarehouses.filter((w) => w.companyId === companyFilter);
  }, [allWarehouses, companyFilter]);

  const handleEdit = (warehouse: Warehouse) => {
    setEditingWarehouse(warehouse);
  };

  const handleDelete = (warehouse: Warehouse) => {
    setDeletingWarehouse(warehouse);
  };

  const handleDeleteConfirm = async () => {
    if (deletingWarehouse) {
      try {
        await deleteWarehouse.mutateAsync(deletingWarehouse.id);
        setDeletingWarehouse(null);
      } catch (error) {
        console.error('Failed to delete warehouse:', error);
      }
    }
  };

  const handleSetDefault = async (warehouse: Warehouse) => {
    try {
      await setDefaultWarehouse.mutateAsync(warehouse.id);
    } catch (error) {
      console.error('Failed to set default warehouse:', error);
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingWarehouse(null);
    refetch();
  };

  const columns: ColumnDef<Warehouse>[] = [
    {
      accessorKey: 'name',
      header: 'Warehouse',
      cell: ({ row }) => {
        const warehouse = row.original;
        return (
          <div className="flex items-start gap-2">
            {warehouse.isDefault && (
              <Star className="h-4 w-4 text-yellow-500 fill-yellow-500 mt-0.5" />
            )}
            <div>
              <div className="font-medium text-gray-900">{warehouse.name}</div>
              {warehouse.code && (
                <div className="text-sm text-gray-500">Code: {warehouse.code}</div>
              )}
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'address',
      header: 'Location',
      cell: ({ row }) => {
        const warehouse = row.original;
        const location = [warehouse.city, warehouse.state].filter(Boolean).join(', ');
        return (
          <div>
            {location && (
              <div className="flex items-center gap-1 text-sm text-gray-900">
                <MapPin className="h-3 w-3" />
                {location}
              </div>
            )}
            {warehouse.pinCode && (
              <div className="text-sm text-gray-500">PIN: {warehouse.pinCode}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'parentWarehouseName',
      header: 'Parent',
      cell: ({ row }) => {
        const parentName = row.original.parentWarehouseName;
        return parentName ? (
          <div className="text-sm text-gray-900">{parentName}</div>
        ) : (
          <div className="text-sm text-gray-400">-</div>
        );
      },
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
        const warehouse = row.original;
        return (
          <div className="flex space-x-2">
            {!warehouse.isDefault && warehouse.isActive && (
              <button
                onClick={() => handleSetDefault(warehouse)}
                className="text-yellow-600 hover:text-yellow-800 p-1 rounded hover:bg-yellow-50 transition-colors"
                title="Set as default"
              >
                <Star size={16} />
              </button>
            )}
            <button
              onClick={() => handleEdit(warehouse)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit warehouse"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(warehouse)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete warehouse"
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
        <div className="text-red-600 mb-4">Failed to load warehouses</div>
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
        <h1 className="text-3xl font-bold text-gray-900">Warehouses</h1>
        <p className="text-gray-600 mt-2">Manage your storage locations and godowns</p>
      </div>

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
            data={warehouses}
            searchPlaceholder="Search warehouses..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Warehouse"
          />
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create New Warehouse"
        size="lg"
      >
        <WarehouseForm
          companyId={companyFilter || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingWarehouse}
        onClose={() => setEditingWarehouse(null)}
        title="Edit Warehouse"
        size="lg"
      >
        {editingWarehouse && (
          <WarehouseForm
            warehouse={editingWarehouse}
            companyId={editingWarehouse.companyId}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingWarehouse(null)}
          />
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingWarehouse}
        onClose={() => setDeletingWarehouse(null)}
        title="Delete Warehouse"
        size="sm"
      >
        {deletingWarehouse && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingWarehouse.name}</strong>? This action
              cannot be undone and may affect stock movements.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingWarehouse(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteWarehouse.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteWarehouse.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default WarehousesManagement;

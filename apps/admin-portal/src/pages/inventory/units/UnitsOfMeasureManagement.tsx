import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useUnitsOfMeasure, useDeleteUnitOfMeasure } from '@/features/inventory/hooks';
import type { UnitOfMeasure } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import { UnitOfMeasureForm } from '@/components/forms/UnitOfMeasureForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { Edit, Trash2, Lock } from 'lucide-react';

const UnitsOfMeasureManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingUnit, setEditingUnit] = useState<UnitOfMeasure | null>(null);
  const [deletingUnit, setDeletingUnit] = useState<UnitOfMeasure | null>(null);
  const [companyFilter, setCompanyFilter] = useState<string>('');

  const { data: allUnits = [], isLoading, error, refetch } = useUnitsOfMeasure(companyFilter || undefined);
  const deleteUnit = useDeleteUnitOfMeasure();

  const units = useMemo(() => {
    if (!companyFilter) return allUnits;
    return allUnits.filter((u) => u.companyId === companyFilter || u.isSystemUnit);
  }, [allUnits, companyFilter]);

  const handleEdit = (unit: UnitOfMeasure) => {
    if (!unit.isSystemUnit) {
      setEditingUnit(unit);
    }
  };

  const handleDelete = (unit: UnitOfMeasure) => {
    if (!unit.isSystemUnit) {
      setDeletingUnit(unit);
    }
  };

  const handleDeleteConfirm = async () => {
    if (deletingUnit) {
      try {
        await deleteUnit.mutateAsync(deletingUnit.id);
        setDeletingUnit(null);
      } catch (error) {
        console.error('Failed to delete unit:', error);
      }
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingUnit(null);
    refetch();
  };

  const columns: ColumnDef<UnitOfMeasure>[] = [
    {
      accessorKey: 'name',
      header: 'Unit Name',
      cell: ({ row }) => {
        const unit = row.original;
        return (
          <div className="flex items-center gap-2">
            <div>
              <div className="font-medium text-gray-900">{unit.name}</div>
              <div className="text-sm text-gray-500">Symbol: {unit.symbol}</div>
            </div>
            {unit.isSystemUnit && (
              <Lock className="h-4 w-4 text-gray-400" title="System unit (read-only)" />
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'symbol',
      header: 'Symbol',
      cell: ({ row }) => (
        <div className="inline-flex px-2 py-1 text-sm font-mono bg-gray-100 rounded">
          {row.getValue('symbol')}
        </div>
      ),
    },
    {
      accessorKey: 'decimalPlaces',
      header: 'Decimals',
      cell: ({ row }) => {
        const decimals = row.getValue('decimalPlaces') as number;
        return <div className="text-sm text-gray-900">{decimals}</div>;
      },
    },
    {
      accessorKey: 'isSystemUnit',
      header: 'Type',
      cell: ({ row }) => {
        const isSystem = row.getValue('isSystemUnit') as boolean;
        return (
          <div
            className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
              isSystem ? 'bg-blue-100 text-blue-800' : 'bg-gray-100 text-gray-800'
            }`}
          >
            {isSystem ? 'System' : 'Custom'}
          </div>
        );
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const unit = row.original;
        if (unit.isSystemUnit) {
          return (
            <div className="text-sm text-gray-400 italic">Read-only</div>
          );
        }
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleEdit(unit)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit unit"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(unit)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete unit"
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
        <div className="text-red-600 mb-4">Failed to load units of measure</div>
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
        <h1 className="text-3xl font-bold text-gray-900">Units of Measure</h1>
        <p className="text-gray-600 mt-2">
          Manage measurement units for your inventory items
        </p>
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
            data={units}
            searchPlaceholder="Search units..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Unit"
          />
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create New Unit"
        size="md"
      >
        <UnitOfMeasureForm
          companyId={companyFilter || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingUnit}
        onClose={() => setEditingUnit(null)}
        title="Edit Unit"
        size="md"
      >
        {editingUnit && (
          <UnitOfMeasureForm
            unit={editingUnit}
            companyId={editingUnit.companyId || companyFilter}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingUnit(null)}
          />
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingUnit}
        onClose={() => setDeletingUnit(null)}
        title="Delete Unit"
        size="sm"
      >
        {deletingUnit && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingUnit.name}</strong> (
              {deletingUnit.symbol})? This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingUnit(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteUnit.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteUnit.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default UnitsOfMeasureManagement;

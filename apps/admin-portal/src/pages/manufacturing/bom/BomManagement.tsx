import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useBoms,
  useDeleteBom,
  useCopyBom,
} from '@/features/manufacturing/hooks';
import type { BillOfMaterials } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import { BomForm } from '@/components/forms/BomForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { Edit, Trash2, Copy, Eye, FileText } from 'lucide-react';

const BomManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingBom, setEditingBom] = useState<BillOfMaterials | null>(null);
  const [deletingBom, setDeletingBom] = useState<BillOfMaterials | null>(null);
  const [viewingBom, setViewingBom] = useState<BillOfMaterials | null>(null);
  const [copyingBom, setCopyingBom] = useState<BillOfMaterials | null>(null);
  const [companyFilter, setCompanyFilter] = useState<string>('');
  const [newBomName, setNewBomName] = useState('');

  const { data: allBoms = [], isLoading, error, refetch } = useBoms(companyFilter || undefined);
  const deleteBom = useDeleteBom();
  const copyBom = useCopyBom();

  const boms = useMemo(() => {
    return companyFilter
      ? allBoms.filter((b) => b.companyId === companyFilter)
      : allBoms;
  }, [allBoms, companyFilter]);

  const handleEdit = (bom: BillOfMaterials) => {
    setEditingBom(bom);
  };

  const handleDelete = (bom: BillOfMaterials) => {
    setDeletingBom(bom);
  };

  const handleView = (bom: BillOfMaterials) => {
    setViewingBom(bom);
  };

  const handleCopy = (bom: BillOfMaterials) => {
    setCopyingBom(bom);
    setNewBomName(`${bom.name} (Copy)`);
  };

  const handleDeleteConfirm = async () => {
    if (deletingBom) {
      try {
        await deleteBom.mutateAsync(deletingBom.id);
        setDeletingBom(null);
      } catch (error) {
        console.error('Failed to delete BOM:', error);
      }
    }
  };

  const handleCopyConfirm = async () => {
    if (copyingBom && newBomName.trim()) {
      try {
        await copyBom.mutateAsync({
          id: copyingBom.id,
          data: { newName: newBomName.trim() },
        });
        setCopyingBom(null);
        setNewBomName('');
      } catch (error) {
        console.error('Failed to copy BOM:', error);
      }
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingBom(null);
    refetch();
  };

  const columns: ColumnDef<BillOfMaterials>[] = [
    {
      accessorKey: 'name',
      header: 'BOM',
      cell: ({ row }) => {
        const bom = row.original;
        return (
          <div>
            <div className="font-medium text-gray-900">{bom.name}</div>
            {bom.description && (
              <div className="text-sm text-gray-500 truncate max-w-xs">{bom.description}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'finishedGoodName',
      header: 'Finished Good',
      cell: ({ row }) => (
        <div className="text-gray-900">{row.original.finishedGoodName || '-'}</div>
      ),
    },
    {
      accessorKey: 'outputQuantity',
      header: 'Output Qty',
      cell: ({ row }) => (
        <div className="text-gray-900">{row.original.outputQuantity}</div>
      ),
    },
    {
      accessorKey: 'items',
      header: 'Components',
      cell: ({ row }) => {
        const itemCount = row.original.items?.length || 0;
        return (
          <div className="text-gray-900">{itemCount} items</div>
        );
      },
    },
    {
      accessorKey: 'effectiveFrom',
      header: 'Effective',
      cell: ({ row }) => {
        const bom = row.original;
        return (
          <div className="text-sm">
            {bom.effectiveFrom && (
              <div className="text-gray-900">
                From: {new Date(bom.effectiveFrom).toLocaleDateString()}
              </div>
            )}
            {bom.effectiveTo && (
              <div className="text-gray-500">
                To: {new Date(bom.effectiveTo).toLocaleDateString()}
              </div>
            )}
          </div>
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
        const bom = row.original;
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleView(bom)}
              className="text-gray-600 hover:text-gray-800 p-1 rounded hover:bg-gray-50 transition-colors"
              title="View details"
            >
              <Eye size={16} />
            </button>
            <button
              onClick={() => handleCopy(bom)}
              className="text-purple-600 hover:text-purple-800 p-1 rounded hover:bg-purple-50 transition-colors"
              title="Copy BOM"
            >
              <Copy size={16} />
            </button>
            <button
              onClick={() => handleEdit(bom)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit BOM"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(bom)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete BOM"
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
        <div className="text-red-600 mb-4">Failed to load BOMs</div>
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
        <h1 className="text-3xl font-bold text-gray-900">Bill of Materials</h1>
        <p className="text-gray-600 mt-2">Manage product recipes and component lists</p>
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
            data={boms}
            searchPlaceholder="Search BOMs..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Create BOM"
          />
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Bill of Materials"
        size="xl"
      >
        <BomForm
          companyId={companyFilter || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingBom}
        onClose={() => setEditingBom(null)}
        title="Edit Bill of Materials"
        size="xl"
      >
        {editingBom && (
          <BomForm
            bom={editingBom}
            companyId={editingBom.companyId}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingBom(null)}
          />
        )}
      </Drawer>

      <Drawer
        isOpen={!!viewingBom}
        onClose={() => setViewingBom(null)}
        title="BOM Details"
        size="lg"
      >
        {viewingBom && (
          <div className="space-y-6">
            <div className="flex items-center gap-4">
              <div className="h-16 w-16 rounded-lg bg-blue-50 flex items-center justify-center">
                <FileText className="h-8 w-8 text-blue-600" />
              </div>
              <div>
                <h3 className="text-xl font-semibold text-gray-900">{viewingBom.name}</h3>
                <p className="text-sm text-gray-500">{viewingBom.finishedGoodName}</p>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="text-sm text-gray-500">Output Quantity</div>
                <div className="text-2xl font-bold text-gray-900">{viewingBom.outputQuantity}</div>
              </div>
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="text-sm text-gray-500">Components</div>
                <div className="text-2xl font-bold text-gray-900">{viewingBom.items?.length || 0}</div>
              </div>
            </div>

            {viewingBom.description && (
              <div>
                <h4 className="text-sm font-medium text-gray-700 mb-1">Description</h4>
                <p className="text-gray-600">{viewingBom.description}</p>
              </div>
            )}

            <div>
              <h4 className="text-sm font-medium text-gray-700 mb-3">Components</h4>
              <div className="border rounded-lg divide-y">
                {viewingBom.items?.map((item, index) => (
                  <div key={index} className="p-3 flex justify-between items-center">
                    <div>
                      <div className="font-medium text-gray-900">
                        {item.componentItemName || item.componentItemId}
                      </div>
                      {item.notes && (
                        <div className="text-sm text-gray-500">{item.notes}</div>
                      )}
                    </div>
                    <div className="text-right">
                      <div className="font-medium text-gray-900">{item.quantity}</div>
                      {(item.scrapPercentage ?? 0) > 0 && (
                        <div className="text-xs text-gray-500">+{item.scrapPercentage}% scrap</div>
                      )}
                      {item.isOptional && (
                        <div className="text-xs text-blue-600">Optional</div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="flex justify-end pt-4 border-t">
              <button
                onClick={() => setViewingBom(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Close
              </button>
            </div>
          </div>
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingBom}
        onClose={() => setDeletingBom(null)}
        title="Delete BOM"
        size="sm"
      >
        {deletingBom && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingBom.name}</strong>? This action
              cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingBom(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteBom.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteBom.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        isOpen={!!copyingBom}
        onClose={() => setCopyingBom(null)}
        title="Copy BOM"
        size="sm"
      >
        {copyingBom && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Create a copy of <strong>{copyingBom.name}</strong>
            </p>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                New BOM Name
              </label>
              <input
                type="text"
                value={newBomName}
                onChange={(e) => setNewBomName(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="Enter new BOM name"
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setCopyingBom(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleCopyConfirm}
                disabled={copyBom.isPending || !newBomName.trim()}
                className="px-4 py-2 text-sm font-medium text-white bg-primary border border-transparent rounded-md hover:bg-primary/90 disabled:opacity-50"
              >
                {copyBom.isPending ? 'Copying...' : 'Copy BOM'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default BomManagement;

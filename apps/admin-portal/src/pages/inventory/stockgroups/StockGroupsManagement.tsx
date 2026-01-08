import { useState, useMemo, useEffect } from 'react';
import {
  useStockGroupsHierarchy,
  useDeleteStockGroup,
} from '@/features/inventory/hooks';
import type { StockGroup } from '@/services/api/types';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import { StockGroupForm } from '@/components/forms/StockGroupForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { useCompanies } from '@/hooks/api/useCompanies';
import { cn } from '@/lib/utils';
import {
  ChevronDown,
  ChevronRight,
  FolderTree,
  Edit,
  Trash2,
  Plus,
  FolderOpen,
  Folder,
} from 'lucide-react';

interface StockGroupNodeProps {
  group: StockGroup;
  level: number;
  expandedNodes: Set<string>;
  toggleExpand: (id: string) => void;
  onEdit: (group: StockGroup) => void;
  onDelete: (group: StockGroup) => void;
  onAddChild: (parentId: string) => void;
}

function StockGroupNode({
  group,
  level,
  expandedNodes,
  toggleExpand,
  onEdit,
  onDelete,
  onAddChild,
}: StockGroupNodeProps) {
  const isExpanded = expandedNodes.has(group.id);
  const hasChildren = group.children && group.children.length > 0;

  return (
    <div className={cn('relative', level > 0 && 'ml-6 border-l border-gray-200 pl-4')}>
      <div className="group flex items-center gap-2 py-2">
        <button
          onClick={() => hasChildren && toggleExpand(group.id)}
          className={cn(
            'flex h-6 w-6 items-center justify-center rounded-md transition-colors',
            hasChildren ? 'hover:bg-gray-100 cursor-pointer' : 'cursor-default'
          )}
        >
          {hasChildren ? (
            isExpanded ? (
              <ChevronDown className="h-4 w-4 text-gray-500" />
            ) : (
              <ChevronRight className="h-4 w-4 text-gray-500" />
            )
          ) : (
            <div className="h-4 w-4" />
          )}
        </button>

        <div className="flex-1 flex items-center gap-3 rounded-lg border bg-white p-3 shadow-sm transition-shadow hover:shadow-md">
          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-blue-50">
            {hasChildren || isExpanded ? (
              <FolderOpen className="h-4 w-4 text-blue-600" />
            ) : (
              <Folder className="h-4 w-4 text-blue-600" />
            )}
          </div>
          <div className="flex-1">
            <div className="flex items-center gap-2">
              <span className="font-medium text-gray-900">{group.name}</span>
              {!group.isActive && (
                <span className="text-xs bg-gray-100 text-gray-600 px-1.5 py-0.5 rounded">
                  Inactive
                </span>
              )}
            </div>
            {group.fullPath && group.fullPath !== group.name && (
              <div className="text-xs text-gray-500">{group.fullPath}</div>
            )}
          </div>
          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <button
              onClick={() => onAddChild(group.id)}
              className="p-1.5 text-green-600 hover:text-green-800 hover:bg-green-50 rounded transition-colors"
              title="Add sub-group"
            >
              <Plus size={14} />
            </button>
            <button
              onClick={() => onEdit(group)}
              className="p-1.5 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded transition-colors"
              title="Edit group"
            >
              <Edit size={14} />
            </button>
            <button
              onClick={() => onDelete(group)}
              className="p-1.5 text-red-600 hover:text-red-800 hover:bg-red-50 rounded transition-colors"
              title="Delete group"
            >
              <Trash2 size={14} />
            </button>
          </div>
        </div>
      </div>

      {hasChildren && isExpanded && (
        <div className="mt-1">
          {group.children!.map((child) => (
            <StockGroupNode
              key={child.id}
              group={child}
              level={level + 1}
              expandedNodes={expandedNodes}
              toggleExpand={toggleExpand}
              onEdit={onEdit}
              onDelete={onDelete}
              onAddChild={onAddChild}
            />
          ))}
        </div>
      )}
    </div>
  );
}

const StockGroupsManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingGroup, setEditingGroup] = useState<StockGroup | null>(null);
  const [deletingGroup, setDeletingGroup] = useState<StockGroup | null>(null);
  const [parentIdForNew, setParentIdForNew] = useState<string | undefined>(undefined);
  const [companyFilter, setCompanyFilter] = useState<string>('');
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set());

  const { data: companies = [] } = useCompanies();
  const {
    data: stockGroups = [],
    isLoading,
    error,
    refetch,
  } = useStockGroupsHierarchy(companyFilter || undefined);
  const deleteStockGroup = useDeleteStockGroup();

  useEffect(() => {
    if (companies.length > 0 && !companyFilter) {
      setCompanyFilter(companies[0].id);
    }
  }, [companies, companyFilter]);

  const toggleExpand = (id: string) => {
    setExpandedNodes((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const expandAll = () => {
    const allIds = new Set<string>();
    const collectIds = (groups: StockGroup[]) => {
      groups.forEach((group) => {
        allIds.add(group.id);
        if (group.children) {
          collectIds(group.children);
        }
      });
    };
    collectIds(stockGroups);
    setExpandedNodes(allIds);
  };

  const collapseAll = () => {
    setExpandedNodes(new Set());
  };

  const handleEdit = (group: StockGroup) => {
    setEditingGroup(group);
  };

  const handleDelete = (group: StockGroup) => {
    setDeletingGroup(group);
  };

  const handleAddChild = (parentId: string) => {
    setParentIdForNew(parentId);
    setIsCreateDrawerOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (deletingGroup) {
      try {
        await deleteStockGroup.mutateAsync(deletingGroup.id);
        setDeletingGroup(null);
      } catch (error) {
        console.error('Failed to delete stock group:', error);
      }
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingGroup(null);
    setParentIdForNew(undefined);
    refetch();
  };

  const handleCreateNew = () => {
    setParentIdForNew(undefined);
    setIsCreateDrawerOpen(true);
  };

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
        <div className="text-red-600 mb-4">Failed to load stock groups</div>
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
        <h1 className="text-3xl font-bold text-gray-900">Stock Groups</h1>
        <p className="text-gray-600 mt-2">Organize your inventory with hierarchical categories</p>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="mb-4 flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
                <CompanyFilterDropdown value={companyFilter} onChange={setCompanyFilter} />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={expandAll}
                className="px-3 py-1.5 text-sm text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-md transition-colors"
              >
                Expand All
              </button>
              <button
                onClick={collapseAll}
                className="px-3 py-1.5 text-sm text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-md transition-colors"
              >
                Collapse All
              </button>
              <button
                onClick={handleCreateNew}
                className="px-4 py-2 text-sm font-medium text-white bg-primary rounded-md hover:bg-primary/90 flex items-center gap-2"
              >
                <Plus size={16} />
                Add Stock Group
              </button>
            </div>
          </div>

          {stockGroups.length === 0 ? (
            <div className="text-center py-12">
              <FolderTree className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No stock groups yet</h3>
              <p className="text-gray-600 mb-4">
                Create your first stock group to organize your inventory
              </p>
              <button
                onClick={handleCreateNew}
                className="px-4 py-2 text-sm font-medium text-white bg-primary rounded-md hover:bg-primary/90"
              >
                Create Stock Group
              </button>
            </div>
          ) : (
            <div className="space-y-1">
              {stockGroups.map((group) => (
                <StockGroupNode
                  key={group.id}
                  group={group}
                  level={0}
                  expandedNodes={expandedNodes}
                  toggleExpand={toggleExpand}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onAddChild={handleAddChild}
                />
              ))}
            </div>
          )}
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => {
          setIsCreateDrawerOpen(false);
          setParentIdForNew(undefined);
        }}
        title={parentIdForNew ? 'Create Sub-Group' : 'Create Stock Group'}
        size="md"
      >
        <StockGroupForm
          companyId={companyFilter || undefined}
          parentId={parentIdForNew}
          onSuccess={handleFormSuccess}
          onCancel={() => {
            setIsCreateDrawerOpen(false);
            setParentIdForNew(undefined);
          }}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingGroup}
        onClose={() => setEditingGroup(null)}
        title="Edit Stock Group"
        size="md"
      >
        {editingGroup && (
          <StockGroupForm
            stockGroup={editingGroup}
            companyId={editingGroup.companyId}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingGroup(null)}
          />
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingGroup}
        onClose={() => setDeletingGroup(null)}
        title="Delete Stock Group"
        size="sm"
      >
        {deletingGroup && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingGroup.name}</strong>?
              {deletingGroup.children && deletingGroup.children.length > 0 && (
                <span className="text-red-600">
                  {' '}
                  This will also delete all sub-groups.
                </span>
              )}
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingGroup(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteStockGroup.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteStockGroup.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default StockGroupsManagement;

import { useState, useMemo } from 'react';
import { useTags, useDeleteTag, useTagTree, useSeedDefaultTags } from '@/features/tags/hooks';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { Tag, TagGroup } from '@/services/api/types';
import { Drawer } from '@/components/ui/Drawer';
import { Modal } from '@/components/ui/Modal';
import { TagForm } from '@/components/forms/TagForm';
import {
  Tags,
  Plus,
  Edit,
  Trash2,
  ChevronRight,
  ChevronDown,
  Building2,
  FolderKanban,
  Users,
  MapPin,
  Wallet,
  Sparkles,
  RefreshCw,
} from 'lucide-react';
import { Link } from 'react-router-dom';

const TAG_GROUP_CONFIG: Record<TagGroup, { label: string; icon: React.ElementType; color: string }> = {
  department: { label: 'Departments', icon: Building2, color: 'blue' },
  project: { label: 'Projects', icon: FolderKanban, color: 'purple' },
  client: { label: 'Clients', icon: Users, color: 'green' },
  region: { label: 'Regions', icon: MapPin, color: 'orange' },
  cost_center: { label: 'Cost Centers', icon: Wallet, color: 'pink' },
  custom: { label: 'Custom Tags', icon: Tags, color: 'gray' },
};

const TagTreeItem = ({
  tag,
  level = 0,
  onEdit,
  onDelete,
  onAddChild,
  expandedIds,
  toggleExpanded,
}: {
  tag: Tag;
  level?: number;
  onEdit: (tag: Tag) => void;
  onDelete: (tag: Tag) => void;
  onAddChild: (parentTag: Tag) => void;
  expandedIds: Set<string>;
  toggleExpanded: (id: string) => void;
}) => {
  const hasChildren = tag.children && tag.children.length > 0;
  const isExpanded = expandedIds.has(tag.id);

  return (
    <div>
      <div
        className={`flex items-center py-2 px-3 hover:bg-gray-50 rounded-lg group ${
          level > 0 ? 'ml-6' : ''
        }`}
      >
        {/* Expand/Collapse Button */}
        <button
          onClick={() => hasChildren && toggleExpanded(tag.id)}
          className={`w-6 h-6 flex items-center justify-center mr-2 ${
            hasChildren ? 'cursor-pointer text-gray-500 hover:text-gray-700' : 'cursor-default'
          }`}
          disabled={!hasChildren}
        >
          {hasChildren ? (
            isExpanded ? (
              <ChevronDown size={16} />
            ) : (
              <ChevronRight size={16} />
            )
          ) : (
            <span className="w-4" />
          )}
        </button>

        {/* Color Indicator */}
        <div
          className="w-3 h-3 rounded-full mr-3 flex-shrink-0"
          style={{ backgroundColor: tag.color || '#6B7280' }}
        />

        {/* Tag Name */}
        <div className="flex-1 min-w-0">
          <div className="font-medium text-gray-900 truncate">{tag.name}</div>
          {tag.code && (
            <div className="text-xs text-gray-500 font-mono">{tag.code}</div>
          )}
        </div>

        {/* Stats */}
        {tag.transactionCount !== undefined && tag.transactionCount > 0 && (
          <div className="text-xs text-gray-500 mr-4">
            {tag.transactionCount} transactions
          </div>
        )}

        {/* Budget */}
        {tag.budgetAmount && (
          <div className="text-xs text-gray-500 mr-4">
            Budget: ₹{tag.budgetAmount.toLocaleString('en-IN')}
          </div>
        )}

        {/* Status */}
        {!tag.isActive && (
          <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-gray-100 text-gray-600 mr-2">
            Inactive
          </span>
        )}

        {/* Actions */}
        <div className="flex items-center space-x-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button
            onClick={() => onAddChild(tag)}
            className="p-1 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded"
            title="Add child tag"
          >
            <Plus size={14} />
          </button>
          <button
            onClick={() => onEdit(tag)}
            className="p-1 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded"
            title="Edit tag"
          >
            <Edit size={14} />
          </button>
          <button
            onClick={() => onDelete(tag)}
            className="p-1 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded"
            title="Delete tag"
          >
            <Trash2 size={14} />
          </button>
        </div>
      </div>

      {/* Children */}
      {hasChildren && isExpanded && (
        <div className="border-l-2 border-gray-100 ml-6">
          {tag.children!.map((child) => (
            <TagTreeItem
              key={child.id}
              tag={child}
              level={level + 1}
              onEdit={onEdit}
              onDelete={onDelete}
              onAddChild={onAddChild}
              expandedIds={expandedIds}
              toggleExpanded={toggleExpanded}
            />
          ))}
        </div>
      )}
    </div>
  );
};

const TagsManagement = () => {
  const { selectedCompanyId } = useCompanyContext();
  const [activeGroup, setActiveGroup] = useState<TagGroup>('department');
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingTag, setEditingTag] = useState<Tag | null>(null);
  const [deletingTag, setDeletingTag] = useState<Tag | null>(null);
  const [parentTagForCreate, setParentTagForCreate] = useState<Tag | null>(null);
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());

  const { data: allTags = [], isLoading, error, refetch } = useTags(selectedCompanyId || undefined);
  const deleteTag = useDeleteTag();
  const seedDefaults = useSeedDefaultTags();

  // Filter tags by group and build tree
  const filteredTags = useMemo(() => {
    return allTags.filter((tag) => tag.tagGroup === activeGroup);
  }, [allTags, activeGroup]);

  const tagTree = useTagTree(filteredTags);

  // Count tags per group
  const groupCounts = useMemo(() => {
    const counts: Record<TagGroup, number> = {
      department: 0,
      project: 0,
      client: 0,
      region: 0,
      cost_center: 0,
      custom: 0,
    };
    allTags.forEach((tag) => {
      if (counts[tag.tagGroup] !== undefined) {
        counts[tag.tagGroup]++;
      }
    });
    return counts;
  }, [allTags]);

  const toggleExpanded = (id: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const handleEdit = (tag: Tag) => {
    setEditingTag(tag);
  };

  const handleDelete = (tag: Tag) => {
    setDeletingTag(tag);
  };

  const handleAddChild = (parentTag: Tag) => {
    setParentTagForCreate(parentTag);
    setIsCreateDrawerOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (deletingTag) {
      try {
        await deleteTag.mutateAsync(deletingTag.id);
        setDeletingTag(null);
      } catch (error) {
        console.error('Failed to delete tag:', error);
      }
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingTag(null);
    setParentTagForCreate(null);
    refetch();
  };

  const handleSeedDefaults = async () => {
    if (selectedCompanyId) {
      try {
        await seedDefaults.mutateAsync(selectedCompanyId);
        refetch();
      } catch (error) {
        console.error('Failed to seed default tags:', error);
      }
    }
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
        <div className="text-red-600 mb-4">Failed to load tags</div>
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
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Tags</h1>
          <p className="text-gray-600 mt-2">
            Organize transactions with flexible labels for multi-dimensional analysis
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Link
            to="/settings/attribution-rules"
            className="inline-flex items-center px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            <Sparkles size={16} className="mr-2" />
            Auto-Attribution Rules
          </Link>
          <button
            onClick={() => {
              setParentTagForCreate(null);
              setIsCreateDrawerOpen(true);
            }}
            className="inline-flex items-center px-4 py-2 text-sm font-medium text-white bg-primary rounded-md hover:bg-primary/90"
          >
            <Plus size={16} className="mr-2" />
            Add Tag
          </button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
        {(Object.entries(TAG_GROUP_CONFIG) as [TagGroup, typeof TAG_GROUP_CONFIG[TagGroup]][]).map(
          ([group, config]) => {
            const Icon = config.icon;
            const isActive = activeGroup === group;
            const count = groupCounts[group];

            return (
              <button
                key={group}
                onClick={() => setActiveGroup(group)}
                className={`p-4 rounded-lg border-2 transition-all ${
                  isActive
                    ? `border-${config.color}-500 bg-${config.color}-50`
                    : 'border-gray-200 bg-white hover:border-gray-300'
                }`}
              >
                <div className="flex items-center">
                  <Icon
                    className={`h-5 w-5 ${
                      isActive ? `text-${config.color}-600` : 'text-gray-400'
                    }`}
                  />
                  <span
                    className={`ml-2 text-2xl font-bold ${
                      isActive ? `text-${config.color}-700` : 'text-gray-900'
                    }`}
                  >
                    {count}
                  </span>
                </div>
                <div
                  className={`text-sm font-medium mt-1 ${
                    isActive ? `text-${config.color}-700` : 'text-gray-600'
                  }`}
                >
                  {config.label}
                </div>
              </button>
            );
          }
        )}
      </div>

      {/* Tag List */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">
              {TAG_GROUP_CONFIG[activeGroup].label}
            </h2>
            {tagTree.length === 0 && (
              <button
                onClick={handleSeedDefaults}
                disabled={seedDefaults.isPending}
                className="inline-flex items-center px-3 py-1.5 text-sm font-medium text-blue-600 hover:text-blue-700 hover:bg-blue-50 rounded-md"
              >
                <RefreshCw
                  size={14}
                  className={`mr-1.5 ${seedDefaults.isPending ? 'animate-spin' : ''}`}
                />
                {seedDefaults.isPending ? 'Creating...' : 'Create Default Tags'}
              </button>
            )}
          </div>

          {tagTree.length === 0 ? (
            <div className="text-center py-12 text-gray-500">
              <Tags className="h-12 w-12 mx-auto mb-4 text-gray-400" />
              <p>No {TAG_GROUP_CONFIG[activeGroup].label.toLowerCase()} yet.</p>
              <p className="text-sm mt-1">
                Click "Add Tag" to create your first {activeGroup} tag.
              </p>
            </div>
          ) : (
            <div className="divide-y divide-gray-100">
              {tagTree.map((tag) => (
                <TagTreeItem
                  key={tag.id}
                  tag={tag}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onAddChild={handleAddChild}
                  expandedIds={expandedIds}
                  toggleExpanded={toggleExpanded}
                />
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Create Tag Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => {
          setIsCreateDrawerOpen(false);
          setParentTagForCreate(null);
        }}
        title={parentTagForCreate ? `Add Child Tag to "${parentTagForCreate.name}"` : 'Create New Tag'}
        size="lg"
      >
        <TagForm
          defaultTagGroup={activeGroup}
          parentTag={parentTagForCreate || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => {
            setIsCreateDrawerOpen(false);
            setParentTagForCreate(null);
          }}
        />
      </Drawer>

      {/* Edit Tag Drawer */}
      <Drawer
        isOpen={!!editingTag}
        onClose={() => setEditingTag(null)}
        title="Edit Tag"
        size="lg"
      >
        {editingTag && (
          <TagForm
            tag={editingTag}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingTag(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingTag}
        onClose={() => setDeletingTag(null)}
        title="Delete Tag"
        size="sm"
      >
        {deletingTag && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingTag.name}</strong>?
            </p>
            {deletingTag.children && deletingTag.children.length > 0 && (
              <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-md">
                <p className="text-sm text-yellow-800">
                  This tag has {deletingTag.children.length} child tag(s). They will become
                  root-level tags.
                </p>
              </div>
            )}
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingTag(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteTag.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteTag.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default TagsManagement;

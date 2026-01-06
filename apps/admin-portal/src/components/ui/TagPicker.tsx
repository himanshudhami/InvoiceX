import { useState, useMemo, useRef, useEffect } from 'react';
import { useTags } from '@/features/tags/hooks';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { Tag, TagGroup, TransactionTag, AllocationMethod } from '@/services/api/types';
import { cn } from '@/lib/utils';
import {
  Tags,
  X,
  ChevronDown,
  Search,
  Building2,
  FolderKanban,
  Users,
  MapPin,
  Wallet,
} from 'lucide-react';

interface SelectedTag {
  tagId: string;
  allocatedAmount?: number;
  allocationPercentage?: number;
}

interface TagPickerProps {
  value: SelectedTag[];
  onChange: (value: SelectedTag[]) => void;
  transactionAmount?: number;
  allocationMethod?: AllocationMethod;
  groupFilter?: TagGroup[];
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  showAllocation?: boolean;
  maxTags?: number;
}

const TAG_GROUP_ICONS: Record<TagGroup, React.ElementType> = {
  department: Building2,
  project: FolderKanban,
  client: Users,
  region: MapPin,
  cost_center: Wallet,
  custom: Tags,
};

const TAG_GROUP_LABELS: Record<TagGroup, string> = {
  department: 'Departments',
  project: 'Projects',
  client: 'Clients',
  region: 'Regions',
  cost_center: 'Cost Centers',
  custom: 'Custom',
};

export const TagPicker = ({
  value,
  onChange,
  transactionAmount,
  allocationMethod = 'full',
  groupFilter,
  placeholder = 'Select tags...',
  disabled = false,
  className,
  showAllocation = false,
  maxTags,
}: TagPickerProps) => {
  const { selectedCompanyId } = useCompanyContext();
  const { data: allTags = [], isLoading } = useTags(selectedCompanyId || undefined);
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [activeGroup, setActiveGroup] = useState<TagGroup | 'all'>('all');
  const containerRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Filter available tags
  const availableTags = useMemo(() => {
    let filtered = allTags.filter((t) => t.isActive);

    // Apply group filter
    if (groupFilter && groupFilter.length > 0) {
      filtered = filtered.filter((t) => groupFilter.includes(t.tagGroup));
    }

    // Apply active group filter
    if (activeGroup !== 'all') {
      filtered = filtered.filter((t) => t.tagGroup === activeGroup);
    }

    // Apply search filter
    if (search) {
      const searchLower = search.toLowerCase();
      filtered = filtered.filter(
        (t) =>
          t.name.toLowerCase().includes(searchLower) ||
          t.code?.toLowerCase().includes(searchLower) ||
          t.fullPath?.toLowerCase().includes(searchLower)
      );
    }

    // Exclude already selected tags
    const selectedIds = new Set(value.map((v) => v.tagId));
    filtered = filtered.filter((t) => !selectedIds.has(t.id));

    return filtered;
  }, [allTags, groupFilter, activeGroup, search, value]);

  // Group available tags
  const groupedTags = useMemo(() => {
    const groups: Partial<Record<TagGroup, Tag[]>> = {};
    availableTags.forEach((tag) => {
      if (!groups[tag.tagGroup]) {
        groups[tag.tagGroup] = [];
      }
      groups[tag.tagGroup]!.push(tag);
    });
    return groups;
  }, [availableTags]);

  // Get available groups for tabs
  const availableGroups = useMemo(() => {
    const groups = new Set<TagGroup>();
    allTags.filter((t) => t.isActive).forEach((t) => groups.add(t.tagGroup));
    if (groupFilter && groupFilter.length > 0) {
      return groupFilter.filter((g) => groups.has(g));
    }
    return Array.from(groups);
  }, [allTags, groupFilter]);

  // Get selected tags with full data
  const selectedTags = useMemo(() => {
    return value
      .map((v) => {
        const tag = allTags.find((t) => t.id === v.tagId);
        return tag ? { ...v, tag } : null;
      })
      .filter(Boolean) as (SelectedTag & { tag: Tag })[];
  }, [value, allTags]);

  const handleSelectTag = (tag: Tag) => {
    if (maxTags && value.length >= maxTags) return;

    const newValue: SelectedTag = {
      tagId: tag.id,
    };

    if (showAllocation && allocationMethod !== 'full') {
      if (allocationMethod === 'percentage') {
        newValue.allocationPercentage = 100;
      } else if (allocationMethod === 'amount') {
        newValue.allocatedAmount = transactionAmount || 0;
      }
    }

    onChange([...value, newValue]);
    setSearch('');
  };

  const handleRemoveTag = (tagId: string) => {
    onChange(value.filter((v) => v.tagId !== tagId));
  };

  const handleAllocationChange = (tagId: string, field: 'allocatedAmount' | 'allocationPercentage', val: number) => {
    onChange(
      value.map((v) =>
        v.tagId === tagId ? { ...v, [field]: val } : v
      )
    );
  };

  if (isLoading) {
    return (
      <div className={cn('px-3 py-2 border border-gray-300 rounded-md bg-gray-50', className)}>
        <span className="text-sm text-gray-400">Loading tags...</span>
      </div>
    );
  }

  return (
    <div ref={containerRef} className={cn('relative', className)}>
      {/* Selected Tags Display */}
      <div
        className={cn(
          'min-h-[42px] px-3 py-2 border rounded-md cursor-pointer transition-colors',
          isOpen ? 'border-primary ring-2 ring-primary/20' : 'border-gray-300 hover:border-gray-400',
          disabled && 'opacity-50 cursor-not-allowed bg-gray-50'
        )}
        onClick={() => !disabled && setIsOpen(!isOpen)}
      >
        {selectedTags.length === 0 ? (
          <div className="flex items-center justify-between text-gray-400">
            <span className="text-sm">{placeholder}</span>
            <ChevronDown size={16} className={cn('transition-transform', isOpen && 'rotate-180')} />
          </div>
        ) : (
          <div className="flex flex-wrap gap-1.5 items-center">
            {selectedTags.map(({ tag, tagId, allocatedAmount, allocationPercentage }) => (
              <div
                key={tagId}
                className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-sm bg-gray-100"
                onClick={(e) => e.stopPropagation()}
              >
                <div
                  className="w-2 h-2 rounded-full flex-shrink-0"
                  style={{ backgroundColor: tag.color || '#6B7280' }}
                />
                <span className="font-medium text-gray-700">{tag.name}</span>
                {showAllocation && allocationMethod === 'percentage' && (
                  <input
                    type="number"
                    min="0"
                    max="100"
                    value={allocationPercentage ?? 100}
                    onChange={(e) =>
                      handleAllocationChange(tagId, 'allocationPercentage', parseInt(e.target.value) || 0)
                    }
                    onClick={(e) => e.stopPropagation()}
                    className="w-12 px-1 py-0.5 text-xs border border-gray-200 rounded text-center"
                    disabled={disabled}
                  />
                )}
                {showAllocation && allocationMethod === 'amount' && (
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={allocatedAmount ?? ''}
                    onChange={(e) =>
                      handleAllocationChange(tagId, 'allocatedAmount', parseFloat(e.target.value) || 0)
                    }
                    onClick={(e) => e.stopPropagation()}
                    className="w-20 px-1 py-0.5 text-xs border border-gray-200 rounded text-right"
                    disabled={disabled}
                  />
                )}
                {!disabled && (
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleRemoveTag(tagId);
                    }}
                    className="text-gray-400 hover:text-gray-600"
                  >
                    <X size={14} />
                  </button>
                )}
              </div>
            ))}
            {(!maxTags || value.length < maxTags) && (
              <ChevronDown size={16} className={cn('text-gray-400 ml-auto transition-transform', isOpen && 'rotate-180')} />
            )}
          </div>
        )}
      </div>

      {/* Dropdown */}
      {isOpen && !disabled && (
        <div className="absolute z-50 w-full mt-1 bg-white border border-gray-200 rounded-md shadow-lg max-h-80 overflow-hidden">
          {/* Search */}
          <div className="p-2 border-b">
            <div className="relative">
              <Search size={16} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search tags..."
                className="w-full pl-8 pr-3 py-1.5 text-sm border border-gray-200 rounded focus:outline-none focus:ring-1 focus:ring-primary"
                autoFocus
              />
            </div>
          </div>

          {/* Group Tabs */}
          {availableGroups.length > 1 && (
            <div className="flex gap-1 p-2 border-b overflow-x-auto">
              <button
                type="button"
                onClick={() => setActiveGroup('all')}
                className={cn(
                  'px-2 py-1 text-xs font-medium rounded whitespace-nowrap',
                  activeGroup === 'all'
                    ? 'bg-primary text-white'
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                )}
              >
                All
              </button>
              {availableGroups.map((group) => {
                const Icon = TAG_GROUP_ICONS[group];
                return (
                  <button
                    key={group}
                    type="button"
                    onClick={() => setActiveGroup(group)}
                    className={cn(
                      'inline-flex items-center gap-1 px-2 py-1 text-xs font-medium rounded whitespace-nowrap',
                      activeGroup === group
                        ? 'bg-primary text-white'
                        : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                    )}
                  >
                    <Icon size={12} />
                    {TAG_GROUP_LABELS[group]}
                  </button>
                );
              })}
            </div>
          )}

          {/* Tags List */}
          <div className="max-h-48 overflow-y-auto p-1">
            {availableTags.length === 0 ? (
              <div className="text-center py-6 text-gray-500 text-sm">
                {search ? 'No matching tags found' : 'No more tags available'}
              </div>
            ) : activeGroup === 'all' ? (
              // Show grouped
              Object.entries(groupedTags).map(([group, tags]) => {
                const Icon = TAG_GROUP_ICONS[group as TagGroup];
                return (
                  <div key={group}>
                    <div className="px-2 py-1 text-xs font-medium text-gray-500 flex items-center gap-1">
                      <Icon size={12} />
                      {TAG_GROUP_LABELS[group as TagGroup]}
                    </div>
                    {tags!.map((tag) => (
                      <button
                        key={tag.id}
                        type="button"
                        onClick={() => handleSelectTag(tag)}
                        className="w-full flex items-center gap-2 px-2 py-1.5 text-sm text-left hover:bg-gray-50 rounded"
                      >
                        <div
                          className="w-3 h-3 rounded-full flex-shrink-0"
                          style={{ backgroundColor: tag.color || '#6B7280' }}
                        />
                        <span className="flex-1 truncate">{tag.fullPath || tag.name}</span>
                        {tag.code && (
                          <span className="text-xs text-gray-400 font-mono">{tag.code}</span>
                        )}
                      </button>
                    ))}
                  </div>
                );
              })
            ) : (
              // Show flat list for single group
              availableTags.map((tag) => (
                <button
                  key={tag.id}
                  type="button"
                  onClick={() => handleSelectTag(tag)}
                  className="w-full flex items-center gap-2 px-2 py-1.5 text-sm text-left hover:bg-gray-50 rounded"
                >
                  <div
                    className="w-3 h-3 rounded-full flex-shrink-0"
                    style={{ backgroundColor: tag.color || '#6B7280' }}
                  />
                  <span className="flex-1 truncate">{tag.fullPath || tag.name}</span>
                  {tag.code && (
                    <span className="text-xs text-gray-400 font-mono">{tag.code}</span>
                  )}
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};

// Simple display component for showing tags on a transaction
export const TagDisplay = ({
  tags,
  maxDisplay = 3,
  className,
}: {
  tags: TransactionTag[] | Tag[];
  maxDisplay?: number;
  className?: string;
}) => {
  if (!tags || tags.length === 0) {
    return null;
  }

  const displayTags = tags.slice(0, maxDisplay);
  const remainingCount = tags.length - maxDisplay;

  return (
    <div className={cn('flex flex-wrap gap-1', className)}>
      {displayTags.map((tag) => {
        const color = 'tagColor' in tag ? tag.tagColor : tag.color;
        const name = 'tagName' in tag ? tag.tagName : tag.name;
        const id = 'tagId' in tag ? tag.tagId : tag.id;

        return (
          <span
            key={id}
            className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-xs bg-gray-100"
          >
            <div
              className="w-2 h-2 rounded-full flex-shrink-0"
              style={{ backgroundColor: color || '#6B7280' }}
            />
            <span className="text-gray-700 truncate max-w-[100px]">{name}</span>
          </span>
        );
      })}
      {remainingCount > 0 && (
        <span className="inline-flex items-center px-1.5 py-0.5 rounded text-xs bg-gray-100 text-gray-500">
          +{remainingCount}
        </span>
      )}
    </div>
  );
};

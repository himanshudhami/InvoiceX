import { useState, useEffect } from 'react';
import { Tag, CreateTagDto, UpdateTagDto, TagGroup } from '@/services/api/types';
import { useCreateTag, useUpdateTag, useTags } from '@/features/tags/hooks';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { cn } from '@/lib/utils';

interface TagFormProps {
  tag?: Tag;
  defaultTagGroup?: TagGroup;
  parentTag?: Tag;
  onSuccess: () => void;
  onCancel: () => void;
}

const TAG_GROUP_OPTIONS: { value: TagGroup; label: string }[] = [
  { value: 'department', label: 'Department' },
  { value: 'project', label: 'Project' },
  { value: 'client', label: 'Client' },
  { value: 'region', label: 'Region' },
  { value: 'cost_center', label: 'Cost Center' },
  { value: 'custom', label: 'Custom' },
];

const DEFAULT_COLORS = [
  '#3B82F6', // Blue
  '#8B5CF6', // Purple
  '#10B981', // Green
  '#F59E0B', // Amber
  '#EF4444', // Red
  '#EC4899', // Pink
  '#6366F1', // Indigo
  '#14B8A6', // Teal
  '#F97316', // Orange
  '#6B7280', // Gray
];

export const TagForm = ({
  tag,
  defaultTagGroup = 'department',
  parentTag,
  onSuccess,
  onCancel,
}: TagFormProps) => {
  const { selectedCompanyId } = useCompanyContext();
  const [formData, setFormData] = useState<CreateTagDto>({
    companyId: selectedCompanyId || undefined,
    name: '',
    code: '',
    tagGroup: defaultTagGroup,
    description: '',
    parentTagId: parentTag?.id,
    color: DEFAULT_COLORS[0],
    icon: '',
    sortOrder: 0,
    budgetAmount: undefined,
    budgetPeriod: 'monthly',
    budgetYear: new Date().getFullYear().toString(),
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createTag = useCreateTag();
  const updateTag = useUpdateTag();
  const { data: allTags = [] } = useTags(selectedCompanyId || undefined);

  const isEditing = !!tag;
  const isLoading = createTag.isPending || updateTag.isPending;

  // Filter potential parent tags (same group, not self or descendants)
  const potentialParentTags = allTags.filter((t) => {
    if (t.tagGroup !== formData.tagGroup) return false;
    if (isEditing && t.id === tag.id) return false;
    // Prevent circular reference - don't allow descendants as parent
    if (isEditing && t.fullPath?.startsWith(tag.fullPath || '')) return false;
    return true;
  });

  // Populate form with existing tag data
  useEffect(() => {
    if (tag) {
      setFormData({
        companyId: tag.companyId,
        name: tag.name || '',
        code: tag.code || '',
        tagGroup: tag.tagGroup,
        description: tag.description || '',
        parentTagId: tag.parentTagId || undefined,
        color: tag.color || DEFAULT_COLORS[0],
        icon: tag.icon || '',
        sortOrder: tag.sortOrder || 0,
        budgetAmount: tag.budgetAmount || undefined,
        budgetPeriod: tag.budgetPeriod || 'monthly',
        budgetYear: tag.budgetYear || new Date().getFullYear().toString(),
      });
    } else if (parentTag) {
      setFormData((prev) => ({
        ...prev,
        tagGroup: parentTag.tagGroup,
        parentTagId: parentTag.id,
      }));
    }
  }, [tag, parentTag]);

  // Update companyId when it changes
  useEffect(() => {
    if (selectedCompanyId && !formData.companyId) {
      setFormData((prev) => ({ ...prev, companyId: selectedCompanyId }));
    }
  }, [selectedCompanyId, formData.companyId]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name?.trim()) {
      newErrors.name = 'Tag name is required';
    }

    if (formData.name && formData.name.length > 100) {
      newErrors.name = 'Tag name must be 100 characters or less';
    }

    if (formData.code && formData.code.length > 20) {
      newErrors.code = 'Tag code must be 20 characters or less';
    }

    // Check for duplicate code in same company
    if (formData.code) {
      const duplicate = allTags.find(
        (t) => t.code === formData.code && t.id !== tag?.id
      );
      if (duplicate) {
        newErrors.code = 'This code is already used by another tag';
      }
    }

    if (formData.budgetAmount !== undefined && formData.budgetAmount < 0) {
      newErrors.budgetAmount = 'Budget amount cannot be negative';
    }

    if (formData.sortOrder !== undefined && formData.sortOrder < 0) {
      newErrors.sortOrder = 'Sort order cannot be negative';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      if (isEditing && tag) {
        const updateData: UpdateTagDto = {
          name: formData.name,
          code: formData.code || undefined,
          tagGroup: formData.tagGroup,
          description: formData.description || undefined,
          parentTagId: formData.parentTagId || undefined,
          color: formData.color || undefined,
          icon: formData.icon || undefined,
          sortOrder: formData.sortOrder,
          budgetAmount: formData.budgetAmount,
          budgetPeriod: formData.budgetPeriod,
          budgetYear: formData.budgetYear,
        };
        await updateTag.mutateAsync({ id: tag.id, data: updateData });
      } else {
        await createTag.mutateAsync(formData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (
    field: keyof CreateTagDto,
    value: string | number | undefined
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Tag Name */}
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
          Tag Name *
        </label>
        <input
          id="name"
          type="text"
          value={formData.name}
          onChange={(e) => handleChange('name', e.target.value)}
          className={cn(
            'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
            errors.name ? 'border-red-500' : 'border-gray-300'
          )}
          placeholder="Enter tag name"
          maxLength={100}
        />
        {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
      </div>

      {/* Code & Group */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="code" className="block text-sm font-medium text-gray-700 mb-1">
            Code
          </label>
          <input
            id="code"
            type="text"
            value={formData.code}
            onChange={(e) => handleChange('code', e.target.value.toUpperCase())}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring font-mono',
              errors.code ? 'border-red-500' : 'border-gray-300'
            )}
            placeholder="e.g., DEPT-001"
            maxLength={20}
          />
          {errors.code && <p className="text-red-500 text-sm mt-1">{errors.code}</p>}
        </div>
        <div>
          <label htmlFor="tagGroup" className="block text-sm font-medium text-gray-700 mb-1">
            Tag Group *
          </label>
          <select
            id="tagGroup"
            value={formData.tagGroup}
            onChange={(e) => handleChange('tagGroup', e.target.value as TagGroup)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            disabled={!!parentTag}
          >
            {TAG_GROUP_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Parent Tag */}
      <div>
        <label htmlFor="parentTagId" className="block text-sm font-medium text-gray-700 mb-1">
          Parent Tag
        </label>
        <select
          id="parentTagId"
          value={formData.parentTagId || ''}
          onChange={(e) => handleChange('parentTagId', e.target.value || undefined)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          disabled={!!parentTag}
        >
          <option value="">No parent (root level)</option>
          {potentialParentTags.map((t) => (
            <option key={t.id} value={t.id}>
              {t.fullPath || t.name}
            </option>
          ))}
        </select>
        {parentTag && (
          <p className="text-sm text-gray-500 mt-1">
            This tag will be a child of "{parentTag.name}"
          </p>
        )}
      </div>

      {/* Description */}
      <div>
        <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
          Description
        </label>
        <textarea
          id="description"
          rows={2}
          value={formData.description}
          onChange={(e) => handleChange('description', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Optional description for this tag..."
        />
      </div>

      {/* Color */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">Color</label>
        <div className="flex flex-wrap gap-2">
          {DEFAULT_COLORS.map((color) => (
            <button
              key={color}
              type="button"
              onClick={() => handleChange('color', color)}
              className={cn(
                'w-8 h-8 rounded-full border-2 transition-all',
                formData.color === color
                  ? 'border-gray-900 scale-110'
                  : 'border-transparent hover:scale-105'
              )}
              style={{ backgroundColor: color }}
              title={color}
            />
          ))}
          <div className="flex items-center gap-2 ml-2">
            <input
              type="color"
              value={formData.color || '#3B82F6'}
              onChange={(e) => handleChange('color', e.target.value)}
              className="w-8 h-8 rounded cursor-pointer"
              title="Custom color"
            />
            <span className="text-sm text-gray-500">Custom</span>
          </div>
        </div>
      </div>

      {/* Budget Section */}
      <div className="border-t pt-4 mt-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Budget (Optional)</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label htmlFor="budgetAmount" className="block text-sm font-medium text-gray-700 mb-1">
              Budget Amount
            </label>
            <input
              id="budgetAmount"
              type="number"
              step="0.01"
              min="0"
              value={formData.budgetAmount ?? ''}
              onChange={(e) =>
                handleChange(
                  'budgetAmount',
                  e.target.value ? parseFloat(e.target.value) : undefined
                )
              }
              className={cn(
                'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                errors.budgetAmount ? 'border-red-500' : 'border-gray-300'
              )}
              placeholder="0.00"
            />
            {errors.budgetAmount && (
              <p className="text-red-500 text-sm mt-1">{errors.budgetAmount}</p>
            )}
          </div>
          <div>
            <label htmlFor="budgetPeriod" className="block text-sm font-medium text-gray-700 mb-1">
              Period
            </label>
            <select
              id="budgetPeriod"
              value={formData.budgetPeriod || 'monthly'}
              onChange={(e) => handleChange('budgetPeriod', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="monthly">Monthly</option>
              <option value="quarterly">Quarterly</option>
              <option value="yearly">Yearly</option>
            </select>
          </div>
          <div>
            <label htmlFor="budgetYear" className="block text-sm font-medium text-gray-700 mb-1">
              Financial Year
            </label>
            <select
              id="budgetYear"
              value={formData.budgetYear || new Date().getFullYear().toString()}
              onChange={(e) => handleChange('budgetYear', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {Array.from({ length: 5 }, (_, i) => {
                const year = new Date().getFullYear() - 1 + i;
                return (
                  <option key={year} value={year.toString()}>
                    FY {year}-{(year + 1).toString().slice(-2)}
                  </option>
                );
              })}
            </select>
          </div>
        </div>
      </div>

      {/* Sort Order */}
      <div className="border-t pt-4 mt-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="sortOrder" className="block text-sm font-medium text-gray-700 mb-1">
              Sort Order
            </label>
            <input
              id="sortOrder"
              type="number"
              min="0"
              value={formData.sortOrder ?? 0}
              onChange={(e) => handleChange('sortOrder', parseInt(e.target.value) || 0)}
              className={cn(
                'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                errors.sortOrder ? 'border-red-500' : 'border-gray-300'
              )}
              placeholder="0"
            />
            {errors.sortOrder && (
              <p className="text-red-500 text-sm mt-1">{errors.sortOrder}</p>
            )}
            <p className="text-xs text-gray-500 mt-1">
              Lower numbers appear first. Tags with the same sort order are sorted alphabetically.
            </p>
          </div>
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-4 border-t mt-4">
        <button
          type="button"
          onClick={onCancel}
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary border border-transparent rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
        >
          {isLoading ? 'Saving...' : isEditing ? 'Update Tag' : 'Create Tag'}
        </button>
      </div>
    </form>
  );
};

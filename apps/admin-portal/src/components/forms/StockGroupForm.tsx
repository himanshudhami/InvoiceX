import { useState, useEffect } from 'react';
import type { StockGroup, CreateStockGroupDto, UpdateStockGroupDto } from '@/services/api/types';
import { useCreateStockGroup, useUpdateStockGroup, useStockGroups } from '@/features/inventory/hooks';
import { cn } from '@/lib/utils';

interface StockGroupFormProps {
  stockGroup?: StockGroup;
  companyId?: string;
  parentId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

export const StockGroupForm = ({
  stockGroup,
  companyId,
  parentId,
  onSuccess,
  onCancel,
}: StockGroupFormProps) => {
  const [formData, setFormData] = useState<CreateStockGroupDto>({
    companyId,
    name: '',
    parentStockGroupId: parentId,
    isActive: true,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createStockGroup = useCreateStockGroup();
  const updateStockGroup = useUpdateStockGroup();
  const { data: stockGroups = [] } = useStockGroups(companyId);

  const isEditing = !!stockGroup;
  const isLoading = createStockGroup.isPending || updateStockGroup.isPending;

  // Filter out current group and its descendants to prevent circular reference
  const getDescendantIds = (groupId: string): string[] => {
    const descendants: string[] = [];
    const children = stockGroups.filter((g) => g.parentStockGroupId === groupId);
    for (const child of children) {
      descendants.push(child.id);
      descendants.push(...getDescendantIds(child.id));
    }
    return descendants;
  };

  const excludeIds = stockGroup ? [stockGroup.id, ...getDescendantIds(stockGroup.id)] : [];
  const parentOptions = stockGroups.filter((g) => !excludeIds.includes(g.id));

  useEffect(() => {
    if (stockGroup) {
      setFormData({
        companyId: stockGroup.companyId,
        name: stockGroup.name || '',
        parentStockGroupId: stockGroup.parentStockGroupId || undefined,
        isActive: stockGroup.isActive ?? true,
      });
    }
  }, [stockGroup]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name?.trim()) {
      newErrors.name = 'Stock group name is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      if (isEditing && stockGroup) {
        const updateData: UpdateStockGroupDto = {
          name: formData.name,
          parentStockGroupId: formData.parentStockGroupId,
          isActive: formData.isActive,
        };
        await updateStockGroup.mutateAsync({ id: stockGroup.id, data: updateData });
      } else {
        await createStockGroup.mutateAsync(formData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (field: keyof CreateStockGroupDto, value: string | boolean | undefined) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Stock Group Name */}
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
          Stock Group Name *
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
          placeholder="Enter stock group name"
        />
        {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
      </div>

      {/* Parent Stock Group */}
      <div>
        <label htmlFor="parentStockGroupId" className="block text-sm font-medium text-gray-700 mb-1">
          Parent Stock Group
        </label>
        <select
          id="parentStockGroupId"
          value={formData.parentStockGroupId || ''}
          onChange={(e) => handleChange('parentStockGroupId', e.target.value || undefined)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
        >
          <option value="">None (Top Level)</option>
          {parentOptions.map((g) => (
            <option key={g.id} value={g.id}>
              {g.fullPath || g.name}
            </option>
          ))}
        </select>
        <p className="text-xs text-gray-500 mt-1">
          Select a parent group to create a sub-group hierarchy
        </p>
      </div>

      {/* Active Status */}
      <div className="flex items-center">
        <input
          id="isActive"
          type="checkbox"
          checked={formData.isActive}
          onChange={(e) => handleChange('isActive', e.target.checked)}
          className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
        />
        <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
          Stock group is active
        </label>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Stock Group' : 'Create Stock Group'}
        </button>
      </div>
    </form>
  );
};

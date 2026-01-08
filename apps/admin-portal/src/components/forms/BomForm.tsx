import { useState, useEffect } from 'react';
import type {
  BillOfMaterials,
  CreateBomDto,
  UpdateBomDto,
  BomItemDto,
} from '@/services/api/types';
import { useCreateBom, useUpdateBom } from '@/features/manufacturing/hooks';
import { useActiveStockItems } from '@/features/inventory/hooks';
import { cn } from '@/lib/utils';
import { Plus, Trash2 } from 'lucide-react';

interface BomFormProps {
  bom?: BillOfMaterials;
  companyId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

interface BomItemRow extends BomItemDto {
  _key: string;
}

export const BomForm = ({
  bom,
  companyId,
  onSuccess,
  onCancel,
}: BomFormProps) => {
  const [formData, setFormData] = useState({
    companyId,
    name: '',
    finishedGoodId: '',
    description: '',
    outputQuantity: 1,
    effectiveFrom: new Date().toISOString().split('T')[0],
    effectiveTo: '',
    isActive: true,
  });

  const [items, setItems] = useState<BomItemRow[]>([
    { _key: crypto.randomUUID(), componentItemId: '', quantity: 0, scrapPercentage: 0, isOptional: false, notes: '' },
  ]);

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createBom = useCreateBom();
  const updateBom = useUpdateBom();
  const { data: stockItems = [] } = useActiveStockItems(companyId);

  const isEditing = !!bom;
  const isLoading = createBom.isPending || updateBom.isPending;

  useEffect(() => {
    if (bom) {
      setFormData({
        companyId: bom.companyId,
        name: bom.name || '',
        finishedGoodId: bom.finishedGoodId,
        description: bom.description || '',
        outputQuantity: bom.outputQuantity || 1,
        effectiveFrom: bom.effectiveFrom || '',
        effectiveTo: bom.effectiveTo || '',
        isActive: bom.isActive ?? true,
      });
      if (bom.items && bom.items.length > 0) {
        setItems(
          bom.items.map((item) => ({
            _key: item.id || crypto.randomUUID(),
            componentItemId: item.componentItemId,
            quantity: item.quantity,
            scrapPercentage: item.scrapPercentage || 0,
            isOptional: item.isOptional || false,
            notes: item.notes || '',
          }))
        );
      }
    }
  }, [bom]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name?.trim()) {
      newErrors.name = 'BOM name is required';
    }

    if (!formData.finishedGoodId) {
      newErrors.finishedGoodId = 'Finished good is required';
    }

    if (formData.outputQuantity <= 0) {
      newErrors.outputQuantity = 'Output quantity must be greater than 0';
    }

    // Validate items
    const validItems = items.filter((item) => item.componentItemId && item.quantity > 0);
    if (validItems.length === 0) {
      newErrors.items = 'At least one component with quantity is required';
    }

    // Check for duplicate components
    const componentIds = validItems.map((i) => i.componentItemId);
    if (new Set(componentIds).size !== componentIds.length) {
      newErrors.items = 'Duplicate components are not allowed';
    }

    // Check that finished good is not in components
    if (validItems.some((i) => i.componentItemId === formData.finishedGoodId)) {
      newErrors.items = 'Finished good cannot be a component of itself';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      const validItems = items
        .filter((item) => item.componentItemId && item.quantity > 0)
        .map(({ _key, ...item }) => item);

      if (isEditing && bom) {
        const updateData: UpdateBomDto = {
          name: formData.name,
          description: formData.description,
          outputQuantity: formData.outputQuantity,
          effectiveFrom: formData.effectiveFrom || undefined,
          effectiveTo: formData.effectiveTo || undefined,
          isActive: formData.isActive,
          items: validItems,
        };
        await updateBom.mutateAsync({ id: bom.id, data: updateData });
      } else {
        const createData: CreateBomDto = {
          companyId: formData.companyId,
          name: formData.name,
          finishedGoodId: formData.finishedGoodId,
          description: formData.description,
          outputQuantity: formData.outputQuantity,
          effectiveFrom: formData.effectiveFrom || undefined,
          effectiveTo: formData.effectiveTo || undefined,
          isActive: formData.isActive,
          items: validItems,
        };
        await createBom.mutateAsync(createData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (field: keyof typeof formData, value: string | number | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  const handleItemChange = (
    key: string,
    field: keyof BomItemDto,
    value: string | number | boolean
  ) => {
    setItems((prev) =>
      prev.map((item) => (item._key === key ? { ...item, [field]: value } : item))
    );
    if (errors.items) {
      setErrors((prev) => ({ ...prev, items: '' }));
    }
  };

  const addItem = () => {
    setItems((prev) => [
      ...prev,
      { _key: crypto.randomUUID(), componentItemId: '', quantity: 0, scrapPercentage: 0, isOptional: false, notes: '' },
    ]);
  };

  const removeItem = (key: string) => {
    if (items.length > 1) {
      setItems((prev) => prev.filter((item) => item._key !== key));
    }
  };

  // Get available components (not already selected, not the finished good)
  const getAvailableComponents = (currentKey: string) => {
    const selectedIds = items.filter((i) => i._key !== currentKey).map((i) => i.componentItemId);
    return stockItems.filter(
      (item) => !selectedIds.includes(item.id) && item.id !== formData.finishedGoodId
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Basic Info */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
            BOM Name *
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
            placeholder="e.g., Computer Assembly v1"
          />
          {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
        </div>
        <div>
          <label htmlFor="finishedGoodId" className="block text-sm font-medium text-gray-700 mb-1">
            Finished Good *
          </label>
          <select
            id="finishedGoodId"
            value={formData.finishedGoodId}
            onChange={(e) => handleChange('finishedGoodId', e.target.value)}
            disabled={isEditing}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.finishedGoodId ? 'border-red-500' : 'border-gray-300',
              isEditing ? 'bg-gray-100' : ''
            )}
          >
            <option value="">Select finished good</option>
            {stockItems.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name} {item.sku ? `(${item.sku})` : ''}
              </option>
            ))}
          </select>
          {errors.finishedGoodId && <p className="text-red-500 text-sm mt-1">{errors.finishedGoodId}</p>}
        </div>
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
          placeholder="BOM description..."
        />
      </div>

      {/* Output and Dates */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div>
          <label htmlFor="outputQuantity" className="block text-sm font-medium text-gray-700 mb-1">
            Output Quantity *
          </label>
          <input
            id="outputQuantity"
            type="number"
            step="0.01"
            min="0.01"
            value={formData.outputQuantity}
            onChange={(e) => handleChange('outputQuantity', parseFloat(e.target.value) || 1)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.outputQuantity ? 'border-red-500' : 'border-gray-300'
            )}
          />
          {errors.outputQuantity && <p className="text-red-500 text-sm mt-1">{errors.outputQuantity}</p>}
        </div>
        <div>
          <label htmlFor="effectiveFrom" className="block text-sm font-medium text-gray-700 mb-1">
            Effective From
          </label>
          <input
            id="effectiveFrom"
            type="date"
            value={formData.effectiveFrom}
            onChange={(e) => handleChange('effectiveFrom', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
        <div>
          <label htmlFor="effectiveTo" className="block text-sm font-medium text-gray-700 mb-1">
            Effective To
          </label>
          <input
            id="effectiveTo"
            type="date"
            value={formData.effectiveTo}
            onChange={(e) => handleChange('effectiveTo', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
      </div>

      {/* BOM Items */}
      <div className="border-t border-gray-200 pt-4">
        <div className="flex justify-between items-center mb-3">
          <h3 className="text-sm font-medium text-gray-900">Components *</h3>
          <button
            type="button"
            onClick={addItem}
            className="inline-flex items-center px-2 py-1 text-xs font-medium text-primary bg-primary/10 rounded-md hover:bg-primary/20"
          >
            <Plus className="h-3 w-3 mr-1" />
            Add Component
          </button>
        </div>

        {errors.items && (
          <p className="text-red-500 text-sm mb-2">{errors.items}</p>
        )}

        <div className="space-y-3">
          {items.map((item, index) => (
            <div
              key={item._key}
              className="grid grid-cols-12 gap-2 items-start p-3 bg-gray-50 rounded-md"
            >
              <div className="col-span-4">
                <label className="block text-xs font-medium text-gray-500 mb-1">
                  Component {index + 1}
                </label>
                <select
                  value={item.componentItemId}
                  onChange={(e) => handleItemChange(item._key, 'componentItemId', e.target.value)}
                  className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-ring"
                >
                  <option value="">Select component</option>
                  {getAvailableComponents(item._key).map((si) => (
                    <option key={si.id} value={si.id}>
                      {si.name} {si.sku ? `(${si.sku})` : ''}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-500 mb-1">Qty</label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  value={item.quantity || ''}
                  onChange={(e) =>
                    handleItemChange(item._key, 'quantity', parseFloat(e.target.value) || 0)
                  }
                  className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-ring"
                  placeholder="0"
                />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-500 mb-1">Scrap %</label>
                <input
                  type="number"
                  step="0.1"
                  min="0"
                  max="100"
                  value={item.scrapPercentage || ''}
                  onChange={(e) =>
                    handleItemChange(item._key, 'scrapPercentage', parseFloat(e.target.value) || 0)
                  }
                  className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-ring"
                  placeholder="0"
                />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-500 mb-1">Optional</label>
                <input
                  type="checkbox"
                  checked={item.isOptional}
                  onChange={(e) => handleItemChange(item._key, 'isOptional', e.target.checked)}
                  className="mt-2 h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
                />
              </div>
              <div className="col-span-1 flex items-end pb-1">
                <button
                  type="button"
                  onClick={() => removeItem(item._key)}
                  disabled={items.length === 1}
                  className="p-1.5 text-gray-400 hover:text-red-500 disabled:opacity-30 disabled:hover:text-gray-400"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Active checkbox */}
      <div className="flex items-center">
        <input
          id="isActive"
          type="checkbox"
          checked={formData.isActive}
          onChange={(e) => handleChange('isActive', e.target.checked)}
          className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
        />
        <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
          BOM is active
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
          {isLoading ? 'Saving...' : isEditing ? 'Update BOM' : 'Create BOM'}
        </button>
      </div>
    </form>
  );
};

import { useState, useEffect } from 'react';
import type { StockItem, CreateStockItemDto, UpdateStockItemDto } from '@/services/api/types';
import { useCreateStockItem, useUpdateStockItem } from '@/features/inventory/hooks';
import { useActiveStockGroups, useUnitsOfMeasure } from '@/features/inventory/hooks';
import { cn } from '@/lib/utils';

interface StockItemFormProps {
  stockItem?: StockItem;
  companyId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

export const StockItemForm = ({ stockItem, companyId, onSuccess, onCancel }: StockItemFormProps) => {
  const [formData, setFormData] = useState<CreateStockItemDto>({
    companyId,
    name: '',
    sku: '',
    description: '',
    stockGroupId: undefined,
    baseUnitId: '',
    hsnSacCode: '',
    gstRate: 18,
    openingQuantity: 0,
    openingValue: 0,
    reorderLevel: undefined,
    reorderQuantity: undefined,
    minimumStock: undefined,
    maximumStock: undefined,
    costPrice: undefined,
    sellingPrice: undefined,
    mrp: undefined,
    isBatchEnabled: false,
    valuationMethod: 'weighted_avg',
    isActive: true,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createStockItem = useCreateStockItem();
  const updateStockItem = useUpdateStockItem();
  const { data: stockGroups = [] } = useActiveStockGroups(companyId);
  const { data: units = [] } = useUnitsOfMeasure(companyId);

  const isEditing = !!stockItem;
  const isLoading = createStockItem.isPending || updateStockItem.isPending;

  const gstRates = [
    { value: 0, label: '0% (Exempt)' },
    { value: 5, label: '5%' },
    { value: 12, label: '12%' },
    { value: 18, label: '18% (Standard)' },
    { value: 28, label: '28%' },
  ];

  const valuationMethods = [
    { value: 'weighted_avg', label: 'Weighted Average' },
    { value: 'fifo', label: 'FIFO (First In, First Out)' },
    { value: 'lifo', label: 'LIFO (Last In, First Out)' },
  ];

  useEffect(() => {
    if (stockItem) {
      setFormData({
        companyId: stockItem.companyId,
        name: stockItem.name || '',
        sku: stockItem.sku || '',
        description: stockItem.description || '',
        stockGroupId: stockItem.stockGroupId || undefined,
        baseUnitId: stockItem.baseUnitId || '',
        hsnSacCode: stockItem.hsnSacCode || '',
        gstRate: stockItem.gstRate ?? 18,
        openingQuantity: stockItem.openingQuantity || 0,
        openingValue: stockItem.openingValue || 0,
        reorderLevel: stockItem.reorderLevel,
        reorderQuantity: stockItem.reorderQuantity,
        minimumStock: stockItem.minimumStock,
        maximumStock: stockItem.maximumStock,
        costPrice: stockItem.costPrice,
        sellingPrice: stockItem.sellingPrice,
        mrp: stockItem.mrp,
        isBatchEnabled: stockItem.isBatchEnabled || false,
        valuationMethod: stockItem.valuationMethod || 'weighted_avg',
        isActive: stockItem.isActive ?? true,
      });
    }
  }, [stockItem]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name?.trim()) {
      newErrors.name = 'Stock item name is required';
    }

    if (!formData.baseUnitId) {
      newErrors.baseUnitId = 'Base unit is required';
    }

    if (formData.gstRate !== undefined && (formData.gstRate < 0 || formData.gstRate > 100)) {
      newErrors.gstRate = 'GST rate must be between 0 and 100';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      if (isEditing && stockItem) {
        const updateData: UpdateStockItemDto = {
          name: formData.name,
          sku: formData.sku,
          description: formData.description,
          stockGroupId: formData.stockGroupId,
          baseUnitId: formData.baseUnitId,
          hsnSacCode: formData.hsnSacCode,
          gstRate: formData.gstRate,
          reorderLevel: formData.reorderLevel,
          reorderQuantity: formData.reorderQuantity,
          minimumStock: formData.minimumStock,
          maximumStock: formData.maximumStock,
          costPrice: formData.costPrice,
          sellingPrice: formData.sellingPrice,
          mrp: formData.mrp,
          isBatchEnabled: formData.isBatchEnabled,
          valuationMethod: formData.valuationMethod,
          isActive: formData.isActive,
        };
        await updateStockItem.mutateAsync({ id: stockItem.id, data: updateData });
      } else {
        await createStockItem.mutateAsync(formData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (
    field: keyof CreateStockItemDto,
    value: string | number | boolean | undefined
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Basic Info */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
            Item Name *
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
            placeholder="Enter item name"
          />
          {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
        </div>
        <div>
          <label htmlFor="sku" className="block text-sm font-medium text-gray-700 mb-1">
            SKU
          </label>
          <input
            id="sku"
            type="text"
            value={formData.sku || ''}
            onChange={(e) => handleChange('sku', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="SKU-001"
          />
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
          value={formData.description || ''}
          onChange={(e) => handleChange('description', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Item description..."
        />
      </div>

      {/* Stock Group and Unit */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="stockGroupId" className="block text-sm font-medium text-gray-700 mb-1">
            Stock Group
          </label>
          <select
            id="stockGroupId"
            value={formData.stockGroupId || ''}
            onChange={(e) => handleChange('stockGroupId', e.target.value || undefined)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">Select stock group</option>
            {stockGroups.map((g) => (
              <option key={g.id} value={g.id}>
                {g.fullPath || g.name}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="baseUnitId" className="block text-sm font-medium text-gray-700 mb-1">
            Base Unit *
          </label>
          <select
            id="baseUnitId"
            value={formData.baseUnitId}
            onChange={(e) => handleChange('baseUnitId', e.target.value)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.baseUnitId ? 'border-red-500' : 'border-gray-300'
            )}
          >
            <option value="">Select unit</option>
            {units.map((u) => (
              <option key={u.id} value={u.id}>
                {u.name} ({u.symbol})
              </option>
            ))}
          </select>
          {errors.baseUnitId && <p className="text-red-500 text-sm mt-1">{errors.baseUnitId}</p>}
        </div>
      </div>

      {/* Tax Info */}
      <div className="border-t border-gray-200 pt-4">
        <h3 className="text-sm font-medium text-gray-900 mb-3">Tax Information</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="hsnSacCode" className="block text-sm font-medium text-gray-700 mb-1">
              HSN/SAC Code
            </label>
            <input
              id="hsnSacCode"
              type="text"
              value={formData.hsnSacCode || ''}
              onChange={(e) => handleChange('hsnSacCode', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="e.g., 8471"
            />
          </div>
          <div>
            <label htmlFor="gstRate" className="block text-sm font-medium text-gray-700 mb-1">
              GST Rate
            </label>
            <select
              id="gstRate"
              value={formData.gstRate}
              onChange={(e) => handleChange('gstRate', parseFloat(e.target.value))}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {gstRates.map((rate) => (
                <option key={rate.value} value={rate.value}>
                  {rate.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Pricing */}
      <div className="border-t border-gray-200 pt-4">
        <h3 className="text-sm font-medium text-gray-900 mb-3">Pricing</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label htmlFor="costPrice" className="block text-sm font-medium text-gray-700 mb-1">
              Cost Price
            </label>
            <input
              id="costPrice"
              type="number"
              step="0.01"
              min="0"
              value={formData.costPrice || ''}
              onChange={(e) => handleChange('costPrice', e.target.value ? parseFloat(e.target.value) : undefined)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="0.00"
            />
          </div>
          <div>
            <label htmlFor="sellingPrice" className="block text-sm font-medium text-gray-700 mb-1">
              Selling Price
            </label>
            <input
              id="sellingPrice"
              type="number"
              step="0.01"
              min="0"
              value={formData.sellingPrice || ''}
              onChange={(e) => handleChange('sellingPrice', e.target.value ? parseFloat(e.target.value) : undefined)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="0.00"
            />
          </div>
          <div>
            <label htmlFor="mrp" className="block text-sm font-medium text-gray-700 mb-1">
              MRP
            </label>
            <input
              id="mrp"
              type="number"
              step="0.01"
              min="0"
              value={formData.mrp || ''}
              onChange={(e) => handleChange('mrp', e.target.value ? parseFloat(e.target.value) : undefined)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="0.00"
            />
          </div>
        </div>
      </div>

      {/* Inventory Control */}
      <div className="border-t border-gray-200 pt-4">
        <h3 className="text-sm font-medium text-gray-900 mb-3">Inventory Control</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="reorderLevel" className="block text-sm font-medium text-gray-700 mb-1">
              Reorder Level
            </label>
            <input
              id="reorderLevel"
              type="number"
              step="0.01"
              min="0"
              value={formData.reorderLevel || ''}
              onChange={(e) => handleChange('reorderLevel', e.target.value ? parseFloat(e.target.value) : undefined)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Minimum stock before reorder"
            />
          </div>
          <div>
            <label htmlFor="reorderQuantity" className="block text-sm font-medium text-gray-700 mb-1">
              Reorder Quantity
            </label>
            <input
              id="reorderQuantity"
              type="number"
              step="0.01"
              min="0"
              value={formData.reorderQuantity || ''}
              onChange={(e) => handleChange('reorderQuantity', e.target.value ? parseFloat(e.target.value) : undefined)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Quantity to reorder"
            />
          </div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
          <div>
            <label htmlFor="valuationMethod" className="block text-sm font-medium text-gray-700 mb-1">
              Valuation Method
            </label>
            <select
              id="valuationMethod"
              value={formData.valuationMethod}
              onChange={(e) => handleChange('valuationMethod', e.target.value as 'fifo' | 'lifo' | 'weighted_avg')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {valuationMethods.map((method) => (
                <option key={method.value} value={method.value}>
                  {method.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Opening Balance (only for new items) */}
      {!isEditing && (
        <div className="border-t border-gray-200 pt-4">
          <h3 className="text-sm font-medium text-gray-900 mb-3">Opening Balance</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="openingQuantity" className="block text-sm font-medium text-gray-700 mb-1">
                Opening Quantity
              </label>
              <input
                id="openingQuantity"
                type="number"
                step="0.01"
                min="0"
                value={formData.openingQuantity || ''}
                onChange={(e) => handleChange('openingQuantity', parseFloat(e.target.value) || 0)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="0.00"
              />
            </div>
            <div>
              <label htmlFor="openingValue" className="block text-sm font-medium text-gray-700 mb-1">
                Opening Value
              </label>
              <input
                id="openingValue"
                type="number"
                step="0.01"
                min="0"
                value={formData.openingValue || ''}
                onChange={(e) => handleChange('openingValue', parseFloat(e.target.value) || 0)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="0.00"
              />
            </div>
          </div>
        </div>
      )}

      {/* Checkboxes */}
      <div className="flex flex-wrap gap-6">
        <div className="flex items-center">
          <input
            id="isBatchEnabled"
            type="checkbox"
            checked={formData.isBatchEnabled}
            onChange={(e) => handleChange('isBatchEnabled', e.target.checked)}
            className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
          />
          <label htmlFor="isBatchEnabled" className="ml-2 block text-sm text-gray-900">
            Enable batch/lot tracking
          </label>
        </div>
        <div className="flex items-center">
          <input
            id="isActive"
            type="checkbox"
            checked={formData.isActive}
            onChange={(e) => handleChange('isActive', e.target.checked)}
            className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
          />
          <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
            Item is active
          </label>
        </div>
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Item' : 'Create Item'}
        </button>
      </div>
    </form>
  );
};

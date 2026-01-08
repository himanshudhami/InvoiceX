import { useState, useEffect } from 'react';
import type { CreateStockMovementDto, MovementType } from '@/services/api/types';
import { useCreateStockMovement } from '@/features/inventory/hooks';
import { useActiveStockItems, useActiveWarehouses } from '@/features/inventory/hooks';
import { cn } from '@/lib/utils';

interface StockMovementFormProps {
  companyId?: string;
  defaultStockItemId?: string;
  defaultWarehouseId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

export const StockMovementForm = ({
  companyId,
  defaultStockItemId,
  defaultWarehouseId,
  onSuccess,
  onCancel,
}: StockMovementFormProps) => {
  const [formData, setFormData] = useState<CreateStockMovementDto>({
    companyId,
    stockItemId: defaultStockItemId || '',
    warehouseId: defaultWarehouseId || '',
    movementDate: new Date().toISOString().split('T')[0],
    movementType: 'adjustment',
    quantity: 0,
    rate: undefined,
    notes: '',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createMovement = useCreateStockMovement();
  const { data: stockItems = [] } = useActiveStockItems(companyId);
  const { data: warehouses = [] } = useActiveWarehouses(companyId);

  const isLoading = createMovement.isPending;

  const movementTypes: { value: MovementType; label: string; direction: 'in' | 'out' }[] = [
    { value: 'purchase', label: 'Purchase (In)', direction: 'in' },
    { value: 'sale', label: 'Sale (Out)', direction: 'out' },
    { value: 'adjustment', label: 'Adjustment', direction: 'in' },
    { value: 'opening', label: 'Opening Balance', direction: 'in' },
    { value: 'return_in', label: 'Sales Return (In)', direction: 'in' },
    { value: 'return_out', label: 'Purchase Return (Out)', direction: 'out' },
  ];

  const selectedMovementType = movementTypes.find((t) => t.value === formData.movementType);

  useEffect(() => {
    if (defaultStockItemId) {
      setFormData((prev) => ({ ...prev, stockItemId: defaultStockItemId }));
    }
    if (defaultWarehouseId) {
      setFormData((prev) => ({ ...prev, warehouseId: defaultWarehouseId }));
    }
  }, [defaultStockItemId, defaultWarehouseId]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.stockItemId) {
      newErrors.stockItemId = 'Stock item is required';
    }

    if (!formData.warehouseId) {
      newErrors.warehouseId = 'Warehouse is required';
    }

    if (!formData.movementDate) {
      newErrors.movementDate = 'Date is required';
    }

    if (!formData.quantity || formData.quantity <= 0) {
      newErrors.quantity = 'Quantity must be greater than 0';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      // Adjust quantity sign based on movement direction
      const isOutward = selectedMovementType?.direction === 'out';
      const adjustedData = {
        ...formData,
        quantity: isOutward ? -Math.abs(formData.quantity) : Math.abs(formData.quantity),
      };
      await createMovement.mutateAsync(adjustedData);
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (
    field: keyof CreateStockMovementDto,
    value: string | number | undefined
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Stock Item */}
      <div>
        <label htmlFor="stockItemId" className="block text-sm font-medium text-gray-700 mb-1">
          Stock Item *
        </label>
        <select
          id="stockItemId"
          value={formData.stockItemId}
          onChange={(e) => handleChange('stockItemId', e.target.value)}
          className={cn(
            'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
            errors.stockItemId ? 'border-red-500' : 'border-gray-300'
          )}
        >
          <option value="">Select stock item</option>
          {stockItems.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name} {item.sku ? `(${item.sku})` : ''}
            </option>
          ))}
        </select>
        {errors.stockItemId && <p className="text-red-500 text-sm mt-1">{errors.stockItemId}</p>}
      </div>

      {/* Warehouse */}
      <div>
        <label htmlFor="warehouseId" className="block text-sm font-medium text-gray-700 mb-1">
          Warehouse *
        </label>
        <select
          id="warehouseId"
          value={formData.warehouseId}
          onChange={(e) => handleChange('warehouseId', e.target.value)}
          className={cn(
            'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
            errors.warehouseId ? 'border-red-500' : 'border-gray-300'
          )}
        >
          <option value="">Select warehouse</option>
          {warehouses.map((wh) => (
            <option key={wh.id} value={wh.id}>
              {wh.name} {wh.code ? `(${wh.code})` : ''}
            </option>
          ))}
        </select>
        {errors.warehouseId && <p className="text-red-500 text-sm mt-1">{errors.warehouseId}</p>}
      </div>

      {/* Movement Type and Date */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="movementType" className="block text-sm font-medium text-gray-700 mb-1">
            Movement Type *
          </label>
          <select
            id="movementType"
            value={formData.movementType}
            onChange={(e) => handleChange('movementType', e.target.value as MovementType)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {movementTypes.map((type) => (
              <option key={type.value} value={type.value}>
                {type.label}
              </option>
            ))}
          </select>
          <p className="text-xs text-gray-500 mt-1">
            {selectedMovementType?.direction === 'in' ? (
              <span className="text-green-600">Stock will increase</span>
            ) : (
              <span className="text-red-600">Stock will decrease</span>
            )}
          </p>
        </div>
        <div>
          <label htmlFor="movementDate" className="block text-sm font-medium text-gray-700 mb-1">
            Date *
          </label>
          <input
            id="movementDate"
            type="date"
            value={formData.movementDate}
            onChange={(e) => handleChange('movementDate', e.target.value)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.movementDate ? 'border-red-500' : 'border-gray-300'
            )}
          />
          {errors.movementDate && <p className="text-red-500 text-sm mt-1">{errors.movementDate}</p>}
        </div>
      </div>

      {/* Quantity and Rate */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="quantity" className="block text-sm font-medium text-gray-700 mb-1">
            Quantity *
          </label>
          <input
            id="quantity"
            type="number"
            step="0.01"
            min="0"
            value={formData.quantity || ''}
            onChange={(e) => handleChange('quantity', parseFloat(e.target.value) || 0)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.quantity ? 'border-red-500' : 'border-gray-300'
            )}
            placeholder="0.00"
          />
          {errors.quantity && <p className="text-red-500 text-sm mt-1">{errors.quantity}</p>}
        </div>
        <div>
          <label htmlFor="rate" className="block text-sm font-medium text-gray-700 mb-1">
            Rate (per unit)
          </label>
          <input
            id="rate"
            type="number"
            step="0.01"
            min="0"
            value={formData.rate || ''}
            onChange={(e) => handleChange('rate', e.target.value ? parseFloat(e.target.value) : undefined)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="0.00"
          />
        </div>
      </div>

      {/* Notes */}
      <div>
        <label htmlFor="notes" className="block text-sm font-medium text-gray-700 mb-1">
          Notes
        </label>
        <textarea
          id="notes"
          rows={2}
          value={formData.notes || ''}
          onChange={(e) => handleChange('notes', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Reason for adjustment..."
        />
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
          {isLoading ? 'Saving...' : 'Record Movement'}
        </button>
      </div>
    </form>
  );
};

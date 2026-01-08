import { useState, useEffect } from 'react';
import type {
  StockTransfer,
  CreateStockTransferDto,
  UpdateStockTransferDto,
  CreateStockTransferItemDto,
} from '@/services/api/types';
import { useCreateStockTransfer, useUpdateStockTransfer } from '@/features/inventory/hooks';
import { useActiveStockItems, useActiveWarehouses } from '@/features/inventory/hooks';
import { cn } from '@/lib/utils';
import { Plus, Trash2 } from 'lucide-react';

interface StockTransferFormProps {
  transfer?: StockTransfer;
  companyId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

interface TransferItemRow extends CreateStockTransferItemDto {
  _key: string; // Unique key for React rendering
}

export const StockTransferForm = ({
  transfer,
  companyId,
  onSuccess,
  onCancel,
}: StockTransferFormProps) => {
  const [formData, setFormData] = useState({
    companyId,
    transferDate: new Date().toISOString().split('T')[0],
    fromWarehouseId: '',
    toWarehouseId: '',
    notes: '',
  });

  const [items, setItems] = useState<TransferItemRow[]>([
    { _key: crypto.randomUUID(), stockItemId: '', quantity: 0, rate: undefined, notes: '' },
  ]);

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createTransfer = useCreateStockTransfer();
  const updateTransfer = useUpdateStockTransfer();
  const { data: stockItems = [] } = useActiveStockItems(companyId);
  const { data: warehouses = [] } = useActiveWarehouses(companyId);

  const isEditing = !!transfer;
  const isLoading = createTransfer.isPending || updateTransfer.isPending;

  useEffect(() => {
    if (transfer) {
      setFormData({
        companyId: transfer.companyId,
        transferDate: transfer.transferDate,
        fromWarehouseId: transfer.fromWarehouseId,
        toWarehouseId: transfer.toWarehouseId,
        notes: transfer.notes || '',
      });
      if (transfer.items && transfer.items.length > 0) {
        setItems(
          transfer.items.map((item) => ({
            _key: item.id || crypto.randomUUID(),
            stockItemId: item.stockItemId,
            quantity: item.quantity,
            rate: item.rate,
            notes: item.notes || '',
          }))
        );
      }
    }
  }, [transfer]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.fromWarehouseId) {
      newErrors.fromWarehouseId = 'Source warehouse is required';
    }

    if (!formData.toWarehouseId) {
      newErrors.toWarehouseId = 'Destination warehouse is required';
    }

    if (formData.fromWarehouseId === formData.toWarehouseId && formData.fromWarehouseId) {
      newErrors.toWarehouseId = 'Source and destination must be different';
    }

    if (!formData.transferDate) {
      newErrors.transferDate = 'Transfer date is required';
    }

    // Validate items
    const validItems = items.filter((item) => item.stockItemId && item.quantity > 0);
    if (validItems.length === 0) {
      newErrors.items = 'At least one item with quantity is required';
    }

    // Check for duplicate items
    const itemIds = validItems.map((i) => i.stockItemId);
    if (new Set(itemIds).size !== itemIds.length) {
      newErrors.items = 'Duplicate items are not allowed';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      const validItems = items
        .filter((item) => item.stockItemId && item.quantity > 0)
        .map(({ _key, ...item }) => item);

      if (isEditing && transfer) {
        const updateData: UpdateStockTransferDto = {
          transferDate: formData.transferDate,
          fromWarehouseId: formData.fromWarehouseId,
          toWarehouseId: formData.toWarehouseId,
          notes: formData.notes,
          items: validItems,
        };
        await updateTransfer.mutateAsync({ id: transfer.id, data: updateData });
      } else {
        const createData: CreateStockTransferDto = {
          companyId: formData.companyId,
          transferDate: formData.transferDate,
          fromWarehouseId: formData.fromWarehouseId,
          toWarehouseId: formData.toWarehouseId,
          notes: formData.notes,
          items: validItems,
        };
        await createTransfer.mutateAsync(createData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (field: keyof typeof formData, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  const handleItemChange = (
    key: string,
    field: keyof CreateStockTransferItemDto,
    value: string | number | undefined
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
      { _key: crypto.randomUUID(), stockItemId: '', quantity: 0, rate: undefined, notes: '' },
    ]);
  };

  const removeItem = (key: string) => {
    if (items.length > 1) {
      setItems((prev) => prev.filter((item) => item._key !== key));
    }
  };

  // Get available items (not already selected)
  const getAvailableItems = (currentKey: string) => {
    const selectedIds = items.filter((i) => i._key !== currentKey).map((i) => i.stockItemId);
    return stockItems.filter((item) => !selectedIds.includes(item.id));
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Warehouses */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="fromWarehouseId" className="block text-sm font-medium text-gray-700 mb-1">
            From Warehouse *
          </label>
          <select
            id="fromWarehouseId"
            value={formData.fromWarehouseId}
            onChange={(e) => handleChange('fromWarehouseId', e.target.value)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.fromWarehouseId ? 'border-red-500' : 'border-gray-300'
            )}
          >
            <option value="">Select source warehouse</option>
            {warehouses.map((wh) => (
              <option key={wh.id} value={wh.id}>
                {wh.name} {wh.code ? `(${wh.code})` : ''}
              </option>
            ))}
          </select>
          {errors.fromWarehouseId && (
            <p className="text-red-500 text-sm mt-1">{errors.fromWarehouseId}</p>
          )}
        </div>
        <div>
          <label htmlFor="toWarehouseId" className="block text-sm font-medium text-gray-700 mb-1">
            To Warehouse *
          </label>
          <select
            id="toWarehouseId"
            value={formData.toWarehouseId}
            onChange={(e) => handleChange('toWarehouseId', e.target.value)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.toWarehouseId ? 'border-red-500' : 'border-gray-300'
            )}
          >
            <option value="">Select destination warehouse</option>
            {warehouses
              .filter((wh) => wh.id !== formData.fromWarehouseId)
              .map((wh) => (
                <option key={wh.id} value={wh.id}>
                  {wh.name} {wh.code ? `(${wh.code})` : ''}
                </option>
              ))}
          </select>
          {errors.toWarehouseId && (
            <p className="text-red-500 text-sm mt-1">{errors.toWarehouseId}</p>
          )}
        </div>
      </div>

      {/* Transfer Date */}
      <div>
        <label htmlFor="transferDate" className="block text-sm font-medium text-gray-700 mb-1">
          Transfer Date *
        </label>
        <input
          id="transferDate"
          type="date"
          value={formData.transferDate}
          onChange={(e) => handleChange('transferDate', e.target.value)}
          className={cn(
            'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
            errors.transferDate ? 'border-red-500' : 'border-gray-300'
          )}
        />
        {errors.transferDate && <p className="text-red-500 text-sm mt-1">{errors.transferDate}</p>}
      </div>

      {/* Transfer Items */}
      <div className="border-t border-gray-200 pt-4">
        <div className="flex justify-between items-center mb-3">
          <h3 className="text-sm font-medium text-gray-900">Transfer Items *</h3>
          <button
            type="button"
            onClick={addItem}
            className="inline-flex items-center px-2 py-1 text-xs font-medium text-primary bg-primary/10 rounded-md hover:bg-primary/20"
          >
            <Plus className="h-3 w-3 mr-1" />
            Add Item
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
              <div className="col-span-5">
                <label className="block text-xs font-medium text-gray-500 mb-1">
                  Item {index + 1}
                </label>
                <select
                  value={item.stockItemId}
                  onChange={(e) => handleItemChange(item._key, 'stockItemId', e.target.value)}
                  className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-ring"
                >
                  <option value="">Select item</option>
                  {getAvailableItems(item._key).map((si) => (
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
                <label className="block text-xs font-medium text-gray-500 mb-1">Rate</label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  value={item.rate || ''}
                  onChange={(e) =>
                    handleItemChange(
                      item._key,
                      'rate',
                      e.target.value ? parseFloat(e.target.value) : undefined
                    )
                  }
                  className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-ring"
                  placeholder="0.00"
                />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-500 mb-1">Notes</label>
                <input
                  type="text"
                  value={item.notes || ''}
                  onChange={(e) => handleItemChange(item._key, 'notes', e.target.value)}
                  className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-ring"
                  placeholder="Optional"
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

      {/* Notes */}
      <div>
        <label htmlFor="notes" className="block text-sm font-medium text-gray-700 mb-1">
          Notes
        </label>
        <textarea
          id="notes"
          rows={2}
          value={formData.notes}
          onChange={(e) => handleChange('notes', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Transfer notes..."
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Transfer' : 'Create Transfer'}
        </button>
      </div>
    </form>
  );
};

import { useState, useEffect } from 'react';
import type {
  ProductionOrder,
  CreateProductionOrderDto,
  UpdateProductionOrderDto,
  ProductionOrderItemDto,
} from '@/services/api/types';
import { useCreateProductionOrder, useUpdateProductionOrder, useActiveBoms, useBom } from '@/features/manufacturing/hooks';
import { useActiveWarehouses } from '@/features/inventory/hooks';
import { cn } from '@/lib/utils';
import { Plus, Trash2 } from 'lucide-react';

interface ProductionOrderFormProps {
  order?: ProductionOrder;
  companyId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

interface OrderItemRow extends ProductionOrderItemDto {
  _key: string;
}

export const ProductionOrderForm = ({
  order,
  companyId,
  onSuccess,
  onCancel,
}: ProductionOrderFormProps) => {
  const [formData, setFormData] = useState({
    companyId,
    orderNumber: '',
    bomId: '',
    warehouseId: '',
    plannedQuantity: 1,
    plannedStartDate: new Date().toISOString().split('T')[0],
    plannedEndDate: '',
    notes: '',
  });

  const [items, setItems] = useState<OrderItemRow[]>([]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const createOrder = useCreateProductionOrder();
  const updateOrder = useUpdateProductionOrder();
  const { data: boms = [] } = useActiveBoms(companyId);
  const { data: warehouses = [] } = useActiveWarehouses(companyId);
  const { data: selectedBom } = useBom(formData.bomId, !!formData.bomId);

  const isEditing = !!order;
  const isLoading = createOrder.isPending || updateOrder.isPending;

  // Auto-populate items when BOM is selected
  useEffect(() => {
    if (selectedBom && selectedBom.items && !isEditing) {
      const multiplier = formData.plannedQuantity / (selectedBom.outputQuantity || 1);
      setItems(
        selectedBom.items.map((bomItem) => ({
          _key: crypto.randomUUID(),
          stockItemId: bomItem.componentItemId,
          stockItemName: bomItem.componentItemName,
          requiredQuantity: bomItem.quantity * multiplier * (1 + (bomItem.scrapPercentage || 0) / 100),
          consumedQuantity: 0,
        }))
      );
    }
  }, [selectedBom, formData.plannedQuantity, isEditing]);

  useEffect(() => {
    if (order) {
      setFormData({
        companyId: order.companyId,
        orderNumber: order.orderNumber || '',
        bomId: order.bomId,
        warehouseId: order.warehouseId,
        plannedQuantity: order.plannedQuantity || 1,
        plannedStartDate: order.plannedStartDate || '',
        plannedEndDate: order.plannedEndDate || '',
        notes: order.notes || '',
      });
      if (order.items && order.items.length > 0) {
        setItems(
          order.items.map((item) => ({
            _key: item.id || crypto.randomUUID(),
            stockItemId: item.stockItemId,
            stockItemName: item.stockItemName,
            requiredQuantity: item.requiredQuantity,
            consumedQuantity: item.consumedQuantity || 0,
          }))
        );
      }
    }
  }, [order]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.bomId) {
      newErrors.bomId = 'Bill of Materials is required';
    }

    if (!formData.warehouseId) {
      newErrors.warehouseId = 'Production warehouse is required';
    }

    if (formData.plannedQuantity <= 0) {
      newErrors.plannedQuantity = 'Planned quantity must be greater than 0';
    }

    if (!formData.plannedStartDate) {
      newErrors.plannedStartDate = 'Planned start date is required';
    }

    if (items.length === 0) {
      newErrors.items = 'At least one component is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      const validItems = items.map(({ _key, stockItemName, ...item }) => item);

      if (isEditing && order) {
        const updateData: UpdateProductionOrderDto = {
          warehouseId: formData.warehouseId,
          plannedQuantity: formData.plannedQuantity,
          plannedStartDate: formData.plannedStartDate,
          plannedEndDate: formData.plannedEndDate || undefined,
          notes: formData.notes,
          items: validItems,
        };
        await updateOrder.mutateAsync({ id: order.id, data: updateData });
      } else {
        const createData: CreateProductionOrderDto = {
          companyId: formData.companyId,
          orderNumber: formData.orderNumber || undefined,
          bomId: formData.bomId,
          warehouseId: formData.warehouseId,
          plannedQuantity: formData.plannedQuantity,
          plannedStartDate: formData.plannedStartDate,
          plannedEndDate: formData.plannedEndDate || undefined,
          notes: formData.notes,
          items: validItems,
        };
        await createOrder.mutateAsync(createData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (field: keyof typeof formData, value: string | number) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  const handleItemChange = (
    key: string,
    field: keyof ProductionOrderItemDto,
    value: number
  ) => {
    setItems((prev) =>
      prev.map((item) => (item._key === key ? { ...item, [field]: value } : item))
    );
  };

  const addItem = () => {
    setItems((prev) => [
      ...prev,
      { _key: crypto.randomUUID(), stockItemId: '', requiredQuantity: 0, consumedQuantity: 0 },
    ]);
  };

  const removeItem = (key: string) => {
    if (items.length > 1) {
      setItems((prev) => prev.filter((item) => item._key !== key));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Basic Info */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="bomId" className="block text-sm font-medium text-gray-700 mb-1">
            Bill of Materials *
          </label>
          <select
            id="bomId"
            value={formData.bomId}
            onChange={(e) => handleChange('bomId', e.target.value)}
            disabled={isEditing}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.bomId ? 'border-red-500' : 'border-gray-300',
              isEditing ? 'bg-gray-100' : ''
            )}
          >
            <option value="">Select BOM</option>
            {boms.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name} - {b.finishedGoodName}
              </option>
            ))}
          </select>
          {errors.bomId && <p className="text-red-500 text-sm mt-1">{errors.bomId}</p>}
        </div>
        <div>
          <label htmlFor="orderNumber" className="block text-sm font-medium text-gray-700 mb-1">
            Order Number
          </label>
          <input
            id="orderNumber"
            type="text"
            value={formData.orderNumber}
            onChange={(e) => handleChange('orderNumber', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="Auto-generated if empty"
          />
        </div>
      </div>

      {/* Warehouse and Quantity */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="warehouseId" className="block text-sm font-medium text-gray-700 mb-1">
            Production Warehouse *
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
        <div>
          <label htmlFor="plannedQuantity" className="block text-sm font-medium text-gray-700 mb-1">
            Planned Quantity *
          </label>
          <input
            id="plannedQuantity"
            type="number"
            step="0.01"
            min="0.01"
            value={formData.plannedQuantity}
            onChange={(e) => handleChange('plannedQuantity', parseFloat(e.target.value) || 1)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.plannedQuantity ? 'border-red-500' : 'border-gray-300'
            )}
          />
          {errors.plannedQuantity && <p className="text-red-500 text-sm mt-1">{errors.plannedQuantity}</p>}
        </div>
      </div>

      {/* Dates */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="plannedStartDate" className="block text-sm font-medium text-gray-700 mb-1">
            Planned Start Date *
          </label>
          <input
            id="plannedStartDate"
            type="date"
            value={formData.plannedStartDate}
            onChange={(e) => handleChange('plannedStartDate', e.target.value)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.plannedStartDate ? 'border-red-500' : 'border-gray-300'
            )}
          />
          {errors.plannedStartDate && <p className="text-red-500 text-sm mt-1">{errors.plannedStartDate}</p>}
        </div>
        <div>
          <label htmlFor="plannedEndDate" className="block text-sm font-medium text-gray-700 mb-1">
            Planned End Date
          </label>
          <input
            id="plannedEndDate"
            type="date"
            value={formData.plannedEndDate}
            onChange={(e) => handleChange('plannedEndDate', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
      </div>

      {/* Production Items */}
      <div className="border-t border-gray-200 pt-4">
        <div className="flex justify-between items-center mb-3">
          <h3 className="text-sm font-medium text-gray-900">Components Required *</h3>
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
          {items.length === 0 ? (
            <p className="text-gray-500 text-sm italic">Select a BOM to auto-populate components</p>
          ) : (
            items.map((item, index) => (
              <div
                key={item._key}
                className="grid grid-cols-12 gap-2 items-start p-3 bg-gray-50 rounded-md"
              >
                <div className="col-span-5">
                  <label className="block text-xs font-medium text-gray-500 mb-1">
                    Component {index + 1}
                  </label>
                  <div className="px-2 py-1.5 text-sm bg-white border border-gray-300 rounded-md">
                    {item.stockItemName || item.stockItemId || 'Unknown Item'}
                  </div>
                </div>
                <div className="col-span-3">
                  <label className="block text-xs font-medium text-gray-500 mb-1">Required Qty</label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    value={item.requiredQuantity || ''}
                    onChange={(e) =>
                      handleItemChange(item._key, 'requiredQuantity', parseFloat(e.target.value) || 0)
                    }
                    className="w-full px-2 py-1.5 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-ring"
                    placeholder="0"
                  />
                </div>
                <div className="col-span-3">
                  <label className="block text-xs font-medium text-gray-500 mb-1">Consumed</label>
                  <div className="px-2 py-1.5 text-sm bg-gray-100 border border-gray-300 rounded-md">
                    {item.consumedQuantity || 0}
                  </div>
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
            ))
          )}
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
          placeholder="Production notes..."
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Order' : 'Create Order'}
        </button>
      </div>
    </form>
  );
};

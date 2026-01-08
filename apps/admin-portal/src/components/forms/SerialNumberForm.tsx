import { useState, useEffect } from 'react';
import type {
  SerialNumber,
  CreateSerialNumberDto,
  UpdateSerialNumberDto,
  BulkCreateSerialNumberDto,
} from '@/services/api/types';
import { useCreateSerialNumber, useBulkCreateSerialNumbers, useUpdateSerialNumber } from '@/features/manufacturing/hooks';
import { useActiveStockItems, useActiveWarehouses } from '@/features/inventory/hooks';
import { cn } from '@/lib/utils';

interface SerialNumberFormProps {
  serialNumber?: SerialNumber;
  companyId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

type FormMode = 'single' | 'bulk';

export const SerialNumberForm = ({
  serialNumber,
  companyId,
  onSuccess,
  onCancel,
}: SerialNumberFormProps) => {
  const [mode, setMode] = useState<FormMode>('single');
  const [singleFormData, setSingleFormData] = useState<CreateSerialNumberDto>({
    companyId,
    stockItemId: '',
    warehouseId: '',
    serialCode: '',
    status: 'available',
    notes: '',
  });

  const [bulkFormData, setBulkFormData] = useState<BulkCreateSerialNumberDto>({
    companyId,
    stockItemId: '',
    warehouseId: '',
    prefix: '',
    startNumber: 1,
    count: 10,
    status: 'available',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createSerial = useCreateSerialNumber();
  const bulkCreateSerials = useBulkCreateSerialNumbers();
  const updateSerial = useUpdateSerialNumber();
  const { data: stockItems = [] } = useActiveStockItems(companyId);
  const { data: warehouses = [] } = useActiveWarehouses(companyId);

  // Filter stock items that have serial tracking enabled
  const serialEnabledItems = stockItems.filter((item) => (item as any).isSerialEnabled);

  const isEditing = !!serialNumber;
  const isLoading = createSerial.isPending || bulkCreateSerials.isPending || updateSerial.isPending;

  const statuses = [
    { value: 'available', label: 'Available' },
    { value: 'reserved', label: 'Reserved' },
    { value: 'sold', label: 'Sold' },
    { value: 'damaged', label: 'Damaged' },
  ];

  useEffect(() => {
    if (serialNumber) {
      setSingleFormData({
        companyId: serialNumber.companyId,
        stockItemId: serialNumber.stockItemId,
        warehouseId: serialNumber.warehouseId || '',
        serialCode: serialNumber.serialCode,
        status: serialNumber.status,
        notes: serialNumber.notes || '',
      });
    }
  }, [serialNumber]);

  const validateSingleForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!singleFormData.stockItemId) {
      newErrors.stockItemId = 'Stock item is required';
    }

    if (!singleFormData.serialCode?.trim()) {
      newErrors.serialCode = 'Serial code is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const validateBulkForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!bulkFormData.stockItemId) {
      newErrors.stockItemId = 'Stock item is required';
    }

    if (!bulkFormData.prefix?.trim()) {
      newErrors.prefix = 'Prefix is required';
    }

    if (bulkFormData.startNumber < 1) {
      newErrors.startNumber = 'Start number must be at least 1';
    }

    if (bulkFormData.count < 1 || bulkFormData.count > 1000) {
      newErrors.count = 'Count must be between 1 and 1000';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (isEditing && serialNumber) {
        if (!validateSingleForm()) return;

        const updateData: UpdateSerialNumberDto = {
          status: singleFormData.status,
          warehouseId: singleFormData.warehouseId || undefined,
          notes: singleFormData.notes,
        };
        await updateSerial.mutateAsync({ id: serialNumber.id, data: updateData });
      } else if (mode === 'single') {
        if (!validateSingleForm()) return;
        await createSerial.mutateAsync(singleFormData);
      } else {
        if (!validateBulkForm()) return;
        await bulkCreateSerials.mutateAsync(bulkFormData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleSingleChange = (
    field: keyof CreateSerialNumberDto,
    value: string
  ) => {
    setSingleFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  const handleBulkChange = (
    field: keyof BulkCreateSerialNumberDto,
    value: string | number
  ) => {
    setBulkFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  // Generate preview of serial numbers
  const generatePreview = () => {
    if (!bulkFormData.prefix) return [];
    const previews: string[] = [];
    for (let i = 0; i < Math.min(bulkFormData.count, 5); i++) {
      previews.push(`${bulkFormData.prefix}${String(bulkFormData.startNumber + i).padStart(4, '0')}`);
    }
    if (bulkFormData.count > 5) {
      previews.push('...');
      previews.push(`${bulkFormData.prefix}${String(bulkFormData.startNumber + bulkFormData.count - 1).padStart(4, '0')}`);
    }
    return previews;
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Mode Toggle (only for create) */}
      {!isEditing && (
        <div className="flex space-x-4 border-b border-gray-200 pb-4">
          <button
            type="button"
            onClick={() => setMode('single')}
            className={cn(
              'px-4 py-2 text-sm font-medium rounded-md',
              mode === 'single'
                ? 'bg-primary text-primary-foreground'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            )}
          >
            Single Serial
          </button>
          <button
            type="button"
            onClick={() => setMode('bulk')}
            className={cn(
              'px-4 py-2 text-sm font-medium rounded-md',
              mode === 'bulk'
                ? 'bg-primary text-primary-foreground'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            )}
          >
            Bulk Create
          </button>
        </div>
      )}

      {/* Single Mode Form */}
      {(mode === 'single' || isEditing) && (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="stockItemId" className="block text-sm font-medium text-gray-700 mb-1">
                Stock Item *
              </label>
              <select
                id="stockItemId"
                value={singleFormData.stockItemId}
                onChange={(e) => handleSingleChange('stockItemId', e.target.value)}
                disabled={isEditing}
                className={cn(
                  'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                  errors.stockItemId ? 'border-red-500' : 'border-gray-300',
                  isEditing ? 'bg-gray-100' : ''
                )}
              >
                <option value="">Select item</option>
                {(serialEnabledItems.length > 0 ? serialEnabledItems : stockItems).map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} {item.sku ? `(${item.sku})` : ''}
                  </option>
                ))}
              </select>
              {errors.stockItemId && <p className="text-red-500 text-sm mt-1">{errors.stockItemId}</p>}
            </div>
            <div>
              <label htmlFor="serialCode" className="block text-sm font-medium text-gray-700 mb-1">
                Serial Code *
              </label>
              <input
                id="serialCode"
                type="text"
                value={singleFormData.serialCode}
                onChange={(e) => handleSingleChange('serialCode', e.target.value)}
                disabled={isEditing}
                className={cn(
                  'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                  errors.serialCode ? 'border-red-500' : 'border-gray-300',
                  isEditing ? 'bg-gray-100' : ''
                )}
                placeholder="e.g., SN-2024-0001"
              />
              {errors.serialCode && <p className="text-red-500 text-sm mt-1">{errors.serialCode}</p>}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="warehouseId" className="block text-sm font-medium text-gray-700 mb-1">
                Warehouse
              </label>
              <select
                id="warehouseId"
                value={singleFormData.warehouseId || ''}
                onChange={(e) => handleSingleChange('warehouseId', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">Select warehouse</option>
                {warehouses.map((wh) => (
                  <option key={wh.id} value={wh.id}>
                    {wh.name} {wh.code ? `(${wh.code})` : ''}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="status" className="block text-sm font-medium text-gray-700 mb-1">
                Status
              </label>
              <select
                id="status"
                value={singleFormData.status}
                onChange={(e) => handleSingleChange('status', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                {statuses.map((s) => (
                  <option key={s.value} value={s.value}>
                    {s.label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label htmlFor="notes" className="block text-sm font-medium text-gray-700 mb-1">
              Notes
            </label>
            <textarea
              id="notes"
              rows={2}
              value={singleFormData.notes || ''}
              onChange={(e) => handleSingleChange('notes', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Additional notes..."
            />
          </div>
        </>
      )}

      {/* Bulk Mode Form */}
      {mode === 'bulk' && !isEditing && (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="bulkStockItemId" className="block text-sm font-medium text-gray-700 mb-1">
                Stock Item *
              </label>
              <select
                id="bulkStockItemId"
                value={bulkFormData.stockItemId}
                onChange={(e) => handleBulkChange('stockItemId', e.target.value)}
                className={cn(
                  'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                  errors.stockItemId ? 'border-red-500' : 'border-gray-300'
                )}
              >
                <option value="">Select item</option>
                {(serialEnabledItems.length > 0 ? serialEnabledItems : stockItems).map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} {item.sku ? `(${item.sku})` : ''}
                  </option>
                ))}
              </select>
              {errors.stockItemId && <p className="text-red-500 text-sm mt-1">{errors.stockItemId}</p>}
            </div>
            <div>
              <label htmlFor="bulkWarehouseId" className="block text-sm font-medium text-gray-700 mb-1">
                Warehouse
              </label>
              <select
                id="bulkWarehouseId"
                value={bulkFormData.warehouseId || ''}
                onChange={(e) => handleBulkChange('warehouseId', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">Select warehouse</option>
                {warehouses.map((wh) => (
                  <option key={wh.id} value={wh.id}>
                    {wh.name} {wh.code ? `(${wh.code})` : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label htmlFor="prefix" className="block text-sm font-medium text-gray-700 mb-1">
                Prefix *
              </label>
              <input
                id="prefix"
                type="text"
                value={bulkFormData.prefix}
                onChange={(e) => handleBulkChange('prefix', e.target.value)}
                className={cn(
                  'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                  errors.prefix ? 'border-red-500' : 'border-gray-300'
                )}
                placeholder="e.g., SN-2024-"
              />
              {errors.prefix && <p className="text-red-500 text-sm mt-1">{errors.prefix}</p>}
            </div>
            <div>
              <label htmlFor="startNumber" className="block text-sm font-medium text-gray-700 mb-1">
                Start Number *
              </label>
              <input
                id="startNumber"
                type="number"
                min="1"
                value={bulkFormData.startNumber}
                onChange={(e) => handleBulkChange('startNumber', parseInt(e.target.value) || 1)}
                className={cn(
                  'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                  errors.startNumber ? 'border-red-500' : 'border-gray-300'
                )}
              />
              {errors.startNumber && <p className="text-red-500 text-sm mt-1">{errors.startNumber}</p>}
            </div>
            <div>
              <label htmlFor="count" className="block text-sm font-medium text-gray-700 mb-1">
                Count *
              </label>
              <input
                id="count"
                type="number"
                min="1"
                max="1000"
                value={bulkFormData.count}
                onChange={(e) => handleBulkChange('count', parseInt(e.target.value) || 10)}
                className={cn(
                  'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                  errors.count ? 'border-red-500' : 'border-gray-300'
                )}
              />
              {errors.count && <p className="text-red-500 text-sm mt-1">{errors.count}</p>}
            </div>
          </div>

          <div>
            <label htmlFor="bulkStatus" className="block text-sm font-medium text-gray-700 mb-1">
              Initial Status
            </label>
            <select
              id="bulkStatus"
              value={bulkFormData.status}
              onChange={(e) => handleBulkChange('status', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {statuses.map((s) => (
                <option key={s.value} value={s.value}>
                  {s.label}
                </option>
              ))}
            </select>
          </div>

          {/* Preview */}
          {bulkFormData.prefix && (
            <div className="border-t border-gray-200 pt-4">
              <h4 className="text-sm font-medium text-gray-700 mb-2">Preview</h4>
              <div className="bg-gray-50 rounded-md p-3">
                <p className="text-sm text-gray-600">
                  Will create {bulkFormData.count} serial numbers:
                </p>
                <div className="mt-2 flex flex-wrap gap-2">
                  {generatePreview().map((code, i) => (
                    <span
                      key={i}
                      className="inline-flex px-2 py-1 text-xs font-mono bg-white border border-gray-200 rounded"
                    >
                      {code}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          )}
        </>
      )}

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
          {isLoading
            ? 'Saving...'
            : isEditing
            ? 'Update Serial'
            : mode === 'bulk'
            ? `Create ${bulkFormData.count} Serials`
            : 'Create Serial'}
        </button>
      </div>
    </form>
  );
};

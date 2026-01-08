import { useState, useEffect } from 'react';
import type { Warehouse, CreateWarehouseDto, UpdateWarehouseDto } from '@/services/api/types';
import { useCreateWarehouse, useUpdateWarehouse, useWarehouses } from '@/features/inventory/hooks';
import { cn } from '@/lib/utils';

interface WarehouseFormProps {
  warehouse?: Warehouse;
  companyId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

export const WarehouseForm = ({ warehouse, companyId, onSuccess, onCancel }: WarehouseFormProps) => {
  const [formData, setFormData] = useState<CreateWarehouseDto>({
    companyId,
    name: '',
    code: '',
    address: '',
    city: '',
    state: '',
    pinCode: '',
    isDefault: false,
    parentWarehouseId: undefined,
    isActive: true,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createWarehouse = useCreateWarehouse();
  const updateWarehouse = useUpdateWarehouse();
  const { data: warehouses = [] } = useWarehouses(companyId);

  const isEditing = !!warehouse;
  const isLoading = createWarehouse.isPending || updateWarehouse.isPending;

  // Filter out current warehouse from parent options to prevent circular reference
  const parentOptions = warehouses.filter((w) => w.id !== warehouse?.id);

  const indianStates = [
    'Andhra Pradesh', 'Arunachal Pradesh', 'Assam', 'Bihar', 'Chhattisgarh',
    'Goa', 'Gujarat', 'Haryana', 'Himachal Pradesh', 'Jharkhand', 'Karnataka',
    'Kerala', 'Madhya Pradesh', 'Maharashtra', 'Manipur', 'Meghalaya', 'Mizoram',
    'Nagaland', 'Odisha', 'Punjab', 'Rajasthan', 'Sikkim', 'Tamil Nadu',
    'Telangana', 'Tripura', 'Uttar Pradesh', 'Uttarakhand', 'West Bengal',
    'Delhi', 'Jammu and Kashmir', 'Ladakh', 'Puducherry', 'Chandigarh',
  ];

  useEffect(() => {
    if (warehouse) {
      setFormData({
        companyId: warehouse.companyId,
        name: warehouse.name || '',
        code: warehouse.code || '',
        address: warehouse.address || '',
        city: warehouse.city || '',
        state: warehouse.state || '',
        pinCode: warehouse.pinCode || '',
        isDefault: warehouse.isDefault || false,
        parentWarehouseId: warehouse.parentWarehouseId || undefined,
        isActive: warehouse.isActive ?? true,
      });
    }
  }, [warehouse]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name?.trim()) {
      newErrors.name = 'Warehouse name is required';
    }

    if (formData.pinCode && !/^\d{6}$/.test(formData.pinCode)) {
      newErrors.pinCode = 'PIN code must be 6 digits';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      if (isEditing && warehouse) {
        const updateData: UpdateWarehouseDto = {
          name: formData.name,
          code: formData.code,
          address: formData.address,
          city: formData.city,
          state: formData.state,
          pinCode: formData.pinCode,
          isDefault: formData.isDefault,
          parentWarehouseId: formData.parentWarehouseId,
          isActive: formData.isActive,
        };
        await updateWarehouse.mutateAsync({ id: warehouse.id, data: updateData });
      } else {
        await createWarehouse.mutateAsync(formData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (field: keyof CreateWarehouseDto, value: string | boolean | undefined) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Warehouse Name */}
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
          Warehouse Name *
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
          placeholder="Enter warehouse name"
        />
        {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
      </div>

      {/* Code and Parent */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="code" className="block text-sm font-medium text-gray-700 mb-1">
            Warehouse Code
          </label>
          <input
            id="code"
            type="text"
            value={formData.code || ''}
            onChange={(e) => handleChange('code', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="WH-001"
          />
        </div>
        <div>
          <label htmlFor="parentWarehouseId" className="block text-sm font-medium text-gray-700 mb-1">
            Parent Warehouse
          </label>
          <select
            id="parentWarehouseId"
            value={formData.parentWarehouseId || ''}
            onChange={(e) => handleChange('parentWarehouseId', e.target.value || undefined)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">None (Top Level)</option>
            {parentOptions.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Address */}
      <div>
        <label htmlFor="address" className="block text-sm font-medium text-gray-700 mb-1">
          Address
        </label>
        <textarea
          id="address"
          rows={2}
          value={formData.address || ''}
          onChange={(e) => handleChange('address', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Street address..."
        />
      </div>

      {/* City, State, PIN */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div>
          <label htmlFor="city" className="block text-sm font-medium text-gray-700 mb-1">
            City
          </label>
          <input
            id="city"
            type="text"
            value={formData.city || ''}
            onChange={(e) => handleChange('city', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="City"
          />
        </div>
        <div>
          <label htmlFor="state" className="block text-sm font-medium text-gray-700 mb-1">
            State
          </label>
          <select
            id="state"
            value={formData.state || ''}
            onChange={(e) => handleChange('state', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">Select state</option>
            {indianStates.map((state) => (
              <option key={state} value={state}>
                {state}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="pinCode" className="block text-sm font-medium text-gray-700 mb-1">
            PIN Code
          </label>
          <input
            id="pinCode"
            type="text"
            value={formData.pinCode || ''}
            onChange={(e) => handleChange('pinCode', e.target.value)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.pinCode ? 'border-red-500' : 'border-gray-300'
            )}
            placeholder="560001"
            maxLength={6}
          />
          {errors.pinCode && <p className="text-red-500 text-sm mt-1">{errors.pinCode}</p>}
        </div>
      </div>

      {/* Checkboxes */}
      <div className="flex flex-wrap gap-6">
        <div className="flex items-center">
          <input
            id="isDefault"
            type="checkbox"
            checked={formData.isDefault}
            onChange={(e) => handleChange('isDefault', e.target.checked)}
            className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
          />
          <label htmlFor="isDefault" className="ml-2 block text-sm text-gray-900">
            Set as default warehouse
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
            Warehouse is active
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Warehouse' : 'Create Warehouse'}
        </button>
      </div>
    </form>
  );
};

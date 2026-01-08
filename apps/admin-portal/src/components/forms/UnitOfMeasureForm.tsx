import { useState, useEffect } from 'react';
import type { UnitOfMeasure, CreateUnitOfMeasureDto, UpdateUnitOfMeasureDto } from '@/services/api/types';
import { useCreateUnitOfMeasure, useUpdateUnitOfMeasure } from '@/features/inventory/hooks';
import { cn } from '@/lib/utils';

interface UnitOfMeasureFormProps {
  unit?: UnitOfMeasure;
  companyId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

export const UnitOfMeasureForm = ({ unit, companyId, onSuccess, onCancel }: UnitOfMeasureFormProps) => {
  const [formData, setFormData] = useState<CreateUnitOfMeasureDto>({
    companyId,
    name: '',
    symbol: '',
    decimalPlaces: 2,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createUnit = useCreateUnitOfMeasure();
  const updateUnit = useUpdateUnitOfMeasure();

  const isEditing = !!unit;
  const isLoading = createUnit.isPending || updateUnit.isPending;

  // Common unit presets for quick selection
  const commonUnits = [
    { name: 'Pieces', symbol: 'pcs', decimalPlaces: 0 },
    { name: 'Kilograms', symbol: 'kg', decimalPlaces: 3 },
    { name: 'Grams', symbol: 'g', decimalPlaces: 2 },
    { name: 'Liters', symbol: 'L', decimalPlaces: 3 },
    { name: 'Milliliters', symbol: 'ml', decimalPlaces: 2 },
    { name: 'Meters', symbol: 'm', decimalPlaces: 2 },
    { name: 'Centimeters', symbol: 'cm', decimalPlaces: 2 },
    { name: 'Square Meters', symbol: 'sqm', decimalPlaces: 2 },
    { name: 'Boxes', symbol: 'box', decimalPlaces: 0 },
    { name: 'Cartons', symbol: 'ctn', decimalPlaces: 0 },
    { name: 'Dozens', symbol: 'dz', decimalPlaces: 0 },
    { name: 'Pairs', symbol: 'pr', decimalPlaces: 0 },
  ];

  useEffect(() => {
    if (unit) {
      setFormData({
        companyId: unit.companyId || companyId,
        name: unit.name || '',
        symbol: unit.symbol || '',
        decimalPlaces: unit.decimalPlaces ?? 2,
      });
    }
  }, [unit, companyId]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name?.trim()) {
      newErrors.name = 'Unit name is required';
    }

    if (!formData.symbol?.trim()) {
      newErrors.symbol = 'Symbol is required';
    }

    if (formData.decimalPlaces !== undefined && (formData.decimalPlaces < 0 || formData.decimalPlaces > 6)) {
      newErrors.decimalPlaces = 'Decimal places must be between 0 and 6';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      if (isEditing && unit) {
        const updateData: UpdateUnitOfMeasureDto = {
          name: formData.name,
          symbol: formData.symbol,
          decimalPlaces: formData.decimalPlaces,
        };
        await updateUnit.mutateAsync({ id: unit.id, data: updateData });
      } else {
        await createUnit.mutateAsync(formData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (field: keyof CreateUnitOfMeasureDto, value: string | number) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  const applyPreset = (preset: { name: string; symbol: string; decimalPlaces: number }) => {
    setFormData((prev) => ({
      ...prev,
      name: preset.name,
      symbol: preset.symbol,
      decimalPlaces: preset.decimalPlaces,
    }));
    setErrors({});
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Quick Presets */}
      {!isEditing && (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Quick Presets</label>
          <div className="flex flex-wrap gap-2">
            {commonUnits.slice(0, 6).map((preset) => (
              <button
                key={preset.symbol}
                type="button"
                onClick={() => applyPreset(preset)}
                className="px-2 py-1 text-xs bg-gray-100 hover:bg-gray-200 rounded-md transition-colors"
              >
                {preset.name} ({preset.symbol})
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Unit Name */}
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
          Unit Name *
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
          placeholder="e.g., Kilograms"
        />
        {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
      </div>

      {/* Symbol and Decimal Places */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="symbol" className="block text-sm font-medium text-gray-700 mb-1">
            Symbol *
          </label>
          <input
            id="symbol"
            type="text"
            value={formData.symbol}
            onChange={(e) => handleChange('symbol', e.target.value)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.symbol ? 'border-red-500' : 'border-gray-300'
            )}
            placeholder="e.g., kg"
            maxLength={10}
          />
          {errors.symbol && <p className="text-red-500 text-sm mt-1">{errors.symbol}</p>}
        </div>
        <div>
          <label htmlFor="decimalPlaces" className="block text-sm font-medium text-gray-700 mb-1">
            Decimal Places
          </label>
          <input
            id="decimalPlaces"
            type="number"
            min={0}
            max={6}
            value={formData.decimalPlaces}
            onChange={(e) => handleChange('decimalPlaces', parseInt(e.target.value) || 0)}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.decimalPlaces ? 'border-red-500' : 'border-gray-300'
            )}
          />
          {errors.decimalPlaces && <p className="text-red-500 text-sm mt-1">{errors.decimalPlaces}</p>}
          <p className="text-xs text-gray-500 mt-1">Number of decimal places for quantity display</p>
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Unit' : 'Create Unit'}
        </button>
      </div>
    </form>
  );
};

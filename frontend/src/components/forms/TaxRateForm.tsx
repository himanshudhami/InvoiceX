import { useState, useEffect } from 'react'
import { TaxRate, CreateTaxRateDto, UpdateTaxRateDto } from '@/services/api/types'
import { useCreateTaxRate, useUpdateTaxRate } from '@/hooks/api/useTaxRates'
import { useCompanies } from '@/hooks/api/useCompanies'
import { cn } from '@/lib/utils'

interface TaxRateFormProps {
  taxRate?: TaxRate
  onSuccess: () => void
  onCancel: () => void
}

export const TaxRateForm = ({ taxRate, onSuccess, onCancel }: TaxRateFormProps) => {
  const [formData, setFormData] = useState<CreateTaxRateDto>({
    companyId: '',
    name: '',
    rate: 0,
    isDefault: false,
    isActive: true,
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createTaxRate = useCreateTaxRate()
  const updateTaxRate = useUpdateTaxRate()
  const { data: companies = [] } = useCompanies()

  const isEditing = !!taxRate
  const isLoading = createTaxRate.isPending || updateTaxRate.isPending

  // Common GST rate presets for India
  const commonTaxRates = [
    { name: 'GST 0% (Exempt)', rate: 0 },
    { name: 'GST 5%', rate: 5 },
    { name: 'GST 12%', rate: 12 },
    { name: 'GST 18% (Standard)', rate: 18 },
    { name: 'GST 28%', rate: 28 },
    { name: 'IGST 18%', rate: 18 },
  ]

  // Populate form with existing tax rate data
  useEffect(() => {
    if (taxRate) {
      setFormData({
        companyId: taxRate.companyId || '',
        name: taxRate.name || '',
        rate: taxRate.rate || 0,
        isDefault: taxRate.isDefault ?? false,
        isActive: taxRate.isActive ?? true,
      })
    }
  }, [taxRate])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.name?.trim()) {
      newErrors.name = 'Tax rate name is required'
    }

    if (formData.rate < 0) {
      newErrors.rate = 'Tax rate cannot be negative'
    }

    if (formData.rate > 100) {
      newErrors.rate = 'Tax rate cannot exceed 100%'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && taxRate) {
        await updateTaxRate.mutateAsync({ id: taxRate.id, data: formData })
      } else {
        await createTaxRate.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const handleChange = (field: keyof CreateTaxRateDto, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  const handlePresetSelect = (preset: { name: string; rate: number }) => {
    setFormData(prev => ({
      ...prev,
      name: preset.name,
      rate: preset.rate,
    }))
    // Clear errors
    setErrors({})
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Quick Presets (only for new tax rates) */}
      {!isEditing && (
        <div className="space-y-3">
          <h3 className="text-lg font-medium text-gray-900 border-b border-gray-200 pb-2">
            Quick Presets
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {commonTaxRates.map((preset, index) => (
              <button
                key={index}
                type="button"
                onClick={() => handlePresetSelect(preset)}
                className="p-3 text-left border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-ring transition-colors"
              >
                <div className="font-medium text-gray-900">{preset.name}</div>
                <div className="text-sm text-gray-500">{preset.rate}%</div>
              </button>
            ))}
          </div>
          <div className="text-center text-gray-500 text-sm">
            Or create a custom tax rate below
          </div>
        </div>
      )}

      {/* Basic Information */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900 border-b border-gray-200 pb-2">
          Tax Rate Details
        </h3>
        
        {/* Tax Rate Name */}
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
            Tax Rate Name *
          </label>
          <input
            id="name"
            type="text"
            value={formData.name}
            onChange={(e) => handleChange('name', e.target.value)}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
              errors.name ? "border-red-500" : "border-gray-300"
            )}
            placeholder="e.g., VAT Standard Rate, Sales Tax"
          />
          {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
        </div>

        {/* Tax Rate Percentage */}
        <div>
          <label htmlFor="rate" className="block text-sm font-medium text-gray-700 mb-1">
            Tax Rate (%) *
          </label>
          <input
            id="rate"
            type="number"
            step="0.01"
            min="0"
            max="100"
            value={formData.rate}
            onChange={(e) => handleChange('rate', parseFloat(e.target.value) || 0)}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
              errors.rate ? "border-red-500" : "border-gray-300"
            )}
            placeholder="0.00"
          />
          {errors.rate && <p className="text-red-500 text-sm mt-1">{errors.rate}</p>}
          <p className="text-sm text-gray-500 mt-1">
            Enter the tax percentage (e.g., 20 for 20%)
          </p>
        </div>

        {/* Company Association */}
        <div>
          <label htmlFor="companyId" className="block text-sm font-medium text-gray-700 mb-1">
            Associated Company
          </label>
          <select
            id="companyId"
            value={formData.companyId || ''}
            onChange={(e) => handleChange('companyId', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">Select a company (optional)</option>
            {companies.map((company) => (
              <option key={company.id} value={company.id}>
                {company.name}
              </option>
            ))}
          </select>
          <p className="text-sm text-gray-500 mt-1">
            Link this tax rate to a specific company or leave blank for global use
          </p>
        </div>

        {/* Settings */}
        <div className="space-y-3">
          {/* Default Tax Rate */}
          <div className="flex items-center">
            <input
              id="isDefault"
              type="checkbox"
              checked={formData.isDefault || false}
              onChange={(e) => handleChange('isDefault', e.target.checked)}
              className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
            />
            <label htmlFor="isDefault" className="ml-2 block text-sm text-gray-900">
              Set as default tax rate
            </label>
          </div>
          <p className="text-sm text-gray-500 ml-6">
            Default tax rates are automatically selected when creating new invoices or products
          </p>

          {/* Active Status */}
          <div className="flex items-center">
            <input
              id="isActive"
              type="checkbox"
              checked={formData.isActive || true}
              onChange={(e) => handleChange('isActive', e.target.checked)}
              className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
            />
            <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
              Tax rate is active
            </label>
          </div>
          <p className="text-sm text-gray-500 ml-6">
            Inactive tax rates won't appear in dropdown selections
          </p>
        </div>
      </div>

      {/* Preview */}
      <div className="bg-gray-50 p-4 rounded-md">
        <h4 className="text-sm font-medium text-gray-900 mb-2">Preview</h4>
        <div className="text-sm text-gray-600">
          <div>Tax Rate: <span className="font-medium">{formData.name || 'Untitled Tax Rate'}</span></div>
          <div>Rate: <span className="font-medium">{formData.rate}%</span></div>
          <div>
            Example: ₹100.00 + {formData.rate}% tax = ₹{(100 + (100 * (formData.rate / 100))).toFixed(2)}
          </div>
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Tax Rate' : 'Create Tax Rate'}
        </button>
      </div>
    </form>
  )
}
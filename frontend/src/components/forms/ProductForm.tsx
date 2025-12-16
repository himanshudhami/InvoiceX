import { useState, useEffect, useMemo } from 'react'
import { Product, CreateProductDto, UpdateProductDto } from '@/services/api/types'
import { useCreateProduct, useUpdateProduct } from '@/hooks/api/useProducts'
import { useCompanies } from '@/hooks/api/useCompanies'
import { cn } from '@/lib/utils'
import { Combobox, ComboboxOption } from '@/components/ui/combobox'

interface ProductFormProps {
  product?: Product
  onSuccess: () => void
  onCancel: () => void
}

export const ProductForm = ({ product, onSuccess, onCancel }: ProductFormProps) => {
  const [formData, setFormData] = useState<CreateProductDto>({
    name: '',
    description: '',
    sku: '',
    category: '',
    type: 'product',
    unitPrice: 0,
    unit: '',
    taxRate: 0,
    isActive: true,
    // GST fields
    hsnSacCode: '',
    isService: true,
    defaultGstRate: 18,
    cessRate: 0,
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createProduct = useCreateProduct()
  const updateProduct = useUpdateProduct()
  const { data: companies = [] } = useCompanies()

  const isEditing = !!product
  const isLoading = createProduct.isPending || updateProduct.isPending

  const productTypes = [
    { value: 'product', label: 'Product' },
    { value: 'service', label: 'Service' },
  ]

  const commonCategories = [
    'General',
    'Software',
    'Hardware',
    'Consulting',
    'Support',
    'Training',
    'Maintenance',
    'Development',
  ]

  const commonUnits = [
    'each',
    'hour',
    'day',
    'month',
    'year',
    'piece',
    'service',
    'license',
  ]

  // Common GST rates in India
  const gstRates = [
    { value: 0, label: '0% (Exempt)' },
    { value: 5, label: '5%' },
    { value: 12, label: '12%' },
    { value: 18, label: '18% (Standard)' },
    { value: 28, label: '28%' },
  ]

  // Common HSN/SAC codes for IT services and software with GST rates
  const commonHsnSacCodes = [
    { code: '998313', description: 'IT consulting and support services', type: 'SAC', gstRate: 18 },
    { code: '998314', description: 'IT design and development services', type: 'SAC', gstRate: 18 },
    { code: '998315', description: 'Hosting and IT infrastructure services', type: 'SAC', gstRate: 18 },
    { code: '998311', description: 'Management consulting services', type: 'SAC', gstRate: 18 },
    { code: '998312', description: 'Business consulting services', type: 'SAC', gstRate: 18 },
    { code: '997331', description: 'Software licensing services', type: 'SAC', gstRate: 18 },
    { code: '999291', description: 'Commercial training services', type: 'SAC', gstRate: 18 },
    { code: '998211', description: 'Financial auditing services', type: 'SAC', gstRate: 18 },
    { code: '998221', description: 'Tax consulting services', type: 'SAC', gstRate: 18 },
    { code: '8523', description: 'Recorded media / packaged software', type: 'HSN', gstRate: 18 },
    { code: '8471', description: 'Computers and data processing machines', type: 'HSN', gstRate: 18 },
    { code: '8473', description: 'Computer parts and accessories', type: 'HSN', gstRate: 18 },
  ]

  // Handle HSN/SAC code selection - auto-update GST rate and isService
  const handleHsnSacChange = (code: string) => {
    handleChange('hsnSacCode', code)

    // Find the code in our list and auto-fill related fields
    const matchedCode = commonHsnSacCodes.find(c => c.code === code)
    if (matchedCode) {
      setFormData(prev => ({
        ...prev,
        hsnSacCode: code,
        defaultGstRate: matchedCode.gstRate,
        isService: matchedCode.type === 'SAC',
      }))
    }
  }

  // Convert HSN/SAC codes to combobox options
  const hsnSacOptions: ComboboxOption[] = useMemo(() =>
    commonHsnSacCodes.map((item) => ({
      value: item.code,
      label: item.description,
      description: `${item.type} - ${item.gstRate}% GST`,
    })),
    []
  )

  // Populate form with existing product data
  useEffect(() => {
    if (product) {
      setFormData({
        companyId: product.companyId,
        name: product.name || '',
        description: product.description || '',
        sku: product.sku || '',
        category: product.category || '',
        type: product.type || 'product',
        unitPrice: product.unitPrice || 0,
        unit: product.unit || '',
        taxRate: product.taxRate || 0,
        isActive: product.isActive ?? true,
        // GST fields
        hsnSacCode: product.hsnSacCode || '',
        isService: product.isService ?? true,
        defaultGstRate: product.defaultGstRate ?? 18,
        cessRate: product.cessRate || 0,
      })
    }
  }, [product])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.name?.trim()) {
      newErrors.name = 'Product name is required'
    }

    if (formData.unitPrice < 0) {
      newErrors.unitPrice = 'Unit price cannot be negative'
    }

    if (formData.taxRate && (formData.taxRate < 0 || formData.taxRate > 100)) {
      newErrors.taxRate = 'Tax rate must be between 0 and 100'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && product) {
        await updateProduct.mutateAsync({ id: product.id, data: formData })
      } else {
        await createProduct.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const handleChange = (field: keyof CreateProductDto, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Product Name */}
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
          Product Name *
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
          placeholder="Enter product name"
        />
        {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
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
      </div>

      {/* Description */}
      <div>
        <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
          Description
        </label>
        <textarea
          id="description"
          rows={3}
          value={formData.description}
          onChange={(e) => handleChange('description', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Product description..."
        />
      </div>

      {/* SKU and Type */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="sku" className="block text-sm font-medium text-gray-700 mb-1">
            SKU
          </label>
          <input
            id="sku"
            type="text"
            value={formData.sku}
            onChange={(e) => handleChange('sku', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="SKU-001"
          />
        </div>
        <div>
          <label htmlFor="type" className="block text-sm font-medium text-gray-700 mb-1">
            Type
          </label>
          <select
            id="type"
            value={formData.type}
            onChange={(e) => handleChange('type', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {productTypes.map((type) => (
              <option key={type.value} value={type.value}>
                {type.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Category */}
      <div>
        <label htmlFor="category" className="block text-sm font-medium text-gray-700 mb-1">
          Category
        </label>
        <input
          id="category"
          type="text"
          list="categories"
          value={formData.category}
          onChange={(e) => handleChange('category', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Select or enter category"
        />
        <datalist id="categories">
          {commonCategories.map((category) => (
            <option key={category} value={category} />
          ))}
        </datalist>
      </div>

      {/* Pricing */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="unitPrice" className="block text-sm font-medium text-gray-700 mb-1">
            Unit Price *
          </label>
          <input
            id="unitPrice"
            type="number"
            step="0.01"
            min="0"
            value={formData.unitPrice}
            onChange={(e) => handleChange('unitPrice', parseFloat(e.target.value) || 0)}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
              errors.unitPrice ? "border-red-500" : "border-gray-300"
            )}
            placeholder="0.00"
          />
          {errors.unitPrice && <p className="text-red-500 text-sm mt-1">{errors.unitPrice}</p>}
        </div>
        <div>
          <label htmlFor="unit" className="block text-sm font-medium text-gray-700 mb-1">
            Unit
          </label>
          <input
            id="unit"
            type="text"
            list="units"
            value={formData.unit}
            onChange={(e) => handleChange('unit', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="Select or enter unit"
          />
          <datalist id="units">
            {commonUnits.map((unit) => (
              <option key={unit} value={unit} />
            ))}
          </datalist>
        </div>
      </div>

      {/* GST Compliance Section */}
      <div className="border-t border-gray-200 pt-4 mt-4">
        <h3 className="text-sm font-medium text-gray-900 mb-3">GST Details (for Domestic Invoices)</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* HSN/SAC Code */}
          <div>
            <label htmlFor="hsnSacCode" className="block text-sm font-medium text-gray-700 mb-1">
              {formData.isService ? 'SAC Code' : 'HSN Code'}
            </label>
            <Combobox
              options={hsnSacOptions}
              value={formData.hsnSacCode || ''}
              onChange={handleHsnSacChange}
              placeholder={formData.isService ? 'Search SAC codes...' : 'Search HSN codes...'}
            />
            <p className="text-xs text-gray-500 mt-1">
              {formData.isService ? 'Service Accounting Code for services' : 'Harmonized System Nomenclature for goods'}
            </p>
          </div>

          {/* Default GST Rate */}
          <div>
            <label htmlFor="defaultGstRate" className="block text-sm font-medium text-gray-700 mb-1">
              Default GST Rate
            </label>
            <select
              id="defaultGstRate"
              value={formData.defaultGstRate}
              onChange={(e) => handleChange('defaultGstRate', parseFloat(e.target.value))}
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

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
          {/* Is Service Toggle */}
          <div className="flex items-center">
            <input
              id="isService"
              type="checkbox"
              checked={formData.isService}
              onChange={(e) => handleChange('isService', e.target.checked)}
              className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
            />
            <label htmlFor="isService" className="ml-2 block text-sm text-gray-900">
              This is a service (uses SAC code)
            </label>
          </div>

          {/* Cess Rate (optional) */}
          <div>
            <label htmlFor="cessRate" className="block text-sm font-medium text-gray-700 mb-1">
              Cess Rate (%) <span className="text-gray-400 font-normal">- optional</span>
            </label>
            <input
              id="cessRate"
              type="number"
              step="0.01"
              min="0"
              max="100"
              value={formData.cessRate}
              onChange={(e) => handleChange('cessRate', parseFloat(e.target.value) || 0)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="0.00"
            />
            <p className="text-xs text-gray-500 mt-1">Additional cess for specific goods (luxury items, tobacco, etc.)</p>
          </div>
        </div>
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
          Product is active
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Product' : 'Create Product'}
        </button>
      </div>
    </form>
  )
}
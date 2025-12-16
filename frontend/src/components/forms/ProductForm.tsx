import { useState, useEffect } from 'react'
import { Product, CreateProductDto, UpdateProductDto } from '@/services/api/types'
import { useCreateProduct, useUpdateProduct } from '@/hooks/api/useProducts'
import { useCompanies } from '@/hooks/api/useCompanies'
import { cn } from '@/lib/utils'

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

      {/* Tax Rate */}
      <div>
        <label htmlFor="taxRate" className="block text-sm font-medium text-gray-700 mb-1">
          Tax Rate (%)
        </label>
        <input
          id="taxRate"
          type="number"
          step="0.01"
          min="0"
          max="100"
          value={formData.taxRate}
          onChange={(e) => handleChange('taxRate', parseFloat(e.target.value) || 0)}
          className={cn(
            "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
            errors.taxRate ? "border-red-500" : "border-gray-300"
          )}
          placeholder="0.00"
        />
        {errors.taxRate && <p className="text-red-500 text-sm mt-1">{errors.taxRate}</p>}
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
import { useState, useEffect } from 'react'
import { Company, CreateCompanyDto, UpdateCompanyDto } from '@/services/api/types'
import { useCreateCompany, useUpdateCompany } from '@/hooks/api/useCompanies'
import { cn } from '@/lib/utils'

interface CompanyFormProps {
  company?: Company
  onSuccess: () => void
  onCancel: () => void
}

export const CompanyForm = ({ company, onSuccess, onCancel }: CompanyFormProps) => {
  const [formData, setFormData] = useState<CreateCompanyDto>({
    name: '',
    logoUrl: '',
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    zipCode: '',
    country: '',
    email: '',
    phone: '',
    website: '',
    taxNumber: '',
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createCompany = useCreateCompany()
  const updateCompany = useUpdateCompany()

  const isEditing = !!company
  const isLoading = createCompany.isPending || updateCompany.isPending

  // Populate form with existing company data
  useEffect(() => {
    if (company) {
      setFormData({
        name: company.name || '',
        logoUrl: company.logoUrl || '',
        addressLine1: company.addressLine1 || '',
        addressLine2: company.addressLine2 || '',
        city: company.city || '',
        state: company.state || '',
        zipCode: company.zipCode || '',
        country: company.country || '',
        email: company.email || '',
        phone: company.phone || '',
        website: company.website || '',
        taxNumber: company.taxNumber || '',
      })
    }
  }, [company])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.name.trim()) {
      newErrors.name = 'Company name is required'
    }

    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address'
    }

    if (formData.website && !formData.website.match(/^https?:\/\/.+/)) {
      newErrors.website = 'Please enter a valid website URL (starting with http:// or https://)'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && company) {
        await updateCompany.mutateAsync({ id: company.id, data: formData })
      } else {
        await createCompany.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const handleChange = (field: keyof CreateCompanyDto, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Company Name */}
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
          Company Name *
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
          placeholder="Enter company name"
        />
        {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
      </div>

      {/* Logo URL */}
      <div>
        <label htmlFor="logoUrl" className="block text-sm font-medium text-gray-700 mb-1">
          Logo URL
        </label>
        <input
          id="logoUrl"
          type="text"
          value={formData.logoUrl}
          onChange={(e) => handleChange('logoUrl', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="https://example.com/logo.png"
        />
      </div>

      {/* Address Fields */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="addressLine1" className="block text-sm font-medium text-gray-700 mb-1">
            Address Line 1
          </label>
          <input
            id="addressLine1"
            type="text"
            value={formData.addressLine1}
            onChange={(e) => handleChange('addressLine1', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="Street address"
          />
        </div>
        <div>
          <label htmlFor="addressLine2" className="block text-sm font-medium text-gray-700 mb-1">
            Address Line 2
          </label>
          <input
            id="addressLine2"
            type="text"
            value={formData.addressLine2}
            onChange={(e) => handleChange('addressLine2', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="Apartment, suite, etc."
          />
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div>
          <label htmlFor="city" className="block text-sm font-medium text-gray-700 mb-1">
            City
          </label>
          <input
            id="city"
            type="text"
            value={formData.city}
            onChange={(e) => handleChange('city', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="City"
          />
        </div>
        <div>
          <label htmlFor="state" className="block text-sm font-medium text-gray-700 mb-1">
            State
          </label>
          <input
            id="state"
            type="text"
            value={formData.state}
            onChange={(e) => handleChange('state', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="State"
          />
        </div>
        <div>
          <label htmlFor="zipCode" className="block text-sm font-medium text-gray-700 mb-1">
            ZIP Code
          </label>
          <input
            id="zipCode"
            type="text"
            value={formData.zipCode}
            onChange={(e) => handleChange('zipCode', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="ZIP"
          />
        </div>
      </div>

      {/* Country */}
      <div>
        <label htmlFor="country" className="block text-sm font-medium text-gray-700 mb-1">
          Country
        </label>
        <input
          id="country"
          type="text"
          value={formData.country}
          onChange={(e) => handleChange('country', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Country"
        />
      </div>

      {/* Contact Information */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
            Email
          </label>
          <input
            id="email"
            type="email"
            value={formData.email}
            onChange={(e) => handleChange('email', e.target.value)}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
              errors.email ? "border-red-500" : "border-gray-300"
            )}
            placeholder="company@example.com"
          />
          {errors.email && <p className="text-red-500 text-sm mt-1">{errors.email}</p>}
        </div>
        <div>
          <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
            Phone
          </label>
          <input
            id="phone"
            type="tel"
            value={formData.phone}
            onChange={(e) => handleChange('phone', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="+1 (555) 123-4567"
          />
        </div>
      </div>

      {/* Website and Tax Number */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="website" className="block text-sm font-medium text-gray-700 mb-1">
            Website
          </label>
          <input
            id="website"
            type="url"
            value={formData.website}
            onChange={(e) => handleChange('website', e.target.value)}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
              errors.website ? "border-red-500" : "border-gray-300"
            )}
            placeholder="https://www.example.com"
          />
          {errors.website && <p className="text-red-500 text-sm mt-1">{errors.website}</p>}
        </div>
        <div>
          <label htmlFor="taxNumber" className="block text-sm font-medium text-gray-700 mb-1">
            Tax Number
          </label>
          <input
            id="taxNumber"
            type="text"
            value={formData.taxNumber}
            onChange={(e) => handleChange('taxNumber', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="TAX123456"
          />
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-4">
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Company' : 'Create Company'}
        </button>
      </div>
    </form>
  )
}
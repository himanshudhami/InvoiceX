import { useState, useEffect } from 'react'
import { Customer, CreateCustomerDto, UpdateCustomerDto } from '@/services/api/types'
import { useCreateCustomer, useUpdateCustomer } from '@/hooks/api/useCustomers'
import { useCompanies } from '@/hooks/api/useCompanies'
import { cn } from '@/lib/utils'

interface CustomerFormProps {
  customer?: Customer
  onSuccess: () => void
  onCancel: () => void
}

export const CustomerForm = ({ customer, onSuccess, onCancel }: CustomerFormProps) => {
  const [formData, setFormData] = useState<CreateCustomerDto>({
    name: '',
    companyName: '',
    email: '',
    phone: '',
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    zipCode: '',
    country: '',
    taxNumber: '',
    notes: '',
    creditLimit: 0,
    paymentTerms: 30,
    isActive: true,
    // GST Compliance
    customerType: 'b2b',
    gstin: '',
    gstStateCode: '',
    isGstRegistered: false,
    panNumber: '',
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createCustomer = useCreateCustomer()
  const updateCustomer = useUpdateCustomer()
  const { data: companies = [] } = useCompanies()

  const isEditing = !!customer
  const isLoading = createCustomer.isPending || updateCustomer.isPending

  // Populate form with existing customer data
  useEffect(() => {
    if (customer) {
      setFormData({
        companyId: customer.companyId,
        name: customer.name || '',
        companyName: customer.companyName || '',
        email: customer.email || '',
        phone: customer.phone || '',
        addressLine1: customer.addressLine1 || '',
        addressLine2: customer.addressLine2 || '',
        city: customer.city || '',
        state: customer.state || '',
        zipCode: customer.zipCode || '',
        country: customer.country || '',
        taxNumber: customer.taxNumber || '',
        notes: customer.notes || '',
        creditLimit: customer.creditLimit || 0,
        paymentTerms: customer.paymentTerms || 30,
        isActive: customer.isActive ?? true,
        // GST Compliance
        customerType: customer.customerType || 'b2b',
        gstin: customer.gstin || '',
        gstStateCode: customer.gstStateCode || '',
        isGstRegistered: customer.isGstRegistered ?? false,
        panNumber: customer.panNumber || '',
      })
    }
  }, [customer])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.name?.trim()) {
      newErrors.name = 'Customer name is required'
    }

    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address'
    }

    if (formData.creditLimit && formData.creditLimit < 0) {
      newErrors.creditLimit = 'Credit limit cannot be negative'
    }

    if (formData.paymentTerms && formData.paymentTerms < 0) {
      newErrors.paymentTerms = 'Payment terms cannot be negative'
    }

    // GSTIN validation (15 characters: 2 state + 10 PAN + 1 entity + 1 check + Z)
    if (formData.gstin && !/^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$/.test(formData.gstin)) {
      newErrors.gstin = 'Invalid GSTIN format (e.g., 27AAACP1234C1Z5)'
    }

    // PAN validation (10 characters: 5 letters + 4 digits + 1 letter)
    if (formData.panNumber && !/^[A-Z]{5}[0-9]{4}[A-Z]{1}$/.test(formData.panNumber)) {
      newErrors.panNumber = 'Invalid PAN format (e.g., AAACP1234C)'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && customer) {
        await updateCustomer.mutateAsync({ id: customer.id, data: formData })
      } else {
        await createCustomer.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const handleChange = (field: keyof CreateCustomerDto, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Customer Name */}
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
          Customer Name *
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
          placeholder="Enter customer name"
        />
        {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
      </div>

      {/* Company Selection & Company Name */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
        <div>
          <label htmlFor="companyName" className="block text-sm font-medium text-gray-700 mb-1">
            Company Name
          </label>
          <input
            id="companyName"
            type="text"
            value={formData.companyName}
            onChange={(e) => handleChange('companyName', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="Customer's company name"
          />
        </div>
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
            placeholder="customer@example.com"
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

      {/* Country and Tax Number */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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

      {/* Business Settings */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="creditLimit" className="block text-sm font-medium text-gray-700 mb-1">
            Credit Limit
          </label>
          <input
            id="creditLimit"
            type="number"
            step="0.01"
            min="0"
            value={formData.creditLimit}
            onChange={(e) => handleChange('creditLimit', parseFloat(e.target.value) || 0)}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
              errors.creditLimit ? "border-red-500" : "border-gray-300"
            )}
            placeholder="0.00"
          />
          {errors.creditLimit && <p className="text-red-500 text-sm mt-1">{errors.creditLimit}</p>}
        </div>
        <div>
          <label htmlFor="paymentTerms" className="block text-sm font-medium text-gray-700 mb-1">
            Payment Terms (days)
          </label>
          <input
            id="paymentTerms"
            type="number"
            min="0"
            value={formData.paymentTerms}
            onChange={(e) => handleChange('paymentTerms', parseInt(e.target.value) || 0)}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
              errors.paymentTerms ? "border-red-500" : "border-gray-300"
            )}
            placeholder="30"
          />
          {errors.paymentTerms && <p className="text-red-500 text-sm mt-1">{errors.paymentTerms}</p>}
        </div>
      </div>

      {/* Indian Tax Compliance Section */}
      <div className="border-t pt-4 mt-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Indian Tax Compliance</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="customerType" className="block text-sm font-medium text-gray-700 mb-1">
              Customer Type
            </label>
            <select
              id="customerType"
              value={formData.customerType}
              onChange={(e) => handleChange('customerType', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="b2b">B2B (Business to Business)</option>
              <option value="b2c">B2C (Business to Consumer)</option>
              <option value="overseas">Overseas</option>
              <option value="sez">SEZ (Special Economic Zone)</option>
            </select>
          </div>
          <div className="flex items-center pt-6">
            <input
              id="isGstRegistered"
              type="checkbox"
              checked={formData.isGstRegistered}
              onChange={(e) => handleChange('isGstRegistered', e.target.checked)}
              className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
            />
            <label htmlFor="isGstRegistered" className="ml-2 block text-sm text-gray-900">
              GST Registered
            </label>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
          <div>
            <label htmlFor="gstin" className="block text-sm font-medium text-gray-700 mb-1">
              GSTIN
            </label>
            <input
              id="gstin"
              type="text"
              value={formData.gstin}
              onChange={(e) => handleChange('gstin', e.target.value.toUpperCase())}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.gstin ? "border-red-500" : "border-gray-300"
              )}
              placeholder="27AAACP1234C1Z5"
              maxLength={15}
            />
            {errors.gstin && <p className="text-red-500 text-sm mt-1">{errors.gstin}</p>}
          </div>
          <div>
            <label htmlFor="gstStateCode" className="block text-sm font-medium text-gray-700 mb-1">
              GST State Code
            </label>
            <input
              id="gstStateCode"
              type="text"
              value={formData.gstStateCode}
              onChange={(e) => handleChange('gstStateCode', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="27"
              maxLength={2}
            />
          </div>
        </div>

        <div className="mt-4">
          <label htmlFor="panNumber" className="block text-sm font-medium text-gray-700 mb-1">
            PAN Number
          </label>
          <input
            id="panNumber"
            type="text"
            value={formData.panNumber}
            onChange={(e) => handleChange('panNumber', e.target.value.toUpperCase())}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring md:w-1/2",
              errors.panNumber ? "border-red-500" : "border-gray-300"
            )}
            placeholder="AAACP1234C"
            maxLength={10}
          />
          {errors.panNumber && <p className="text-red-500 text-sm mt-1">{errors.panNumber}</p>}
        </div>
      </div>

      {/* Notes */}
      <div>
        <label htmlFor="notes" className="block text-sm font-medium text-gray-700 mb-1">
          Notes
        </label>
        <textarea
          id="notes"
          rows={3}
          value={formData.notes}
          onChange={(e) => handleChange('notes', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Additional notes about this customer..."
        />
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
          Customer is active
        </label>
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Customer' : 'Create Customer'}
        </button>
      </div>
    </form>
  )
}
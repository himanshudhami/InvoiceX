import { useState, useEffect } from 'react'
import { Invoice, CreateInvoiceDto, UpdateInvoiceDto } from '@/services/api/types'
import { useCreateInvoice, useUpdateInvoice } from '@/hooks/api/useInvoices'
import { useCustomers } from '@/hooks/api/useCustomers'
import { useCompanies } from '@/hooks/api/useCompanies'
import { cn } from '@/lib/utils'
import { INVOICE_STATUSES } from '@/lib/constants'
import { CurrencySelect } from '@/components/ui/currency-select'
import { InvoiceStatusSelect } from '@/components/ui/status-select'

interface InvoiceFormProps {
  invoice?: Invoice
  onSuccess: () => void
  onCancel: () => void
}

export const InvoiceForm = ({ invoice, onSuccess, onCancel }: InvoiceFormProps) => {
  const [formData, setFormData] = useState<CreateInvoiceDto>({
    companyId: '',
    customerId: '',
    invoiceNumber: '',
    invoiceDate: new Date().toISOString().split('T')[0],
    dueDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0], // 30 days from now
    status: 'draft',
    subtotal: 0,
    taxAmount: 0,
    discountAmount: 0,
    totalAmount: 0,
    paidAmount: 0,
    currency: 'USD',
    exchangeRate: 0,
    notes: '',
    terms: '',
    paymentInstructions: '',
    poNumber: '',
    projectName: '',
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createInvoice = useCreateInvoice()
  const updateInvoice = useUpdateInvoice()
  // Scope customers to the selected company so "Bill To" reflects the chosen "From"
  const { data: customers = [] } = useCustomers(formData.companyId || undefined)
  const { data: companies = [] } = useCompanies()

  const isEditing = !!invoice
  const isLoading = createInvoice.isPending || updateInvoice.isPending



  // Generate next invoice number if creating new invoice
  useEffect(() => {
    if (!invoice) {
      const today = new Date()
      const year = today.getFullYear()
      const month = String(today.getMonth() + 1).padStart(2, '0')
      const random = Math.floor(Math.random() * 1000).toString().padStart(3, '0')
      const invoiceNumber = `INV-${year}${month}-${random}`
      
      setFormData(prev => ({ ...prev, invoiceNumber }))
    }
  }, [invoice])

  // Populate form with existing invoice data
  useEffect(() => {
    if (invoice) {
      setFormData({
        companyId: invoice.companyId || '',
        customerId: invoice.customerId || '',
        invoiceNumber: invoice.invoiceNumber || '',
        invoiceDate: invoice.invoiceDate?.split('T')[0] || '',
        dueDate: invoice.dueDate?.split('T')[0] || '',
        status: invoice.status || 'draft',
        subtotal: invoice.subtotal || 0,
        taxAmount: invoice.taxAmount || 0,
        discountAmount: invoice.discountAmount || 0,
        totalAmount: invoice.totalAmount || 0,
        paidAmount: invoice.paidAmount || 0,
        currency: invoice.currency || 'USD',
        exchangeRate: invoice.exchangeRate || 0,
        notes: invoice.notes || '',
        terms: invoice.terms || '',
        paymentInstructions: invoice.paymentInstructions || '',
        poNumber: invoice.poNumber || '',
        projectName: invoice.projectName || '',
      })
    }
  }, [invoice])

  // When company changes, clear customer if it belongs to a different company
  useEffect(() => {
    if (!formData.customerId || !formData.companyId) return
    const customer = customers.find(c => c.id === formData.customerId)
    if (customer && customer.companyId && customer.companyId !== formData.companyId) {
      setFormData(prev => ({ ...prev, customerId: '' }))
    }
  }, [formData.companyId, formData.customerId, customers])

  // Recalculate total when subtotal, tax, or discount changes
  useEffect(() => {
    const subtotal = formData.subtotal || 0
    const taxAmount = formData.taxAmount || 0
    const discountAmount = formData.discountAmount || 0
    const totalAmount = subtotal + taxAmount - discountAmount
    
    if (totalAmount !== formData.totalAmount) {
      setFormData(prev => ({ ...prev, totalAmount: Math.max(0, totalAmount) }))
    }
  }, [formData.subtotal, formData.taxAmount, formData.discountAmount])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.invoiceNumber?.trim()) {
      newErrors.invoiceNumber = 'Invoice number is required'
    }

    if (!formData.invoiceDate) {
      newErrors.invoiceDate = 'Invoice date is required'
    }

    if (!formData.dueDate) {
      newErrors.dueDate = 'Due date is required'
    }

    if (formData.invoiceDate && formData.dueDate && formData.dueDate < formData.invoiceDate) {
      newErrors.dueDate = 'Due date must be after invoice date'
    }

    if (formData.subtotal < 0) {
      newErrors.subtotal = 'Subtotal cannot be negative'
    }

    if (formData.taxAmount && formData.taxAmount < 0) {
      newErrors.taxAmount = 'Tax amount cannot be negative'
    }

    if (formData.discountAmount && formData.discountAmount < 0) {
      newErrors.discountAmount = 'Discount amount cannot be negative'
    }

    if (formData.currency?.toUpperCase() !== 'INR' && (!formData.exchangeRate || formData.exchangeRate <= 0)) {
      newErrors.exchangeRate = 'Exchange rate is required for foreign currency invoices'
    }

    if (formData.paidAmount && formData.paidAmount < 0) {
      newErrors.paidAmount = 'Paid amount cannot be negative'
    }

    if (formData.paidAmount && formData.paidAmount > formData.totalAmount) {
      newErrors.paidAmount = 'Paid amount cannot exceed total amount'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && invoice) {
        await updateInvoice.mutateAsync({ id: invoice.id, data: formData })
      } else {
        await createInvoice.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const handleChange = (field: keyof CreateInvoiceDto, value: string | number) => {
    if (field === 'companyId') {
      setFormData(prev => ({ ...prev, companyId: value as string, customerId: '' }))
      if (errors.customerId) {
        setErrors(prev => ({ ...prev, customerId: '' }))
      }
    } else {
      setFormData(prev => ({ ...prev, [field]: value }))
      // Clear error when user starts typing
      if (errors[field]) {
        setErrors(prev => ({ ...prev, [field]: '' }))
      }
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Basic Information */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900 border-b border-gray-200 pb-2">
          Basic Information
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Invoice Number */}
          <div>
            <label htmlFor="invoiceNumber" className="block text-sm font-medium text-gray-700 mb-1">
              Invoice Number *
            </label>
            <input
              id="invoiceNumber"
              type="text"
              value={formData.invoiceNumber}
              onChange={(e) => handleChange('invoiceNumber', e.target.value)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.invoiceNumber ? "border-red-500" : "border-gray-300"
              )}
              placeholder="INV-2024-001"
            />
            {errors.invoiceNumber && <p className="text-red-500 text-sm mt-1">{errors.invoiceNumber}</p>}
          </div>

          {/* Status */}
          <div>
            <label htmlFor="status" className="block text-sm font-medium text-gray-700 mb-1">
              Status
            </label>
            <InvoiceStatusSelect
            value={formData.status}
            onChange={(value) => handleChange('status', value)}
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Company */}
          <div>
            <label htmlFor="companyId" className="block text-sm font-medium text-gray-700 mb-1">
              Company
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

          {/* Customer */}
          <div>
            <label htmlFor="customerId" className="block text-sm font-medium text-gray-700 mb-1">
              Customer
            </label>
            <select
              id="customerId"
              value={formData.customerId || ''}
              onChange={(e) => handleChange('customerId', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">Select a customer (optional)</option>
              {customers.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.name} {customer.companyName && `(${customer.companyName})`}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Invoice Date */}
          <div>
            <label htmlFor="invoiceDate" className="block text-sm font-medium text-gray-700 mb-1">
              Invoice Date *
            </label>
            <input
              id="invoiceDate"
              type="date"
              value={formData.invoiceDate}
              onChange={(e) => handleChange('invoiceDate', e.target.value)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.invoiceDate ? "border-red-500" : "border-gray-300"
              )}
            />
            {errors.invoiceDate && <p className="text-red-500 text-sm mt-1">{errors.invoiceDate}</p>}
          </div>

          {/* Due Date */}
          <div>
            <label htmlFor="dueDate" className="block text-sm font-medium text-gray-700 mb-1">
              Due Date *
            </label>
            <input
              id="dueDate"
              type="date"
              value={formData.dueDate}
              onChange={(e) => handleChange('dueDate', e.target.value)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.dueDate ? "border-red-500" : "border-gray-300"
              )}
            />
            {errors.dueDate && <p className="text-red-500 text-sm mt-1">{errors.dueDate}</p>}
          </div>

          {/* Currency */}
          <div>
            <label htmlFor="currency" className="block text-sm font-medium text-gray-700 mb-1">
              Currency
            </label>
            <CurrencySelect
            value={formData.currency}
            onChange={(value) => handleChange('currency', value)}
            />
          </div>

          {formData.currency?.toUpperCase() !== 'INR' && (
            <div>
              <label htmlFor="exchangeRate" className="block text-sm font-medium text-gray-700 mb-1">
                Invoice Exchange Rate (INR)
              </label>
              <input
                id="exchangeRate"
                type="number"
                min="0.0001"
                step="0.0001"
                value={formData.exchangeRate || ''}
                onChange={(e) => handleChange('exchangeRate', parseFloat(e.target.value) || 0)}
                className={cn(
                  "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                  errors.exchangeRate ? "border-red-500" : "border-gray-300"
                )}
                placeholder="RBI reference rate on invoice date"
              />
              {errors.exchangeRate && <p className="text-red-500 text-sm mt-1">{errors.exchangeRate}</p>}
            </div>
          )}
        </div>
      </div>

      {/* Financial Information */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900 border-b border-gray-200 pb-2">
          Financial Details
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Subtotal */}
          <div>
            <label htmlFor="subtotal" className="block text-sm font-medium text-gray-700 mb-1">
              Subtotal *
            </label>
            <input
              id="subtotal"
              type="number"
              step="0.01"
              min="0"
              value={formData.subtotal}
              onChange={(e) => handleChange('subtotal', parseFloat(e.target.value) || 0)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.subtotal ? "border-red-500" : "border-gray-300"
              )}
              placeholder="0.00"
            />
            {errors.subtotal && <p className="text-red-500 text-sm mt-1">{errors.subtotal}</p>}
          </div>

          {/* Tax Amount */}
          <div>
            <label htmlFor="taxAmount" className="block text-sm font-medium text-gray-700 mb-1">
              Tax Amount
            </label>
            <input
              id="taxAmount"
              type="number"
              step="0.01"
              min="0"
              value={formData.taxAmount || ''}
              onChange={(e) => handleChange('taxAmount', parseFloat(e.target.value) || 0)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.taxAmount ? "border-red-500" : "border-gray-300"
              )}
              placeholder="0.00"
            />
            {errors.taxAmount && <p className="text-red-500 text-sm mt-1">{errors.taxAmount}</p>}
          </div>

          {/* Discount Amount */}
          <div>
            <label htmlFor="discountAmount" className="block text-sm font-medium text-gray-700 mb-1">
              Discount Amount
            </label>
            <input
              id="discountAmount"
              type="number"
              step="0.01"
              min="0"
              value={formData.discountAmount || ''}
              onChange={(e) => handleChange('discountAmount', parseFloat(e.target.value) || 0)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.discountAmount ? "border-red-500" : "border-gray-300"
              )}
              placeholder="0.00"
            />
            {errors.discountAmount && <p className="text-red-500 text-sm mt-1">{errors.discountAmount}</p>}
          </div>

          {/* Total Amount (Read-only, calculated) */}
          <div>
            <label htmlFor="totalAmount" className="block text-sm font-medium text-gray-700 mb-1">
              Total Amount
            </label>
            <input
              id="totalAmount"
              type="number"
              step="0.01"
              value={formData.totalAmount}
              readOnly
              className="w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 cursor-not-allowed"
              placeholder="0.00"
            />
          </div>

          {/* Paid Amount */}
          <div>
            <label htmlFor="paidAmount" className="block text-sm font-medium text-gray-700 mb-1">
              Paid Amount
            </label>
            <input
              id="paidAmount"
              type="number"
              step="0.01"
              min="0"
              max={formData.totalAmount}
              value={formData.paidAmount || ''}
              onChange={(e) => handleChange('paidAmount', parseFloat(e.target.value) || 0)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.paidAmount ? "border-red-500" : "border-gray-300"
              )}
              placeholder="0.00"
            />
            {errors.paidAmount && <p className="text-red-500 text-sm mt-1">{errors.paidAmount}</p>}
          </div>
        </div>
      </div>

      {/* Additional Information */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900 border-b border-gray-200 pb-2">
          Additional Information
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* PO Number */}
          <div>
            <label htmlFor="poNumber" className="block text-sm font-medium text-gray-700 mb-1">
              PO Number
            </label>
            <input
              id="poNumber"
              type="text"
              value={formData.poNumber || ''}
              onChange={(e) => handleChange('poNumber', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="PO-2024-001"
            />
          </div>

          {/* Project Name */}
          <div>
            <label htmlFor="projectName" className="block text-sm font-medium text-gray-700 mb-1">
              Project Name
            </label>
            <input
              id="projectName"
              type="text"
              value={formData.projectName || ''}
              onChange={(e) => handleChange('projectName', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Website Development"
            />
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
            value={formData.notes || ''}
            onChange={(e) => handleChange('notes', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="Additional notes..."
          />
        </div>

        {/* Terms */}
        <div>
          <label htmlFor="terms" className="block text-sm font-medium text-gray-700 mb-1">
            Terms & Conditions
          </label>
          <textarea
            id="terms"
            rows={3}
            value={formData.terms || ''}
            onChange={(e) => handleChange('terms', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="Payment terms and conditions..."
          />
        </div>

        {/* Payment Instructions */}
        <div>
          <label htmlFor="paymentInstructions" className="block text-sm font-medium text-gray-700 mb-1">
            Payment Instructions
          </label>
          <textarea
            id="paymentInstructions"
            rows={3}
            value={formData.paymentInstructions || ''}
            onChange={(e) => handleChange('paymentInstructions', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="How customers should pay this invoice (bank details, etc.)..."
          />
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Invoice' : 'Create Invoice'}
        </button>
      </div>
    </form>
  )
}

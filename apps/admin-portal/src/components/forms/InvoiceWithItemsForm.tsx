import { useState, useEffect } from 'react'
import { Invoice, CreateInvoiceDto, UpdateInvoiceDto, Product } from '@/services/api/types'
import { useCreateInvoice, useUpdateInvoice } from '@/hooks/api/useInvoices'
import { useCustomers } from '@/hooks/api/useCustomers'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useProducts } from '@/hooks/api/useProducts'
import { useApplyTags, useTransactionTags } from '@/features/tags/hooks'
import { cn } from '@/lib/utils'
import { Trash2, Plus, Tags } from 'lucide-react'
import { TagPicker } from '@/components/ui/TagPicker'

interface LineItem {
  id: string;
  productId: string;
  description: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
  lineTotal: number;
}

interface InvoiceWithItemsFormProps {
  invoice?: Invoice;
  onSuccess: () => void;
  onCancel: () => void;
}

export const InvoiceWithItemsForm = ({ invoice, onSuccess, onCancel }: InvoiceWithItemsFormProps) => {
  const [formData, setFormData] = useState<CreateInvoiceDto>({
    companyId: '',
    partyId: '',
    invoiceNumber: '',
    invoiceDate: new Date().toISOString().split('T')[0],
    dueDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
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
    poNumber: '',
    projectName: '',
  })

  const [lineItems, setLineItems] = useState<LineItem[]>([])
  const [selectedTags, setSelectedTags] = useState<{ tagId: string; allocationPercentage?: number }[]>([])
  const [errors, setErrors] = useState<Record<string, string>>({})

  const createInvoice = useCreateInvoice()
  const updateInvoice = useUpdateInvoice()
  const applyTags = useApplyTags()

  // Load existing tags when editing
  const { data: existingTags = [] } = useTransactionTags(
    invoice?.id || '',
    'invoice',
    !!invoice?.id
  )
  // Scope customers to selected company for the bill-to dropdown
  const { data: customers = [] } = useCustomers(formData.companyId || undefined)
  const { data: companies = [] } = useCompanies()
  const { data: products = [] } = useProducts()

  const isEditing = !!invoice
  const isLoading = createInvoice.isPending || updateInvoice.isPending

  const invoiceStatuses = [
    { value: 'draft', label: 'Draft' },
    { value: 'sent', label: 'Sent' },
    { value: 'viewed', label: 'Viewed' },
    { value: 'paid', label: 'Paid' },
    { value: 'overdue', label: 'Overdue' },
    { value: 'cancelled', label: 'Cancelled' },
  ]

  const currencies = [
    { value: 'USD', label: 'USD ($)' },
    { value: 'EUR', label: 'EUR (€)' },
    { value: 'GBP', label: 'GBP (£)' },
    { value: 'CAD', label: 'CAD ($)' },
    { value: 'AUD', label: 'AUD ($)' },
  ]

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

  // Clear customer if it doesn't belong to the selected company
  useEffect(() => {
    if (!formData.partyId || !formData.companyId) return
    const customer = customers.find(c => c.id === formData.partyId)
    if (customer && customer.companyId && customer.companyId !== formData.companyId) {
      setFormData(prev => ({ ...prev, partyId: '' }))
    }
  }, [formData.companyId, formData.partyId, customers])

  // Populate form with existing invoice data
  useEffect(() => {
    if (invoice) {
      setFormData({
        companyId: invoice.companyId || '',
        partyId: invoice.partyId || '',
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
        poNumber: invoice.poNumber || '',
        projectName: invoice.projectName || '',
      })
    }
  }, [invoice])

  // Populate tags when editing
  useEffect(() => {
    if (existingTags.length > 0) {
      setSelectedTags(existingTags.map(t => ({
        tagId: t.tagId,
        allocationPercentage: t.allocationPercentage,
      })))
    }
  }, [existingTags])

  // Recalculate totals when line items change
  useEffect(() => {
    const subtotal = lineItems.reduce((sum, item) => sum + item.lineTotal, 0)
    const taxAmount = lineItems.reduce((sum, item) => sum + (item.lineTotal * item.taxRate / 100), 0)
    const totalAmount = subtotal + taxAmount - (formData.discountAmount || 0)
    
    setFormData(prev => ({ 
      ...prev, 
      subtotal,
      taxAmount,
      totalAmount: Math.max(0, totalAmount)
    }))
  }, [lineItems, formData.discountAmount])

  const addLineItem = () => {
    const newItem: LineItem = {
      id: `temp-${Date.now()}`,
      productId: '',
      description: '',
      quantity: 1,
      unitPrice: 0,
      taxRate: 0,
      lineTotal: 0,
    }
    setLineItems(prev => [...prev, newItem])
  }

  const removeLineItem = (id: string) => {
    setLineItems(prev => prev.filter(item => item.id !== id))
  }

  const updateLineItem = (id: string, field: keyof LineItem, value: string | number) => {
    setLineItems(prev => prev.map(item => {
      if (item.id !== id) return item
      
      const updated = { ...item, [field]: value }
      
      // Auto-populate from selected product
      if (field === 'productId' && value) {
        const product = products.find(p => p.id === value)
        if (product) {
          updated.description = product.name
          updated.unitPrice = product.unitPrice
          updated.taxRate = product.taxRate || 0
        }
      }
      
      // Recalculate line total
      updated.lineTotal = updated.quantity * updated.unitPrice
      
      return updated
    }))
  }

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

    if (formData.currency?.toUpperCase() !== 'INR' && (!formData.exchangeRate || formData.exchangeRate <= 0)) {
      newErrors.exchangeRate = 'Exchange rate is required for foreign currency invoices'
    }

    if (lineItems.length === 0) {
      newErrors.lineItems = 'At least one line item is required'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      let invoiceId: string

      if (isEditing && invoice) {
        await updateInvoice.mutateAsync({ id: invoice.id, data: formData })
        invoiceId = invoice.id
      } else {
        const created = await createInvoice.mutateAsync(formData)
        invoiceId = created.id
      }

      // Apply tags if any selected
      if (selectedTags.length > 0) {
        await applyTags.mutateAsync({
          transactionId: invoiceId,
          transactionType: 'invoice',
          tags: selectedTags.map(t => ({
            tagId: t.tagId,
            allocationPercentage: t.allocationPercentage,
          })),
        })
      }

      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const handleChange = (field: keyof CreateInvoiceDto, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-8">
      {/* Basic Invoice Information */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900 border-b border-gray-200 pb-2">
          Invoice Details
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
            />
            {errors.invoiceNumber && <p className="text-red-500 text-sm mt-1">{errors.invoiceNumber}</p>}
          </div>

          <div>
            <label htmlFor="status" className="block text-sm font-medium text-gray-700 mb-1">
              Status
            </label>
            <select
              id="status"
              value={formData.status}
              onChange={(e) => handleChange('status', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {invoiceStatuses.map((status) => (
                <option key={status.value} value={status.value}>
                  {status.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="partyId" className="block text-sm font-medium text-gray-700 mb-1">
              Customer
            </label>
            <select
              id="partyId"
              value={formData.partyId || ''}
              onChange={(e) => handleChange('partyId', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">Select a customer</option>
              {customers.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.name} {customer.companyName && `(${customer.companyName})`}
                </option>
              ))}
            </select>
          </div>

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
              <option value="">Select a company</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
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

          <div>
            <label htmlFor="currency" className="block text-sm font-medium text-gray-700 mb-1">
              Currency
            </label>
            <select
              id="currency"
              value={formData.currency}
              onChange={(e) => handleChange('currency', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {currencies.map((currency) => (
                <option key={currency.value} value={currency.value}>
                  {currency.label}
                </option>
              ))}
            </select>
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

      {/* Line Items */}
      <div className="space-y-4">
        <div className="flex justify-between items-center">
          <h3 className="text-lg font-medium text-gray-900">Line Items</h3>
          <button
            type="button"
            onClick={addLineItem}
            className="flex items-center px-3 py-2 text-sm font-medium text-primary border border-primary/20 rounded-md hover:bg-primary/5 focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <Plus className="w-4 h-4 mr-2" />
            Add Item
          </button>
        </div>

        {errors.lineItems && (
          <p className="text-red-500 text-sm">{errors.lineItems}</p>
        )}

        <div className="space-y-4">
          {lineItems.map((item, index) => (
            <div key={item.id} className="p-4 border border-gray-200 rounded-lg bg-gray-50">
              <div className="flex justify-between items-start mb-4">
                <h4 className="font-medium text-gray-900">Item {index + 1}</h4>
                <button
                  type="button"
                  onClick={() => removeLineItem(item.id)}
                  className="text-red-600 hover:text-red-800 p-1"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Product
                  </label>
                  <select
                    value={item.productId}
                    onChange={(e) => updateLineItem(item.id, 'productId', e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value="">Select a product</option>
                    {products.map((product) => (
                      <option key={product.id} value={product.id}>
                        {product.name} - ${product.unitPrice}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Description
                  </label>
                  <input
                    type="text"
                    value={item.description}
                    onChange={(e) => updateLineItem(item.id, 'description', e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                    placeholder="Item description"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Quantity
                  </label>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={item.quantity}
                    onChange={(e) => updateLineItem(item.id, 'quantity', parseFloat(e.target.value) || 0)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Unit Price
                  </label>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={item.unitPrice}
                    onChange={(e) => updateLineItem(item.id, 'unitPrice', parseFloat(e.target.value) || 0)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Tax Rate (%)
                  </label>
                  <input
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    value={item.taxRate}
                    onChange={(e) => updateLineItem(item.id, 'taxRate', parseFloat(e.target.value) || 0)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Line Total
                  </label>
                  <input
                    type="text"
                    value={`$${item.lineTotal.toFixed(2)}`}
                    readOnly
                    className="w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-100 cursor-not-allowed"
                  />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Totals Summary */}
      <div className="bg-gray-50 p-4 rounded-lg">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
          <div className="flex justify-between">
            <span className="text-gray-600">Subtotal:</span>
            <span className="font-medium">${formData.subtotal.toFixed(2)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-600">Tax:</span>
            <span className="font-medium">${formData.taxAmount.toFixed(2)}</span>
          </div>
          <div className="flex justify-between font-semibold text-lg">
            <span>Total:</span>
            <span>${formData.totalAmount.toFixed(2)}</span>
          </div>
        </div>
      </div>

      {/* Tags */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900 border-b border-gray-200 pb-2 flex items-center gap-2">
          <Tags className="w-5 h-5" />
          Tags & Classification
        </h3>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Assign Tags
          </label>
          <TagPicker
            value={selectedTags}
            onChange={setSelectedTags}
            transactionAmount={formData.totalAmount}
            placeholder="Click to add tags (department, project, client...)"
          />
          <p className="text-xs text-gray-500 mt-1">
            Tags help categorize this invoice for reporting and analysis
          </p>
        </div>
      </div>

      {/* Additional Information */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900 border-b border-gray-200 pb-2">
          Additional Information
        </h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
            />
          </div>

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
            />
          </div>
        </div>

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
          />
        </div>

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
          />
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
        <button
          type="button"
          onClick={onCancel}
          disabled={isLoading}
          className="px-6 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-6 py-2 text-sm font-medium text-primary-foreground bg-primary border border-transparent rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
        >
          {isLoading ? 'Saving...' : isEditing ? 'Update Invoice' : 'Create Invoice'}
        </button>
      </div>
    </form>
  )
}

import { useState, useEffect } from 'react'
import { Quote, CreateQuoteDto, QuoteItem } from '@/services/api/types'
import { useCreateQuote, useUpdateQuote, useQuoteItems, useCreateQuoteItem, useUpdateQuoteItem, useDeleteQuoteItem } from '@/hooks/api/useQuotes'
import { useCustomers } from '@/hooks/api/useCustomers'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useProducts } from '@/hooks/api/useProducts'
import { cn } from '@/lib/utils'

interface QuoteFormProps {
  quote?: Quote
  onSuccess: () => void
  onCancel: () => void
}

export const QuoteForm = ({ quote, onSuccess, onCancel }: QuoteFormProps) => {
  const [formData, setFormData] = useState<CreateQuoteDto>({
    companyId: '',
    customerId: '',
    quoteNumber: '',
    quoteDate: new Date().toISOString().split('T')[0],
    expiryDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0], // 30 days from now
    status: 'draft',
    subtotal: 0,
    discountType: 'percentage',
    discountValue: 0,
    discountAmount: 0,
    taxAmount: 0,
    totalAmount: 0,
    currency: 'USD',
    notes: '',
    terms: '',
    paymentInstructions: '',
    poNumber: '',
    projectName: '',
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const [quoteItems, setQuoteItems] = useState<Omit<QuoteItem, 'id' | 'quoteId' | 'createdAt' | 'updatedAt'>[]>([])

  const handleProductSelect = (index: number, productId: string) => {
    const product = products.find(p => p.id === productId)
    if (product) {
      setQuoteItems(prev => prev.map((item, i) => {
        if (i === index) {
          const updatedItem = {
            ...item,
            productId,
            description: product.name,
            unitPrice: product.unitPrice,
            taxRate: product.taxRate || 0,
          }
          // Recalculate line total
          updatedItem.lineTotal = calculateLineTotal(
            updatedItem.quantity,
            updatedItem.unitPrice,
            updatedItem.discountRate,
            updatedItem.taxRate
          )
          return updatedItem
        }
        return item
      }))
    }
  }

  const createQuote = useCreateQuote()
  const updateQuote = useUpdateQuote()
  const createQuoteItem = useCreateQuoteItem()
  const updateQuoteItem = useUpdateQuoteItem()
  const deleteQuoteItem = useDeleteQuoteItem()

  const { data: customers = [] } = useCustomers()
  const { data: companies = [] } = useCompanies()
  const { data: products = [] } = useProducts()
  const { data: existingQuoteItems = [] } = useQuoteItems(quote?.id || '')

  const isEditing = !!quote
  const isLoading = createQuote.isPending || updateQuote.isPending || createQuoteItem.isPending || updateQuoteItem.isPending || deleteQuoteItem.isPending

  const quoteStatuses = [
    { value: 'draft', label: 'Draft' },
    { value: 'sent', label: 'Sent' },
    { value: 'viewed', label: 'Viewed' },
    { value: 'accepted', label: 'Accepted' },
    { value: 'rejected', label: 'Rejected' },
    { value: 'expired', label: 'Expired' },
    { value: 'cancelled', label: 'Cancelled' },
  ]

  const currencies = [
    { value: 'USD', label: 'USD ($)' },
    { value: 'EUR', label: 'EUR (€)' },
    { value: 'GBP', label: 'GBP (£)' },
    { value: 'INR', label: 'INR (₹)' },
    { value: 'CAD', label: 'CAD ($)' },
    { value: 'AUD', label: 'AUD ($)' },
  ]

  const discountTypes = [
    { value: 'percentage', label: 'Percentage (%)' },
    { value: 'fixed', label: 'Fixed Amount' },
  ]



  // Generate next quote number if creating new quote
  useEffect(() => {
    if (!quote) {
      const today = new Date()
      const year = today.getFullYear()
      const month = String(today.getMonth() + 1).padStart(2, '0')
      const random = Math.floor(Math.random() * 1000).toString().padStart(3, '0')
      const quoteNumber = `QUO-${year}${month}-${random}`

      setFormData(prev => ({ ...prev, quoteNumber }))
    }
  }, [quote])

  // Populate form with existing quote data
  useEffect(() => {
    if (quote) {
      setFormData({
        companyId: quote.companyId || '',
        customerId: quote.customerId || '',
        quoteNumber: quote.quoteNumber || '',
        quoteDate: quote.quoteDate?.split('T')[0] || '',
        expiryDate: quote.expiryDate?.split('T')[0] || '',
        status: quote.status || 'draft',
        subtotal: quote.subtotal || 0,
        discountType: quote.discountType || 'percentage',
        discountValue: quote.discountValue || 0,
        discountAmount: quote.discountAmount || 0,
        taxAmount: quote.taxAmount || 0,
        totalAmount: quote.totalAmount || 0,
        currency: quote.currency || 'USD',
        notes: quote.notes || '',
        terms: quote.terms || '',
        paymentInstructions: quote.paymentInstructions || '',
        poNumber: quote.poNumber || '',
        projectName: quote.projectName || '',
      })
    }
  }, [quote])

  // Populate quote items when editing
  useEffect(() => {
    if (isEditing && existingQuoteItems.length > 0) {
      setQuoteItems(existingQuoteItems.map(item => ({
        productId: item.productId,
        description: item.description,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        taxRate: item.taxRate || 0,
        discountRate: item.discountRate || 0,
        lineTotal: item.lineTotal,
        sortOrder: item.sortOrder || 0,
      })))
    }
  }, [isEditing, existingQuoteItems])

  // Recalculate total when quote items, discount, or tax changes
  useEffect(() => {
    const subtotal = quoteItems.reduce((sum, item) => sum + item.lineTotal, 0)
    const { discountType, discountValue, taxAmount } = formData
    let discountAmount = 0
    let total = subtotal

    if (discountType === 'percentage') {
      discountAmount = (subtotal * (discountValue || 0)) / 100
    } else {
      discountAmount = discountValue || 0
    }

    total = subtotal - discountAmount + (taxAmount || 0)

    setFormData(prev => ({
      ...prev,
      subtotal,
      discountAmount,
      totalAmount: Math.max(0, total) // Ensure total is not negative
    }))
  }, [quoteItems, formData.discountType, formData.discountValue, formData.taxAmount])

  const handleInputChange = (field: keyof CreateQuoteDto, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  const calculateLineTotal = (quantity: number, unitPrice: number, discountRate: number = 0, taxRate: number = 0) => {
    const subtotal = quantity * unitPrice
    const discountAmount = subtotal * (discountRate / 100)
    const discountedSubtotal = subtotal - discountAmount
    const taxAmount = discountedSubtotal * (taxRate / 100)
    return discountedSubtotal + taxAmount
  }

  const handleAddQuoteItem = () => {
    const newItem: Omit<QuoteItem, 'id' | 'quoteId' | 'createdAt' | 'updatedAt'> = {
      productId: undefined,
      description: '',
      quantity: 1,
      unitPrice: 0,
      taxRate: 0,
      discountRate: 0,
      lineTotal: 0,
      sortOrder: quoteItems.length,
    }
    setQuoteItems(prev => [...prev, newItem])
  }

  const handleUpdateQuoteItem = (index: number, field: keyof Omit<QuoteItem, 'id' | 'quoteId' | 'createdAt' | 'updatedAt'>, value: any) => {
    setQuoteItems(prev => prev.map((item, i) => {
      if (i === index) {
        const updatedItem = { ...item, [field]: value }

        // Recalculate line total if quantity, unitPrice, discountRate, or taxRate changed
        if (['quantity', 'unitPrice', 'discountRate', 'taxRate'].includes(field)) {
          updatedItem.lineTotal = calculateLineTotal(
            updatedItem.quantity,
            updatedItem.unitPrice,
            updatedItem.discountRate,
            updatedItem.taxRate
          )
        }

        return updatedItem
      }
      return item
    }))
  }

  const handleDeleteQuoteItem = (index: number) => {
    if (isEditing && existingQuoteItems[index]) {
      // If editing and item exists in database, delete from API
      deleteQuoteItem.mutate(existingQuoteItems[index].id)
    }
    // Remove from local state
    setQuoteItems(prev => prev.filter((_, i) => i !== index))
  }

  const validateForm = () => {
    const newErrors: Record<string, string> = {}

    if (!formData.quoteNumber.trim()) {
      newErrors.quoteNumber = 'Quote number is required'
    }
    if (!formData.customerId) {
      newErrors.customerId = 'Customer is required'
    }
    if (!formData.quoteDate) {
      newErrors.quoteDate = 'Quote date is required'
    }
    if (!formData.expiryDate) {
      newErrors.expiryDate = 'Expiry date is required'
    }
    if (quoteItems.length === 0) {
      newErrors.quoteItems = 'At least one quote item is required'
    }
    if (quoteItems.some(item => !item.description.trim())) {
      newErrors.quoteItems = 'All quote items must have a description'
    }
    if (quoteItems.some(item => item.quantity <= 0)) {
      newErrors.quoteItems = 'All quote items must have a quantity greater than 0'
    }
    if (quoteItems.some(item => item.unitPrice < 0)) {
      newErrors.quoteItems = 'Quote item prices cannot be negative'
    }
    if ((formData.discountValue || 0) < 0) {
      newErrors.discountValue = 'Discount cannot be negative'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) {
      return
    }

    try {
      let quoteId: string

      if (isEditing && quote) {
        await updateQuote.mutateAsync({ id: quote.id, data: { ...formData, id: quote.id } })
        quoteId = quote.id

        // Handle quote items for editing
        for (let i = 0; i < quoteItems.length; i++) {
          const item = quoteItems[i]
          const existingItem = existingQuoteItems[i]

          if (existingItem) {
            // Update existing item
            await updateQuoteItem.mutateAsync({
              id: existingItem.id,
              data: { ...item, quoteId }
            })
          } else {
            // Create new item
            await createQuoteItem.mutateAsync({ ...item, quoteId })
          }
        }
      } else {
        // Create new quote
        const newQuote = await createQuote.mutateAsync(formData)
        quoteId = newQuote.id

        // Create quote items
        for (const item of quoteItems) {
          await createQuoteItem.mutateAsync({ ...item, quoteId })
        }
      }

      onSuccess()
    } catch (error) {
      console.error('Failed to save quote:', error)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Basic Information */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Quote Number *
          </label>
          <input
            type="text"
            value={formData.quoteNumber}
            onChange={(e) => handleInputChange('quoteNumber', e.target.value)}
            className={cn(
              "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent",
              errors.quoteNumber && "border-red-500"
            )}
            placeholder="QUO-202510-001"
          />
          {errors.quoteNumber && (
            <p className="mt-1 text-sm text-red-600">{errors.quoteNumber}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Company
          </label>
          <select
            value={formData.companyId}
            onChange={(e) => handleInputChange('companyId', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value="">Select a company</option>
            {companies.map((company) => (
              <option key={company.id} value={company.id}>
                {company.name}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Customer *
          </label>
          <select
            value={formData.customerId}
            onChange={(e) => handleInputChange('customerId', e.target.value)}
            className={cn(
              "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent",
              errors.customerId && "border-red-500"
            )}
          >
            <option value="">Select a customer</option>
            {customers.map((customer) => (
              <option key={customer.id} value={customer.id}>
                {customer.name}{customer.companyName ? ` (${customer.companyName})` : ''}
              </option>
            ))}
          </select>
          {errors.customerId && (
            <p className="mt-1 text-sm text-red-600">{errors.customerId}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Project Name
          </label>
          <input
            type="text"
            value={formData.projectName}
            onChange={(e) => handleInputChange('projectName', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Project name (optional)"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Quote Date *
          </label>
          <input
            type="date"
            value={formData.quoteDate}
            onChange={(e) => handleInputChange('quoteDate', e.target.value)}
            className={cn(
              "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent",
              errors.quoteDate && "border-red-500"
            )}
          />
          {errors.quoteDate && (
            <p className="mt-1 text-sm text-red-600">{errors.quoteDate}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Expiry Date *
          </label>
          <input
            type="date"
            value={formData.expiryDate}
            onChange={(e) => handleInputChange('expiryDate', e.target.value)}
            className={cn(
              "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent",
              errors.expiryDate && "border-red-500"
            )}
          />
          {errors.expiryDate && (
            <p className="mt-1 text-sm text-red-600">{errors.expiryDate}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Status
          </label>
          <select
            value={formData.status}
            onChange={(e) => handleInputChange('status', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            {quoteStatuses.map((status) => (
              <option key={status.value} value={status.value}>
                {status.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Currency
          </label>
          <select
            value={formData.currency}
            onChange={(e) => handleInputChange('currency', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            {currencies.map((currency) => (
              <option key={currency.value} value={currency.value}>
                {currency.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Quote Items */}
      <div className="border-t pt-6">
        <div className="flex justify-between items-center">
          <h3 className="text-lg font-medium text-gray-900">Line Items</h3>
          <button
            type="button"
            onClick={handleAddQuoteItem}
            className="flex items-center px-3 py-1.5 text-sm font-medium text-blue-600 border border-blue-300 rounded-md hover:bg-blue-50 transition-colors"
          >
            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            Add Item
          </button>
        </div>

        {errors.quoteItems && (
          <p className="text-red-500 text-sm mt-2">{errors.quoteItems}</p>
        )}

        <div className="mt-4">
          {quoteItems.length === 0 ? (
            <div className="text-center py-8 bg-gray-50 rounded-lg border-2 border-dashed border-gray-300">
              <p className="text-gray-500">No line items added yet</p>
              <button
                type="button"
                onClick={handleAddQuoteItem}
                className="mt-2 text-blue-600 hover:text-blue-700 font-medium"
              >
                Add your first item
              </button>
            </div>
          ) : (
            <>
              {/* Header */}
              <div className="grid grid-cols-12 gap-2 text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
                <div className="col-span-1"></div>
                <div className="col-span-4">Description</div>
                <div className="col-span-2">Quantity</div>
                <div className="col-span-2">Price</div>
                <div className="col-span-1">Tax %</div>
                <div className="col-span-2 text-right">Total</div>
              </div>

              {/* Line Items */}
              {quoteItems.map((item, index) => (
                <div
                  key={index}
                  className="group grid grid-cols-12 gap-2 p-3 bg-white border border-gray-200 rounded-lg hover:shadow-sm transition-shadow mb-2"
                >
                  <div className="col-span-1 flex items-center">
                    <button
                      type="button"
                      className="opacity-0 group-hover:opacity-100 text-gray-400 hover:text-gray-600 cursor-move transition-opacity"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 8h16M4 16h16" />
                      </svg>
                    </button>
                  </div>

                  <div className="col-span-4 space-y-2">
                    <select
                      value={item.productId || ''}
                      onChange={(e) => handleProductSelect(index, e.target.value)}
                      className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                    >
                      <option value="">Select product or custom</option>
                      {products.map((product) => (
                        <option key={product.id} value={product.id}>
                          {product.name} - {(formData.currency === 'EUR' ? '€' : formData.currency === 'GBP' ? '£' : formData.currency === 'INR' ? '₹' : '$')}{product.unitPrice}
                        </option>
                      ))}
                    </select>
                    <input
                      type="text"
                      value={item.description}
                      onChange={(e) => handleUpdateQuoteItem(index, 'description', e.target.value)}
                      className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                      placeholder="Item description"
                    />
                  </div>

                  <div className="col-span-2">
                    <input
                      type="number"
                      step="0.01"
                      value={item.quantity}
                      onChange={(e) => handleUpdateQuoteItem(index, 'quantity', parseFloat(e.target.value) || 0)}
                      className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                    />
                  </div>

                  <div className="col-span-2">
                    <input
                      type="number"
                      step="0.01"
                      value={item.unitPrice}
                      onChange={(e) => handleUpdateQuoteItem(index, 'unitPrice', parseFloat(e.target.value) || 0)}
                      className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                    />
                  </div>

                  <div className="col-span-1">
                    <input
                      type="number"
                      min="0"
                      max="100"
                      step="0.01"
                      value={item.taxRate}
                      onChange={(e) => handleUpdateQuoteItem(index, 'taxRate', parseFloat(e.target.value) || 0)}
                      className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                    />
                  </div>

                  <div className="col-span-2 flex items-center justify-between">
                    <span className="text-sm font-medium">
                      {(formData.currency === 'EUR' ? '€' : formData.currency === 'GBP' ? '£' : formData.currency === 'INR' ? '₹' : '$')}{item.lineTotal.toFixed(2)}
                    </span>
                    {quoteItems.length > 1 && (
                      <button
                        type="button"
                        onClick={() => handleDeleteQuoteItem(index)}
                        className="opacity-0 group-hover:opacity-100 text-red-500 hover:text-red-700 transition-opacity"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </>
          )}
        </div>
      </div>

      {/* Financial Information */}
      <div className="border-t pt-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Financial Information</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
          Subtotal
          </label>
          <input
          type="number"
          step="0.01"
          min="0"
          value={formData.subtotal}
          readOnly
          className="w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 text-gray-700 cursor-not-allowed"
          placeholder="0.00"
          />
          <p className="mt-1 text-xs text-gray-500">Calculated from quote items</p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Discount Type
            </label>
            <select
              value={formData.discountType}
              onChange={(e) => handleInputChange('discountType', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              {discountTypes.map((type) => (
                <option key={type.value} value={type.value}>
                  {type.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Discount Value
            </label>
            <input
              type="number"
              step="0.01"
              value={formData.discountValue}
              onChange={(e) => handleInputChange('discountValue', parseFloat(e.target.value) || 0)}
              className={cn(
                "w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent",
                errors.discountValue && "border-red-500"
              )}
              placeholder={formData.discountType === 'percentage' ? "10.00" : "100.00"}
            />
            {errors.discountValue && (
              <p className="mt-1 text-sm text-red-600">{errors.discountValue}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Tax Amount
            </label>
            <input
              type="number"
              step="0.01"
              value={formData.taxAmount}
              onChange={(e) => handleInputChange('taxAmount', parseFloat(e.target.value) || 0)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="0.00"
            />
          </div>
        </div>

        {/* Calculated Fields */}
        <div className="mt-4 grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-gray-50 p-3 rounded-md">
            <div className="text-sm text-gray-600">Discount Amount</div>
            <div className="text-lg font-semibold text-green-600">
            {formData.currency === 'EUR' ? '€' : formData.currency === 'GBP' ? '£' : formData.currency === 'INR' ? '₹' : '$'}
            {(formData.discountAmount || 0).toFixed(2)}
            </div>
          </div>
          <div className="bg-gray-50 p-3 rounded-md">
            <div className="text-sm text-gray-600">Tax Amount</div>
            <div className="text-lg font-semibold text-blue-600">
              {formData.currency === 'EUR' ? '€' : formData.currency === 'GBP' ? '£' : formData.currency === 'INR' ? '₹' : '$'}
              {(formData.taxAmount || 0).toFixed(2)}
            </div>
          </div>
          <div className="bg-blue-50 p-3 rounded-md">
            <div className="text-sm text-gray-600">Total Amount</div>
            <div className="text-xl font-bold text-blue-600">
              {formData.currency === 'EUR' ? '€' : formData.currency === 'GBP' ? '£' : formData.currency === 'INR' ? '₹' : '$'}
              {formData.totalAmount.toFixed(2)}
            </div>
          </div>
        </div>
      </div>

      {/* Additional Fields */}
      <div className="border-t pt-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Additional Information</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              PO Number
            </label>
            <input
              type="text"
              value={formData.poNumber}
              onChange={(e) => handleInputChange('poNumber', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="Purchase order number"
            />
          </div>
        </div>

        <div className="mt-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Notes
          </label>
          <textarea
            value={formData.notes}
            onChange={(e) => handleInputChange('notes', e.target.value)}
            rows={3}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Additional notes for the quote"
          />
        </div>

        <div className="mt-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Terms & Conditions
          </label>
          <textarea
            value={formData.terms}
            onChange={(e) => handleInputChange('terms', e.target.value)}
            rows={3}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Terms and conditions for the quote"
          />
        </div>

        <div className="mt-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Payment Instructions
          </label>
          <textarea
            value={formData.paymentInstructions}
            onChange={(e) => handleInputChange('paymentInstructions', e.target.value)}
            rows={2}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Payment instructions for the customer"
          />
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-6 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isLoading ? 'Saving...' : isEditing ? 'Update Quote' : 'Create Quote'}
        </button>
      </div>
    </form>
  )
}

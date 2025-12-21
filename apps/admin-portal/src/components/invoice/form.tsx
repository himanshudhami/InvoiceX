import { useState, useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Invoice, CreateInvoiceDto, UpdateInvoiceDto, InvoiceItem } from '@/services/api/types'
import { useCreateInvoice, useUpdateInvoice, useInvoiceItems, invoiceKeys } from '@/features/invoices/hooks'
import { invoiceService } from '@/services/api/invoiceService'
import { useCustomers } from '@/features/customers/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useProducts } from '@/features/products/hooks'
import { InvoiceFormProvider } from './form-context'
import { InvoiceNumber } from './invoice-number'
import { InvoiceStatus } from './invoice-status'
import { FromDetails } from './from-details'
import { CustomerDetails } from './customer-details'
import { InvoiceDates } from './invoice-dates'
import { PaymentDetails } from './payment-details'
import { LineItems } from './line-items'
import { Summary } from './summary'
import { NoteDetails } from './note-details'
import { GstDetails } from './gst-details'

interface LineItem {
  id: string
  productId?: string
  description: string
  quantity: number
  unitPrice: number
  taxRate: number
  discountRate?: number
  lineTotal: number
  sortOrder?: number
  // GST fields
  hsnSacCode?: string
  isService?: boolean
  cgstRate?: number
  cgstAmount?: number
  sgstRate?: number
  sgstAmount?: number
  igstRate?: number
  igstAmount?: number
  cessRate?: number
  cessAmount?: number
}

type InvoiceFormState = Omit<CreateInvoiceDto, 'status' | 'taxAmount' | 'discountAmount' | 'paidAmount' | 'currency'> & {
  status: string
  taxAmount: number
  discountAmount: number
  paidAmount: number
  currency: string
  lineItems: LineItem[]
  // GST Classification
  invoiceType: string
  supplyType?: string
  placeOfSupply?: string
  reverseCharge: boolean
  // GST Totals
  totalCgst: number
  totalSgst: number
  totalIgst: number
  totalCess: number
}

interface InvoiceFormProps {
  invoice?: Invoice
  onSuccess: () => void
  onCancel: () => void
}

export const InvoiceForm = ({ invoice, onSuccess, onCancel }: InvoiceFormProps) => {
  const queryClient = useQueryClient()
  const [formData, setFormData] = useState<InvoiceFormState>({
    companyId: '',
    customerId: '',
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
    notes: '',
    terms: '',
    poNumber: '',
    projectName: '',
    lineItems: [],
    // GST defaults
    invoiceType: 'export',
    supplyType: 'export',
    placeOfSupply: '',
    reverseCharge: false,
    totalCgst: 0,
    totalSgst: 0,
    totalIgst: 0,
    totalCess: 0,
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  
  const createInvoice = useCreateInvoice()
  const updateInvoice = useUpdateInvoice()
  // Scope customers to the selected company so "Bill To" updates when company changes
  const { data: customers = [] } = useCustomers(formData.companyId || undefined)
  const { data: companies = [] } = useCompanies()
  const { data: products = [] } = useProducts(formData.companyId || undefined)
  const { data: existingItems = [] } = useInvoiceItems(invoice?.id || '')

  const isEditing = !!invoice
  const isLoading = createInvoice.isPending || updateInvoice.isPending

  // Generate invoice number for new invoices
  useEffect(() => {
    if (!invoice) {
      const today = new Date()
      const year = today.getFullYear()
      const month = String(today.getMonth() + 1).padStart(2, '0')
      const random = Math.floor(Math.random() * 1000).toString().padStart(3, '0')
      const invoiceNumber = `INV-${year}${month}-${random}`
      
      setFormData(prev => {
        // Only add a line item if none exist (prevents duplicate items in StrictMode)
        if (prev.lineItems.length === 0) {
          const newItem: LineItem = {
            id: `temp-${Date.now()}`,
            productId: '',
            description: '',
            quantity: 1,
            unitPrice: 0,
            taxRate: 0,
            lineTotal: 0,
          }
          return { 
            ...prev, 
            invoiceNumber,
            lineItems: [newItem]
          }
        }
        return { ...prev, invoiceNumber }
      })
    }
  }, [invoice])

  // Populate form for editing
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
        notes: invoice.notes || '',
        terms: invoice.terms || '',
        poNumber: invoice.poNumber || '',
        projectName: invoice.projectName || '',
        lineItems: [], // Will be populated by existingItems effect
        // GST fields
        invoiceType: invoice.invoiceType || 'export',
        supplyType: invoice.supplyType || 'export',
        placeOfSupply: invoice.placeOfSupply || '',
        reverseCharge: invoice.reverseCharge || false,
        totalCgst: invoice.totalCgst || 0,
        totalSgst: invoice.totalSgst || 0,
        totalIgst: invoice.totalIgst || 0,
        totalCess: invoice.totalCess || 0,
      })
    }
  }, [invoice])

  // Clear customer immediately when company changes to avoid cross-company selections
  const handleCompanyChange = (companyId: string) => {
    setFormData(prev => ({
      ...prev,
      companyId,
      customerId: '',
    }))
    if (errors.customerId) {
      setErrors(prev => ({ ...prev, customerId: '' }))
    }
  }

  // Populate line items for editing
  useEffect(() => {
    if (invoice && existingItems.length > 0) {
      const lineItems = existingItems
        .slice()
        .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
        .map((item, index) => ({
          id: item.id,
          productId: item.productId || '',
          description: item.description,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          taxRate: item.taxRate || 0,
          discountRate: item.discountRate || 0,
          lineTotal: item.lineTotal,
          sortOrder: item.sortOrder ?? index,
          // GST fields
          hsnSacCode: item.hsnSacCode || '',
          isService: item.isService ?? true,
          cgstRate: item.cgstRate || 0,
          cgstAmount: item.cgstAmount || 0,
          sgstRate: item.sgstRate || 0,
          sgstAmount: item.sgstAmount || 0,
          igstRate: item.igstRate || 0,
          igstAmount: item.igstAmount || 0,
          cessRate: item.cessRate || 0,
          cessAmount: item.cessAmount || 0,
        }))

      setFormData(prev => ({ ...prev, lineItems }))
    }
  }, [invoice, existingItems])

  // Recalculate totals when line items or invoice type change
  useEffect(() => {
    const subtotal = formData.lineItems.reduce((sum, item) => sum + item.lineTotal, 0)

    // Calculate GST based on supply type
    let totalCgst = 0
    let totalSgst = 0
    let totalIgst = 0
    let totalCess = 0
    let taxAmount = 0

    // Only calculate GST for domestic invoices
    if (formData.invoiceType !== 'export') {
      formData.lineItems.forEach(item => {
        totalCgst += item.cgstAmount || 0
        totalSgst += item.sgstAmount || 0
        totalIgst += item.igstAmount || 0
        totalCess += item.cessAmount || 0
      })
      taxAmount = totalCgst + totalSgst + totalIgst + totalCess
    } else {
      // For export invoices, use the simple tax calculation
      taxAmount = formData.lineItems.reduce((sum, item) => sum + (item.lineTotal * item.taxRate / 100), 0)
    }

    const totalAmount = subtotal + taxAmount - (formData.discountAmount || 0)

    setFormData(prev => ({
      ...prev,
      subtotal,
      taxAmount,
      totalAmount: Math.max(0, totalAmount),
      totalCgst,
      totalSgst,
      totalIgst,
      totalCess,
    }))
  }, [formData.lineItems, formData.discountAmount, formData.invoiceType])

  const updateField = (field: string, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  const addLineItem = () => {
    const newItem: LineItem = {
      id: `temp-${Date.now()}`,
      productId: '',
      description: '',
      quantity: 1,
      unitPrice: 0,
      taxRate: 0,
      discountRate: 0,
      lineTotal: 0,
    }
    setFormData(prev => ({ ...prev, lineItems: [...prev.lineItems, newItem] }))
  }

  const removeLineItem = (id: string) => {
    setFormData(prev => ({
      ...prev,
      lineItems: prev.lineItems.filter(item => item.id !== id)
    }))
  }

  const updateLineItem = (id: string, field: keyof LineItem, value: any) => {
    setFormData(prev => ({
      ...prev,
      lineItems: prev.lineItems.map(item => {
        if (item.id !== id) return item

        const updated = { ...item, [field]: value }

        // Recalculate line total
        updated.lineTotal = updated.quantity * updated.unitPrice

        // Recalculate GST amounts based on supply type
        if (prev.invoiceType !== 'export') {
          const taxableAmount = updated.lineTotal
          const gstRate = updated.taxRate || 0

          if (prev.supplyType === 'intra_state') {
            // CGST + SGST (split equally)
            updated.cgstRate = gstRate / 2
            updated.sgstRate = gstRate / 2
            updated.igstRate = 0
            updated.cgstAmount = taxableAmount * (updated.cgstRate / 100)
            updated.sgstAmount = taxableAmount * (updated.sgstRate / 100)
            updated.igstAmount = 0
          } else {
            // IGST (inter-state)
            updated.igstRate = gstRate
            updated.cgstRate = 0
            updated.sgstRate = 0
            updated.igstAmount = taxableAmount * (gstRate / 100)
            updated.cgstAmount = 0
            updated.sgstAmount = 0
          }

          // Calculate cess
          if (updated.cessRate) {
            updated.cessAmount = taxableAmount * (updated.cessRate / 100)
          }
        }

        return updated
      })
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

    if (formData.lineItems.length === 0) {
      newErrors.lineItems = 'At least one line item is required'
    }

    const hasInvalidLineItem = formData.lineItems.some(item => {
      const hasDescription = item.description?.trim().length > 0
      const quantityValid = Number(item.quantity) > 0
      const priceValid = Number(item.unitPrice) >= 0
      return !(hasDescription && quantityValid && priceValid)
    })

    if (hasInvalidLineItem) {
      newErrors.lineItems = 'Each line item requires a description, quantity greater than zero, and a non-negative price'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const updateInvoiceLineItems = async (
    invoiceId: string,
    currentItems: LineItem[],
    existingItems: InvoiceItem[]
  ) => {
    try {
      const normalizedItems = currentItems
        .map((item, index) => {
          const quantity = Number(item.quantity) || 0
          const unitPrice = Number(item.unitPrice) || 0
          const lineTotal = Number((quantity * unitPrice).toFixed(2))

          return {
            ...item,
            description: item.description.trim(),
            quantity,
            unitPrice,
            taxRate: Number(item.taxRate ?? 0),
            discountRate: Number(item.discountRate ?? 0),
            lineTotal,
            sortOrder: index,
          }
        })
        .filter(item => item.description.length > 0)

      if (normalizedItems.length === 0) {
        return
      }

      const existingById = new Map(existingItems.map(item => [item.id, item]))
      const persistedIds = new Set(
        normalizedItems.filter(item => !item.id.startsWith('temp-')).map(item => item.id)
      )

      const itemsToDelete = existingItems.filter(item => !persistedIds.has(item.id))
      const itemsToCreate = normalizedItems.filter(item => item.id.startsWith('temp-'))

      const numbersEqual = (a: number | null | undefined, b: number | null | undefined) =>
        Math.abs(Number(a ?? 0) - Number(b ?? 0)) < 0.0001

      const itemsToUpdate = normalizedItems
        .filter(item => !item.id.startsWith('temp-'))
        .filter(item => {
          const original = existingById.get(item.id)
          if (!original) return false

          return !(
            original.description === item.description &&
            numbersEqual(original.quantity, item.quantity) &&
            numbersEqual(original.unitPrice, item.unitPrice) &&
            numbersEqual(original.taxRate ?? 0, item.taxRate ?? 0) &&
            numbersEqual(original.discountRate ?? 0, item.discountRate ?? 0) &&
            numbersEqual(original.lineTotal, item.lineTotal) &&
            numbersEqual(original.sortOrder ?? 0, item.sortOrder ?? 0)
          )
        })

      for (const item of itemsToDelete) {
        await invoiceService.deleteInvoiceItem(item.id)
      }

      for (const item of itemsToUpdate) {
        await invoiceService.updateInvoiceItem(item.id, {
          invoiceId,
          productId: item.productId || undefined,
          description: item.description,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          taxRate: item.taxRate,
          discountRate: item.discountRate,
          lineTotal: item.lineTotal,
          sortOrder: item.sortOrder,
        })
      }

      for (const item of itemsToCreate) {
        await invoiceService.createInvoiceItem({
          invoiceId,
          productId: item.productId || undefined,
          description: item.description,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          taxRate: item.taxRate,
          discountRate: item.discountRate,
          lineTotal: item.lineTotal,
          sortOrder: item.sortOrder,
        })
      }

      // Invalidate invoice queries to refresh data
      await queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() })
    } catch (error) {
      console.error('Error updating line items:', error)
      throw error
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return
    if (isLoading) return // Prevent double submission

    try {
      if (isEditing && invoice) {
        const updateData: UpdateInvoiceDto = {
          id: invoice.id,
          companyId: formData.companyId,
          customerId: formData.customerId,
          invoiceNumber: formData.invoiceNumber,
          invoiceDate: formData.invoiceDate,
          dueDate: formData.dueDate,
          status: formData.status,
          subtotal: formData.subtotal,
          taxAmount: formData.taxAmount,
          discountAmount: formData.discountAmount,
          totalAmount: formData.totalAmount,
          paidAmount: formData.paidAmount,
          currency: formData.currency,
          notes: formData.notes,
          terms: formData.terms,
          poNumber: formData.poNumber,
          projectName: formData.projectName,
          // GST fields
          invoiceType: formData.invoiceType,
          supplyType: formData.supplyType,
          placeOfSupply: formData.placeOfSupply,
          reverseCharge: formData.reverseCharge,
          totalCgst: formData.totalCgst,
          totalSgst: formData.totalSgst,
          totalIgst: formData.totalIgst,
          totalCess: formData.totalCess,
        }
        await updateInvoice.mutateAsync({ id: invoice.id, data: updateData })
        
        // Update line items for existing invoice
        await updateInvoiceLineItems(invoice.id, formData.lineItems, existingItems)
        
      } else {
        const createData: CreateInvoiceDto = {
          companyId: formData.companyId,
          customerId: formData.customerId,
          invoiceNumber: formData.invoiceNumber,
          invoiceDate: formData.invoiceDate,
          dueDate: formData.dueDate,
          status: formData.status,
          subtotal: formData.subtotal,
          taxAmount: formData.taxAmount,
          discountAmount: formData.discountAmount,
          totalAmount: formData.totalAmount,
          paidAmount: formData.paidAmount,
          currency: formData.currency,
          notes: formData.notes,
          terms: formData.terms,
          poNumber: formData.poNumber,
          projectName: formData.projectName,
          // GST fields
          invoiceType: formData.invoiceType,
          supplyType: formData.supplyType,
          placeOfSupply: formData.placeOfSupply,
          reverseCharge: formData.reverseCharge,
          totalCgst: formData.totalCgst,
          totalSgst: formData.totalSgst,
          totalIgst: formData.totalIgst,
          totalCess: formData.totalCess,
        }
        const createdInvoice = await createInvoice.mutateAsync(createData)
        
        // Create line items for the new invoice
        if (formData.lineItems.length > 0) {
          console.log('Creating line items:', formData.lineItems.length)
          
          // Filter out duplicate and empty line items
          const validItems = formData.lineItems.filter((item, index, arr) => 
            item.description.trim() && 
            // Remove duplicates by description + price + quantity
            arr.findIndex(i => 
              i.description === item.description && 
              i.unitPrice === item.unitPrice && 
              i.quantity === item.quantity
            ) === index
          )
          
          console.log('Valid unique line items:', validItems.length)
          
          for (const [index, item] of validItems.entries()) {
            console.log(`Creating line item ${index + 1}:`, item)
            try {
              await invoiceService.createInvoiceItem({
                invoiceId: createdInvoice.id,
                productId: item.productId || undefined,
                description: item.description,
                quantity: item.quantity,
                unitPrice: item.unitPrice,
                taxRate: item.taxRate,
                lineTotal: item.lineTotal,
                sortOrder: index
              })

              console.log(`Line item ${index + 1} created successfully`)
            } catch (itemError) {
              console.error(`Failed to create line item ${index + 1}:`, itemError)
            }
          }

          // Invalidate invoice queries to refresh data
          await queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() })
        } else {
          console.log('No line items to create')
        }
      }
      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const contextValue = {
    formData,
    updateField,
    addLineItem,
    removeLineItem,
    updateLineItem,
    customers,
    companies,
    products,
    isLoading,
    errors,
    setErrors,
  }

  return (
    <InvoiceFormProvider value={contextValue}>
      <form onSubmit={handleSubmit} className="space-y-8">
        {/* Header Section */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <InvoiceNumber />
          <InvoiceStatus />
          <PaymentDetails />
        </div>

        {/* From and To Section */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <FromDetails />
          <CustomerDetails />
        </div>

        {/* GST Details */}
        <GstDetails />

        {/* Dates */}
        <InvoiceDates />

        {/* Line Items */}
        <LineItems />

        {/* Summary */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <NoteDetails />
          <Summary />
        </div>

        {/* Form Actions */}
        <div className="flex justify-end space-x-3 pt-6 border-t border-gray-200">
          <button
            type="button"
            onClick={onCancel}
            disabled={isLoading}
            className="px-6 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isLoading}
            className="px-6 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50"
          >
            {isLoading ? 'Saving...' : isEditing ? 'Update Invoice' : 'Create Invoice'}
          </button>
        </div>
      </form>
    </InvoiceFormProvider>
  )
}

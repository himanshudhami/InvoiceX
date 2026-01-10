import { useMemo } from 'react'
import { useInvoiceForm } from './form-context'
import { Trash2, Plus, GripVertical } from 'lucide-react'
import { formatCurrency } from '@/lib/currency'
import { Combobox, ComboboxOption } from '@/components/ui/combobox'
import { ProductSelect } from '@/components/ui/ProductSelect'

// Common HSN/SAC codes for quick selection with GST rates
const COMMON_HSN_SAC_CODES = [
  { code: '998313', description: 'IT consulting and support', type: 'SAC', gstRate: 18 },
  { code: '998314', description: 'IT design and development', type: 'SAC', gstRate: 18 },
  { code: '998315', description: 'Hosting and IT infrastructure', type: 'SAC', gstRate: 18 },
  { code: '998311', description: 'Management consulting', type: 'SAC', gstRate: 18 },
  { code: '997331', description: 'Software licensing', type: 'SAC', gstRate: 18 },
  { code: '999291', description: 'Training services', type: 'SAC', gstRate: 18 },
  { code: '8523', description: 'Packaged software', type: 'HSN', gstRate: 18 },
  { code: '8471', description: 'Computers', type: 'HSN', gstRate: 18 },
]

export const LineItems = () => {
  const { formData, addLineItem, removeLineItem, updateLineItem, products, errors, isLocked, isTallyImport } = useInvoiceForm()
  const { lineItems } = formData

  const handleProductSelect = (itemId: string, productId: string) => {
    const product = products.find(p => p.id === productId)
    if (product) {
      updateLineItem(itemId, 'productId', productId)
      updateLineItem(itemId, 'description', product.name)
      updateLineItem(itemId, 'unitPrice', product.unitPrice)
      updateLineItem(itemId, 'taxRate', product.defaultGstRate || product.taxRate || 0)
      // Copy GST fields from product
      updateLineItem(itemId, 'hsnSacCode', product.hsnSacCode || '')
      updateLineItem(itemId, 'isService', product.isService ?? true)
      updateLineItem(itemId, 'cessRate', product.cessRate || 0)
    }
  }

  // Handle HSN/SAC code selection - auto-update tax rate
  const handleHsnSacChange = (itemId: string, code: string) => {
    updateLineItem(itemId, 'hsnSacCode', code)

    // Find the code in our list and auto-fill tax rate
    const matchedCode = COMMON_HSN_SAC_CODES.find(c => c.code === code)
    if (matchedCode) {
      updateLineItem(itemId, 'taxRate', matchedCode.gstRate)
      updateLineItem(itemId, 'isService', matchedCode.type === 'SAC')
    }
  }

  // Check if GST mode is enabled (non-export invoice)
  const showGstFields = formData.invoiceType !== 'export'

  // Convert HSN/SAC codes to combobox options
  const hsnSacOptions: ComboboxOption[] = useMemo(() =>
    COMMON_HSN_SAC_CODES.map((item) => ({
      value: item.code,
      label: item.description,
      description: `${item.type} - ${item.gstRate}% GST`,
    })),
    []
  )

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h3 className="text-lg font-medium text-gray-900">Line Items</h3>
        {!isLocked && (
          <button
            type="button"
            onClick={addLineItem}
            className="flex items-center px-3 py-1.5 text-sm font-medium text-primary border border-primary/30 rounded-md hover:bg-primary/5 transition-colors"
          >
            <Plus className="w-4 h-4 mr-1" />
            Add Item
          </button>
        )}
      </div>

      {errors.lineItems && (
        <p className="text-red-500 text-sm">{errors.lineItems}</p>
      )}

      <div className="space-y-3">
        {lineItems.length === 0 ? (
          <div className="text-center py-8 bg-gray-50 rounded-lg border-2 border-dashed border-gray-300">
            {isTallyImport ? (
              <p className="text-gray-500">This invoice was imported from Tally without line item details</p>
            ) : (
              <>
                <p className="text-gray-500">No line items added yet</p>
                {!isLocked && (
                  <button
                    type="button"
                    onClick={addLineItem}
                    className="mt-2 text-primary hover:text-primary/80 font-medium"
                  >
                    Add your first item
                  </button>
                )}
              </>
            )}
          </div>
        ) : (
          <>
            {/* Header */}
            <div className="hidden md:grid md:grid-cols-12 gap-3 text-xs font-medium text-gray-500 uppercase tracking-wider px-3">
              <div className="col-span-1"></div>
              <div className={showGstFields ? 'col-span-3' : 'col-span-4'}>Description</div>
              {showGstFields && <div className="col-span-1">HSN/SAC</div>}
              <div className="col-span-2">Qty</div>
              <div className="col-span-2">Price</div>
              <div className="col-span-1">Tax %</div>
              <div className="col-span-2 text-right">Total</div>
            </div>

            {/* Line Items */}
            {lineItems.map((item) => (
              <div
                key={item.id}
                className="group p-3 bg-white border border-gray-200 rounded-lg hover:shadow-sm transition-shadow"
              >
                <div className="grid grid-cols-12 gap-3 items-start">
                  {/* Drag Handle */}
                  <div className="col-span-1 flex items-center pt-2">
                    <button
                      type="button"
                      className="opacity-0 group-hover:opacity-100 text-gray-400 hover:text-gray-600 cursor-move transition-opacity"
                    >
                      <GripVertical className="w-4 h-4" />
                    </button>
                  </div>

                  {/* Description */}
                  <div className={`${showGstFields ? 'col-span-3' : 'col-span-4'} space-y-2`}>
                    <ProductSelect
                      companyId={formData.companyId || undefined}
                      value={item.productId || ''}
                      onChange={(productId) => handleProductSelect(item.id, productId)}
                      placeholder="Select product..."
                      className="w-full"
                      disabled={isLocked}
                    />
                    <input
                      type="text"
                      value={item.description}
                      onChange={(e) => updateLineItem(item.id, 'description', e.target.value)}
                      className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring disabled:bg-gray-100 disabled:cursor-not-allowed"
                      placeholder="Item description"
                      disabled={isLocked}
                    />
                  </div>

                  {/* HSN/SAC Code - only for domestic invoices */}
                  {showGstFields && (
                    <div className="col-span-1">
                      <Combobox
                        options={hsnSacOptions}
                        value={item.hsnSacCode || ''}
                        onChange={(value) => handleHsnSacChange(item.id, value)}
                        placeholder="HSN/SAC"
                      />
                    </div>
                  )}

                  {/* Quantity */}
                  <div className="col-span-2">
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={item.quantity}
                      onChange={(e) => updateLineItem(item.id, 'quantity', parseFloat(e.target.value) || 0)}
                      className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring disabled:bg-gray-100 disabled:cursor-not-allowed"
                      disabled={isLocked}
                    />
                  </div>

                  {/* Unit Price */}
                  <div className="col-span-2">
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={item.unitPrice}
                      onChange={(e) => updateLineItem(item.id, 'unitPrice', parseFloat(e.target.value) || 0)}
                      className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring disabled:bg-gray-100 disabled:cursor-not-allowed"
                      disabled={isLocked}
                    />
                  </div>

                  {/* Tax Rate */}
                  <div className="col-span-1">
                    <input
                      type="number"
                      min="0"
                      max="100"
                      step="0.01"
                      value={item.taxRate}
                      onChange={(e) => updateLineItem(item.id, 'taxRate', parseFloat(e.target.value) || 0)}
                      className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring disabled:bg-gray-100 disabled:cursor-not-allowed"
                      disabled={isLocked}
                    />
                  </div>

                  {/* Total */}
                  <div className="col-span-2 flex items-center justify-between pt-2">
                    <span className="text-sm font-medium">
                      {formatCurrency(item.lineTotal, formData.currency)}
                    </span>
                    {!isLocked && lineItems.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeLineItem(item.id)}
                        className="opacity-0 group-hover:opacity-100 text-red-500 hover:text-red-700 transition-opacity"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                </div>

                {/* GST breakdown for domestic invoices - shown below line item */}
                {showGstFields && (item.cgstAmount || item.sgstAmount || item.igstAmount) && (
                  <div className="mt-2 pt-2 border-t border-gray-100 text-xs text-gray-500 flex gap-4 pl-12">
                    {formData.supplyType === 'intra_state' ? (
                      <>
                        <span>CGST: {formatCurrency(item.cgstAmount || 0, formData.currency)}</span>
                        <span>SGST: {formatCurrency(item.sgstAmount || 0, formData.currency)}</span>
                      </>
                    ) : (
                      <span>IGST: {formatCurrency(item.igstAmount || 0, formData.currency)}</span>
                    )}
                    {item.cessAmount ? (
                      <span>Cess: {formatCurrency(item.cessAmount, formData.currency)}</span>
                    ) : null}
                  </div>
                )}
              </div>
            ))}
          </>
        )}
      </div>
    </div>
  )
}

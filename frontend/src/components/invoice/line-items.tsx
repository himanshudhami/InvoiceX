import { useInvoiceForm } from './form-context'
import { Trash2, Plus, GripVertical } from 'lucide-react'
import { formatCurrency } from '@/lib/currency'

export const LineItems = () => {
  const { formData, addLineItem, removeLineItem, updateLineItem, products, errors } = useInvoiceForm()
  const { lineItems } = formData

  const handleProductSelect = (itemId: string, productId: string) => {
    const product = products.find(p => p.id === productId)
    if (product) {
      updateLineItem(itemId, 'productId', productId)
      updateLineItem(itemId, 'description', product.name)
      updateLineItem(itemId, 'unitPrice', product.unitPrice)
      updateLineItem(itemId, 'taxRate', product.taxRate || 0)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h3 className="text-lg font-medium text-gray-900">Line Items</h3>
        <button
          type="button"
          onClick={addLineItem}
          className="flex items-center px-3 py-1.5 text-sm font-medium text-blue-600 border border-blue-300 rounded-md hover:bg-blue-50 transition-colors"
        >
          <Plus className="w-4 h-4 mr-1" />
          Add Item
        </button>
      </div>

      {errors.lineItems && (
        <p className="text-red-500 text-sm">{errors.lineItems}</p>
      )}

      <div className="space-y-3">
        {lineItems.length === 0 ? (
          <div className="text-center py-8 bg-gray-50 rounded-lg border-2 border-dashed border-gray-300">
            <p className="text-gray-500">No line items added yet</p>
            <button
              type="button"
              onClick={addLineItem}
              className="mt-2 text-blue-600 hover:text-blue-700 font-medium"
            >
              Add your first item
            </button>
          </div>
        ) : (
          <>
            {/* Header */}
            <div className="grid grid-cols-12 gap-2 text-xs font-medium text-gray-500 uppercase tracking-wider">
              <div className="col-span-1"></div>
              <div className="col-span-4">Description</div>
              <div className="col-span-2">Quantity</div>
              <div className="col-span-2">Price</div>
              <div className="col-span-1">Tax %</div>
              <div className="col-span-2 text-right">Total</div>
            </div>

            {/* Line Items */}
            {lineItems.map((item, index) => (
              <div 
                key={item.id}
                className="group grid grid-cols-12 gap-2 p-3 bg-white border border-gray-200 rounded-lg hover:shadow-sm transition-shadow"
              >
                <div className="col-span-1 flex items-center">
                  <button
                    type="button"
                    className="opacity-0 group-hover:opacity-100 text-gray-400 hover:text-gray-600 cursor-move transition-opacity"
                  >
                    <GripVertical className="w-4 h-4" />
                  </button>
                </div>

                <div className="col-span-4 space-y-2">
                  <select
                    value={item.productId || ''}
                    onChange={(e) => handleProductSelect(item.id, e.target.value)}
                    className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                  >
                    <option value="">Select product or custom</option>
                    {products.map((product) => (
                      <option key={product.id} value={product.id}>
                        {product.name} - {formatCurrency(product.unitPrice, formData.currency)}
                      </option>
                    ))}
                  </select>
                  <input
                    type="text"
                    value={item.description}
                    onChange={(e) => updateLineItem(item.id, 'description', e.target.value)}
                    className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                    placeholder="Item description"
                  />
                </div>

                <div className="col-span-2">
                  <input
                    type="number"
                    step="0.01"
                    value={item.quantity}
                    onChange={(e) => updateLineItem(item.id, 'quantity', parseFloat(e.target.value) || 0)}
                    className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                </div>

                <div className="col-span-2">
                  <input
                    type="number"
                    step="0.01"
                    value={item.unitPrice}
                    onChange={(e) => updateLineItem(item.id, 'unitPrice', parseFloat(e.target.value) || 0)}
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
                    onChange={(e) => updateLineItem(item.id, 'taxRate', parseFloat(e.target.value) || 0)}
                    className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                </div>

                <div className="col-span-2 flex items-center justify-between">
                  <span className="text-sm font-medium">
                    {formatCurrency(item.lineTotal, formData.currency)}
                  </span>
                  {lineItems.length > 1 && (
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
            ))}
          </>
        )}
      </div>
    </div>
  )
}
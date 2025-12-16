import { useInvoiceForm } from './form-context'
import { formatCurrency } from '@/lib/currency'

export const Summary = () => {
  const { formData, updateField } = useInvoiceForm()
  const { subtotal, taxAmount, discountAmount, totalAmount } = formData



  return (
    <div className="bg-gray-50 rounded-lg p-6 space-y-3">
      <div className="flex justify-between text-sm">
        <span className="text-gray-600">Subtotal</span>
        <span className="font-medium">{formatCurrency(subtotal, formData.currency)}</span>
      </div>

      <div className="flex justify-between text-sm">
        <span className="text-gray-600">Tax</span>
        <span className="font-medium">{formatCurrency(taxAmount, formData.currency)}</span>
      </div>

      <div className="flex justify-between text-sm">
        <div className="flex items-center space-x-2">
          <span className="text-gray-600">Discount</span>
          <input
            type="number"
            min="0"
            step="0.01"
            value={discountAmount}
            onChange={(e) => updateField('discountAmount', parseFloat(e.target.value) || 0)}
            className="w-24 px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
            placeholder="0.00"
          />
        </div>
        <span className="font-medium text-red-600">
          -{formatCurrency(discountAmount, formData.currency)}
        </span>
      </div>

      <div className="pt-3 border-t border-gray-300">
        <div className="flex justify-between">
          <span className="text-lg font-semibold">Total</span>
          <span className="text-lg font-bold">{formatCurrency(totalAmount, formData.currency)}</span>
        </div>
      </div>

      {formData.status === 'paid' || formData.paidAmount > 0 ? (
        <div className="pt-3 border-t border-gray-300">
          <div className="flex justify-between text-sm">
            <div className="flex items-center space-x-2">
              <span className="text-gray-600">Paid Amount</span>
              <input
                type="number"
                min="0"
                max={totalAmount}
                step="0.01"
                value={formData.paidAmount}
                onChange={(e) => updateField('paidAmount', parseFloat(e.target.value) || 0)}
                className="w-24 px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>
            <span className="font-medium text-green-600">
              {formatCurrency(formData.paidAmount, formData.currency)}
            </span>
          </div>
          {formData.paidAmount < totalAmount && (
            <div className="flex justify-between text-sm mt-2">
              <span className="text-gray-600">Balance Due</span>
              <span className="font-medium text-red-600">
                {formatCurrency(totalAmount - formData.paidAmount, formData.currency)}
              </span>
            </div>
          )}
        </div>
      ) : null}
    </div>
  )
}
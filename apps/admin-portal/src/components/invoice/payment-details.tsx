import { useInvoiceForm } from './form-context'
import { CurrencySelect } from '@/components/ui/currency-select'

export const PaymentDetails = () => {
  const { formData, updateField, errors } = useInvoiceForm()
  const isForex = formData.currency?.toUpperCase() !== 'INR'

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label htmlFor="currency" className="block text-sm font-medium text-gray-700">
            Currency
          </label>
          <CurrencySelect
            value={formData.currency}
            onChange={(value) => updateField('currency', value)}
          />
        </div>

        {isForex && (
          <div className="space-y-2">
            <label htmlFor="exchangeRate" className="block text-sm font-medium text-gray-700">
              Invoice Exchange Rate (INR)
            </label>
            <input
              id="exchangeRate"
              type="number"
              min="0.0001"
              step="0.0001"
              value={formData.exchangeRate || ''}
              onChange={(e) => updateField('exchangeRate', parseFloat(e.target.value) || 0)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="RBI reference rate on invoice date"
            />
            {errors.exchangeRate && (
              <p className="text-xs text-red-600">{errors.exchangeRate}</p>
            )}
          </div>
        )}
        <div className="space-y-2">
          <label htmlFor="poNumber" className="block text-sm font-medium text-gray-700">
            PO Number
          </label>
          <input
            id="poNumber"
            type="text"
            value={formData.poNumber || ''}
            onChange={(e) => updateField('poNumber', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="PO-2024-001"
          />
        </div>
      </div>

      <div className="space-y-2">
        <label htmlFor="projectName" className="block text-sm font-medium text-gray-700">
          Project / Reference
        </label>
        <input
          id="projectName"
          type="text"
          value={formData.projectName || ''}
          onChange={(e) => updateField('projectName', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Project name or reference"
        />
      </div>

      {isForex && (
        <div className="text-xs text-gray-500">
          Use the RBI reference rate for the invoice date to comply with Ind AS 21.
        </div>
      )}
    </div>
  )
}

import { useInvoiceForm } from './form-context'
import { CurrencySelect } from '@/components/ui/currency-select'

export const PaymentDetails = () => {
  const { formData, updateField } = useInvoiceForm()

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
    </div>
  )
}
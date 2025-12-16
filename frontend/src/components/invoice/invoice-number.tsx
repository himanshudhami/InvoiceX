import { useInvoiceForm } from './form-context'
import { cn } from '@/lib/utils'

export const InvoiceNumber = () => {
  const { formData, updateField, errors } = useInvoiceForm()

  return (
    <div className="space-y-2">
      <label htmlFor="invoiceNumber" className="block text-sm font-medium text-gray-700">
        Invoice Number
      </label>
      <input
        id="invoiceNumber"
        type="text"
        value={formData.invoiceNumber}
        onChange={(e) => updateField('invoiceNumber', e.target.value)}
        className={cn(
          "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500",
          errors.invoiceNumber ? "border-red-500" : "border-gray-300"
        )}
        placeholder="INV-2024-001"
      />
      {errors.invoiceNumber && (
        <p className="text-red-500 text-sm">{errors.invoiceNumber}</p>
      )}
    </div>
  )
}
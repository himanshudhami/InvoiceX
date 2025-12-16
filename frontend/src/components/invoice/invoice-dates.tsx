import { useInvoiceForm } from './form-context'
import { cn } from '@/lib/utils'

export const InvoiceDates = () => {
  const { formData, updateField, errors } = useInvoiceForm()

  return (
    <div className="grid grid-cols-2 gap-4">
      <div className="space-y-2">
        <label htmlFor="invoiceDate" className="block text-sm font-medium text-gray-700">
          Invoice Date
        </label>
        <input
          id="invoiceDate"
          type="date"
          value={formData.invoiceDate}
          onChange={(e) => updateField('invoiceDate', e.target.value)}
          className={cn(
            "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500",
            errors.invoiceDate ? "border-red-500" : "border-gray-300"
          )}
        />
        {errors.invoiceDate && (
          <p className="text-red-500 text-sm">{errors.invoiceDate}</p>
        )}
      </div>

      <div className="space-y-2">
        <label htmlFor="dueDate" className="block text-sm font-medium text-gray-700">
          Due Date
        </label>
        <input
          id="dueDate"
          type="date"
          value={formData.dueDate}
          onChange={(e) => updateField('dueDate', e.target.value)}
          className={cn(
            "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500",
            errors.dueDate ? "border-red-500" : "border-gray-300"
          )}
        />
        {errors.dueDate && (
          <p className="text-red-500 text-sm">{errors.dueDate}</p>
        )}
      </div>
    </div>
  )
}
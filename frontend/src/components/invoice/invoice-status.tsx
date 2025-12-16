import { useInvoiceForm } from './form-context'

const statuses = [
  { value: 'draft', label: 'Draft', color: 'bg-gray-100 text-gray-800' },
  { value: 'sent', label: 'Sent', color: 'bg-blue-100 text-blue-800' },
  { value: 'viewed', label: 'Viewed', color: 'bg-purple-100 text-purple-800' },
  { value: 'paid', label: 'Paid', color: 'bg-green-100 text-green-800' },
  { value: 'overdue', label: 'Overdue', color: 'bg-red-100 text-red-800' },
  { value: 'cancelled', label: 'Cancelled', color: 'bg-gray-100 text-gray-600' },
]

export const InvoiceStatus = () => {
  const { formData, updateField } = useInvoiceForm()
  const currentStatus = statuses.find(s => s.value === formData.status) || statuses[0]

  return (
    <div className="space-y-2">
      <label htmlFor="status" className="block text-sm font-medium text-gray-700">
        Status
      </label>
      <div className="flex items-center space-x-2">
        <select
          id="status"
          value={formData.status}
          onChange={(e) => updateField('status', e.target.value)}
          className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          {statuses.map((status) => (
            <option key={status.value} value={status.value}>
              {status.label}
            </option>
          ))}
        </select>
        <span className={`px-3 py-2 text-sm font-medium rounded-md ${currentStatus.color}`}>
          {currentStatus.label}
        </span>
      </div>
    </div>
  )
}
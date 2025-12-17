import { useInvoiceForm } from './form-context'

export const NoteDetails = () => {
  const { formData, updateField } = useInvoiceForm()

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <label htmlFor="notes" className="block text-sm font-medium text-gray-700">
          Notes
        </label>
        <textarea
          id="notes"
          rows={3}
          value={formData.notes || ''}
          onChange={(e) => updateField('notes', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Additional notes or payment instructions..."
        />
      </div>

      <div className="space-y-2">
        <label htmlFor="terms" className="block text-sm font-medium text-gray-700">
          Terms & Conditions
        </label>
        <textarea
          id="terms"
          rows={3}
          value={formData.terms || ''}
          onChange={(e) => updateField('terms', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Payment terms and conditions..."
        />
      </div>
    </div>
  )
}
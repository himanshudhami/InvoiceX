import { useInvoiceForm } from './form-context'

export const FromDetails = () => {
  const { formData, updateField, companies } = useInvoiceForm()

  return (
    <div className="space-y-2">
      <label htmlFor="companyId" className="block text-sm font-medium text-gray-700">
        From
      </label>
      <select
        id="companyId"
        value={formData.companyId || ''}
        onChange={(e) => updateField('companyId', e.target.value)}
        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="">Select your company</option>
        {companies.map((company) => (
          <option key={company.id} value={company.id}>
            {company.name}
          </option>
        ))}
      </select>
      
      {formData.companyId && (() => {
        const company = companies.find(c => c.id === formData.companyId)
        return company ? (
          <div className="mt-2 p-3 bg-gray-50 rounded-md text-sm">
            <p className="font-medium">{company.name}</p>
            {company.addressLine1 && <p>{company.addressLine1}</p>}
            {company.addressLine2 && <p>{company.addressLine2}</p>}
            {(company.city || company.state || company.zipCode) && (
              <p>
                {[company.city, company.state, company.zipCode]
                  .filter(Boolean)
                  .join(', ')}
              </p>
            )}
            {company.email && <p>{company.email}</p>}
            {company.phone && <p>{company.phone}</p>}
            {company.taxNumber && <p>Tax ID: {company.taxNumber}</p>}
          </div>
        ) : null
      })()}
    </div>
  )
}
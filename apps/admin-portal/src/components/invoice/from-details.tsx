import { useInvoiceForm } from './form-context'
import { CompanySelect } from '@/components/ui/CompanySelect'

export const FromDetails = () => {
  const { formData, updateField, companies } = useInvoiceForm()

  return (
    <div className="space-y-2">
      <label htmlFor="companyId" className="block text-sm font-medium text-gray-700">
        From
      </label>
      <CompanySelect
        companies={companies}
        value={formData.companyId || ''}
        onChange={(value) => updateField('companyId', value)}
        placeholder="Select your company"
      />
      
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

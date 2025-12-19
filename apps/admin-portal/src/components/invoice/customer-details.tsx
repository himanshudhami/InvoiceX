import { useInvoiceForm } from './form-context'
import { CustomerSelect } from '@/components/ui/CustomerSelect'

export const CustomerDetails = () => {
  const { formData, updateField, customers } = useInvoiceForm()

  return (
    <div className="space-y-2">
      <label htmlFor="customerId" className="block text-sm font-medium text-gray-700">
        Bill To
      </label>
      <CustomerSelect
        customers={customers}
        value={formData.customerId || ''}
        onChange={(value) => updateField('customerId', value)}
        placeholder="Select a customer"
      />
      
      {formData.customerId && (() => {
        const customer = customers.find(c => c.id === formData.customerId)
        return customer ? (
          <div className="mt-2 p-3 bg-gray-50 rounded-md text-sm">
            <p className="font-medium">{customer.name}</p>
            {customer.companyName && <p>{customer.companyName}</p>}
            {customer.addressLine1 && <p>{customer.addressLine1}</p>}
            {customer.addressLine2 && <p>{customer.addressLine2}</p>}
            {(customer.city || customer.state || customer.zipCode) && (
              <p>
                {[customer.city, customer.state, customer.zipCode]
                  .filter(Boolean)
                  .join(', ')}
              </p>
            )}
            {customer.email && <p>{customer.email}</p>}
            {customer.phone && <p>{customer.phone}</p>}
          </div>
        ) : null
      })()}
    </div>
  )
}

import { FC, useState } from 'react'
import { useCustomers } from '@/hooks/api'
import { Customer } from '@/services/api/types'

interface Props {
  selectedCustomer?: Customer
  onCustomerSelect: (customer: Customer) => void
  pdfMode?: boolean
}

const CustomerSelector: FC<Props> = ({ selectedCustomer, onCustomerSelect, pdfMode }) => {
  const [isOpen, setIsOpen] = useState(false)
  const { data: customers = [], isLoading, error } = useCustomers()

  if (pdfMode) {
    return <span>{selectedCustomer?.name || 'Select Customer'}</span>
  }

  if (error) {
    return (
      <div className="text-red-500 text-sm p-2">
        Failed to load customers. Please try again.
      </div>
    )
  }

  return (
    <div className="relative">
      <button
        type="button"
        className="w-full text-left bg-transparent border-none outline-none p-1 focus:bg-gray-50 focus:ring-1 focus:ring-primary/20 rounded"
        onClick={() => setIsOpen(!isOpen)}
        disabled={isLoading}
      >
        {isLoading ? 'Loading...' : (selectedCustomer?.name || 'Select Customer')}
      </button>
      
      {isOpen && !isLoading && (
        <div className="absolute top-full left-0 right-0 bg-white border border-gray-200 rounded-md shadow-lg z-50 max-h-60 overflow-y-auto">
          {customers.length === 0 ? (
            <div className="px-3 py-2 text-gray-500">No customers available</div>
          ) : (
            customers.map((customer: Customer) => (
              <button
                key={customer.id}
                type="button"
                className="block w-full text-left px-3 py-2 hover:bg-gray-50 border-none bg-transparent"
                onClick={() => {
                  onCustomerSelect(customer)
                  setIsOpen(false)
                }}
              >
                <div className="font-medium">{customer.name}</div>
                <div className="text-sm text-gray-600">{customer.companyName || 'No company'}</div>
              </button>
            ))
          )}
        </div>
      )}
    </div>
  )
}

export default CustomerSelector
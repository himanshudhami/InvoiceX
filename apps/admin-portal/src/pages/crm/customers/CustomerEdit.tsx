import { useParams, useNavigate } from 'react-router-dom'
import { useCustomer } from '@/hooks/api/useCustomers'
import { CustomerForm } from '@/components/forms/CustomerForm'

const CustomerEdit = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: customer, isLoading, error } = useCustomer(id!)

  const handleSuccess = () => {
    navigate('/customers')
  }

  const handleCancel = () => {
    navigate('/customers')
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error || !customer) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load customer</div>
        <button
          onClick={() => navigate('/customers')}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Back to Customers
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Edit Customer</h1>
        <p className="text-gray-600 mt-2">Update customer information</p>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <CustomerForm
          customer={customer}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </div>
    </div>
  )
}

export default CustomerEdit
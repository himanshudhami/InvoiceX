import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { CustomerForm } from '@/components/forms/CustomerForm'

const CustomerCreate = () => {
  const navigate = useNavigate()

  const handleSuccess = () => {
    navigate('/customers')
  }

  const handleCancel = () => {
    navigate('/customers')
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Add Customer</h1>
        <p className="text-gray-600 mt-2">Create a new customer record</p>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <CustomerForm
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </div>
    </div>
  )
}

export default CustomerCreate
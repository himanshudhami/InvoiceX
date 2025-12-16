import { useNavigate } from 'react-router-dom'
import { QuoteForm } from '@/components/forms/QuoteForm'

const QuoteCreate = () => {
  const navigate = useNavigate()

  const handleSuccess = () => {
    navigate('/quotes')
  }

  const handleCancel = () => {
    navigate('/quotes')
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Create Quote</h1>
        <p className="text-gray-600 mt-2">Create a new quote with line items and discounts</p>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <QuoteForm
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </div>
    </div>
  )
}

export default QuoteCreate

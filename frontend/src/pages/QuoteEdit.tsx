import { useParams, useNavigate } from 'react-router-dom'
import { QuoteForm } from '@/components/forms/QuoteForm'
import { useQuote } from '@/hooks/api/useQuotes'

const QuoteEdit = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: quote, isLoading, error } = useQuote(id!)

  const handleSuccess = () => {
    navigate('/quotes')
  }

  const handleCancel = () => {
    navigate('/quotes')
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error || !quote) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">
          {error ? 'Failed to load quote' : 'Quote not found'}
        </div>
        <button
          onClick={() => navigate('/quotes')}
          className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
        >
          Back to Quotes
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Edit Quote</h1>
        <p className="text-gray-600 mt-2">
          Update quote {quote.quoteNumber} details and information
        </p>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <QuoteForm
          quote={quote}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </div>
    </div>
  )
}

export default QuoteEdit

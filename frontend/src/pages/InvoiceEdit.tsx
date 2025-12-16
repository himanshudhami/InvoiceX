import { useParams, useNavigate } from 'react-router-dom'
import { useInvoice } from '@/hooks/api/useInvoices'
import { InvoiceForm } from '@/components/invoice/form'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'

const InvoiceEdit = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: invoice, isLoading, error } = useInvoice(id!)

  const handleSuccess = () => {
    navigate('/invoices')
  }

  const handleCancel = () => {
    navigate('/invoices')
  }

  const handleBack = () => {
    navigate('/invoices')
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error || !invoice) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load invoice</div>
        <Button onClick={handleBack} variant="outline">
          Back to Invoices
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header with Back Button */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button
            variant="ghost"
            size="icon"
            onClick={handleBack}
            className="rounded-full"
          >
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Edit Invoice</h1>
            <p className="text-gray-600 mt-2">Update invoice #{invoice.invoiceNumber}</p>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <InvoiceForm
          invoice={invoice}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </div>
    </div>
  )
}

export default InvoiceEdit
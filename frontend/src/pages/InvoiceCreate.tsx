import { useNavigate } from 'react-router-dom'
import { InvoiceForm } from '@/components/invoice/form'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'

const InvoiceCreate = () => {
  const navigate = useNavigate()

  const handleSuccess = () => {
    navigate('/invoices')
  }

  const handleCancel = () => {
    navigate('/invoices')
  }

  const handleBack = () => {
    navigate('/invoices')
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
            <h1 className="text-3xl font-bold text-gray-900">Create Invoice</h1>
            <p className="text-gray-600 mt-2">Create a new invoice with line items</p>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <InvoiceForm
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </div>
    </div>
  )
}

export default InvoiceCreate
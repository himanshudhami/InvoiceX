import { useParams, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { useQuote, useQuoteItems, useSendQuote, useAcceptQuote, useRejectQuote, useConvertQuoteToInvoice, useUpdateQuote } from '@/hooks/api/useQuotes'
import { useCustomers } from '@/hooks/api/useCustomers'
import { useCompanies } from '@/hooks/api/useCompanies'
import { format } from 'date-fns'
import { formatCurrency } from '@/lib/currency'
import {
  ArrowLeft,
  Download,
  Mail,
  Copy,
  Send,
  CheckCircle,
  XCircle,
  FileText,
  Edit,
  Clock,
  AlertCircle,
  MoreHorizontal
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { QuoteTemplateSelectModal } from '@/components/modals/QuoteTemplateSelectModal'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

const QuoteView = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { data: quote, isLoading, error } = useQuote(id!)
  const { data: quoteItems = [] } = useQuoteItems(id!)
  // Scope customers to quote company to avoid cross-company mismatches
  const { data: customers = [] } = useCustomers(quote?.companyId)
  const { data: companies = [] } = useCompanies()

  const sendQuote = useSendQuote()
  const acceptQuote = useAcceptQuote()
  const rejectQuote = useRejectQuote()
  const convertQuoteToInvoice = useConvertQuoteToInvoice()
  const updateQuote = useUpdateQuote()

  const [templateModal, setTemplateModal] = useState(false)

  const customer = customers.find(c => c.id === quote?.partyId)
  const company = companies.find(c => c.id === quote?.companyId)

  const handleEdit = () => {
    navigate(`/quotes/${id}/edit`)
  }

  const handleBack = () => {
    navigate('/quotes')
  }

  const handleSend = async () => {
    if (quote) {
      await sendQuote.mutateAsync(quote.id)
    }
  }

  const handleAccept = async () => {
    if (quote) {
      await acceptQuote.mutateAsync(quote.id)
    }
  }

  const handleReject = async () => {
    if (quote) {
      await rejectQuote.mutateAsync({ id: quote.id })
    }
  }

  const handleConvertToInvoice = async () => {
    if (quote) {
      await convertQuoteToInvoice.mutateAsync(quote.id)
    }
  }

  const handleStatusChange = async (newStatus: string) => {
    if (quote) {
      await updateQuote.mutateAsync({
        id: quote.id,
        data: { ...quote, status: newStatus }
      })
    }
  }

  const getStatusBadge = (status?: string) => {
    switch (status?.toLowerCase()) {
      case 'accepted':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
            <CheckCircle className="w-3 h-3 mr-1" />
            Accepted
          </span>
        )
      case 'rejected':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
            <XCircle className="w-3 h-3 mr-1" />
            Rejected
          </span>
        )
      case 'sent':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
            <Send className="w-3 h-3 mr-1" />
            Sent
          </span>
        )
      case 'viewed':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
            <Clock className="w-3 h-3 mr-1" />
            Viewed
          </span>
        )
      case 'expired':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
            <AlertCircle className="w-3 h-3 mr-1" />
            Expired
          </span>
        )
      case 'draft':
      default:
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
            <Edit className="w-3 h-3 mr-1" />
            Draft
          </span>
        )
    }
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

  const canSend = quote.status === 'draft'
  const canAccept = quote.status === 'sent' || quote.status === 'viewed'
  const canReject = quote.status === 'sent' || quote.status === 'viewed'
  const canConvert = quote.status === 'accepted'

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" size="sm" onClick={handleBack}>
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Quotes
          </Button>
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{quote.quoteNumber}</h1>
            <p className="text-gray-600">Quote details and status</p>
          </div>
        </div>

        <div className="flex items-center space-x-2">
        {getStatusBadge(quote.status)}

        <Button variant="outline" size="sm" onClick={() => setTemplateModal(true)}>
        <Download className="h-4 w-4 mr-2" />
        Download PDF
        </Button>

        <Button variant="outline" size="sm">
        <Mail className="h-4 w-4 mr-2" />
        Email
        </Button>

        <Button onClick={handleEdit} size="sm">
        <Edit className="h-4 w-4 mr-2" />
        Edit
        </Button>

        <DropdownMenu>
        <DropdownMenuTrigger asChild>
        <Button variant="outline" size="icon">
        <MoreHorizontal className="h-4 w-4" />
        </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuItem onClick={handleEdit}>
          <Edit className="mr-2 h-4 w-4" />
        Edit Quote
        </DropdownMenuItem>

        {canSend && (
          <DropdownMenuItem onClick={handleSend} disabled={sendQuote.isPending}>
            <Send className="mr-2 h-4 w-4" />
          Send Quote
        </DropdownMenuItem>
        )}

        {canAccept && (
          <DropdownMenuItem onClick={handleAccept} disabled={acceptQuote.isPending}>
            <CheckCircle className="mr-2 h-4 w-4" />
          Accept Quote
        </DropdownMenuItem>
        )}

        {canReject && (
        <DropdownMenuItem onClick={handleReject} disabled={rejectQuote.isPending}>
            <XCircle className="mr-2 h-4 w-4" />
            Reject Quote
        </DropdownMenuItem>
        )}

          {canConvert && (
              <DropdownMenuItem onClick={handleConvertToInvoice} disabled={convertQuoteToInvoice.isPending}>
                  <FileText className="mr-2 h-4 w-4" />
                  Convert to Invoice
                </DropdownMenuItem>
              )}

              <DropdownMenuSeparator />

              {/* Status Change Options */}
              <DropdownMenuItem
                onClick={() => handleStatusChange('draft')}
                disabled={updateQuote.isPending}
              >
                <Edit className="mr-2 h-4 w-4" />
                Mark as Draft
              </DropdownMenuItem>

              <DropdownMenuItem
                onClick={() => handleStatusChange('sent')}
                disabled={updateQuote.isPending}
              >
                <Send className="mr-2 h-4 w-4" />
                Mark as Sent
              </DropdownMenuItem>

              <DropdownMenuItem
                onClick={() => handleStatusChange('viewed')}
                disabled={updateQuote.isPending}
              >
                <Clock className="mr-2 h-4 w-4" />
                Mark as Viewed
              </DropdownMenuItem>

              <DropdownMenuItem
                onClick={() => handleStatusChange('accepted')}
                disabled={updateQuote.isPending}
              >
                <CheckCircle className="mr-2 h-4 w-4" />
                Mark as Accepted
              </DropdownMenuItem>

              <DropdownMenuItem
                onClick={() => handleStatusChange('rejected')}
                disabled={updateQuote.isPending}
              >
                <XCircle className="mr-2 h-4 w-4" />
                Mark as Rejected
              </DropdownMenuItem>

              <DropdownMenuItem
                onClick={() => handleStatusChange('expired')}
                disabled={updateQuote.isPending}
              >
                <AlertCircle className="mr-2 h-4 w-4" />
                Mark as Expired
              </DropdownMenuItem>

              <DropdownMenuItem
                onClick={() => handleStatusChange('cancelled')}
                disabled={updateQuote.isPending}
              >
                <XCircle className="mr-2 h-4 w-4" />
                Mark as Cancelled
              </DropdownMenuItem>

              <DropdownMenuSeparator />

              <DropdownMenuItem>
                <Copy className="mr-2 h-4 w-4" />
                Duplicate
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      {/* Quote Details */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Quote Information */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Quote Information</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="text-sm font-medium text-gray-500">Quote Date</label>
                <p className="text-sm text-gray-900">
                  {format(new Date(quote.quoteDate), 'PPP')}
                </p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Valid Until</label>
                <p className="text-sm text-gray-900">
                  {quote.validUntil ? format(new Date(quote.validUntil), 'PPP') : '—'}
                </p>
              </div>
            </div>
          </div>

          {/* Line Items */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Line Items</h2>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Description
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Quantity
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Unit Price
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Total
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {quoteItems.map((item) => (
                    <tr key={item.id}>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {item.description}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {item.quantity}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {formatCurrency(item.unitPrice, quote.currency)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {formatCurrency(item.lineTotal, quote.currency)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Notes and Terms */}
          {(quote.notes || quote.terms) && (
            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">Additional Information</h2>
              {quote.notes && (
                <div className="mb-4">
                  <label className="text-sm font-medium text-gray-500">Notes</label>
                  <p className="text-sm text-gray-900 mt-1">{quote.notes}</p>
                </div>
              )}
              {quote.terms && (
                <div>
                  <label className="text-sm font-medium text-gray-500">Terms & Conditions</label>
                  <p className="text-sm text-gray-900 mt-1">{quote.terms}</p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Customer & Company Info */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Customer & Company</h2>
            {customer && (
              <div className="mb-4">
                <label className="text-sm font-medium text-gray-500">Customer</label>
                <p className="text-sm text-gray-900">{customer.name}</p>
                {customer.companyName && (
                  <p className="text-sm text-gray-600">{customer.companyName}</p>
                )}
                {customer.email && (
                  <p className="text-sm text-gray-600">{customer.email}</p>
                )}
              </div>
            )}
            {company && (
              <div>
                <label className="text-sm font-medium text-gray-500">Company</label>
                <p className="text-sm text-gray-900">{company.name}</p>
              </div>
            )}
          </div>

          {/* Financial Summary */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Financial Summary</h2>
            <div className="space-y-2">
            <div className="flex justify-between">
            <span className="text-sm text-gray-600">Subtotal:</span>
            <span className="text-sm font-medium">{formatCurrency(quote.subtotal, quote.currency)}</span>
            </div>
            {(quote.discountAmount || 0) > 0 && (
            <div className="flex justify-between">
            <span className="text-sm text-gray-600">Discount:</span>
            <span className="text-sm font-medium text-green-600">
            -{formatCurrency(quote.discountAmount || 0, quote.currency)}
            </span>
            </div>
            )}
            {(quote.taxAmount || 0) > 0 && (
            <div className="flex justify-between">
            <span className="text-sm text-gray-600">Tax:</span>
            <span className="text-sm font-medium">{formatCurrency(quote.taxAmount || 0, quote.currency)}</span>
            </div>
            )}
              <hr className="my-2" />
              <div className="flex justify-between">
                <span className="text-lg font-semibold text-gray-900">Total:</span>
                <span className="text-lg font-bold text-blue-600">
                  {formatCurrency(quote.totalAmount, quote.currency)}
                </span>
              </div>
            </div>
          </div>

        </div>
      </div>

      {/* Template Select Modal */}
      <QuoteTemplateSelectModal
        isOpen={templateModal}
        onClose={() => setTemplateModal(false)}
        quote={quote}
        quoteItems={quoteItems}
        customer={customer}
        company={company}
      />
    </div>
  )
}

export default QuoteView

import { useParams, useNavigate } from 'react-router-dom'
import { useState, useCallback } from 'react'
import { useInvoice, useInvoiceItems } from '@/hooks/api/useInvoices'
import { useCreditNotesByInvoice } from '@/features/credit-notes/hooks'
import { useCustomers } from '@/hooks/api/useCustomers'
import { useCompanies } from '@/hooks/api/useCompanies'
import { format } from 'date-fns'
import { formatINR } from '@/lib/currency'
import {
  ArrowLeft,
  Download,
  Mail,
  Copy,
  Share2,
  MoreHorizontal,
  Edit,
  Clock,
  CheckCircle,
  XCircle,
  AlertCircle,
  FileCheck,
  FileMinus,
  ExternalLink
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { TemplateSelectModal } from '@/components/modals/TemplateSelectModal'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { EInvoiceStatusBadge } from '@/components/invoice/EInvoiceStatusBadge'
import { IrnGenerationButton } from '@/components/invoice/IrnGenerationButton'
import { QrCodeDisplay } from '@/components/invoice/QrCodeDisplay'
import {
  MarkAsPaidDrawer,
  createInvoicePaymentEntity,
  InvoicePaymentStatus,
  type PaymentEntity,
  type MarkAsPaidFormData,
  type MarkAsPaidResult,
} from '@/components/payments'
import { invoiceService } from '@/services/api/billing/invoiceService'

const InvoiceView = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  
  const { data: invoice, isLoading, error } = useInvoice(id!)
  const { data: invoiceItems = [] } = useInvoiceItems(id!)
  // Scope customers to the invoice company to avoid cross-company mismatches
  const { data: customers = [] } = useCustomers(invoice?.companyId)
  const { data: companies = [] } = useCompanies()
  const { data: creditNotes = [] } = useCreditNotesByInvoice(id!, !!invoice)

  const customer = customers.find(c => c.id === invoice?.partyId)
  const company = companies.find(c => c.id === invoice?.companyId)
  const [templateModal, setTemplateModal] = useState(false)
  const [paymentDrawerOpen, setPaymentDrawerOpen] = useState(false)
  const [paymentEntity, setPaymentEntity] = useState<PaymentEntity | null>(null)

  const handleEdit = () => {
    navigate(`/invoices/${id}/edit`)
  }

  const handleBack = () => {
    navigate('/invoices')
  }

  const handleDuplicate = () => {
    // TODO: Implement duplicate functionality
    console.log('Duplicate invoice')
  }

  const handleMarkAsPaid = () => {
    if (!invoice || !invoice.companyId) return
    const entity = createInvoicePaymentEntity({
      id: invoice.id,
      companyId: invoice.companyId,
      invoiceNumber: invoice.invoiceNumber,
      customerName: customer?.name,
      totalAmount: invoice.totalAmount,
      paidAmount: invoice.paidAmount,
      currency: invoice.currency,
    })
    setPaymentEntity(entity)
    setPaymentDrawerOpen(true)
  }

  const handlePaymentSubmit = useCallback(async (data: MarkAsPaidFormData): Promise<MarkAsPaidResult> => {
    try {
      await invoiceService.recordPayment(data.entityId, {
        amount: data.amount,
        paymentDate: data.paymentDate,
        paymentMethod: data.paymentMethod,
        referenceNumber: data.referenceNumber,
        notes: data.notes,
        bankAccountId: data.bankAccountId,
      })
      return {
        success: true,
        message: 'Payment recorded successfully',
      }
    } catch (err: any) {
      return {
        success: false,
        error: err.message || 'Failed to record payment',
      }
    }
  }, [])

  const handlePaymentSuccess = () => {
    setPaymentDrawerOpen(false)
    setPaymentEntity(null)
    // Refetch invoice data to update payment status
    window.location.reload() // Simple refresh for now
  }

  const handleSendReminder = () => {
    // TODO: Implement send reminder functionality
    console.log('Send reminder')
  }

  const handleVoid = () => {
    // TODO: Implement void functionality
    console.log('Void invoice')
  }

  const getStatusBadge = (status?: string) => {
    switch (status?.toLowerCase()) {
      case 'paid':
        return {
          color: 'bg-green-50 text-green-700 border-green-200',
          icon: <CheckCircle className="w-4 h-4" />,
          label: 'Paid'
        }
      case 'sent':
        return {
          color: 'bg-blue-50 text-blue-700 border-blue-200',
          icon: <Clock className="w-4 h-4" />,
          label: 'Sent'
        }
      case 'overdue':
        return {
          color: 'bg-red-50 text-red-700 border-red-200',
          icon: <AlertCircle className="w-4 h-4" />,
          label: 'Overdue'
        }
      case 'draft':
        return {
          color: 'bg-gray-50 text-gray-700 border-gray-200',
          icon: <Edit className="w-4 h-4" />,
          label: 'Draft'
        }
      case 'cancelled':
      case 'void':
        return {
          color: 'bg-gray-50 text-gray-500 border-gray-200',
          icon: <XCircle className="w-4 h-4" />,
          label: 'Void'
        }
      default:
        return {
          color: 'bg-gray-50 text-gray-600 border-gray-200',
          icon: null,
          label: status || 'Unknown'
        }
    }
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

  const statusBadge = getStatusBadge(invoice.status)

  return (
    <div className="min-h-screen bg-gray-50/50">
      {/* Modern Header */}
      <div className="bg-white border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center space-x-4">
              <Button
                variant="ghost"
                size="icon"
                onClick={handleBack}
                className="rounded-full"
              >
                <ArrowLeft className="h-4 w-4" />
              </Button>
              <div className="flex items-center space-x-3">
                <div>
                  <div className="flex items-center space-x-2 flex-wrap gap-2">
                    <h1 className="text-lg font-medium text-gray-900">
                      Invoice #{invoice.invoiceNumber}
                    </h1>
                    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${statusBadge.color}`}>
                      {statusBadge.icon}
                      {statusBadge.label}
                    </span>
                    {invoice.invoiceType && invoice.invoiceType !== 'standard' && (
                      <EInvoiceStatusBadge
                        status={invoice.eInvoiceStatus}
                        irn={invoice.irn}
                      />
                    )}
                  </div>
                  <p className="text-sm text-gray-500">
                    {customer?.name || 'Unknown Customer'}
                  </p>
                </div>
              </div>
            </div>
            
            <div className="flex items-center space-x-2">
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

              {/* E-Invoice IRN Button - only show for applicable invoices */}
              {invoice.invoiceType && invoice.invoiceType !== 'standard' && invoice.status !== 'draft' && (
                <IrnGenerationButton
                  invoiceId={invoice.id}
                  invoiceNumber={invoice.invoiceNumber}
                  eInvoiceStatus={invoice.eInvoiceStatus}
                  irn={invoice.irn}
                  size="sm"
                  variant="outline"
                />
              )}

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="icon">
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
                  <DropdownMenuItem onClick={handleDuplicate}>
                    <Copy className="mr-2 h-4 w-4" />
                    Duplicate
                  </DropdownMenuItem>
                  <DropdownMenuItem>
                    <Share2 className="mr-2 h-4 w-4" />
                    Share link
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  {invoice.status !== 'paid' && (
                    <DropdownMenuItem onClick={handleMarkAsPaid}>
                      <CheckCircle className="mr-2 h-4 w-4" />
                      Mark as paid
                    </DropdownMenuItem>
                  )}
                  {invoice.status === 'sent' && (
                    <DropdownMenuItem onClick={handleSendReminder}>
                      <Clock className="mr-2 h-4 w-4" />
                      Send reminder
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  {/* Credit Note option - shown for paid or partially paid invoices */}
                  {(invoice.status === 'paid' || (invoice.paidAmount && invoice.paidAmount > 0)) && (
                    <DropdownMenuItem
                      onClick={() => navigate(`/credit-notes/from-invoice/${invoice.id}`)}
                      className="text-amber-600 focus:text-amber-600"
                    >
                      <FileCheck className="mr-2 h-4 w-4" />
                      Issue Credit Note
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuItem
                    onClick={handleVoid}
                    className="text-red-600 focus:text-red-600"
                  >
                    <XCircle className="mr-2 h-4 w-4" />
                    Void invoice
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </div>
      </div>

      {/* Invoice Display */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Invoice Content */}
          <div className="lg:col-span-2">
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
              {/* Invoice Header */}
              <div className="p-6 border-b">
                <div className="grid grid-cols-2 gap-6">
                  {/* From Section */}
                  <div>
                    <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-3">From</p>
                    <div className="space-y-1">
                      <p className="text-sm font-medium text-gray-900">
                        {company?.name || 'Your Company'}
                      </p>
                      {company && (
                        <div className="text-sm text-gray-600 space-y-0.5">
                          {company.addressLine1 && <p>{company.addressLine1}</p>}
                          {company.addressLine2 && <p>{company.addressLine2}</p>}
                          {(company.city || company.state || company.zipCode) && (
                            <p>
                              {[company.city, company.state, company.zipCode]
                                .filter(Boolean)
                                .join(', ')}
                            </p>
                          )}
                          {company.country && <p>{company.country}</p>}
                          {company.taxNumber && (
                            <p className="text-xs text-gray-500 mt-2">VAT: {company.taxNumber}</p>
                          )}
                        </div>
                      )}
                    </div>
                  </div>

                  {/* To Section */}
                  <div>
                    <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-3">To</p>
                    <div className="space-y-1">
                      <p className="text-sm font-medium text-gray-900">
                        {customer?.name || 'Unknown Customer'}
                      </p>
                      {customer && (
                        <div className="text-sm text-gray-600 space-y-0.5">
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
                          {customer.country && <p>{customer.country}</p>}
                          {customer.taxNumber && (
                            <p className="text-xs text-gray-500 mt-2">VAT: {customer.taxNumber}</p>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>


              {/* Line Items */}
              <div className="p-6">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-gray-200">
                      <th className="pb-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Description
                      </th>
                      <th className="pb-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Qty
                      </th>
                      <th className="pb-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Price
                      </th>
                      <th className="pb-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Total
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {invoiceItems.length > 0 ? (
                      invoiceItems.map((item, index) => (
                        <tr key={item.id || index} className="group">
                          <td className="py-4 text-sm">
                            <div>
                              <p className="font-medium text-gray-900">{item.description}</p>
                              {item.taxRate !== undefined && item.taxRate > 0 && (
                                <p className="text-xs text-gray-500 mt-1">Tax: {item.taxRate}%</p>
                              )}
                            </div>
                          </td>
                          <td className="py-4 text-sm text-center text-gray-600">
                            {item.quantity}
                          </td>
                          <td className="py-4 text-sm text-right text-gray-600">
                            {formatINR(item.unitPrice)}
                          </td>
                          <td className="py-4 text-sm text-right font-medium text-gray-900">
                            {formatINR(item.lineTotal)}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={4} className="py-8 text-center text-gray-500">
                          No line items found
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>

                {/* Totals */}
                <div className="mt-6 pt-6 border-t border-gray-200">
                  <div className="flex justify-end">
                    <div className="w-64 space-y-2">
                      <div className="flex justify-between text-sm">
                        <span className="text-gray-600">Subtotal</span>
                        <span className="text-gray-900">
                          {formatINR(invoice.subtotal)}
                        </span>
                      </div>
                      {invoice.taxAmount !== undefined && invoice.taxAmount > 0 && (
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Tax</span>
                          <span className="text-gray-900">
                            {formatINR(invoice.taxAmount || 0)}
                        </span>
                        </div>
                      )}
                      {invoice.discountAmount !== undefined && invoice.discountAmount > 0 && (
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Discount</span>
                          <span className="text-red-600">
                            -{formatINR(invoice.discountAmount || 0)}
                          </span>
                        </div>
                      )}
                      <div className="flex justify-between pt-2 border-t border-gray-200">
                        <span className="text-base font-medium text-gray-900">Total</span>
                        <span className="text-base font-medium text-gray-900">
                          {formatINR(invoice.totalAmount)}
                        </span>
                      </div>
                      {invoice.paidAmount !== undefined && invoice.paidAmount > 0 && (
                        <>
                          <div className="flex justify-between text-sm text-green-600">
                            <span>Paid</span>
                            <span>{formatINR(invoice.paidAmount || 0)}</span>
                          </div>
                          {invoice.paidAmount !== undefined && invoice.paidAmount < invoice.totalAmount && (
                            <div className="flex justify-between text-sm font-medium">
                              <span className="text-gray-900">Balance Due</span>
                              <span className="text-red-600">
                                {formatINR(invoice.totalAmount - (invoice.paidAmount || 0))}
                              </span>
                            </div>
                          )}
                        </>
                      )}
                    </div>
                  </div>
                </div>
              </div>

              {/* Notes, Terms & Payment Instructions */}
              {(invoice.notes || invoice.terms || invoice.paymentInstructions || company?.paymentInstructions) && (
                <div className="p-6 bg-gray-50 border-t">
                  <div className="space-y-4">
                    {invoice.notes && (
                      <div>
                        <h3 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
                          Notes
                        </h3>
                        <p className="text-sm text-gray-600 whitespace-pre-wrap">{invoice.notes}</p>
                      </div>
                    )}
                    {invoice.terms && (
                      <div>
                        <h3 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
                          Terms & Conditions
                        </h3>
                        <p className="text-sm text-gray-600 whitespace-pre-wrap">{invoice.terms}</p>
                      </div>
                    )}
                    {(invoice.paymentInstructions || company?.paymentInstructions) && (
                      <div>
                        <h3 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
                          Payment Instructions
                        </h3>
                        <p className="text-sm text-gray-600 whitespace-pre-wrap">
                          {invoice.paymentInstructions || company?.paymentInstructions}
                        </p>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Sidebar */}
          <div className="lg:col-span-1">
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-6 sticky top-6">
              {/* Amount Due */}
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
                  {invoice.status === 'paid' ? 'Total Amount' :
                   invoice.paidAmount !== undefined && invoice.paidAmount > 0 && invoice.paidAmount < invoice.totalAmount ? 'Balance Due' : 'Total Amount'}
                </p>
                <p className="text-3xl font-bold text-gray-900">
                  {formatINR(
                    invoice.status === 'paid' ? invoice.totalAmount :
                    invoice.paidAmount !== undefined && invoice.paidAmount > 0 && invoice.paidAmount < invoice.totalAmount
                      ? invoice.totalAmount - invoice.paidAmount
                      : invoice.totalAmount
                  )}
                </p>
              </div>

              {/* Payment Status */}
              <div className="pt-4 border-t">
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-3">
                  Payment Status
                </p>
                {/* Tally-imported invoices: status='paid' but paidAmount=0 */}
                {invoice.status === 'paid' && (!invoice.paidAmount || invoice.paidAmount === 0) ? (
                  <div className="space-y-2">
                    <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">
                      <CheckCircle className="h-3.5 w-3.5" />
                      <span>Fully Paid</span>
                    </div>
                    <p className="text-xs text-gray-500">
                      Imported from Tally - payment was recorded in source system
                    </p>
                  </div>
                ) : (
                  /* New invoices with payment tracking */
                  <InvoicePaymentStatus
                    invoiceId={invoice.id}
                    showDetails={true}
                    currency="INR"
                  />
                )}
              </div>

              {/* Credit Notes */}
              {creditNotes.length > 0 && (
                <div className="pt-4 border-t">
                  <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-3">
                    Credit Notes ({creditNotes.length})
                  </p>
                  <div className="space-y-2">
                    {creditNotes.map(cn => (
                      <button
                        key={cn.id}
                        onClick={() => navigate(`/credit-notes/${cn.id}`)}
                        className="w-full flex items-center justify-between p-2 rounded-lg hover:bg-gray-50 transition-colors text-left"
                      >
                        <div className="flex items-center gap-2">
                          <FileMinus className="h-4 w-4 text-amber-500" />
                          <div>
                            <p className="text-sm font-medium text-gray-900">{cn.creditNoteNumber}</p>
                            <p className="text-xs text-gray-500">
                              {cn.creditNoteDate ? format(new Date(cn.creditNoteDate), 'MMM dd, yyyy') : ''}
                            </p>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium text-red-600">
                            -{formatINR(cn.totalAmount)}
                          </span>
                          <ExternalLink className="h-3 w-3 text-gray-400" />
                        </div>
                      </button>
                    ))}
                    <div className="pt-2 border-t flex justify-between text-sm">
                      <span className="text-gray-500">Total Credited</span>
                      <span className="font-medium text-red-600">
                        -{formatINR(creditNotes.reduce((sum, cn) => sum + (cn.totalAmount || 0), 0))}
                      </span>
                    </div>
                  </div>
                </div>
              )}

              {/* E-Invoice QR Code */}
              {invoice.irn && invoice.qrCodeData && (
                <QrCodeDisplay
                  qrCodeData={invoice.qrCodeData}
                  irn={invoice.irn}
                  signedInvoice={invoice.eInvoiceSignedJson}
                  invoiceNumber={invoice.invoiceNumber}
                  size="sm"
                />
              )}

              {/* Invoice Details */}
              <div className="space-y-3">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Invoice Number</span>
                  <span className="font-medium text-gray-900">#{invoice.invoiceNumber}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Issue Date</span>
                  <span className="font-medium text-gray-900">
                    {format(new Date(invoice.invoiceDate), 'MMM dd, yyyy')}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Due Date</span>
                  <span className="font-medium text-gray-900">
                    {format(new Date(invoice.dueDate), 'MMM dd, yyyy')}
                  </span>
                </div>
                {invoice.poNumber && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">PO Number</span>
                    <span className="font-medium text-gray-900">{invoice.poNumber}</span>
                  </div>
                )}
                {invoice.projectName && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">Project</span>
                    <span className="font-medium text-gray-900">{invoice.projectName}</span>
                  </div>
                )}
              </div>

              {/* Customer Info */}
              <div className="pt-4 border-t">
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-3">
                  Customer
                </p>
                <div className="space-y-1">
                  <p className="text-sm font-medium text-gray-900">
                    {customer?.name || 'Unknown Customer'}
                  </p>
                  {customer?.email && (
                    <p className="text-sm text-gray-600">{customer.email}</p>
                  )}
                  {customer?.phone && (
                    <p className="text-sm text-gray-600">{customer.phone}</p>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <TemplateSelectModal
        isOpen={templateModal}
        onClose={() => setTemplateModal(false)}
        invoice={invoice}
        invoiceItems={invoiceItems}
        customer={customer}
        company={company}
      />

      <MarkAsPaidDrawer
        isOpen={paymentDrawerOpen}
        onClose={() => {
          setPaymentDrawerOpen(false)
          setPaymentEntity(null)
        }}
        entity={paymentEntity}
        onSubmit={handlePaymentSubmit}
        onSuccess={handlePaymentSuccess}
      />
    </div>
  )
}

export default InvoiceView

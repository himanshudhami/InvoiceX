import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useCreditNote, useIssueCreditNote, useCancelCreditNote } from '@/features/credit-notes/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useParties } from '@/features/parties/hooks'
import { format } from 'date-fns'
import { formatINR } from '@/lib/currency'
import {
  ArrowLeft,
  Download,
  Printer,
  CheckCircle,
  XCircle,
  ExternalLink,
  MoreHorizontal,
  AlertTriangle,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Modal } from '@/components/ui/Modal'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

const statusColors: Record<string, string> = {
  draft: 'bg-gray-100 text-gray-800',
  issued: 'bg-green-100 text-green-800',
  cancelled: 'bg-red-100 text-red-800',
}

const CreditNoteView = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [showIssueModal, setShowIssueModal] = useState(false)
  const [showCancelModal, setShowCancelModal] = useState(false)
  const [cancelReason, setCancelReason] = useState('')

  const { data: creditNote, isLoading, error } = useCreditNote(id!)
  const { data: companies = [] } = useCompanies()
  const { data: parties = [] } = useParties({ companyId: creditNote?.companyId })
  const issueCreditNote = useIssueCreditNote()
  const cancelCreditNote = useCancelCreditNote()

  const company = companies.find(c => c.id === creditNote?.companyId)
  const party = parties.find(p => p.id === creditNote?.partyId)

  const handleIssueConfirm = async () => {
    await issueCreditNote.mutateAsync(id!)
    setShowIssueModal(false)
  }

  const handleCancelConfirm = async () => {
    if (cancelReason) {
      await cancelCreditNote.mutateAsync({ id: id!, reason: cancelReason })
      setShowCancelModal(false)
      setCancelReason('')
    }
  }

  const handleViewInvoice = () => {
    if (creditNote?.originalInvoiceId) {
      navigate(`/invoices/${creditNote.originalInvoiceId}`)
    }
  }

  const handlePrint = () => {
    window.print()
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-gray-500">Loading credit note...</div>
      </div>
    )
  }

  if (error || !creditNote) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-red-500">Failed to load credit note</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b sticky top-0 z-10 print:hidden">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button variant="ghost" size="sm" onClick={() => navigate('/credit-notes')}>
                <ArrowLeft className="h-4 w-4 mr-2" />
                Back
              </Button>
              <div>
                <div className="flex items-center gap-2">
                  <h1 className="text-xl font-semibold text-gray-900">{creditNote.creditNoteNumber}</h1>
                  <Badge className={statusColors[creditNote.status || 'draft']}>
                    {creditNote.status || 'draft'}
                  </Badge>
                </div>
                <p className="text-sm text-gray-500">
                  Credit note for Invoice {creditNote.originalInvoiceNumber}
                </p>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={handlePrint}>
                <Printer className="h-4 w-4 mr-2" />
                Print
              </Button>
              <Button variant="outline" size="sm">
                <Download className="h-4 w-4 mr-2" />
                Download PDF
              </Button>

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm">
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={handleViewInvoice}>
                    <ExternalLink className="mr-2 h-4 w-4" />
                    View Original Invoice
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  {creditNote.status === 'draft' && (
                    <DropdownMenuItem onClick={() => setShowIssueModal(true)}>
                      <CheckCircle className="mr-2 h-4 w-4" />
                      Issue Credit Note
                    </DropdownMenuItem>
                  )}
                  {creditNote.status === 'issued' && (
                    <DropdownMenuItem onClick={() => setShowCancelModal(true)} className="text-red-600 focus:text-red-600">
                      <XCircle className="mr-2 h-4 w-4" />
                      Cancel Credit Note
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Cancelled Warning */}
        {creditNote.status === 'cancelled' && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3 print:bg-red-100">
            <AlertTriangle className="h-5 w-5 text-red-500 flex-shrink-0 mt-0.5" />
            <div>
              <p className="font-medium text-red-800">This credit note has been cancelled</p>
              {creditNote.cancelledAt && (
                <p className="text-sm text-red-700">
                  Cancelled on {format(new Date(creditNote.cancelledAt), 'dd MMM yyyy HH:mm')}
                </p>
              )}
            </div>
          </div>
        )}

        {/* Credit Note Document */}
        <div className="bg-white rounded-lg shadow-sm border overflow-hidden">
          {/* Document Header */}
          <div className="p-6 border-b bg-gray-50">
            <div className="flex justify-between">
              <div>
                <h2 className="text-2xl font-bold text-gray-900">CREDIT NOTE</h2>
                <p className="text-lg font-medium text-gray-700 mt-1">{creditNote.creditNoteNumber}</p>
              </div>
              <div className="text-right">
                <p className="text-sm text-gray-500">Date</p>
                <p className="font-medium">
                  {creditNote.creditNoteDate ? format(new Date(creditNote.creditNoteDate), 'dd MMM yyyy') : '-'}
                </p>
              </div>
            </div>
          </div>

          {/* From/To Section */}
          <div className="p-6 border-b">
            <div className="grid grid-cols-2 gap-8">
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">From</p>
                <p className="font-medium text-gray-900">{company?.name || 'Your Company'}</p>
                {company?.address && <p className="text-sm text-gray-600">{company.address}</p>}
                {company?.gstin && (
                  <p className="text-sm text-gray-600 mt-1">
                    <span className="font-medium">GSTIN:</span> {company.gstin}
                  </p>
                )}
              </div>
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">To</p>
                <p className="font-medium text-gray-900">{party?.name || 'Customer'}</p>
                {party?.address && <p className="text-sm text-gray-600">{party.address}</p>}
                {party?.gstin && (
                  <p className="text-sm text-gray-600 mt-1">
                    <span className="font-medium">GSTIN:</span> {party.gstin}
                  </p>
                )}
              </div>
            </div>
          </div>

          {/* Original Invoice Reference */}
          <div className="p-6 border-b bg-blue-50">
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">Original Invoice Reference</p>
            <div className="grid grid-cols-3 gap-4">
              <div>
                <p className="text-sm text-gray-500">Invoice Number</p>
                <p className="font-medium text-blue-600">{creditNote.originalInvoiceNumber}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Invoice Date</p>
                <p className="font-medium">
                  {creditNote.originalInvoiceDate ? format(new Date(creditNote.originalInvoiceDate), 'dd MMM yyyy') : '-'}
                </p>
              </div>
              <div>
                <Button variant="link" className="p-0 h-auto" onClick={handleViewInvoice}>
                  View Invoice <ExternalLink className="h-3 w-3 ml-1" />
                </Button>
              </div>
            </div>
          </div>

          {/* Reason */}
          <div className="p-6 border-b">
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">Reason for Credit Note</p>
            <p className="font-medium text-gray-900 capitalize">{creditNote.reason?.replace(/_/g, ' ')}</p>
            {creditNote.reasonDescription && (
              <p className="text-sm text-gray-600 mt-1">{creditNote.reasonDescription}</p>
            )}
          </div>

          {/* Line Items */}
          {creditNote.items && creditNote.items.length > 0 && (
            <div className="p-6 border-b">
              <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-4">Items</p>
              <table className="w-full">
                <thead>
                  <tr className="text-left text-xs text-gray-500 uppercase">
                    <th className="pb-2">Description</th>
                    <th className="pb-2 text-right">Qty</th>
                    <th className="pb-2 text-right">Unit Price</th>
                    <th className="pb-2 text-right">Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {creditNote.items.map((item, idx) => (
                    <tr key={item.id || idx} className="border-t">
                      <td className="py-3">
                        <p className="font-medium text-gray-900">{item.description}</p>
                        {item.hsnSacCode && (
                          <p className="text-xs text-gray-500">HSN/SAC: {item.hsnSacCode}</p>
                        )}
                      </td>
                      <td className="py-3 text-right">{item.quantity}</td>
                      <td className="py-3 text-right">{formatINR(item.unitPrice)}</td>
                      <td className="py-3 text-right font-medium">{formatINR(item.lineTotal)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Totals */}
          <div className="p-6">
            <div className="flex justify-end">
              <div className="w-64 space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Subtotal</span>
                  <span>{formatINR(creditNote.subtotal)}</span>
                </div>
                {(creditNote.totalCgst || 0) > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">CGST</span>
                    <span>{formatINR(creditNote.totalCgst || 0)}</span>
                  </div>
                )}
                {(creditNote.totalSgst || 0) > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">SGST</span>
                    <span>{formatINR(creditNote.totalSgst || 0)}</span>
                  </div>
                )}
                {(creditNote.totalIgst || 0) > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">IGST</span>
                    <span>{formatINR(creditNote.totalIgst || 0)}</span>
                  </div>
                )}
                {(creditNote.totalCess || 0) > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">Cess</span>
                    <span>{formatINR(creditNote.totalCess || 0)}</span>
                  </div>
                )}
                {(creditNote.discountAmount || 0) > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">Discount</span>
                    <span>-{formatINR(creditNote.discountAmount || 0)}</span>
                  </div>
                )}
                <div className="flex justify-between font-medium text-lg border-t pt-2">
                  <span>Total Credit</span>
                  <span>{formatINR(creditNote.totalAmount)}</span>
                </div>
              </div>
            </div>
          </div>

          {/* Notes & Terms */}
          {(creditNote.notes || creditNote.terms) && (
            <div className="p-6 border-t bg-gray-50">
              {creditNote.notes && (
                <div className="mb-4">
                  <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Notes</p>
                  <p className="text-sm text-gray-600">{creditNote.notes}</p>
                </div>
              )}
              {creditNote.terms && (
                <div>
                  <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Terms & Conditions</p>
                  <p className="text-sm text-gray-600">{creditNote.terms}</p>
                </div>
              )}
            </div>
          )}

          {/* ITC Reversal Notice */}
          {creditNote.itcReversalRequired && (
            <div className="p-6 border-t bg-amber-50">
              <div className="flex items-start gap-3">
                <AlertTriangle className="h-5 w-5 text-amber-500 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="font-medium text-amber-800">ITC Reversal Required</p>
                  <p className="text-sm text-amber-700">
                    As per 2025 Amendment, the recipient must reverse Input Tax Credit for this credit note.
                    {creditNote.totalAmount > 500000 && (
                      <> For tax amounts exceeding ₹5 Lakhs, a CA/CMA certificate is required.</>
                    )}
                  </p>
                  {creditNote.itcReversalConfirmed ? (
                    <p className="text-sm text-green-700 mt-2">
                      <CheckCircle className="h-4 w-4 inline mr-1" />
                      ITC Reversal confirmed on {creditNote.itcReversalDate ? format(new Date(creditNote.itcReversalDate), 'dd MMM yyyy') : 'N/A'}
                    </p>
                  ) : (
                    <p className="text-sm text-amber-700 mt-2">
                      Awaiting ITC reversal confirmation from recipient
                    </p>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* E-Invoice / IRN Info */}
          {creditNote.eInvoiceApplicable && (
            <div className="p-6 border-t">
              <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">E-Invoice Details</p>
              {creditNote.irn ? (
                <div className="space-y-2">
                  <div>
                    <p className="text-sm text-gray-500">IRN</p>
                    <p className="font-mono text-sm break-all">{creditNote.irn}</p>
                  </div>
                  {creditNote.irnGeneratedAt && (
                    <p className="text-sm text-gray-500">
                      Generated on {format(new Date(creditNote.irnGeneratedAt), 'dd MMM yyyy HH:mm')}
                    </p>
                  )}
                </div>
              ) : (
                <p className="text-sm text-amber-600">E-Invoice/IRN not yet generated</p>
              )}
            </div>
          )}

          {/* GSTR-1 Reporting */}
          {creditNote.reportedInGstr1 && (
            <div className="p-6 border-t bg-green-50">
              <div className="flex items-center gap-2">
                <CheckCircle className="h-5 w-5 text-green-500" />
                <div>
                  <p className="font-medium text-green-800">Reported in GSTR-1</p>
                  <p className="text-sm text-green-700">
                    Period: {creditNote.gstr1Period}
                    {creditNote.gstr1FilingDate && ` | Filed on ${format(new Date(creditNote.gstr1FilingDate), 'dd MMM yyyy')}`}
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Issue Confirmation Modal */}
      <Modal
        isOpen={showIssueModal}
        onClose={() => setShowIssueModal(false)}
        title="Issue Credit Note"
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-gray-700">
            Are you sure you want to issue credit note <strong>{creditNote.creditNoteNumber}</strong>?
            This action cannot be undone.
          </p>
          <div className="flex justify-end space-x-3">
            <button
              onClick={() => setShowIssueModal(false)}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              onClick={handleIssueConfirm}
              disabled={issueCreditNote.isPending}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
            >
              {issueCreditNote.isPending ? 'Issuing...' : 'Issue Credit Note'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Cancel Confirmation Modal */}
      <Modal
        isOpen={showCancelModal}
        onClose={() => setShowCancelModal(false)}
        title="Cancel Credit Note"
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-gray-700">
            Are you sure you want to cancel credit note <strong>{creditNote.creditNoteNumber}</strong>?
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Cancellation Reason *
            </label>
            <input
              type="text"
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
              placeholder="Enter reason for cancellation"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div className="flex justify-end space-x-3">
            <button
              onClick={() => setShowCancelModal(false)}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
            >
              Close
            </button>
            <button
              onClick={handleCancelConfirm}
              disabled={cancelCreditNote.isPending || !cancelReason}
              className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
            >
              {cancelCreditNote.isPending ? 'Cancelling...' : 'Cancel Credit Note'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

export default CreditNoteView

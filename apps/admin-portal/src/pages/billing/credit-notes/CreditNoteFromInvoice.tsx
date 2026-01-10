import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import { useState, useEffect, useMemo } from 'react'
import { useInvoice } from '@/hooks/api/useInvoices'
import { useCreateCreditNoteFromInvoice, useNextCreditNoteNumber, useCreditNotesByInvoice } from '@/features/credit-notes/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useParties } from '@/features/parties/hooks'
import { format } from 'date-fns'
import { formatINR } from '@/lib/currency'
import { ArrowLeft, AlertTriangle, CheckCircle2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Checkbox } from '@/components/ui/checkbox'
import type { CreditNoteReason, CreateCreditNoteFromInvoice as CreateCreditNoteFromInvoiceDto } from '@/services/api/types'

const CREDIT_NOTE_REASONS: { value: CreditNoteReason; label: string; description: string }[] = [
  { value: 'goods_returned', label: 'Goods Returned', description: 'Goods returned by recipient' },
  { value: 'post_sale_discount', label: 'Post-Sale Discount', description: 'Discount given after invoice' },
  { value: 'deficiency_in_services', label: 'Deficiency in Services', description: 'Services found deficient' },
  { value: 'excess_amount_charged', label: 'Excess Amount Charged', description: 'Excess taxable value charged' },
  { value: 'excess_tax_charged', label: 'Excess Tax Charged', description: 'Excess tax charged in original invoice' },
  { value: 'change_in_pos', label: 'Change in Place of Supply', description: 'Change in Place of Supply' },
  { value: 'export_refund', label: 'Export Refund', description: 'Refund on export goods' },
  { value: 'other', label: 'Other', description: 'Other reasons (specify in description)' },
]

const CreditNoteFromInvoice = () => {
  const { invoiceId } = useParams<{ invoiceId: string }>()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  const { data: invoice, isLoading: invoiceLoading, error: invoiceError } = useInvoice(invoiceId!)
  const { data: companies = [] } = useCompanies()
  const { data: parties = [] } = useParties({ companyId: invoice?.companyId })
  const { data: existingCreditNotes = [] } = useCreditNotesByInvoice(invoiceId!, !!invoice)
  const { data: nextNumber } = useNextCreditNoteNumber(invoice?.companyId || '', !!invoice?.companyId)
  const createCreditNote = useCreateCreditNoteFromInvoice()

  const [reason, setReason] = useState<CreditNoteReason>('goods_returned')
  const [reasonDescription, setReasonDescription] = useState('')
  const [isFullCreditNote, setIsFullCreditNote] = useState(true)
  const [selectedItems, setSelectedItems] = useState<{ [itemId: string]: { selected: boolean; quantity: number } }>({})
  const [errors, setErrors] = useState<Record<string, string>>({})

  const company = companies.find(c => c.id === invoice?.companyId)
  const party = parties.find(p => p.id === invoice?.partyId)

  // Calculate remaining creditable amount
  const totalCredited = useMemo(() => {
    return existingCreditNotes
      .filter(cn => cn.status !== 'cancelled')
      .reduce((sum, cn) => sum + cn.totalAmount, 0)
  }, [existingCreditNotes])

  const remainingAmount = (invoice?.totalAmount || 0) - totalCredited

  // Check if credit note can still be issued (time limit: 30th Nov of next FY)
  const timeBarredDate = useMemo(() => {
    if (!invoice?.invoiceDate) return null
    const invoiceDate = new Date(invoice.invoiceDate)
    const invoiceMonth = invoiceDate.getMonth() + 1
    const invoiceYear = invoiceDate.getFullYear()

    // Fiscal year in India: April to March
    const fiscalYearEnd = invoiceMonth >= 4 ? invoiceYear + 1 : invoiceYear
    return new Date(fiscalYearEnd + 1, 10, 30) // 30th November of next fiscal year
  }, [invoice?.invoiceDate])

  const isTimeBarred = timeBarredDate ? new Date() > timeBarredDate : false

  // Initialize selected items when invoice loads
  useEffect(() => {
    if (invoice?.items) {
      const initial: { [itemId: string]: { selected: boolean; quantity: number } } = {}
      invoice.items.forEach(item => {
        initial[item.id] = { selected: isFullCreditNote, quantity: item.quantity }
      })
      setSelectedItems(initial)
    }
  }, [invoice?.items, isFullCreditNote])

  // Calculate credit note amount based on selections
  const calculatedAmount = useMemo(() => {
    if (!invoice?.items) return 0
    if (isFullCreditNote) return remainingAmount

    let total = 0
    invoice.items.forEach(item => {
      const selection = selectedItems[item.id]
      if (selection?.selected) {
        const proportion = selection.quantity / item.quantity
        total += item.lineTotal * proportion
      }
    })
    return Math.min(total, remainingAmount)
  }, [invoice?.items, selectedItems, isFullCreditNote, remainingAmount])

  const handleItemToggle = (itemId: string, checked: boolean) => {
    setSelectedItems(prev => ({
      ...prev,
      [itemId]: { ...prev[itemId], selected: checked }
    }))
  }

  const handleQuantityChange = (itemId: string, quantity: number) => {
    const item = invoice?.items?.find(i => i.id === itemId)
    if (!item) return

    const validQuantity = Math.min(Math.max(0, quantity), item.quantity)
    setSelectedItems(prev => ({
      ...prev,
      [itemId]: { ...prev[itemId], quantity: validQuantity }
    }))
  }

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!reason) {
      newErrors.reason = 'Reason is required for GST compliance'
    }

    if (reason === 'other' && !reasonDescription.trim()) {
      newErrors.reasonDescription = 'Description is required when reason is "Other"'
    }

    if (!isFullCreditNote) {
      const hasSelection = Object.values(selectedItems).some(s => s.selected && s.quantity > 0)
      if (!hasSelection) {
        newErrors.items = 'Select at least one item for partial credit note'
      }
    }

    if (calculatedAmount <= 0) {
      newErrors.amount = 'Credit note amount must be greater than zero'
    }

    if (calculatedAmount > remainingAmount) {
      newErrors.amount = `Credit note amount cannot exceed remaining creditable amount (${formatINR(remainingAmount)})`
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    const dto: CreateCreditNoteFromInvoiceDto = {
      invoiceId: invoiceId!,
      reason,
      reasonDescription: reasonDescription || undefined,
      isFullCreditNote,
    }

    if (!isFullCreditNote) {
      dto.items = Object.entries(selectedItems)
        .filter(([_, s]) => s.selected && s.quantity > 0)
        .map(([itemId, s]) => ({
          originalItemId: itemId,
          quantity: s.quantity,
        }))
    }

    try {
      const creditNote = await createCreditNote.mutateAsync(dto)
      navigate(`/credit-notes/${creditNote.id}`)
    } catch (error) {
      console.error('Failed to create credit note:', error)
    }
  }

  if (invoiceLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-gray-500">Loading invoice...</div>
      </div>
    )
  }

  if (invoiceError || !invoice) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-red-500">Failed to load invoice</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b sticky top-0 z-10">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
                <ArrowLeft className="h-4 w-4 mr-2" />
                Back
              </Button>
              <div>
                <h1 className="text-xl font-semibold text-gray-900">Issue Credit Note</h1>
                <p className="text-sm text-gray-500">For Invoice {invoice.invoiceNumber}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Time-barred Warning */}
        {isTimeBarred && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-red-500 flex-shrink-0 mt-0.5" />
            <div>
              <p className="font-medium text-red-800">Time-barred Credit Note</p>
              <p className="text-sm text-red-700">
                Credit notes for this invoice should have been issued by {timeBarredDate ? format(timeBarredDate, 'dd MMM yyyy') : 'N/A'}.
                As per GST rules, credit notes must be issued before 30th November of the next financial year.
              </p>
            </div>
          </div>
        )}

        {/* Remaining Amount Info */}
        {totalCredited > 0 && (
          <div className="mb-6 bg-amber-50 border border-amber-200 rounded-lg p-4">
            <p className="text-sm text-amber-800">
              <strong>Existing Credit Notes:</strong> {formatINR(totalCredited)} already credited from this invoice.
              <br />
              <strong>Remaining Creditable Amount:</strong> {formatINR(remainingAmount)}
            </p>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Invoice Summary Card */}
          <div className="bg-white rounded-lg shadow-sm border p-6">
            <h2 className="text-lg font-medium text-gray-900 mb-4">Original Invoice Details</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div>
                <p className="text-xs text-gray-500">Invoice Number</p>
                <p className="font-medium">{invoice.invoiceNumber}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500">Invoice Date</p>
                <p className="font-medium">{invoice.invoiceDate ? format(new Date(invoice.invoiceDate), 'dd MMM yyyy') : '-'}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500">Customer</p>
                <p className="font-medium">{party?.name || 'Unknown'}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500">Invoice Amount</p>
                <p className="font-medium">{formatINR(invoice.totalAmount)}</p>
              </div>
            </div>
          </div>

          {/* Credit Note Details */}
          <div className="bg-white rounded-lg shadow-sm border p-6">
            <h2 className="text-lg font-medium text-gray-900 mb-4">Credit Note Details</h2>

            <div className="space-y-4">
              {/* Credit Note Number */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label>Credit Note Number</Label>
                  <Input value={nextNumber || 'Generating...'} disabled className="bg-gray-50" />
                  <p className="text-xs text-gray-500 mt-1">Auto-generated</p>
                </div>
                <div>
                  <Label>Credit Note Date</Label>
                  <Input type="date" value={format(new Date(), 'yyyy-MM-dd')} disabled className="bg-gray-50" />
                  <p className="text-xs text-gray-500 mt-1">Today's date</p>
                </div>
              </div>

              {/* Reason */}
              <div>
                <Label htmlFor="reason">Reason for Credit Note *</Label>
                <Select value={reason} onValueChange={(v) => setReason(v as CreditNoteReason)}>
                  <SelectTrigger id="reason" className={errors.reason ? 'border-red-500' : ''}>
                    <SelectValue placeholder="Select reason" />
                  </SelectTrigger>
                  <SelectContent>
                    {CREDIT_NOTE_REASONS.map(r => (
                      <SelectItem key={r.value} value={r.value}>
                        <span className="font-medium">{r.label}</span>
                        <span className="text-gray-500 ml-2 text-sm">- {r.description}</span>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.reason && <p className="text-red-500 text-sm mt-1">{errors.reason}</p>}
              </div>

              {/* Reason Description */}
              <div>
                <Label htmlFor="reasonDescription">
                  Reason Description {reason === 'other' && '*'}
                </Label>
                <Textarea
                  id="reasonDescription"
                  value={reasonDescription}
                  onChange={(e) => setReasonDescription(e.target.value)}
                  placeholder="Provide additional details about the reason..."
                  rows={3}
                  className={errors.reasonDescription ? 'border-red-500' : ''}
                />
                {errors.reasonDescription && <p className="text-red-500 text-sm mt-1">{errors.reasonDescription}</p>}
              </div>
            </div>
          </div>

          {/* Credit Note Type */}
          <div className="bg-white rounded-lg shadow-sm border p-6">
            <h2 className="text-lg font-medium text-gray-900 mb-4">Credit Note Type</h2>

            <div className="space-y-4">
              <div className="flex items-center gap-3">
                <Checkbox
                  id="fullCreditNote"
                  checked={isFullCreditNote}
                  onCheckedChange={(checked) => setIsFullCreditNote(!!checked)}
                />
                <Label htmlFor="fullCreditNote" className="cursor-pointer">
                  Full Credit Note (credit entire remaining invoice amount)
                </Label>
              </div>

              {!isFullCreditNote && invoice.items && invoice.items.length > 0 && (
                <div className="mt-4 border-t pt-4">
                  <p className="text-sm text-gray-600 mb-3">Select items to include in partial credit note:</p>
                  {errors.items && <p className="text-red-500 text-sm mb-2">{errors.items}</p>}

                  <div className="space-y-2">
                    {invoice.items.map(item => {
                      const selection = selectedItems[item.id]
                      return (
                        <div key={item.id} className="flex items-center gap-4 p-3 bg-gray-50 rounded-lg">
                          <Checkbox
                            checked={selection?.selected || false}
                            onCheckedChange={(checked) => handleItemToggle(item.id, !!checked)}
                          />
                          <div className="flex-1">
                            <p className="font-medium text-sm">{item.description}</p>
                            <p className="text-xs text-gray-500">
                              Unit Price: {formatINR(item.unitPrice)} | Original Qty: {item.quantity}
                            </p>
                          </div>
                          {selection?.selected && (
                            <div className="flex items-center gap-2">
                              <Label className="text-xs">Qty:</Label>
                              <Input
                                type="number"
                                min={0}
                                max={item.quantity}
                                value={selection.quantity}
                                onChange={(e) => handleQuantityChange(item.id, parseFloat(e.target.value) || 0)}
                                className="w-20 h-8 text-sm"
                              />
                            </div>
                          )}
                          <p className="text-sm font-medium w-24 text-right">
                            {formatINR(item.lineTotal)}
                          </p>
                        </div>
                      )
                    })}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Summary */}
          <div className="bg-white rounded-lg shadow-sm border p-6">
            <h2 className="text-lg font-medium text-gray-900 mb-4">Credit Note Summary</h2>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">Original Invoice Amount</span>
                <span>{formatINR(invoice.totalAmount)}</span>
              </div>
              {totalCredited > 0 && (
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Already Credited</span>
                  <span className="text-red-600">-{formatINR(totalCredited)}</span>
                </div>
              )}
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">Remaining Creditable</span>
                <span>{formatINR(remainingAmount)}</span>
              </div>
              <div className="border-t pt-2 mt-2">
                <div className="flex justify-between font-medium">
                  <span>This Credit Note Amount</span>
                  <span className="text-lg">{formatINR(calculatedAmount)}</span>
                </div>
              </div>
              {errors.amount && <p className="text-red-500 text-sm">{errors.amount}</p>}
            </div>
          </div>

          {/* GST Compliance Notice */}
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 flex items-start gap-3">
            <CheckCircle2 className="h-5 w-5 text-blue-500 flex-shrink-0 mt-0.5" />
            <div className="text-sm text-blue-800">
              <p className="font-medium">GST Compliance</p>
              <ul className="list-disc list-inside mt-1 space-y-1">
                <li>This credit note will be linked to the original invoice as per Section 34 of CGST Act</li>
                <li>GST tax components will be automatically calculated proportionally</li>
                <li>Remember to report this in your GSTR-1 return</li>
                {invoice.totalAmount > 500000 && (
                  <li className="text-amber-700">
                    <strong>Note:</strong> For tax exceeding ₹5L, recipient must provide CA/CMA certificate for ITC reversal
                  </li>
                )}
              </ul>
            </div>
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-3">
            <Button type="button" variant="outline" onClick={() => navigate(-1)}>
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={createCreditNote.isPending || isTimeBarred}
            >
              {createCreditNote.isPending ? 'Creating...' : 'Create Credit Note'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default CreditNoteFromInvoice

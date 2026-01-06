import { useState, useEffect } from 'react'
import { Drawer } from '@/components/ui/Drawer'
import { BankAccountSelect } from '@/components/ui/BankAccountSelect'
import { BankAccount } from '@/services/api/types'
import { formatCurrency } from '@/lib/currency'
import { Calendar, FileText, CreditCard, Banknote, AlertCircle, CheckCircle } from 'lucide-react'
import { cn } from '@/lib/utils'

// ==================== Types ====================

export type PaymentEntityType = 'invoice' | 'payroll' | 'contractor' | 'statutory'

export interface PaymentEntity {
  id: string
  companyId: string
  displayNumber: string  // Invoice number, payroll period, contractor name, etc.
  displayTitle: string   // Description of what's being paid
  totalAmount: number
  paidAmount?: number    // For partial payments (invoices)
  currency?: string
  entityType: PaymentEntityType
  // Additional fields for specific entity types
  metadata?: Record<string, any>
}

export interface MarkAsPaidResult {
  success: boolean
  journalEntryId?: string
  journalNumber?: string
  message?: string
  error?: string
}

interface MarkAsPaidDrawerProps {
  isOpen: boolean
  onClose: () => void
  entity: PaymentEntity | null
  onSuccess: (result: MarkAsPaidResult) => void
  // Custom submit handler for different entity types
  onSubmit: (data: MarkAsPaidFormData) => Promise<MarkAsPaidResult>
}

export interface MarkAsPaidFormData {
  entityId: string
  entityType: PaymentEntityType
  bankAccountId: string
  paymentDate: string
  paymentMethod: string
  referenceNumber?: string
  notes?: string
  amount: number
}

// ==================== Payment Methods ====================

const PAYMENT_METHODS_BY_ENTITY: Record<PaymentEntityType, { value: string; label: string }[]> = {
  invoice: [
    { value: 'bank_transfer', label: 'Bank Transfer' },
    { value: 'check', label: 'Check' },
    { value: 'cash', label: 'Cash' },
    { value: 'credit_card', label: 'Credit Card' },
    { value: 'paypal', label: 'PayPal' },
    { value: 'other', label: 'Other' },
  ],
  payroll: [
    { value: 'neft_batch', label: 'NEFT Batch' },
    { value: 'imps', label: 'IMPS' },
    { value: 'upi', label: 'UPI' },
    { value: 'manual', label: 'Manual' },
  ],
  contractor: [
    { value: 'neft', label: 'NEFT' },
    { value: 'imps', label: 'IMPS' },
    { value: 'upi', label: 'UPI' },
    { value: 'cheque', label: 'Cheque' },
    { value: 'cash', label: 'Cash' },
  ],
  statutory: [
    { value: 'neft', label: 'NEFT' },
    { value: 'rtgs', label: 'RTGS' },
    { value: 'online', label: 'Online' },
    { value: 'cheque', label: 'Cheque' },
    { value: 'upi', label: 'UPI' },
  ],
}

const DEFAULT_PAYMENT_METHOD: Record<PaymentEntityType, string> = {
  invoice: 'bank_transfer',
  payroll: 'neft_batch',
  contractor: 'neft',
  statutory: 'neft',
}

// ==================== Component ====================

export const MarkAsPaidDrawer = ({
  isOpen,
  onClose,
  entity,
  onSuccess,
  onSubmit,
}: MarkAsPaidDrawerProps) => {
  // Form state
  const [bankAccountId, setBankAccountId] = useState('')
  const [selectedAccount, setSelectedAccount] = useState<BankAccount | undefined>()
  const [paymentDate, setPaymentDate] = useState(new Date().toISOString().split('T')[0])
  const [paymentMethod, setPaymentMethod] = useState(DEFAULT_PAYMENT_METHOD.invoice)
  const [referenceNumber, setReferenceNumber] = useState('')
  const [notes, setNotes] = useState('')

  // UI state
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<MarkAsPaidResult | null>(null)

  // Reset form when entity changes or drawer opens
  useEffect(() => {
    if (isOpen && entity) {
      setPaymentDate(new Date().toISOString().split('T')[0])
      setPaymentMethod(DEFAULT_PAYMENT_METHOD[entity.entityType])
      setReferenceNumber('')
      setNotes('')
      setError(null)
      setResult(null)
      // Don't reset bank account - let it auto-select primary
    }
  }, [isOpen, entity?.id, entity?.entityType])

  const handleBankAccountChange = (value: string, account?: BankAccount) => {
    setBankAccountId(value)
    setSelectedAccount(account)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!entity) return

    // Validation
    if (!bankAccountId) {
      setError('Please select a bank account')
      return
    }

    setError(null)
    setIsSubmitting(true)

    try {
      const formData: MarkAsPaidFormData = {
        entityId: entity.id,
        entityType: entity.entityType,
        bankAccountId,
        paymentDate,
        paymentMethod,
        referenceNumber: referenceNumber || undefined,
        notes: notes || undefined,
        amount: getPayableAmount(),
      }

      const submitResult = await onSubmit(formData)

      if (submitResult.success) {
        setResult(submitResult)
        // Delay closing to show success state
        setTimeout(() => {
          onSuccess(submitResult)
          onClose()
        }, 1500)
      } else {
        setError(submitResult.error || 'Failed to record payment')
      }
    } catch (err: any) {
      setError(err.message || 'An error occurred while recording payment')
    } finally {
      setIsSubmitting(false)
    }
  }

  const getPayableAmount = () => {
    if (!entity) return 0
    if (entity.paidAmount) {
      return entity.totalAmount - entity.paidAmount
    }
    return entity.totalAmount
  }

  const getEntityTypeLabel = () => {
    switch (entity?.entityType) {
      case 'invoice': return 'Invoice'
      case 'payroll': return 'Payroll'
      case 'contractor': return 'Contractor Payment'
      case 'statutory': return 'Statutory Payment'
      default: return 'Payment'
    }
  }

  const getTitle = () => {
    return `Mark ${getEntityTypeLabel()} as Paid`
  }

  if (!entity) return null

  const paymentMethods = PAYMENT_METHODS_BY_ENTITY[entity.entityType]

  const payableAmount = getPayableAmount()

  return (
    <Drawer isOpen={isOpen} onClose={onClose} title={getTitle()} size="md">
      <form onSubmit={handleSubmit} className="flex flex-col h-full">
        {/* Success State */}
        {result?.success && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-4">
            <div className="flex items-center gap-2 text-green-700">
              <CheckCircle className="w-5 h-5" />
              <span className="font-medium">Payment Recorded Successfully</span>
            </div>
            {result.journalNumber && (
              <p className="text-sm text-green-600 mt-1">
                Journal Entry: {result.journalNumber}
              </p>
            )}
          </div>
        )}

        {/* Error State */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
            <div className="flex items-center gap-2 text-red-700">
              <AlertCircle className="w-5 h-5" />
              <span>{error}</span>
            </div>
          </div>
        )}

        {/* Entity Info */}
        <div className="bg-gray-50 rounded-lg p-4 mb-6">
          <div className="flex justify-between items-start mb-3">
            <div>
              <span className="text-xs text-gray-500 uppercase tracking-wide">
                {getEntityTypeLabel()}
              </span>
              <p className="font-semibold text-gray-900">{entity.displayNumber}</p>
            </div>
            <span className={cn(
              "px-2 py-1 text-xs font-medium rounded",
              entity.paidAmount && entity.paidAmount > 0
                ? "bg-yellow-100 text-yellow-800"
                : "bg-gray-100 text-gray-800"
            )}>
              {entity.paidAmount && entity.paidAmount > 0 ? 'Partial' : 'Unpaid'}
            </span>
          </div>

          <p className="text-sm text-gray-600 mb-3">{entity.displayTitle}</p>

          <div className="space-y-2 border-t border-gray-200 pt-3">
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">Total Amount:</span>
              <span className="font-medium">
                {formatCurrency(entity.totalAmount, entity.currency)}
              </span>
            </div>

            {entity.paidAmount !== undefined && entity.paidAmount > 0 && (
              <div className="flex justify-between text-sm">
                <span className="text-gray-500">Already Paid:</span>
                <span className="text-green-600 font-medium">
                  {formatCurrency(entity.paidAmount, entity.currency)}
                </span>
              </div>
            )}

            <div className="flex justify-between text-sm pt-2 border-t border-gray-200">
              <span className="text-gray-700 font-medium">Amount to Pay:</span>
              <span className="text-lg font-bold text-blue-600">
                {formatCurrency(payableAmount, entity.currency)}
              </span>
            </div>
          </div>
        </div>

        {/* Form Fields */}
        <div className="flex-1 space-y-4 overflow-y-auto">
          {/* Bank Account Selection - REQUIRED */}
          <BankAccountSelect
            companyId={entity.companyId}
            value={bankAccountId}
            onChange={handleBankAccountChange}
            label="Bank Account"
            required
            placeholder="Select bank account for payment..."
            error={!bankAccountId && error ? 'Bank account is required' : undefined}
          />

          {/* Payment Date */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Calendar className="w-4 h-4 inline mr-1" />
              Payment Date
              <span className="text-red-500 ml-1">*</span>
            </label>
            <input
              type="date"
              required
              value={paymentDate}
              onChange={(e) => setPaymentDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Payment Method */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <CreditCard className="w-4 h-4 inline mr-1" />
              Payment Method
              <span className="text-red-500 ml-1">*</span>
            </label>
            <select
              required
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              {paymentMethods.map((method) => (
                <option key={method.value} value={method.value}>
                  {method.label}
                </option>
              ))}
            </select>
          </div>

          {/* Reference Number */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Banknote className="w-4 h-4 inline mr-1" />
              Reference Number
            </label>
            <input
              type="text"
              value={referenceNumber}
              onChange={(e) => setReferenceNumber(e.target.value)}
              placeholder="UTR, Transaction ID, Cheque No, etc."
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Notes */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <FileText className="w-4 h-4 inline mr-1" />
              Notes
            </label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={3}
              placeholder="Additional payment details..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>

        {/* Selected Bank Info */}
        {selectedAccount && (
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mt-4">
            <p className="text-sm text-blue-800">
              <span className="font-medium">Payment from:</span>{' '}
              {selectedAccount.accountName} - {selectedAccount.bankName}
            </p>
            <p className="text-xs text-blue-600 mt-1">
              A/C: ****{selectedAccount.accountNumber.slice(-4)} |
              Balance: {formatCurrency(selectedAccount.currentBalance, selectedAccount.currency)}
            </p>
          </div>
        )}

        {/* Footer Actions */}
        <div className="flex justify-end gap-3 pt-4 mt-4 border-t border-gray-200">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting || result?.success}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting || !bankAccountId || result?.success}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 flex items-center gap-2"
          >
            {isSubmitting ? (
              <>
                <span className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent" />
                Processing...
              </>
            ) : result?.success ? (
              <>
                <CheckCircle className="w-4 h-4" />
                Done
              </>
            ) : (
              <>
                <Banknote className="w-4 h-4" />
                Mark as Paid
              </>
            )}
          </button>
        </div>
      </form>
    </Drawer>
  )
}

// ==================== Helper: Create Payment Entity ====================

// Helper functions to create PaymentEntity from different source types

export const createInvoicePaymentEntity = (invoice: {
  id: string
  companyId: string
  invoiceNumber: string
  customerName?: string
  totalAmount: number
  paidAmount?: number
  currency?: string
}): PaymentEntity => ({
  id: invoice.id,
  companyId: invoice.companyId,
  displayNumber: invoice.invoiceNumber,
  displayTitle: invoice.customerName ? `Payment from ${invoice.customerName}` : 'Invoice Payment',
  totalAmount: invoice.totalAmount,
  paidAmount: invoice.paidAmount,
  currency: invoice.currency || 'INR',
  entityType: 'invoice',
})

export const createPayrollPaymentEntity = (payroll: {
  id: string
  companyId: string
  payrollMonth: number
  payrollYear: number
  totalNetSalary: number
}): PaymentEntity => {
  const monthName = new Date(payroll.payrollYear, payroll.payrollMonth - 1).toLocaleString('default', { month: 'long' })
  return {
    id: payroll.id,
    companyId: payroll.companyId,
    displayNumber: `${monthName} ${payroll.payrollYear}`,
    displayTitle: `Salary disbursement for ${monthName} ${payroll.payrollYear}`,
    totalAmount: payroll.totalNetSalary,
    currency: 'INR',
    entityType: 'payroll',
  }
}

export const createContractorPaymentEntity = (payment: {
  id: string
  companyId: string
  contractorName: string
  invoiceNumber?: string
  totalAmount: number
}): PaymentEntity => ({
  id: payment.id,
  companyId: payment.companyId,
  displayNumber: payment.invoiceNumber || `Contractor: ${payment.contractorName}`,
  displayTitle: `Payment to ${payment.contractorName}`,
  totalAmount: payment.totalAmount,
  currency: 'INR',
  entityType: 'contractor',
})

export const createStatutoryPaymentEntity = (payment: {
  id: string
  companyId: string
  paymentType: string
  paymentTypeName: string
  periodMonth: number
  periodYear: number
  totalAmount: number
}): PaymentEntity => ({
  id: payment.id,
  companyId: payment.companyId,
  displayNumber: `${payment.paymentTypeName}`,
  displayTitle: `${payment.paymentTypeName} for ${payment.periodMonth}/${payment.periodYear}`,
  totalAmount: payment.totalAmount,
  currency: 'INR',
  entityType: 'statutory',
})

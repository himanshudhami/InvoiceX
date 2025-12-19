import { useState, useEffect } from 'react'
import { TdsReceivable, CreateTdsReceivableDto } from '@/services/api/types'
import { useCreateTdsReceivable, useUpdateTdsReceivable } from '@/hooks/api/useTdsReceivables'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useCustomers } from '@/hooks/api/useCustomers'
import { cn } from '@/lib/utils'

interface TdsReceivableFormProps {
  tdsReceivable?: TdsReceivable
  onSuccess: () => void
  onCancel: () => void
}

const TDS_SECTIONS = [
  { value: '194J', label: '194J - Professional/Technical Services' },
  { value: '194C', label: '194C - Contract Payments' },
  { value: '194H', label: '194H - Commission/Brokerage' },
  { value: '194O', label: '194O - E-commerce Transactions' },
  { value: '194A', label: '194A - Interest' },
  { value: '194I', label: '194I - Rent' },
  { value: '194Q', label: '194Q - Purchase of Goods' },
  { value: '195', label: '195 - Payment to Non-Residents' },
]

const QUARTERS = [
  { value: 'Q1', label: 'Q1 (Apr-Jun)' },
  { value: 'Q2', label: 'Q2 (Jul-Sep)' },
  { value: 'Q3', label: 'Q3 (Oct-Dec)' },
  { value: 'Q4', label: 'Q4 (Jan-Mar)' },
]

// Generate financial year options (current year and 4 previous years)
const generateFinancialYears = () => {
  const currentYear = new Date().getFullYear()
  const currentMonth = new Date().getMonth() + 1
  // If after March, current FY is currentYear-nextYear, else it's previousYear-currentYear
  const startYear = currentMonth > 3 ? currentYear : currentYear - 1

  const years = []
  for (let i = 0; i < 5; i++) {
    const year = startYear - i
    years.push({
      value: `${year}-${(year + 1).toString().slice(-2)}`,
      label: `FY ${year}-${(year + 1).toString().slice(-2)}`
    })
  }
  return years
}

export const TdsReceivableForm = ({ tdsReceivable, onSuccess, onCancel }: TdsReceivableFormProps) => {
  const financialYears = generateFinancialYears()

  const [formData, setFormData] = useState<CreateTdsReceivableDto>({
    companyId: '',
    financialYear: financialYears[0].value,
    quarter: 'Q1',
    customerId: '',
    deductorName: '',
    deductorTan: '',
    deductorPan: '',
    paymentDate: new Date().toISOString().split('T')[0],
    tdsSection: '194J',
    grossAmount: 0,
    tdsRate: 10,
    tdsAmount: 0,
    netReceived: 0,
    certificateNumber: '',
    certificateDate: '',
    certificateDownloaded: false,
    paymentId: '',
    invoiceId: '',
    notes: '',
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createTdsReceivable = useCreateTdsReceivable()
  const updateTdsReceivable = useUpdateTdsReceivable()
  const { data: companies = [] } = useCompanies()
  const { data: customers = [] } = useCustomers(formData.companyId || undefined)

  const isEditing = !!tdsReceivable
  const isLoading = createTdsReceivable.isPending || updateTdsReceivable.isPending

  // Populate form with existing data
  useEffect(() => {
    if (tdsReceivable) {
      setFormData({
        companyId: tdsReceivable.companyId,
        financialYear: tdsReceivable.financialYear,
        quarter: tdsReceivable.quarter,
        customerId: tdsReceivable.customerId || '',
        deductorName: tdsReceivable.deductorName,
        deductorTan: tdsReceivable.deductorTan || '',
        deductorPan: tdsReceivable.deductorPan || '',
        paymentDate: tdsReceivable.paymentDate.split('T')[0],
        tdsSection: tdsReceivable.tdsSection,
        grossAmount: tdsReceivable.grossAmount,
        tdsRate: tdsReceivable.tdsRate,
        tdsAmount: tdsReceivable.tdsAmount,
        netReceived: tdsReceivable.netReceived,
        certificateNumber: tdsReceivable.certificateNumber || '',
        certificateDate: tdsReceivable.certificateDate?.split('T')[0] || '',
        certificateDownloaded: tdsReceivable.certificateDownloaded,
        paymentId: tdsReceivable.paymentId || '',
        invoiceId: tdsReceivable.invoiceId || '',
        notes: tdsReceivable.notes || '',
      })
    }
  }, [tdsReceivable])

  // Auto-calculate TDS amount and net received when gross amount or rate changes
  useEffect(() => {
    if (formData.grossAmount && formData.tdsRate) {
      const tdsAmount = (formData.grossAmount * formData.tdsRate) / 100
      const netReceived = formData.grossAmount - tdsAmount
      setFormData(prev => ({
        ...prev,
        tdsAmount: Math.round(tdsAmount * 100) / 100,
        netReceived: Math.round(netReceived * 100) / 100,
      }))
    }
  }, [formData.grossAmount, formData.tdsRate])

  // Auto-fill deductor details when customer is selected
  useEffect(() => {
    if (formData.customerId) {
      const customer = customers.find(c => c.id === formData.customerId)
      if (customer) {
        setFormData(prev => ({
          ...prev,
          deductorName: customer.companyName || customer.name,
          deductorPan: customer.panNumber || prev.deductorPan,
        }))
      }
    }
  }, [formData.customerId, customers])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.companyId) {
      newErrors.companyId = 'Company is required'
    }

    if (!formData.deductorName?.trim()) {
      newErrors.deductorName = 'Deductor name is required'
    }

    if (!formData.paymentDate) {
      newErrors.paymentDate = 'Payment date is required'
    }

    if (!formData.grossAmount || formData.grossAmount <= 0) {
      newErrors.grossAmount = 'Gross amount must be greater than 0'
    }

    if (formData.tdsRate < 0 || formData.tdsRate > 100) {
      newErrors.tdsRate = 'TDS rate must be between 0 and 100'
    }

    // TAN validation (10 characters: 4 letters + 5 digits + 1 letter)
    if (formData.deductorTan && !/^[A-Z]{4}[0-9]{5}[A-Z]$/.test(formData.deductorTan)) {
      newErrors.deductorTan = 'Invalid TAN format (e.g., MUMA12345A)'
    }

    // PAN validation (10 characters: 5 letters + 4 digits + 1 letter)
    if (formData.deductorPan && !/^[A-Z]{5}[0-9]{4}[A-Z]$/.test(formData.deductorPan)) {
      newErrors.deductorPan = 'Invalid PAN format (e.g., AAACP1234C)'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && tdsReceivable) {
        await updateTdsReceivable.mutateAsync({ id: tdsReceivable.id, data: formData })
      } else {
        await createTdsReceivable.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const handleChange = (field: keyof CreateTdsReceivableDto, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Company Selection */}
      <div>
        <label htmlFor="companyId" className="block text-sm font-medium text-gray-700 mb-1">
          Company *
        </label>
        <select
          id="companyId"
          value={formData.companyId}
          onChange={(e) => handleChange('companyId', e.target.value)}
          className={cn(
            "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
            errors.companyId ? "border-red-500" : "border-gray-300"
          )}
        >
          <option value="">Select company</option>
          {companies.map((company) => (
            <option key={company.id} value={company.id}>
              {company.name}
            </option>
          ))}
        </select>
        {errors.companyId && <p className="text-red-500 text-sm mt-1">{errors.companyId}</p>}
      </div>

      {/* Financial Year and Quarter */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="financialYear" className="block text-sm font-medium text-gray-700 mb-1">
            Financial Year *
          </label>
          <select
            id="financialYear"
            value={formData.financialYear}
            onChange={(e) => handleChange('financialYear', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {financialYears.map((fy) => (
              <option key={fy.value} value={fy.value}>
                {fy.label}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="quarter" className="block text-sm font-medium text-gray-700 mb-1">
            Quarter *
          </label>
          <select
            id="quarter"
            value={formData.quarter}
            onChange={(e) => handleChange('quarter', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {QUARTERS.map((q) => (
              <option key={q.value} value={q.value}>
                {q.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Deductor Details Section */}
      <div className="border-t pt-4 mt-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Deductor Details</h3>

        <div className="mb-4">
          <label htmlFor="customerId" className="block text-sm font-medium text-gray-700 mb-1">
            Link to Customer (Optional)
          </label>
          <select
            id="customerId"
            value={formData.customerId}
            onChange={(e) => handleChange('customerId', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">Select customer (optional)</option>
            {customers.map((customer) => (
              <option key={customer.id} value={customer.id}>
                {customer.name} {customer.companyName ? `(${customer.companyName})` : ''}
              </option>
            ))}
          </select>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label htmlFor="deductorName" className="block text-sm font-medium text-gray-700 mb-1">
              Deductor Name *
            </label>
            <input
              id="deductorName"
              type="text"
              value={formData.deductorName}
              onChange={(e) => handleChange('deductorName', e.target.value)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.deductorName ? "border-red-500" : "border-gray-300"
              )}
              placeholder="Company/Person who deducted TDS"
            />
            {errors.deductorName && <p className="text-red-500 text-sm mt-1">{errors.deductorName}</p>}
          </div>
          <div>
            <label htmlFor="deductorTan" className="block text-sm font-medium text-gray-700 mb-1">
              Deductor TAN
            </label>
            <input
              id="deductorTan"
              type="text"
              value={formData.deductorTan}
              onChange={(e) => handleChange('deductorTan', e.target.value.toUpperCase())}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.deductorTan ? "border-red-500" : "border-gray-300"
              )}
              placeholder="MUMA12345A"
              maxLength={10}
            />
            {errors.deductorTan && <p className="text-red-500 text-sm mt-1">{errors.deductorTan}</p>}
          </div>
          <div>
            <label htmlFor="deductorPan" className="block text-sm font-medium text-gray-700 mb-1">
              Deductor PAN
            </label>
            <input
              id="deductorPan"
              type="text"
              value={formData.deductorPan}
              onChange={(e) => handleChange('deductorPan', e.target.value.toUpperCase())}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.deductorPan ? "border-red-500" : "border-gray-300"
              )}
              placeholder="AAACP1234C"
              maxLength={10}
            />
            {errors.deductorPan && <p className="text-red-500 text-sm mt-1">{errors.deductorPan}</p>}
          </div>
        </div>
      </div>

      {/* Transaction Details Section */}
      <div className="border-t pt-4 mt-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Transaction Details</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="paymentDate" className="block text-sm font-medium text-gray-700 mb-1">
              Payment Date *
            </label>
            <input
              id="paymentDate"
              type="date"
              value={formData.paymentDate}
              onChange={(e) => handleChange('paymentDate', e.target.value)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.paymentDate ? "border-red-500" : "border-gray-300"
              )}
            />
            {errors.paymentDate && <p className="text-red-500 text-sm mt-1">{errors.paymentDate}</p>}
          </div>
          <div>
            <label htmlFor="tdsSection" className="block text-sm font-medium text-gray-700 mb-1">
              TDS Section *
            </label>
            <select
              id="tdsSection"
              value={formData.tdsSection}
              onChange={(e) => handleChange('tdsSection', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {TDS_SECTIONS.map((section) => (
                <option key={section.value} value={section.value}>
                  {section.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mt-4">
          <div>
            <label htmlFor="grossAmount" className="block text-sm font-medium text-gray-700 mb-1">
              Gross Amount *
            </label>
            <input
              id="grossAmount"
              type="number"
              step="0.01"
              min="0"
              value={formData.grossAmount}
              onChange={(e) => handleChange('grossAmount', parseFloat(e.target.value) || 0)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.grossAmount ? "border-red-500" : "border-gray-300"
              )}
              placeholder="0.00"
            />
            {errors.grossAmount && <p className="text-red-500 text-sm mt-1">{errors.grossAmount}</p>}
          </div>
          <div>
            <label htmlFor="tdsRate" className="block text-sm font-medium text-gray-700 mb-1">
              TDS Rate (%) *
            </label>
            <input
              id="tdsRate"
              type="number"
              step="0.01"
              min="0"
              max="100"
              value={formData.tdsRate}
              onChange={(e) => handleChange('tdsRate', parseFloat(e.target.value) || 0)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.tdsRate ? "border-red-500" : "border-gray-300"
              )}
              placeholder="10"
            />
            {errors.tdsRate && <p className="text-red-500 text-sm mt-1">{errors.tdsRate}</p>}
          </div>
          <div>
            <label htmlFor="tdsAmount" className="block text-sm font-medium text-gray-700 mb-1">
              TDS Amount
            </label>
            <input
              id="tdsAmount"
              type="number"
              step="0.01"
              value={formData.tdsAmount}
              readOnly
              className="w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 focus:outline-none"
              placeholder="Auto-calculated"
            />
          </div>
          <div>
            <label htmlFor="netReceived" className="block text-sm font-medium text-gray-700 mb-1">
              Net Received
            </label>
            <input
              id="netReceived"
              type="number"
              step="0.01"
              value={formData.netReceived}
              readOnly
              className="w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 focus:outline-none"
              placeholder="Auto-calculated"
            />
          </div>
        </div>
      </div>

      {/* Certificate Details Section */}
      <div className="border-t pt-4 mt-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3">TDS Certificate Details</h3>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label htmlFor="certificateNumber" className="block text-sm font-medium text-gray-700 mb-1">
              Certificate Number
            </label>
            <input
              id="certificateNumber"
              type="text"
              value={formData.certificateNumber}
              onChange={(e) => handleChange('certificateNumber', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Form 16A number"
            />
          </div>
          <div>
            <label htmlFor="certificateDate" className="block text-sm font-medium text-gray-700 mb-1">
              Certificate Date
            </label>
            <input
              id="certificateDate"
              type="date"
              value={formData.certificateDate}
              onChange={(e) => handleChange('certificateDate', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div className="flex items-center pt-6">
            <input
              id="certificateDownloaded"
              type="checkbox"
              checked={formData.certificateDownloaded}
              onChange={(e) => handleChange('certificateDownloaded', e.target.checked)}
              className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
            />
            <label htmlFor="certificateDownloaded" className="ml-2 block text-sm text-gray-900">
              Certificate Downloaded
            </label>
          </div>
        </div>
      </div>

      {/* Notes */}
      <div>
        <label htmlFor="notes" className="block text-sm font-medium text-gray-700 mb-1">
          Notes
        </label>
        <textarea
          id="notes"
          rows={3}
          value={formData.notes}
          onChange={(e) => handleChange('notes', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Additional notes..."
        />
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-4">
        <button
          type="button"
          onClick={onCancel}
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary border border-transparent rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
        >
          {isLoading ? 'Saving...' : isEditing ? 'Update Entry' : 'Add TDS Entry'}
        </button>
      </div>
    </form>
  )
}

import { useState, useEffect } from 'react'
import {
  useCreateContractorPayment,
  useUpdateContractorPayment,
} from '@/features/payroll/hooks'
import type {
  ContractorPayment,
  CreateContractorPaymentDto,
  UpdateContractorPaymentDto,
} from '@/features/payroll/types/payroll'
import { useVendorsPaged } from '@/features/parties/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { formatINR } from '@/lib/currency'
import { CompanySelect } from '@/components/ui/CompanySelect'
import type { PartyListItem } from '@/services/api/types/party'

interface ContractorPaymentFormProps {
  payment?: ContractorPayment
  defaultCompanyId?: string
  onSuccess: () => void
  onCancel: () => void
}

export const ContractorPaymentForm = ({
  payment,
  defaultCompanyId,
  onSuccess,
  onCancel,
}: ContractorPaymentFormProps) => {
  const initialCompanyId = payment?.companyId || defaultCompanyId || ''

  const [formData, setFormData] = useState<CreateContractorPaymentDto>({
    partyId: '',
    companyId: initialCompanyId,
    paymentMonth: new Date().getMonth() + 1,
    paymentYear: new Date().getFullYear(),
    grossAmount: 0,
    tdsSection: '194J',
    tdsRate: 10.0,
    otherDeductions: 0,
    gstApplicable: false,
    gstRate: 18.0,
    invoiceNumber: '',
    contractReference: '',
    description: '',
    remarks: '',
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const [searchTerm, setSearchTerm] = useState('')

  // Fetch vendors (contractors are vendors with TDS applicable)
  const { data: vendorsData } = useVendorsPaged({
    companyId: formData.companyId || undefined,
    pageSize: 100,
    searchTerm: searchTerm || undefined,
  })
  const vendors = vendorsData?.items || []

  const { data: companies = [] } = useCompanies()
  const createPayment = useCreateContractorPayment()
  const updatePayment = useUpdateContractorPayment()

  const isEditing = !!payment
  const isLoading = createPayment.isPending || updatePayment.isPending

  // Calculate derived values
  const tdsAmount = Math.round((formData.grossAmount * (formData.tdsRate || 10)) / 100)
  const gstAmount = formData.gstApplicable
    ? Math.round((formData.grossAmount * (formData.gstRate || 18)) / 100)
    : 0
  const totalInvoiceAmount = formData.grossAmount + gstAmount
  const netPayable = formData.grossAmount - tdsAmount - (formData.otherDeductions || 0)

  useEffect(() => {
    if (payment) {
      setFormData({
        partyId: payment.partyId,
        companyId: payment.companyId,
        paymentMonth: payment.paymentMonth,
        paymentYear: payment.paymentYear,
        grossAmount: payment.grossAmount,
        tdsSection: payment.tdsSection || '194J',
        tdsRate: payment.tdsRate,
        otherDeductions: payment.otherDeductions,
        gstApplicable: payment.gstApplicable,
        gstRate: payment.gstRate,
        invoiceNumber: payment.invoiceNumber || '',
        contractReference: payment.contractReference || '',
        description: payment.description || '',
        remarks: payment.remarks || '',
      })
    }
  }, [payment])

  useEffect(() => {
    if (payment) return

    // Keep form in sync with the selected company from page filters
    setFormData((prev) => {
      if ((defaultCompanyId || '') === prev.companyId) return prev
      return {
        ...prev,
        companyId: defaultCompanyId || '',
        partyId: '',
      }
    })
  }, [defaultCompanyId, payment])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.partyId) newErrors.partyId = 'Contractor is required'
    if (!formData.companyId) newErrors.companyId = 'Company is required'
    if (!formData.paymentMonth || formData.paymentMonth < 1 || formData.paymentMonth > 12) {
      newErrors.paymentMonth = 'Valid month is required'
    }
    if (!formData.paymentYear || formData.paymentYear < 2000) {
      newErrors.paymentYear = 'Valid year is required'
    }
    if (!formData.grossAmount || formData.grossAmount <= 0) {
      newErrors.grossAmount = 'Gross amount must be greater than 0'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && payment) {
        await updatePayment.mutateAsync({
          id: payment.id,
          data: formData as UpdateContractorPaymentDto,
        })
      } else {
        await createPayment.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Failed to save contractor payment:', error)
    }
  }

  const handleInputChange = (
    field: keyof CreateContractorPaymentDto,
    value: string | number | boolean
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }))
    }
  }

  const handleCompanyChange = (value: string) => {
    setFormData((prev) => ({
      ...prev,
      companyId: value,
      partyId: '',
    }))
    if (errors.companyId) {
      setErrors((prev) => ({ ...prev, companyId: '' }))
    }
    if (errors.partyId) {
      setErrors((prev) => ({ ...prev, partyId: '' }))
    }
  }

  // Get selected vendor for display
  const selectedVendor = vendors.find((v) => v.id === formData.partyId)

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Company <span className="text-red-500">*</span>
          </label>
          <CompanySelect
            companies={companies}
            value={formData.companyId}
            onChange={handleCompanyChange}
            placeholder="Select company..."
            disabled={isEditing}
            error={errors.companyId}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Contractor (Vendor) <span className="text-red-500">*</span>
          </label>
          <select
            className={`w-full rounded-md border px-3 py-2 ${
              errors.partyId ? 'border-red-500' : 'border-gray-300'
            }`}
            value={formData.partyId}
            onChange={(e) => handleInputChange('partyId', e.target.value)}
            disabled={isEditing || !formData.companyId}
          >
            <option value="">Select contractor...</option>
            {vendors.map((vendor: PartyListItem) => (
              <option key={vendor.id} value={vendor.id}>
                {vendor.name} {vendor.panNumber ? `(${vendor.panNumber})` : ''}
              </option>
            ))}
          </select>
          {errors.partyId && (
            <p className="text-red-500 text-xs mt-1">{errors.partyId}</p>
          )}
          {selectedVendor && selectedVendor.tdsSection && (
            <p className="text-xs text-gray-500 mt-1">
              TDS: {selectedVendor.tdsSection} @ {selectedVendor.tdsRate}%
            </p>
          )}
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Month <span className="text-red-500">*</span>
          </label>
          <select
            className={`w-full rounded-md border px-3 py-2 ${
              errors.paymentMonth ? 'border-red-500' : 'border-gray-300'
            }`}
            value={formData.paymentMonth}
            onChange={(e) => handleInputChange('paymentMonth', parseInt(e.target.value))}
            disabled={isEditing}
          >
            {Array.from({ length: 12 }, (_, i) => i + 1).map((month) => (
              <option key={month} value={month}>
                {new Date(2000, month - 1, 1).toLocaleString('default', { month: 'long' })}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Year <span className="text-red-500">*</span>
          </label>
          <input
            type="number"
            className={`w-full rounded-md border px-3 py-2 ${
              errors.paymentYear ? 'border-red-500' : 'border-gray-300'
            }`}
            value={formData.paymentYear}
            onChange={(e) => handleInputChange('paymentYear', parseInt(e.target.value))}
            min={2000}
            max={2100}
            disabled={isEditing}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            TDS Section
          </label>
          <select
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.tdsSection}
            onChange={(e) => {
              const section = e.target.value
              handleInputChange('tdsSection', section)
              // Auto-set rate based on section
              if (section === '194J') handleInputChange('tdsRate', 10)
              else if (section === '194C') handleInputChange('tdsRate', 2)
            }}
          >
            <option value="194J">194J - Professional/Technical Fees (10%)</option>
            <option value="194C">194C - Contractors (2%)</option>
            <option value="194H">194H - Commission/Brokerage (5%)</option>
            <option value="194I(b)">194I(b) - Rent - Building (10%)</option>
          </select>
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Gross Amount <span className="text-red-500">*</span>
        </label>
        <input
          type="number"
          step="0.01"
          className={`w-full rounded-md border px-3 py-2 ${
            errors.grossAmount ? 'border-red-500' : 'border-gray-300'
          }`}
          value={formData.grossAmount}
          onChange={(e) => handleInputChange('grossAmount', parseFloat(e.target.value) || 0)}
        />
        {errors.grossAmount && (
          <p className="text-red-500 text-xs mt-1">{errors.grossAmount}</p>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            TDS Rate (%)
          </label>
          <input
            type="number"
            step="0.01"
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.tdsRate}
            onChange={(e) => handleInputChange('tdsRate', parseFloat(e.target.value) || 10)}
          />
          <p className="text-xs text-gray-500 mt-1">Section {formData.tdsSection}</p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Other Deductions
          </label>
          <input
            type="number"
            step="0.01"
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.otherDeductions}
            onChange={(e) => handleInputChange('otherDeductions', parseFloat(e.target.value) || 0)}
          />
        </div>
      </div>

      <div className="flex items-center gap-2">
        <input
          type="checkbox"
          id="gstApplicable"
          checked={formData.gstApplicable}
          onChange={(e) => handleInputChange('gstApplicable', e.target.checked)}
        />
        <label htmlFor="gstApplicable" className="text-sm font-medium">
          GST Applicable
        </label>
      </div>

      {formData.gstApplicable && (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            GST Rate (%)
          </label>
          <input
            type="number"
            step="0.01"
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.gstRate}
            onChange={(e) => handleInputChange('gstRate', parseFloat(e.target.value) || 18)}
          />
        </div>
      )}

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Invoice Number
        </label>
        <input
          type="text"
          className="w-full rounded-md border border-gray-300 px-3 py-2"
          value={formData.invoiceNumber}
          onChange={(e) => handleInputChange('invoiceNumber', e.target.value)}
          placeholder="Contractor invoice number"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Contract Reference
        </label>
        <input
          type="text"
          className="w-full rounded-md border border-gray-300 px-3 py-2"
          value={formData.contractReference}
          onChange={(e) => handleInputChange('contractReference', e.target.value)}
          placeholder="PO/Contract reference"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Description
        </label>
        <textarea
          className="w-full rounded-md border border-gray-300 px-3 py-2"
          value={formData.description}
          onChange={(e) => handleInputChange('description', e.target.value)}
          rows={3}
          placeholder="Description of work/services"
        />
      </div>

      {/* Calculation Summary */}
      <div className="bg-gray-50 p-4 rounded-lg space-y-2">
        <div className="flex justify-between">
          <span className="text-gray-600">Gross Amount:</span>
          <span className="font-medium">{formatINR(formData.grossAmount)}</span>
        </div>
        {formData.gstApplicable && (
          <div className="flex justify-between">
            <span className="text-gray-600">GST ({formData.gstRate}%):</span>
            <span className="font-medium">{formatINR(gstAmount)}</span>
          </div>
        )}
        {formData.gstApplicable && (
          <div className="flex justify-between">
            <span className="text-gray-600">Total Invoice Amount:</span>
            <span className="font-medium">{formatINR(totalInvoiceAmount)}</span>
          </div>
        )}
        <div className="flex justify-between">
          <span className="text-gray-600">TDS ({formData.tdsSection} @ {formData.tdsRate}%):</span>
          <span className="font-medium text-red-600">-{formatINR(tdsAmount)}</span>
        </div>
        {(formData.otherDeductions ?? 0) > 0 && (
          <div className="flex justify-between">
            <span className="text-gray-600">Other Deductions:</span>
            <span className="font-medium text-red-600">-{formatINR(formData.otherDeductions ?? 0)}</span>
          </div>
        )}
        <div className="flex justify-between border-t pt-2 mt-2">
          <span className="font-semibold">Net Payable:</span>
          <span className="font-bold text-lg text-green-600">{formatINR(netPayable)}</span>
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Remarks
        </label>
        <textarea
          className="w-full rounded-md border border-gray-300 px-3 py-2"
          value={formData.remarks}
          onChange={(e) => handleInputChange('remarks', e.target.value)}
          rows={2}
          placeholder="Additional notes..."
        />
      </div>

      <div className="flex justify-end gap-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          disabled={isLoading}
        >
          Cancel
        </button>
        <button
          type="submit"
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50"
          disabled={isLoading}
        >
          {isLoading ? 'Saving...' : isEditing ? 'Update' : 'Create'} Payment
        </button>
      </div>
    </form>
  )
}

import { useState, useEffect } from 'react'
import { BankAccount, CreateBankAccountDto } from '@/services/api/types'
import { useCreateBankAccount, useUpdateBankAccount } from '@/hooks/api/useBankAccounts'
import { useCompanies } from '@/hooks/api/useCompanies'
import { cn } from '@/lib/utils'

interface BankAccountFormProps {
  bankAccount?: BankAccount
  onSuccess: () => void
  onCancel: () => void
  defaultCompanyId?: string
}

const ACCOUNT_TYPES = [
  { value: 'current', label: 'Current Account' },
  { value: 'savings', label: 'Savings Account' },
  { value: 'cc', label: 'Cash Credit (CC)' },
  { value: 'od', label: 'Overdraft (OD)' },
  { value: 'foreign', label: 'Foreign Currency Account' },
]

const CURRENCIES = [
  { value: 'INR', label: 'INR - Indian Rupee' },
  { value: 'USD', label: 'USD - US Dollar' },
  { value: 'EUR', label: 'EUR - Euro' },
  { value: 'GBP', label: 'GBP - British Pound' },
  { value: 'AED', label: 'AED - UAE Dirham' },
  { value: 'SGD', label: 'SGD - Singapore Dollar' },
]

export const BankAccountForm = ({
  bankAccount,
  onSuccess,
  onCancel,
  defaultCompanyId,
}: BankAccountFormProps) => {
  const [formData, setFormData] = useState<CreateBankAccountDto>({
    companyId: defaultCompanyId || '',
    accountName: '',
    accountNumber: '',
    bankName: '',
    ifscCode: '',
    branchName: '',
    accountType: 'current',
    currency: 'INR',
    openingBalance: 0,
    currentBalance: 0,
    asOfDate: new Date().toISOString().split('T')[0],
    isPrimary: false,
    isActive: true,
    notes: '',
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createBankAccount = useCreateBankAccount()
  const updateBankAccount = useUpdateBankAccount()
  const { data: companies = [] } = useCompanies()

  const isEditing = !!bankAccount
  const isLoading = createBankAccount.isPending || updateBankAccount.isPending

  // Populate form with existing bank account data
  useEffect(() => {
    if (bankAccount) {
      setFormData({
        companyId: bankAccount.companyId || '',
        accountName: bankAccount.accountName || '',
        accountNumber: bankAccount.accountNumber || '',
        bankName: bankAccount.bankName || '',
        ifscCode: bankAccount.ifscCode || '',
        branchName: bankAccount.branchName || '',
        accountType: bankAccount.accountType || 'current',
        currency: bankAccount.currency || 'INR',
        openingBalance: bankAccount.openingBalance || 0,
        currentBalance: bankAccount.currentBalance || 0,
        asOfDate: bankAccount.asOfDate || new Date().toISOString().split('T')[0],
        isPrimary: bankAccount.isPrimary || false,
        isActive: bankAccount.isActive ?? true,
        notes: bankAccount.notes || '',
      })
    }
  }, [bankAccount])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.accountName?.trim()) {
      newErrors.accountName = 'Account name is required'
    }

    if (!formData.accountNumber?.trim()) {
      newErrors.accountNumber = 'Account number is required'
    }

    if (!formData.bankName?.trim()) {
      newErrors.bankName = 'Bank name is required'
    }

    if (formData.ifscCode && !/^[A-Z]{4}0[A-Z0-9]{6}$/.test(formData.ifscCode.toUpperCase())) {
      newErrors.ifscCode = 'Please enter a valid IFSC code (e.g., HDFC0001234)'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      const dataToSubmit = {
        ...formData,
        ifscCode: formData.ifscCode?.toUpperCase() || undefined,
      }

      if (isEditing && bankAccount) {
        await updateBankAccount.mutateAsync({ id: bankAccount.id, data: dataToSubmit })
      } else {
        await createBankAccount.mutateAsync(dataToSubmit)
      }
      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const handleChange = (field: keyof CreateBankAccountDto, value: string | number | boolean) => {
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
          Company
        </label>
        <select
          id="companyId"
          value={formData.companyId}
          onChange={(e) => handleChange('companyId', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
        >
          <option value="">Select a company (optional)</option>
          {companies.map((company) => (
            <option key={company.id} value={company.id}>
              {company.name}
            </option>
          ))}
        </select>
      </div>

      {/* Account Name */}
      <div>
        <label htmlFor="accountName" className="block text-sm font-medium text-gray-700 mb-1">
          Account Name *
        </label>
        <input
          id="accountName"
          type="text"
          value={formData.accountName}
          onChange={(e) => handleChange('accountName', e.target.value)}
          className={cn(
            "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
            errors.accountName ? "border-red-500" : "border-gray-300"
          )}
          placeholder="e.g., Main Business Account"
        />
        {errors.accountName && <p className="text-red-500 text-sm mt-1">{errors.accountName}</p>}
      </div>

      {/* Bank Name */}
      <div>
        <label htmlFor="bankName" className="block text-sm font-medium text-gray-700 mb-1">
          Bank Name *
        </label>
        <input
          id="bankName"
          type="text"
          value={formData.bankName}
          onChange={(e) => handleChange('bankName', e.target.value)}
          className={cn(
            "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
            errors.bankName ? "border-red-500" : "border-gray-300"
          )}
          placeholder="e.g., HDFC Bank"
        />
        {errors.bankName && <p className="text-red-500 text-sm mt-1">{errors.bankName}</p>}
      </div>

      {/* Account Number and IFSC */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="accountNumber" className="block text-sm font-medium text-gray-700 mb-1">
            Account Number *
          </label>
          <input
            id="accountNumber"
            type="text"
            value={formData.accountNumber}
            onChange={(e) => handleChange('accountNumber', e.target.value)}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
              errors.accountNumber ? "border-red-500" : "border-gray-300"
            )}
            placeholder="e.g., 12340000123456"
          />
          {errors.accountNumber && <p className="text-red-500 text-sm mt-1">{errors.accountNumber}</p>}
        </div>
        <div>
          <label htmlFor="ifscCode" className="block text-sm font-medium text-gray-700 mb-1">
            IFSC Code
          </label>
          <input
            id="ifscCode"
            type="text"
            value={formData.ifscCode}
            onChange={(e) => handleChange('ifscCode', e.target.value.toUpperCase())}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring uppercase",
              errors.ifscCode ? "border-red-500" : "border-gray-300"
            )}
            placeholder="e.g., HDFC0001234"
            maxLength={11}
          />
          {errors.ifscCode && <p className="text-red-500 text-sm mt-1">{errors.ifscCode}</p>}
        </div>
      </div>

      {/* Branch Name */}
      <div>
        <label htmlFor="branchName" className="block text-sm font-medium text-gray-700 mb-1">
          Branch Name
        </label>
        <input
          id="branchName"
          type="text"
          value={formData.branchName}
          onChange={(e) => handleChange('branchName', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="e.g., MG Road Branch"
        />
      </div>

      {/* Account Type and Currency */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="accountType" className="block text-sm font-medium text-gray-700 mb-1">
            Account Type
          </label>
          <select
            id="accountType"
            value={formData.accountType}
            onChange={(e) => handleChange('accountType', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {ACCOUNT_TYPES.map((type) => (
              <option key={type.value} value={type.value}>
                {type.label}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="currency" className="block text-sm font-medium text-gray-700 mb-1">
            Currency
          </label>
          <select
            id="currency"
            value={formData.currency}
            onChange={(e) => handleChange('currency', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {CURRENCIES.map((currency) => (
              <option key={currency.value} value={currency.value}>
                {currency.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Opening and Current Balance */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label htmlFor="openingBalance" className="block text-sm font-medium text-gray-700 mb-1">
            Opening Balance
          </label>
          <input
            id="openingBalance"
            type="number"
            step="0.01"
            value={formData.openingBalance}
            onChange={(e) => handleChange('openingBalance', parseFloat(e.target.value) || 0)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="0.00"
          />
        </div>
        <div>
          <label htmlFor="currentBalance" className="block text-sm font-medium text-gray-700 mb-1">
            Current Balance
          </label>
          <input
            id="currentBalance"
            type="number"
            step="0.01"
            value={formData.currentBalance}
            onChange={(e) => handleChange('currentBalance', parseFloat(e.target.value) || 0)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="0.00"
          />
        </div>
      </div>

      {/* Balance As Of Date */}
      <div>
        <label htmlFor="asOfDate" className="block text-sm font-medium text-gray-700 mb-1">
          Balance As Of Date
        </label>
        <input
          id="asOfDate"
          type="date"
          value={formData.asOfDate}
          onChange={(e) => handleChange('asOfDate', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
        />
      </div>

      {/* Status Checkboxes */}
      <div className="flex space-x-6">
        <label className="flex items-center">
          <input
            type="checkbox"
            checked={formData.isPrimary}
            onChange={(e) => handleChange('isPrimary', e.target.checked)}
            className="rounded border-gray-300 text-primary focus:ring-ring"
          />
          <span className="ml-2 text-sm text-gray-700">Primary Account</span>
        </label>
        <label className="flex items-center">
          <input
            type="checkbox"
            checked={formData.isActive}
            onChange={(e) => handleChange('isActive', e.target.checked)}
            className="rounded border-gray-300 text-primary focus:ring-ring"
          />
          <span className="ml-2 text-sm text-gray-700">Active</span>
        </label>
      </div>

      {/* Notes */}
      <div>
        <label htmlFor="notes" className="block text-sm font-medium text-gray-700 mb-1">
          Notes
        </label>
        <textarea
          id="notes"
          value={formData.notes}
          onChange={(e) => handleChange('notes', e.target.value)}
          rows={3}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Additional notes about this account..."
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
          {isLoading ? 'Saving...' : isEditing ? 'Update Account' : 'Create Account'}
        </button>
      </div>
    </form>
  )
}

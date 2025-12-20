import { useState, useEffect, useMemo } from 'react'
import { ChartOfAccount, CreateChartOfAccountDto, UpdateChartOfAccountDto, AccountType, NormalBalance } from '@/services/api/types'
import { useCreateAccount, useUpdateAccount } from '@/features/ledger/hooks'
import toast from 'react-hot-toast'

interface ChartOfAccountFormProps {
  companyId: string
  account?: ChartOfAccount
  accounts: ChartOfAccount[]
  onSuccess: () => void
  onCancel: () => void
}

const accountTypes: { value: AccountType; label: string; normalBalance: NormalBalance }[] = [
  { value: 'asset', label: 'Asset', normalBalance: 'debit' },
  { value: 'liability', label: 'Liability', normalBalance: 'credit' },
  { value: 'equity', label: 'Equity', normalBalance: 'credit' },
  { value: 'income', label: 'Income', normalBalance: 'credit' },
  { value: 'expense', label: 'Expense', normalBalance: 'debit' },
]

const accountSubtypes: Record<AccountType, string[]> = {
  asset: ['Current Assets', 'Fixed Assets', 'Investments', 'Other Assets', 'Bank Accounts', 'Cash', 'Receivables'],
  liability: ['Current Liabilities', 'Long-term Liabilities', 'Provisions', 'Payables', 'Statutory Dues'],
  equity: ['Share Capital', 'Reserves', 'Retained Earnings', 'Other Equity'],
  income: ['Operating Revenue', 'Other Income', 'Interest Income', 'Sales'],
  expense: ['Operating Expenses', 'Administrative Expenses', 'Finance Costs', 'Depreciation', 'Salaries'],
}

const gstTreatments = [
  { value: '', label: 'None' },
  { value: 'input', label: 'GST Input Credit' },
  { value: 'output', label: 'GST Output Tax' },
  { value: 'exempt', label: 'Exempt from GST' },
  { value: 'tds_payable', label: 'TDS Payable' },
  { value: 'tds_receivable', label: 'TDS Receivable' },
]

export const ChartOfAccountForm = ({ companyId, account, accounts, onSuccess, onCancel }: ChartOfAccountFormProps) => {
  const [formData, setFormData] = useState<CreateChartOfAccountDto>({
    companyId,
    accountCode: '',
    accountName: '',
    accountType: 'asset',
    accountSubtype: '',
    parentAccountId: undefined,
    scheduleReference: '',
    gstTreatment: '',
    isControlAccount: false,
    normalBalance: 'debit',
    openingBalance: 0,
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createAccount = useCreateAccount()
  const updateAccount = useUpdateAccount()

  const isEditing = !!account
  const isLoading = createAccount.isPending || updateAccount.isPending

  // Filter parent accounts (only accounts of same type and not the current account)
  const parentAccountOptions = useMemo(() => {
    return accounts.filter(a =>
      a.accountType === formData.accountType &&
      a.id !== account?.id &&
      a.depthLevel < 3 // Limit nesting depth
    )
  }, [accounts, formData.accountType, account?.id])

  useEffect(() => {
    if (account) {
      setFormData({
        companyId,
        accountCode: account.accountCode,
        accountName: account.accountName,
        accountType: account.accountType,
        accountSubtype: account.accountSubtype || '',
        parentAccountId: account.parentAccountId,
        scheduleReference: account.scheduleReference || '',
        gstTreatment: account.gstTreatment || '',
        isControlAccount: account.isControlAccount,
        normalBalance: account.normalBalance,
        openingBalance: account.openingBalance,
      })
    }
  }, [account, companyId])

  const handleChange = (field: keyof CreateChartOfAccountDto, value: unknown) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  const handleAccountTypeChange = (type: AccountType) => {
    const typeConfig = accountTypes.find(t => t.value === type)
    setFormData(prev => ({
      ...prev,
      accountType: type,
      normalBalance: typeConfig?.normalBalance || 'debit',
      accountSubtype: '',
      parentAccountId: undefined,
    }))
  }

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.accountCode.trim()) {
      newErrors.accountCode = 'Account code is required'
    } else if (!/^[0-9]{4,6}$/.test(formData.accountCode)) {
      newErrors.accountCode = 'Account code must be 4-6 digits'
    } else {
      // Check for duplicate account codes
      const duplicate = accounts.find(a =>
        a.accountCode === formData.accountCode && a.id !== account?.id
      )
      if (duplicate) {
        newErrors.accountCode = 'Account code already exists'
      }
    }

    if (!formData.accountName.trim()) {
      newErrors.accountName = 'Account name is required'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) return

    try {
      if (isEditing && account) {
        const updateData: UpdateChartOfAccountDto = {
          accountName: formData.accountName,
          accountSubtype: formData.accountSubtype || undefined,
          scheduleReference: formData.scheduleReference || undefined,
          gstTreatment: formData.gstTreatment || undefined,
          isActive: true,
        }
        await updateAccount.mutateAsync({ id: account.id, data: updateData })
        toast.success('Account updated successfully')
      } else {
        await createAccount.mutateAsync(formData)
        toast.success('Account created successfully')
      }
      onSuccess()
    } catch (error) {
      console.error('Failed to save account:', error)
      toast.error(isEditing ? 'Failed to update account' : 'Failed to create account')
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Account Code */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Account Code <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          value={formData.accountCode}
          onChange={(e) => handleChange('accountCode', e.target.value)}
          disabled={isEditing}
          placeholder="e.g., 1110, 2200, 4100"
          className={`w-full px-3 py-2 border rounded-md shadow-sm focus:ring-primary focus:border-primary ${
            errors.accountCode ? 'border-red-500' : 'border-gray-300'
          } ${isEditing ? 'bg-gray-100' : ''}`}
        />
        {errors.accountCode && (
          <p className="mt-1 text-sm text-red-500">{errors.accountCode}</p>
        )}
        <p className="mt-1 text-xs text-gray-500">
          4-6 digits. 1xxx=Assets, 2xxx=Liabilities, 3xxx=Equity, 4xxx=Income, 5xxx=Expenses
        </p>
      </div>

      {/* Account Name */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Account Name <span className="text-red-500">*</span>
        </label>
        <input
          type="text"
          value={formData.accountName}
          onChange={(e) => handleChange('accountName', e.target.value)}
          placeholder="e.g., Cash in Hand, Accounts Receivable"
          className={`w-full px-3 py-2 border rounded-md shadow-sm focus:ring-primary focus:border-primary ${
            errors.accountName ? 'border-red-500' : 'border-gray-300'
          }`}
        />
        {errors.accountName && (
          <p className="mt-1 text-sm text-red-500">{errors.accountName}</p>
        )}
      </div>

      {/* Account Type */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Account Type <span className="text-red-500">*</span>
        </label>
        <select
          value={formData.accountType}
          onChange={(e) => handleAccountTypeChange(e.target.value as AccountType)}
          disabled={isEditing}
          className={`w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-primary focus:border-primary ${
            isEditing ? 'bg-gray-100' : ''
          }`}
        >
          {accountTypes.map(type => (
            <option key={type.value} value={type.value}>
              {type.label} (Normal: {type.normalBalance === 'debit' ? 'Debit' : 'Credit'})
            </option>
          ))}
        </select>
      </div>

      {/* Account Subtype */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Account Subtype
        </label>
        <select
          value={formData.accountSubtype}
          onChange={(e) => handleChange('accountSubtype', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-primary focus:border-primary"
        >
          <option value="">Select subtype...</option>
          {accountSubtypes[formData.accountType].map(subtype => (
            <option key={subtype} value={subtype}>{subtype}</option>
          ))}
        </select>
      </div>

      {/* Parent Account */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Parent Account
        </label>
        <select
          value={formData.parentAccountId || ''}
          onChange={(e) => handleChange('parentAccountId', e.target.value || undefined)}
          disabled={isEditing}
          className={`w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-primary focus:border-primary ${
            isEditing ? 'bg-gray-100' : ''
          }`}
        >
          <option value="">No parent (top-level)</option>
          {parentAccountOptions.map(acc => (
            <option key={acc.id} value={acc.id}>
              {acc.accountCode} - {acc.accountName}
            </option>
          ))}
        </select>
        <p className="mt-1 text-xs text-gray-500">
          Optional. Use for sub-accounts under a parent account.
        </p>
      </div>

      {/* GST Treatment */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          GST Treatment
        </label>
        <select
          value={formData.gstTreatment}
          onChange={(e) => handleChange('gstTreatment', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-primary focus:border-primary"
        >
          {gstTreatments.map(treatment => (
            <option key={treatment.value} value={treatment.value}>{treatment.label}</option>
          ))}
        </select>
      </div>

      {/* Schedule Reference */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Schedule Reference (Schedule III)
        </label>
        <input
          type="text"
          value={formData.scheduleReference}
          onChange={(e) => handleChange('scheduleReference', e.target.value)}
          placeholder="e.g., Schedule III Part I - Assets"
          className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-primary focus:border-primary"
        />
      </div>

      {/* Opening Balance (only for new accounts) */}
      {!isEditing && (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Opening Balance
          </label>
          <div className="flex items-center gap-2">
            <input
              type="number"
              step="0.01"
              value={formData.openingBalance}
              onChange={(e) => handleChange('openingBalance', parseFloat(e.target.value) || 0)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-primary focus:border-primary"
            />
            <span className="text-sm text-gray-500 whitespace-nowrap">
              {formData.normalBalance === 'debit' ? 'Dr' : 'Cr'}
            </span>
          </div>
        </div>
      )}

      {/* Control Account */}
      {!isEditing && (
        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="isControlAccount"
            checked={formData.isControlAccount}
            onChange={(e) => handleChange('isControlAccount', e.target.checked)}
            className="rounded border-gray-300 text-primary focus:ring-primary"
          />
          <label htmlFor="isControlAccount" className="text-sm text-gray-700">
            Control Account (subsidiary ledger required)
          </label>
        </div>
      )}

      {/* Form Actions */}
      <div className="flex justify-end gap-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 disabled:opacity-50"
        >
          {isLoading ? 'Saving...' : (isEditing ? 'Update Account' : 'Create Account')}
        </button>
      </div>
    </form>
  )
}

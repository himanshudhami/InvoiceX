import { useState, useMemo } from 'react'
import { CreateJournalEntryDto, CreateJournalEntryLineDto, ChartOfAccount } from '@/services/api/types'
import { useAccounts, useCreateJournalEntry } from '@/features/ledger/hooks'
import toast from 'react-hot-toast'
import { Plus, Trash2, AlertCircle } from 'lucide-react'
import { format } from 'date-fns'

interface JournalEntryFormProps {
  companyId: string
  onSuccess: () => void
  onCancel: () => void
}

interface LineItem {
  id: string
  accountId: string
  debitAmount: number
  creditAmount: number
  description: string
}

export const JournalEntryForm = ({ companyId, onSuccess, onCancel }: JournalEntryFormProps) => {
  const [journalDate, setJournalDate] = useState(format(new Date(), 'yyyy-MM-dd'))
  const [description, setDescription] = useState('')
  const [lines, setLines] = useState<LineItem[]>([
    { id: '1', accountId: '', debitAmount: 0, creditAmount: 0, description: '' },
    { id: '2', accountId: '', debitAmount: 0, creditAmount: 0, description: '' },
  ])
  const [errors, setErrors] = useState<Record<string, string>>({})

  const { data: accounts = [] } = useAccounts(companyId)
  const createEntry = useCreateJournalEntry()

  const isLoading = createEntry.isPending

  // Sort accounts by code for dropdown
  const sortedAccounts = useMemo(() => {
    return [...accounts]
      .filter(a => a.isActive)
      .sort((a, b) => a.accountCode.localeCompare(b.accountCode))
  }, [accounts])

  // Calculate totals
  const totalDebit = lines.reduce((sum, line) => sum + (line.debitAmount || 0), 0)
  const totalCredit = lines.reduce((sum, line) => sum + (line.creditAmount || 0), 0)
  const isBalanced = Math.abs(totalDebit - totalCredit) < 0.01
  const difference = Math.abs(totalDebit - totalCredit)

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(amount)
  }

  const addLine = () => {
    const newId = (parseInt(lines[lines.length - 1]?.id || '0') + 1).toString()
    setLines([...lines, { id: newId, accountId: '', debitAmount: 0, creditAmount: 0, description: '' }])
  }

  const removeLine = (id: string) => {
    if (lines.length <= 2) {
      toast.error('Minimum 2 lines required')
      return
    }
    setLines(lines.filter(line => line.id !== id))
  }

  const updateLine = (id: string, field: keyof LineItem, value: unknown) => {
    setLines(lines.map(line => {
      if (line.id !== id) return line

      // If entering debit, clear credit and vice versa
      if (field === 'debitAmount' && (value as number) > 0) {
        return { ...line, [field]: value, creditAmount: 0 }
      }
      if (field === 'creditAmount' && (value as number) > 0) {
        return { ...line, [field]: value, debitAmount: 0 }
      }

      return { ...line, [field]: value }
    }))
  }

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!journalDate) {
      newErrors.journalDate = 'Date is required'
    }

    if (!description.trim()) {
      newErrors.description = 'Description is required'
    }

    // Validate lines
    const validLines = lines.filter(line =>
      line.accountId && (line.debitAmount > 0 || line.creditAmount > 0)
    )

    if (validLines.length < 2) {
      newErrors.lines = 'At least 2 valid lines are required'
    }

    if (!isBalanced) {
      newErrors.balance = `Entry is not balanced. Difference: ${formatCurrency(difference)}`
    }

    // Check for duplicate accounts
    const accountIds = validLines.map(l => l.accountId)
    const hasDuplicates = accountIds.length !== new Set(accountIds).size
    if (hasDuplicates) {
      newErrors.duplicates = 'Same account used multiple times. Consider combining lines.'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) return

    try {
      const validLines: CreateJournalEntryLineDto[] = lines
        .filter(line => line.accountId && (line.debitAmount > 0 || line.creditAmount > 0))
        .map(line => ({
          accountId: line.accountId,
          debitAmount: line.debitAmount || 0,
          creditAmount: line.creditAmount || 0,
          description: line.description || undefined,
        }))

      const data: CreateJournalEntryDto = {
        companyId,
        journalDate,
        description,
        lines: validLines,
      }

      await createEntry.mutateAsync(data)
      toast.success('Journal entry created successfully')
      onSuccess()
    } catch (error) {
      console.error('Failed to create journal entry:', error)
      toast.error('Failed to create journal entry')
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Header Info */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Journal Date <span className="text-red-500">*</span>
          </label>
          <input
            type="date"
            value={journalDate}
            onChange={(e) => setJournalDate(e.target.value)}
            className={`w-full px-3 py-2 border rounded-md shadow-sm focus:ring-primary focus:border-primary ${
              errors.journalDate ? 'border-red-500' : 'border-gray-300'
            }`}
          />
          {errors.journalDate && (
            <p className="mt-1 text-sm text-red-500">{errors.journalDate}</p>
          )}
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Description <span className="text-red-500">*</span>
        </label>
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={2}
          placeholder="Enter journal entry description..."
          className={`w-full px-3 py-2 border rounded-md shadow-sm focus:ring-primary focus:border-primary ${
            errors.description ? 'border-red-500' : 'border-gray-300'
          }`}
        />
        {errors.description && (
          <p className="mt-1 text-sm text-red-500">{errors.description}</p>
        )}
      </div>

      {/* Journal Lines */}
      <div>
        <div className="flex justify-between items-center mb-2">
          <label className="block text-sm font-medium text-gray-700">
            Journal Lines <span className="text-red-500">*</span>
          </label>
          <button
            type="button"
            onClick={addLine}
            className="inline-flex items-center gap-1 px-2 py-1 text-sm text-primary hover:bg-primary/10 rounded"
          >
            <Plus size={14} />
            Add Line
          </button>
        </div>

        <div className="border rounded-md overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Account</th>
                <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase w-32">Debit</th>
                <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase w-32">Credit</th>
                <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Narration</th>
                <th className="px-3 py-2 w-10"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {lines.map((line, index) => (
                <tr key={line.id} className="hover:bg-gray-50">
                  <td className="px-3 py-2">
                    <select
                      value={line.accountId}
                      onChange={(e) => updateLine(line.id, 'accountId', e.target.value)}
                      className="w-full text-sm border-gray-300 rounded focus:ring-primary focus:border-primary"
                    >
                      <option value="">Select account...</option>
                      {sortedAccounts.map(acc => (
                        <option key={acc.id} value={acc.id}>
                          {acc.accountCode} - {acc.accountName}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="px-3 py-2">
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={line.debitAmount || ''}
                      onChange={(e) => updateLine(line.id, 'debitAmount', parseFloat(e.target.value) || 0)}
                      placeholder="0.00"
                      className="w-full text-right text-sm border-gray-300 rounded focus:ring-primary focus:border-primary"
                    />
                  </td>
                  <td className="px-3 py-2">
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={line.creditAmount || ''}
                      onChange={(e) => updateLine(line.id, 'creditAmount', parseFloat(e.target.value) || 0)}
                      placeholder="0.00"
                      className="w-full text-right text-sm border-gray-300 rounded focus:ring-primary focus:border-primary"
                    />
                  </td>
                  <td className="px-3 py-2">
                    <input
                      type="text"
                      value={line.description}
                      onChange={(e) => updateLine(line.id, 'description', e.target.value)}
                      placeholder="Optional narration"
                      className="w-full text-sm border-gray-300 rounded focus:ring-primary focus:border-primary"
                    />
                  </td>
                  <td className="px-3 py-2">
                    <button
                      type="button"
                      onClick={() => removeLine(line.id)}
                      className="text-red-500 hover:text-red-700 p-1"
                      disabled={lines.length <= 2}
                    >
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot className="bg-gray-50 font-medium">
              <tr>
                <td className="px-3 py-2 text-right">Totals:</td>
                <td className="px-3 py-2 text-right text-blue-600">{formatCurrency(totalDebit)}</td>
                <td className="px-3 py-2 text-right text-green-600">{formatCurrency(totalCredit)}</td>
                <td colSpan={2} className="px-3 py-2">
                  {isBalanced ? (
                    <span className="text-green-600 text-sm">Balanced</span>
                  ) : (
                    <span className="text-red-600 text-sm">
                      Difference: {formatCurrency(difference)}
                    </span>
                  )}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>

        {(errors.lines || errors.balance || errors.duplicates) && (
          <div className="mt-2 space-y-1">
            {errors.lines && (
              <p className="text-sm text-red-500 flex items-center gap-1">
                <AlertCircle size={14} /> {errors.lines}
              </p>
            )}
            {errors.balance && (
              <p className="text-sm text-red-500 flex items-center gap-1">
                <AlertCircle size={14} /> {errors.balance}
              </p>
            )}
            {errors.duplicates && (
              <p className="text-sm text-yellow-600 flex items-center gap-1">
                <AlertCircle size={14} /> {errors.duplicates}
              </p>
            )}
          </div>
        )}
      </div>

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
          disabled={isLoading || !isBalanced}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 disabled:opacity-50"
        >
          {isLoading ? 'Creating...' : 'Create Entry'}
        </button>
      </div>
    </form>
  )
}

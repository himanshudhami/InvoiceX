import { useState, useEffect } from 'react'
import {
  useCreateProfessionalTaxSlab,
  useUpdateProfessionalTaxSlab,
} from '@/features/payroll/hooks'
import type {
  ProfessionalTaxSlab,
  CreateProfessionalTaxSlabDto,
  UpdateProfessionalTaxSlabDto,
} from '@/features/payroll/types/payroll'
import { INDIAN_STATES, NO_PT_STATES } from '@/features/payroll/types/payroll'
import { formatINR } from '@/lib/currency'

interface ProfessionalTaxSlabFormProps {
  slab?: ProfessionalTaxSlab
  onSuccess: () => void
  onCancel: () => void
}

export const ProfessionalTaxSlabForm = ({
  slab,
  onSuccess,
  onCancel,
}: ProfessionalTaxSlabFormProps) => {
  const [formData, setFormData] = useState<CreateProfessionalTaxSlabDto>({
    state: '',
    minMonthlyIncome: 0,
    maxMonthlyIncome: null,
    monthlyTax: 0,
    februaryTax: null,
    effectiveFrom: null,
    effectiveTo: null,
    isActive: true,
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const [hasMaxIncome, setHasMaxIncome] = useState(true)
  const [hasFebruaryTax, setHasFebruaryTax] = useState(false)
  const [hasEffectiveFrom, setHasEffectiveFrom] = useState(false)
  const [hasEffectiveTo, setHasEffectiveTo] = useState(false)

  const createSlab = useCreateProfessionalTaxSlab()
  const updateSlab = useUpdateProfessionalTaxSlab()

  const isEditing = !!slab
  const isLoading = createSlab.isPending || updateSlab.isPending

  // Check if selected state levies PT
  const selectedStateHasNoPT = NO_PT_STATES.includes(formData.state as any)

  useEffect(() => {
    if (slab) {
      setFormData({
        state: slab.state,
        minMonthlyIncome: slab.minMonthlyIncome,
        maxMonthlyIncome: slab.maxMonthlyIncome ?? null,
        monthlyTax: slab.monthlyTax,
        februaryTax: slab.februaryTax ?? null,
        effectiveFrom: slab.effectiveFrom ?? null,
        effectiveTo: slab.effectiveTo ?? null,
        isActive: slab.isActive,
      })
      setHasMaxIncome(slab.maxMonthlyIncome !== null && slab.maxMonthlyIncome !== undefined)
      setHasFebruaryTax(slab.februaryTax !== null && slab.februaryTax !== undefined)
      setHasEffectiveFrom(slab.effectiveFrom !== null && slab.effectiveFrom !== undefined)
      setHasEffectiveTo(slab.effectiveTo !== null && slab.effectiveTo !== undefined)
    }
  }, [slab])

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value, type } = e.target
    let newValue: string | number | boolean | null = value

    if (type === 'number') {
      newValue = value === '' ? 0 : parseFloat(value)
    } else if (type === 'checkbox') {
      newValue = (e.target as HTMLInputElement).checked
    }

    setFormData((prev) => ({ ...prev, [name]: newValue }))
    setErrors((prev) => ({ ...prev, [name]: '' }))
  }

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.state) {
      newErrors.state = 'State is required'
    }

    if (formData.minMonthlyIncome < 0) {
      newErrors.minMonthlyIncome = 'Minimum income cannot be negative'
    }

    if (
      hasMaxIncome &&
      formData.maxMonthlyIncome != null &&
      formData.maxMonthlyIncome <= formData.minMonthlyIncome
    ) {
      newErrors.maxMonthlyIncome = 'Maximum income must be greater than minimum income'
    }

    if (formData.monthlyTax < 0) {
      newErrors.monthlyTax = 'Monthly tax cannot be negative'
    }

    if (hasFebruaryTax && formData.februaryTax != null && formData.februaryTax < 0) {
      newErrors.februaryTax = 'February tax cannot be negative'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    const submitData = {
      ...formData,
      maxMonthlyIncome: hasMaxIncome ? formData.maxMonthlyIncome : null,
      februaryTax: hasFebruaryTax ? formData.februaryTax : null,
      effectiveFrom: hasEffectiveFrom ? formData.effectiveFrom : null,
      effectiveTo: hasEffectiveTo ? formData.effectiveTo : null,
    }

    try {
      if (isEditing && slab) {
        await updateSlab.mutateAsync({
          id: slab.id,
          data: submitData as UpdateProfessionalTaxSlabDto,
        })
      } else {
        await createSlab.mutateAsync(submitData)
      }
      onSuccess()
    } catch (error) {
      // Error handled by mutation
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* State Warning */}
      {selectedStateHasNoPT && (
        <div className="bg-yellow-50 border-l-4 border-yellow-400 p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg
                className="h-5 w-5 text-yellow-400"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fillRule="evenodd"
                  d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                  clipRule="evenodd"
                />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-yellow-700">
                <strong>{formData.state}</strong> does not levy Professional Tax. You can
                still create a slab with zero tax for record-keeping purposes.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* State Selection */}
      <div>
        <label htmlFor="state" className="block text-sm font-medium text-gray-700">
          State *
        </label>
        <select
          id="state"
          name="state"
          value={formData.state}
          onChange={handleChange}
          className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
            errors.state ? 'border-red-500' : ''
          }`}
        >
          <option value="">Select a state</option>
          {INDIAN_STATES.map((state) => (
            <option key={state} value={state}>
              {state}
              {NO_PT_STATES.includes(state as any) ? ' (No PT)' : ''}
            </option>
          ))}
        </select>
        {errors.state && <p className="mt-1 text-sm text-red-600">{errors.state}</p>}
      </div>

      {/* Income Range */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label
            htmlFor="minMonthlyIncome"
            className="block text-sm font-medium text-gray-700"
          >
            Minimum Monthly Income (INR) *
          </label>
          <input
            type="number"
            id="minMonthlyIncome"
            name="minMonthlyIncome"
            value={formData.minMonthlyIncome}
            onChange={handleChange}
            min="0"
            step="1"
            className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
              errors.minMonthlyIncome ? 'border-red-500' : ''
            }`}
          />
          {errors.minMonthlyIncome && (
            <p className="mt-1 text-sm text-red-600">{errors.minMonthlyIncome}</p>
          )}
        </div>

        <div>
          <div className="flex items-center justify-between">
            <label
              htmlFor="maxMonthlyIncome"
              className="block text-sm font-medium text-gray-700"
            >
              Maximum Monthly Income (INR)
            </label>
            <label className="flex items-center text-xs text-gray-500">
              <input
                type="checkbox"
                checked={hasMaxIncome}
                onChange={(e) => {
                  setHasMaxIncome(e.target.checked)
                  if (!e.target.checked) {
                    setFormData((prev) => ({ ...prev, maxMonthlyIncome: null }))
                  }
                }}
                className="mr-1 rounded"
              />
              Has upper limit
            </label>
          </div>
          {hasMaxIncome ? (
            <input
              type="number"
              id="maxMonthlyIncome"
              name="maxMonthlyIncome"
              value={formData.maxMonthlyIncome ?? ''}
              onChange={handleChange}
              min="0"
              step="1"
              className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
                errors.maxMonthlyIncome ? 'border-red-500' : ''
              }`}
            />
          ) : (
            <div className="mt-1 py-2 px-3 bg-gray-100 rounded-md text-sm text-gray-500">
              No upper limit (highest slab)
            </div>
          )}
          {errors.maxMonthlyIncome && (
            <p className="mt-1 text-sm text-red-600">{errors.maxMonthlyIncome}</p>
          )}
        </div>
      </div>

      {/* Tax Amounts */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label
            htmlFor="monthlyTax"
            className="block text-sm font-medium text-gray-700"
          >
            Monthly Tax (INR) *
          </label>
          <input
            type="number"
            id="monthlyTax"
            name="monthlyTax"
            value={formData.monthlyTax}
            onChange={handleChange}
            min="0"
            step="1"
            className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
              errors.monthlyTax ? 'border-red-500' : ''
            }`}
          />
          {errors.monthlyTax && (
            <p className="mt-1 text-sm text-red-600">{errors.monthlyTax}</p>
          )}
        </div>

        <div>
          <div className="flex items-center justify-between">
            <label
              htmlFor="februaryTax"
              className="block text-sm font-medium text-gray-700"
            >
              February Tax (INR)
            </label>
            <label className="flex items-center text-xs text-gray-500">
              <input
                type="checkbox"
                checked={hasFebruaryTax}
                onChange={(e) => {
                  setHasFebruaryTax(e.target.checked)
                  if (!e.target.checked) {
                    setFormData((prev) => ({ ...prev, februaryTax: null }))
                  }
                }}
                className="mr-1 rounded"
              />
              Has Feb tax
            </label>
          </div>
          {hasFebruaryTax ? (
            <input
              type="number"
              id="februaryTax"
              name="februaryTax"
              value={formData.februaryTax ?? ''}
              onChange={handleChange}
              min="0"
              step="1"
              className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm ${
                errors.februaryTax ? 'border-red-500' : ''
              }`}
            />
          ) : (
            <div className="mt-1 py-2 px-3 bg-gray-100 rounded-md text-sm text-gray-500">
              Same as monthly tax
            </div>
          )}
          {errors.februaryTax && (
            <p className="mt-1 text-sm text-red-600">{errors.februaryTax}</p>
          )}
          <p className="mt-1 text-xs text-gray-500">
            States like Karnataka & Maharashtra have higher PT in February to meet the
            annual cap of Rs 2,500
          </p>
        </div>
      </div>

      {/* Effective Dates */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <div className="flex items-center justify-between">
            <label
              htmlFor="effectiveFrom"
              className="block text-sm font-medium text-gray-700"
            >
              Effective From
            </label>
            <label className="flex items-center text-xs text-gray-500">
              <input
                type="checkbox"
                checked={hasEffectiveFrom}
                onChange={(e) => {
                  setHasEffectiveFrom(e.target.checked)
                  if (!e.target.checked) {
                    setFormData((prev) => ({ ...prev, effectiveFrom: null }))
                  }
                }}
                className="mr-1 rounded"
              />
              Set date
            </label>
          </div>
          {hasEffectiveFrom ? (
            <input
              type="date"
              id="effectiveFrom"
              name="effectiveFrom"
              value={formData.effectiveFrom ?? ''}
              onChange={handleChange}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            />
          ) : (
            <div className="mt-1 py-2 px-3 bg-gray-100 rounded-md text-sm text-gray-500">
              No start date
            </div>
          )}
        </div>

        <div>
          <div className="flex items-center justify-between">
            <label
              htmlFor="effectiveTo"
              className="block text-sm font-medium text-gray-700"
            >
              Effective To
            </label>
            <label className="flex items-center text-xs text-gray-500">
              <input
                type="checkbox"
                checked={hasEffectiveTo}
                onChange={(e) => {
                  setHasEffectiveTo(e.target.checked)
                  if (!e.target.checked) {
                    setFormData((prev) => ({ ...prev, effectiveTo: null }))
                  }
                }}
                className="mr-1 rounded"
              />
              Set date
            </label>
          </div>
          {hasEffectiveTo ? (
            <input
              type="date"
              id="effectiveTo"
              name="effectiveTo"
              value={formData.effectiveTo ?? ''}
              onChange={handleChange}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            />
          ) : (
            <div className="mt-1 py-2 px-3 bg-gray-100 rounded-md text-sm text-gray-500">
              No end date (ongoing)
            </div>
          )}
        </div>
      </div>

      {/* Active Toggle */}
      <div className="flex items-center">
        <input
          type="checkbox"
          id="isActive"
          name="isActive"
          checked={formData.isActive}
          onChange={(e) =>
            setFormData((prev) => ({ ...prev, isActive: e.target.checked }))
          }
          className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
        />
        <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
          Active
        </label>
      </div>

      {/* Summary */}
      <div className="bg-blue-50 p-4 rounded-md">
        <h4 className="text-sm font-medium text-blue-800 mb-2">Summary</h4>
        <div className="text-sm text-blue-700 space-y-1">
          <p>
            <strong>Income Range:</strong>{' '}
            {formatINR(formData.minMonthlyIncome)} -{' '}
            {hasMaxIncome && formData.maxMonthlyIncome
              ? formatINR(formData.maxMonthlyIncome)
              : 'No limit'}
          </p>
          <p>
            <strong>Monthly PT:</strong> {formatINR(formData.monthlyTax)}
          </p>
          {hasFebruaryTax && formData.februaryTax != null && (
            <p>
              <strong>February PT:</strong> {formatINR(formData.februaryTax)}
            </p>
          )}
          <p>
            <strong>Annual PT:</strong>{' '}
            {formatINR(
              (formData.monthlyTax * 11) +
                (hasFebruaryTax && formData.februaryTax != null
                  ? formData.februaryTax
                  : formData.monthlyTax)
            )}
          </p>
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end gap-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isLoading
            ? isEditing
              ? 'Updating...'
              : 'Creating...'
            : isEditing
            ? 'Update Slab'
            : 'Create Slab'}
        </button>
      </div>
    </form>
  )
}

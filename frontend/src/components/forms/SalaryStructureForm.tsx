import { useState, useEffect, useMemo } from 'react'
import {
  useCreateSalaryStructure,
  useUpdateSalaryStructure,
  usePayrollInfoByType,
} from '@/features/payroll/hooks'
import type {
  EmployeeSalaryStructure,
  CreateEmployeeSalaryStructureDto,
  UpdateEmployeeSalaryStructureDto,
} from '@/features/payroll/types/payroll'
import { useEmployees } from '@/hooks/api/useEmployees'
import { useCompanies } from '@/hooks/api/useCompanies'
import { formatINR } from '@/lib/currency'

interface SalaryStructureFormProps {
  structure?: EmployeeSalaryStructure
  onSuccess: () => void
  onCancel: () => void
}

export const SalaryStructureForm = ({
  structure,
  onSuccess,
  onCancel,
}: SalaryStructureFormProps) => {
  const { data: allEmployees = [] } = useEmployees()
  const { data: companies = [] } = useCompanies()
  const { data: employeePayrollInfo = [] } = usePayrollInfoByType('employee')

  const [formData, setFormData] = useState<CreateEmployeeSalaryStructureDto>({
    employeeId: '',
    companyId: '',
    effectiveFrom: new Date().toISOString().split('T')[0],
    annualCtc: 0,
    basicSalary: 0,
    hra: 0,
    dearnessAllowance: 0,
    conveyanceAllowance: 0,
    medicalAllowance: 0,
    specialAllowance: 0,
    otherAllowances: 0,
    ltaAnnual: 0,
    bonusAnnual: 0,
    pfEmployerMonthly: 0,
    esiEmployerMonthly: 0,
    gratuityMonthly: 0,
    revisionReason: '',
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const [useCtcCalculator, setUseCtcCalculator] = useState(true)
  const createStructure = useCreateSalaryStructure()
  const updateStructure = useUpdateSalaryStructure()

  const isEditing = !!structure
  const isLoading = createStructure.isPending || updateStructure.isPending

  // Filter employees: only full-time employees (not contractors) and optionally by company
  const filteredEmployees = useMemo(() => {
    // Get employee IDs that are marked as 'employee' (not contractor)
    const employeeIds = new Set(employeePayrollInfo.map(info => info.employeeId))
    
    // Filter employees by:
    // 1. Must be a full-time employee (not contractor)
    // 2. If company is selected, must belong to that company
    return allEmployees.filter(emp => {
      const isFullTimeEmployee = employeeIds.has(emp.id)
      const matchesCompany = !formData.companyId || emp.companyId === formData.companyId
      return isFullTimeEmployee && matchesCompany
    })
  }, [allEmployees, employeePayrollInfo, formData.companyId])

  // Calculate monthly gross from components (includes LTA as it's paid monthly in India)
  const calculatedMonthlyGross = useMemo(() => {
    return (
      formData.basicSalary +
      formData.hra +
      formData.dearnessAllowance +
      formData.conveyanceAllowance +
      formData.medicalAllowance +
      formData.specialAllowance +
      formData.otherAllowances +
      (formData.ltaAnnual / 12) // LTA is paid monthly, tax exemption is annual
    )
  }, [
    formData.basicSalary,
    formData.hra,
    formData.dearnessAllowance,
    formData.conveyanceAllowance,
    formData.medicalAllowance,
    formData.specialAllowance,
    formData.otherAllowances,
    formData.ltaAnnual,
  ])

  // Calculate annual CTC from monthly components
  // LTA is already included in monthly gross (paid monthly), so only add bonus separately
  const calculatedAnnualCtc = useMemo(() => {
    const monthlyGross = calculatedMonthlyGross
    const monthlyCtc = monthlyGross + formData.pfEmployerMonthly + formData.esiEmployerMonthly + formData.gratuityMonthly
    const annualCtc = monthlyCtc * 12 + formData.bonusAnnual // LTA already in monthlyGross
    return annualCtc
  }, [calculatedMonthlyGross, formData.pfEmployerMonthly, formData.esiEmployerMonthly, formData.gratuityMonthly, formData.bonusAnnual])

  // Auto-calculate from CTC
  const calculateFromCtc = (ctc: number) => {
    const monthlyCtc = ctc / 12
    // Standard breakdown: Basic = 40-50% of CTC, HRA = 50% of Basic
    const basic = Math.round(monthlyCtc * 0.45) // 45% of monthly CTC
    const hra = Math.round(basic * 0.5) // 50% of basic
    const da = Math.round(basic * 0.1) // 10% of basic (if applicable)
    const conveyance = 1600 // Standard conveyance allowance
    const medical = 1250 // Standard medical allowance
    const remaining = monthlyCtc - basic - hra - da - conveyance - medical
    const specialAllowance = Math.max(0, remaining * 0.8)
    const otherAllowances = Math.max(0, remaining * 0.2)

    // Employer contributions (approximate)
    const pfEmployer = Math.round(Math.min(basic, 15000) * 0.12) // 12% of basic (capped at 15k)
    const esiEmployer = 0 // Only if gross <= 21k
    const gratuity = Math.round(basic * 0.0481) // 4.81% of basic

    setFormData((prev) => ({
      ...prev,
      basicSalary: basic,
      hra: hra,
      dearnessAllowance: da,
      conveyanceAllowance: conveyance,
      medicalAllowance: medical,
      specialAllowance: specialAllowance,
      otherAllowances: otherAllowances,
      pfEmployerMonthly: pfEmployer,
      esiEmployerMonthly: esiEmployer,
      gratuityMonthly: gratuity,
    }))
  }

  useEffect(() => {
    if (structure) {
      setFormData({
        employeeId: structure.employeeId,
        companyId: structure.companyId,
        effectiveFrom: structure.effectiveFrom.split('T')[0],
        annualCtc: structure.annualCtc,
        basicSalary: structure.basicSalary,
        hra: structure.hra,
        dearnessAllowance: structure.dearnessAllowance,
        conveyanceAllowance: structure.conveyanceAllowance,
        medicalAllowance: structure.medicalAllowance,
        specialAllowance: structure.specialAllowance,
        otherAllowances: structure.otherAllowances,
        ltaAnnual: structure.ltaAnnual,
        bonusAnnual: structure.bonusAnnual,
        pfEmployerMonthly: structure.pfEmployerMonthly,
        esiEmployerMonthly: structure.esiEmployerMonthly,
        gratuityMonthly: structure.gratuityMonthly,
        revisionReason: structure.revisionReason || '',
      })
      // Keep useCtcCalculator enabled so users can recalculate when editing
      setUseCtcCalculator(true)
    }
  }, [structure])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.employeeId) newErrors.employeeId = 'Employee is required'
    if (!formData.companyId) newErrors.companyId = 'Company is required'
    if (!formData.effectiveFrom) newErrors.effectiveFrom = 'Effective from date is required'
    if (formData.annualCtc <= 0) newErrors.annualCtc = 'Annual CTC must be greater than 0'

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && structure) {
        await updateStructure.mutateAsync({
          id: structure.id,
          data: formData as UpdateEmployeeSalaryStructureDto,
        })
      } else {
        await createStructure.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Failed to save salary structure:', error)
    }
  }

  const handleInputChange = (
    field: keyof CreateEmployeeSalaryStructureDto,
    value: string | number
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }))
    }

    // Auto-calculate from CTC if enabled
    if (useCtcCalculator && field === 'annualCtc' && typeof value === 'number' && value > 0) {
      calculateFromCtc(value)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Company <span className="text-red-500">*</span>
          </label>
          <select
            className={`w-full rounded-md border px-3 py-2 ${
              errors.companyId ? 'border-red-500' : 'border-gray-300'
            }`}
            value={formData.companyId}
            onChange={(e) => {
              handleInputChange('companyId', e.target.value)
              // Clear employee selection when company changes
              if (!isEditing) {
                handleInputChange('employeeId', '')
              }
            }}
            disabled={isEditing}
          >
            <option value="">Select company</option>
            {companies.map((company) => (
              <option key={company.id} value={company.id}>
                {company.name}
              </option>
            ))}
          </select>
          {errors.companyId && (
            <p className="text-red-500 text-xs mt-1">{errors.companyId}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Employee <span className="text-red-500">*</span>
          </label>
          <select
            className={`w-full rounded-md border px-3 py-2 ${
              errors.employeeId ? 'border-red-500' : 'border-gray-300'
            }`}
            value={formData.employeeId}
            onChange={(e) => handleInputChange('employeeId', e.target.value)}
            disabled={isEditing}
          >
            <option value="">Select employee</option>
            {filteredEmployees.length === 0 ? (
              <option value="" disabled>
                {formData.companyId 
                  ? 'No full-time employees found for this company'
                  : 'Please select a company first'}
              </option>
            ) : (
              filteredEmployees.map((emp) => (
                <option key={emp.id} value={emp.id}>
                  {emp.employeeName}
                </option>
              ))
            )}
          </select>
          {errors.employeeId && (
            <p className="text-red-500 text-xs mt-1">{errors.employeeId}</p>
          )}
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Effective From <span className="text-red-500">*</span>
        </label>
        <input
          type="date"
          className={`w-full rounded-md border px-3 py-2 ${
            errors.effectiveFrom ? 'border-red-500' : 'border-gray-300'
          }`}
          value={formData.effectiveFrom}
          onChange={(e) => handleInputChange('effectiveFrom', e.target.value)}
        />
        {errors.effectiveFrom && (
          <p className="text-red-500 text-xs mt-1">{errors.effectiveFrom}</p>
        )}
      </div>

      {/* CTC Calculator */}
      <div className="bg-blue-50 p-4 rounded-lg">
        <div className="flex items-center gap-2 mb-3">
          <input
            type="checkbox"
            id="useCtcCalculator"
            checked={useCtcCalculator}
            onChange={(e) => setUseCtcCalculator(e.target.checked)}
          />
          <label htmlFor="useCtcCalculator" className="text-sm font-medium">
            Use CTC Calculator (Auto-calculate components from CTC)
          </label>
        </div>

        {useCtcCalculator && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Annual CTC <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              step="0.01"
              className={`w-full rounded-md border px-3 py-2 ${
                errors.annualCtc ? 'border-red-500' : 'border-gray-300'
              }`}
              value={formData.annualCtc}
              onChange={(e) => handleInputChange('annualCtc', parseFloat(e.target.value) || 0)}
              placeholder="Enter annual CTC"
            />
            {errors.annualCtc && (
              <p className="text-red-500 text-xs mt-1">{errors.annualCtc}</p>
            )}
            <p className="text-xs text-gray-500 mt-1">
              Components will be auto-calculated based on standard breakdown
            </p>
          </div>
        )}
      </div>

      {/* Manual Component Entry */}
      {!useCtcCalculator && (
        <>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Annual CTC
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.annualCtc}
                onChange={(e) => handleInputChange('annualCtc', parseFloat(e.target.value) || 0)}
              />
            </div>
          </div>

          <div>
            <h3 className="text-sm font-semibold mb-3">Monthly Components</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Basic Salary
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.basicSalary}
                  onChange={(e) => handleInputChange('basicSalary', parseFloat(e.target.value) || 0)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  HRA
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.hra}
                  onChange={(e) => handleInputChange('hra', parseFloat(e.target.value) || 0)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Dearness Allowance
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.dearnessAllowance}
                  onChange={(e) => handleInputChange('dearnessAllowance', parseFloat(e.target.value) || 0)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Conveyance Allowance
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.conveyanceAllowance}
                  onChange={(e) => handleInputChange('conveyanceAllowance', parseFloat(e.target.value) || 0)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Medical Allowance
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.medicalAllowance}
                  onChange={(e) => handleInputChange('medicalAllowance', parseFloat(e.target.value) || 0)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Special Allowance
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.specialAllowance}
                  onChange={(e) => handleInputChange('specialAllowance', parseFloat(e.target.value) || 0)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Other Allowances
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.otherAllowances}
                  onChange={(e) => handleInputChange('otherAllowances', parseFloat(e.target.value) || 0)}
                />
              </div>
            </div>

            <div className="mt-4 p-3 bg-gray-50 rounded">
              <div className="flex justify-between">
                <span className="text-sm text-gray-600">Calculated Monthly Gross:</span>
                <span className="font-semibold">{formatINR(calculatedMonthlyGross)}</span>
              </div>
            </div>
          </div>

          <div>
            <h3 className="text-sm font-semibold mb-3">Annual Components</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  LTA (Annual)
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.ltaAnnual}
                  onChange={(e) => handleInputChange('ltaAnnual', parseFloat(e.target.value) || 0)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Bonus (Annual)
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.bonusAnnual}
                  onChange={(e) => handleInputChange('bonusAnnual', parseFloat(e.target.value) || 0)}
                />
              </div>
            </div>
          </div>

          <div>
            <h3 className="text-sm font-semibold mb-3">Employer Contributions (Monthly)</h3>
            <div className="grid grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  PF (Employer)
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.pfEmployerMonthly}
                  onChange={(e) => handleInputChange('pfEmployerMonthly', parseFloat(e.target.value) || 0)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  ESI (Employer)
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.esiEmployerMonthly}
                  onChange={(e) => handleInputChange('esiEmployerMonthly', parseFloat(e.target.value) || 0)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Gratuity
                </label>
                <input
                  type="number"
                  step="0.01"
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                  value={formData.gratuityMonthly}
                  onChange={(e) => handleInputChange('gratuityMonthly', parseFloat(e.target.value) || 0)}
                />
              </div>
            </div>
          </div>

          <div className="mt-4 p-3 bg-blue-50 rounded">
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Calculated Annual CTC:</span>
              <span className="font-semibold">{formatINR(calculatedAnnualCtc)}</span>
            </div>
          </div>
        </>
      )}

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Revision Reason
        </label>
        <textarea
          className="w-full rounded-md border border-gray-300 px-3 py-2"
          value={formData.revisionReason}
          onChange={(e) => handleInputChange('revisionReason', e.target.value)}
          rows={2}
          placeholder="Reason for salary revision (e.g., promotion, increment)"
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
          {isLoading ? 'Saving...' : isEditing ? 'Update' : 'Create'} Structure
        </button>
      </div>
    </form>
  )
}





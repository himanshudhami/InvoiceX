import { useState, useEffect, useMemo } from 'react'
import {
  useCreateTaxDeclaration,
  useUpdateTaxDeclaration,
  useReviseTaxDeclaration,
  usePayrollInfoByType,
} from '@/features/payroll/hooks'
import type {
  EmployeeTaxDeclaration,
  CreateEmployeeTaxDeclarationDto,
  UpdateEmployeeTaxDeclarationDto,
} from '@/features/payroll/types/payroll'
import { TAX_LIMITS } from '@/features/payroll/types/payroll'
import { useEmployees } from '@/hooks/api/useEmployees'
import { useCompanies } from '@/hooks/api/useCompanies'
import { EmployeeSelect } from '@/components/ui/EmployeeSelect'
import { CompanySelect } from '@/components/ui/CompanySelect'
import { formatINR } from '@/lib/currency'

interface TaxDeclarationFormProps {
  declaration?: EmployeeTaxDeclaration
  isRevision?: boolean
  onSuccess: () => void
  onCancel: () => void
}

export const TaxDeclarationForm = ({
  declaration,
  isRevision = false,
  onSuccess,
  onCancel,
}: TaxDeclarationFormProps) => {
  // Track the selected company and use it to scope employees
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>(declaration?.employee?.companyId || '')

  const { data: allEmployees = [] } = useEmployees(selectedCompanyId || undefined)
  const { data: companies = [] } = useCompanies()
  const { data: employeePayrollInfo = [] } = usePayrollInfoByType('employee')

  const currentYear = new Date().getFullYear()
  const financialYear = new Date().getMonth() >= 3
    ? `${currentYear}-${(currentYear + 1) % 100}`
    : `${currentYear - 1}-${currentYear % 100}`

  const [formData, setFormData] = useState<CreateEmployeeTaxDeclarationDto>({
    employeeId: '',
    financialYear: financialYear,
    taxRegime: 'new',
    // Section 80C
    sec80cPpf: 0,
    sec80cElss: 0,
    sec80cLifeInsurance: 0,
    sec80cHomeLoanPrincipal: 0,
    sec80cChildrenTuition: 0,
    sec80cNsc: 0,
    sec80cSukanyaSamriddhi: 0,
    sec80cFixedDeposit: 0,
    sec80cOthers: 0,
    // Section 80CCD(1B)
    sec80ccdNps: 0,
    // Section 80D
    sec80dSelfFamily: 0,
    sec80dParents: 0,
    sec80dPreventiveCheckup: 0,
    sec80dSelfSeniorCitizen: false,
    sec80dParentsSeniorCitizen: false,
    // Other Sections
    sec80eEducationLoan: 0,
    sec24HomeLoanInterest: 0,
    sec80gDonations: 0,
    sec80ttaSavingsInterest: 0,
    // HRA
    hraRentPaidAnnual: 0,
    hraMetroCity: false,
    hraLandlordPan: '',
    hraLandlordName: '',
    // Other Income
    otherIncomeAnnual: 0,
    // Column 388A - Other TDS/TCS Credits
    otherTdsInterest: 0,
    otherTdsDividend: 0,
    otherTdsCommission: 0,
    otherTdsRent: 0,
    otherTdsProfessional: 0,
    otherTdsOthers: 0,
    tcsForeignRemittance: 0,
    tcsOverseasTour: 0,
    tcsVehiclePurchase: 0,
    tcsOthers: 0,
    otherTdsTcsDetails: '',
    // Previous Employer
    prevEmployerIncome: 0,
    prevEmployerTds: 0,
    prevEmployerPf: 0,
    prevEmployerPt: 0,
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createDeclaration = useCreateTaxDeclaration()
  const updateDeclaration = useUpdateTaxDeclaration()
  const reviseDeclaration = useReviseTaxDeclaration()

  const isEditing = !!declaration
  const isLoading = createDeclaration.isPending || updateDeclaration.isPending || reviseDeclaration.isPending

  // Filter employees: only full-time employees (not contractors) and optionally by company
  const filteredEmployees = useMemo(() => {
    // Get employee IDs that are marked as 'employee' (not contractor)
    const employeeIds = new Set(employeePayrollInfo.map(info => info.employeeId))
    
    // Filter employees by:
    // 1. Must be a full-time employee (not contractor)
    // 2. If company is selected, must belong to that company
    return allEmployees.filter(emp => {
      const isFullTimeEmployee = employeeIds.has(emp.id)
      const matchesCompany = !selectedCompanyId || emp.companyId === selectedCompanyId
      return isFullTimeEmployee && matchesCompany
    })
  }, [allEmployees, employeePayrollInfo, selectedCompanyId])

  // Load company from employee when editing
  useEffect(() => {
    if (!selectedCompanyId && declaration?.employeeId) {
      const employee = allEmployees.find(e => e.id === declaration.employeeId)
      if (employee?.companyId) {
        setSelectedCompanyId(employee.companyId)
      }
    }
  }, [declaration, allEmployees, selectedCompanyId])

  // Calculate totals
  const total80c = useMemo(() => {
    return (
      (formData.sec80cPpf || 0) +
      (formData.sec80cElss || 0) +
      (formData.sec80cLifeInsurance || 0) +
      (formData.sec80cHomeLoanPrincipal || 0) +
      (formData.sec80cChildrenTuition || 0) +
      (formData.sec80cNsc || 0) +
      (formData.sec80cSukanyaSamriddhi || 0) +
      (formData.sec80cFixedDeposit || 0) +
      (formData.sec80cOthers || 0)
    )
  }, [
    formData.sec80cPpf,
    formData.sec80cElss,
    formData.sec80cLifeInsurance,
    formData.sec80cHomeLoanPrincipal,
    formData.sec80cChildrenTuition,
    formData.sec80cNsc,
    formData.sec80cSukanyaSamriddhi,
    formData.sec80cFixedDeposit,
    formData.sec80cOthers,
  ])

  const allowed80c = Math.min(total80c, 150000)
  const allowed80ccd = Math.min(formData.sec80ccdNps || 0, 50000)

  useEffect(() => {
    if (declaration) {
      setFormData({
        employeeId: declaration.employeeId,
        financialYear: declaration.financialYear,
        taxRegime: declaration.taxRegime,
        sec80cPpf: declaration.sec80cPpf,
        sec80cElss: declaration.sec80cElss,
        sec80cLifeInsurance: declaration.sec80cLifeInsurance,
        sec80cHomeLoanPrincipal: declaration.sec80cHomeLoanPrincipal,
        sec80cChildrenTuition: declaration.sec80cChildrenTuition,
        sec80cNsc: declaration.sec80cNsc,
        sec80cSukanyaSamriddhi: declaration.sec80cSukanyaSamriddhi,
        sec80cFixedDeposit: declaration.sec80cFixedDeposit,
        sec80cOthers: declaration.sec80cOthers,
        sec80ccdNps: declaration.sec80ccdNps,
        sec80dSelfFamily: declaration.sec80dSelfFamily,
        sec80dParents: declaration.sec80dParents,
        sec80dPreventiveCheckup: declaration.sec80dPreventiveCheckup,
        sec80dSelfSeniorCitizen: declaration.sec80dSelfSeniorCitizen,
        sec80dParentsSeniorCitizen: declaration.sec80dParentsSeniorCitizen,
        sec80eEducationLoan: declaration.sec80eEducationLoan,
        sec24HomeLoanInterest: declaration.sec24HomeLoanInterest,
        sec80gDonations: declaration.sec80gDonations,
        sec80ttaSavingsInterest: declaration.sec80ttaSavingsInterest,
        hraRentPaidAnnual: declaration.hraRentPaidAnnual,
        hraMetroCity: declaration.hraMetroCity,
        hraLandlordPan: declaration.hraLandlordPan || '',
        hraLandlordName: declaration.hraLandlordName || '',
        otherIncomeAnnual: declaration.otherIncomeAnnual,
        // Column 388A
        otherTdsInterest: declaration.otherTdsInterest || 0,
        otherTdsDividend: declaration.otherTdsDividend || 0,
        otherTdsCommission: declaration.otherTdsCommission || 0,
        otherTdsRent: declaration.otherTdsRent || 0,
        otherTdsProfessional: declaration.otherTdsProfessional || 0,
        otherTdsOthers: declaration.otherTdsOthers || 0,
        tcsForeignRemittance: declaration.tcsForeignRemittance || 0,
        tcsOverseasTour: declaration.tcsOverseasTour || 0,
        tcsVehiclePurchase: declaration.tcsVehiclePurchase || 0,
        tcsOthers: declaration.tcsOthers || 0,
        otherTdsTcsDetails: declaration.otherTdsTcsDetails || '',
        prevEmployerIncome: declaration.prevEmployerIncome,
        prevEmployerTds: declaration.prevEmployerTds,
        prevEmployerPf: declaration.prevEmployerPf,
        prevEmployerPt: declaration.prevEmployerPt,
      })
    }
  }, [declaration])

  // Calculate 80D limits based on senior citizen status
  const max80dSelfFamily = formData.sec80dSelfSeniorCitizen
    ? TAX_LIMITS.MAX_80D_SELF_FAMILY_SENIOR
    : TAX_LIMITS.MAX_80D_SELF_FAMILY
  const max80dParents = formData.sec80dParentsSeniorCitizen
    ? TAX_LIMITS.MAX_80D_PARENTS_SENIOR
    : TAX_LIMITS.MAX_80D_PARENTS

  // PAN format validation regex
  const PAN_REGEX = /^[A-Z]{5}[0-9]{4}[A-Z]$/

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.employeeId) newErrors.employeeId = 'Employee is required'
    if (!formData.financialYear) newErrors.financialYear = 'Financial year is required'

    // Only validate deduction limits for old regime
    if (formData.taxRegime === 'old') {
      // Section 80C total limit
      if (total80c > TAX_LIMITS.MAX_80C) {
        newErrors.sec80cTotal = `Total 80C deductions (${formatINR(total80c)}) exceeds limit of ${formatINR(TAX_LIMITS.MAX_80C)}`
      }

      // Section 80D - Self/Family limit
      if ((formData.sec80dSelfFamily || 0) > max80dSelfFamily) {
        newErrors.sec80dSelfFamily = `Cannot exceed ${formatINR(max80dSelfFamily)} ${formData.sec80dSelfSeniorCitizen ? '(senior citizen)' : ''}`
      }

      // Section 80D - Parents limit
      if ((formData.sec80dParents || 0) > max80dParents) {
        newErrors.sec80dParents = `Cannot exceed ${formatINR(max80dParents)} ${formData.sec80dParentsSeniorCitizen ? '(senior citizen)' : ''}`
      }

      // Section 80D - Preventive checkup limit
      if ((formData.sec80dPreventiveCheckup || 0) > TAX_LIMITS.MAX_80D_PREVENTIVE) {
        newErrors.sec80dPreventiveCheckup = `Cannot exceed ${formatINR(TAX_LIMITS.MAX_80D_PREVENTIVE)}`
      }

      // Section 24 - Home loan interest limit
      if ((formData.sec24HomeLoanInterest || 0) > TAX_LIMITS.MAX_SECTION_24) {
        newErrors.sec24HomeLoanInterest = `Cannot exceed ${formatINR(TAX_LIMITS.MAX_SECTION_24)}`
      }

      // Section 80CCD NPS limit
      if ((formData.sec80ccdNps || 0) > TAX_LIMITS.MAX_80CCD_NPS) {
        newErrors.sec80ccdNps = `Cannot exceed ${formatINR(TAX_LIMITS.MAX_80CCD_NPS)}`
      }

      // Section 80TTA limit
      if ((formData.sec80ttaSavingsInterest || 0) > TAX_LIMITS.MAX_80TTA) {
        newErrors.sec80ttaSavingsInterest = `Cannot exceed ${formatINR(TAX_LIMITS.MAX_80TTA)}`
      }

      // HRA Landlord PAN validation
      if ((formData.hraRentPaidAnnual || 0) > TAX_LIMITS.HRA_PAN_THRESHOLD) {
        if (!formData.hraLandlordPan) {
          newErrors.hraLandlordPan = `Landlord PAN is mandatory when annual rent exceeds ${formatINR(TAX_LIMITS.HRA_PAN_THRESHOLD)}`
        } else if (!PAN_REGEX.test(formData.hraLandlordPan)) {
          newErrors.hraLandlordPan = 'Invalid PAN format (e.g., ABCDE1234F)'
        }
      } else if (formData.hraLandlordPan && !PAN_REGEX.test(formData.hraLandlordPan)) {
        newErrors.hraLandlordPan = 'Invalid PAN format (e.g., ABCDE1234F)'
      }
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isRevision && declaration) {
        // Revise and resubmit rejected declaration
        await reviseDeclaration.mutateAsync({
          id: declaration.id,
          data: formData as UpdateEmployeeTaxDeclarationDto,
        })
      } else if (isEditing && declaration) {
        await updateDeclaration.mutateAsync({
          id: declaration.id,
          data: formData as UpdateEmployeeTaxDeclarationDto,
        })
      } else {
        await createDeclaration.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Failed to save tax declaration:', error)
    }
  }

  const handleInputChange = (
    field: keyof CreateEmployeeTaxDeclarationDto,
    value: string | number | boolean
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }))
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6 max-h-[80vh] overflow-y-auto">
      {/* Basic Info */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Company
          </label>
          <CompanySelect
            companies={companies}
            value={selectedCompanyId}
            onChange={(value) => {
              setSelectedCompanyId(value)
              if (!isEditing) {
                handleInputChange('employeeId', '')
              }
            }}
            placeholder="Select company..."
            disabled={isEditing}
          />
          <p className="text-xs text-gray-500 mt-1">
            Filter employees by company (only full-time employees shown)
          </p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Employee <span className="text-red-500">*</span>
          </label>
          <EmployeeSelect
            employees={filteredEmployees}
            value={formData.employeeId}
            onChange={(value) => handleInputChange('employeeId', value)}
            placeholder={selectedCompanyId ? "Search employee..." : "Select company first..."}
            disabled={isEditing}
            error={errors.employeeId}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Financial Year <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            className={`w-full rounded-md border px-3 py-2 ${
              errors.financialYear ? 'border-red-500' : 'border-gray-300'
            }`}
            value={formData.financialYear}
            onChange={(e) => handleInputChange('financialYear', e.target.value)}
            placeholder="2024-25"
            disabled={isEditing}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Tax Regime
          </label>
          <select
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.taxRegime}
            onChange={(e) => handleInputChange('taxRegime', e.target.value as 'old' | 'new')}
            disabled={isEditing}
          >
            <option value="new">New Regime</option>
            <option value="old">Old Regime</option>
          </select>
        </div>
      </div>

      {/* Section 80C - Only for Old Regime */}
      {formData.taxRegime === 'old' && (
        <div className="border rounded-lg p-4">
          <h3 className="font-semibold mb-3">Section 80C Deductions (Max ₹1,50,000)</h3>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">PPF</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80cPpf}
                onChange={(e) => handleInputChange('sec80cPpf', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">ELSS</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80cElss}
                onChange={(e) => handleInputChange('sec80cElss', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Life Insurance</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80cLifeInsurance}
                onChange={(e) => handleInputChange('sec80cLifeInsurance', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Home Loan Principal</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80cHomeLoanPrincipal}
                onChange={(e) => handleInputChange('sec80cHomeLoanPrincipal', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Children Tuition</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80cChildrenTuition}
                onChange={(e) => handleInputChange('sec80cChildrenTuition', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">NSC</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80cNsc}
                onChange={(e) => handleInputChange('sec80cNsc', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Sukanya Samriddhi</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80cSukanyaSamriddhi}
                onChange={(e) => handleInputChange('sec80cSukanyaSamriddhi', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Fixed Deposit</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80cFixedDeposit}
                onChange={(e) => handleInputChange('sec80cFixedDeposit', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Others</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80cOthers}
                onChange={(e) => handleInputChange('sec80cOthers', parseFloat(e.target.value) || 0)}
              />
            </div>
          </div>
          <div className={`mt-3 p-2 rounded ${total80c > TAX_LIMITS.MAX_80C ? 'bg-red-50 border border-red-200' : 'bg-gray-50'}`}>
            <div className="flex justify-between text-sm">
              <span>Total 80C:</span>
              <span className={`font-medium ${total80c > TAX_LIMITS.MAX_80C ? 'text-red-600' : ''}`}>
                {formatINR(total80c)}
              </span>
            </div>
            <div className="flex justify-between text-sm">
              <span>Allowed (capped at {formatINR(TAX_LIMITS.MAX_80C)}):</span>
              <span className="font-semibold">{formatINR(allowed80c)}</span>
            </div>
            {total80c > TAX_LIMITS.MAX_80C && (
              <p className="text-red-600 text-xs mt-1">
                Excess of {formatINR(total80c - TAX_LIMITS.MAX_80C)} will not be considered for tax deduction
              </p>
            )}
            {errors.sec80cTotal && (
              <p className="text-red-500 text-xs mt-1 font-medium">{errors.sec80cTotal}</p>
            )}
          </div>
        </div>
      )}

      {/* Section 80CCD(1B) - Only for Old Regime */}
      {formData.taxRegime === 'old' && (
        <div className="border rounded-lg p-4">
          <h3 className="font-semibold mb-3">Section 80CCD(1B) - NPS (Max ₹50,000)</h3>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">NPS Contribution</label>
            <input
              type="number"
              step="0.01"
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.sec80ccdNps}
              onChange={(e) => handleInputChange('sec80ccdNps', parseFloat(e.target.value) || 0)}
            />
            <div className="mt-2 p-2 bg-gray-50 rounded">
              <div className="flex justify-between text-sm">
                <span>Allowed (capped at ₹50,000):</span>
                <span className="font-semibold">{formatINR(allowed80ccd)}</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Section 80D - Only for Old Regime */}
      {formData.taxRegime === 'old' && (
        <div className="border rounded-lg p-4">
          <h3 className="font-semibold mb-3">Section 80D - Health Insurance</h3>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Self/Family <span className="text-gray-500 text-xs">(Max {formatINR(max80dSelfFamily)})</span>
              </label>
              <input
                type="number"
                step="0.01"
                className={`w-full rounded-md border px-3 py-2 ${
                  errors.sec80dSelfFamily ? 'border-red-500' : 'border-gray-300'
                }`}
                value={formData.sec80dSelfFamily}
                onChange={(e) => handleInputChange('sec80dSelfFamily', parseFloat(e.target.value) || 0)}
              />
              <div className="mt-1">
                <input
                  type="checkbox"
                  id="sec80dSelfSeniorCitizen"
                  checked={formData.sec80dSelfSeniorCitizen}
                  onChange={(e) => handleInputChange('sec80dSelfSeniorCitizen', e.target.checked)}
                />
                <label htmlFor="sec80dSelfSeniorCitizen" className="text-xs text-gray-600 ml-1">
                  Senior Citizen (limit {formatINR(TAX_LIMITS.MAX_80D_SELF_FAMILY_SENIOR)})
                </label>
              </div>
              {errors.sec80dSelfFamily && (
                <p className="text-red-500 text-xs mt-1">{errors.sec80dSelfFamily}</p>
              )}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Parents <span className="text-gray-500 text-xs">(Max {formatINR(max80dParents)})</span>
              </label>
              <input
                type="number"
                step="0.01"
                className={`w-full rounded-md border px-3 py-2 ${
                  errors.sec80dParents ? 'border-red-500' : 'border-gray-300'
                }`}
                value={formData.sec80dParents}
                onChange={(e) => handleInputChange('sec80dParents', parseFloat(e.target.value) || 0)}
              />
              <div className="mt-1">
                <input
                  type="checkbox"
                  id="sec80dParentsSeniorCitizen"
                  checked={formData.sec80dParentsSeniorCitizen}
                  onChange={(e) => handleInputChange('sec80dParentsSeniorCitizen', e.target.checked)}
                />
                <label htmlFor="sec80dParentsSeniorCitizen" className="text-xs text-gray-600 ml-1">
                  Senior Citizen (limit {formatINR(TAX_LIMITS.MAX_80D_PARENTS_SENIOR)})
                </label>
              </div>
              {errors.sec80dParents && (
                <p className="text-red-500 text-xs mt-1">{errors.sec80dParents}</p>
              )}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Preventive Checkup <span className="text-gray-500 text-xs">(Max {formatINR(TAX_LIMITS.MAX_80D_PREVENTIVE)})</span>
              </label>
              <input
                type="number"
                step="0.01"
                className={`w-full rounded-md border px-3 py-2 ${
                  errors.sec80dPreventiveCheckup ? 'border-red-500' : 'border-gray-300'
                }`}
                value={formData.sec80dPreventiveCheckup}
                onChange={(e) => handleInputChange('sec80dPreventiveCheckup', parseFloat(e.target.value) || 0)}
              />
              {errors.sec80dPreventiveCheckup && (
                <p className="text-red-500 text-xs mt-1">{errors.sec80dPreventiveCheckup}</p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* HRA - Only for Old Regime */}
      {formData.taxRegime === 'old' && (
        <div className="border rounded-lg p-4">
          <h3 className="font-semibold mb-3">HRA Exemption</h3>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Annual Rent Paid
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.hraRentPaidAnnual}
                onChange={(e) => handleInputChange('hraRentPaidAnnual', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="hraMetroCity"
                checked={formData.hraMetroCity}
                onChange={(e) => handleInputChange('hraMetroCity', e.target.checked)}
              />
              <label htmlFor="hraMetroCity" className="text-sm font-medium">
                Metro City (50% of Basic) / Non-Metro (40% of Basic)
              </label>
            </div>
            {(formData.hraRentPaidAnnual || 0) > 100000 && (
              <>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Landlord PAN <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    className={`w-full rounded-md border px-3 py-2 ${
                      errors.hraLandlordPan ? 'border-red-500' : 'border-gray-300'
                    }`}
                    value={formData.hraLandlordPan}
                    onChange={(e) => handleInputChange('hraLandlordPan', e.target.value.toUpperCase())}
                    placeholder="ABCDE1234F"
                    maxLength={10}
                  />
                  {errors.hraLandlordPan && (
                    <p className="text-red-500 text-xs mt-1">{errors.hraLandlordPan}</p>
                  )}
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Landlord Name
                  </label>
                  <input
                    type="text"
                    className="w-full rounded-md border border-gray-300 px-3 py-2"
                    value={formData.hraLandlordName}
                    onChange={(e) => handleInputChange('hraLandlordName', e.target.value)}
                  />
                </div>
              </>
            )}
          </div>
        </div>
      )}

      {/* Other Deductions - Only for Old Regime */}
      {formData.taxRegime === 'old' && (
        <div className="border rounded-lg p-4">
          <h3 className="font-semibold mb-3">Other Deductions</h3>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Section 80E - Education Loan Interest
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80eEducationLoan}
                onChange={(e) => handleInputChange('sec80eEducationLoan', parseFloat(e.target.value) || 0)}
              />
              <p className="text-xs text-gray-500 mt-1">No limit</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Section 24 - Home Loan Interest <span className="text-gray-500 text-xs">(Max {formatINR(TAX_LIMITS.MAX_SECTION_24)})</span>
              </label>
              <input
                type="number"
                step="0.01"
                className={`w-full rounded-md border px-3 py-2 ${
                  errors.sec24HomeLoanInterest ? 'border-red-500' : 'border-gray-300'
                }`}
                value={formData.sec24HomeLoanInterest}
                onChange={(e) => handleInputChange('sec24HomeLoanInterest', parseFloat(e.target.value) || 0)}
              />
              {errors.sec24HomeLoanInterest && (
                <p className="text-red-500 text-xs mt-1">{errors.sec24HomeLoanInterest}</p>
              )}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Section 80G - Donations
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80gDonations}
                onChange={(e) => handleInputChange('sec80gDonations', parseFloat(e.target.value) || 0)}
              />
              <p className="text-xs text-gray-500 mt-1">50% deduction allowed</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Section 80TTA - Savings Interest <span className="text-gray-500 text-xs">(Max {formatINR(TAX_LIMITS.MAX_80TTA)})</span>
              </label>
              <input
                type="number"
                step="0.01"
                className={`w-full rounded-md border px-3 py-2 ${
                  errors.sec80ttaSavingsInterest ? 'border-red-500' : 'border-gray-300'
                }`}
                value={formData.sec80ttaSavingsInterest}
                onChange={(e) => handleInputChange('sec80ttaSavingsInterest', parseFloat(e.target.value) || 0)}
              />
              {errors.sec80ttaSavingsInterest && (
                <p className="text-red-500 text-xs mt-1">{errors.sec80ttaSavingsInterest}</p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Previous Employer & Other Income */}
      <div className="border rounded-lg p-4">
        <h3 className="font-semibold mb-3">Previous Employer & Other Income</h3>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Previous Employer Income
            </label>
            <input
              type="number"
              step="0.01"
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.prevEmployerIncome}
              onChange={(e) => handleInputChange('prevEmployerIncome', parseFloat(e.target.value) || 0)}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Previous Employer TDS
            </label>
            <input
              type="number"
              step="0.01"
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.prevEmployerTds}
              onChange={(e) => handleInputChange('prevEmployerTds', parseFloat(e.target.value) || 0)}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Previous Employer PF
            </label>
            <input
              type="number"
              step="0.01"
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.prevEmployerPf}
              onChange={(e) => handleInputChange('prevEmployerPf', parseFloat(e.target.value) || 0)}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Previous Employer PT
            </label>
            <input
              type="number"
              step="0.01"
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.prevEmployerPt}
              onChange={(e) => handleInputChange('prevEmployerPt', parseFloat(e.target.value) || 0)}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Other Income (Annual)
            </label>
            <input
              type="number"
              step="0.01"
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.otherIncomeAnnual}
              onChange={(e) => handleInputChange('otherIncomeAnnual', parseFloat(e.target.value) || 0)}
            />
          </div>
        </div>
      </div>

      {/* Column 388A - Other TDS/TCS Credits (CBDT Feb 2025) */}
      <div className="border rounded-lg p-4 bg-blue-50">
        <h3 className="font-semibold mb-1">Column 388A - Other TDS/TCS Credits</h3>
        <p className="text-xs text-gray-600 mb-3">
          Declare TDS/TCS from other sources to adjust against salary TDS (per CBDT Circular Feb 2025)
        </p>

        {/* Other TDS Credits */}
        <div className="mb-4">
          <h4 className="text-sm font-medium text-gray-700 mb-2">TDS from Other Sources</h4>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Interest Income (FD/RD) - Sec 194A
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.otherTdsInterest}
                onChange={(e) => handleInputChange('otherTdsInterest', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Dividend Income - Sec 194
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.otherTdsDividend}
                onChange={(e) => handleInputChange('otherTdsDividend', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Commission/Brokerage - Sec 194H
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.otherTdsCommission}
                onChange={(e) => handleInputChange('otherTdsCommission', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Rental Income - Sec 194I
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.otherTdsRent}
                onChange={(e) => handleInputChange('otherTdsRent', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Professional Fees - Sec 194J
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.otherTdsProfessional}
                onChange={(e) => handleInputChange('otherTdsProfessional', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Other TDS
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.otherTdsOthers}
                onChange={(e) => handleInputChange('otherTdsOthers', parseFloat(e.target.value) || 0)}
              />
            </div>
          </div>
        </div>

        {/* TCS Credits */}
        <div className="mb-4">
          <h4 className="text-sm font-medium text-gray-700 mb-2">TCS Credits</h4>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Foreign Remittance (LRS) - Sec 206C(1G)
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.tcsForeignRemittance}
                onChange={(e) => handleInputChange('tcsForeignRemittance', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Overseas Tour Package - Sec 206C(1G)
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.tcsOverseasTour}
                onChange={(e) => handleInputChange('tcsOverseasTour', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Vehicle Purchase ({'>'}10L) - Sec 206C(1F)
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.tcsVehiclePurchase}
                onChange={(e) => handleInputChange('tcsVehiclePurchase', parseFloat(e.target.value) || 0)}
              />
            </div>
            <div>
              <label className="block text-sm text-gray-600 mb-1">
                Other TCS
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.tcsOthers}
                onChange={(e) => handleInputChange('tcsOthers', parseFloat(e.target.value) || 0)}
              />
            </div>
          </div>
        </div>

        {/* 388A Summary */}
        <div className="mt-3 p-2 bg-white rounded border">
          <div className="flex justify-between text-sm">
            <span>Total Other TDS:</span>
            <span className="font-medium">
              {formatINR(
                (formData.otherTdsInterest || 0) +
                (formData.otherTdsDividend || 0) +
                (formData.otherTdsCommission || 0) +
                (formData.otherTdsRent || 0) +
                (formData.otherTdsProfessional || 0) +
                (formData.otherTdsOthers || 0)
              )}
            </span>
          </div>
          <div className="flex justify-between text-sm">
            <span>Total TCS:</span>
            <span className="font-medium">
              {formatINR(
                (formData.tcsForeignRemittance || 0) +
                (formData.tcsOverseasTour || 0) +
                (formData.tcsVehiclePurchase || 0) +
                (formData.tcsOthers || 0)
              )}
            </span>
          </div>
          <div className="flex justify-between text-sm font-semibold border-t pt-1 mt-1">
            <span>Total Column 388A Credit:</span>
            <span className="text-blue-600">
              {formatINR(
                (formData.otherTdsInterest || 0) +
                (formData.otherTdsDividend || 0) +
                (formData.otherTdsCommission || 0) +
                (formData.otherTdsRent || 0) +
                (formData.otherTdsProfessional || 0) +
                (formData.otherTdsOthers || 0) +
                (formData.tcsForeignRemittance || 0) +
                (formData.tcsOverseasTour || 0) +
                (formData.tcsVehiclePurchase || 0) +
                (formData.tcsOthers || 0)
              )}
            </span>
          </div>
        </div>
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
          {isLoading ? 'Saving...' : isRevision ? 'Revise & Resubmit' : isEditing ? 'Update' : 'Create'} Declaration
        </button>
      </div>
    </form>
  )
}

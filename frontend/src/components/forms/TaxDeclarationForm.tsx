import { useState, useEffect, useMemo } from 'react'
import {
  useCreateTaxDeclaration,
  useUpdateTaxDeclaration,
  usePayrollInfoByType,
} from '@/features/payroll/hooks'
import type {
  EmployeeTaxDeclaration,
  CreateEmployeeTaxDeclarationDto,
  UpdateEmployeeTaxDeclarationDto,
} from '@/features/payroll/types/payroll'
import { useEmployees } from '@/hooks/api/useEmployees'
import { useCompanies } from '@/hooks/api/useCompanies'
import { formatINR } from '@/lib/currency'

interface TaxDeclarationFormProps {
  declaration?: EmployeeTaxDeclaration
  onSuccess: () => void
  onCancel: () => void
}

export const TaxDeclarationForm = ({
  declaration,
  onSuccess,
  onCancel,
}: TaxDeclarationFormProps) => {
  const { data: allEmployees = [] } = useEmployees()
  const { data: companies = [] } = useCompanies()
  const { data: employeePayrollInfo = [] } = usePayrollInfoByType('employee')

  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')

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
    // Previous Employer
    prevEmployerIncome: 0,
    prevEmployerTds: 0,
    prevEmployerPf: 0,
    prevEmployerPt: 0,
  })

  const [errors, setErrors] = useState<Record<string, string>>({})
  const createDeclaration = useCreateTaxDeclaration()
  const updateDeclaration = useUpdateTaxDeclaration()

  const isEditing = !!declaration
  const isLoading = createDeclaration.isPending || updateDeclaration.isPending

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
    if (declaration && !selectedCompanyId) {
      const employee = allEmployees.find(e => e.id === declaration.employeeId)
      if (employee?.companyId) {
        setSelectedCompanyId(employee.companyId)
      }
    }
  }, [declaration, allEmployees, selectedCompanyId])

  // Calculate totals
  const total80c = useMemo(() => {
    return (
      formData.sec80cPpf +
      formData.sec80cElss +
      formData.sec80cLifeInsurance +
      formData.sec80cHomeLoanPrincipal +
      formData.sec80cChildrenTuition +
      formData.sec80cNsc +
      formData.sec80cSukanyaSamriddhi +
      formData.sec80cFixedDeposit +
      formData.sec80cOthers
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
  const allowed80ccd = Math.min(formData.sec80ccdNps, 50000)

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
        prevEmployerIncome: declaration.prevEmployerIncome,
        prevEmployerTds: declaration.prevEmployerTds,
        prevEmployerPf: declaration.prevEmployerPf,
        prevEmployerPt: declaration.prevEmployerPt,
      })
    }
  }, [declaration])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.employeeId) newErrors.employeeId = 'Employee is required'
    if (!formData.financialYear) newErrors.financialYear = 'Financial year is required'
    if (formData.hraRentPaidAnnual > 100000 && !formData.hraLandlordPan) {
      newErrors.hraLandlordPan = 'Landlord PAN is required if rent > ₹1,00,000/year'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && declaration) {
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
          <select
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={selectedCompanyId}
            onChange={(e) => {
              setSelectedCompanyId(e.target.value)
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
          <p className="text-xs text-gray-500 mt-1">
            Filter employees by company (only full-time employees shown)
          </p>
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
                {selectedCompanyId 
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
          <div className="mt-3 p-2 bg-gray-50 rounded">
            <div className="flex justify-between text-sm">
              <span>Total 80C:</span>
              <span className="font-medium">{formatINR(total80c)}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span>Allowed (capped at ₹1,50,000):</span>
              <span className="font-semibold">{formatINR(allowed80c)}</span>
            </div>
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
              <label className="block text-sm font-medium text-gray-700 mb-1">Self/Family</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
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
                  Senior Citizen (limit ₹50,000)
                </label>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Parents</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
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
                  Senior Citizen (limit ₹50,000)
                </label>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Preventive Checkup</label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80dPreventiveCheckup}
                onChange={(e) => handleInputChange('sec80dPreventiveCheckup', parseFloat(e.target.value) || 0)}
              />
              <p className="text-xs text-gray-500 mt-1">Max ₹5,000</p>
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
            {formData.hraRentPaidAnnual > 100000 && (
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
                Section 24 - Home Loan Interest
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec24HomeLoanInterest}
                onChange={(e) => handleInputChange('sec24HomeLoanInterest', parseFloat(e.target.value) || 0)}
              />
              <p className="text-xs text-gray-500 mt-1">Max ₹2,00,000</p>
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
                Section 80TTA - Savings Interest
              </label>
              <input
                type="number"
                step="0.01"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.sec80ttaSavingsInterest}
                onChange={(e) => handleInputChange('sec80ttaSavingsInterest', parseFloat(e.target.value) || 0)}
              />
              <p className="text-xs text-gray-500 mt-1">Max ₹10,000</p>
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
          {isLoading ? 'Saving...' : isEditing ? 'Update' : 'Create'} Declaration
        </button>
      </div>
    </form>
  )
}




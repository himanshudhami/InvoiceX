import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useProcessPayroll, usePayrollPreview } from '@/features/payroll/hooks'
import { ArrowLeft, ArrowRight, CheckCircle, AlertTriangle } from 'lucide-react'
import { formatINR } from '@/lib/currency'

type Step = 1 | 2 | 3 | 4 | 5

const PayrollProcess = () => {
  const navigate = useNavigate()
  const [currentStep, setCurrentStep] = useState<Step>(1)
  const [urlState, setUrlState] = useQueryStates(
    {
      companyId: parseAsString,
      payrollMonth: parseAsInteger.withDefault(new Date().getMonth() + 1),
      payrollYear: parseAsInteger.withDefault(new Date().getFullYear()),
      includeContractors: parseAsString.withDefault('true'),
    },
    { history: 'push' }
  )
  
  const [formData, setFormData] = useState({
    companyId: urlState.companyId || '',
    payrollMonth: urlState.payrollMonth,
    payrollYear: urlState.payrollYear,
    includeContractors: urlState.includeContractors === 'true',
    processedBy: 'current-user', // TODO: Get from auth context
  })

  const { data: companies = [] } = useCompanies()
  const processPayroll = useProcessPayroll()

  // Fetch preview when on step 3
  const { data: preview, isLoading: previewLoading } = usePayrollPreview(
    {
      companyId: formData.companyId,
      payrollMonth: formData.payrollMonth,
      payrollYear: formData.payrollYear,
      includeContractors: formData.includeContractors,
    },
    currentStep === 3 && !!formData.companyId
  )

  // Sync formData with URL state when URL changes
  useEffect(() => {
    setFormData({
      companyId: urlState.companyId || '',
      payrollMonth: urlState.payrollMonth,
      payrollYear: urlState.payrollYear,
      includeContractors: urlState.includeContractors === 'true',
      processedBy: 'current-user',
    })
  }, [urlState.companyId, urlState.payrollMonth, urlState.payrollYear, urlState.includeContractors])

  const handleNext = () => {
    if (currentStep < 5) {
      setCurrentStep((prev) => (prev + 1) as Step)
    }
  }

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep((prev) => (prev - 1) as Step)
    }
  }

  const handleProcess = async () => {
    try {
      const result = await processPayroll.mutateAsync({
        companyId: formData.companyId,
        payrollMonth: formData.payrollMonth,
        payrollYear: formData.payrollYear,
        includeContractors: formData.includeContractors,
        processedBy: formData.processedBy,
      })
      navigate(`/payroll/runs/${result.payrollRunId}`)
    } catch (error) {
      console.error('Failed to process payroll:', error)
    }
  }

  const getMonthName = (month: number) => {
    const months = [
      'January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December'
    ]
    return months[month - 1] || ''
  }

  const getFinancialYear = (month: number, year: number) => {
    if (month >= 4) {
      return `${year}-${(year + 1) % 100}`
    } else {
      return `${year - 1}-${year % 100}`
    }
  }

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      {/* Header */}
      <div>
        <Button variant="ghost" onClick={() => navigate('/payroll')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Dashboard
        </Button>
        <h1 className="text-3xl font-bold text-gray-900 mt-4">Process Payroll</h1>
        <p className="text-gray-600 mt-2">Run monthly payroll for your employees</p>
      </div>

      {/* Progress Steps */}
      <div className="flex items-center justify-between mb-8">
        {[1, 2, 3, 4, 5].map((step) => (
          <div key={step} className="flex items-center flex-1">
            <div className="flex flex-col items-center">
              <div
                className={`w-10 h-10 rounded-full flex items-center justify-center ${
                  step <= currentStep
                    ? 'bg-blue-600 text-white'
                    : 'bg-gray-200 text-gray-600'
                }`}
              >
                {step < currentStep ? <CheckCircle className="w-6 h-6" /> : step}
              </div>
              <div className="text-xs mt-2 text-center">
                {step === 1 && 'Select Period'}
                {step === 2 && 'Review'}
                {step === 3 && 'Preview'}
                {step === 4 && 'Process'}
                {step === 5 && 'Complete'}
              </div>
            </div>
            {step < 5 && (
              <div
                className={`flex-1 h-1 mx-2 ${
                  step < currentStep ? 'bg-blue-600' : 'bg-gray-200'
                }`}
              />
            )}
          </div>
        ))}
      </div>

      {/* Step Content */}
      <Card>
        <CardHeader>
          <CardTitle>
            {currentStep === 1 && 'Step 1: Select Company and Period'}
            {currentStep === 2 && 'Step 2: Review Selection'}
            {currentStep === 3 && 'Step 3: Preview Calculation'}
            {currentStep === 4 && 'Step 4: Process Payroll'}
            {currentStep === 5 && 'Step 5: Complete'}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Step 1: Select Period */}
          {currentStep === 1 && (
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Company
                </label>
                <CompanyFilterDropdown
                  value={formData.companyId}
                  onChange={(value) => {
                    setFormData({ ...formData, companyId: value })
                    setUrlState({ companyId: value || null })
                  }}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Month
                  </label>
                  <select
                    className="w-full rounded-md border border-gray-300 px-3 py-2"
                    value={formData.payrollMonth}
                    onChange={(e) => {
                      const month = parseInt(e.target.value)
                      setFormData({ ...formData, payrollMonth: month })
                      setUrlState({ payrollMonth: month })
                    }}
                  >
                    {Array.from({ length: 12 }, (_, i) => i + 1).map((month) => (
                      <option key={month} value={month}>
                        {getMonthName(month)}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Year
                  </label>
                  <input
                    type="number"
                    className="w-full rounded-md border border-gray-300 px-3 py-2"
                    value={formData.payrollYear}
                    onChange={(e) => {
                      const year = parseInt(e.target.value)
                      setFormData({ ...formData, payrollYear: year })
                      setUrlState({ payrollYear: year })
                    }}
                    min={2020}
                    max={2100}
                  />
                </div>
              </div>

              <div>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={formData.includeContractors}
                    onChange={(e) => {
                      const include = e.target.checked
                      setFormData({ ...formData, includeContractors: include })
                      setUrlState({ includeContractors: include ? 'true' : 'false' })
                    }}
                  />
                  <span className="text-sm text-gray-700">
                    Include contractors in payroll run
                  </span>
                </label>
                <p className="text-xs text-gray-500 mt-1 ml-6">
                  Note: Contractors are typically processed separately via Contractor Payments page
                </p>
              </div>
            </div>
          )}

          {/* Step 2: Review */}
          {currentStep === 2 && (
            <div className="space-y-4">
              <div className="bg-gray-50 p-4 rounded-lg space-y-2">
                <div className="flex justify-between">
                  <span className="text-gray-600">Company:</span>
                  <span className="font-medium">
                    {companies.find((c) => c.id === formData.companyId)?.name || '—'}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Period:</span>
                  <span className="font-medium">
                    {getMonthName(formData.payrollMonth)} {formData.payrollYear}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Financial Year:</span>
                  <span className="font-medium">
                    {getFinancialYear(formData.payrollMonth, formData.payrollYear)}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Include Contractors:</span>
                  <span className="font-medium">
                    {formData.includeContractors ? 'Yes' : 'No'}
                  </span>
                </div>
              </div>

              <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                <p className="text-sm text-yellow-800">
                  <strong>Note:</strong> Processing payroll will calculate salaries for all active
                  employees with salary structures. Make sure all tax declarations are up to date.
                </p>
              </div>
            </div>
          )}

          {/* Step 3: Preview */}
          {currentStep === 3 && (
            <div className="space-y-4">
              {previewLoading ? (
                <div className="text-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
                  <p className="text-sm text-gray-600 mt-2">Loading preview...</p>
                </div>
              ) : preview ? (
                <>
                  {/* Summary Cards */}
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    <div className="bg-blue-50 p-4 rounded-lg">
                      <p className="text-sm text-blue-600 font-medium">Employees</p>
                      <p className="text-2xl font-bold text-blue-700">{preview.employeeCount}</p>
                    </div>
                    <div className="bg-green-50 p-4 rounded-lg">
                      <p className="text-sm text-green-600 font-medium">Total Gross</p>
                      <p className="text-2xl font-bold text-green-700">{formatINR(preview.totalMonthlyGross)}</p>
                    </div>
                    <div className="bg-red-50 p-4 rounded-lg">
                      <p className="text-sm text-red-600 font-medium">Total Deductions</p>
                      <p className="text-2xl font-bold text-red-700">{formatINR(preview.totalDeductions)}</p>
                    </div>
                    <div className="bg-purple-50 p-4 rounded-lg">
                      <p className="text-sm text-purple-600 font-medium">Net Pay</p>
                      <p className="text-2xl font-bold text-purple-700">{formatINR(preview.totalNetPay)}</p>
                    </div>
                  </div>

                  {/* Detailed Breakdown */}
                  <div className="bg-gray-50 p-4 rounded-lg space-y-2">
                    <h4 className="font-medium text-gray-700 mb-3">Deductions Breakdown (Estimated)</h4>
                    <div className="grid grid-cols-2 gap-2 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-600">PF (Employee):</span>
                        <span className="font-medium">{formatINR(preview.totalPfEmployee)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">PF (Employer):</span>
                        <span className="font-medium">{formatINR(preview.totalPfEmployer)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">ESI (Employee):</span>
                        <span className="font-medium">{formatINR(preview.totalEsiEmployee)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">ESI (Employer):</span>
                        <span className="font-medium">{formatINR(preview.totalEsiEmployer)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">Professional Tax:</span>
                        <span className="font-medium">{formatINR(preview.totalPt)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">TDS:</span>
                        <span className="font-medium">{formatINR(preview.totalTds)}</span>
                      </div>
                    </div>
                  </div>

                  {/* Warning for employees without salary structure */}
                  {preview.employeesWithoutStructure.length > 0 && (
                    <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                      <div className="flex items-start gap-2">
                        <AlertTriangle className="w-5 h-5 text-yellow-600 mt-0.5" />
                        <div>
                          <p className="text-sm font-medium text-yellow-800">
                            {preview.employeesWithoutStructure.length} employee(s) will be skipped
                          </p>
                          <p className="text-sm text-yellow-700 mt-1">
                            The following employees don't have a salary structure defined:
                          </p>
                          <ul className="text-sm text-yellow-700 mt-2 list-disc list-inside">
                            {preview.employeesWithoutStructure.slice(0, 5).map((name, idx) => (
                              <li key={idx}>{name}</li>
                            ))}
                            {preview.employeesWithoutStructure.length > 5 && (
                              <li>...and {preview.employeesWithoutStructure.length - 5} more</li>
                            )}
                          </ul>
                        </div>
                      </div>
                    </div>
                  )}

                  <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                    <p className="text-sm text-blue-800">
                      <strong>Note:</strong> These are estimated values. Actual TDS will be calculated based on individual tax declarations during processing.
                    </p>
                  </div>
                </>
              ) : (
                <div className="text-center py-8 text-gray-500">
                  <p>No preview data available. Please ensure a company is selected.</p>
                </div>
              )}
            </div>
          )}

          {/* Step 4: Process */}
          {currentStep === 4 && (
            <div className="space-y-4">
              <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-sm text-red-800 font-medium mb-2">
                  Ready to process payroll
                </p>
                <p className="text-sm text-red-700">
                  Click the button below to process payroll. This will calculate salaries for all
                  employees and create payroll transactions.
                </p>
              </div>

              {processPayroll.isPending && (
                <div className="text-center py-4">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
                  <p className="text-sm text-gray-600 mt-2">Processing payroll...</p>
                </div>
              )}
            </div>
          )}

          {/* Step 5: Complete */}
          {currentStep === 5 && (
            <div className="text-center space-y-4">
              <CheckCircle className="w-16 h-16 text-green-600 mx-auto" />
              <h3 className="text-xl font-semibold">Payroll Processed Successfully!</h3>
              <p className="text-gray-600">
                The payroll has been processed and transactions have been created.
              </p>
              <Button onClick={() => navigate('/payroll/runs')}>
                View Payroll Runs
              </Button>
            </div>
          )}

          {/* Navigation Buttons */}
          {currentStep < 5 && (
            <div className="flex justify-between pt-4 border-t">
              <Button
                variant="outline"
                onClick={handleBack}
                disabled={currentStep === 1}
              >
                <ArrowLeft className="w-4 h-4 mr-2" />
                Back
              </Button>

              {currentStep < 4 ? (
                <Button
                  onClick={handleNext}
                  disabled={!formData.companyId}
                >
                  Next
                  <ArrowRight className="w-4 h-4 ml-2" />
                </Button>
              ) : (
                <Button
                  onClick={handleProcess}
                  disabled={processPayroll.isPending || !formData.companyId}
                >
                  {processPayroll.isPending ? 'Processing...' : 'Process Payroll'}
                </Button>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

export default PayrollProcess




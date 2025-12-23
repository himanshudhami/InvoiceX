import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryState, parseAsString } from 'nuqs'
import { useQuery } from '@tanstack/react-query'
import { useCompanyStatutoryConfig, useUpdateStatutoryConfig, useCreateStatutoryConfig } from '@/features/payroll/hooks'
import { taxRulePackService } from '@/services/api/finance/tax/taxRulePackService'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { ArrowLeft, Info, ExternalLink } from 'lucide-react'

import type { PfCalculationMode, PfTrustType } from '@/features/payroll/types/payroll'

// Default form values
const defaultFormData = {
  pfEnabled: true,
  pfEstablishmentCode: '',
  pfEmployeeRate: 12.0,
  pfEmployerRate: 12.0,
  pfWageCeiling: 15000,
  pfCalculationMode: 'ceiling_based' as PfCalculationMode,
  pfTrustType: 'epfo' as PfTrustType,
  pfTrustName: '',
  pfTrustRegistrationNumber: '',
  restrictedPfMaxWage: 15000,
  esiEnabled: false,
  esiCode: '',
  esiEmployeeRate: 0.75,
  esiEmployerRate: 3.25,
  esiWageCeiling: 21000,
  ptEnabled: true,
  ptState: 'Karnataka',
  ptRegistrationNumber: '',
  lwfEnabled: false,
  lwfEmployeeAmount: 0,
  lwfEmployerAmount: 0,
  gratuityEnabled: false,
  gratuityRate: 4.81,
}

const PayrollSettings = () => {
  const navigate = useNavigate()
  const [selectedCompanyId, setSelectedCompanyId] = useQueryState('company', parseAsString.withDefault(''))
  const { data: config, isLoading } = useCompanyStatutoryConfig(selectedCompanyId, !!selectedCompanyId)
  const updateConfig = useUpdateStatutoryConfig()
  const createConfig = useCreateStatutoryConfig()

  // Fetch statutory rates from Tax Rule Packs (read-only government rates)
  const { data: pfEsiRates } = useQuery({
    queryKey: ['pf-esi-rates', '2025-26'],
    queryFn: () => taxRulePackService.getPfEsiRates('2025-26'),
  })

  const [formData, setFormData] = useState(defaultFormData)

  // Update form data when config loads or reset to defaults when switching companies
  useEffect(() => {
    if (config) {
      setFormData({
        pfEnabled: config.pfEnabled,
        pfEstablishmentCode: config.pfEstablishmentCode || '',
        pfEmployeeRate: config.pfEmployeeRate,
        pfEmployerRate: config.pfEmployerRate,
        pfWageCeiling: config.pfWageCeiling,
        pfCalculationMode: config.pfCalculationMode || 'ceiling_based',
        pfTrustType: config.pfTrustType || 'epfo',
        pfTrustName: config.pfTrustName || '',
        pfTrustRegistrationNumber: config.pfTrustRegistrationNumber || '',
        restrictedPfMaxWage: config.restrictedPfMaxWage || 15000,
        esiEnabled: config.esiEnabled,
        esiCode: config.esiCode || '',
        esiEmployeeRate: config.esiEmployeeRate,
        esiEmployerRate: config.esiEmployerRate,
        esiWageCeiling: config.esiWageCeiling,
        ptEnabled: config.ptEnabled,
        ptState: config.ptState || 'Karnataka',
        ptRegistrationNumber: config.ptRegistrationNumber || '',
        lwfEnabled: config.lwfEnabled,
        lwfEmployeeAmount: config.lwfEmployeeAmount,
        lwfEmployerAmount: config.lwfEmployerAmount,
        gratuityEnabled: config.gratuityEnabled ?? false,
        gratuityRate: config.gratuityRate ?? 4.81,
      })
    } else if (selectedCompanyId && !isLoading) {
      // Reset to defaults when company changes and no config exists
      setFormData(defaultFormData)
    }
  }, [config, selectedCompanyId, isLoading])

  const handleSave = async () => {
    if (!selectedCompanyId) {
      alert('Please select a company')
      return
    }

    try {
      if (config) {
        await updateConfig.mutateAsync({
          id: config.id,
          data: formData,
        })
      } else {
        await createConfig.mutateAsync({
          companyId: selectedCompanyId,
          ...formData,
        })
      }
    } catch (error) {
      console.error('Failed to save statutory config:', error)
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 bg-gray-200 rounded animate-pulse w-1/4"></div>
        <div className="h-64 bg-gray-200 rounded animate-pulse"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6 max-w-4xl">
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate('/payroll')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Dashboard
        </Button>
      </div>
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Company Statutory Settings</h1>
        <p className="text-gray-600 mt-1">Configure company-specific choices for PF, ESI, PT, and Gratuity</p>
      </div>

      {/* Architecture Info Banner */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <Info className="h-5 w-5 text-blue-600 mt-0.5 flex-shrink-0" />
          <div className="text-sm text-blue-800">
            <p className="font-medium mb-1">Configuration Hierarchy</p>
            <ul className="list-disc list-inside space-y-1 text-blue-700">
              <li><strong>Government Rates</strong> (PF 12%, ESI 0.75%/3.25%) are set in <a href="/tax-rule-packs" className="underline hover:text-blue-900 inline-flex items-center gap-1">Tax Rule Packs<ExternalLink className="h-3 w-3" /></a></li>
              <li><strong>Company Settings</strong> (this page) control which components are enabled and company-specific choices</li>
              <li><strong>Custom Formulas</strong> for edge cases can be configured in <a href="/payroll/calculation-rules" className="underline hover:text-blue-900 inline-flex items-center gap-1">Calculation Rules<ExternalLink className="h-3 w-3" /></a></li>
            </ul>
          </div>
        </div>
      </div>

      <div className="flex gap-3 mb-6">
        <CompanyFilterDropdown
          value={selectedCompanyId}
          onChange={setSelectedCompanyId}
        />
      </div>

      {!selectedCompanyId ? (
        <Card>
          <CardContent className="p-6 text-center text-gray-500">
            Please select a company to view or configure statutory settings
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-6">
          {/* PF Configuration */}
          <Card>
            <CardHeader>
              <CardTitle>Provident Fund (PF) Configuration</CardTitle>
              <CardDescription>Company choices for PF - statutory rates are set by government</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="pfEnabled"
                  checked={formData.pfEnabled}
                  onChange={(e) => setFormData({ ...formData, pfEnabled: e.target.checked })}
                />
                <label htmlFor="pfEnabled" className="text-sm font-medium">
                  Enable PF for this company
                </label>
              </div>

              {formData.pfEnabled && (
                <>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      PF Establishment Code
                    </label>
                    <input
                      type="text"
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      value={formData.pfEstablishmentCode}
                      onChange={(e) => setFormData({ ...formData, pfEstablishmentCode: e.target.value })}
                      placeholder="PF registration number"
                    />
                  </div>

                  {/* Statutory Rates from Tax Rule Packs (Read-Only) */}
                  <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
                    <div className="flex items-center justify-between mb-3">
                      <span className="text-sm font-medium text-gray-700">Government Statutory Rates (FY 2025-26)</span>
                      <a href="/tax-rule-packs" className="text-xs text-blue-600 hover:text-blue-800 flex items-center gap-1">
                        View in Tax Rule Packs <ExternalLink className="h-3 w-3" />
                      </a>
                    </div>
                    <div className="grid grid-cols-3 gap-4 text-sm">
                      <div>
                        <span className="text-gray-500">Employee Rate</span>
                        <div className="font-semibold text-gray-900">{pfEsiRates?.pf?.employee_contribution || 12}%</div>
                      </div>
                      <div>
                        <span className="text-gray-500">Employer Rate</span>
                        <div className="font-semibold text-gray-900">{pfEsiRates?.pf?.employer_contribution || 12}%</div>
                      </div>
                      <div>
                        <span className="text-gray-500">Wage Ceiling</span>
                        <div className="font-semibold text-gray-900">₹{(pfEsiRates?.pf?.wage_ceiling || 15000).toLocaleString('en-IN')}</div>
                      </div>
                    </div>
                  </div>

                  {/* PF Calculation Mode */}
                  <div className="pt-4 border-t">
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      PF Calculation Mode
                    </label>
                    <div className="space-y-2">
                      <label className="flex items-start gap-2 cursor-pointer">
                        <input
                          type="radio"
                          name="pfCalculationMode"
                          value="ceiling_based"
                          checked={formData.pfCalculationMode === 'ceiling_based'}
                          onChange={(e) => setFormData({ ...formData, pfCalculationMode: e.target.value as PfCalculationMode })}
                          className="mt-1"
                        />
                        <div>
                          <span className="font-medium">Ceiling Based</span>
                          <p className="text-xs text-gray-500">
                            12% of PF wage capped at ceiling (₹{formData.pfWageCeiling.toLocaleString()}) = Max ₹{Math.round(formData.pfWageCeiling * 0.12).toLocaleString()}/month
                          </p>
                        </div>
                      </label>
                      <label className="flex items-start gap-2 cursor-pointer">
                        <input
                          type="radio"
                          name="pfCalculationMode"
                          value="actual_wage"
                          checked={formData.pfCalculationMode === 'actual_wage'}
                          onChange={(e) => setFormData({ ...formData, pfCalculationMode: e.target.value as PfCalculationMode })}
                          className="mt-1"
                        />
                        <div>
                          <span className="font-medium">Actual Wage (No Ceiling)</span>
                          <p className="text-xs text-gray-500">
                            12% of actual basic salary without any ceiling - higher contribution for employees
                          </p>
                        </div>
                      </label>
                      <label className="flex items-start gap-2 cursor-pointer">
                        <input
                          type="radio"
                          name="pfCalculationMode"
                          value="restricted_pf"
                          checked={formData.pfCalculationMode === 'restricted_pf'}
                          onChange={(e) => setFormData({ ...formData, pfCalculationMode: e.target.value as PfCalculationMode })}
                          className="mt-1"
                        />
                        <div>
                          <span className="font-medium">Restricted PF (Employee Choice)</span>
                          <p className="text-xs text-gray-500">
                            Employees earning above ceiling can opt to contribute on statutory minimum
                          </p>
                        </div>
                      </label>
                    </div>

                    {formData.pfCalculationMode === 'restricted_pf' && (
                      <div className="mt-3 p-3 bg-gray-50 rounded-md">
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                          Restricted PF Max Wage
                        </label>
                        <input
                          type="number"
                          className="w-full rounded-md border border-gray-300 px-3 py-2"
                          value={formData.restrictedPfMaxWage}
                          onChange={(e) => setFormData({ ...formData, restrictedPfMaxWage: parseFloat(e.target.value) })}
                        />
                        <p className="text-xs text-gray-500 mt-1">
                          Employees who opt for restricted PF will have PF calculated on this maximum
                        </p>
                      </div>
                    )}
                  </div>

                  {/* PF Trust Type */}
                  <div className="pt-4 border-t">
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      PF Trust Type
                    </label>
                    <select
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      value={formData.pfTrustType}
                      onChange={(e) => setFormData({ ...formData, pfTrustType: e.target.value as PfTrustType })}
                    >
                      <option value="epfo">EPFO (Government Provident Fund)</option>
                      <option value="private_trust">Private PF Trust</option>
                    </select>
                    <p className="text-xs text-gray-500 mt-1">
                      Most companies use EPFO. Large companies (500+ employees) may have a private PF trust.
                    </p>

                    {formData.pfTrustType === 'private_trust' && (
                      <div className="mt-3 space-y-3 p-3 bg-gray-50 rounded-md">
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Trust Name
                          </label>
                          <input
                            type="text"
                            className="w-full rounded-md border border-gray-300 px-3 py-2"
                            value={formData.pfTrustName}
                            onChange={(e) => setFormData({ ...formData, pfTrustName: e.target.value })}
                            placeholder="Enter private trust name"
                          />
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Trust Registration Number
                          </label>
                          <input
                            type="text"
                            className="w-full rounded-md border border-gray-300 px-3 py-2"
                            value={formData.pfTrustRegistrationNumber}
                            onChange={(e) => setFormData({ ...formData, pfTrustRegistrationNumber: e.target.value })}
                            placeholder="Enter trust registration number"
                          />
                        </div>
                      </div>
                    )}
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* ESI Configuration */}
          <Card>
            <CardHeader>
              <CardTitle>Employee State Insurance (ESI) Configuration</CardTitle>
              <CardDescription>Company choices for ESI - statutory rates are set by government</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="esiEnabled"
                  checked={formData.esiEnabled}
                  onChange={(e) => setFormData({ ...formData, esiEnabled: e.target.checked })}
                />
                <label htmlFor="esiEnabled" className="text-sm font-medium">
                  Enable ESI for this company
                </label>
              </div>

              {formData.esiEnabled && (
                <>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      ESI Code
                    </label>
                    <input
                      type="text"
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      value={formData.esiCode}
                      onChange={(e) => setFormData({ ...formData, esiCode: e.target.value })}
                      placeholder="ESI registration number"
                    />
                  </div>

                  {/* Statutory Rates from Tax Rule Packs (Read-Only) */}
                  <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
                    <div className="flex items-center justify-between mb-3">
                      <span className="text-sm font-medium text-gray-700">Government Statutory Rates (FY 2025-26)</span>
                      <a href="/tax-rule-packs" className="text-xs text-blue-600 hover:text-blue-800 flex items-center gap-1">
                        View in Tax Rule Packs <ExternalLink className="h-3 w-3" />
                      </a>
                    </div>
                    <div className="grid grid-cols-3 gap-4 text-sm">
                      <div>
                        <span className="text-gray-500">Employee Rate</span>
                        <div className="font-semibold text-gray-900">{pfEsiRates?.esi?.employee_contribution || 0.75}%</div>
                      </div>
                      <div>
                        <span className="text-gray-500">Employer Rate</span>
                        <div className="font-semibold text-gray-900">{pfEsiRates?.esi?.employer_contribution || 3.25}%</div>
                      </div>
                      <div>
                        <span className="text-gray-500">Wage Ceiling</span>
                        <div className="font-semibold text-gray-900">₹{(pfEsiRates?.esi?.wage_ceiling || 21000).toLocaleString('en-IN')}</div>
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mt-2">
                      ESI is applicable only if employee's monthly gross salary is ≤ ₹{(pfEsiRates?.esi?.wage_ceiling || 21000).toLocaleString('en-IN')}
                    </p>
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* PT Configuration */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>Professional Tax (PT) Configuration</CardTitle>
                  <CardDescription>State-wise professional tax settings</CardDescription>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => navigate('/payroll/settings/pt-slabs')}
                >
                  Manage PT Slabs
                </Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="ptEnabled"
                  checked={formData.ptEnabled}
                  onChange={(e) => setFormData({ ...formData, ptEnabled: e.target.checked })}
                />
                <label htmlFor="ptEnabled" className="text-sm font-medium">
                  Enable Professional Tax
                </label>
              </div>

              {formData.ptEnabled && (
                <>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      State
                    </label>
                    <select
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      value={formData.ptState}
                      onChange={(e) => setFormData({ ...formData, ptState: e.target.value })}
                    >
                      <option value="Karnataka">Karnataka</option>
                      <option value="Maharashtra">Maharashtra</option>
                      <option value="Tamil Nadu">Tamil Nadu</option>
                      <option value="Gujarat">Gujarat</option>
                      <option value="Delhi">Delhi</option>
                      <option value="West Bengal">West Bengal</option>
                      <option value="Andhra Pradesh">Andhra Pradesh</option>
                      <option value="Telangana">Telangana</option>
                      <option value="Kerala">Kerala</option>
                      <option value="Madhya Pradesh">Madhya Pradesh</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      PT Registration Number
                    </label>
                    <input
                      type="text"
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      value={formData.ptRegistrationNumber}
                      onChange={(e) => setFormData({ ...formData, ptRegistrationNumber: e.target.value })}
                      placeholder="PT registration number"
                    />
                  </div>

                  <div className="bg-blue-50 p-3 rounded-md text-sm text-blue-700">
                    <p>
                      <strong>Note:</strong> PT slabs are configured globally for all states. Click "Manage PT Slabs" to view or modify the income slabs and tax amounts for each state.
                    </p>
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* LWF Configuration */}
          <Card>
            <CardHeader>
              <CardTitle>Labour Welfare Fund (LWF) Configuration</CardTitle>
              <CardDescription>LWF contribution settings (if applicable)</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="lwfEnabled"
                  checked={formData.lwfEnabled}
                  onChange={(e) => setFormData({ ...formData, lwfEnabled: e.target.checked })}
                />
                <label htmlFor="lwfEnabled" className="text-sm font-medium">
                  Enable LWF
                </label>
              </div>

              {formData.lwfEnabled && (
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Employee Amount (Monthly)
                    </label>
                    <input
                      type="number"
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      value={formData.lwfEmployeeAmount}
                      onChange={(e) => setFormData({ ...formData, lwfEmployeeAmount: parseFloat(e.target.value) })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Employer Amount (Monthly)
                    </label>
                    <input
                      type="number"
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      value={formData.lwfEmployerAmount}
                      onChange={(e) => setFormData({ ...formData, lwfEmployerAmount: parseFloat(e.target.value) })}
                    />
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Gratuity Configuration */}
          <Card>
            <CardHeader>
              <CardTitle>Gratuity Configuration</CardTitle>
              <CardDescription>
                Gratuity provision settings (optional - not mandatory for payroll)
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="gratuityEnabled"
                  checked={formData.gratuityEnabled}
                  onChange={(e) => setFormData({ ...formData, gratuityEnabled: e.target.checked })}
                />
                <label htmlFor="gratuityEnabled" className="text-sm font-medium">
                  Enable Gratuity Provision
                </label>
              </div>

              <div className="bg-yellow-50 p-3 rounded-md text-sm text-yellow-800">
                <p>
                  <strong>Note:</strong> Gratuity is payable only after 5 years of continuous service.
                  Enabling this will add a monthly provision to employer cost calculations, but it's
                  not a statutory deduction from salary. Most companies don't provision monthly and
                  pay gratuity only when an employee completes 5 years and leaves.
                </p>
              </div>

              {formData.gratuityEnabled && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Gratuity Rate (% of Basic)
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    className="w-full rounded-md border border-gray-300 px-3 py-2"
                    value={formData.gratuityRate}
                    onChange={(e) => setFormData({ ...formData, gratuityRate: parseFloat(e.target.value) })}
                  />
                  <p className="text-xs text-gray-500 mt-1">
                    Standard rate is 4.81% (formula: 15/26 days per month = 15/26/12 = 4.81%)
                  </p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Save Button */}
          <div className="flex justify-end">
            <Button
              onClick={handleSave}
              disabled={updateConfig.isPending || createConfig.isPending}
            >
              {updateConfig.isPending || createConfig.isPending ? 'Saving...' : 'Save Settings'}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

export default PayrollSettings





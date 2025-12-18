import { useState, useEffect } from 'react'
import { useCompanies } from '@/hooks/api/useCompanies'
import {
  useCreateCalculationRule,
  useUpdateCalculationRule,
  useFormulaVariables,
  useValidateFormula,
  usePreviewRuleCalculation,
} from '@/features/payroll/hooks/useCalculationRules'
import type { CalculationRule, CreateCalculationRuleDto } from '@/features/payroll/types/payroll'
import { AlertCircle, CheckCircle, Play, Info, Lightbulb, HelpCircle } from 'lucide-react'

// Tooltip component for inline help
const HelpTooltip = ({ text }: { text: string }) => (
  <div className="group relative inline-block ml-1">
    <HelpCircle size={14} className="text-gray-400 hover:text-gray-600 cursor-help" />
    <div className="absolute z-10 invisible group-hover:visible bg-gray-900 text-white text-xs rounded-lg py-2 px-3 w-64 bottom-full left-1/2 transform -translate-x-1/2 mb-2">
      {text}
      <div className="absolute top-full left-1/2 transform -translate-x-1/2 -mt-1">
        <div className="border-4 border-transparent border-t-gray-900"></div>
      </div>
    </div>
  </div>
)

// Quick tips for each rule type
const RuleTypeTips: Record<string, { tip: string; example: string }> = {
  percentage: {
    tip: 'Best for: PF/ESI contributions, HRA, DA, performance bonuses',
    example: 'Example: 12% of basic salary for PF contribution',
  },
  fixed: {
    tip: 'Best for: Fixed allowances like conveyance, medical, food allowance',
    example: 'Example: Rs.1,600 monthly conveyance allowance',
  },
  slab: {
    tip: 'Best for: Professional Tax, income tax slabs, tiered bonuses',
    example: 'Example: PT Karnataka - Rs.0 if gross<=15k, Rs.200 if gross>15k',
  },
  formula: {
    tip: 'Best for: Complex calculations with conditions, multiple variables',
    example: 'Example: MIN(basic * 0.12, 1800) for ceiling-based PF',
  },
}

interface CalculationRuleFormProps {
  rule?: CalculationRule
  companyId?: string
  onSuccess: () => void
  onCancel: () => void
}

type RuleType = 'percentage' | 'fixed' | 'slab' | 'formula'
type ComponentType = 'earning' | 'deduction' | 'employer_contribution'

interface SlabConfig {
  slabs: Array<{ min: number; max: number | null; value: number }>
  baseField: string
}

interface PercentageConfig {
  percentage: number
  base: string
  maxAmount?: number
}

interface FixedConfig {
  amount: number
}

interface FormulaConfig {
  expression: string
}

const CalculationRuleForm = ({ rule, companyId, onSuccess, onCancel }: CalculationRuleFormProps) => {
  const { data: companies = [] } = useCompanies()
  const { data: variables = [] } = useFormulaVariables()
  const createRule = useCreateCalculationRule()
  const updateRule = useUpdateCalculationRule()
  const validateFormula = useValidateFormula()
  const previewCalculation = usePreviewRuleCalculation()

  // Form state
  const [selectedCompanyId, setSelectedCompanyId] = useState(companyId || rule?.companyId || '')
  const [name, setName] = useState(rule?.name || '')
  const [description, setDescription] = useState(rule?.description || '')
  const [componentType, setComponentType] = useState<ComponentType>(rule?.componentType || 'earning')
  const [componentCode, setComponentCode] = useState(rule?.componentCode || '')
  const [componentName, setComponentName] = useState(rule?.componentName || '')
  const [ruleType, setRuleType] = useState<RuleType>(rule?.ruleType as RuleType || 'percentage')
  const [priority, setPriority] = useState(rule?.priority || 100)
  const [effectiveFrom, setEffectiveFrom] = useState(rule?.effectiveFrom?.split('T')[0] || new Date().toISOString().split('T')[0])
  const [effectiveTo, setEffectiveTo] = useState(rule?.effectiveTo?.split('T')[0] || '')
  const [isActive, setIsActive] = useState(rule?.isActive ?? true)
  const [isTaxable, setIsTaxable] = useState(rule?.isTaxable ?? true)
  const [affectsPfWage, setAffectsPfWage] = useState(rule?.affectsPfWage ?? false)
  const [affectsEsiWage, setAffectsEsiWage] = useState(rule?.affectsEsiWage ?? false)

  // Rule type specific state
  const [percentageConfig, setPercentageConfig] = useState<PercentageConfig>({ percentage: 0, base: 'basic' })
  const [fixedConfig, setFixedConfig] = useState<FixedConfig>({ amount: 0 })
  const [slabConfig, setSlabConfig] = useState<SlabConfig>({
    slabs: [{ min: 0, max: null, value: 0 }],
    baseField: 'monthly_gross',
  })
  const [formulaExpression, setFormulaExpression] = useState('')

  // Validation and preview state
  const [formulaError, setFormulaError] = useState<string | null>(null)
  const [formulaValid, setFormulaValid] = useState(false)
  const [previewResult, setPreviewResult] = useState<number | null>(null)
  const [previewError, setPreviewError] = useState<string | null>(null)

  // Parse existing rule config
  useEffect(() => {
    if (rule) {
      try {
        const config = JSON.parse(rule.formulaConfig)
        switch (rule.ruleType) {
          case 'percentage':
            setPercentageConfig({
              percentage: config.percentage || 0,
              base: config.base || 'basic',
              maxAmount: config.maxAmount,
            })
            break
          case 'fixed':
            setFixedConfig({ amount: config.amount || 0 })
            break
          case 'slab':
            setSlabConfig({
              slabs: config.slabs || [{ min: 0, max: null, value: 0 }],
              baseField: config.baseField || 'monthly_gross',
            })
            break
          case 'formula':
            setFormulaExpression(config.expression || '')
            break
        }
      } catch {
        // Invalid JSON, use defaults
      }
    }
  }, [rule])

  const buildFormulaConfig = (): string => {
    switch (ruleType) {
      case 'percentage':
        return JSON.stringify(percentageConfig)
      case 'fixed':
        return JSON.stringify(fixedConfig)
      case 'slab':
        return JSON.stringify(slabConfig)
      case 'formula':
        return JSON.stringify({ expression: formulaExpression })
      default:
        return '{}'
    }
  }

  const handleValidateFormula = async () => {
    if (ruleType !== 'formula' || !formulaExpression) return

    try {
      const result = await validateFormula.mutateAsync({ expression: formulaExpression })
      if (result.isValid) {
        setFormulaValid(true)
        setFormulaError(null)
        setPreviewResult(result.sampleResult ?? null)
      } else {
        setFormulaValid(false)
        setFormulaError(result.errorMessage || 'Invalid formula')
      }
    } catch (err: any) {
      setFormulaValid(false)
      setFormulaError(err.message || 'Validation failed')
    }
  }

  const handlePreview = async () => {
    try {
      const ruleData: CreateCalculationRuleDto = {
        companyId: selectedCompanyId,
        name,
        componentType,
        componentCode,
        ruleType,
        formulaConfig: buildFormulaConfig(),
      }
      const result = await previewCalculation.mutateAsync({ rule: ruleData })
      if (result.success) {
        setPreviewResult(result.result)
        setPreviewError(null)
      } else {
        setPreviewError(result.errorMessage || 'Preview failed')
        setPreviewResult(null)
      }
    } catch (err: any) {
      setPreviewError(err.message || 'Preview failed')
      setPreviewResult(null)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!selectedCompanyId || !name || !componentCode) {
      return
    }

    const data: CreateCalculationRuleDto = {
      companyId: selectedCompanyId,
      name,
      description: description || undefined,
      componentType,
      componentCode,
      componentName: componentName || undefined,
      ruleType,
      formulaConfig: buildFormulaConfig(),
      priority,
      effectiveFrom: effectiveFrom || undefined,
      effectiveTo: effectiveTo || undefined,
      isTaxable,
      affectsPfWage,
      affectsEsiWage,
    }

    try {
      if (rule) {
        await updateRule.mutateAsync({
          id: rule.id,
          data: { ...data, isActive },
        })
      } else {
        await createRule.mutateAsync(data)
      }
      onSuccess()
    } catch (error) {
      console.error('Failed to save rule:', error)
    }
  }

  const addSlab = () => {
    setSlabConfig(prev => ({
      ...prev,
      slabs: [...prev.slabs, { min: 0, max: null, value: 0 }],
    }))
  }

  const removeSlab = (index: number) => {
    setSlabConfig(prev => ({
      ...prev,
      slabs: prev.slabs.filter((_, i) => i !== index),
    }))
  }

  const updateSlab = (index: number, field: 'min' | 'max' | 'value', value: number | null) => {
    setSlabConfig(prev => ({
      ...prev,
      slabs: prev.slabs.map((slab, i) =>
        i === index ? { ...slab, [field]: value } : slab
      ),
    }))
  }

  const isPending = createRule.isPending || updateRule.isPending

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Basic Info */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900">Basic Information</h3>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Company *</label>
            <select
              value={selectedCompanyId}
              onChange={(e) => setSelectedCompanyId(e.target.value)}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="">Select Company</option>
              {companies.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Rule Name *</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              placeholder="e.g., PF Employee Contribution"
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={2}
            placeholder="Optional description of what this rule calculates"
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
      </div>

      {/* Component Configuration */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900">Component Configuration</h3>

        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Component Type *
              <HelpTooltip text="Earnings add to salary, Deductions reduce take-home, Employer contributions are company costs not deducted from employee." />
            </label>
            <select
              value={componentType}
              onChange={(e) => setComponentType(e.target.value as ComponentType)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="earning">Earning (adds to salary)</option>
              <option value="deduction">Deduction (reduces take-home)</option>
              <option value="employer_contribution">Employer Contribution (company cost)</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Component Code *
              <HelpTooltip text="Unique identifier for this component. Use UPPERCASE with underscores. This code is used to reference this component in payroll processing." />
            </label>
            <input
              type="text"
              value={componentCode}
              onChange={(e) => setComponentCode(e.target.value.toUpperCase())}
              required
              placeholder="e.g., PF_EMPLOYEE"
              className="w-full px-3 py-2 border border-gray-300 rounded-md font-mono"
            />
            {/* Common component codes */}
            <div className="flex flex-wrap gap-1 mt-2">
              {(componentType === 'deduction'
                ? ['PF_EMPLOYEE', 'ESI_EMPLOYEE', 'PT', 'TDS', 'LOP']
                : componentType === 'employer_contribution'
                ? ['PF_EMPLOYER', 'ESI_EMPLOYER', 'GRATUITY', 'PF_ADMIN', 'PF_EDLI']
                : ['BASIC', 'HRA', 'DA', 'SPECIAL', 'CONVEYANCE', 'MEDICAL', 'BONUS']
              ).map(code => (
                <button
                  key={code}
                  type="button"
                  onClick={() => setComponentCode(code)}
                  className="px-2 py-0.5 text-xs bg-gray-100 hover:bg-gray-200 rounded text-gray-600 font-mono"
                >
                  {code}
                </button>
              ))}
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Display Name
              <HelpTooltip text="Human-readable name shown on payslips. If left blank, the component code will be used." />
            </label>
            <input
              type="text"
              value={componentName}
              onChange={(e) => setComponentName(e.target.value)}
              placeholder="e.g., PF Contribution"
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
        </div>
      </div>

      {/* Rule Type Selection */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900">Calculation Method</h3>

        <div className="grid grid-cols-4 gap-3">
          {[
            { type: 'percentage', label: 'Percentage', desc: 'Calculate as % of base', icon: '%' },
            { type: 'fixed', label: 'Fixed Amount', desc: 'Fixed value', icon: '₹' },
            { type: 'slab', label: 'Slab-based', desc: 'Income slabs', icon: '⊟' },
            { type: 'formula', label: 'Custom Formula', desc: 'Advanced expression', icon: 'ƒx' },
          ].map(({ type, label, desc, icon }) => (
            <button
              key={type}
              type="button"
              onClick={() => setRuleType(type as RuleType)}
              className={`p-3 rounded-lg border-2 text-left transition-colors ${
                ruleType === type
                  ? 'border-blue-500 bg-blue-50'
                  : 'border-gray-200 hover:border-gray-300'
              }`}
            >
              <div className="flex items-center gap-2">
                <span className="text-lg font-mono text-gray-400">{icon}</span>
                <span className="font-medium text-gray-900">{label}</span>
              </div>
              <div className="text-xs text-gray-500 mt-1">{desc}</div>
            </button>
          ))}
        </div>

        {/* Rule type tip */}
        {RuleTypeTips[ruleType] && (
          <div className="flex items-start gap-2 p-3 bg-amber-50 border border-amber-200 rounded-lg text-sm">
            <Lightbulb size={16} className="text-amber-600 mt-0.5 flex-shrink-0" />
            <div>
              <div className="text-amber-800">{RuleTypeTips[ruleType].tip}</div>
              <div className="text-amber-600 text-xs mt-1">{RuleTypeTips[ruleType].example}</div>
            </div>
          </div>
        )}

        {/* Rule Type Specific Config */}
        <div className="p-4 bg-gray-50 rounded-lg">
          {ruleType === 'percentage' && (
            <div className="grid grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Percentage (%)</label>
                <input
                  type="number"
                  step="0.01"
                  value={percentageConfig.percentage}
                  onChange={(e) => setPercentageConfig(prev => ({ ...prev, percentage: parseFloat(e.target.value) || 0 }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Base Field</label>
                <select
                  value={percentageConfig.base}
                  onChange={(e) => setPercentageConfig(prev => ({ ...prev, base: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                >
                  {variables.map(v => (
                    <option key={v.code} value={v.code}>{v.displayName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Max Amount (optional)</label>
                <input
                  type="number"
                  value={percentageConfig.maxAmount || ''}
                  onChange={(e) => setPercentageConfig(prev => ({
                    ...prev,
                    maxAmount: e.target.value ? parseFloat(e.target.value) : undefined,
                  }))}
                  placeholder="No limit"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                />
              </div>
            </div>
          )}

          {ruleType === 'fixed' && (
            <div className="w-1/3">
              <label className="block text-sm font-medium text-gray-700 mb-1">Fixed Amount</label>
              <input
                type="number"
                step="0.01"
                value={fixedConfig.amount}
                onChange={(e) => setFixedConfig({ amount: parseFloat(e.target.value) || 0 })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
              />
            </div>
          )}

          {ruleType === 'slab' && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <label className="block text-sm font-medium text-gray-700">Income Slabs</label>
                <button
                  type="button"
                  onClick={addSlab}
                  className="text-sm text-blue-600 hover:text-blue-800"
                >
                  + Add Slab
                </button>
              </div>
              <div className="space-y-2">
                {slabConfig.slabs.map((slab, index) => (
                  <div key={index} className="flex items-center gap-3">
                    <input
                      type="number"
                      value={slab.min}
                      onChange={(e) => updateSlab(index, 'min', parseFloat(e.target.value) || 0)}
                      placeholder="Min"
                      className="w-24 px-2 py-1 border border-gray-300 rounded text-sm"
                    />
                    <span className="text-gray-400">to</span>
                    <input
                      type="number"
                      value={slab.max ?? ''}
                      onChange={(e) => updateSlab(index, 'max', e.target.value ? parseFloat(e.target.value) : null)}
                      placeholder="Max (or blank)"
                      className="w-24 px-2 py-1 border border-gray-300 rounded text-sm"
                    />
                    <span className="text-gray-400">=</span>
                    <input
                      type="number"
                      value={slab.value}
                      onChange={(e) => updateSlab(index, 'value', parseFloat(e.target.value) || 0)}
                      placeholder="Amount"
                      className="w-24 px-2 py-1 border border-gray-300 rounded text-sm"
                    />
                    {slabConfig.slabs.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeSlab(index)}
                        className="text-red-500 hover:text-red-700 text-sm"
                      >
                        Remove
                      </button>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {ruleType === 'formula' && (
            <div className="space-y-4">
              {/* Quick formula templates */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Quick Formula Templates</label>
                <div className="flex flex-wrap gap-2">
                  {[
                    { label: 'PF (Ceiling)', formula: 'MIN(basic + da, 15000) * 12 / 100' },
                    { label: 'PF (Actual)', formula: '(basic + da) * 12 / 100' },
                    { label: 'ESI', formula: 'IF(monthly_gross <= 21000, gross_earnings * 0.75 / 100, 0)' },
                    { label: 'Gratuity', formula: 'basic * 4.81 / 100' },
                    { label: 'LOP', formula: '(monthly_gross / working_days) * lop_days' },
                    { label: 'Pro-rata', formula: 'basic * present_days / working_days' },
                  ].map(t => (
                    <button
                      key={t.label}
                      type="button"
                      onClick={() => setFormulaExpression(t.formula)}
                      className="px-3 py-1.5 text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 rounded-full border border-blue-200"
                    >
                      {t.label}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <div className="flex items-center justify-between mb-1">
                  <label className="block text-sm font-medium text-gray-700">
                    Formula Expression
                    <HelpTooltip text="Write a mathematical expression using variables and functions. Use +, -, *, / for arithmetic. Functions: MIN(), MAX(), ROUND(), IF(), ABS(), FLOOR(), CEILING()" />
                  </label>
                  <button
                    type="button"
                    onClick={handleValidateFormula}
                    disabled={!formulaExpression || validateFormula.isPending}
                    className="text-sm text-blue-600 hover:text-blue-800 flex items-center gap-1"
                  >
                    <Play size={14} />
                    {validateFormula.isPending ? 'Validating...' : 'Validate'}
                  </button>
                </div>
                <textarea
                  value={formulaExpression}
                  onChange={(e) => {
                    setFormulaExpression(e.target.value)
                    setFormulaValid(false)
                    setFormulaError(null)
                  }}
                  rows={3}
                  placeholder="e.g., MIN(basic * 0.12, 1800)"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md font-mono text-sm"
                />
                {formulaError && (
                  <div className="mt-1 flex items-center gap-1 text-red-600 text-sm">
                    <AlertCircle size={14} />
                    {formulaError}
                  </div>
                )}
                {formulaValid && (
                  <div className="mt-1 flex items-center gap-1 text-green-600 text-sm">
                    <CheckCircle size={14} />
                    Valid formula. Sample result: {previewResult?.toFixed(2)}
                  </div>
                )}
              </div>

              {/* Available Variables with categories */}
              <div className="grid grid-cols-2 gap-3">
                <div className="p-3 bg-white rounded border border-gray-200">
                  <div className="text-xs font-medium text-gray-500 uppercase tracking-wide mb-2">Salary Components</div>
                  <div className="flex flex-wrap gap-1">
                    {['basic', 'hra', 'da', 'special', 'conveyance', 'medical', 'monthly_gross'].map(v => (
                      <button
                        key={v}
                        type="button"
                        onClick={() => setFormulaExpression(prev => prev + (prev && !prev.endsWith(' ') ? ' ' : '') + v)}
                        className="px-2 py-0.5 text-xs font-mono bg-green-50 hover:bg-green-100 rounded text-green-700"
                      >
                        {v}
                      </button>
                    ))}
                  </div>
                </div>
                <div className="p-3 bg-white rounded border border-gray-200">
                  <div className="text-xs font-medium text-gray-500 uppercase tracking-wide mb-2">Attendance & Time</div>
                  <div className="flex flex-wrap gap-1">
                    {['working_days', 'present_days', 'lop_days', 'payable_days'].map(v => (
                      <button
                        key={v}
                        type="button"
                        onClick={() => setFormulaExpression(prev => prev + (prev && !prev.endsWith(' ') ? ' ' : '') + v)}
                        className="px-2 py-0.5 text-xs font-mono bg-purple-50 hover:bg-purple-100 rounded text-purple-700"
                      >
                        {v}
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              {/* Function reference */}
              <div className="p-3 bg-gray-100 rounded text-xs text-gray-600">
                <strong>Functions:</strong> MIN(a,b), MAX(a,b), ROUND(x,decimals), IF(condition, true_val, false_val), ABS(x), FLOOR(x), CEILING(x)
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Preview */}
      <div className="p-4 bg-blue-50 border border-blue-200 rounded-lg">
        <div className="flex items-center justify-between">
          <div>
            <div className="font-medium text-blue-900">Preview Calculation</div>
            <div className="text-sm text-blue-700">Test with sample values</div>
          </div>
          <button
            type="button"
            onClick={handlePreview}
            disabled={previewCalculation.isPending}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50"
          >
            {previewCalculation.isPending ? 'Calculating...' : 'Run Preview'}
          </button>
        </div>
        {previewResult !== null && (
          <div className="mt-3 text-2xl font-bold text-blue-900">
            Result: {previewResult.toLocaleString('en-IN', { style: 'currency', currency: 'INR' })}
          </div>
        )}
        {previewError && (
          <div className="mt-2 text-red-600 text-sm">{previewError}</div>
        )}
      </div>

      {/* Advanced Settings */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900">Advanced Settings</h3>

        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Priority</label>
            <input
              type="number"
              value={priority}
              onChange={(e) => setPriority(parseInt(e.target.value) || 100)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
            <p className="text-xs text-gray-500 mt-1">Lower = Higher priority</p>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Effective From</label>
            <input
              type="date"
              value={effectiveFrom}
              onChange={(e) => setEffectiveFrom(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Effective To (optional)</label>
            <input
              type="date"
              value={effectiveTo}
              onChange={(e) => setEffectiveTo(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>
        </div>

        <div className="flex flex-wrap gap-6">
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="rounded border-gray-300"
            />
            <span className="text-sm text-gray-700">Active</span>
          </label>
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={isTaxable}
              onChange={(e) => setIsTaxable(e.target.checked)}
              className="rounded border-gray-300"
            />
            <span className="text-sm text-gray-700">Taxable</span>
          </label>
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={affectsPfWage}
              onChange={(e) => setAffectsPfWage(e.target.checked)}
              className="rounded border-gray-300"
            />
            <span className="text-sm text-gray-700">Affects PF Wage</span>
          </label>
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={affectsEsiWage}
              onChange={(e) => setAffectsEsiWage(e.target.checked)}
              className="rounded border-gray-300"
            />
            <span className="text-sm text-gray-700">Affects ESI Wage</span>
          </label>
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isPending}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
        >
          {isPending ? 'Saving...' : rule ? 'Update Rule' : 'Create Rule'}
        </button>
      </div>
    </form>
  )
}

export default CalculationRuleForm

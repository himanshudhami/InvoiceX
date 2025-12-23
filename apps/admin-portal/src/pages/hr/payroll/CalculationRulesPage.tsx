import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import {
  useCalculationRules,
  useDeleteCalculationRule,
  useCalculationRuleTemplates,
  useCreateRuleFromTemplate,
} from '@/features/payroll/hooks/useCalculationRules'
import { useCompanies } from '@/hooks/api/useCompanies'
import type { CalculationRule, CalculationRuleTemplate } from '@/features/payroll/types/payroll'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import {
  Edit,
  Trash2,
  Copy,
  Plus,
  Calculator,
  Zap,
  Settings2,
  HelpCircle,
  ChevronDown,
  ChevronUp,
  BookOpen,
  Code,
  Percent,
  IndianRupee,
  Layers,
  AlertTriangle,
  ExternalLink
} from 'lucide-react'
import CalculationRuleForm from '@/components/forms/CalculationRuleForm'

// Help documentation content
const HelpSection = ({ isOpen, onToggle }: { isOpen: boolean; onToggle: () => void }) => {
  const [activeTab, setActiveTab] = useState<'overview' | 'formula' | 'examples' | 'indian'>('overview')

  return (
    <div className="bg-white rounded-lg shadow border border-blue-200">
      <button
        onClick={onToggle}
        className="w-full px-6 py-4 flex items-center justify-between text-left hover:bg-blue-50 transition-colors"
      >
        <div className="flex items-center gap-3">
          <HelpCircle className="h-5 w-5 text-blue-600" />
          <div>
            <span className="font-medium text-gray-900">How to Use Calculation Rules</span>
            <span className="ml-2 text-sm text-gray-500">Click to {isOpen ? 'hide' : 'expand'} documentation</span>
          </div>
        </div>
        {isOpen ? <ChevronUp className="h-5 w-5 text-gray-400" /> : <ChevronDown className="h-5 w-5 text-gray-400" />}
      </button>

      {isOpen && (
        <div className="px-6 pb-6 border-t border-gray-100">
          {/* Tab Navigation */}
          <div className="flex gap-2 mt-4 mb-4 border-b border-gray-200">
            {[
              { key: 'overview', label: 'Overview', icon: BookOpen },
              { key: 'formula', label: 'Formula Reference', icon: Code },
              { key: 'examples', label: 'Examples', icon: Layers },
              { key: 'indian', label: 'Indian Payroll', icon: IndianRupee },
            ].map(tab => (
              <button
                key={tab.key}
                onClick={() => setActiveTab(tab.key as any)}
                className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
                  activeTab === tab.key
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                <tab.icon size={16} />
                {tab.label}
              </button>
            ))}
          </div>

          {/* Tab Content */}
          {activeTab === 'overview' && (
            <div className="space-y-4 text-sm text-gray-700">
              <div>
                <h4 className="font-semibold text-gray-900 mb-2">What are Calculation Rules?</h4>
                <p>Calculation rules define how salary components are computed during payroll processing. Instead of hardcoded formulas, you can create flexible rules that can be changed without code updates.</p>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="p-3 bg-green-50 rounded-lg border border-green-200">
                  <h5 className="font-medium text-green-800 flex items-center gap-2">
                    <Percent size={16} /> Percentage Rules
                  </h5>
                  <p className="text-green-700 mt-1">Calculate a percentage of a base value. Example: "12% of Basic Salary"</p>
                </div>
                <div className="p-3 bg-blue-50 rounded-lg border border-blue-200">
                  <h5 className="font-medium text-blue-800 flex items-center gap-2">
                    <IndianRupee size={16} /> Fixed Amount
                  </h5>
                  <p className="text-blue-700 mt-1">A constant amount regardless of salary. Example: "Rs.1,600 Conveyance"</p>
                </div>
                <div className="p-3 bg-purple-50 rounded-lg border border-purple-200">
                  <h5 className="font-medium text-purple-800 flex items-center gap-2">
                    <Layers size={16} /> Slab-based
                  </h5>
                  <p className="text-purple-700 mt-1">Different values for different income ranges. Example: Professional Tax slabs</p>
                </div>
                <div className="p-3 bg-orange-50 rounded-lg border border-orange-200">
                  <h5 className="font-medium text-orange-800 flex items-center gap-2">
                    <Code size={16} /> Custom Formula
                  </h5>
                  <p className="text-orange-700 mt-1">Complex calculations with expressions. Example: "MIN(basic * 0.12, 1800)"</p>
                </div>
              </div>

              <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg">
                <h5 className="font-medium text-amber-800">Priority System</h5>
                <p className="text-amber-700 mt-1">When multiple rules match a component, the one with <strong>lowest priority number</strong> is used. Priority 1 runs before Priority 100.</p>
              </div>
            </div>
          )}

          {activeTab === 'formula' && (
            <div className="space-y-4 text-sm">
              <div>
                <h4 className="font-semibold text-gray-900 mb-3">Formula Syntax Reference</h4>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <h5 className="font-medium text-gray-800 mb-2">Operators</h5>
                    <table className="w-full text-xs">
                      <tbody className="divide-y divide-gray-100">
                        <tr><td className="py-1 font-mono text-blue-600">+</td><td className="py-1 text-gray-600">Addition</td></tr>
                        <tr><td className="py-1 font-mono text-blue-600">-</td><td className="py-1 text-gray-600">Subtraction</td></tr>
                        <tr><td className="py-1 font-mono text-blue-600">*</td><td className="py-1 text-gray-600">Multiplication</td></tr>
                        <tr><td className="py-1 font-mono text-blue-600">/</td><td className="py-1 text-gray-600">Division</td></tr>
                        <tr><td className="py-1 font-mono text-blue-600">%</td><td className="py-1 text-gray-600">Modulo (remainder)</td></tr>
                        <tr><td className="py-1 font-mono text-blue-600">&gt; &lt; &gt;= &lt;=</td><td className="py-1 text-gray-600">Comparisons</td></tr>
                        <tr><td className="py-1 font-mono text-blue-600">== !=</td><td className="py-1 text-gray-600">Equality checks</td></tr>
                        <tr><td className="py-1 font-mono text-blue-600">AND OR</td><td className="py-1 text-gray-600">Logical operators</td></tr>
                      </tbody>
                    </table>
                  </div>

                  <div>
                    <h5 className="font-medium text-gray-800 mb-2">Functions</h5>
                    <table className="w-full text-xs">
                      <tbody className="divide-y divide-gray-100">
                        <tr><td className="py-1 font-mono text-green-600">MIN(a, b)</td><td className="py-1 text-gray-600">Returns smaller value</td></tr>
                        <tr><td className="py-1 font-mono text-green-600">MAX(a, b)</td><td className="py-1 text-gray-600">Returns larger value</td></tr>
                        <tr><td className="py-1 font-mono text-green-600">ROUND(x, n)</td><td className="py-1 text-gray-600">Round to n decimals</td></tr>
                        <tr><td className="py-1 font-mono text-green-600">FLOOR(x)</td><td className="py-1 text-gray-600">Round down</td></tr>
                        <tr><td className="py-1 font-mono text-green-600">CEILING(x)</td><td className="py-1 text-gray-600">Round up</td></tr>
                        <tr><td className="py-1 font-mono text-green-600">ABS(x)</td><td className="py-1 text-gray-600">Absolute value</td></tr>
                        <tr><td className="py-1 font-mono text-green-600">IF(cond, t, f)</td><td className="py-1 text-gray-600">Conditional: if/then/else</td></tr>
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>

              <div>
                <h5 className="font-medium text-gray-800 mb-2">Available Variables</h5>
                <div className="grid grid-cols-4 gap-2 text-xs">
                  {[
                    { code: 'basic', desc: 'Basic Salary' },
                    { code: 'hra', desc: 'HRA' },
                    { code: 'da', desc: 'Dearness Allowance' },
                    { code: 'special', desc: 'Special Allowance' },
                    { code: 'conveyance', desc: 'Conveyance' },
                    { code: 'medical', desc: 'Medical Allowance' },
                    { code: 'monthly_gross', desc: 'Monthly Gross' },
                    { code: 'pf_wage', desc: 'PF Wage Base' },
                    { code: 'gross_earnings', desc: 'Current Month Gross' },
                    { code: 'working_days', desc: 'Working Days' },
                    { code: 'present_days', desc: 'Present Days' },
                    { code: 'lop_days', desc: 'LOP Days' },
                    { code: 'payroll_month', desc: 'Month (1-12)' },
                    { code: 'payroll_year', desc: 'Year (e.g., 2024)' },
                  ].map(v => (
                    <div key={v.code} className="p-2 bg-gray-50 rounded font-mono">
                      <span className="text-purple-600">{v.code}</span>
                      <div className="text-gray-500 font-sans text-[10px]">{v.desc}</div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {activeTab === 'examples' && (
            <div className="space-y-4 text-sm">
              <h4 className="font-semibold text-gray-900 mb-3">Formula Examples</h4>

              <div className="space-y-3">
                <div className="p-3 bg-gray-50 rounded-lg border">
                  <div className="flex justify-between items-start">
                    <span className="font-medium text-gray-800">PF Employee (Ceiling-based)</span>
                    <span className="text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded">Deduction</span>
                  </div>
                  <code className="block mt-2 p-2 bg-gray-800 text-green-400 rounded text-xs font-mono">
                    MIN(basic + da, 15000) * 12 / 100
                  </code>
                  <p className="text-xs text-gray-500 mt-2">12% of (Basic + DA), capped at Rs.15,000 wage base = Rs.1,800 max</p>
                </div>

                <div className="p-3 bg-gray-50 rounded-lg border">
                  <div className="flex justify-between items-start">
                    <span className="font-medium text-gray-800">PF Employee (Actual Wage)</span>
                    <span className="text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded">Deduction</span>
                  </div>
                  <code className="block mt-2 p-2 bg-gray-800 text-green-400 rounded text-xs font-mono">
                    (basic + da) * 12 / 100
                  </code>
                  <p className="text-xs text-gray-500 mt-2">12% of actual (Basic + DA) - no ceiling, higher contribution for high earners</p>
                </div>

                <div className="p-3 bg-gray-50 rounded-lg border">
                  <div className="flex justify-between items-start">
                    <span className="font-medium text-gray-800">ESI Employee</span>
                    <span className="text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded">Deduction</span>
                  </div>
                  <code className="block mt-2 p-2 bg-gray-800 text-green-400 rounded text-xs font-mono">
                    IF(monthly_gross &lt;= 21000, gross_earnings * 0.75 / 100, 0)
                  </code>
                  <p className="text-xs text-gray-500 mt-2">0.75% of gross only if monthly gross is Rs.21,000 or below</p>
                </div>

                <div className="p-3 bg-gray-50 rounded-lg border">
                  <div className="flex justify-between items-start">
                    <span className="font-medium text-gray-800">Gratuity Provision</span>
                    <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded">Employer</span>
                  </div>
                  <code className="block mt-2 p-2 bg-gray-800 text-green-400 rounded text-xs font-mono">
                    basic * 4.81 / 100
                  </code>
                  <p className="text-xs text-gray-500 mt-2">4.81% of basic (15/26 days per year, monthly provision)</p>
                </div>

                <div className="p-3 bg-gray-50 rounded-lg border">
                  <div className="flex justify-between items-start">
                    <span className="font-medium text-gray-800">LOP Deduction</span>
                    <span className="text-xs bg-red-100 text-red-700 px-2 py-0.5 rounded">Deduction</span>
                  </div>
                  <code className="block mt-2 p-2 bg-gray-800 text-green-400 rounded text-xs font-mono">
                    (monthly_gross / working_days) * lop_days
                  </code>
                  <p className="text-xs text-gray-500 mt-2">Daily salary rate multiplied by LOP days</p>
                </div>

                <div className="p-3 bg-gray-50 rounded-lg border">
                  <div className="flex justify-between items-start">
                    <span className="font-medium text-gray-800">PT Karnataka 2024 (Feb Adjustment)</span>
                    <span className="text-xs bg-purple-100 text-purple-700 px-2 py-0.5 rounded">Statutory</span>
                  </div>
                  <code className="block mt-2 p-2 bg-gray-800 text-green-400 rounded text-xs font-mono">
                    IF(gross_earnings &lt; 25000, 0, IF(payroll_month = 2, 300, 200))
                  </code>
                  <p className="text-xs text-gray-500 mt-2">Rs.0 if &lt; 25K; Rs.200/month; Rs.300 in Feb (annual cap Rs.2,500)</p>
                </div>

                <div className="p-3 bg-gray-50 rounded-lg border">
                  <div className="flex justify-between items-start">
                    <span className="font-medium text-gray-800">Performance Bonus (Conditional)</span>
                    <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded">Earning</span>
                  </div>
                  <code className="block mt-2 p-2 bg-gray-800 text-green-400 rounded text-xs font-mono">
                    IF(tenure_years &gt;= 1, basic * 10 / 100, 0)
                  </code>
                  <p className="text-xs text-gray-500 mt-2">10% of basic as bonus only if tenure is 1+ years</p>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'indian' && (
            <div className="space-y-4 text-sm text-gray-700">
              <h4 className="font-semibold text-gray-900 mb-3">Indian Payroll Compliance Guide</h4>

              <div className="grid grid-cols-2 gap-4">
                <div className="p-4 border rounded-lg">
                  <h5 className="font-semibold text-gray-900 mb-2">Provident Fund (PF)</h5>
                  <ul className="space-y-1 text-xs text-gray-600">
                    <li><strong>Wage Base:</strong> Basic + DA (+ Special Allowance if opted)</li>
                    <li><strong>Ceiling Mode:</strong> 12% on Rs.15,000 = Rs.1,800 max</li>
                    <li><strong>Actual Mode:</strong> 12% on full wage (no ceiling)</li>
                    <li><strong>Employer Split:</strong> 8.33% EPS + 3.67% EPF</li>
                    <li><strong>Admin Charges:</strong> 0.5% on PF wage</li>
                    <li><strong>EDLI:</strong> 0.5% on PF wage (capped at Rs.15,000)</li>
                  </ul>
                </div>

                <div className="p-4 border rounded-lg">
                  <h5 className="font-semibold text-gray-900 mb-2">ESI (Employee State Insurance)</h5>
                  <ul className="space-y-1 text-xs text-gray-600">
                    <li><strong>Eligibility:</strong> Gross salary up to Rs.21,000/month</li>
                    <li><strong>Employee:</strong> 0.75% of gross earnings</li>
                    <li><strong>Employer:</strong> 3.25% of gross earnings</li>
                    <li><strong>6-Month Rule:</strong> If eligible at start of period (Apr-Sep or Oct-Mar), continue full period</li>
                    <li><strong>Total:</strong> 4% of gross</li>
                  </ul>
                </div>

                <div className="p-4 border rounded-lg">
                  <h5 className="font-semibold text-gray-900 mb-2">Professional Tax (PT)</h5>
                  <ul className="space-y-1 text-xs text-gray-600">
                    <li><strong>State-wise:</strong> Varies by Indian state</li>
                    <li><strong>Karnataka 2024:</strong> Rs.0 if gross &lt; Rs.25,000; Rs.200/month (Apr-Jan); Rs.300 in Feb</li>
                    <li><strong>Maharashtra:</strong> Rs.175-300 based on slabs</li>
                    <li><strong>Max:</strong> Rs.2,500 per year (Constitutional limit)</li>
                    <li><strong>Feb Adjustment:</strong> Use <code className="bg-gray-100 px-1">IF(payroll_month = 2, 300, 200)</code></li>
                  </ul>
                </div>

                <div className="p-4 border rounded-lg">
                  <h5 className="font-semibold text-gray-900 mb-2">Gratuity</h5>
                  <ul className="space-y-1 text-xs text-gray-600">
                    <li><strong>Formula:</strong> (15/26) * Basic * Years of Service</li>
                    <li><strong>Monthly Provision:</strong> ~4.81% of basic</li>
                    <li><strong>Eligibility:</strong> 5 years of continuous service</li>
                    <li><strong>Maximum:</strong> Rs.20 lakh (as of 2024)</li>
                    <li><strong>Employer Cost:</strong> This is an employer-only contribution</li>
                  </ul>
                </div>
              </div>

              <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg">
                <h5 className="font-medium text-blue-800">Pro Tip: PF Calculation Mode</h5>
                <p className="text-blue-700 text-xs mt-1">
                  Your company currently uses <strong>actual_wage</strong> mode for PF. This means 12% is calculated on the full Basic + DA without the Rs.15,000 ceiling.
                  To match your April payroll: Create a rule with formula <code className="bg-blue-100 px-1 rounded">(basic + da) * 12 / 100</code>
                </p>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

const CalculationRulesPage = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingRule, setEditingRule] = useState<CalculationRule | null>(null)
  const [deletingRule, setDeletingRule] = useState<CalculationRule | null>(null)
  const [showTemplates, setShowTemplates] = useState(false)
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  const [filterComponentType, setFilterComponentType] = useState<string>('')
  const [filterActiveOnly, setFilterActiveOnly] = useState<boolean | undefined>(undefined)
  const [showHelp, setShowHelp] = useState(false)

  const { data: rulesData, isLoading, error, refetch } = useCalculationRules({
    companyId: selectedCompanyId || undefined,
    componentType: filterComponentType || undefined,
    isActive: filterActiveOnly,
  })
  const { data: companies = [] } = useCompanies()
  const { data: templates = [] } = useCalculationRuleTemplates()
  const deleteRule = useDeleteCalculationRule()
  const createFromTemplate = useCreateRuleFromTemplate()

  const rules = rulesData?.items ?? []

  const handleEdit = (rule: CalculationRule) => {
    setEditingRule(rule)
  }

  const handleDelete = (rule: CalculationRule) => {
    setDeletingRule(rule)
  }

  const handleDeleteConfirm = async () => {
    if (deletingRule) {
      try {
        await deleteRule.mutateAsync(deletingRule.id)
        setDeletingRule(null)
      } catch (error) {
        console.error('Failed to delete rule:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingRule(null)
    refetch()
  }

  const handleCreateFromTemplate = async (template: CalculationRuleTemplate) => {
    if (!selectedCompanyId) {
      return // UI already prevents this, but safeguard
    }
    try {
      await createFromTemplate.mutateAsync({
        templateId: template.id,
        companyId: selectedCompanyId,
      })
      setShowTemplates(false)
      refetch()
    } catch (error) {
      console.error('Failed to create from template:', error)
    }
  }

  const getCompanyName = (companyId: string) => {
    const company = companies.find(c => c.id === companyId)
    return company?.name || 'Unknown'
  }

  const getRuleTypeLabel = (ruleType: string) => {
    switch (ruleType) {
      case 'percentage': return 'Percentage'
      case 'fixed': return 'Fixed Amount'
      case 'slab': return 'Slab-based'
      case 'formula': return 'Custom Formula'
      default: return ruleType
    }
  }

  const getComponentTypeColor = (type: string) => {
    switch (type) {
      case 'earning': return 'bg-green-100 text-green-800'
      case 'deduction': return 'bg-red-100 text-red-800'
      case 'employer_contribution': return 'bg-blue-100 text-blue-800'
      default: return 'bg-gray-100 text-gray-800'
    }
  }

  const parseFormulaConfig = (config: string) => {
    try {
      return JSON.parse(config)
    } catch {
      return {}
    }
  }

  const columns: ColumnDef<CalculationRule>[] = [
    {
      accessorKey: 'name',
      header: 'Rule',
      cell: ({ row }) => {
        const rule = row.original
        const config = parseFormulaConfig(rule.formulaConfig)
        return (
          <div>
            <div className="font-medium text-gray-900">{rule.name}</div>
            <div className="text-sm text-gray-500">
              {rule.componentCode} - {getRuleTypeLabel(rule.ruleType)}
            </div>
            {rule.ruleType === 'percentage' && config.percentage && (
              <div className="text-xs text-gray-400">{config.percentage}% of {config.base || 'base'}</div>
            )}
            {rule.ruleType === 'fixed' && config.amount && (
              <div className="text-xs text-gray-400">Fixed: {config.amount}</div>
            )}
            {rule.ruleType === 'formula' && config.expression && (
              <div className="text-xs text-gray-400 font-mono truncate max-w-xs" title={config.expression}>
                {config.expression}
              </div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'componentType',
      header: 'Type',
      cell: ({ row }) => {
        const type = row.original.componentType
        const label = type === 'employer_contribution' ? 'Employer' : type.charAt(0).toUpperCase() + type.slice(1)
        return (
          <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${getComponentTypeColor(type)}`}>
            {label}
          </span>
        )
      },
    },
    {
      accessorKey: 'priority',
      header: 'Priority',
      cell: ({ row }) => (
        <div className="text-center">
          <span className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-gray-100 text-gray-700 font-medium text-sm">
            {row.original.priority}
          </span>
        </div>
      ),
    },
    {
      accessorKey: 'companyId',
      header: 'Company',
      cell: ({ row }) => (
        <div className="text-sm text-gray-600">
          {getCompanyName(row.original.companyId)}
        </div>
      ),
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => {
        const rule = row.original
        return (
          <div className="space-y-1">
            {rule.isSystem && (
              <span className="inline-flex px-2 py-0.5 text-xs font-medium rounded-full bg-purple-100 text-purple-800">
                System
              </span>
            )}
            <span className={`inline-flex px-2 py-0.5 text-xs font-medium rounded-full ${
              rule.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'
            }`}>
              {rule.isActive ? 'Active' : 'Inactive'}
            </span>
          </div>
        )
      },
    },
    {
      accessorKey: 'effectiveFrom',
      header: 'Effective',
      cell: ({ row }) => {
        const rule = row.original
        return (
          <div className="text-sm text-gray-500">
            <div>From: {new Date(rule.effectiveFrom).toLocaleDateString()}</div>
            {rule.effectiveTo && (
              <div>To: {new Date(rule.effectiveTo).toLocaleDateString()}</div>
            )}
          </div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const rule = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleEdit(rule)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit rule"
              disabled={rule.isSystem}
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(rule)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete rule"
              disabled={rule.isSystem}
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load calculation rules</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  const activeRules = rules.filter(r => r.isActive)
  const systemRules = rules.filter(r => r.isSystem)
  const earningRules = rules.filter(r => r.componentType === 'earning')
  const deductionRules = rules.filter(r => r.componentType === 'deduction')

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Calculation Rules</h1>
          <p className="text-gray-600 mt-2">Advanced formula-based rules for custom payroll calculations</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setShowHelp(!showHelp)}
            className={`px-4 py-2 text-sm font-medium border rounded-md flex items-center gap-2 transition-colors ${
              showHelp
                ? 'text-blue-700 bg-blue-50 border-blue-300'
                : 'text-gray-700 bg-white border-gray-300 hover:bg-gray-50'
            }`}
          >
            <HelpCircle size={16} />
            Help
          </button>
          <button
            onClick={() => setShowTemplates(true)}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 flex items-center gap-2"
          >
            <Copy size={16} />
            From Template
          </button>
          <button
            onClick={() => setIsCreateDrawerOpen(true)}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 flex items-center gap-2"
          >
            <Plus size={16} />
            Create Rule
          </button>
        </div>
      </div>

      {/* Info Banner - When to use */}
      <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <AlertTriangle className="h-5 w-5 text-amber-600 mt-0.5 flex-shrink-0" />
          <div className="text-sm text-amber-800">
            <p className="font-medium mb-1">Advanced Feature - Use Only When Needed</p>
            <p className="text-amber-700 mb-2">
              Calculation Rules are for <strong>custom formulas</strong> when standard calculations don't apply.
              Most companies don't need this - statutory rates are configured elsewhere.
            </p>
            <ul className="list-disc list-inside space-y-1 text-amber-700">
              <li><strong>Government Rates</strong> (Tax slabs, PF 12%, ESI 0.75%): Use <a href="/tax-rule-packs" className="underline hover:text-amber-900 inline-flex items-center gap-1">Tax Rule Packs<ExternalLink className="h-3 w-3" /></a></li>
              <li><strong>Company Settings</strong> (Enable/disable PF/ESI, calculation mode): Use <a href="/payroll/settings" className="underline hover:text-amber-900 inline-flex items-center gap-1">Payroll Settings<ExternalLink className="h-3 w-3" /></a></li>
              <li><strong>Custom Formulas</strong> (e.g., special PT logic, conditional bonuses): Use this page</li>
            </ul>
          </div>
        </div>
      </div>

      {/* Help Documentation */}
      <HelpSection isOpen={showHelp} onToggle={() => setShowHelp(!showHelp)} />

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center gap-2">
            <Calculator className="h-5 w-5 text-gray-400" />
            <span className="text-sm font-medium text-gray-500">Total Rules</span>
          </div>
          <div className="text-2xl font-bold text-gray-900 mt-1">{rules.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center gap-2">
            <Zap className="h-5 w-5 text-green-400" />
            <span className="text-sm font-medium text-gray-500">Active</span>
          </div>
          <div className="text-2xl font-bold text-green-600 mt-1">{activeRules.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center gap-2">
            <Settings2 className="h-5 w-5 text-purple-400" />
            <span className="text-sm font-medium text-gray-500">System</span>
          </div>
          <div className="text-2xl font-bold text-purple-600 mt-1">{systemRules.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Earnings</div>
          <div className="text-2xl font-bold text-green-600 mt-1">{earningRules.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Deductions</div>
          <div className="text-2xl font-bold text-red-600 mt-1">{deductionRules.length}</div>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="flex flex-wrap gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
            <select
              value={selectedCompanyId}
              onChange={(e) => setSelectedCompanyId(e.target.value)}
              className="w-48 px-3 py-2 border border-gray-300 rounded-md text-sm"
            >
              <option value="">All Companies</option>
              {companies.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Component Type</label>
            <select
              value={filterComponentType}
              onChange={(e) => setFilterComponentType(e.target.value)}
              className="w-48 px-3 py-2 border border-gray-300 rounded-md text-sm"
            >
              <option value="">All Types</option>
              <option value="earning">Earnings</option>
              <option value="deduction">Deductions</option>
              <option value="employer_contribution">Employer Contributions</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
            <select
              value={filterActiveOnly === undefined ? '' : filterActiveOnly ? 'active' : 'inactive'}
              onChange={(e) => {
                if (e.target.value === '') setFilterActiveOnly(undefined)
                else setFilterActiveOnly(e.target.value === 'active')
              }}
              className="w-40 px-3 py-2 border border-gray-300 rounded-md text-sm"
            >
              <option value="">All Status</option>
              <option value="active">Active Only</option>
              <option value="inactive">Inactive Only</option>
            </select>
          </div>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={rules}
            searchPlaceholder="Search rules..."
          />
        </div>
      </div>

      {/* Create Rule Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Calculation Rule"
        size="xl"
      >
        <CalculationRuleForm
          companyId={selectedCompanyId}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Rule Drawer */}
      <Drawer
        isOpen={!!editingRule}
        onClose={() => setEditingRule(null)}
        title="Edit Calculation Rule"
        size="xl"
      >
        {editingRule && (
          <CalculationRuleForm
            rule={editingRule}
            companyId={editingRule.companyId}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingRule(null)}
          />
        )}
      </Drawer>

      {/* Templates Modal */}
      <Modal
        isOpen={showTemplates}
        onClose={() => setShowTemplates(false)}
        title="Create from Template"
        size="lg"
      >
        <div className="space-y-4">
          {/* Company Selector */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Select Company <span className="text-red-500">*</span>
            </label>
            <select
              value={selectedCompanyId}
              onChange={(e) => setSelectedCompanyId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">-- Select a company --</option>
              {companies.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
            {!selectedCompanyId && (
              <p className="text-xs text-amber-600 mt-1">Please select a company to create rules for</p>
            )}
          </div>

          {/* Templates Grid */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Available Templates
            </label>
            <div className={`grid grid-cols-1 md:grid-cols-2 gap-3 max-h-80 overflow-y-auto ${!selectedCompanyId ? 'opacity-50 pointer-events-none' : ''}`}>
              {templates.map(template => (
                <div
                  key={template.id}
                  className="p-4 border border-gray-200 rounded-lg hover:border-blue-300 hover:bg-blue-50 cursor-pointer transition-colors"
                  onClick={() => handleCreateFromTemplate(template)}
                >
                  <div className="font-medium text-gray-900">{template.name}</div>
                  <div className="text-sm text-gray-500 mt-1 line-clamp-2">{template.description}</div>
                  <div className="flex gap-2 mt-2">
                    <span className={`inline-flex px-2 py-0.5 text-xs font-medium rounded-full ${getComponentTypeColor(template.componentType)}`}>
                      {template.componentType}
                    </span>
                    <span className="inline-flex px-2 py-0.5 text-xs font-medium rounded-full bg-gray-100 text-gray-600">
                      {template.ruleType}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {templates.length === 0 && (
            <div className="text-center py-8 text-gray-500">
              No templates available. Run migration 061 to seed templates.
            </div>
          )}
        </div>
      </Modal>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingRule}
        onClose={() => setDeletingRule(null)}
        title="Delete Calculation Rule"
        size="sm"
      >
        {deletingRule && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the rule <strong>{deletingRule.name}</strong>?
              This action cannot be undone.
            </p>
            {deletingRule.isActive && (
              <div className="p-3 bg-amber-50 border border-amber-200 rounded-md">
                <div className="text-amber-800 text-sm">
                  <strong>Warning:</strong> This is an active rule. Deleting it may affect payroll calculations.
                </div>
              </div>
            )}
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingRule(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteRule.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteRule.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default CalculationRulesPage

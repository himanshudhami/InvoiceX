import { useState, useMemo, useCallback } from 'react'
import { ArrowRight, Info } from 'lucide-react'
import {
  TallyParsedData,
  TallyMappingConfig as MappingConfigType,
  TallyGroupMapping,
  TallyCostCategoryMapping,
  tallyMigrationApi
} from '@/services/api/migration/tallyMigrationService'

interface TallyMappingConfigProps {
  batchId: string
  parsedData: TallyParsedData
  onConfigured: (config: MappingConfigType) => void
  onBack: () => void
}

// Default mapping rules for Tally groups
const DEFAULT_GROUP_MAPPINGS: Record<string, { targetEntity: string; targetAccountType?: string }> = {
  'sundry creditors': { targetEntity: 'vendors' },
  'sundry debtors': { targetEntity: 'customers' },
  'bank accounts': { targetEntity: 'bank_accounts' },
  'bank o/d a/c': { targetEntity: 'bank_accounts' },
  'cash-in-hand': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
  'purchase accounts': { targetEntity: 'chart_of_accounts', targetAccountType: 'expense' },
  'direct expenses': { targetEntity: 'chart_of_accounts', targetAccountType: 'expense' },
  'indirect expenses': { targetEntity: 'chart_of_accounts', targetAccountType: 'expense' },
  'sales accounts': { targetEntity: 'chart_of_accounts', targetAccountType: 'income' },
  'direct incomes': { targetEntity: 'chart_of_accounts', targetAccountType: 'income' },
  'indirect incomes': { targetEntity: 'chart_of_accounts', targetAccountType: 'income' },
  'duties & taxes': { targetEntity: 'chart_of_accounts', targetAccountType: 'liability' },
  'fixed assets': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
  'investments': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
  'loans (liability)': { targetEntity: 'chart_of_accounts', targetAccountType: 'liability' },
  'secured loans': { targetEntity: 'chart_of_accounts', targetAccountType: 'liability' },
  'unsecured loans': { targetEntity: 'chart_of_accounts', targetAccountType: 'liability' },
  'loans & advances (asset)': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
  'current assets': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
  'current liabilities': { targetEntity: 'chart_of_accounts', targetAccountType: 'liability' },
  'provisions': { targetEntity: 'chart_of_accounts', targetAccountType: 'liability' },
  'reserves & surplus': { targetEntity: 'chart_of_accounts', targetAccountType: 'equity' },
  'capital account': { targetEntity: 'chart_of_accounts', targetAccountType: 'equity' },
  'stock-in-hand': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
  'deposits (asset)': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
  'branch / divisions': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
  'suspense a/c': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
  'misc. expenses (asset)': { targetEntity: 'chart_of_accounts', targetAccountType: 'asset' },
}

const TARGET_ENTITIES = [
  { value: 'customers', label: 'Customers (AR)' },
  { value: 'vendors', label: 'Vendors (AP)' },
  { value: 'bank_accounts', label: 'Bank Accounts' },
  { value: 'chart_of_accounts', label: 'General Ledger Account' },
  { value: 'suspense', label: 'Suspense Account' },
  { value: 'skip', label: 'Skip (Do not import)' },
]

const ACCOUNT_TYPES = [
  { value: 'asset', label: 'Asset' },
  { value: 'liability', label: 'Liability' },
  { value: 'equity', label: 'Equity' },
  { value: 'income', label: 'Income' },
  { value: 'expense', label: 'Expense' },
]

const TallyMappingConfig = ({ batchId, parsedData, onConfigured, onBack }: TallyMappingConfigProps) => {
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [createSuspenseAccounts, setCreateSuspenseAccounts] = useState(true)
  const [skipUnmapped, setSkipUnmapped] = useState(false)

  // Get unique groups from ledgers
  const uniqueGroups = useMemo(() => {
    const groups = new Map<string, number>()
    parsedData.masters.ledgers.forEach(ledger => {
      const group = ledger.ledgerGroup || ledger.parent || 'Unknown'
      groups.set(group, (groups.get(group) || 0) + 1)
    })
    return Array.from(groups.entries()).sort((a, b) => b[1] - a[1])
  }, [parsedData])

  // Initialize mappings with defaults
  const [groupMappings, setGroupMappings] = useState<TallyGroupMapping[]>(() =>
    uniqueGroups.map(([groupName]) => {
      const defaultMapping = DEFAULT_GROUP_MAPPINGS[groupName.toLowerCase()]
      return {
        tallyGroupName: groupName,
        targetEntity: defaultMapping?.targetEntity || 'suspense',
        targetAccountType: defaultMapping?.targetAccountType,
      }
    })
  )

  // Cost category mappings
  const uniqueCostCategories = useMemo(() => {
    const categories = new Set<string>()
    parsedData.masters.costCategories.forEach(c => categories.add(c.name))
    parsedData.masters.costCenters.forEach(c => {
      if (c.category) categories.add(c.category)
    })
    return Array.from(categories)
  }, [parsedData])

  const [costCategoryMappings, setCostCategoryMappings] = useState<TallyCostCategoryMapping[]>(() =>
    uniqueCostCategories.map(name => ({
      tallyCostCategoryName: name,
      targetTagGroup: 'cost_center'
    }))
  )

  const updateGroupMapping = useCallback((index: number, field: keyof TallyGroupMapping, value: string) => {
    setGroupMappings(prev => {
      const next = [...prev]
      next[index] = { ...next[index], [field]: value }
      // Clear account type if not chart_of_accounts
      if (field === 'targetEntity' && value !== 'chart_of_accounts') {
        next[index].targetAccountType = undefined
      }
      return next
    })
  }, [])

  const updateCostCategoryMapping = useCallback((index: number, value: string) => {
    setCostCategoryMappings(prev => {
      const next = [...prev]
      next[index] = { ...next[index], targetTagGroup: value }
      return next
    })
  }, [])

  const handleSaveAndContinue = async () => {
    setIsSaving(true)
    setError(null)

    const config: Omit<MappingConfigType, 'batchId'> = {
      groupMappings,
      ledgerMappings: [], // Individual ledger overrides can be added later
      costCategoryMappings,
      createSuspenseAccounts,
      skipUnmapped
    }

    try {
      await tallyMigrationApi.configureMappings(batchId, config)
      onConfigured({ ...config, batchId })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save configuration')
    } finally {
      setIsSaving(false)
    }
  }

  const unmappedCount = groupMappings.filter(m => m.targetEntity === 'suspense').length
  const ledgerCountInSuspense = uniqueGroups
    .filter(([group]) => groupMappings.find(m => m.tallyGroupName === group)?.targetEntity === 'suspense')
    .reduce((sum, [, count]) => sum + count, 0)

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
          Configure Mappings
        </h2>
        <p className="text-gray-600 dark:text-gray-400 mt-1">
          Define how Tally ledger groups should be mapped to your system
        </p>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-4">
          <p className="text-sm text-green-600 dark:text-green-400">Auto-mapped Groups</p>
          <p className="text-2xl font-bold text-green-700 dark:text-green-300">
            {groupMappings.filter(m => m.targetEntity !== 'suspense' && m.targetEntity !== 'skip').length}
          </p>
        </div>
        <div className="bg-yellow-50 dark:bg-yellow-900/20 rounded-lg p-4">
          <p className="text-sm text-yellow-600 dark:text-yellow-400">Unmapped (Suspense)</p>
          <p className="text-2xl font-bold text-yellow-700 dark:text-yellow-300">
            {unmappedCount}
          </p>
        </div>
        <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4">
          <p className="text-sm text-blue-600 dark:text-blue-400">Total Ledgers</p>
          <p className="text-2xl font-bold text-blue-700 dark:text-blue-300">
            {parsedData.masters.ledgers.length}
          </p>
        </div>
      </div>

      {/* Group Mappings */}
      <div className="space-y-3">
        <h3 className="font-medium text-gray-900 dark:text-white">
          Ledger Group Mappings
        </h3>
        <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
          <div className="max-h-96 overflow-y-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 dark:bg-gray-800 sticky top-0">
                <tr>
                  <th className="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">
                    Tally Group
                  </th>
                  <th className="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">
                    Count
                  </th>
                  <th className="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">
                    <ArrowRight className="h-4 w-4 inline mr-1" />
                    Map To
                  </th>
                  <th className="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">
                    Account Type
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                {uniqueGroups.map(([group, count], index) => {
                  const mapping = groupMappings[index]
                  return (
                    <tr key={group} className="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                      <td className="px-4 py-3 text-gray-900 dark:text-white">
                        {group}
                      </td>
                      <td className="px-4 py-3 text-gray-500 dark:text-gray-400">
                        {count}
                      </td>
                      <td className="px-4 py-3">
                        <select
                          value={mapping.targetEntity}
                          onChange={(e) => updateGroupMapping(index, 'targetEntity', e.target.value)}
                          className="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                        >
                          {TARGET_ENTITIES.map(opt => (
                            <option key={opt.value} value={opt.value}>{opt.label}</option>
                          ))}
                        </select>
                      </td>
                      <td className="px-4 py-3">
                        {mapping.targetEntity === 'chart_of_accounts' && (
                          <select
                            value={mapping.targetAccountType || ''}
                            onChange={(e) => updateGroupMapping(index, 'targetAccountType', e.target.value)}
                            className="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                          >
                            <option value="">Select type...</option>
                            {ACCOUNT_TYPES.map(opt => (
                              <option key={opt.value} value={opt.value}>{opt.label}</option>
                            ))}
                          </select>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Cost Category Mappings */}
      {uniqueCostCategories.length > 0 && (
        <div className="space-y-3">
          <h3 className="font-medium text-gray-900 dark:text-white">
            Cost Category Mappings
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Tally Cost Centers will be imported as Tags for multi-dimensional tracking
          </p>
          <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th className="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">
                    Tally Cost Category
                  </th>
                  <th className="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">
                    <ArrowRight className="h-4 w-4 inline mr-1" />
                    Tag Group
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                {uniqueCostCategories.map((category, index) => (
                  <tr key={category} className="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                    <td className="px-4 py-3 text-gray-900 dark:text-white">
                      {category}
                    </td>
                    <td className="px-4 py-3">
                      <select
                        value={costCategoryMappings[index]?.targetTagGroup || 'cost_center'}
                        onChange={(e) => updateCostCategoryMapping(index, e.target.value)}
                        className="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                      >
                        <option value="cost_center">Cost Center</option>
                        <option value="department">Department</option>
                        <option value="project">Project</option>
                        <option value="client">Client</option>
                        <option value="region">Region</option>
                      </select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Options */}
      <div className="space-y-3">
        <h3 className="font-medium text-gray-900 dark:text-white">Options</h3>
        <div className="space-y-2">
          <label className="flex items-start gap-3 p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
            <input
              type="checkbox"
              checked={createSuspenseAccounts}
              onChange={(e) => setCreateSuspenseAccounts(e.target.checked)}
              className="mt-0.5"
            />
            <div>
              <span className="text-sm font-medium text-gray-900 dark:text-white">
                Create suspense accounts for unmapped ledgers
              </span>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                Ledgers in unmapped groups ({ledgerCountInSuspense} ledgers) will be posted to a suspense account.
                You can review and reclassify them later.
              </p>
            </div>
          </label>
          <label className="flex items-start gap-3 p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
            <input
              type="checkbox"
              checked={skipUnmapped}
              onChange={(e) => setSkipUnmapped(e.target.checked)}
              className="mt-0.5"
            />
            <div>
              <span className="text-sm font-medium text-gray-900 dark:text-white">
                Skip unmapped ledgers entirely
              </span>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                Ledgers in unmapped groups will not be imported at all (not recommended for full migration)
              </p>
            </div>
          </label>
        </div>
      </div>

      {/* Info */}
      <div className="flex items-start gap-3 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
        <Info className="h-5 w-5 text-blue-500 flex-shrink-0 mt-0.5" />
        <div className="text-sm text-blue-700 dark:text-blue-300">
          <p className="font-medium">About the mapping</p>
          <ul className="mt-2 space-y-1 text-blue-600 dark:text-blue-400">
            <li>• Sundry Debtors will create Customer records with linked receivable accounts</li>
            <li>• Sundry Creditors will create Vendor records with linked payable accounts</li>
            <li>• Bank Accounts will create both Bank Account and GL Account records</li>
            <li>• All other ledgers will be created as Chart of Accounts entries</li>
          </ul>
        </div>
      </div>

      {/* Error Display */}
      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
          <p className="text-red-700 dark:text-red-300">{error}</p>
        </div>
      )}

      {/* Actions */}
      <div className="flex justify-between pt-4 border-t border-gray-200 dark:border-gray-700">
        <button
          onClick={onBack}
          disabled={isSaving}
          className="px-4 py-2 text-gray-600 hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200 disabled:opacity-50"
        >
          Back
        </button>
        <button
          onClick={handleSaveAndContinue}
          disabled={isSaving}
          className="px-6 py-2 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700 disabled:opacity-50"
        >
          {isSaving ? 'Saving...' : 'Save & Continue'}
        </button>
      </div>
    </div>
  )
}

export default TallyMappingConfig

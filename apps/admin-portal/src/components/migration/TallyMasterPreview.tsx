import { useState } from 'react'
import {
  Users,
  Building2,
  Package,
  Warehouse,
  Ruler,
  Tag,
  AlertTriangle,
  AlertCircle,
  Info,
  ChevronDown,
  ChevronUp
} from 'lucide-react'
import { TallyParsedData, TallyValidationIssue } from '@/services/api/migration/tallyMigrationService'

interface TallyMasterPreviewProps {
  parsedData: TallyParsedData
  onNext: () => void
  onBack: () => void
}

interface MasterCategoryProps {
  title: string
  icon: React.ComponentType<{ className?: string }>
  count: number
  items: Array<{ name: string; group?: string; guid: string }>
  expanded: boolean
  onToggle: () => void
}

const MasterCategory = ({ title, icon: Icon, count, items, expanded, onToggle }: MasterCategoryProps) => (
  <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
    <button
      onClick={onToggle}
      className="w-full flex items-center justify-between p-4 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
    >
      <div className="flex items-center gap-3">
        <Icon className="h-5 w-5 text-gray-500" />
        <span className="font-medium text-gray-900 dark:text-white">{title}</span>
        <span className="px-2 py-0.5 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs rounded-full">
          {count}
        </span>
      </div>
      {expanded ? (
        <ChevronUp className="h-5 w-5 text-gray-400" />
      ) : (
        <ChevronDown className="h-5 w-5 text-gray-400" />
      )}
    </button>
    {expanded && (
      <div className="border-t border-gray-200 dark:border-gray-700 max-h-64 overflow-y-auto">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 dark:bg-gray-800 sticky top-0">
            <tr>
              <th className="px-4 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Name</th>
              <th className="px-4 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Group/Parent</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {items.slice(0, 100).map((item) => (
              <tr key={item.guid} className="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                <td className="px-4 py-2 text-gray-900 dark:text-white">{item.name}</td>
                <td className="px-4 py-2 text-gray-500 dark:text-gray-400">{item.group || '-'}</td>
              </tr>
            ))}
            {items.length > 100 && (
              <tr>
                <td colSpan={2} className="px-4 py-2 text-center text-gray-500 dark:text-gray-400">
                  ... and {items.length - 100} more
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    )}
  </div>
)

const ValidationIssueItem = ({ issue }: { issue: TallyValidationIssue }) => {
  const getIcon = () => {
    switch (issue.severity) {
      case 'error':
        return <AlertCircle className="h-4 w-4 text-red-500" />
      case 'warning':
        return <AlertTriangle className="h-4 w-4 text-yellow-500" />
      default:
        return <Info className="h-4 w-4 text-blue-500" />
    }
  }

  const getBgColor = () => {
    switch (issue.severity) {
      case 'error':
        return 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800'
      case 'warning':
        return 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-200 dark:border-yellow-800'
      default:
        return 'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800'
    }
  }

  return (
    <div className={`flex items-start gap-3 p-3 rounded-lg border ${getBgColor()}`}>
      {getIcon()}
      <div className="flex-1 min-w-0">
        <p className="text-sm text-gray-900 dark:text-white">{issue.message}</p>
        {issue.recordName && (
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            {issue.recordType}: {issue.recordName}
          </p>
        )}
      </div>
      <span className="text-xs font-mono text-gray-400">{issue.code}</span>
    </div>
  )
}

const TallyMasterPreview = ({ parsedData, onNext, onBack }: TallyMasterPreviewProps) => {
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set(['ledgers']))

  const toggleCategory = (category: string) => {
    setExpandedCategories(prev => {
      const next = new Set(prev)
      if (next.has(category)) {
        next.delete(category)
      } else {
        next.add(category)
      }
      return next
    })
  }

  const { masters, validationIssues, hasErrors } = parsedData

  const errorCount = validationIssues.filter(i => i.severity === 'error').length
  const warningCount = validationIssues.filter(i => i.severity === 'warning').length

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
          Preview Master Data
        </h2>
        <p className="text-gray-600 dark:text-gray-400 mt-1">
          Review the data that will be imported from your Tally export
        </p>
      </div>

      {/* Company Info */}
      {masters.tallyCompanyName && (
        <div className="bg-gray-50 dark:bg-gray-900/50 rounded-lg p-4">
          <h3 className="font-medium text-gray-900 dark:text-white mb-2">Tally Company</h3>
          <p className="text-gray-600 dark:text-gray-400">{masters.tallyCompanyName}</p>
          {masters.financialYearFrom && masters.financialYearTo && (
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
              Financial Year: {masters.financialYearFrom} to {masters.financialYearTo}
            </p>
          )}
        </div>
      )}

      {/* Validation Issues */}
      {validationIssues.length > 0 && (
        <div className="space-y-3">
          <h3 className="font-medium text-gray-900 dark:text-white flex items-center gap-2">
            Validation Results
            {errorCount > 0 && (
              <span className="px-2 py-0.5 bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200 text-xs rounded-full">
                {errorCount} error{errorCount !== 1 ? 's' : ''}
              </span>
            )}
            {warningCount > 0 && (
              <span className="px-2 py-0.5 bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200 text-xs rounded-full">
                {warningCount} warning{warningCount !== 1 ? 's' : ''}
              </span>
            )}
          </h3>
          <div className="space-y-2 max-h-48 overflow-y-auto">
            {validationIssues.slice(0, 20).map((issue, idx) => (
              <ValidationIssueItem key={idx} issue={issue} />
            ))}
            {validationIssues.length > 20 && (
              <p className="text-sm text-gray-500 dark:text-gray-400 text-center py-2">
                ... and {validationIssues.length - 20} more issues
              </p>
            )}
          </div>
        </div>
      )}

      {/* Masters Categories */}
      <div className="space-y-3">
        <h3 className="font-medium text-gray-900 dark:text-white">Masters to Import</h3>

        <MasterCategory
          title="Ledgers"
          icon={Users}
          count={masters.ledgers.length}
          items={masters.ledgers.map(l => ({ name: l.name, group: l.ledgerGroup || l.parent, guid: l.guid }))}
          expanded={expandedCategories.has('ledgers')}
          onToggle={() => toggleCategory('ledgers')}
        />

        <MasterCategory
          title="Stock Groups"
          icon={Building2}
          count={masters.stockGroups.length}
          items={masters.stockGroups.map(g => ({ name: g.name, group: g.parent, guid: g.guid }))}
          expanded={expandedCategories.has('stockGroups')}
          onToggle={() => toggleCategory('stockGroups')}
        />

        <MasterCategory
          title="Stock Items"
          icon={Package}
          count={masters.stockItems.length}
          items={masters.stockItems.map(i => ({ name: i.name, group: i.stockGroup, guid: i.guid }))}
          expanded={expandedCategories.has('stockItems')}
          onToggle={() => toggleCategory('stockItems')}
        />

        <MasterCategory
          title="Godowns/Warehouses"
          icon={Warehouse}
          count={masters.godowns.length}
          items={masters.godowns.map(g => ({ name: g.name, group: g.parent, guid: g.guid }))}
          expanded={expandedCategories.has('godowns')}
          onToggle={() => toggleCategory('godowns')}
        />

        <MasterCategory
          title="Units of Measure"
          icon={Ruler}
          count={masters.units.length}
          items={masters.units.map(u => ({ name: u.name, group: u.symbol, guid: u.guid }))}
          expanded={expandedCategories.has('units')}
          onToggle={() => toggleCategory('units')}
        />

        <MasterCategory
          title="Cost Centers"
          icon={Tag}
          count={masters.costCenters.length}
          items={masters.costCenters.map(c => ({ name: c.name, group: c.category || c.parent, guid: c.guid }))}
          expanded={expandedCategories.has('costCenters')}
          onToggle={() => toggleCategory('costCenters')}
        />
      </div>

      {/* Ledger Groups Summary */}
      {Object.keys(masters.ledgerCountsByGroup).length > 0 && (
        <div className="bg-gray-50 dark:bg-gray-900/50 rounded-lg p-4">
          <h3 className="font-medium text-gray-900 dark:text-white mb-3">Ledgers by Group</h3>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
            {Object.entries(masters.ledgerCountsByGroup)
              .sort(([, a], [, b]) => b - a)
              .slice(0, 12)
              .map(([group, count]) => (
                <div
                  key={group}
                  className="flex items-center justify-between p-2 bg-white dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700"
                >
                  <span className="text-sm text-gray-700 dark:text-gray-300 truncate">{group}</span>
                  <span className="text-sm font-medium text-gray-900 dark:text-white ml-2">{count}</span>
                </div>
              ))
            }
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex justify-between pt-4 border-t border-gray-200 dark:border-gray-700">
        <button
          onClick={onBack}
          className="px-4 py-2 text-gray-600 hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200"
        >
          Back
        </button>
        <button
          onClick={onNext}
          disabled={hasErrors}
          className="px-6 py-2 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {hasErrors ? 'Fix Errors to Continue' : 'Continue to Mapping'}
        </button>
      </div>
    </div>
  )
}

export default TallyMasterPreview

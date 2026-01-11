import { useState, useMemo } from 'react'
import { useAbnormalBalances } from '@/features/ledger/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { AbnormalBalanceItem, AccountType } from '@/services/api/types'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { format } from 'date-fns'
import {
  FileText,
  Download,
  Printer,
  AlertTriangle,
  CheckCircle,
  Info,
  Wallet,
  TrendingDown,
  BookOpen,
  TrendingUp,
  DollarSign,
  ChevronDown,
  ChevronRight,
  AlertCircle,
  Lightbulb
} from 'lucide-react'

const accountTypeConfig: Record<AccountType, { label: string; color: string; icon: React.ElementType }> = {
  asset: { label: 'Assets', color: 'text-blue-600', icon: Wallet },
  liability: { label: 'Liabilities', color: 'text-red-600', icon: TrendingDown },
  equity: { label: 'Equity', color: 'text-purple-600', icon: BookOpen },
  income: { label: 'Income', color: 'text-green-600', icon: TrendingUp },
  expense: { label: 'Expenses', color: 'text-orange-600', icon: DollarSign },
}

const severityConfig = {
  info: { color: 'bg-blue-50 border-blue-200 text-blue-800', icon: Info },
  warning: { color: 'bg-amber-50 border-amber-200 text-amber-800', icon: AlertTriangle },
}

const AbnormalBalanceReport = () => {
  const [companyFilter, setCompanyFilter] = useState<string>('')
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set())
  const [showContraAccounts, setShowContraAccounts] = useState(true)

  const { data: companies = [] } = useCompanies()
  const { data: report, isLoading, error, refetch } = useAbnormalBalances(
    companyFilter,
    !!companyFilter
  )

  const selectedCompany = companies.find(c => c.id === companyFilter)

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(amount)
  }

  // Filter and group items
  const filteredItems = useMemo(() => {
    if (!report?.items) return []
    return showContraAccounts
      ? report.items
      : report.items.filter(item => !item.isContraAccount)
  }, [report?.items, showContraAccounts])

  const groupedItems = useMemo(() => {
    return filteredItems.reduce((acc, item) => {
      const category = item.category
      if (!acc[category]) acc[category] = []
      acc[category].push(item)
      return acc
    }, {} as Record<string, AbnormalBalanceItem[]>)
  }, [filteredItems])

  const toggleCategory = (category: string) => {
    const newExpanded = new Set(expandedCategories)
    if (newExpanded.has(category)) {
      newExpanded.delete(category)
    } else {
      newExpanded.add(category)
    }
    setExpandedCategories(newExpanded)
  }

  const expandAll = () => {
    setExpandedCategories(new Set(Object.keys(groupedItems)))
  }

  const collapseAll = () => {
    setExpandedCategories(new Set())
  }

  const handlePrint = () => {
    window.print()
  }

  const handleExport = () => {
    if (!report) return

    const headers = ['Category', 'Account Code', 'Account Name', 'Account Type', 'Expected', 'Actual', 'Amount', 'Possible Reason', 'Recommended Action']
    const rows = filteredItems.map(item => [
      item.category,
      item.accountCode,
      item.accountName,
      item.accountType,
      item.expectedBalanceSide,
      item.actualBalanceSide,
      item.amount.toFixed(2),
      item.possibleReason,
      item.recommendedAction
    ])

    const csvContent = [
      `Abnormal Balance Report`,
      `Company: ${selectedCompany?.name || ''}`,
      `Generated: ${format(new Date(), 'dd MMM yyyy HH:mm')}`,
      '',
      headers.join(','),
      ...rows.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n')

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `abnormal-balance-report-${format(new Date(), 'yyyy-MM-dd')}.csv`
    link.click()
  }

  if (!companyFilter) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Data Quality: Abnormal Balances</h1>
          <p className="text-gray-600 mt-2">Identify accounts with balances opposite to their normal balance</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <AlertCircle className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">Select a Company</h3>
            <p className="mt-1 text-sm text-gray-500">Please select a company to view its data quality report</p>
            <div className="mt-6 flex justify-center">
              <CompanyFilterDropdown
                value={companyFilter}
                onChange={setCompanyFilter}
              />
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6 print:space-y-4">
      {/* Header */}
      <div className="flex justify-between items-start print:hidden">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Data Quality: Abnormal Balances</h1>
          <p className="text-gray-600 mt-2">Identify accounts with balances opposite to their normal balance</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={handlePrint}
            className="inline-flex items-center gap-2 px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            <Printer size={16} />
            Print
          </button>
          <button
            onClick={handleExport}
            disabled={!report}
            className="inline-flex items-center gap-2 px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50"
          >
            <Download size={16} />
            Export CSV
          </button>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-6 print:hidden">
        <div className="flex items-end gap-4 flex-wrap">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
            <CompanyFilterDropdown
              value={companyFilter}
              onChange={setCompanyFilter}
            />
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="showContra"
              checked={showContraAccounts}
              onChange={(e) => setShowContraAccounts(e.target.checked)}
              className="rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="showContra" className="text-sm text-gray-700">
              Show contra accounts (e.g., Accumulated Depreciation)
            </label>
          </div>
        </div>
      </div>

      {/* Print Header */}
      <div className="hidden print:block text-center mb-4">
        <h1 className="text-2xl font-bold">{selectedCompany?.name}</h1>
        <h2 className="text-xl mt-2">Abnormal Balance Report</h2>
        <p className="text-gray-600">Generated on {format(new Date(), 'dd MMMM yyyy')}</p>
      </div>

      {/* Loading / Error */}
      {isLoading && (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      )}

      {error && (
        <div className="text-center py-12">
          <div className="text-red-600 mb-4">Failed to load abnormal balance report</div>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
          >
            Retry
          </button>
        </div>
      )}

      {/* Report Content */}
      {report && !isLoading && (
        <>
          {/* Summary Banner */}
          {report.actionableIssues === 0 ? (
            <div className="rounded-lg p-4 flex items-center gap-3 bg-green-50 border border-green-200">
              <CheckCircle className="h-6 w-6 text-green-600" />
              <div>
                <span className="font-medium text-green-800">No Actionable Issues Found</span>
                <span className="text-green-600 ml-2">
                  {report.totalAbnormalAccounts > 0
                    ? `(${report.totalAbnormalAccounts} contra accounts are normal)`
                    : 'All account balances are as expected'}
                </span>
              </div>
            </div>
          ) : (
            <div className="rounded-lg p-4 flex items-center gap-3 bg-amber-50 border border-amber-200">
              <AlertTriangle className="h-6 w-6 text-amber-600" />
              <div className="flex-1">
                <span className="font-medium text-amber-800">
                  {report.actionableIssues} Account(s) Need Review
                </span>
                <span className="text-amber-600 ml-2">
                  Total Amount: {formatCurrency(report.totalAbnormalAmount)}
                </span>
              </div>
            </div>
          )}

          {/* Category Summary Cards */}
          {report.categorySummary.length > 0 && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 print:hidden">
              {report.categorySummary.map((cat) => {
                const isContra = cat.categoryName.includes('Contra')
                return (
                  <div
                    key={cat.categoryName}
                    className={`rounded-lg border p-4 ${
                      isContra
                        ? 'bg-blue-50 border-blue-200'
                        : 'bg-amber-50 border-amber-200'
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <span className={`text-sm font-medium ${isContra ? 'text-blue-800' : 'text-amber-800'}`}>
                        {cat.categoryName}
                      </span>
                      <span className={`text-xs px-2 py-1 rounded ${isContra ? 'bg-blue-100 text-blue-700' : 'bg-amber-100 text-amber-700'}`}>
                        {cat.count} account{cat.count > 1 ? 's' : ''}
                      </span>
                    </div>
                    <div className={`text-lg font-semibold mt-2 ${isContra ? 'text-blue-900' : 'text-amber-900'}`}>
                      {formatCurrency(cat.totalAmount)}
                    </div>
                  </div>
                )
              })}
            </div>
          )}

          {/* Expand/Collapse Controls */}
          {filteredItems.length > 0 && (
            <div className="flex gap-2 print:hidden">
              <button
                onClick={expandAll}
                className="text-sm text-blue-600 hover:underline"
              >
                Expand All
              </button>
              <span className="text-gray-400">|</span>
              <button
                onClick={collapseAll}
                className="text-sm text-blue-600 hover:underline"
              >
                Collapse All
              </button>
            </div>
          )}

          {/* Grouped Items */}
          <div className="space-y-4">
            {Object.entries(groupedItems).map(([category, items]) => {
              const isExpanded = expandedCategories.has(category)
              const isContra = category.includes('Contra')
              const categoryTotal = items.reduce((sum, item) => sum + item.amount, 0)

              return (
                <div key={category} className="bg-white rounded-lg shadow overflow-hidden">
                  {/* Category Header */}
                  <button
                    onClick={() => toggleCategory(category)}
                    className={`w-full px-4 py-3 flex items-center justify-between hover:bg-gray-50 ${
                      isContra ? 'bg-blue-50' : 'bg-amber-50'
                    }`}
                  >
                    <div className="flex items-center gap-3">
                      {isExpanded ? <ChevronDown size={20} /> : <ChevronRight size={20} />}
                      <span className={`font-medium ${isContra ? 'text-blue-800' : 'text-amber-800'}`}>
                        {category}
                      </span>
                      <span className={`text-sm px-2 py-0.5 rounded ${
                        isContra ? 'bg-blue-100 text-blue-700' : 'bg-amber-100 text-amber-700'
                      }`}>
                        {items.length} account{items.length > 1 ? 's' : ''}
                      </span>
                    </div>
                    <span className={`font-semibold ${isContra ? 'text-blue-800' : 'text-amber-800'}`}>
                      {formatCurrency(categoryTotal)}
                    </span>
                  </button>

                  {/* Category Items */}
                  {isExpanded && (
                    <div className="border-t">
                      <table className="w-full">
                        <thead className="bg-gray-50">
                          <tr>
                            <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Account</th>
                            <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
                            <th className="px-4 py-2 text-center text-xs font-medium text-gray-500 uppercase">Expected</th>
                            <th className="px-4 py-2 text-center text-xs font-medium text-gray-500 uppercase">Actual</th>
                            <th className="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase">Amount</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-200">
                          {items.map((item) => {
                            const typeConfig = accountTypeConfig[item.accountType]
                            const SeverityIcon = severityConfig[item.severity]?.icon || Info

                            return (
                              <tr key={item.accountId} className="hover:bg-gray-50">
                                <td className="px-4 py-3">
                                  <div className="flex items-start gap-2">
                                    <SeverityIcon size={16} className={item.isContraAccount ? 'text-blue-500 mt-0.5' : 'text-amber-500 mt-0.5'} />
                                    <div>
                                      <div className="font-mono text-sm text-gray-600">{item.accountCode}</div>
                                      <div className="text-sm font-medium text-gray-900">{item.accountName}</div>
                                    </div>
                                  </div>
                                </td>
                                <td className="px-4 py-3">
                                  <span className={`inline-flex items-center gap-1 text-sm ${typeConfig?.color || 'text-gray-600'}`}>
                                    {typeConfig?.icon && <typeConfig.icon size={14} />}
                                    {typeConfig?.label || item.accountType}
                                  </span>
                                </td>
                                <td className="px-4 py-3 text-center">
                                  <span className="px-2 py-1 rounded text-xs font-medium bg-gray-100 text-gray-700">
                                    {item.expectedBalanceSide.toUpperCase()}
                                  </span>
                                </td>
                                <td className="px-4 py-3 text-center">
                                  <span className={`px-2 py-1 rounded text-xs font-medium ${
                                    item.isContraAccount
                                      ? 'bg-blue-100 text-blue-700'
                                      : 'bg-amber-100 text-amber-700'
                                  }`}>
                                    {item.actualBalanceSide.toUpperCase()}
                                  </span>
                                </td>
                                <td className="px-4 py-3 text-right font-medium">
                                  {formatCurrency(item.amount)}
                                </td>
                              </tr>
                            )
                          })}
                        </tbody>
                      </table>

                      {/* Recommendations */}
                      {!isContra && items.length > 0 && (
                        <div className="px-4 py-3 bg-gray-50 border-t">
                          <div className="flex items-start gap-2">
                            <Lightbulb size={16} className="text-amber-500 mt-0.5 flex-shrink-0" />
                            <div className="text-sm">
                              <span className="font-medium text-gray-700">Recommendation: </span>
                              <span className="text-gray-600">{items[0].recommendedAction}</span>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )
            })}
          </div>

          {/* Empty State */}
          {filteredItems.length === 0 && (
            <div className="bg-white rounded-lg shadow p-6">
              <div className="text-center py-12">
                <CheckCircle className="mx-auto h-12 w-12 text-green-400" />
                <h3 className="mt-2 text-lg font-medium text-gray-900">All Clear!</h3>
                <p className="mt-1 text-sm text-gray-500">
                  No abnormal balances found. All accounts have expected balance directions.
                </p>
              </div>
            </div>
          )}

          {/* Report Footer */}
          <div className="text-center text-sm text-gray-500 print:mt-8">
            <p>Report generated on {format(new Date(), 'dd MMM yyyy HH:mm')}</p>
            <p className="mt-1">
              Total accounts reviewed: {report.totalAbnormalAccounts} |
              Actionable issues: {report.actionableIssues} |
              Contra accounts: {report.totalAbnormalAccounts - report.actionableIssues}
            </p>
          </div>
        </>
      )}
    </div>
  )
}

export default AbnormalBalanceReport

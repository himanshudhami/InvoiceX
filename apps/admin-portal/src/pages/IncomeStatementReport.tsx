import { useState, useMemo } from 'react'
import { useIncomeStatement } from '@/features/ledger/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { IncomeStatementSection, IncomeStatementRow } from '@/services/api/types'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { format, startOfMonth, endOfMonth, subMonths } from 'date-fns'
import {
  FileText,
  Download,
  Printer,
  TrendingUp,
  TrendingDown,
  DollarSign,
  ArrowUpCircle,
  ArrowDownCircle
} from 'lucide-react'

const IncomeStatementReport = () => {
  const [companyFilter, setCompanyFilter] = useState<string>('')
  const today = new Date()
  const [fromDate, setFromDate] = useState(format(startOfMonth(subMonths(today, 11)), 'yyyy-MM-dd'))
  const [toDate, setToDate] = useState(format(endOfMonth(today), 'yyyy-MM-dd'))
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set(['income', 'expenses']))

  const { data: companies = [] } = useCompanies()
  const { data: report, isLoading, error, refetch } = useIncomeStatement(
    companyFilter,
    fromDate,
    toDate,
    !!companyFilter && !!fromDate && !!toDate
  )

  const selectedCompany = companies.find(c => c.id === companyFilter)

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(amount)
  }

  const formatDate = (dateStr: string) => {
    try {
      return format(new Date(dateStr), 'dd MMMM yyyy')
    } catch {
      return dateStr
    }
  }

  const toggleSection = (section: string) => {
    const newExpanded = new Set(expandedSections)
    if (newExpanded.has(section)) {
      newExpanded.delete(section)
    } else {
      newExpanded.add(section)
    }
    setExpandedSections(newExpanded)
  }

  const handlePrint = () => {
    window.print()
  }

  const handleExport = () => {
    if (!report) return

    const lines: string[] = [
      `Income Statement (Profit & Loss)`,
      `Company: ${selectedCompany?.name || ''}`,
      `Period: ${formatDate(fromDate)} to ${formatDate(toDate)}`,
      '',
      'Account Code,Account Name,Amount',
      '',
      '=== INCOME ===',
    ]

    report.incomeSections.forEach(section => {
      lines.push(`--- ${section.sectionName} ---`)
      section.rows.forEach(row => {
        lines.push(`${row.accountCode},${row.accountName},${row.amount.toFixed(2)}`)
      })
      lines.push(`,${section.sectionName} Total,${section.sectionTotal.toFixed(2)}`)
    })

    lines.push(`,,TOTAL INCOME,${report.totalIncome.toFixed(2)}`)
    lines.push('')
    lines.push('=== EXPENSES ===')

    report.expenseSections.forEach(section => {
      lines.push(`--- ${section.sectionName} ---`)
      section.rows.forEach(row => {
        lines.push(`${row.accountCode},${row.accountName},${row.amount.toFixed(2)}`)
      })
      lines.push(`,,${section.sectionName} Total,${section.sectionTotal.toFixed(2)}`)
    })

    lines.push(`,,TOTAL EXPENSES,${report.totalExpenses.toFixed(2)}`)
    lines.push('')
    lines.push(`,,NET INCOME,${report.netIncome.toFixed(2)}`)

    const csvContent = lines.join('\n')
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `income-statement-${fromDate}-to-${toDate}.csv`
    link.click()
  }

  const setQuickDateRange = (range: string) => {
    const now = new Date()
    let from: Date
    let to: Date

    switch (range) {
      case 'mtd':
        from = startOfMonth(now)
        to = now
        break
      case 'lastMonth':
        from = startOfMonth(subMonths(now, 1))
        to = endOfMonth(subMonths(now, 1))
        break
      case 'ytd':
        // Indian FY starts April
        const fyStart = now.getMonth() >= 3
          ? new Date(now.getFullYear(), 3, 1)
          : new Date(now.getFullYear() - 1, 3, 1)
        from = fyStart
        to = now
        break
      case 'lastFy':
        const lastFyStart = now.getMonth() >= 3
          ? new Date(now.getFullYear() - 1, 3, 1)
          : new Date(now.getFullYear() - 2, 3, 1)
        const lastFyEnd = now.getMonth() >= 3
          ? new Date(now.getFullYear(), 2, 31)
          : new Date(now.getFullYear() - 1, 2, 31)
        from = lastFyStart
        to = lastFyEnd
        break
      default:
        return
    }

    setFromDate(format(from, 'yyyy-MM-dd'))
    setToDate(format(to, 'yyyy-MM-dd'))
  }

  if (!companyFilter) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Income Statement</h1>
          <p className="text-gray-600 mt-2">View profit and loss for a specific period</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <FileText className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">Select a Company</h3>
            <p className="mt-1 text-sm text-gray-500">Please select a company to view its income statement</p>
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
          <h1 className="text-3xl font-bold text-gray-900">Income Statement</h1>
          <p className="text-gray-600 mt-2">Profit & Loss Report</p>
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
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">From Date</label>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="block w-48 rounded-md border-gray-300 shadow-sm focus:border-primary focus:ring-primary sm:text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">To Date</label>
            <input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="block w-48 rounded-md border-gray-300 shadow-sm focus:border-primary focus:ring-primary sm:text-sm"
            />
          </div>
          <div className="flex gap-1">
            <button
              onClick={() => setQuickDateRange('mtd')}
              className="px-3 py-2 text-xs font-medium text-gray-600 bg-gray-100 rounded hover:bg-gray-200"
            >
              MTD
            </button>
            <button
              onClick={() => setQuickDateRange('lastMonth')}
              className="px-3 py-2 text-xs font-medium text-gray-600 bg-gray-100 rounded hover:bg-gray-200"
            >
              Last Month
            </button>
            <button
              onClick={() => setQuickDateRange('ytd')}
              className="px-3 py-2 text-xs font-medium text-gray-600 bg-gray-100 rounded hover:bg-gray-200"
            >
              YTD
            </button>
            <button
              onClick={() => setQuickDateRange('lastFy')}
              className="px-3 py-2 text-xs font-medium text-gray-600 bg-gray-100 rounded hover:bg-gray-200"
            >
              Last FY
            </button>
          </div>
        </div>
      </div>

      {/* Print Header */}
      <div className="hidden print:block text-center mb-4">
        <h1 className="text-2xl font-bold">{selectedCompany?.name}</h1>
        <h2 className="text-xl mt-2">Income Statement (Profit & Loss)</h2>
        <p className="text-gray-600">For the period {formatDate(fromDate)} to {formatDate(toDate)}</p>
      </div>

      {/* Loading / Error */}
      {isLoading && (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      )}

      {error && (
        <div className="text-center py-12">
          <div className="text-red-600 mb-4">Failed to load income statement</div>
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
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 print:hidden">
            <div className="bg-green-50 rounded-lg p-4 border border-green-200">
              <div className="flex items-center gap-3">
                <ArrowUpCircle className="h-8 w-8 text-green-600" />
                <div>
                  <p className="text-sm text-green-600">Total Income</p>
                  <p className="text-2xl font-bold text-green-700">{formatCurrency(report.totalIncome)}</p>
                </div>
              </div>
            </div>
            <div className="bg-red-50 rounded-lg p-4 border border-red-200">
              <div className="flex items-center gap-3">
                <ArrowDownCircle className="h-8 w-8 text-red-600" />
                <div>
                  <p className="text-sm text-red-600">Total Expenses</p>
                  <p className="text-2xl font-bold text-red-700">{formatCurrency(report.totalExpenses)}</p>
                </div>
              </div>
            </div>
            <div className={`rounded-lg p-4 border ${report.netIncome >= 0 ? 'bg-blue-50 border-blue-200' : 'bg-orange-50 border-orange-200'}`}>
              <div className="flex items-center gap-3">
                <DollarSign className={`h-8 w-8 ${report.netIncome >= 0 ? 'text-blue-600' : 'text-orange-600'}`} />
                <div>
                  <p className={`text-sm ${report.netIncome >= 0 ? 'text-blue-600' : 'text-orange-600'}`}>
                    Net {report.netIncome >= 0 ? 'Profit' : 'Loss'}
                  </p>
                  <p className={`text-2xl font-bold ${report.netIncome >= 0 ? 'text-blue-700' : 'text-orange-700'}`}>
                    {formatCurrency(Math.abs(report.netIncome))}
                  </p>
                </div>
              </div>
            </div>
          </div>

          {/* Income Section */}
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <button
              onClick={() => toggleSection('income')}
              className="w-full px-4 py-3 bg-green-600 text-white flex items-center justify-between"
            >
              <div className="flex items-center gap-2">
                <TrendingUp size={20} />
                <span className="font-bold text-lg">INCOME</span>
              </div>
              <span className="text-lg font-bold">{formatCurrency(report.totalIncome)}</span>
            </button>

            {expandedSections.has('income') && (
              <div className="p-4">
                {report.incomeSections.map((section, sIdx) => (
                  <div key={sIdx} className="mb-4 last:mb-0">
                    <div className="flex items-center justify-between py-2 border-b border-gray-200 bg-gray-50 px-3 rounded">
                      <span className="font-semibold text-gray-700">{section.sectionName}</span>
                      <span className="font-semibold text-green-600">{formatCurrency(section.sectionTotal)}</span>
                    </div>
                    <table className="w-full mt-2">
                      <tbody>
                        {section.rows.map((row) => (
                          <tr key={row.accountId} className="hover:bg-gray-50">
                            <td className="py-2 pl-6 text-sm font-mono text-gray-500">{row.accountCode}</td>
                            <td className="py-2 text-sm text-gray-900">{row.accountName}</td>
                            <td className="py-2 pr-3 text-sm text-right text-green-600 font-medium">
                              {formatCurrency(row.amount)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Expenses Section */}
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <button
              onClick={() => toggleSection('expenses')}
              className="w-full px-4 py-3 bg-red-600 text-white flex items-center justify-between"
            >
              <div className="flex items-center gap-2">
                <TrendingDown size={20} />
                <span className="font-bold text-lg">EXPENSES</span>
              </div>
              <span className="text-lg font-bold">{formatCurrency(report.totalExpenses)}</span>
            </button>

            {expandedSections.has('expenses') && (
              <div className="p-4">
                {report.expenseSections.map((section, sIdx) => (
                  <div key={sIdx} className="mb-4 last:mb-0">
                    <div className="flex items-center justify-between py-2 border-b border-gray-200 bg-gray-50 px-3 rounded">
                      <span className="font-semibold text-gray-700">{section.sectionName}</span>
                      <span className="font-semibold text-red-600">{formatCurrency(section.sectionTotal)}</span>
                    </div>
                    <table className="w-full mt-2">
                      <tbody>
                        {section.rows.map((row) => (
                          <tr key={row.accountId} className="hover:bg-gray-50">
                            <td className="py-2 pl-6 text-sm font-mono text-gray-500">{row.accountCode}</td>
                            <td className="py-2 text-sm text-gray-900">{row.accountName}</td>
                            <td className="py-2 pr-3 text-sm text-right text-red-600 font-medium">
                              {formatCurrency(row.amount)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Net Income Footer */}
          <div className={`rounded-lg p-6 ${report.netIncome >= 0 ? 'bg-blue-900' : 'bg-orange-900'} text-white`}>
            <div className="flex items-center justify-between">
              <span className="text-xl font-bold">
                NET {report.netIncome >= 0 ? 'PROFIT' : 'LOSS'}
              </span>
              <span className="text-3xl font-bold">
                {formatCurrency(Math.abs(report.netIncome))}
              </span>
            </div>
            <p className="mt-2 text-sm opacity-80">
              For the period {formatDate(fromDate)} to {formatDate(toDate)}
            </p>
          </div>

          {/* Report Footer */}
          <div className="text-center text-sm text-gray-500 print:mt-8">
            <p>Generated on {format(new Date(), 'dd MMM yyyy HH:mm')}</p>
          </div>
        </>
      )}

      {/* Empty State */}
      {report && report.incomeSections.length === 0 && report.expenseSections.length === 0 && !isLoading && (
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <FileText className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">No Data</h3>
            <p className="mt-1 text-sm text-gray-500">
              No income or expense data found for the selected period.
            </p>
          </div>
        </div>
      )}
    </div>
  )
}

export default IncomeStatementReport

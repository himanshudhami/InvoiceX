import { useState } from 'react'
import { useBalanceSheet } from '@/features/ledger/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { BalanceSheetSection } from '@/services/api/types'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { format } from 'date-fns'
import {
  FileText,
  Download,
  Printer,
  CheckCircle,
  AlertTriangle,
  Wallet,
  CreditCard,
  Scale,
  TrendingUp,
  ChevronDown,
  ChevronRight
} from 'lucide-react'

const BalanceSheetReport = () => {
  const [companyFilter, setCompanyFilter] = useState<string>('')
  const [asOfDate, setAsOfDate] = useState(format(new Date(), 'yyyy-MM-dd'))
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set(['assets', 'liabilities', 'equity']))

  const { data: companies = [] } = useCompanies()
  const { data: report, isLoading, error, refetch } = useBalanceSheet(
    companyFilter,
    asOfDate,
    !!companyFilter && !!asOfDate
  )

  const selectedCompany = companies.find(c => c.id === companyFilter)

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(Math.abs(amount))
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
      `Balance Sheet`,
      `Company: ${selectedCompany?.name || ''}`,
      `As of: ${formatDate(asOfDate)}`,
      '',
      'Account Code,Account Name,Amount',
      '',
      '=== ASSETS ===',
    ]

    report.assetSections.forEach(section => {
      lines.push(`--- ${section.sectionName} ---`)
      section.rows.forEach(row => {
        lines.push(`${row.accountCode},${row.accountName},${row.balance.toFixed(2)}`)
      })
      lines.push(`,,${section.sectionName} Total,${section.sectionTotal.toFixed(2)}`)
    })

    lines.push(`,,TOTAL ASSETS,${report.totalAssets.toFixed(2)}`)
    lines.push('')
    lines.push('=== LIABILITIES ===')

    report.liabilitySections.forEach(section => {
      lines.push(`--- ${section.sectionName} ---`)
      section.rows.forEach(row => {
        lines.push(`${row.accountCode},${row.accountName},${row.balance.toFixed(2)}`)
      })
      lines.push(`,,${section.sectionName} Total,${section.sectionTotal.toFixed(2)}`)
    })

    lines.push(`,,TOTAL LIABILITIES,${report.totalLiabilities.toFixed(2)}`)
    lines.push('')
    lines.push('=== EQUITY ===')

    report.equitySections.forEach(section => {
      lines.push(`--- ${section.sectionName} ---`)
      section.rows.forEach(row => {
        lines.push(`${row.accountCode},${row.accountName},${row.balance.toFixed(2)}`)
      })
      lines.push(`,,${section.sectionName} Total,${section.sectionTotal.toFixed(2)}`)
    })

    lines.push(`,,TOTAL EQUITY,${report.totalEquity.toFixed(2)}`)
    lines.push('')
    lines.push(`,,TOTAL LIABILITIES + EQUITY,${(report.totalLiabilities + report.totalEquity).toFixed(2)}`)

    const csvContent = lines.join('\n')
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `balance-sheet-${asOfDate}.csv`
    link.click()
  }

  const renderSection = (
    title: string,
    sections: BalanceSheetSection[],
    total: number,
    colorClass: string,
    icon: React.ElementType,
    sectionKey: string
  ) => {
    const Icon = icon
    const isExpanded = expandedSections.has(sectionKey)

    return (
      <div className="bg-white rounded-lg shadow overflow-hidden mb-4">
        <button
          onClick={() => toggleSection(sectionKey)}
          className={`w-full px-4 py-3 ${colorClass} text-white flex items-center justify-between`}
        >
          <div className="flex items-center gap-2">
            {isExpanded ? <ChevronDown size={20} /> : <ChevronRight size={20} />}
            <Icon size={20} />
            <span className="font-bold text-lg">{title}</span>
          </div>
          <span className="text-lg font-bold">{formatCurrency(total)}</span>
        </button>

        {isExpanded && (
          <div className="p-4">
            {sections.map((section, sIdx) => (
              <div key={sIdx} className="mb-4 last:mb-0">
                <div className="flex items-center justify-between py-2 border-b border-gray-200 bg-gray-50 px-3 rounded">
                  <span className="font-semibold text-gray-700">{section.sectionName}</span>
                  <span className="font-semibold text-gray-800">{formatCurrency(section.sectionTotal)}</span>
                </div>
                <table className="w-full mt-2">
                  <tbody>
                    {section.rows.map((row) => (
                      <tr key={row.accountId} className="hover:bg-gray-50">
                        <td className="py-2 pl-6 text-sm font-mono text-gray-500">{row.accountCode}</td>
                        <td className="py-2 text-sm text-gray-900">{row.accountName}</td>
                        <td className="py-2 pr-3 text-sm text-right font-medium">
                          {formatCurrency(row.balance)}
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
    )
  }

  if (!companyFilter) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Balance Sheet</h1>
          <p className="text-gray-600 mt-2">View assets, liabilities, and equity as of a specific date</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <FileText className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">Select a Company</h3>
            <p className="mt-1 text-sm text-gray-500">Please select a company to view its balance sheet</p>
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
          <h1 className="text-3xl font-bold text-gray-900">Balance Sheet</h1>
          <p className="text-gray-600 mt-2">Statement of Financial Position</p>
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
            <label className="block text-sm font-medium text-gray-700 mb-2">As of Date</label>
            <input
              type="date"
              value={asOfDate}
              onChange={(e) => setAsOfDate(e.target.value)}
              className="block w-48 rounded-md border-gray-300 shadow-sm focus:border-primary focus:ring-primary sm:text-sm"
            />
          </div>
        </div>
      </div>

      {/* Print Header */}
      <div className="hidden print:block text-center mb-4">
        <h1 className="text-2xl font-bold">{selectedCompany?.name}</h1>
        <h2 className="text-xl mt-2">Balance Sheet</h2>
        <p className="text-gray-600">As of {formatDate(asOfDate)}</p>
      </div>

      {/* Loading / Error */}
      {isLoading && (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      )}

      {error && (
        <div className="text-center py-12">
          <div className="text-red-600 mb-4">Failed to load balance sheet</div>
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
          {/* Balance Status Banner */}
          <div className={`rounded-lg p-4 flex items-center gap-3 ${
            report.isBalanced
              ? 'bg-green-50 border border-green-200'
              : 'bg-red-50 border border-red-200'
          }`}>
            {report.isBalanced ? (
              <>
                <CheckCircle className="h-6 w-6 text-green-600" />
                <div>
                  <span className="font-medium text-green-800">Balance Sheet is Balanced</span>
                  <span className="text-green-600 ml-2">
                    Assets = Liabilities + Equity
                  </span>
                </div>
              </>
            ) : (
              <>
                <AlertTriangle className="h-6 w-6 text-red-600" />
                <div>
                  <span className="font-medium text-red-800">Balance Sheet is NOT Balanced</span>
                  <span className="text-red-600 ml-2">
                    Difference: {formatCurrency(Math.abs(report.totalAssets - (report.totalLiabilities + report.totalEquity)))}
                  </span>
                </div>
              </>
            )}
          </div>

          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 print:hidden">
            <div className="bg-blue-50 rounded-lg p-4 border border-blue-200">
              <div className="flex items-center gap-3">
                <Wallet className="h-8 w-8 text-blue-600" />
                <div>
                  <p className="text-sm text-blue-600">Total Assets</p>
                  <p className="text-xl font-bold text-blue-700">{formatCurrency(report.totalAssets)}</p>
                </div>
              </div>
            </div>
            <div className="bg-red-50 rounded-lg p-4 border border-red-200">
              <div className="flex items-center gap-3">
                <CreditCard className="h-8 w-8 text-red-600" />
                <div>
                  <p className="text-sm text-red-600">Total Liabilities</p>
                  <p className="text-xl font-bold text-red-700">{formatCurrency(report.totalLiabilities)}</p>
                </div>
              </div>
            </div>
            <div className="bg-purple-50 rounded-lg p-4 border border-purple-200">
              <div className="flex items-center gap-3">
                <TrendingUp className="h-8 w-8 text-purple-600" />
                <div>
                  <p className="text-sm text-purple-600">Total Equity</p>
                  <p className="text-xl font-bold text-purple-700">{formatCurrency(report.totalEquity)}</p>
                </div>
              </div>
            </div>
            <div className="bg-gray-50 rounded-lg p-4 border border-gray-200">
              <div className="flex items-center gap-3">
                <Scale className="h-8 w-8 text-gray-600" />
                <div>
                  <p className="text-sm text-gray-600">L + E</p>
                  <p className="text-xl font-bold text-gray-700">{formatCurrency(report.totalLiabilities + report.totalEquity)}</p>
                </div>
              </div>
            </div>
          </div>

          {/* Balance Sheet Sections */}
          {renderSection('ASSETS', report.assetSections, report.totalAssets, 'bg-blue-600', Wallet, 'assets')}
          {renderSection('LIABILITIES', report.liabilitySections, report.totalLiabilities, 'bg-red-600', CreditCard, 'liabilities')}
          {renderSection('EQUITY', report.equitySections, report.totalEquity, 'bg-purple-600', TrendingUp, 'equity')}

          {/* Summary Footer */}
          <div className="bg-gray-900 rounded-lg p-6 text-white">
            <div className="grid grid-cols-2 gap-4">
              <div className="text-center border-r border-gray-700">
                <p className="text-sm text-gray-400">TOTAL ASSETS</p>
                <p className="text-2xl font-bold">{formatCurrency(report.totalAssets)}</p>
              </div>
              <div className="text-center">
                <p className="text-sm text-gray-400">TOTAL LIABILITIES + EQUITY</p>
                <p className="text-2xl font-bold">{formatCurrency(report.totalLiabilities + report.totalEquity)}</p>
              </div>
            </div>
          </div>

          {/* Report Footer */}
          <div className="text-center text-sm text-gray-500 print:mt-8">
            <p>As of {formatDate(asOfDate)}</p>
            <p className="mt-1">Generated on {format(new Date(), 'dd MMM yyyy HH:mm')}</p>
          </div>
        </>
      )}

      {/* Empty State */}
      {report && report.assetSections.length === 0 && report.liabilitySections.length === 0 && report.equitySections.length === 0 && !isLoading && (
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <FileText className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">No Data</h3>
            <p className="mt-1 text-sm text-gray-500">
              No balance sheet data found. Please ensure chart of accounts is initialized.
            </p>
          </div>
        </div>
      )}
    </div>
  )
}

export default BalanceSheetReport

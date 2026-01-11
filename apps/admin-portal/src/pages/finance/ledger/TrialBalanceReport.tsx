import React, { useState, useMemo } from 'react'
import { useQueryStates, parseAsString, parseAsBoolean } from 'nuqs'
import { useTrialBalance } from '@/features/ledger/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { TrialBalanceRow, AccountType } from '@/services/api/types'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import SubledgerDrilldownDrawer from '@/components/ledger/SubledgerDrilldownDrawer'
import { format } from 'date-fns'
import {
  FileText,
  Download,
  Printer,
  CheckCircle,
  AlertTriangle,
  TrendingUp,
  TrendingDown,
  Wallet,
  DollarSign,
  BookOpen,
  ChevronRight
} from 'lucide-react'

const accountTypeConfig: Record<AccountType, { label: string; color: string; icon: React.ElementType }> = {
  asset: { label: 'Assets', color: 'text-blue-600', icon: Wallet },
  liability: { label: 'Liabilities', color: 'text-red-600', icon: TrendingDown },
  equity: { label: 'Equity', color: 'text-purple-600', icon: BookOpen },
  income: { label: 'Income', color: 'text-green-600', icon: TrendingUp },
  expense: { label: 'Expenses', color: 'text-orange-600', icon: DollarSign },
}

const TrialBalanceReport = () => {
  // URL-backed filter state with nuqs
  const [filters, setFilters] = useQueryStates({
    company: parseAsString.withDefault(''),
    asOfDate: parseAsString.withDefault(format(new Date(), 'yyyy-MM-dd')),
    includeZero: parseAsBoolean.withDefault(false),
    groupByType: parseAsBoolean.withDefault(true),
  })

  const { company: companyFilter, asOfDate, includeZero: includeZeroBalances, groupByType } = filters

  // Subledger drill-down state (not in URL - ephemeral UI state)
  const [selectedControlAccount, setSelectedControlAccount] = useState<{
    id: string
    name: string
  } | null>(null)

  const { data: companies = [] } = useCompanies()
  const { data: report, isLoading, error, refetch } = useTrialBalance(
    companyFilter,
    asOfDate,
    includeZeroBalances,
    !!companyFilter && !!asOfDate
  )

  // Handle control account click for drill-down
  const handleControlAccountClick = (row: TrialBalanceRow) => {
    if (row.isControlAccount) {
      setSelectedControlAccount({ id: row.accountId, name: row.accountName })
    }
  }

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

  // Group rows by account type if enabled
  const groupedRows = useMemo(() => {
    if (!report?.rows) return {}

    if (!groupByType) {
      return { all: report.rows }
    }

    return report.rows.reduce((acc, row) => {
      const type = row.accountType
      if (!acc[type]) acc[type] = []
      acc[type].push(row)
      return acc
    }, {} as Record<string, TrialBalanceRow[]>)
  }, [report?.rows, groupByType])

  // Calculate type totals
  const typeTotals = useMemo(() => {
    if (!report?.rows) return {}

    return report.rows.reduce((acc, row) => {
      const type = row.accountType
      if (!acc[type]) acc[type] = { debit: 0, credit: 0 }
      acc[type].debit += row.debitBalance
      acc[type].credit += row.creditBalance
      return acc
    }, {} as Record<string, { debit: number; credit: number }>)
  }, [report?.rows])

  const handlePrint = () => {
    window.print()
  }

  const handleExport = () => {
    if (!report) return

    const headers = ['Account Code', 'Account Name', 'Type', 'Debit', 'Credit']
    const rows = report.rows.map(row => [
      row.accountCode,
      row.accountName,
      row.accountType,
      row.debitBalance > 0 ? row.debitBalance.toFixed(2) : '',
      row.creditBalance > 0 ? row.creditBalance.toFixed(2) : ''
    ])

    // Add totals row
    rows.push(['', 'TOTAL', '', report.totalDebits.toFixed(2), report.totalCredits.toFixed(2)])

    const csvContent = [
      `Trial Balance as of ${formatDate(asOfDate)}`,
      `Company: ${selectedCompany?.name || ''}`,
      '',
      headers.join(','),
      ...rows.map(row => row.join(','))
    ].join('\n')

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `trial-balance-${asOfDate}.csv`
    link.click()
  }

  if (!companyFilter) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Trial Balance</h1>
          <p className="text-gray-600 mt-2">View account balances as of a specific date</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <FileText className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">Select a Company</h3>
            <p className="mt-1 text-sm text-gray-500">Please select a company to view its trial balance</p>
            <div className="mt-6 flex justify-center">
              <CompanyFilterDropdown
                value={companyFilter}
                onChange={(value) => setFilters({ company: value })}
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
          <h1 className="text-3xl font-bold text-gray-900">Trial Balance</h1>
          <p className="text-gray-600 mt-2">View account balances as of a specific date</p>
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
              onChange={(value) => setFilters({ company: value })}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">As of Date</label>
            <input
              type="date"
              value={asOfDate}
              onChange={(e) => setFilters({ asOfDate: e.target.value })}
              className="block w-48 rounded-md border-gray-300 shadow-sm focus:border-primary focus:ring-primary sm:text-sm"
            />
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="includeZero"
              checked={includeZeroBalances}
              onChange={(e) => setFilters({ includeZero: e.target.checked })}
              className="rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="includeZero" className="text-sm text-gray-700">
              Include zero balances
            </label>
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="groupByType"
              checked={groupByType}
              onChange={(e) => setFilters({ groupByType: e.target.checked })}
              className="rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="groupByType" className="text-sm text-gray-700">
              Group by account type
            </label>
          </div>
        </div>
      </div>

      {/* Print Header */}
      <div className="hidden print:block text-center mb-4">
        <h1 className="text-2xl font-bold">{selectedCompany?.name}</h1>
        <h2 className="text-xl mt-2">Trial Balance</h2>
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
          <div className="text-red-600 mb-4">Failed to load trial balance</div>
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
                  <span className="font-medium text-green-800">Trial Balance is Balanced</span>
                  <span className="text-green-600 ml-2">
                    Total: {formatCurrency(report.totalDebits)}
                  </span>
                </div>
              </>
            ) : (
              <>
                <AlertTriangle className="h-6 w-6 text-red-600" />
                <div>
                  <span className="font-medium text-red-800">Trial Balance is NOT Balanced</span>
                  <span className="text-red-600 ml-2">
                    Difference: {formatCurrency(Math.abs(report.totalDebits - report.totalCredits))}
                  </span>
                </div>
              </>
            )}
          </div>

          {/* Trial Balance Table */}
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50 print:bg-gray-200">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Account Code
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Account Name
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-36">
                    Debit (Dr)
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-36">
                    Credit (Cr)
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {groupByType ? (
                  // Grouped by type
                  Object.entries(groupedRows).map(([type, rows]) => {
                    const config = accountTypeConfig[type as AccountType]
                    const totals = typeTotals[type]
                    return (
                      <React.Fragment key={type}>
                        {/* Type Header */}
                        <tr className="bg-gray-50">
                          <td colSpan={4} className="px-4 py-2">
                            <div className={`flex items-center gap-2 font-semibold ${config?.color || 'text-gray-900'}`}>
                              {config?.icon && <config.icon size={16} />}
                              {config?.label || type}
                            </div>
                          </td>
                        </tr>
                        {/* Rows */}
                        {rows.map((row) => (
                          <tr
                            key={row.accountId}
                            className={`hover:bg-gray-50 ${row.isControlAccount ? 'cursor-pointer' : ''}`}
                            onClick={() => handleControlAccountClick(row)}
                          >
                            <td className="px-4 py-3 text-sm font-mono text-gray-600" style={{ paddingLeft: `${(row.depthLevel + 1) * 16}px` }}>
                              {row.accountCode}
                            </td>
                            <td className="px-4 py-3 text-sm text-gray-900">
                              <span className="flex items-center gap-2">
                                {row.accountName}
                                {row.isControlAccount && (
                                  <ChevronRight size={14} className="text-blue-500" />
                                )}
                              </span>
                            </td>
                            <td className="px-4 py-3 text-sm text-right font-medium text-blue-600 w-36">
                              {row.debitBalance > 0 ? formatCurrency(row.debitBalance) : ''}
                            </td>
                            <td className="px-4 py-3 text-sm text-right font-medium text-green-600 w-36">
                              {row.creditBalance > 0 ? formatCurrency(row.creditBalance) : ''}
                            </td>
                          </tr>
                        ))}
                        {/* Type Subtotal */}
                        <tr className="bg-gray-100 font-medium">
                          <td colSpan={2} className="px-4 py-2 text-right text-sm text-gray-600">
                            Subtotal - {config?.label}:
                          </td>
                          <td className="px-4 py-2 text-right text-sm text-blue-700 w-36">
                            {totals?.debit ? formatCurrency(totals.debit) : ''}
                          </td>
                          <td className="px-4 py-2 text-right text-sm text-green-700 w-36">
                            {totals?.credit ? formatCurrency(totals.credit) : ''}
                          </td>
                        </tr>
                      </React.Fragment>
                    )
                  })
                ) : (
                  // Flat list
                  report.rows.map((row) => (
                    <tr
                      key={row.accountId}
                      className={`hover:bg-gray-50 ${row.isControlAccount ? 'cursor-pointer' : ''}`}
                      onClick={() => handleControlAccountClick(row)}
                    >
                      <td className="px-4 py-3 text-sm font-mono text-gray-600" style={{ paddingLeft: `${(row.depthLevel + 1) * 16}px` }}>
                        {row.accountCode}
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-900">
                        <span className="flex items-center gap-2">
                          {row.accountName}
                          {row.isControlAccount && (
                            <ChevronRight size={14} className="text-blue-500" />
                          )}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-sm text-right font-medium text-blue-600 w-36">
                        {row.debitBalance > 0 ? formatCurrency(row.debitBalance) : ''}
                      </td>
                      <td className="px-4 py-3 text-sm text-right font-medium text-green-600 w-36">
                        {row.creditBalance > 0 ? formatCurrency(row.creditBalance) : ''}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
              <tfoot className="bg-gray-900 text-white">
                <tr>
                  <td colSpan={2} className="px-4 py-3 text-right font-bold">
                    GRAND TOTAL
                  </td>
                  <td className="px-4 py-3 text-right font-bold w-36">
                    {formatCurrency(report.totalDebits)}
                  </td>
                  <td className="px-4 py-3 text-right font-bold w-36">
                    {formatCurrency(report.totalCredits)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>

          {/* Report Footer */}
          <div className="text-center text-sm text-gray-500 print:mt-8">
            <p>Financial Year: {report.financialYear}</p>
            <p className="mt-1">Generated on {format(new Date(), 'dd MMM yyyy HH:mm')}</p>
          </div>
        </>
      )}

      {/* Empty State */}
      {report && report.rows.length === 0 && !isLoading && (
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <FileText className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">No Data</h3>
            <p className="mt-1 text-sm text-gray-500">
              No accounts with balances found for the selected period.
            </p>
          </div>
        </div>
      )}

      {/* Subledger Drill-Down Drawer */}
      <SubledgerDrilldownDrawer
        isOpen={!!selectedControlAccount}
        onClose={() => setSelectedControlAccount(null)}
        companyId={companyFilter}
        controlAccountId={selectedControlAccount?.id || ''}
        controlAccountName={selectedControlAccount?.name || ''}
        asOfDate={asOfDate}
      />
    </div>
  )
}

export default TrialBalanceReport

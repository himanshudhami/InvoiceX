import { useState, useMemo } from 'react'
import { useAccountLedger, useAccounts } from '@/features/ledger/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { ChartOfAccount } from '@/services/api/types'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { format, startOfMonth, endOfMonth, subMonths } from 'date-fns'
import {
  FileText,
  Download,
  Printer,
  Search,
  ChevronDown,
  TrendingUp,
  TrendingDown,
  ArrowRight,
  ExternalLink
} from 'lucide-react'

const AccountLedgerReport = () => {
  const [companyFilter, setCompanyFilter] = useState<string>('')
  const [selectedAccountId, setSelectedAccountId] = useState<string>('')
  const [accountSearch, setAccountSearch] = useState('')
  const [showAccountDropdown, setShowAccountDropdown] = useState(false)
  const today = new Date()
  const [fromDate, setFromDate] = useState(format(startOfMonth(today), 'yyyy-MM-dd'))
  const [toDate, setToDate] = useState(format(endOfMonth(today), 'yyyy-MM-dd'))

  const { data: companies = [] } = useCompanies()
  const { data: accounts = [] } = useAccounts(companyFilter, !!companyFilter)
  const { data: report, isLoading, error, refetch } = useAccountLedger(
    selectedAccountId,
    fromDate,
    toDate,
    !!selectedAccountId && !!fromDate && !!toDate
  )

  const selectedCompany = companies.find(c => c.id === companyFilter)
  const selectedAccount = accounts.find(a => a.id === selectedAccountId)

  // Filter accounts based on search
  const filteredAccounts = useMemo(() => {
    if (!accountSearch) return accounts.filter(a => a.isActive)
    const search = accountSearch.toLowerCase()
    return accounts.filter(a =>
      a.isActive &&
      (a.accountCode.toLowerCase().includes(search) ||
       a.accountName.toLowerCase().includes(search))
    )
  }, [accounts, accountSearch])

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(Math.abs(amount))
  }

  const formatDate = (dateStr: string) => {
    try {
      return format(new Date(dateStr), 'dd MMM yyyy')
    } catch {
      return dateStr
    }
  }

  const handlePrint = () => {
    window.print()
  }

  const handleExport = () => {
    if (!report) return

    const lines: string[] = [
      `Account Ledger`,
      `Account: ${report.accountCode} - ${report.accountName}`,
      `Period: ${formatDate(fromDate)} to ${formatDate(toDate)}`,
      '',
      'Date,Journal No,Description,Debit,Credit,Balance',
      '',
      `Opening Balance,,,,,${report.openingBalance.toFixed(2)}`,
    ]

    report.entries.forEach(entry => {
      lines.push(
        `${formatDate(entry.date)},${entry.journalNumber},"${entry.description}",${entry.debit > 0 ? entry.debit.toFixed(2) : ''},${entry.credit > 0 ? entry.credit.toFixed(2) : ''},${entry.runningBalance.toFixed(2)}`
      )
    })

    lines.push(`Closing Balance,,,,,${report.closingBalance.toFixed(2)}`)

    const csvContent = lines.join('\n')
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `account-ledger-${report.accountCode}-${fromDate}-to-${toDate}.csv`
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
        const fyStart = now.getMonth() >= 3
          ? new Date(now.getFullYear(), 3, 1)
          : new Date(now.getFullYear() - 1, 3, 1)
        from = fyStart
        to = now
        break
      case 'last3m':
        from = startOfMonth(subMonths(now, 2))
        to = endOfMonth(now)
        break
      default:
        return
    }

    setFromDate(format(from, 'yyyy-MM-dd'))
    setToDate(format(to, 'yyyy-MM-dd'))
  }

  const selectAccount = (account: ChartOfAccount) => {
    setSelectedAccountId(account.id)
    setAccountSearch('')
    setShowAccountDropdown(false)
  }

  if (!companyFilter) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Account Ledger</h1>
          <p className="text-gray-600 mt-2">View transaction history for a specific account</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <FileText className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">Select a Company</h3>
            <p className="mt-1 text-sm text-gray-500">Please select a company to view account ledgers</p>
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
          <h1 className="text-3xl font-bold text-gray-900">Account Ledger</h1>
          <p className="text-gray-600 mt-2">Transaction history for individual accounts</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={handlePrint}
            disabled={!report}
            className="inline-flex items-center gap-2 px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50"
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
              onChange={(id) => {
                setCompanyFilter(id)
                setSelectedAccountId('')
              }}
            />
          </div>

          {/* Account Selector */}
          <div className="relative flex-1 min-w-64">
            <label className="block text-sm font-medium text-gray-700 mb-2">Account</label>
            <div className="relative">
              <div className="flex">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                  <input
                    type="text"
                    value={selectedAccountId ? `${selectedAccount?.accountCode} - ${selectedAccount?.accountName}` : accountSearch}
                    onChange={(e) => {
                      setAccountSearch(e.target.value)
                      setSelectedAccountId('')
                      setShowAccountDropdown(true)
                    }}
                    onFocus={() => setShowAccountDropdown(true)}
                    placeholder="Search by code or name..."
                    className="w-full pl-10 pr-10 py-2 border border-gray-300 rounded-md shadow-sm focus:border-primary focus:ring-primary sm:text-sm"
                  />
                  <button
                    type="button"
                    onClick={() => setShowAccountDropdown(!showAccountDropdown)}
                    className="absolute right-2 top-1/2 -translate-y-1/2"
                  >
                    <ChevronDown className="h-5 w-5 text-gray-400" />
                  </button>
                </div>
              </div>

              {/* Account Dropdown */}
              {showAccountDropdown && (
                <div className="absolute z-10 mt-1 w-full bg-white border border-gray-300 rounded-md shadow-lg max-h-60 overflow-auto">
                  {filteredAccounts.length === 0 ? (
                    <div className="px-4 py-3 text-sm text-gray-500">
                      No accounts found
                    </div>
                  ) : (
                    filteredAccounts.slice(0, 50).map(account => (
                      <button
                        key={account.id}
                        onClick={() => selectAccount(account)}
                        className="w-full px-4 py-2 text-left hover:bg-gray-100 flex items-center gap-2"
                      >
                        <span className="font-mono text-sm text-gray-500">{account.accountCode}</span>
                        <span className="text-sm text-gray-900">{account.accountName}</span>
                        <span className={`ml-auto text-xs px-2 py-0.5 rounded ${
                          account.accountType === 'asset' ? 'bg-blue-100 text-blue-700' :
                          account.accountType === 'liability' ? 'bg-red-100 text-red-700' :
                          account.accountType === 'equity' ? 'bg-purple-100 text-purple-700' :
                          account.accountType === 'income' ? 'bg-green-100 text-green-700' :
                          'bg-orange-100 text-orange-700'
                        }`}>
                          {account.accountType}
                        </span>
                      </button>
                    ))
                  )}
                </div>
              )}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">From Date</label>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="block w-40 rounded-md border-gray-300 shadow-sm focus:border-primary focus:ring-primary sm:text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">To Date</label>
            <input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="block w-40 rounded-md border-gray-300 shadow-sm focus:border-primary focus:ring-primary sm:text-sm"
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
              onClick={() => setQuickDateRange('last3m')}
              className="px-3 py-2 text-xs font-medium text-gray-600 bg-gray-100 rounded hover:bg-gray-200"
            >
              3 Months
            </button>
            <button
              onClick={() => setQuickDateRange('ytd')}
              className="px-3 py-2 text-xs font-medium text-gray-600 bg-gray-100 rounded hover:bg-gray-200"
            >
              YTD
            </button>
          </div>
        </div>
      </div>

      {/* Select Account Prompt */}
      {!selectedAccountId && (
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <Search className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">Select an Account</h3>
            <p className="mt-1 text-sm text-gray-500">
              Search and select an account to view its transaction history
            </p>
          </div>
        </div>
      )}

      {/* Print Header */}
      {selectedAccountId && (
        <div className="hidden print:block text-center mb-4">
          <h1 className="text-2xl font-bold">{selectedCompany?.name}</h1>
          <h2 className="text-xl mt-2">Account Ledger</h2>
          <p className="text-lg font-medium mt-1">{report?.accountCode} - {report?.accountName}</p>
          <p className="text-gray-600">For the period {formatDate(fromDate)} to {formatDate(toDate)}</p>
        </div>
      )}

      {/* Loading / Error */}
      {selectedAccountId && isLoading && (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      )}

      {selectedAccountId && error && (
        <div className="text-center py-12">
          <div className="text-red-600 mb-4">Failed to load account ledger</div>
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
          {/* Account Header */}
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-xl font-bold text-gray-900">
                  <span className="font-mono text-gray-500">{report.accountCode}</span>
                  {' - '}
                  {report.accountName}
                </h2>
                <p className="text-sm text-gray-500 mt-1">
                  {formatDate(fromDate)} to {formatDate(toDate)}
                </p>
              </div>
              <div className="text-right">
                <p className="text-sm text-gray-500">Closing Balance</p>
                <p className={`text-2xl font-bold ${report.closingBalance >= 0 ? 'text-blue-600' : 'text-red-600'}`}>
                  {formatCurrency(report.closingBalance)}
                </p>
              </div>
            </div>
          </div>

          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 print:hidden">
            <div className="bg-gray-50 rounded-lg p-4 border border-gray-200">
              <div className="flex items-center gap-3">
                <ArrowRight className="h-6 w-6 text-gray-400" />
                <div>
                  <p className="text-sm text-gray-600">Opening Balance</p>
                  <p className="text-xl font-bold text-gray-900">{formatCurrency(report.openingBalance)}</p>
                </div>
              </div>
            </div>
            <div className="bg-blue-50 rounded-lg p-4 border border-blue-200">
              <div className="flex items-center gap-3">
                <TrendingUp className="h-6 w-6 text-blue-600" />
                <div>
                  <p className="text-sm text-blue-600">Total Debits</p>
                  <p className="text-xl font-bold text-blue-700">
                    {formatCurrency(report.entries.reduce((sum, e) => sum + e.debit, 0))}
                  </p>
                </div>
              </div>
            </div>
            <div className="bg-green-50 rounded-lg p-4 border border-green-200">
              <div className="flex items-center gap-3">
                <TrendingDown className="h-6 w-6 text-green-600" />
                <div>
                  <p className="text-sm text-green-600">Total Credits</p>
                  <p className="text-xl font-bold text-green-700">
                    {formatCurrency(report.entries.reduce((sum, e) => sum + e.credit, 0))}
                  </p>
                </div>
              </div>
            </div>
          </div>

          {/* Ledger Table */}
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50 print:bg-gray-200">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Date
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Journal No
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Description
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">
                    Debit
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">
                    Credit
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">
                    Balance
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {/* Opening Balance Row */}
                <tr className="bg-gray-50 font-medium">
                  <td className="px-4 py-3 text-sm text-gray-500" colSpan={5}>
                    Opening Balance
                  </td>
                  <td className="px-4 py-3 text-sm text-right font-bold text-gray-900">
                    {formatCurrency(report.openingBalance)}
                  </td>
                </tr>

                {/* Transaction Rows */}
                {report.entries.map((entry, idx) => (
                  <tr key={idx} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-sm text-gray-600">
                      {formatDate(entry.date)}
                    </td>
                    <td className="px-4 py-3 text-sm">
                      <a
                        href={`/ledger/journals?id=${entry.journalEntryId}`}
                        className="text-primary hover:underline inline-flex items-center gap-1"
                      >
                        {entry.journalNumber}
                        <ExternalLink size={12} />
                      </a>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {entry.description}
                    </td>
                    <td className="px-4 py-3 text-sm text-right text-blue-600 font-medium">
                      {entry.debit > 0 ? formatCurrency(entry.debit) : ''}
                    </td>
                    <td className="px-4 py-3 text-sm text-right text-green-600 font-medium">
                      {entry.credit > 0 ? formatCurrency(entry.credit) : ''}
                    </td>
                    <td className={`px-4 py-3 text-sm text-right font-medium ${
                      entry.runningBalance >= 0 ? 'text-gray-900' : 'text-red-600'
                    }`}>
                      {formatCurrency(entry.runningBalance)}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-gray-900 text-white">
                <tr>
                  <td colSpan={3} className="px-4 py-3 text-right font-bold">
                    CLOSING BALANCE
                  </td>
                  <td className="px-4 py-3 text-right font-bold">
                    {formatCurrency(report.entries.reduce((sum, e) => sum + e.debit, 0))}
                  </td>
                  <td className="px-4 py-3 text-right font-bold">
                    {formatCurrency(report.entries.reduce((sum, e) => sum + e.credit, 0))}
                  </td>
                  <td className="px-4 py-3 text-right font-bold">
                    {formatCurrency(report.closingBalance)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>

          {/* Report Footer */}
          <div className="text-center text-sm text-gray-500 print:mt-8">
            <p>Generated on {format(new Date(), 'dd MMM yyyy HH:mm')}</p>
          </div>
        </>
      )}

      {/* Empty State */}
      {report && report.entries.length === 0 && !isLoading && (
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <FileText className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">No Transactions</h3>
            <p className="mt-1 text-sm text-gray-500">
              No transactions found for this account in the selected period.
            </p>
          </div>
        </div>
      )}
    </div>
  )
}

export default AccountLedgerReport

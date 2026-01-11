import { useMemo } from 'react'
import { useSearchParams, useNavigate, useLocation, Link } from 'react-router-dom'
import { usePartyLedger } from '@/features/ledger/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { format, subMonths } from 'date-fns'
import type { PartyLedgerNavigationState } from '@/components/ledger/SubledgerDrilldownDrawer'
import {
  FileText,
  Download,
  Printer,
  User,
  Building2,
  ArrowLeft,
  ChevronRight,
  Landmark
} from 'lucide-react'

// Derive filter state directly from URL params (no useState needed)
const usePartyLedgerParams = () => {
  const [searchParams, setSearchParams] = useSearchParams()

  return useMemo(() => {
    const defaultFromDate = format(subMonths(new Date(), 12), 'yyyy-MM-dd')
    const defaultToDate = format(new Date(), 'yyyy-MM-dd')

    return {
      companyId: searchParams.get('companyId') || '',
      partyType: searchParams.get('partyType') || 'customer',
      partyId: searchParams.get('partyId') || '',
      fromDate: searchParams.get('fromDate') || defaultFromDate,
      toDate: searchParams.get('toDate') || defaultToDate,
      // Update functions that modify URL params directly
      setFromDate: (date: string) => {
        const newParams = new URLSearchParams(searchParams)
        newParams.set('fromDate', date)
        setSearchParams(newParams, { replace: true })
      },
      setToDate: (date: string) => {
        const newParams = new URLSearchParams(searchParams)
        newParams.set('toDate', date)
        setSearchParams(newParams, { replace: true })
      }
    }
  }, [searchParams, setSearchParams])
}

const PartyLedgerReport = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const navState = location.state as PartyLedgerNavigationState | null

  // Derive all filter state from URL params
  const { companyId, partyType, partyId, fromDate, toDate, setFromDate, setToDate } = usePartyLedgerParams()

  const { data: companies = [] } = useCompanies()

  // Enable query only when all required params are present
  const queryEnabled = !!(companyId && partyId && fromDate && toDate)

  const { data: report, isLoading, error, refetch } = usePartyLedger(
    companyId,
    partyType,
    partyId,
    fromDate,
    toDate,
    queryEnabled
  )

  const selectedCompany = useMemo(
    () => companies.find(c => c.id === companyId),
    [companies, companyId]
  )

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

  const handleBack = () => {
    if (navState?.from === 'trial-balance' && navState.returnUrl) {
      // Go back to Trial Balance with preserved URL state
      navigate(navState.returnUrl)
    } else {
      // Generic back
      navigate(-1)
    }
  }

  const handlePrint = () => {
    window.print()
  }

  const handleExport = () => {
    if (!report) return

    const headers = ['Date', 'Journal #', 'Source', 'Description', 'Debit', 'Credit', 'Balance']
    const rows = report.entries.map(entry => [
      formatDate(entry.date),
      entry.journalNumber,
      entry.sourceNumber || '',
      entry.description,
      entry.debit > 0 ? entry.debit.toFixed(2) : '',
      entry.credit > 0 ? entry.credit.toFixed(2) : '',
      entry.runningBalance.toFixed(2)
    ])

    const csvContent = [
      `Party Ledger: ${report.partyName}`,
      `Party Type: ${report.partyType}`,
      `Period: ${formatDate(report.fromDate)} to ${formatDate(report.toDate)}`,
      `Company: ${selectedCompany?.name || ''}`,
      '',
      `Opening Balance: ${report.openingBalance.toFixed(2)}`,
      '',
      headers.join(','),
      ...rows.map(row => row.join(',')),
      '',
      `Closing Balance: ${report.closingBalance.toFixed(2)}`
    ].join('\n')

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `party-ledger-${partyId}-${toDate}.csv`
    link.click()
  }

  // If no party selected, show selection UI
  if (!partyId) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Party Ledger</h1>
          <p className="text-gray-600 mt-2">View transaction history for a customer or vendor</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <User className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">Select a Party</h3>
            <p className="mt-1 text-sm text-gray-500">
              Click on a party from the subledger drill-down in Trial Balance to view their ledger
            </p>
            <Link
              to="/ledger/trial-balance"
              className="mt-4 inline-flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
            >
              Go to Trial Balance
            </Link>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6 print:space-y-4">
      {/* Back Navigation & Breadcrumb */}
      <div className="flex items-center gap-4 print:hidden">
        <button
          onClick={handleBack}
          className="inline-flex items-center gap-2 px-3 py-2 text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-md transition-colors"
        >
          <ArrowLeft size={18} />
          Back
        </button>

        {/* Breadcrumb */}
        <nav className="flex items-center gap-2 text-sm text-gray-500">
          <Link to="/ledger/trial-balance" className="hover:text-gray-900">
            Trial Balance
          </Link>
          {navState?.controlAccountName && (
            <>
              <ChevronRight size={14} />
              <span>{navState.controlAccountName}</span>
            </>
          )}
          <ChevronRight size={14} />
          <span className="text-gray-900 font-medium">
            {navState?.partyName || report?.partyName || 'Party Ledger'}
          </span>
        </nav>
      </div>

      {/* Header */}
      <div className="flex justify-between items-start print:hidden">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Party Ledger</h1>
          <p className="text-gray-600 mt-2">
            Transaction history for {report?.partyName || navState?.partyName || 'party'}
          </p>
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
          <button
            onClick={() => refetch()}
            className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
          >
            Refresh
          </button>
        </div>
      </div>

      {/* Print Header */}
      <div className="hidden print:block text-center mb-4">
        <h1 className="text-2xl font-bold">{selectedCompany?.name}</h1>
        <h2 className="text-xl mt-2">Party Ledger</h2>
        <p className="text-gray-600">{report?.partyName}</p>
        <p className="text-gray-500">
          {formatDate(fromDate)} to {formatDate(toDate)}
        </p>
      </div>

      {/* Loading / Error */}
      {isLoading && (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      )}

      {error && (
        <div className="text-center py-12">
          <div className="text-red-600 mb-4">Failed to load party ledger</div>
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
          {/* Party Info Card */}
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                {report.partyType === 'vendor' ? (
                  <Building2 className="h-10 w-10 text-purple-600" />
                ) : (
                  <User className="h-10 w-10 text-blue-600" />
                )}
                <div>
                  <h2 className="text-xl font-semibold text-gray-900">{report.partyName}</h2>
                  <p className="text-sm text-gray-500 capitalize">{report.partyType}</p>
                </div>
              </div>
              <div className="text-right">
                <div className="text-sm text-gray-500">Closing Balance</div>
                <div className={`text-2xl font-bold ${
                  report.closingBalance >= 0 ? 'text-blue-600' : 'text-green-600'
                }`}>
                  {report.closingBalance >= 0 ? 'Dr ' : 'Cr '}
                  {formatCurrency(report.closingBalance)}
                </div>
              </div>
            </div>
          </div>

          {/* Summary Row */}
          <div className="grid grid-cols-4 gap-4">
            <div className="bg-white rounded-lg shadow p-4">
              <div className="text-sm text-gray-500">Opening Balance</div>
              <div className={`text-lg font-semibold ${
                report.openingBalance >= 0 ? 'text-blue-600' : 'text-green-600'
              }`}>
                {report.openingBalance >= 0 ? 'Dr ' : 'Cr '}
                {formatCurrency(report.openingBalance)}
              </div>
            </div>
            <div className="bg-white rounded-lg shadow p-4">
              <div className="text-sm text-gray-500">Total Debits</div>
              <div className="text-lg font-semibold text-blue-600">
                {formatCurrency(report.totalDebits)}
              </div>
            </div>
            <div className="bg-white rounded-lg shadow p-4">
              <div className="text-sm text-gray-500">Total Credits</div>
              <div className="text-lg font-semibold text-green-600">
                {formatCurrency(report.totalCredits)}
              </div>
            </div>
            <div className="bg-white rounded-lg shadow p-4">
              <div className="text-sm text-gray-500">Transactions</div>
              <div className="text-lg font-semibold text-gray-900">
                {report.entries.length}
              </div>
            </div>
          </div>

          {/* Transaction Table */}
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50 print:bg-gray-200">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-28">
                    Date
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-32">
                    Journal #
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Description
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">
                    Debit (Dr)
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">
                    Credit (Cr)
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">
                    Balance
                  </th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider w-12 print:hidden">
                    Bank
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {/* Opening Balance Row */}
                <tr className="bg-gray-50">
                  <td className="px-4 py-3 text-sm text-gray-500">{formatDate(report.fromDate)}</td>
                  <td className="px-4 py-3 text-sm text-gray-500">-</td>
                  <td className="px-4 py-3 text-sm font-medium text-gray-900">Opening Balance</td>
                  <td className="px-4 py-3 text-sm text-right"></td>
                  <td className="px-4 py-3 text-sm text-right"></td>
                  <td className={`px-4 py-3 text-sm text-right font-medium ${
                    report.openingBalance >= 0 ? 'text-blue-600' : 'text-green-600'
                  }`}>
                    {report.openingBalance >= 0 ? 'Dr ' : 'Cr '}
                    {formatCurrency(report.openingBalance)}
                  </td>
                  <td className="print:hidden"></td>
                </tr>

                {/* Transaction Entries */}
                {report.entries.map((entry, idx) => (
                  <tr key={`${entry.journalEntryId}-${idx}`} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-sm text-gray-600">
                      {formatDate(entry.date)}
                    </td>
                    <td className="px-4 py-3 text-sm font-mono text-gray-600">
                      {entry.journalNumber}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      <div>{entry.description}</div>
                      {entry.sourceNumber && (
                        <div className="text-xs text-gray-500">
                          {entry.sourceType}: {entry.sourceNumber}
                        </div>
                      )}
                    </td>
                    <td className="px-4 py-3 text-sm text-right font-medium text-blue-600">
                      {entry.debit > 0 ? formatCurrency(entry.debit) : ''}
                    </td>
                    <td className="px-4 py-3 text-sm text-right font-medium text-green-600">
                      {entry.credit > 0 ? formatCurrency(entry.credit) : ''}
                    </td>
                    <td className={`px-4 py-3 text-sm text-right font-medium ${
                      entry.runningBalance >= 0 ? 'text-blue-600' : 'text-green-600'
                    }`}>
                      {entry.runningBalance >= 0 ? 'Dr ' : 'Cr '}
                      {formatCurrency(entry.runningBalance)}
                    </td>
                    <td className="px-4 py-3 text-center print:hidden">
                      <Link
                        to={`/bank/transactions?companyId=${companyId}&search=${encodeURIComponent(report.partyName)}`}
                        className="inline-flex items-center justify-center p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors"
                        title={`Search bank transactions for ${report.partyName}`}
                      >
                        <Landmark size={16} />
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-gray-900 text-white">
                <tr>
                  <td colSpan={3} className="px-4 py-3 text-right font-bold">
                    Closing Balance
                  </td>
                  <td className="px-4 py-3 text-right font-bold">
                    {formatCurrency(report.totalDebits)}
                  </td>
                  <td className="px-4 py-3 text-right font-bold">
                    {formatCurrency(report.totalCredits)}
                  </td>
                  <td className="px-4 py-3 text-right font-bold">
                    {report.closingBalance >= 0 ? 'Dr ' : 'Cr '}
                    {formatCurrency(report.closingBalance)}
                  </td>
                  <td className="print:hidden"></td>
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
              No transactions found for this party in the selected period.
            </p>
          </div>
        </div>
      )}
    </div>
  )
}

export default PartyLedgerReport

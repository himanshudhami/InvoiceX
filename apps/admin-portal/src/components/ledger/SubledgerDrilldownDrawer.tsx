import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { Drawer } from '@/components/ui/Drawer'
import { useSubledgerDrilldown } from '@/features/ledger/hooks'
import { format, subMonths } from 'date-fns'
import {
  Users,
  CheckCircle,
  AlertTriangle,
  ArrowRight,
  Calendar,
  Hash
} from 'lucide-react'

export interface PartyLedgerNavigationState {
  from: 'trial-balance'
  returnUrl: string
  controlAccountId: string
  controlAccountName: string
  asOfDate: string
  partyName: string
}

interface SubledgerDrilldownDrawerProps {
  isOpen: boolean
  onClose: () => void
  companyId: string
  controlAccountId: string
  controlAccountName: string
  asOfDate: string
}

export const SubledgerDrilldownDrawer = ({
  isOpen,
  onClose,
  companyId,
  controlAccountId,
  controlAccountName,
  asOfDate
}: SubledgerDrilldownDrawerProps) => {
  const navigate = useNavigate()
  const { data: drilldown, isLoading, error } = useSubledgerDrilldown(
    companyId,
    controlAccountId,
    asOfDate,
    isOpen && !!companyId && !!controlAccountId
  )

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(Math.abs(amount))
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '-'
    try {
      return format(new Date(dateStr), 'dd MMM yyyy')
    } catch {
      return dateStr
    }
  }

  // Sort parties by absolute balance descending
  const sortedParties = useMemo(() => {
    if (!drilldown?.parties) return []
    return [...drilldown.parties].sort((a, b) => Math.abs(b.balance) - Math.abs(a.balance))
  }, [drilldown?.parties])

  // Navigate to party ledger with state for back navigation
  const handlePartyClick = (partyId: string, partyType: string, partyName: string) => {
    const toDate = asOfDate
    const fromDate = format(subMonths(new Date(asOfDate), 12), 'yyyy-MM-dd')

    // Capture current Trial Balance URL with all query params for return navigation
    const returnUrl = window.location.pathname + window.location.search

    const state: PartyLedgerNavigationState = {
      from: 'trial-balance',
      returnUrl,
      controlAccountId,
      controlAccountName,
      asOfDate,
      partyName
    }

    onClose() // Close drawer before navigating
    navigate(
      `/ledger/party-ledger?companyId=${companyId}&partyType=${partyType}&partyId=${partyId}&fromDate=${fromDate}&toDate=${toDate}`,
      { state }
    )
  }

  return (
    <Drawer
      isOpen={isOpen}
      onClose={onClose}
      title={`Subledger: ${controlAccountName}`}
      size="2xl"
      resizable
      resizeStorageKey="subledger-drilldown-drawer"
    >
      <div className="space-y-6">
        {/* Summary */}
        {drilldown && (
          <div className={`rounded-lg p-4 flex items-center justify-between ${
            drilldown.isReconciled
              ? 'bg-green-50 border border-green-200'
              : 'bg-amber-50 border border-amber-200'
          }`}>
            <div className="flex items-center gap-3">
              {drilldown.isReconciled ? (
                <CheckCircle className="h-6 w-6 text-green-600" />
              ) : (
                <AlertTriangle className="h-6 w-6 text-amber-600" />
              )}
              <div>
                <div className="font-medium">
                  {drilldown.isReconciled ? 'Reconciled' : 'Difference Found'}
                </div>
                <div className="text-sm text-gray-600">
                  Control: {formatCurrency(drilldown.controlAccountBalance)} |
                  Subledger: {formatCurrency(drilldown.subledgerSum)}
                </div>
              </div>
            </div>
            <div className="text-right">
              <div className="text-sm text-gray-500">As of</div>
              <div className="font-medium">{formatDate(asOfDate)}</div>
            </div>
          </div>
        )}

        {/* Loading State */}
        {isLoading && (
          <div className="flex items-center justify-center h-48">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          </div>
        )}

        {/* Error State */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
            Failed to load subledger data. Please try again.
          </div>
        )}

        {/* Party List */}
        {drilldown && !isLoading && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-medium text-gray-900 flex items-center gap-2">
                <Users size={18} />
                Party Breakdown ({drilldown.parties.length})
              </h3>
            </div>

            {sortedParties.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                No party transactions found for this control account
              </div>
            ) : (
              <div className="border rounded-lg overflow-hidden">
                <table className="w-full">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                        Party
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                        Balance
                      </th>
                      <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase w-20">
                        Txns
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase w-28">
                        Last Txn
                      </th>
                      <th className="px-4 py-3 w-10"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-200">
                    {sortedParties.map((party) => (
                      <tr
                        key={party.partyId}
                        className="hover:bg-gray-50 cursor-pointer"
                        onClick={() => handlePartyClick(party.partyId, party.partyType, party.partyName)}
                      >
                        <td className="px-4 py-3">
                          <div className="font-medium text-gray-900">{party.partyName}</div>
                          <div className="text-xs text-gray-500 flex items-center gap-2">
                            <span className="capitalize">{party.partyType}</span>
                            {party.partyCode && (
                              <>
                                <span className="text-gray-300">|</span>
                                <span>{party.partyCode}</span>
                              </>
                            )}
                          </div>
                        </td>
                        <td className={`px-4 py-3 text-right font-medium ${
                          party.balance >= 0 ? 'text-blue-600' : 'text-green-600'
                        }`}>
                          {party.balance >= 0 ? 'Dr ' : 'Cr '}
                          {formatCurrency(party.balance)}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <span className="inline-flex items-center gap-1 text-sm text-gray-600">
                            <Hash size={12} />
                            {party.transactionCount}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-right text-sm text-gray-500">
                          {party.lastTransactionDate ? (
                            <span className="inline-flex items-center gap-1">
                              <Calendar size={12} />
                              {formatDate(party.lastTransactionDate)}
                            </span>
                          ) : '-'}
                        </td>
                        <td className="px-4 py-3">
                          <ArrowRight size={16} className="text-gray-400" />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot className="bg-gray-100 font-medium">
                    <tr>
                      <td className="px-4 py-3">Total</td>
                      <td className={`px-4 py-3 text-right ${
                        drilldown.subledgerSum >= 0 ? 'text-blue-700' : 'text-green-700'
                      }`}>
                        {drilldown.subledgerSum >= 0 ? 'Dr ' : 'Cr '}
                        {formatCurrency(drilldown.subledgerSum)}
                      </td>
                      <td colSpan={3}></td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            )}
          </div>
        )}
      </div>
    </Drawer>
  )
}

export default SubledgerDrilldownDrawer

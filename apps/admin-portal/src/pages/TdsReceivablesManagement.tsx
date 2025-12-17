import { useState, useMemo } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import {
  useTdsReceivables,
  useDeleteTdsReceivable,
  useMatchTdsWith26As,
  useUpdateTdsStatus,
  useTdsSummary
} from '@/hooks/api/useTdsReceivables'
import { useCompanies } from '@/hooks/api/useCompanies'
import { TdsReceivable } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { TdsReceivableForm } from '@/components/forms/TdsReceivableForm'
import {
  Edit,
  Trash2,
  Building2,
  FileCheck,
  Receipt,
  CheckCircle,
  AlertCircle,
  Clock,
  XCircle
} from 'lucide-react'
import { cn } from '@/lib/utils'

// Generate financial year options
const generateFinancialYears = () => {
  const currentYear = new Date().getFullYear()
  const currentMonth = new Date().getMonth() + 1
  const startYear = currentMonth > 3 ? currentYear : currentYear - 1

  const years = []
  for (let i = 0; i < 5; i++) {
    const year = startYear - i
    years.push({
      value: `${year}-${(year + 1).toString().slice(-2)}`,
      label: `FY ${year}-${(year + 1).toString().slice(-2)}`
    })
  }
  return years
}

const TdsReceivablesManagement = () => {
  const financialYears = generateFinancialYears()
  const [selectedFY, setSelectedFY] = useState(financialYears[0].value)
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')

  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingEntry, setEditingEntry] = useState<TdsReceivable | null>(null)
  const [deletingEntry, setDeletingEntry] = useState<TdsReceivable | null>(null)
  const [matchingEntry, setMatchingEntry] = useState<TdsReceivable | null>(null)
  const [match26AsAmount, setMatch26AsAmount] = useState<string>('')
  const [statusEntry, setStatusEntry] = useState<TdsReceivable | null>(null)
  const [newStatus, setNewStatus] = useState<string>('')

  const { data: allTdsReceivables = [], isLoading, error, refetch } = useTdsReceivables()
  const { data: companies = [] } = useCompanies()
  const deleteTdsReceivable = useDeleteTdsReceivable()
  const matchWith26As = useMatchTdsWith26As()
  const updateStatus = useUpdateTdsStatus()

  // Get summary for selected company and FY
  const { data: summary } = useTdsSummary(
    selectedCompanyId,
    selectedFY,
    !!selectedCompanyId
  )

  const companiesMap = new Map(companies.map(c => [c.id, c.name]))

  // Filter TDS receivables based on selected company and financial year
  const tdsReceivables = useMemo(() => {
    return allTdsReceivables.filter(tds => {
      const matchesFY = tds.financialYear === selectedFY
      const matchesCompany = !selectedCompanyId || tds.companyId === selectedCompanyId
      return matchesFY && matchesCompany
    })
  }, [allTdsReceivables, selectedFY, selectedCompanyId])

  const handleEdit = (entry: TdsReceivable) => {
    setEditingEntry(entry)
  }

  const handleDelete = (entry: TdsReceivable) => {
    setDeletingEntry(entry)
  }

  const handleDeleteConfirm = async () => {
    if (deletingEntry) {
      try {
        await deleteTdsReceivable.mutateAsync(deletingEntry.id)
        setDeletingEntry(null)
      } catch (error) {
        console.error('Failed to delete TDS entry:', error)
      }
    }
  }

  const handleMatch26As = (entry: TdsReceivable) => {
    setMatchingEntry(entry)
    setMatch26AsAmount(entry.tdsAmount.toString())
  }

  const handleMatch26AsConfirm = async () => {
    if (matchingEntry && match26AsAmount) {
      try {
        await matchWith26As.mutateAsync({
          id: matchingEntry.id,
          data: { form26AsAmount: parseFloat(match26AsAmount) }
        })
        setMatchingEntry(null)
        setMatch26AsAmount('')
      } catch (error) {
        console.error('Failed to match with 26AS:', error)
      }
    }
  }

  const handleUpdateStatus = (entry: TdsReceivable) => {
    setStatusEntry(entry)
    setNewStatus(entry.status)
  }

  const handleStatusConfirm = async () => {
    if (statusEntry && newStatus) {
      try {
        await updateStatus.mutateAsync({
          id: statusEntry.id,
          data: { status: newStatus as TdsReceivable['status'] }
        })
        setStatusEntry(null)
        setNewStatus('')
      } catch (error) {
        console.error('Failed to update status:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingEntry(null)
    refetch()
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2,
    }).format(amount)
  }

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { color: string; icon: typeof Clock; label: string }> = {
      pending: { color: 'bg-yellow-100 text-yellow-800', icon: Clock, label: 'Pending' },
      matched: { color: 'bg-blue-100 text-blue-800', icon: CheckCircle, label: 'Matched' },
      claimed: { color: 'bg-green-100 text-green-800', icon: FileCheck, label: 'Claimed' },
      disputed: { color: 'bg-red-100 text-red-800', icon: AlertCircle, label: 'Disputed' },
      written_off: { color: 'bg-gray-100 text-gray-800', icon: XCircle, label: 'Written Off' },
    }
    const config = statusConfig[status] || statusConfig.pending
    const Icon = config.icon
    return (
      <span className={cn('inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium', config.color)}>
        <Icon className="h-3 w-3" />
        {config.label}
      </span>
    )
  }

  const columns: ColumnDef<TdsReceivable>[] = [
    {
      accessorKey: 'deductorName',
      header: 'Deductor',
      cell: ({ row }) => {
        const entry = row.original
        return (
          <div className="flex items-start gap-3">
            <div className="p-2 bg-orange-100 rounded-lg">
              <Building2 className="h-5 w-5 text-orange-600" />
            </div>
            <div>
              <div className="font-medium text-gray-900">{entry.deductorName}</div>
              {entry.deductorTan && (
                <div className="text-xs text-gray-500 font-mono">TAN: {entry.deductorTan}</div>
              )}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'paymentDate',
      header: 'Payment Date',
      cell: ({ row }) => {
        const entry = row.original
        return (
          <div>
            <div className="text-sm text-gray-900">
              {new Date(entry.paymentDate).toLocaleDateString('en-IN', {
                day: '2-digit',
                month: 'short',
                year: 'numeric',
              })}
            </div>
            <div className="text-xs text-gray-500">{entry.quarter}</div>
          </div>
        )
      },
    },
    {
      accessorKey: 'tdsSection',
      header: 'Section',
      cell: ({ row }) => (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
          {row.original.tdsSection}
        </span>
      ),
    },
    {
      accessorKey: 'grossAmount',
      header: 'Gross Amount',
      cell: ({ row }) => (
        <div className="text-right font-medium text-gray-900">
          {formatCurrency(row.original.grossAmount)}
        </div>
      ),
    },
    {
      accessorKey: 'tdsAmount',
      header: 'TDS Amount',
      cell: ({ row }) => {
        const entry = row.original
        return (
          <div className="text-right">
            <div className="font-medium text-green-600">{formatCurrency(entry.tdsAmount)}</div>
            <div className="text-xs text-gray-500">{entry.tdsRate}%</div>
          </div>
        )
      },
    },
    {
      accessorKey: 'matchedWith26As',
      header: '26AS Status',
      cell: ({ row }) => {
        const entry = row.original
        if (entry.matchedWith26As) {
          const diff = entry.amountDifference || 0
          return (
            <div className="text-center">
              <span className={cn(
                'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium',
                diff === 0 ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'
              )}>
                <CheckCircle className="h-3 w-3" />
                {diff === 0 ? 'Matched' : `Diff: ${formatCurrency(diff)}`}
              </span>
            </div>
          )
        }
        return (
          <div className="text-center">
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
              <Clock className="h-3 w-3" />
              Not Matched
            </span>
          </div>
        )
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const entry = row.original
        return (
          <div className="flex space-x-2">
            {!entry.matchedWith26As && (
              <button
                onClick={() => handleMatch26As(entry)}
                className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                title="Match with 26AS"
              >
                <FileCheck size={16} />
              </button>
            )}
            <button
              onClick={() => handleUpdateStatus(entry)}
              className="text-purple-600 hover:text-purple-800 p-1 rounded hover:bg-purple-50 transition-colors"
              title="Update status"
            >
              <Receipt size={16} />
            </button>
            <button
              onClick={() => handleEdit(entry)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit entry"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(entry)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete entry"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  // Summary calculations
  const totalTdsAmount = tdsReceivables.reduce((sum, tds) => sum + tds.tdsAmount, 0)
  const matchedCount = tdsReceivables.filter(tds => tds.matchedWith26As).length
  const claimedCount = tdsReceivables.filter(tds => tds.status === 'claimed').length

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
        <div className="text-red-600 mb-4">Failed to load TDS receivables</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">TDS Receivables</h1>
          <p className="text-gray-600 mt-2">Track TDS deducted by customers and match with Form 26AS</p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label htmlFor="companyFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Company
            </label>
            <select
              id="companyFilter"
              value={selectedCompanyId}
              onChange={(e) => setSelectedCompanyId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">All Companies</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="fyFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Financial Year
            </label>
            <select
              id="fyFilter"
              value={selectedFY}
              onChange={(e) => setSelectedFY(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {financialYears.map((fy) => (
                <option key={fy.value} value={fy.value}>
                  {fy.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Total TDS Amount</p>
              <p className="text-2xl font-bold text-green-600">{formatCurrency(totalTdsAmount)}</p>
            </div>
            <div className="p-3 bg-green-100 rounded-full">
              <Receipt className="h-6 w-6 text-green-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">{tdsReceivables.length} entries</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Matched with 26AS</p>
              <p className="text-2xl font-bold text-blue-600">{matchedCount}</p>
            </div>
            <div className="p-3 bg-blue-100 rounded-full">
              <CheckCircle className="h-6 w-6 text-blue-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">
            {tdsReceivables.length > 0
              ? `${Math.round((matchedCount / tdsReceivables.length) * 100)}% matched`
              : '0% matched'}
          </p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Claimed in Return</p>
              <p className="text-2xl font-bold text-purple-600">{claimedCount}</p>
            </div>
            <div className="p-3 bg-purple-100 rounded-full">
              <FileCheck className="h-6 w-6 text-purple-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">
            {formatCurrency(
              tdsReceivables
                .filter(tds => tds.status === 'claimed')
                .reduce((sum, tds) => sum + tds.tdsAmount, 0)
            )}
          </p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Pending</p>
              <p className="text-2xl font-bold text-yellow-600">
                {tdsReceivables.filter(tds => tds.status === 'pending').length}
              </p>
            </div>
            <div className="p-3 bg-yellow-100 rounded-full">
              <Clock className="h-6 w-6 text-yellow-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">
            {formatCurrency(
              tdsReceivables
                .filter(tds => tds.status === 'pending')
                .reduce((sum, tds) => sum + tds.tdsAmount, 0)
            )}
          </p>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={tdsReceivables}
            searchPlaceholder="Search by deductor name..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add TDS Entry"
          />
        </div>
      </div>

      {/* Create Entry Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Add TDS Entry"
        size="lg"
      >
        <TdsReceivableForm
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Entry Drawer */}
      <Drawer
        isOpen={!!editingEntry}
        onClose={() => setEditingEntry(null)}
        title="Edit TDS Entry"
        size="lg"
      >
        {editingEntry && (
          <TdsReceivableForm
            tdsReceivable={editingEntry}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingEntry(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingEntry}
        onClose={() => setDeletingEntry(null)}
        title="Delete TDS Entry"
        size="sm"
      >
        {deletingEntry && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the TDS entry for <strong>{deletingEntry.deductorName}</strong> of{' '}
              <strong>{formatCurrency(deletingEntry.tdsAmount)}</strong>? This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingEntry(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteTdsReceivable.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteTdsReceivable.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Match 26AS Modal */}
      <Modal
        isOpen={!!matchingEntry}
        onClose={() => {
          setMatchingEntry(null)
          setMatch26AsAmount('')
        }}
        title="Match with Form 26AS"
        size="sm"
      >
        {matchingEntry && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <p className="text-sm text-gray-600">TDS Entry Details:</p>
              <p className="font-medium">{matchingEntry.deductorName}</p>
              <p className="text-sm text-gray-500">Expected TDS: {formatCurrency(matchingEntry.tdsAmount)}</p>
            </div>
            <div>
              <label htmlFor="form26AsAmount" className="block text-sm font-medium text-gray-700 mb-1">
                Amount in Form 26AS
              </label>
              <input
                id="form26AsAmount"
                type="number"
                step="0.01"
                value={match26AsAmount}
                onChange={(e) => setMatch26AsAmount(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="Enter amount from 26AS"
              />
              {match26AsAmount && parseFloat(match26AsAmount) !== matchingEntry.tdsAmount && (
                <p className="text-sm text-yellow-600 mt-1">
                  Difference: {formatCurrency(parseFloat(match26AsAmount) - matchingEntry.tdsAmount)}
                </p>
              )}
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => {
                  setMatchingEntry(null)
                  setMatch26AsAmount('')
                }}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleMatch26AsConfirm}
                disabled={matchWith26As.isPending || !match26AsAmount}
                className="px-4 py-2 text-sm font-medium text-white bg-green-600 border border-transparent rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {matchWith26As.isPending ? 'Matching...' : 'Confirm Match'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Update Status Modal */}
      <Modal
        isOpen={!!statusEntry}
        onClose={() => {
          setStatusEntry(null)
          setNewStatus('')
        }}
        title="Update TDS Status"
        size="sm"
      >
        {statusEntry && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <p className="text-sm text-gray-600">TDS Entry:</p>
              <p className="font-medium">{statusEntry.deductorName}</p>
              <p className="text-sm text-gray-500">Amount: {formatCurrency(statusEntry.tdsAmount)}</p>
              <p className="text-sm text-gray-500">Current Status: {statusEntry.status}</p>
            </div>
            <div>
              <label htmlFor="newStatus" className="block text-sm font-medium text-gray-700 mb-1">
                New Status
              </label>
              <select
                id="newStatus"
                value={newStatus}
                onChange={(e) => setNewStatus(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="pending">Pending</option>
                <option value="matched">Matched</option>
                <option value="claimed">Claimed in Return</option>
                <option value="disputed">Disputed</option>
                <option value="written_off">Written Off</option>
              </select>
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => {
                  setStatusEntry(null)
                  setNewStatus('')
                }}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleStatusConfirm}
                disabled={updateStatus.isPending || !newStatus}
                className="px-4 py-2 text-sm font-medium text-white bg-primary border border-transparent rounded-md hover:bg-primary/90 disabled:opacity-50"
              >
                {updateStatus.isPending ? 'Updating...' : 'Update Status'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default TdsReceivablesManagement

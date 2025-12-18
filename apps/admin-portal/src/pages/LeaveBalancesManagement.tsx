import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useLeaveBalances, useAdjustLeaveBalance, useInitializeLeaveBalances, useCarryForwardLeaveBalances } from '@/hooks/api/useLeaveBalances'
import { useLeaveTypes } from '@/hooks/api/useLeaveTypes'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useEmployees } from '@/hooks/api/useEmployees'
import { EmployeeLeaveBalance, AdjustLeaveBalanceDto } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { CompanySelect } from '@/components/ui/CompanySelect'
import { RefreshCw, ArrowRightCircle, Plus, Minus } from 'lucide-react'

const currentFinancialYear = () => {
  const now = new Date()
  const year = now.getFullYear()
  const month = now.getMonth()
  return month >= 3 ? `${year}-${(year + 1).toString().slice(-2)}` : `${year - 1}-${year.toString().slice(-2)}`
}

const LeaveBalancesManagement = () => {
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  const [selectedFinancialYear, setSelectedFinancialYear] = useState<string>(currentFinancialYear())
  const [adjustingBalance, setAdjustingBalance] = useState<EmployeeLeaveBalance | null>(null)
  const [adjustmentData, setAdjustmentData] = useState<AdjustLeaveBalanceDto>({ adjustment: 0, reason: '' })

  const { data: leaveBalances = [], isLoading, error, refetch } = useLeaveBalances({
    companyId: selectedCompanyId || undefined,
    financialYear: selectedFinancialYear,
  })
  const { data: companies = [] } = useCompanies()
  const { data: employees = [] } = useEmployees()
  const { data: leaveTypes = [] } = useLeaveTypes(selectedCompanyId || undefined)

  const adjustBalance = useAdjustLeaveBalance()
  const initializeBalances = useInitializeLeaveBalances()
  const carryForwardBalances = useCarryForwardLeaveBalances()

  const handleAdjust = (balance: EmployeeLeaveBalance) => {
    setAdjustingBalance(balance)
    setAdjustmentData({ adjustment: 0, reason: '' })
  }

  const handleAdjustConfirm = async () => {
    if (adjustingBalance && adjustmentData.reason) {
      try {
        await adjustBalance.mutateAsync({ id: adjustingBalance.id, data: adjustmentData })
        setAdjustingBalance(null)
      } catch (error) {
        console.error('Failed to adjust balance:', error)
      }
    }
  }

  const handleInitialize = async () => {
    if (!selectedCompanyId) return
    try {
      await initializeBalances.mutateAsync({
        companyId: selectedCompanyId,
        financialYear: selectedFinancialYear,
      })
      refetch()
    } catch (error) {
      console.error('Failed to initialize balances:', error)
    }
  }

  const handleCarryForward = async () => {
    if (!selectedCompanyId) return
    const [startYear] = selectedFinancialYear.split('-')
    const nextYear = `${Number(startYear) + 1}-${(Number(startYear) + 2).toString().slice(-2)}`
    try {
      await carryForwardBalances.mutateAsync({
        companyId: selectedCompanyId,
        fromYear: selectedFinancialYear,
        toYear: nextYear,
      })
      refetch()
    } catch (error) {
      console.error('Failed to carry forward balances:', error)
    }
  }

  const getEmployeeName = (employeeId: string) => {
    const employee = employees.find(e => e.id === employeeId)
    return employee?.employeeName || 'Unknown'
  }

  const getLeaveTypeName = (leaveTypeId: string) => {
    const leaveType = leaveTypes.find(lt => lt.id === leaveTypeId)
    return leaveType?.name || 'Unknown'
  }

  const columns: ColumnDef<EmployeeLeaveBalance>[] = [
    {
      accessorKey: 'employeeId',
      header: 'Employee',
      cell: ({ row }) => (
        <div className="font-medium text-gray-900">
          {row.original.employee?.employeeName || getEmployeeName(row.original.employeeId)}
        </div>
      ),
    },
    {
      accessorKey: 'leaveTypeId',
      header: 'Leave Type',
      cell: ({ row }) => (
        <div className="text-gray-900">
          {row.original.leaveType?.name || getLeaveTypeName(row.original.leaveTypeId)}
        </div>
      ),
    },
    {
      accessorKey: 'openingBalance',
      header: 'Opening',
      cell: ({ row }) => (
        <div className="text-center">{row.original.openingBalance}</div>
      ),
    },
    {
      accessorKey: 'accrued',
      header: 'Accrued',
      cell: ({ row }) => (
        <div className="text-center text-green-600">+{row.original.accrued}</div>
      ),
    },
    {
      accessorKey: 'taken',
      header: 'Taken',
      cell: ({ row }) => (
        <div className="text-center text-red-600">-{row.original.taken}</div>
      ),
    },
    {
      accessorKey: 'adjusted',
      header: 'Adjusted',
      cell: ({ row }) => {
        const adj = row.original.adjusted
        return (
          <div className={`text-center ${adj > 0 ? 'text-green-600' : adj < 0 ? 'text-red-600' : 'text-gray-500'}`}>
            {adj > 0 ? `+${adj}` : adj}
          </div>
        )
      },
    },
    {
      accessorKey: 'available',
      header: 'Available',
      cell: ({ row }) => (
        <div className="text-center font-bold text-blue-600">{row.original.available}</div>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const balance = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleAdjust(balance)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Adjust balance"
            >
              <Plus size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  const financialYears = [
    currentFinancialYear(),
    (() => {
      const [startYear] = currentFinancialYear().split('-')
      return `${Number(startYear) - 1}-${startYear.slice(-2)}`
    })(),
    (() => {
      const [startYear] = currentFinancialYear().split('-')
      return `${Number(startYear) + 1}-${(Number(startYear) + 2).toString().slice(-2)}`
    })(),
  ]

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
        <div className="text-red-600 mb-4">Failed to load leave balances</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  const totalAvailable = leaveBalances.reduce((sum, b) => sum + b.available, 0)
  const totalTaken = leaveBalances.reduce((sum, b) => sum + b.taken, 0)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Leave Balances</h1>
        <p className="text-gray-600 mt-2">View and manage employee leave balances</p>
      </div>

      <div className="flex flex-wrap items-center gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
          <CompanySelect
            companies={companies}
            value={selectedCompanyId}
            onChange={setSelectedCompanyId}
            showAllOption
            allOptionLabel="All Companies"
            className="w-[250px]"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Financial Year</label>
          <select
            value={selectedFinancialYear}
            onChange={(e) => setSelectedFinancialYear(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm"
          >
            {financialYears.map(fy => (
              <option key={fy} value={fy}>{fy}</option>
            ))}
          </select>
        </div>
        {selectedCompanyId && (
          <>
            <button
              onClick={handleInitialize}
              disabled={initializeBalances.isPending}
              className="mt-6 inline-flex items-center px-3 py-2 text-sm font-medium text-white bg-green-600 rounded-md hover:bg-green-700 disabled:opacity-50"
            >
              <RefreshCw size={16} className="mr-2" />
              Initialize Balances
            </button>
            <button
              onClick={handleCarryForward}
              disabled={carryForwardBalances.isPending}
              className="mt-6 inline-flex items-center px-3 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50"
            >
              <ArrowRightCircle size={16} className="mr-2" />
              Carry Forward to Next Year
            </button>
          </>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Records</div>
          <div className="text-2xl font-bold text-gray-900">{leaveBalances.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Available Days</div>
          <div className="text-2xl font-bold text-blue-600">{totalAvailable}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Taken Days</div>
          <div className="text-2xl font-bold text-red-600">{totalTaken}</div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={leaveBalances}
            searchPlaceholder="Search by employee..."
          />
        </div>
      </div>

      <Modal
        isOpen={!!adjustingBalance}
        onClose={() => setAdjustingBalance(null)}
        title="Adjust Leave Balance"
        size="sm"
      >
        {adjustingBalance && (
          <div className="space-y-4">
            <div>
              <p className="text-sm text-gray-600">
                Employee: <strong>{adjustingBalance.employee?.employeeName || getEmployeeName(adjustingBalance.employeeId)}</strong>
              </p>
              <p className="text-sm text-gray-600">
                Leave Type: <strong>{adjustingBalance.leaveType?.name || getLeaveTypeName(adjustingBalance.leaveTypeId)}</strong>
              </p>
              <p className="text-sm text-gray-600">
                Current Available: <strong>{adjustingBalance.available}</strong>
              </p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Adjustment (+ or -)</label>
              <input
                type="number"
                value={adjustmentData.adjustment}
                onChange={(e) => setAdjustmentData({ ...adjustmentData, adjustment: Number(e.target.value) })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
                step="0.5"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Reason *</label>
              <textarea
                value={adjustmentData.reason}
                onChange={(e) => setAdjustmentData({ ...adjustmentData, reason: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
                rows={2}
                required
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setAdjustingBalance(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleAdjustConfirm}
                disabled={adjustBalance.isPending || !adjustmentData.reason}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
              >
                {adjustBalance.isPending ? 'Saving...' : 'Adjust'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default LeaveBalancesManagement

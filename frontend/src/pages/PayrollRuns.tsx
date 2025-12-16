import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import { usePayrollRuns, useApprovePayrollRun, useMarkPayrollAsPaid } from '@/features/payroll/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import { PayrollRun } from '@/features/payroll/types/payroll'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { formatINR } from '@/lib/currency'
import { Eye, CheckCircle, DollarSign, Users, ArrowLeft } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'
import { Button } from '@/components/ui/button'

const PayrollRuns = () => {
  const navigate = useNavigate()
  const [approvingRun, setApprovingRun] = useState<PayrollRun | null>(null)
  const [payingRun, setPayingRun] = useState<PayrollRun | null>(null)
  const [paymentData, setPaymentData] = useState({ paymentReference: '', paymentMode: '', remarks: '' })

  const { data: companies = [] } = useCompanies()
  const approvePayrollRun = useApprovePayrollRun()
  const markPayrollAsPaid = useMarkPayrollAsPaid()

  const [urlState, setUrlState] = useQueryStates(
    {
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(10),
      searchTerm: parseAsString,
      companyId: parseAsString,
      status: parseAsString,
      payrollMonth: parseAsInteger,
      payrollYear: parseAsInteger,
    },
    { history: 'push' }
  )

  const { data, isLoading, error } = usePayrollRuns({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: urlState.searchTerm || undefined,
    companyId: urlState.companyId || undefined,
    status: urlState.status || undefined,
    payrollMonth: urlState.payrollMonth || undefined,
    payrollYear: urlState.payrollYear || undefined,
  })

  const handleView = (run: PayrollRun) => {
    navigate(`/payroll/runs/${run.id}`)
  }

  const handleApprove = (run: PayrollRun) => {
    setApprovingRun(run)
  }

  const handleApproveConfirm = async () => {
    if (approvingRun) {
      try {
        await approvePayrollRun.mutateAsync({ id: approvingRun.id })
        setApprovingRun(null)
      } catch (error) {
        console.error('Failed to approve payroll run:', error)
      }
    }
  }

  const handleMarkAsPaid = (run: PayrollRun) => {
    setPayingRun(run)
  }

  const handleMarkAsPaidConfirm = async () => {
    if (payingRun) {
      try {
        await markPayrollAsPaid.mutateAsync({
          id: payingRun.id,
          data: {
            paymentReference: paymentData.paymentReference,
            paymentMode: paymentData.paymentMode,
            remarks: paymentData.remarks,
            updatedBy: 'current-user', // TODO: Get from auth context
          },
        })
        setPayingRun(null)
        setPaymentData({ paymentReference: '', paymentMode: '', remarks: '' })
      } catch (error) {
        console.error('Failed to mark payroll as paid:', error)
      }
    }
  }

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { label: string; className: string }> = {
      draft: { label: 'Draft', className: 'bg-gray-100 text-gray-800' },
      processing: { label: 'Processing', className: 'bg-blue-100 text-blue-800' },
      computed: { label: 'Computed', className: 'bg-yellow-100 text-yellow-800' },
      approved: { label: 'Approved', className: 'bg-green-100 text-green-800' },
      paid: { label: 'Paid', className: 'bg-green-100 text-green-800' },
      cancelled: { label: 'Cancelled', className: 'bg-red-100 text-red-800' },
    }

    const config = statusConfig[status] || statusConfig.draft
    return <span className={`px-2 py-1 rounded text-xs font-medium ${config.className}`}>{config.label}</span>
  }

  const getMonthYear = (month: number, year: number) => {
    const date = new Date(year, month - 1, 1)
    return format(date, 'MMM yyyy')
  }

  const columns: ColumnDef<PayrollRun>[] = [
    {
      accessorKey: 'monthYear',
      header: 'Period',
      cell: ({ row }) => {
        const run = row.original
        return (
          <div>
            <div className="font-medium">{getMonthYear(run.payrollMonth, run.payrollYear)}</div>
            <div className="text-sm text-gray-500">{run.financialYear}</div>
          </div>
        )
      },
    },
    {
      accessorKey: 'companyName',
      header: 'Company',
      cell: ({ row }) => row.original.companyName || '—',
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status),
    },
    {
      accessorKey: 'totalEmployees',
      header: 'Employees',
      cell: ({ row }) => (
        <div className="flex items-center gap-1">
          <Users className="w-4 h-4 text-gray-400" />
          <span>{row.original.totalEmployees}</span>
        </div>
      ),
    },
    {
      accessorKey: 'totalGrossSalary',
      header: 'Gross Salary',
      cell: ({ row }) => formatINR(row.original.totalGrossSalary),
    },
    {
      accessorKey: 'totalNetSalary',
      header: 'Net Pay',
      cell: ({ row }) => formatINR(row.original.totalNetSalary),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const run = row.original
        return (
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" onClick={() => handleView(run)}>
              <Eye className="w-4 h-4" />
            </Button>
            {run.status === 'computed' && (
              <Button variant="ghost" size="sm" onClick={() => handleApprove(run)}>
                <CheckCircle className="w-4 h-4" />
              </Button>
            )}
            {run.status === 'approved' && (
              <Button variant="ghost" size="sm" onClick={() => handleMarkAsPaid(run)}>
                <DollarSign className="w-4 h-4" />
              </Button>
            )}
          </div>
        )
      },
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate('/payroll')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Dashboard
        </Button>
      </div>
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Payroll Runs</h1>
          <p className="text-gray-600 mt-1">Manage and track monthly payroll processing</p>
        </div>
        <div className="flex gap-3">
          <CompanyFilterDropdown
            value={urlState.companyId || ''}
            onChange={(value) => setUrlState({ companyId: value || null })}
          />
          <Button onClick={() => navigate('/payroll/process')}>
            Process Payroll
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-600">Failed to load payroll runs</p>
        </div>
      )}

      <DataTable
        columns={columns}
        data={data?.items || []}
        isLoading={isLoading}
        searchPlaceholder="Search by month, year, company..."
        onSearch={(value) => setUrlState({ searchTerm: value || null, page: 1 })}
        pagination={{
          pageIndex: (data?.pageNumber || urlState.page) - 1,
          pageSize: data?.pageSize || urlState.pageSize,
          totalCount: data?.totalCount || 0,
          onPageChange: (page) => setUrlState({ page: page + 1 }),
          onPageSizeChange: (size) => setUrlState({ pageSize: size, page: 1 }),
        }}
        footerInfo={() => {
          return `${data?.totalCount || 0} payroll runs • Page ${data?.pageNumber || urlState.page} of ${data?.totalPages || 1}`
        }}
      />

      {/* Approve Modal */}
      <Modal
        isOpen={!!approvingRun}
        onClose={() => setApprovingRun(null)}
        title="Approve Payroll Run"
      >
        <div className="space-y-4">
          <p>
            Are you sure you want to approve payroll run for{' '}
            <span className="font-semibold">
              {approvingRun ? getMonthYear(approvingRun.payrollMonth, approvingRun.payrollYear) : ''}
            </span>?
          </p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setApprovingRun(null)}>
              Cancel
            </Button>
            <Button
              onClick={handleApproveConfirm}
              disabled={approvePayrollRun.isPending}
            >
              {approvePayrollRun.isPending ? 'Approving...' : 'Approve'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Mark as Paid Modal */}
      <Modal
        isOpen={!!payingRun}
        onClose={() => {
          setPayingRun(null)
          setPaymentData({ paymentReference: '', paymentMode: '', remarks: '' })
        }}
        title="Mark Payroll as Paid"
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Payment Reference
            </label>
            <input
              type="text"
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={paymentData.paymentReference}
              onChange={(e) => setPaymentData({ ...paymentData, paymentReference: e.target.value })}
              placeholder="Transaction ID, cheque number, etc."
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Payment Mode
            </label>
            <select
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={paymentData.paymentMode}
              onChange={(e) => setPaymentData({ ...paymentData, paymentMode: e.target.value })}
            >
              <option value="">Select payment mode</option>
              <option value="bank_transfer">Bank Transfer</option>
              <option value="cheque">Cheque</option>
              <option value="cash">Cash</option>
              <option value="online">Online</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Remarks
            </label>
            <textarea
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={paymentData.remarks}
              onChange={(e) => setPaymentData({ ...paymentData, remarks: e.target.value })}
              rows={3}
              placeholder="Additional notes..."
            />
          </div>
          <div className="flex justify-end gap-3">
            <Button
              variant="outline"
              onClick={() => {
                setPayingRun(null)
                setPaymentData({ paymentReference: '', paymentMode: '', remarks: '' })
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleMarkAsPaidConfirm}
              disabled={markPayrollAsPaid.isPending}
            >
              {markPayrollAsPaid.isPending ? 'Saving...' : 'Mark as Paid'}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

export default PayrollRuns




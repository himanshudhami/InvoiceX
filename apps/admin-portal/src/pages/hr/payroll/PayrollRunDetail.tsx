import { useParams, useNavigate } from 'react-router-dom'
import { ColumnDef } from '@tanstack/react-table'
import { usePayrollRun, usePayrollRunSummary, usePayrollTransactions, useApprovePayrollRun, useMarkPayrollAsPaid } from '@/features/payroll/hooks'
import { PayrollTransaction } from '@/features/payroll/types/payroll'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { formatINR } from '@/lib/currency'
import { ArrowLeft, CheckCircle, DollarSign, Download, Eye } from 'lucide-react'
import { format } from 'date-fns'
import { useState, useMemo, useCallback } from 'react'
import { exportPayrollRunToExcel } from '@/services/export/payrollExportService'
import {
  MarkAsPaidDrawer,
  createPayrollPaymentEntity,
  type PaymentEntity,
  type MarkAsPaidFormData,
  type MarkAsPaidResult,
} from '@/components/payments'

const PayrollRunDetail = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [approvingRun, setApprovingRun] = useState(false)
  const [paymentDrawerOpen, setPaymentDrawerOpen] = useState(false)
  const [paymentEntity, setPaymentEntity] = useState<PaymentEntity | null>(null)

  const { data: run, isLoading: isLoadingRun } = usePayrollRun(id!, !!id)
  const { data: summary } = usePayrollRunSummary(id!, !!id)
  const { data: transactionsData, isLoading: isLoadingTransactions } = usePayrollTransactions({
    payrollRunId: id,
    pageSize: 100,
  })

  const approvePayrollRun = useApprovePayrollRun()
  const markPayrollAsPaid = useMarkPayrollAsPaid()

  const transactions = transactionsData?.items || []

  // Calculate totals for footer
  const totals = useMemo(() => {
    const result = {
      grossEarnings: 0,
      totalDeductions: 0,
      netPayable: 0,
      count: transactions.length
    }

    transactions.forEach(transaction => {
      result.grossEarnings += transaction.grossEarnings || 0
      result.totalDeductions += transaction.totalDeductions || 0
      result.netPayable += transaction.netPayable || 0
    })

    return result
  }, [transactions])

  const handleApprove = async () => {
    if (!run) return
    try {
      await approvePayrollRun.mutateAsync({ id: run.id })
      setApprovingRun(false)
    } catch (error) {
      console.error('Failed to approve payroll run:', error)
    }
  }

  const handleOpenPaymentDrawer = () => {
    if (!run) return
    const entity = createPayrollPaymentEntity({
      id: run.id,
      companyId: run.companyId,
      payrollMonth: run.payrollMonth,
      payrollYear: run.payrollYear,
      totalNetSalary: run.totalNetSalary,
    })
    setPaymentEntity(entity)
    setPaymentDrawerOpen(true)
  }

  const handlePaymentSubmit = useCallback(async (data: MarkAsPaidFormData): Promise<MarkAsPaidResult> => {
    try {
      const result = await markPayrollAsPaid.mutateAsync({
        id: data.entityId,
        data: {
          paymentReference: data.referenceNumber,
          paymentMode: data.paymentMethod,
          remarks: data.notes,
          bankAccountId: data.bankAccountId,
          updatedBy: 'current-user', // TODO: Get from auth context
        },
      })
      return {
        success: true,
        journalEntryId: result?.disbursementJournalEntryId,
        message: result?.message || 'Payroll marked as paid successfully',
      }
    } catch (error: any) {
      return {
        success: false,
        error: error?.message || 'Failed to mark payroll as paid',
      }
    }
  }, [markPayrollAsPaid])

  const handlePaymentSuccess = () => {
    setPaymentDrawerOpen(false)
    setPaymentEntity(null)
  }

  const handleViewPayslip = (transactionId: string) => {
    navigate(`/payroll/payslip/${transactionId}`)
  }

  const handleExportToExcel = () => {
    if (!run || transactions.length === 0) {
      return
    }
    
    exportPayrollRunToExcel({
      payrollRun: run,
      transactions,
    })
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
    return <Badge className={config.className}>{config.label}</Badge>
  }

  const getMonthYear = (month: number, year: number) => {
    const date = new Date(year, month - 1, 1)
    return format(date, 'MMMM yyyy')
  }

  const columns: ColumnDef<PayrollTransaction>[] = [
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => row.original.employeeName || '—',
    },
    {
      accessorKey: 'grossEarnings',
      header: 'Gross',
      cell: ({ row }) => formatINR(row.original.grossEarnings),
    },
    {
      accessorKey: 'totalDeductions',
      header: 'Deductions',
      cell: ({ row }) => formatINR(row.original.totalDeductions),
    },
    {
      accessorKey: 'netPayable',
      header: 'Net Pay',
      cell: ({ row }) => formatINR(row.original.netPayable),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => (
        <Button variant="ghost" size="sm" onClick={() => handleViewPayslip(row.original.id)}>
          <Eye className="w-4 h-4" />
        </Button>
      ),
    },
  ]

  if (isLoadingRun) {
    return (
      <div className="space-y-6">
        <div className="h-8 bg-gray-200 rounded animate-pulse w-1/4"></div>
        <div className="h-64 bg-gray-200 rounded animate-pulse"></div>
      </div>
    )
  }

  if (!run) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" onClick={() => navigate('/payroll/runs')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Payroll Runs
        </Button>
        <div className="text-center py-8 text-gray-500">Payroll run not found</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" onClick={() => navigate('/payroll/runs')}>
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              Payroll Run - {getMonthYear(run.payrollMonth, run.payrollYear)}
            </h1>
            <p className="text-gray-600 mt-1">{run.companyName || 'Company'}</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          {getStatusBadge(run.status)}
          {run.status === 'computed' && (
            <Button onClick={() => setApprovingRun(true)}>
              <CheckCircle className="w-4 h-4 mr-2" />
              Approve
            </Button>
          )}
          {run.status === 'approved' && (
            <Button onClick={handleOpenPaymentDrawer}>
              <DollarSign className="w-4 h-4 mr-2" />
              Mark as Paid
            </Button>
          )}
          <Button variant="outline" onClick={handleExportToExcel} disabled={!run || transactions.length === 0}>
            <Download className="w-4 h-4 mr-2" />
            Export to Excel
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">Total Employees</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{run.totalEmployees}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">Total Gross</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatINR(run.totalGrossSalary)}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">Total Deductions</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatINR(run.totalDeductions)}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">Net Pay</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatINR(run.totalNetSalary)}</div>
          </CardContent>
        </Card>
      </div>

      {/* Detailed Summary */}
      {summary && (
        <Card>
          <CardHeader>
            <CardTitle>Detailed Summary</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div>
                <div className="text-sm text-gray-500">PF (Employee)</div>
                <div className="text-lg font-semibold">{formatINR(summary.totalPfEmployee)}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">PF (Employer)</div>
                <div className="text-lg font-semibold">{formatINR(summary.totalPfEmployer)}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">ESI (Employee)</div>
                <div className="text-lg font-semibold">{formatINR(summary.totalEsiEmployee)}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">ESI (Employer)</div>
                <div className="text-lg font-semibold">{formatINR(summary.totalEsiEmployer)}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">Professional Tax</div>
                <div className="text-lg font-semibold">{formatINR(summary.totalPt)}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">TDS</div>
                <div className="text-lg font-semibold">{formatINR(summary.totalTds)}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">Total Employer Cost</div>
                <div className="text-lg font-semibold">{formatINR(summary.totalEmployerCost)}</div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Transactions Table */}
      <Card>
        <CardHeader>
          <CardTitle>Payroll Transactions</CardTitle>
        </CardHeader>
        <CardContent>
          <DataTable
            columns={columns}
            data={transactions}
            isLoading={isLoadingTransactions}
            searchPlaceholder="Search employees..."
            footer={(table) => {
              if (table.getFilteredRowModel().rows.length === 0) return null
              return (
                <tfoot className="bg-gray-100 border-t-2 border-gray-300">
                  <tr className="font-semibold">
                    <td className="px-6 py-4 text-sm text-gray-900">
                      Totals ({totals.count} {totals.count === 1 ? 'employee' : 'employees'})
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900">
                      <div className="font-bold">{formatINR(totals.grossEarnings)}</div>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900">
                      <div className="font-bold">{formatINR(totals.totalDeductions)}</div>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900">
                      <div className="font-bold">{formatINR(totals.netPayable)}</div>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900"></td>
                  </tr>
                </tfoot>
              )
            }}
          />
        </CardContent>
      </Card>

      {/* Approve Modal */}
      <Modal
        isOpen={approvingRun}
        onClose={() => setApprovingRun(false)}
        title="Approve Payroll Run"
      >
        <div className="space-y-4">
          <p>
            Are you sure you want to approve this payroll run? This action cannot be undone.
          </p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setApprovingRun(false)}>
              Cancel
            </Button>
            <Button onClick={handleApprove} disabled={approvePayrollRun.isPending}>
              {approvePayrollRun.isPending ? 'Approving...' : 'Approve'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Mark as Paid Drawer */}
      <MarkAsPaidDrawer
        isOpen={paymentDrawerOpen}
        onClose={() => {
          setPaymentDrawerOpen(false)
          setPaymentEntity(null)
        }}
        entity={paymentEntity}
        onSubmit={handlePaymentSubmit}
        onSuccess={handlePaymentSuccess}
      />
    </div>
  )
}

export default PayrollRunDetail






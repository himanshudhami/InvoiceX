import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import {
  useContractorPayments,
  useDeleteContractorPayment,
  useApproveContractorPayment,
  useMarkContractorPaymentAsPaid,
} from '@/features/payroll/hooks'
import { useEmployees } from '@/hooks/api/useEmployees'
import { useCompanies } from '@/hooks/api/useCompanies'
import { ContractorPayment } from '@/features/payroll/types/payroll'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { formatINR } from '@/lib/currency'
import { Edit, Trash2, Plus, CheckCircle, DollarSign, ArrowLeft } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ContractorPaymentForm } from '@/components/forms/ContractorPaymentForm'

const ContractorPaymentsPage = () => {
  const navigate = useNavigate()
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingPayment, setEditingPayment] = useState<ContractorPayment | null>(null)
  const [deletingPayment, setDeletingPayment] = useState<ContractorPayment | null>(null)
  const [approvingPayment, setApprovingPayment] = useState<ContractorPayment | null>(null)
  const [payingPayment, setPayingPayment] = useState<ContractorPayment | null>(null)
  const [paymentData, setPaymentData] = useState({ paymentReference: '', paymentMode: '', remarks: '' })

  const { data: employees = [] } = useEmployees(urlState.companyId || undefined)
  const { data: companies = [] } = useCompanies()
  const deleteContractorPayment = useDeleteContractorPayment()
  const approveContractorPayment = useApproveContractorPayment()
  const markContractorPaymentAsPaid = useMarkContractorPaymentAsPaid()

  const [urlState, setUrlState] = useQueryStates(
    {
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(100),
      searchTerm: parseAsString,
      companyId: parseAsString,
      employeeId: parseAsString,
      paymentMonth: parseAsInteger,
      paymentYear: parseAsInteger,
      status: parseAsString,
    },
    { history: 'push' }
  )

  const { data, isLoading, error } = useContractorPayments({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: urlState.searchTerm || undefined,
    companyId: urlState.companyId || undefined,
    employeeId: urlState.employeeId || undefined,
    paymentMonth: urlState.paymentMonth || undefined,
    paymentYear: urlState.paymentYear || undefined,
    status: urlState.status || undefined,
  })

  const handleEdit = (payment: ContractorPayment) => {
    setEditingPayment(payment)
  }

  const handleDelete = (payment: ContractorPayment) => {
    setDeletingPayment(payment)
  }

  const handleDeleteConfirm = async () => {
    if (deletingPayment) {
      try {
        await deleteContractorPayment.mutateAsync(deletingPayment.id)
        setDeletingPayment(null)
      } catch (error) {
        console.error('Failed to delete contractor payment:', error)
      }
    }
  }

  const handleApprove = (payment: ContractorPayment) => {
    setApprovingPayment(payment)
  }

  const handleApproveConfirm = async () => {
    if (approvingPayment) {
      try {
        await approveContractorPayment.mutateAsync(approvingPayment.id)
        setApprovingPayment(null)
      } catch (error) {
        console.error('Failed to approve contractor payment:', error)
      }
    }
  }

  const handleMarkAsPaid = (payment: ContractorPayment) => {
    setPayingPayment(payment)
  }

  const handleMarkAsPaidConfirm = async () => {
    if (payingPayment) {
      try {
        await markContractorPaymentAsPaid.mutateAsync({
          id: payingPayment.id,
          data: {
            paymentReference: paymentData.paymentReference,
            paymentMethod: paymentData.paymentMode,
            paymentDate: new Date().toISOString(),
            updatedBy: 'current-user', // TODO: Get from auth context
          },
        })
        setPayingPayment(null)
        setPaymentData({ paymentReference: '', paymentMode: '', remarks: '' })
      } catch (error) {
        console.error('Failed to mark contractor payment as paid:', error)
      }
    }
  }

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { label: string; className: string }> = {
      pending: { label: 'Pending', className: 'bg-gray-100 text-gray-800' },
      approved: { label: 'Approved', className: 'bg-green-100 text-green-800' },
      paid: { label: 'Paid', className: 'bg-blue-100 text-blue-800' },
      cancelled: { label: 'Cancelled', className: 'bg-red-100 text-red-800' },
    }

    const config = statusConfig[status] || statusConfig.pending
    return <Badge className={config.className}>{config.label}</Badge>
  }

  const getMonthYear = (month: number, year: number) => {
    const date = new Date(year, month - 1, 1)
    return format(date, 'MMM yyyy')
  }

  const columns: ColumnDef<ContractorPayment>[] = [
    {
      accessorKey: 'employeeName',
      header: 'Contractor',
      cell: ({ row }) => row.original.employeeName || '—',
    },
    {
      accessorKey: 'paymentMonth',
      header: 'Period',
      cell: ({ row }) => getMonthYear(row.original.paymentMonth, row.original.paymentYear),
    },
    {
      accessorKey: 'grossAmount',
      header: 'Gross Amount',
      cell: ({ row }) => formatINR(row.original.grossAmount),
    },
    {
      accessorKey: 'tdsAmount',
      header: 'TDS (10%)',
      cell: ({ row }) => formatINR(row.original.tdsAmount),
    },
    {
      accessorKey: 'netPayable',
      header: 'Net Payable',
      cell: ({ row }) => formatINR(row.original.netPayable),
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
        const payment = row.original
        return (
          <div className="flex items-center gap-2">
            {payment.status === 'pending' && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleApprove(payment)}
                title="Approve"
              >
                <CheckCircle className="w-4 h-4" />
              </Button>
            )}
            {payment.status === 'approved' && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleMarkAsPaid(payment)}
                title="Mark as Paid"
              >
                <DollarSign className="w-4 h-4" />
              </Button>
            )}
            {payment.status !== 'paid' && (
              <>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleEdit(payment)}
                  title="Edit"
                >
                  <Edit className="w-4 h-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleDelete(payment)}
                  title="Delete"
                >
                  <Trash2 className="w-4 h-4" />
                </Button>
              </>
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
          <h1 className="text-2xl font-bold text-gray-900">Contractor Payments</h1>
          <p className="text-gray-600 mt-1">Manage contractor and freelancer payments</p>
        </div>
        <div className="flex gap-3">
          <CompanyFilterDropdown
            value={urlState.companyId || ''}
            onChange={(value) => setUrlState({ companyId: value || null })}
          />
          <Button onClick={() => setIsCreateDrawerOpen(true)}>
            <Plus className="w-4 h-4 mr-2" />
            Add Payment
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-600">Failed to load contractor payments</p>
        </div>
      )}

      <DataTable
        columns={columns}
        data={data?.items || []}
        isLoading={isLoading}
        searchPlaceholder="Search by contractor name, invoice number..."
        onSearch={(value) => setUrlState({ searchTerm: value || null, page: 1 })}
        pagination={{
          pageIndex: (data?.pageNumber || urlState.page) - 1,
          pageSize: data?.pageSize || urlState.pageSize,
          totalCount: data?.totalCount || 0,
          onPageChange: (page) => setUrlState({ page: page + 1 }),
          onPageSizeChange: (size) => setUrlState({ pageSize: size, page: 1 }),
        }}
        footerInfo={() => {
          return `${data?.totalCount || 0} contractor payments • Page ${data?.pageNumber || urlState.page} of ${data?.totalPages || 1}`
        }}
      />

      {/* Create/Edit Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen || !!editingPayment}
        onClose={() => {
          setIsCreateDrawerOpen(false)
          setEditingPayment(null)
        }}
        title={editingPayment ? 'Edit Contractor Payment' : 'Add Contractor Payment'}
      >
        <ContractorPaymentForm
          payment={editingPayment || undefined}
          onSuccess={() => {
            setIsCreateDrawerOpen(false)
            setEditingPayment(null)
          }}
          onCancel={() => {
            setIsCreateDrawerOpen(false)
            setEditingPayment(null)
          }}
        />
      </Drawer>

      {/* Delete Modal */}
      <Modal
        isOpen={!!deletingPayment}
        onClose={() => setDeletingPayment(null)}
        title="Delete Contractor Payment"
      >
        <div className="space-y-4">
          <p>
            Are you sure you want to delete this contractor payment?
          </p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setDeletingPayment(null)}>
              Cancel
            </Button>
            <Button
              onClick={handleDeleteConfirm}
              disabled={deleteContractorPayment.isPending}
            >
              {deleteContractorPayment.isPending ? 'Deleting...' : 'Delete'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Approve Modal */}
      <Modal
        isOpen={!!approvingPayment}
        onClose={() => setApprovingPayment(null)}
        title="Approve Contractor Payment"
      >
        <div className="space-y-4">
          <p>
            Are you sure you want to approve this contractor payment?
          </p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setApprovingPayment(null)}>
              Cancel
            </Button>
            <Button
              onClick={handleApproveConfirm}
              disabled={approveContractorPayment.isPending}
            >
              {approveContractorPayment.isPending ? 'Approving...' : 'Approve'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Mark as Paid Modal */}
      <Modal
        isOpen={!!payingPayment}
        onClose={() => {
          setPayingPayment(null)
          setPaymentData({ paymentReference: '', paymentMode: '', remarks: '' })
        }}
        title="Mark Contractor Payment as Paid"
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
          <div className="flex justify-end gap-3">
            <Button
              variant="outline"
              onClick={() => {
                setPayingPayment(null)
                setPaymentData({ paymentReference: '', paymentMode: '', remarks: '' })
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleMarkAsPaidConfirm}
              disabled={markContractorPaymentAsPaid.isPending}
            >
              {markContractorPaymentAsPaid.isPending ? 'Saving...' : 'Mark as Paid'}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

export default ContractorPaymentsPage




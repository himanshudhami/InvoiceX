import { useMemo, useState, useCallback, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import {
  useContractorPayments,
  useDeleteContractorPayment,
  useApproveContractorPayment,
  useMarkContractorPaymentAsPaid,
} from '@/features/payroll/hooks'
import { useVendorsPaged } from '@/features/parties/hooks'
import type { PartyListItem } from '@/services/api/types'
import { ContractorPayment } from '@/features/payroll/types/payroll'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { formatINR } from '@/lib/currency'
import {
  Edit,
  Trash2,
  CheckCircle,
  DollarSign,
  Eye,
  CreditCard,
  Banknote,
  AlertTriangle,
  Clock,
  Landmark,
} from 'lucide-react'
import { format } from 'date-fns'
import { Button } from '@/components/ui/button'
import { ContractorPaymentForm } from '@/components/forms/ContractorPaymentForm'
import {
  MarkAsPaidDrawer,
  createContractorPaymentEntity,
  type PaymentEntity,
  type MarkAsPaidFormData,
  type MarkAsPaidResult,
} from '@/components/payments'

const STATUS_COLORS: Record<string, string> = {
  pending: 'bg-gray-100 text-gray-800',
  approved: 'bg-blue-100 text-blue-800',
  paid: 'bg-green-100 text-green-800',
  cancelled: 'bg-red-100 text-red-800',
}

const ContractorPaymentsPage = () => {
  const navigate = useNavigate()
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingPayment, setEditingPayment] = useState<ContractorPayment | null>(null)
  const [viewingPayment, setViewingPayment] = useState<ContractorPayment | null>(null)
  const [deletingPayment, setDeletingPayment] = useState<ContractorPayment | null>(null)
  const [approvingPayment, setApprovingPayment] = useState<ContractorPayment | null>(null)
  const [paymentDrawerOpen, setPaymentDrawerOpen] = useState(false)
  const [paymentEntity, setPaymentEntity] = useState<PaymentEntity | null>(null)

  const [urlState, setUrlState] = useQueryStates(
    {
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(100),
      searchTerm: parseAsString,
      companyId: parseAsString,
      partyId: parseAsString,
      paymentMonth: parseAsInteger,
      paymentYear: parseAsInteger,
      status: parseAsString,
      highlight: parseAsString,
    },
    { history: 'push' }
  )

  // Debounced search term - only updates after 300ms delay
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState(urlState.searchTerm || '')

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(urlState.searchTerm || '')
    }, 300)
    return () => clearTimeout(timer)
  }, [urlState.searchTerm])

  const { data, isLoading, error, refetch } = useContractorPayments({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: debouncedSearchTerm || undefined,
    companyId: urlState.companyId || undefined,
    partyId: urlState.partyId || undefined,
    paymentMonth: urlState.paymentMonth || undefined,
    paymentYear: urlState.paymentYear || undefined,
    status: urlState.status || undefined,
  })

  // Fetch contractors (vendors are used as contractors)
  const { data: vendorsData } = useVendorsPaged({
    // @ts-ignore companyId works at runtime but type definition doesn't include it
    companyId: urlState.companyId || undefined,
    pageSize: 100,
  })
  const contractors: PartyListItem[] = (vendorsData as { items?: PartyListItem[] })?.items || []
  const deleteContractorPayment = useDeleteContractorPayment()
  const approveContractorPayment = useApproveContractorPayment()
  const markContractorPaymentAsPaid = useMarkContractorPaymentAsPaid()

  // Calculate summary stats
  const stats = useMemo(() => {
    const items = data?.items || []
    const totalAmount = items.reduce((sum, p) => sum + (p.netPayable || 0), 0)
    const totalTds = items.reduce((sum, p) => sum + (p.tdsAmount || 0), 0)
    const pendingCount = items.filter(p => ['pending', 'approved'].includes(p.status)).length
    return { totalAmount, totalTds, pendingCount, count: items.length }
  }, [data?.items])

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

  const handleOpenPaymentDrawer = (payment: ContractorPayment) => {
    const entity = createContractorPaymentEntity({
      id: payment.id,
      companyId: payment.companyId,
      contractorName: payment.partyName || 'Contractor',
      invoiceNumber: payment.invoiceNumber,
      totalAmount: payment.netPayable,
    })
    setPaymentEntity(entity)
    setPaymentDrawerOpen(true)
  }

  const handlePaymentSubmit = useCallback(async (data: MarkAsPaidFormData): Promise<MarkAsPaidResult> => {
    try {
      await markContractorPaymentAsPaid.mutateAsync({
        id: data.entityId,
        data: {
          paymentReference: data.referenceNumber,
          paymentMethod: data.paymentMethod,
          paymentDate: data.paymentDate,
          remarks: data.notes,
          bankAccountId: data.bankAccountId,
          updatedBy: 'current-user', // TODO: Get from auth context
        },
      })
      return {
        success: true,
        message: 'Contractor payment marked as paid successfully',
      }
    } catch (error: any) {
      return {
        success: false,
        error: error?.message || 'Failed to mark contractor payment as paid',
      }
    }
  }, [markContractorPaymentAsPaid])

  const handlePaymentSuccess = () => {
    setPaymentDrawerOpen(false)
    setPaymentEntity(null)
  }

  const getStatusDisplay = (status: string) => {
    const displayStatus = status?.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase()) || 'Pending'
    return (
      <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${STATUS_COLORS[status] || STATUS_COLORS.pending}`}>
        {displayStatus}
      </div>
    )
  }

  const getMonthYear = (month: number, year: number) => {
    const date = new Date(year, month - 1, 1)
    return format(date, 'MMM yyyy')
  }

  const columns: ColumnDef<ContractorPayment>[] = [
    {
      accessorKey: 'partyName',
      header: 'Contractor',
      cell: ({ row }) => {
        const payment = row.original
        return (
          <div>
            <div className="text-sm text-gray-900">{payment.partyName || '—'}</div>
            {payment.invoiceNumber && (
              <div className="text-xs text-gray-500">Inv: {payment.invoiceNumber}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'paymentMonth',
      header: 'Period',
      cell: ({ row }) => {
        const payment = row.original
        return (
          <div className="text-sm text-gray-900">
            {getMonthYear(payment.paymentMonth, payment.paymentYear)}
          </div>
        )
      },
    },
    {
      accessorKey: 'grossAmount',
      header: 'Gross Amount',
      cell: ({ row }) => {
        const payment = row.original
        return (
          <div className="text-sm font-medium text-gray-900">
            {formatINR(payment.grossAmount)}
          </div>
        )
      },
    },
    {
      accessorKey: 'tdsAmount',
      header: 'TDS',
      cell: ({ row }) => {
        const payment = row.original
        if (!payment.tdsAmount) {
          return <span className="text-gray-500">—</span>
        }
        return (
          <div>
            <div className="text-sm text-purple-600">
              {formatINR(payment.tdsAmount)}
            </div>
            <div className="text-xs text-gray-500">
              {payment.tdsSection} @ {payment.tdsRate}%
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'netPayable',
      header: 'Net Payable',
      cell: ({ row }) => {
        const payment = row.original
        return (
          <div className="text-sm font-medium text-green-600">
            {formatINR(payment.netPayable)}
          </div>
        )
      },
    },
    {
      accessorKey: 'paymentReference',
      header: 'Reference',
      cell: ({ row }) => {
        const payment = row.original
        if (!payment.paymentReference) {
          return <span className="text-gray-400">—</span>
        }
        const methodLabel = payment.paymentMethod || 'Ref'
        return (
          <span className="text-sm text-gray-700">
            {methodLabel} ({payment.paymentReference})
          </span>
        )
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusDisplay(row.original.status),
    },
    {
      accessorKey: 'isReconciled',
      header: 'Reconciled',
      cell: ({ row }) => {
        const payment = row.original
        // Only show reconciliation status for paid payments
        if (payment.status !== 'paid') {
          return <span className="text-gray-400">—</span>
        }
        // Check if reconciled (either isReconciled flag or reconciledAt timestamp)
        const isReconciled = payment.isReconciled || !!payment.reconciledAt
        return isReconciled ? (
          <div className="inline-flex items-center gap-1">
            <div className="inline-flex items-center text-green-600">
              <CheckCircle size={14} className="mr-1" />
              <span className="text-xs">Yes</span>
            </div>
            {payment.bankTransactionId && (
              <Button
                variant="ghost"
                size="icon"
                className="h-6 w-6 text-blue-600 hover:text-blue-700 hover:bg-blue-50"
                onClick={() => navigate(`/bank/transactions?highlight=${payment.bankTransactionId}`)}
                title="View Bank Transaction"
              >
                <Landmark size={14} />
              </Button>
            )}
          </div>
        ) : (
          <div className="inline-flex items-center text-yellow-600">
            <Clock size={14} className="mr-1" />
            <span className="text-xs">Pending</span>
          </div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const payment = row.original
        const status = (payment.status || '').toLowerCase()
        return (
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setViewingPayment(payment)}
              title="View Details"
            >
              <Eye className="w-4 h-4" />
            </Button>
            {status === 'pending' && (
              <>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleApprove(payment)}
                  title="Approve"
                  className="text-green-600 hover:text-green-700 hover:bg-green-50"
                >
                  <CheckCircle className="w-4 h-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleEdit(payment)}
                  title="Edit"
                  className="hover:text-blue-700 hover:bg-blue-50"
                >
                  <Edit className="w-4 h-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleDelete(payment)}
                  title="Delete"
                  className="text-red-600 hover:text-red-700 hover:bg-red-50"
                >
                  <Trash2 className="w-4 h-4" />
                </Button>
              </>
            )}
            {status === 'approved' && (
              <>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleOpenPaymentDrawer(payment)}
                  title="Mark as Paid"
                  className="text-blue-600 hover:text-blue-700 hover:bg-blue-50"
                >
                  <DollarSign className="w-4 h-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleEdit(payment)}
                  title="Edit"
                  className="hover:text-blue-700 hover:bg-blue-50"
                >
                  <Edit className="w-4 h-4" />
                </Button>
              </>
            )}
          </div>
        )
      },
    },
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
        <div className="text-red-600 mb-4">Failed to load contractor payments</div>
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
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <Link to="/payroll" className="text-gray-500 hover:text-gray-700">
              Payroll
            </Link>
            <span className="text-gray-400">/</span>
            <h1 className="text-3xl font-bold text-gray-900">Contractor Payments</h1>
          </div>
          <p className="text-gray-600 mt-2">Manage contractor and freelancer payments</p>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <CreditCard className="h-8 w-8 text-blue-600" />
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Total Payments</p>
              <p className="text-2xl font-semibold text-gray-900">{stats.count}</p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <Banknote className="h-8 w-8 text-green-600" />
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Total Amount</p>
              <p className="text-2xl font-semibold text-gray-900">
                {stats.totalAmount.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <div className="h-8 w-8 bg-purple-100 rounded-full flex items-center justify-center">
              <span className="text-purple-600 font-semibold text-sm">TDS</span>
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Total TDS</p>
              <p className="text-2xl font-semibold text-gray-900">
                {stats.totalTds.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <AlertTriangle className="h-8 w-8 text-yellow-600" />
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Pending</p>
              <p className="text-2xl font-semibold text-gray-900">{stats.pendingCount}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="mb-4 flex flex-wrap items-center gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
              <CompanyFilterDropdown
                value={urlState.companyId ?? ''}
                onChange={(value) => setUrlState({ companyId: value || null, page: 1 })}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Contractor</label>
              <select
                value={urlState.partyId || ''}
                onChange={(e) => setUrlState({ partyId: e.target.value || null, page: 1 })}
                className="w-48 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">All Contractors</option>
                {contractors.map((c: PartyListItem) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Status</label>
              <select
                value={urlState.status || ''}
                onChange={(e) => setUrlState({ status: e.target.value || null, page: 1 })}
                className="w-40 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">All Status</option>
                <option value="pending">Pending</option>
                <option value="approved">Approved</option>
                <option value="paid">Paid</option>
                <option value="cancelled">Cancelled</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Month</label>
              <select
                value={urlState.paymentMonth || ''}
                onChange={(e) => setUrlState({ paymentMonth: e.target.value ? parseInt(e.target.value) : null, page: 1 })}
                className="w-32 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">All</option>
                {Array.from({ length: 12 }, (_, i) => i + 1).map((month) => (
                  <option key={month} value={month}>
                    {new Date(2000, month - 1, 1).toLocaleString('default', { month: 'short' })}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Year</label>
              <select
                value={urlState.paymentYear || ''}
                onChange={(e) => setUrlState({ paymentYear: e.target.value ? parseInt(e.target.value) : null, page: 1 })}
                className="w-28 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">All</option>
                {Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - i).map((year) => (
                  <option key={year} value={year}>{year}</option>
                ))}
              </select>
            </div>
          </div>
          <DataTable
            columns={columns}
            data={data?.items || []}
            searchPlaceholder="Search by contractor name, invoice number..."
            searchValue={urlState.searchTerm || ''}
            onSearchChange={(value: string) => setUrlState({ searchTerm: value || null, page: 1 })}
            highlightedRowId={urlState.highlight || undefined}
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Payment"
            pagination={{
              pageIndex: (data?.pageNumber || urlState.page) - 1,
              pageSize: data?.pageSize || urlState.pageSize,
              totalCount: data?.totalCount || 0,
              onPageChange: (page) => setUrlState({ page: page + 1 }),
              onPageSizeChange: (size) => setUrlState({ pageSize: size, page: 1 }),
            }}
          />
        </div>
      </div>

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
          defaultCompanyId={urlState.companyId || undefined}
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

      {/* View Details Drawer */}
      <Drawer
        isOpen={!!viewingPayment}
        onClose={() => setViewingPayment(null)}
        title="Contractor Payment Details"
      >
        {viewingPayment && (
          <div className="space-y-6">
            {/* Contractor Info */}
            <div className="bg-gray-50 rounded-lg p-4">
              <h3 className="text-sm font-medium text-gray-500 mb-2">Contractor</h3>
              <p className="text-lg font-semibold text-gray-900">{viewingPayment.partyName}</p>
              {viewingPayment.contractorPan && (
                <p className="text-sm text-gray-600">PAN: {viewingPayment.contractorPan}</p>
              )}
            </div>

            {/* Payment Period */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <h3 className="text-sm font-medium text-gray-500">Period</h3>
                <p className="text-gray-900">{getMonthYear(viewingPayment.paymentMonth, viewingPayment.paymentYear)}</p>
              </div>
              <div>
                <h3 className="text-sm font-medium text-gray-500">Status</h3>
                {getStatusDisplay(viewingPayment.status)}
              </div>
            </div>

            {/* Invoice & Contract Reference */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <h3 className="text-sm font-medium text-gray-500">Invoice Number</h3>
                <p className="text-gray-900">{viewingPayment.invoiceNumber || '—'}</p>
              </div>
              <div>
                <h3 className="text-sm font-medium text-gray-500">Contract Reference</h3>
                <p className="text-gray-900">{viewingPayment.contractReference || '—'}</p>
              </div>
            </div>

            {/* Financial Details */}
            <div className="border rounded-lg divide-y">
              <div className="flex justify-between p-3">
                <span className="text-gray-600">Gross Amount</span>
                <span className="font-medium">{formatINR(viewingPayment.grossAmount)}</span>
              </div>
              {viewingPayment.gstApplicable && (
                <div className="flex justify-between p-3">
                  <span className="text-gray-600">GST ({viewingPayment.gstRate}%)</span>
                  <span className="font-medium">{formatINR(viewingPayment.gstAmount)}</span>
                </div>
              )}
              {viewingPayment.totalInvoiceAmount && (
                <div className="flex justify-between p-3 bg-gray-50">
                  <span className="text-gray-600">Total Invoice Amount</span>
                  <span className="font-medium">{formatINR(viewingPayment.totalInvoiceAmount)}</span>
                </div>
              )}
              <div className="flex justify-between p-3">
                <span className="text-gray-600">TDS ({viewingPayment.tdsSection} @ {viewingPayment.tdsRate}%)</span>
                <span className="font-medium text-red-600">- {formatINR(viewingPayment.tdsAmount)}</span>
              </div>
              {viewingPayment.otherDeductions > 0 && (
                <div className="flex justify-between p-3">
                  <span className="text-gray-600">Other Deductions</span>
                  <span className="font-medium text-red-600">- {formatINR(viewingPayment.otherDeductions)}</span>
                </div>
              )}
              <div className="flex justify-between p-3 bg-green-50">
                <span className="font-medium text-gray-900">Net Payable</span>
                <span className="font-bold text-green-600">{formatINR(viewingPayment.netPayable)}</span>
              </div>
            </div>

            {/* Payment Info (if paid) */}
            {viewingPayment.status === 'paid' && (
              <div className="border rounded-lg p-4 bg-blue-50">
                <h3 className="text-sm font-medium text-blue-800 mb-3">Payment Information</h3>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-gray-600">Payment Date</span>
                    <p className="text-gray-900">
                      {viewingPayment.paymentDate ? format(new Date(viewingPayment.paymentDate), 'dd MMM yyyy') : '—'}
                    </p>
                  </div>
                  <div>
                    <span className="text-gray-600">Payment Method</span>
                    <p className="text-gray-900">{viewingPayment.paymentMethod || '—'}</p>
                  </div>
                  <div>
                    <span className="text-gray-600">Reference</span>
                    <p className="text-gray-900">{viewingPayment.paymentReference || '—'}</p>
                  </div>
                  <div>
                    <span className="text-gray-600">Reconciled</span>
                    <p className="text-gray-900">
                      {viewingPayment.isReconciled ? (
                        <span className="text-green-600 flex items-center gap-1">
                          <CheckCircle size={14} /> Yes
                        </span>
                      ) : (
                        <span className="text-yellow-600 flex items-center gap-1">
                          <Clock size={14} /> Pending
                        </span>
                      )}
                    </p>
                  </div>
                </div>
              </div>
            )}

            {/* Description & Remarks */}
            {(viewingPayment.description || viewingPayment.remarks) && (
              <div className="space-y-3">
                {viewingPayment.description && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">Description</h3>
                    <p className="text-gray-900">{viewingPayment.description}</p>
                  </div>
                )}
                {viewingPayment.remarks && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">Remarks</h3>
                    <p className="text-gray-900">{viewingPayment.remarks}</p>
                  </div>
                )}
              </div>
            )}

            {/* Action Buttons */}
            <div className="flex justify-end gap-3 pt-4 border-t">
              <Button variant="outline" onClick={() => setViewingPayment(null)}>
                Close
              </Button>
              {viewingPayment.status !== 'paid' && viewingPayment.status !== 'cancelled' && (
                <Button
                  onClick={() => {
                    setViewingPayment(null)
                    handleEdit(viewingPayment)
                  }}
                >
                  Edit Payment
                </Button>
              )}
            </div>
          </div>
        )}
      </Drawer>

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

export default ContractorPaymentsPage

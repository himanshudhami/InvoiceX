import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useInvoices, useDeleteInvoice } from '@/hooks/api/useInvoices'
import { useCustomers } from '@/hooks/api/useCustomers'
import { useCompanies } from '@/hooks/api/useCompanies'
import { Invoice } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { InvoiceForm } from '@/components/forms/InvoiceForm'
import { Eye, Edit, Trash2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'

const InvoicesManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingInvoice, setEditingInvoice] = useState<Invoice | null>(null)
  const [deletingInvoice, setDeletingInvoice] = useState<Invoice | null>(null)
  const navigate = useNavigate()

  const { data: invoices = [], isLoading, error, refetch } = useInvoices()
  const { data: customers = [] } = useCustomers(selectedCompanyId || undefined)
  const { data: companies = [] } = useCompanies()
  const deleteInvoice = useDeleteInvoice()

  const handleView = (invoice: Invoice) => {
    navigate(`/invoices/${invoice.id}`)
  }

  const handleEdit = (invoice: Invoice) => {
    setEditingInvoice(invoice)
  }

  const handleDelete = (invoice: Invoice) => {
    setDeletingInvoice(invoice)
  }

  const handleDeleteConfirm = async () => {
    if (deletingInvoice) {
      try {
        await deleteInvoice.mutateAsync(deletingInvoice.id)
        setDeletingInvoice(null)
      } catch (error) {
        console.error('Failed to delete invoice:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingInvoice(null)
    refetch()
  }

  // Helper functions to get related entity names
  const getCustomerName = (customerId?: string) => {
    if (!customerId) return '—'
    const customer = customers.find(c => c.id === customerId)
    return customer ? `${customer.name}${customer.companyName ? ` (${customer.companyName})` : ''}` : '—'
  }

  const getCompanyName = (companyId?: string) => {
    if (!companyId) return '—'
    const company = companies.find(c => c.id === companyId)
    return company?.name || '—'
  }

  const getStatusBadgeColor = (status?: string) => {
    switch (status?.toLowerCase()) {
      case 'paid':
        return 'bg-green-100 text-green-800'
      case 'sent':
        return 'bg-blue-100 text-blue-800'
      case 'viewed':
        return 'bg-purple-100 text-purple-800'
      case 'overdue':
        return 'bg-red-100 text-red-800'
      case 'cancelled':
        return 'bg-gray-100 text-gray-800'
      case 'draft':
      default:
        return 'bg-yellow-100 text-yellow-800'
    }
  }

  const formatCurrency = (amount: number, currency?: string) => {
    const currencySymbol = currency === 'EUR' ? '€' : currency === 'GBP' ? '£' : currency === 'INR' ? '₹' : '$'
    return `${currencySymbol}${amount.toFixed(2)}`
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString()
  }

  const getPaymentStatus = (totalAmount: number, paidAmount?: number) => {
    if (!paidAmount || paidAmount === 0) return 'Unpaid'
    if (paidAmount >= totalAmount) return 'Fully Paid'
    return 'Partially Paid'
  }

  const columns: ColumnDef<Invoice>[] = [
    {
      accessorKey: 'invoiceNumber',
      header: 'Invoice',
      cell: ({ row }) => {
        const invoice = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{invoice.invoiceNumber}</div>
            <div className="text-sm text-gray-500">
              {formatDate(invoice.invoiceDate)}
            </div>
            {invoice.projectName && (
              <div className="text-sm text-blue-600">{invoice.projectName}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'customerId',
      header: 'Customer',
      cell: ({ row }) => {
        const customerName = getCustomerName(row.original.customerId)
        return (
          <div className="text-sm text-gray-900">{customerName}</div>
        )
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const invoice = row.original
        const status = invoice.status || 'draft'
        return (
          <div>
            <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${getStatusBadgeColor(status)}`}>
              {status.charAt(0).toUpperCase() + status.slice(1)}
            </div>
            <div className="text-xs text-gray-500 mt-1">
              {getPaymentStatus(invoice.totalAmount, invoice.paidAmount)}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'dueDate',
      header: 'Due Date',
      cell: ({ row }) => {
        const invoice = row.original
        const dueDate = new Date(invoice.dueDate)
        const isOverdue = dueDate < new Date() && invoice.status !== 'paid'
        
        return (
          <div>
            <div className={`text-sm ${isOverdue ? 'text-red-600 font-medium' : 'text-gray-900'}`}>
              {formatDate(invoice.dueDate)}
            </div>
            {isOverdue && (
              <div className="text-xs text-red-500">Overdue</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'totalAmount',
      header: 'Amount',
      cell: ({ row }) => {
        const invoice = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">
              {formatCurrency(invoice.totalAmount, invoice.currency)}
            </div>
            {invoice.paidAmount !== undefined && invoice.paidAmount > 0 && (
              <div className="text-sm text-green-600">
                Paid: {formatCurrency(invoice.paidAmount, invoice.currency)}
              </div>
            )}
            {invoice.subtotal !== invoice.totalAmount && (
              <div className="text-xs text-gray-500">
                Subtotal: {formatCurrency(invoice.subtotal, invoice.currency)}
              </div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'companyId',
      header: 'Company',
      cell: ({ row }) => {
        const companyName = getCompanyName(row.original.companyId)
        return (
          <div className="text-sm text-gray-500">{companyName}</div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const invoice = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleView(invoice)}
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="View invoice"
            >
              <Eye size={16} />
            </button>
            <button
              onClick={() => handleEdit(invoice)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit invoice"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(invoice)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete invoice"
            >
              <Trash2 size={16} />
            </button>
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
        <div className="text-red-600 mb-4">Failed to load invoices</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Invoices</h1>
        <p className="text-gray-600 mt-2">Manage your invoices and track payments</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Invoices</div>
          <div className="text-2xl font-bold text-gray-900">{invoices.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Paid Invoices</div>
          <div className="text-2xl font-bold text-green-600">
            {invoices.filter(inv => inv.status === 'paid').length}
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Overdue</div>
          <div className="text-2xl font-bold text-red-600">
            {invoices.filter(inv => 
              new Date(inv.dueDate) < new Date() && inv.status !== 'paid'
            ).length}
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Amount</div>
          <div className="text-2xl font-bold text-gray-900">
            ${invoices.reduce((sum, inv) => sum + inv.totalAmount, 0).toFixed(2)}
          </div>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={invoices}
            searchPlaceholder="Search invoices..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Create Invoice"
          />
        </div>
      </div>

      {/* Create Invoice Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create New Invoice"
        size="xl"
      >
        <InvoiceForm
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Invoice Drawer */}
      <Drawer
        isOpen={!!editingInvoice}
        onClose={() => setEditingInvoice(null)}
        title="Edit Invoice"
        size="xl"
      >
        {editingInvoice && (
          <InvoiceForm
            invoice={editingInvoice}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingInvoice(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingInvoice}
        onClose={() => setDeletingInvoice(null)}
        title="Delete Invoice"
        size="sm"
      >
        {deletingInvoice && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete invoice <strong>{deletingInvoice.invoiceNumber}</strong>? 
              This action cannot be undone and will also delete all associated invoice items.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingInvoice(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteInvoice.isPending}
                className="px-4 py-2 text-sm font-medium text-primary-foreground bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteInvoice.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default InvoicesManagement

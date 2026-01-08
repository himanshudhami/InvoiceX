import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { ColumnDef } from '@tanstack/react-table';
import {
  useVendorPayments,
  useDeleteVendorPayment,
  useApproveVendorPayment,
  useProcessVendorPayment,
  useCancelVendorPayment,
} from '@/features/vendors/hooks';
import { useVendors } from '@/features/vendors/hooks';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { VendorPayment } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import {
  Edit,
  Trash2,
  CheckCircle,
  XCircle,
  CreditCard,
  Banknote,
  Clock,
  AlertTriangle,
} from 'lucide-react';
import { format } from 'date-fns';
import { useQueryState, parseAsString } from 'nuqs';

const STATUS_COLORS: Record<string, string> = {
  draft: 'bg-gray-100 text-gray-800',
  pending_approval: 'bg-yellow-100 text-yellow-800',
  approved: 'bg-blue-100 text-blue-800',
  processed: 'bg-green-100 text-green-800',
  cancelled: 'bg-red-100 text-red-800',
};

const PAYMENT_METHOD_LABELS: Record<string, string> = {
  bank_transfer: 'Bank Transfer',
  cheque: 'Cheque',
  cash: 'Cash',
  neft: 'NEFT',
  rtgs: 'RTGS',
  upi: 'UPI',
};

const VendorPaymentsManagement = () => {
  // Get selected company from context (for multi-company users)
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext();

  // URL-backed filter state with nuqs
  const [companyFilter, setCompanyFilter] = useQueryState('company', parseAsString.withDefault(''));
  const [vendorFilter, setVendorFilter] = useQueryState('vendorId', parseAsString.withDefault(''));
  const [statusFilter, setStatusFilter] = useQueryState('status', parseAsString.withDefault(''));

  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingPayment, setEditingPayment] = useState<VendorPayment | null>(null);
  const [deletingPayment, setDeletingPayment] = useState<VendorPayment | null>(null);

  // Determine effective company ID: URL filter takes precedence, then context selection
  const effectiveCompanyId = companyFilter || (hasMultiCompanyAccess ? selectedCompanyId : undefined);

  const { data: allPayments = [], isLoading, error, refetch } = useVendorPayments(effectiveCompanyId || undefined);
  const { data: vendors = [] } = useVendors(effectiveCompanyId || undefined);
  const deletePayment = useDeleteVendorPayment();
  const approvePayment = useApproveVendorPayment();
  const processPayment = useProcessVendorPayment();
  const cancelPayment = useCancelVendorPayment();

  // Get vendor lookup by party ID
  const vendorLookup = useMemo(() => {
    const lookup: Record<string, string> = {};
    vendors.forEach(v => { lookup[v.id] = v.name; });
    return lookup;
  }, [vendors]);

  // Filter payments
  const payments = useMemo(() => {
    let filtered = allPayments;
    if (companyFilter) {
      filtered = filtered.filter(p => p.companyId === companyFilter);
    }
    if (vendorFilter) {
      filtered = filtered.filter(p => p.partyId === vendorFilter);
    }
    if (statusFilter) {
      filtered = filtered.filter(p => p.status === statusFilter);
    }
    return filtered;
  }, [allPayments, companyFilter, vendorFilter, statusFilter]);

  // Calculate summary stats
  const stats = useMemo(() => {
    const totalAmount = payments.reduce((sum, p) => sum + (p.amount || 0), 0);
    const totalTds = payments.reduce((sum, p) => sum + (p.tdsAmount || 0), 0);
    const pendingCount = payments.filter(p => ['draft', 'pending_approval', 'approved'].includes(p.status || '')).length;
    const unreconciledCount = payments.filter(p => !p.isReconciled && p.status === 'processed').length;

    return { totalAmount, totalTds, pendingCount, unreconciledCount, count: payments.length };
  }, [payments]);

  const handleApprove = async (id: string) => {
    try {
      await approvePayment.mutateAsync(id);
      refetch();
    } catch (error) {
      console.error('Failed to approve payment:', error);
    }
  };

  const handleProcess = async (id: string) => {
    try {
      await processPayment.mutateAsync(id);
      refetch();
    } catch (error) {
      console.error('Failed to process payment:', error);
    }
  };

  const handleCancel = async (id: string) => {
    try {
      await cancelPayment.mutateAsync(id);
      refetch();
    } catch (error) {
      console.error('Failed to cancel payment:', error);
    }
  };

  const handleDeleteConfirm = async () => {
    if (deletingPayment) {
      try {
        await deletePayment.mutateAsync(deletingPayment.id);
        setDeletingPayment(null);
      } catch (error) {
        console.error('Failed to delete payment:', error);
      }
    }
  };

  const columns: ColumnDef<VendorPayment>[] = [
    {
      accessorKey: 'paymentDate',
      header: 'Date',
      cell: ({ row }) => {
        const payment = row.original;
        return (
          <div>
            <div className="text-sm text-gray-900">
              {format(new Date(payment.paymentDate), 'dd MMM yyyy')}
            </div>
            {payment.referenceNumber && (
              <div className="text-xs text-gray-500">Ref: {payment.referenceNumber}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'partyId',
      header: 'Vendor',
      cell: ({ row }) => {
        const payment = row.original;
        return (
          <div className="text-sm text-gray-900">
            {vendorLookup[payment.partyId] || payment.vendor?.name || 'Unknown'}
          </div>
        );
      },
    },
    {
      accessorKey: 'paymentMethod',
      header: 'Method',
      cell: ({ row }) => {
        const payment = row.original;
        return (
          <div>
            <div className="text-sm text-gray-900">
              {PAYMENT_METHOD_LABELS[payment.paymentMethod || ''] || payment.paymentMethod || 'N/A'}
            </div>
            {payment.chequeNumber && (
              <div className="text-xs text-gray-500">Chq: {payment.chequeNumber}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'amount',
      header: 'Amount',
      cell: ({ row }) => {
        const payment = row.original;
        return (
          <div>
            <div className="text-sm font-medium text-gray-900">
              {payment.currency || 'INR'} {payment.amount?.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </div>
            {payment.grossAmount && payment.grossAmount !== payment.amount && (
              <div className="text-xs text-gray-500">
                Gross: {payment.grossAmount.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
              </div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'tdsAmount',
      header: 'TDS',
      cell: ({ row }) => {
        const payment = row.original;
        if (!payment.tdsApplicable || !payment.tdsAmount) {
          return <span className="text-gray-500">—</span>;
        }
        return (
          <div>
            <div className="text-sm text-purple-600">
              {payment.tdsAmount.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </div>
            <div className="text-xs text-gray-500">
              {payment.tdsSection} @ {payment.tdsRate}%
            </div>
            {payment.tdsDeposited && (
              <div className="text-xs text-green-600">Deposited</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.getValue('status') as string;
        const displayStatus = status?.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase()) || 'Draft';
        return (
          <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${STATUS_COLORS[status] || STATUS_COLORS.draft}`}>
            {displayStatus}
          </div>
        );
      },
    },
    {
      accessorKey: 'isReconciled',
      header: 'Reconciled',
      cell: ({ row }) => {
        const payment = row.original;
        if (payment.status !== 'processed') return <span className="text-gray-500">—</span>;
        return payment.isReconciled ? (
          <div className="inline-flex items-center text-green-600">
            <CheckCircle size={14} className="mr-1" />
            <span className="text-xs">Yes</span>
          </div>
        ) : (
          <div className="inline-flex items-center text-yellow-600">
            <Clock size={14} className="mr-1" />
            <span className="text-xs">Pending</span>
          </div>
        );
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const payment = row.original;
        const canApprove = payment.status === 'pending_approval';
        const canProcess = payment.status === 'approved';
        const canEdit = payment.status === 'draft';
        const canCancel = !['cancelled', 'processed'].includes(payment.status || '');

        return (
          <div className="flex space-x-1">
            {canApprove && (
              <button
                onClick={() => handleApprove(payment.id)}
                className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                title="Approve"
              >
                <CheckCircle size={16} />
              </button>
            )}
            {canProcess && (
              <button
                onClick={() => handleProcess(payment.id)}
                className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                title="Process Payment"
              >
                <Banknote size={16} />
              </button>
            )}
            {canEdit && (
              <button
                onClick={() => setEditingPayment(payment)}
                className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                title="Edit"
              >
                <Edit size={16} />
              </button>
            )}
            {canCancel && (
              <button
                onClick={() => handleCancel(payment.id)}
                className="text-yellow-600 hover:text-yellow-800 p-1 rounded hover:bg-yellow-50 transition-colors"
                title="Cancel"
              >
                <XCircle size={16} />
              </button>
            )}
            {canEdit && (
              <button
                onClick={() => setDeletingPayment(payment)}
                className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                title="Delete"
              >
                <Trash2 size={16} />
              </button>
            )}
          </div>
        );
      },
    },
  ];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load vendor payments</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <Link to="/finance/ap/vendors" className="text-gray-500 hover:text-gray-700">
              Vendors
            </Link>
            <span className="text-gray-400">/</span>
            <h1 className="text-3xl font-bold text-gray-900">Payments</h1>
          </div>
          <p className="text-gray-600 mt-2">Manage outgoing payments to vendors</p>
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
                value={companyFilter ?? ''}
                onChange={(value) => setCompanyFilter(value || null)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Vendor</label>
              <select
                value={vendorFilter}
                onChange={(e) => setVendorFilter(e.target.value)}
                className="w-48 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">All Vendors</option>
                {vendors.map((v) => (
                  <option key={v.id} value={v.id}>{v.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Status</label>
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                className="w-40 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">All Status</option>
                <option value="draft">Draft</option>
                <option value="pending_approval">Pending Approval</option>
                <option value="approved">Approved</option>
                <option value="processed">Processed</option>
                <option value="cancelled">Cancelled</option>
              </select>
            </div>
          </div>
          <DataTable
            columns={columns}
            data={payments}
            searchPlaceholder="Search payments..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Payment"
          />
        </div>
      </div>

      {/* Create Payment Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Vendor Payment"
        size="xl"
      >
        <div className="p-4 text-center text-gray-500">
          <CreditCard className="h-12 w-12 mx-auto mb-4 text-gray-400" />
          <p>Payment form coming soon...</p>
          <p className="text-sm mt-2">Use the API directly for now.</p>
          <button
            onClick={() => setIsCreateDrawerOpen(false)}
            className="mt-4 px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            Close
          </button>
        </div>
      </Drawer>

      {/* Edit Payment Drawer */}
      <Drawer
        isOpen={!!editingPayment}
        onClose={() => setEditingPayment(null)}
        title="Edit Vendor Payment"
        size="xl"
      >
        <div className="p-4 text-center text-gray-500">
          <CreditCard className="h-12 w-12 mx-auto mb-4 text-gray-400" />
          <p>Payment form coming soon...</p>
          <button
            onClick={() => setEditingPayment(null)}
            className="mt-4 px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            Close
          </button>
        </div>
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingPayment}
        onClose={() => setDeletingPayment(null)}
        title="Delete Vendor Payment"
        size="sm"
      >
        {deletingPayment && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete this payment of{' '}
              <strong>
                {deletingPayment.currency || 'INR'} {deletingPayment.amount?.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
              </strong>?
              This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingPayment(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deletePayment.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deletePayment.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default VendorPaymentsManagement;

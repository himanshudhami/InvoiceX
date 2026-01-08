import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { ColumnDef } from '@tanstack/react-table';
import {
  useVendorInvoices,
  useDeleteVendorInvoice,
  useApproveVendorInvoice,
  usePostVendorInvoice,
  useCancelVendorInvoice,
} from '@/features/vendors/hooks';
import { useVendors } from '@/features/vendors/hooks';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { VendorInvoice } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import {
  Edit,
  Trash2,
  CheckCircle,
  XCircle,
  Send,
  FileText,
  AlertTriangle,
  Building2,
  Clock,
} from 'lucide-react';
import { format } from 'date-fns';
import { useQueryState, parseAsString } from 'nuqs';

const STATUS_COLORS: Record<string, string> = {
  draft: 'bg-gray-100 text-gray-800',
  pending_approval: 'bg-yellow-100 text-yellow-800',
  approved: 'bg-blue-100 text-blue-800',
  posted: 'bg-green-100 text-green-800',
  paid: 'bg-emerald-100 text-emerald-800',
  cancelled: 'bg-red-100 text-red-800',
};

const VendorInvoicesManagement = () => {
  // Get selected company from context (for multi-company users)
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext();

  // URL-backed filter state with nuqs
  const [companyFilter, setCompanyFilter] = useQueryState('company', parseAsString.withDefault(''));
  const [vendorFilter, setVendorFilter] = useQueryState('vendorId', parseAsString.withDefault(''));
  const [statusFilter, setStatusFilter] = useQueryState('status', parseAsString.withDefault(''));

  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingInvoice, setEditingInvoice] = useState<VendorInvoice | null>(null);
  const [deletingInvoice, setDeletingInvoice] = useState<VendorInvoice | null>(null);

  // Determine effective company ID: URL filter takes precedence, then context selection
  const effectiveCompanyId = companyFilter || (hasMultiCompanyAccess ? selectedCompanyId : undefined);

  const { data: allInvoices = [], isLoading, error, refetch } = useVendorInvoices(effectiveCompanyId || undefined);
  const { data: vendors = [] } = useVendors(effectiveCompanyId || undefined);
  const deleteInvoice = useDeleteVendorInvoice();
  const approveInvoice = useApproveVendorInvoice();
  const postInvoice = usePostVendorInvoice();
  const cancelInvoice = useCancelVendorInvoice();

  // Get vendor lookup
  const vendorLookup = useMemo(() => {
    const lookup: Record<string, string> = {};
    vendors.forEach(v => { lookup[v.id] = v.name; });
    return lookup;
  }, [vendors]);

  // Filter invoices
  const invoices = useMemo(() => {
    let filtered = allInvoices;
    if (companyFilter) {
      filtered = filtered.filter(i => i.companyId === companyFilter);
    }
    if (vendorFilter) {
      filtered = filtered.filter(i => i.partyId === vendorFilter);
    }
    if (statusFilter) {
      filtered = filtered.filter(i => i.status === statusFilter);
    }
    return filtered;
  }, [allInvoices, companyFilter, vendorFilter, statusFilter]);

  // Calculate summary stats
  const stats = useMemo(() => {
    const totalAmount = invoices.reduce((sum, i) => sum + (i.totalAmount || 0), 0);
    const pendingAmount = invoices
      .filter(i => ['draft', 'pending_approval', 'approved'].includes(i.status || ''))
      .reduce((sum, i) => sum + (i.totalAmount || 0), 0);
    const overdueCount = invoices.filter(i => {
      if (!i.dueDate || i.status === 'paid' || i.status === 'cancelled') return false;
      return new Date(i.dueDate) < new Date();
    }).length;

    return { totalAmount, pendingAmount, overdueCount, count: invoices.length };
  }, [invoices]);

  const handleApprove = async (id: string) => {
    try {
      await approveInvoice.mutateAsync(id);
      refetch();
    } catch (error) {
      console.error('Failed to approve invoice:', error);
    }
  };

  const handlePost = async (id: string) => {
    try {
      await postInvoice.mutateAsync(id);
      refetch();
    } catch (error) {
      console.error('Failed to post invoice:', error);
    }
  };

  const handleCancel = async (id: string) => {
    try {
      await cancelInvoice.mutateAsync(id);
      refetch();
    } catch (error) {
      console.error('Failed to cancel invoice:', error);
    }
  };

  const handleDeleteConfirm = async () => {
    if (deletingInvoice) {
      try {
        await deleteInvoice.mutateAsync(deletingInvoice.id);
        setDeletingInvoice(null);
      } catch (error) {
        console.error('Failed to delete invoice:', error);
      }
    }
  };

  const columns: ColumnDef<VendorInvoice>[] = [
    {
      accessorKey: 'invoiceNumber',
      header: 'Invoice #',
      cell: ({ row }) => {
        const invoice = row.original;
        return (
          <div>
            <div className="font-medium text-gray-900">{invoice.invoiceNumber}</div>
            <div className="text-xs text-gray-500">
              {invoice.invoiceType === 'credit_note' ? 'Credit Note' :
               invoice.invoiceType === 'debit_note' ? 'Debit Note' : 'Invoice'}
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'partyId',
      header: 'Vendor',
      cell: ({ row }) => {
        const invoice = row.original;
        return (
          <div>
            <div className="text-sm text-gray-900">
              {vendorLookup[invoice.partyId] || invoice.vendor?.name || 'Unknown'}
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'invoiceDate',
      header: 'Date',
      cell: ({ row }) => {
        const invoice = row.original;
        return (
          <div>
            <div className="text-sm text-gray-900">
              {format(new Date(invoice.invoiceDate), 'dd MMM yyyy')}
            </div>
            <div className="text-xs text-gray-500">
              Due: {format(new Date(invoice.dueDate), 'dd MMM')}
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'totalAmount',
      header: 'Amount',
      cell: ({ row }) => {
        const invoice = row.original;
        const isOverdue = invoice.dueDate &&
          new Date(invoice.dueDate) < new Date() &&
          invoice.status !== 'paid' &&
          invoice.status !== 'cancelled';
        return (
          <div>
            <div className={`text-sm font-medium ${isOverdue ? 'text-red-600' : 'text-gray-900'}`}>
              {invoice.currency || 'INR'} {invoice.totalAmount?.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </div>
            {invoice.tdsAmount && invoice.tdsAmount > 0 && (
              <div className="text-xs text-purple-600">
                TDS: {invoice.tdsAmount.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
              </div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'supplyType',
      header: 'GST Type',
      cell: ({ row }) => {
        const invoice = row.original;
        if (!invoice.supplyType) return <span className="text-gray-500">—</span>;

        const typeLabels: Record<string, string> = {
          intra_state: 'Intra-State',
          inter_state: 'Inter-State',
          import: 'Import',
        };

        return (
          <div>
            <div className="text-sm text-gray-900">{typeLabels[invoice.supplyType] || invoice.supplyType}</div>
            {invoice.reverseCharge && (
              <div className="text-xs text-orange-600">RCM</div>
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
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const invoice = row.original;
        const canApprove = invoice.status === 'pending_approval';
        const canPost = invoice.status === 'approved';
        const canEdit = invoice.status === 'draft';
        const canCancel = !['cancelled', 'posted', 'paid'].includes(invoice.status || '');

        return (
          <div className="flex space-x-1">
            {canApprove && (
              <button
                onClick={() => handleApprove(invoice.id)}
                className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                title="Approve"
              >
                <CheckCircle size={16} />
              </button>
            )}
            {canPost && (
              <button
                onClick={() => handlePost(invoice.id)}
                className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                title="Post to Ledger"
              >
                <Send size={16} />
              </button>
            )}
            {canEdit && (
              <button
                onClick={() => setEditingInvoice(invoice)}
                className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                title="Edit"
              >
                <Edit size={16} />
              </button>
            )}
            {canCancel && (
              <button
                onClick={() => handleCancel(invoice.id)}
                className="text-yellow-600 hover:text-yellow-800 p-1 rounded hover:bg-yellow-50 transition-colors"
                title="Cancel"
              >
                <XCircle size={16} />
              </button>
            )}
            {canEdit && (
              <button
                onClick={() => setDeletingInvoice(invoice)}
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
        <div className="text-red-600 mb-4">Failed to load vendor invoices</div>
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
            <h1 className="text-3xl font-bold text-gray-900">Bills & Invoices</h1>
          </div>
          <p className="text-gray-600 mt-2">Manage purchase invoices from vendors</p>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <FileText className="h-8 w-8 text-blue-600" />
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Total Bills</p>
              <p className="text-2xl font-semibold text-gray-900">{stats.count}</p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <Building2 className="h-8 w-8 text-green-600" />
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
            <Clock className="h-8 w-8 text-yellow-600" />
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Pending</p>
              <p className="text-2xl font-semibold text-gray-900">
                {stats.pendingAmount.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <AlertTriangle className="h-8 w-8 text-red-600" />
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Overdue</p>
              <p className="text-2xl font-semibold text-gray-900">{stats.overdueCount}</p>
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
                <option value="posted">Posted</option>
                <option value="paid">Paid</option>
                <option value="cancelled">Cancelled</option>
              </select>
            </div>
          </div>
          <DataTable
            columns={columns}
            data={invoices}
            searchPlaceholder="Search invoices..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Bill"
          />
        </div>
      </div>

      {/* Create Invoice Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Vendor Invoice"
        size="xl"
      >
        <div className="p-4 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
          <p>Invoice form coming soon...</p>
          <p className="text-sm mt-2">Use the API directly for now.</p>
          <button
            onClick={() => setIsCreateDrawerOpen(false)}
            className="mt-4 px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            Close
          </button>
        </div>
      </Drawer>

      {/* Edit Invoice Drawer */}
      <Drawer
        isOpen={!!editingInvoice}
        onClose={() => setEditingInvoice(null)}
        title="Edit Vendor Invoice"
        size="xl"
      >
        <div className="p-4 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
          <p>Invoice form coming soon...</p>
          <button
            onClick={() => setEditingInvoice(null)}
            className="mt-4 px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            Close
          </button>
        </div>
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingInvoice}
        onClose={() => setDeletingInvoice(null)}
        title="Delete Vendor Invoice"
        size="sm"
      >
        {deletingInvoice && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete invoice <strong>{deletingInvoice.invoiceNumber}</strong>?
              This action cannot be undone.
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
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteInvoice.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default VendorInvoicesManagement;

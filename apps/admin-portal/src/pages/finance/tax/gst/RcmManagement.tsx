import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useRcmTransactionsPaged,
  useDeleteRcmTransaction,
  useRecordRcmPayment,
  useClaimRcmItc,
  useRcmSummary,
} from '@/features/gst-compliance/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import type { RcmTransaction, RcmPaymentRequest, RcmItcClaimRequest } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import {
  Edit,
  Trash2,
  RotateCcw,
  CreditCard,
  FileCheck,
  Clock,
  CheckCircle,
  AlertCircle,
  Building2,
  Receipt,
  Banknote,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// RCM Categories as per Notification 13/2017
const RCM_CATEGORIES = [
  { value: 'legal', label: 'Legal Services' },
  { value: 'security', label: 'Security Services' },
  { value: 'gta', label: 'Goods Transport Agency (GTA)' },
  { value: 'import_service', label: 'Import of Services' },
  { value: 'unregistered_supplier', label: 'From Unregistered Supplier' },
  { value: 'other', label: 'Other RCM Categories' },
];

// Generate return period options (last 12 months)
const generateReturnPeriods = () => {
  const periods = [];
  const now = new Date();
  for (let i = 0; i < 12; i++) {
    const date = new Date(now.getFullYear(), now.getMonth() - i, 1);
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    periods.push({
      value: `${month}${year}`,
      label: `${date.toLocaleString('default', { month: 'short' })} ${year}`,
    });
  }
  return periods;
};

const RcmManagement = () => {
  const returnPeriods = generateReturnPeriods();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedReturnPeriod, setSelectedReturnPeriod] = useState(returnPeriods[0].value);
  const [selectedStatus, setSelectedStatus] = useState<string>('');
  const [selectedCategory, setSelectedCategory] = useState<string>('');

  const [deletingEntry, setDeletingEntry] = useState<RcmTransaction | null>(null);
  const [paymentEntry, setPaymentEntry] = useState<RcmTransaction | null>(null);
  const [itcClaimEntry, setItcClaimEntry] = useState<RcmTransaction | null>(null);
  const [paymentDetails, setPaymentDetails] = useState({
    paymentDate: new Date().toISOString().split('T')[0],
    paymentReference: '',
    bankAccountId: '',
  });
  const [itcClaimDetails, setItcClaimDetails] = useState({
    claimDate: new Date().toISOString().split('T')[0],
    returnPeriod: returnPeriods[0].value,
  });

  const { data: companies = [] } = useCompanies();

  const { data: rcmData, isLoading, error, refetch } = useRcmTransactionsPaged({
    companyId: selectedCompanyId || undefined,
    status: selectedStatus || undefined,
    rcmCategory: selectedCategory || undefined,
    returnPeriod: selectedReturnPeriod || undefined,
    page: 1,
    pageSize: 50,
  });

  const { data: summary } = useRcmSummary(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId
  );

  const deleteRcm = useDeleteRcmTransaction();
  const recordPayment = useRecordRcmPayment();
  const claimItc = useClaimRcmItc();

  const rcmTransactions = rcmData?.data || [];

  const handleDelete = (entry: RcmTransaction) => {
    setDeletingEntry(entry);
  };

  const handleDeleteConfirm = async () => {
    if (deletingEntry) {
      try {
        await deleteRcm.mutateAsync(deletingEntry.id);
        setDeletingEntry(null);
      } catch (error) {
        console.error('Failed to delete RCM transaction:', error);
      }
    }
  };

  const handleRecordPayment = (entry: RcmTransaction) => {
    setPaymentEntry(entry);
    setPaymentDetails({
      paymentDate: new Date().toISOString().split('T')[0],
      paymentReference: '',
      bankAccountId: '',
    });
  };

  const handlePaymentConfirm = async () => {
    if (paymentEntry) {
      const request: RcmPaymentRequest = {
        rcmTransactionId: paymentEntry.id,
        paymentDate: paymentDetails.paymentDate,
        paymentReference: paymentDetails.paymentReference,
        bankAccountId: paymentDetails.bankAccountId || undefined,
      };
      try {
        await recordPayment.mutateAsync(request);
        setPaymentEntry(null);
      } catch (error) {
        console.error('Failed to record RCM payment:', error);
      }
    }
  };

  const handleClaimItc = (entry: RcmTransaction) => {
    setItcClaimEntry(entry);
    setItcClaimDetails({
      claimDate: new Date().toISOString().split('T')[0],
      returnPeriod: selectedReturnPeriod,
    });
  };

  const handleItcClaimConfirm = async () => {
    if (itcClaimEntry) {
      const request: RcmItcClaimRequest = {
        rcmTransactionId: itcClaimEntry.id,
        claimDate: itcClaimDetails.claimDate,
        returnPeriod: itcClaimDetails.returnPeriod,
      };
      try {
        await claimItc.mutateAsync(request);
        setItcClaimEntry(null);
      } catch (error) {
        console.error('Failed to claim ITC:', error);
      }
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2,
    }).format(amount);
  };

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { color: string; icon: typeof Clock; label: string }> = {
      pending: { color: 'bg-yellow-100 text-yellow-800', icon: Clock, label: 'Pending' },
      rcm_paid: { color: 'bg-blue-100 text-blue-800', icon: CreditCard, label: 'RCM Paid' },
      itc_claimed: { color: 'bg-green-100 text-green-800', icon: CheckCircle, label: 'ITC Claimed' },
    };
    const config = statusConfig[status] || statusConfig.pending;
    const Icon = config.icon;
    return (
      <span className={cn('inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium', config.color)}>
        <Icon className="h-3 w-3" />
        {config.label}
      </span>
    );
  };

  const getCategoryLabel = (category: string) => {
    const cat = RCM_CATEGORIES.find(c => c.value === category);
    return cat?.label || category;
  };

  const columns: ColumnDef<RcmTransaction>[] = [
    {
      accessorKey: 'supplierName',
      header: 'Supplier',
      cell: ({ row }) => {
        const entry = row.original;
        return (
          <div className="flex items-start gap-3">
            <div className="p-2 bg-purple-100 rounded-lg">
              <Building2 className="h-5 w-5 text-purple-600" />
            </div>
            <div>
              <div className="font-medium text-gray-900">{entry.supplierName}</div>
              {entry.supplierGstin && (
                <div className="text-xs text-gray-500 font-mono">GSTIN: {entry.supplierGstin}</div>
              )}
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'rcmCategory',
      header: 'Category',
      cell: ({ row }) => (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
          {getCategoryLabel(row.original.rcmCategory)}
        </span>
      ),
    },
    {
      accessorKey: 'invoiceDate',
      header: 'Invoice Date',
      cell: ({ row }) => (
        <div className="text-sm text-gray-900">
          {new Date(row.original.invoiceDate).toLocaleDateString('en-IN', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
          })}
        </div>
      ),
    },
    {
      accessorKey: 'taxableValue',
      header: 'Taxable Value',
      cell: ({ row }) => (
        <div className="text-right font-medium text-gray-900">
          {formatCurrency(row.original.taxableValue)}
        </div>
      ),
    },
    {
      accessorKey: 'totalGst',
      header: 'GST Amount',
      cell: ({ row }) => {
        const entry = row.original;
        const totalGst = (entry.cgstAmount || 0) + (entry.sgstAmount || 0) + (entry.igstAmount || 0);
        return (
          <div className="text-right">
            <div className="font-medium text-green-600">{formatCurrency(totalGst)}</div>
            <div className="text-xs text-gray-500">
              {entry.igstAmount > 0
                ? `IGST: ${entry.gstRate}%`
                : `CGST+SGST: ${entry.gstRate}%`}
            </div>
          </div>
        );
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
        const entry = row.original;
        return (
          <div className="flex space-x-2">
            {entry.status === 'pending' && (
              <button
                onClick={() => handleRecordPayment(entry)}
                className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                title="Record RCM Payment"
              >
                <CreditCard size={16} />
              </button>
            )}
            {entry.status === 'rcm_paid' && (
              <button
                onClick={() => handleClaimItc(entry)}
                className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                title="Claim ITC"
              >
                <FileCheck size={16} />
              </button>
            )}
            {entry.status === 'pending' && (
              <button
                onClick={() => handleDelete(entry)}
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

  // Summary calculations
  const totalTaxable = rcmTransactions.reduce((sum, t) => sum + t.taxableValue, 0);
  const totalGst = rcmTransactions.reduce((sum, t) => sum + (t.cgstAmount || 0) + (t.sgstAmount || 0) + (t.igstAmount || 0), 0);
  const pendingCount = rcmTransactions.filter(t => t.status === 'pending').length;
  const paidCount = rcmTransactions.filter(t => t.status === 'rcm_paid').length;

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
        <div className="text-red-600 mb-4">Failed to load RCM transactions</div>
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
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">RCM Management</h1>
          <p className="text-gray-600 mt-2">
            Manage Reverse Charge Mechanism transactions - Payment and ITC Claim workflow
          </p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
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
            <label htmlFor="periodFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Return Period
            </label>
            <select
              id="periodFilter"
              value={selectedReturnPeriod}
              onChange={(e) => setSelectedReturnPeriod(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {returnPeriods.map((period) => (
                <option key={period.value} value={period.value}>
                  {period.label}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="statusFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Status
            </label>
            <select
              id="statusFilter"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">All Status</option>
              <option value="pending">Pending</option>
              <option value="rcm_paid">RCM Paid</option>
              <option value="itc_claimed">ITC Claimed</option>
            </select>
          </div>
          <div>
            <label htmlFor="categoryFilter" className="block text-sm font-medium text-gray-700 mb-1">
              RCM Category
            </label>
            <select
              id="categoryFilter"
              value={selectedCategory}
              onChange={(e) => setSelectedCategory(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">All Categories</option>
              {RCM_CATEGORIES.map((cat) => (
                <option key={cat.value} value={cat.value}>
                  {cat.label}
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
              <p className="text-sm font-medium text-gray-500">Total Taxable Value</p>
              <p className="text-2xl font-bold text-gray-900">{formatCurrency(totalTaxable)}</p>
            </div>
            <div className="p-3 bg-purple-100 rounded-full">
              <Receipt className="h-6 w-6 text-purple-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">{rcmTransactions.length} transactions</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Total GST (RCM)</p>
              <p className="text-2xl font-bold text-green-600">{formatCurrency(totalGst)}</p>
            </div>
            <div className="p-3 bg-green-100 rounded-full">
              <RotateCcw className="h-6 w-6 text-green-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">Reverse charge liability</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Pending Payment</p>
              <p className="text-2xl font-bold text-yellow-600">{pendingCount}</p>
            </div>
            <div className="p-3 bg-yellow-100 rounded-full">
              <Clock className="h-6 w-6 text-yellow-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">Awaiting RCM payment</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Pending ITC Claim</p>
              <p className="text-2xl font-bold text-blue-600">{paidCount}</p>
            </div>
            <div className="p-3 bg-blue-100 rounded-full">
              <FileCheck className="h-6 w-6 text-blue-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">RCM paid, ITC claimable</p>
        </div>
      </div>

      {/* Workflow Info */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h3 className="font-medium text-blue-800 mb-2">RCM Two-Stage Workflow</h3>
        <div className="flex items-center gap-4 text-sm text-blue-700">
          <div className="flex items-center gap-2">
            <span className="flex items-center justify-center w-6 h-6 rounded-full bg-yellow-200 text-yellow-800 text-xs font-bold">1</span>
            <span>Create Transaction (Pending)</span>
          </div>
          <span className="text-blue-400">→</span>
          <div className="flex items-center gap-2">
            <span className="flex items-center justify-center w-6 h-6 rounded-full bg-blue-200 text-blue-800 text-xs font-bold">2</span>
            <span>Record RCM Payment</span>
          </div>
          <span className="text-blue-400">→</span>
          <div className="flex items-center gap-2">
            <span className="flex items-center justify-center w-6 h-6 rounded-full bg-green-200 text-green-800 text-xs font-bold">3</span>
            <span>Claim ITC in GSTR-3B</span>
          </div>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={rcmTransactions}
            searchPlaceholder="Search by supplier name..."
          />
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingEntry}
        onClose={() => setDeletingEntry(null)}
        title="Delete RCM Transaction"
        size="sm"
      >
        {deletingEntry && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the RCM transaction for <strong>{deletingEntry.supplierName}</strong> of{' '}
              <strong>{formatCurrency(deletingEntry.taxableValue)}</strong>? This action cannot be undone.
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
                disabled={deleteRcm.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteRcm.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Record Payment Modal */}
      <Modal
        isOpen={!!paymentEntry}
        onClose={() => setPaymentEntry(null)}
        title="Record RCM Payment"
        size="md"
      >
        {paymentEntry && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <p className="text-sm text-gray-600">RCM Transaction Details:</p>
              <p className="font-medium">{paymentEntry.supplierName}</p>
              <p className="text-sm text-gray-500">
                GST Amount: {formatCurrency(
                  (paymentEntry.cgstAmount || 0) + (paymentEntry.sgstAmount || 0) + (paymentEntry.igstAmount || 0)
                )}
              </p>
            </div>
            <div>
              <label htmlFor="paymentDate" className="block text-sm font-medium text-gray-700 mb-1">
                Payment Date *
              </label>
              <input
                id="paymentDate"
                type="date"
                value={paymentDetails.paymentDate}
                onChange={(e) => setPaymentDetails({ ...paymentDetails, paymentDate: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label htmlFor="paymentReference" className="block text-sm font-medium text-gray-700 mb-1">
                Payment Reference / Challan No.
              </label>
              <input
                id="paymentReference"
                type="text"
                value={paymentDetails.paymentReference}
                onChange={(e) => setPaymentDetails({ ...paymentDetails, paymentReference: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="Enter challan or payment reference"
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setPaymentEntry(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handlePaymentConfirm}
                disabled={recordPayment.isPending || !paymentDetails.paymentDate}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
              >
                {recordPayment.isPending ? 'Recording...' : 'Record Payment'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Claim ITC Modal */}
      <Modal
        isOpen={!!itcClaimEntry}
        onClose={() => setItcClaimEntry(null)}
        title="Claim ITC on RCM"
        size="md"
      >
        {itcClaimEntry && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <p className="text-sm text-gray-600">RCM Transaction Details:</p>
              <p className="font-medium">{itcClaimEntry.supplierName}</p>
              <p className="text-sm text-gray-500">
                ITC Claimable: {formatCurrency(
                  (itcClaimEntry.cgstAmount || 0) + (itcClaimEntry.sgstAmount || 0) + (itcClaimEntry.igstAmount || 0)
                )}
              </p>
              <p className="text-xs text-green-600 mt-1">RCM payment recorded - eligible for ITC</p>
            </div>
            <div>
              <label htmlFor="claimDate" className="block text-sm font-medium text-gray-700 mb-1">
                Claim Date *
              </label>
              <input
                id="claimDate"
                type="date"
                value={itcClaimDetails.claimDate}
                onChange={(e) => setItcClaimDetails({ ...itcClaimDetails, claimDate: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label htmlFor="claimReturnPeriod" className="block text-sm font-medium text-gray-700 mb-1">
                Return Period (GSTR-3B) *
              </label>
              <select
                id="claimReturnPeriod"
                value={itcClaimDetails.returnPeriod}
                onChange={(e) => setItcClaimDetails({ ...itcClaimDetails, returnPeriod: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                {returnPeriods.map((period) => (
                  <option key={period.value} value={period.value}>
                    {period.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setItcClaimEntry(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleItcClaimConfirm}
                disabled={claimItc.isPending || !itcClaimDetails.claimDate || !itcClaimDetails.returnPeriod}
                className="px-4 py-2 text-sm font-medium text-white bg-green-600 border border-transparent rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {claimItc.isPending ? 'Processing...' : 'Claim ITC'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default RcmManagement;

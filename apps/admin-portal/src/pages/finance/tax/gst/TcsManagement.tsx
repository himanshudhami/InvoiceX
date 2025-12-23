import { useState } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useTcsTransactionsPaged,
  useDeleteTcsTransaction,
  useRecordTcsRemittance,
  useTcsSummary,
  useTcsLiabilityReport,
  usePendingTcsRemittance,
} from '@/features/gst-compliance/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import type { TcsTransaction, TcsRemittanceRequest } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import {
  Trash2,
  Coins,
  Building2,
  CreditCard,
  CheckCircle,
  Clock,
  Receipt,
  TrendingUp,
  AlertTriangle,
  Banknote,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// TCS Sections
const TCS_SECTIONS = [
  { value: '206C(1H)', label: '206C(1H) - Sale of Goods > 50L', rate: 0.1 },
  { value: '206C(1)', label: '206C(1) - Scrap', rate: 1 },
  { value: '206C(1F)', label: '206C(1F) - Motor Vehicle > 10L', rate: 1 },
  { value: '206C(1G)', label: '206C(1G) - Foreign Remittance', rate: 5 },
];

// Generate financial year options
const generateFinancialYears = () => {
  const currentYear = new Date().getFullYear();
  const currentMonth = new Date().getMonth() + 1;
  const startYear = currentMonth > 3 ? currentYear : currentYear - 1;

  const years = [];
  for (let i = 0; i < 5; i++) {
    const year = startYear - i;
    years.push({
      value: `${year}-${(year + 1).toString().slice(-2)}`,
      label: `FY ${year}-${(year + 1).toString().slice(-2)}`,
    });
  }
  return years;
};

const QUARTERS = [
  { value: 'Q1', label: 'Q1 (Apr-Jun)' },
  { value: 'Q2', label: 'Q2 (Jul-Sep)' },
  { value: 'Q3', label: 'Q3 (Oct-Dec)' },
  { value: 'Q4', label: 'Q4 (Jan-Mar)' },
];

const TcsManagement = () => {
  const financialYears = generateFinancialYears();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedFY, setSelectedFY] = useState(financialYears[0].value);
  const [selectedQuarter, setSelectedQuarter] = useState<string>('');
  const [selectedStatus, setSelectedStatus] = useState<string>('');

  const [deletingEntry, setDeletingEntry] = useState<TcsTransaction | null>(null);
  const [remittanceModal, setRemittanceModal] = useState(false);
  const [selectedForRemittance, setSelectedForRemittance] = useState<string[]>([]);
  const [remittanceDetails, setRemittanceDetails] = useState({
    remittanceDate: new Date().toISOString().split('T')[0],
    challanNumber: '',
    bsrCode: '',
    bankAccountId: '',
  });

  const { data: companies = [] } = useCompanies();

  const { data: tcsData, isLoading, error, refetch } = useTcsTransactionsPaged({
    companyId: selectedCompanyId || undefined,
    status: selectedStatus || undefined,
    financialYear: selectedFY || undefined,
    quarter: selectedQuarter || undefined,
    page: 1,
    pageSize: 50,
  });

  const { data: summary } = useTcsSummary(
    selectedCompanyId,
    selectedFY,
    selectedQuarter || undefined,
    !!selectedCompanyId
  );

  const { data: liabilityReport } = useTcsLiabilityReport(
    selectedCompanyId,
    selectedFY,
    !!selectedCompanyId
  );

  const { data: pendingRemittance = [] } = usePendingTcsRemittance(
    selectedCompanyId,
    !!selectedCompanyId
  );

  const deleteTcs = useDeleteTcsTransaction();
  const recordRemittance = useRecordTcsRemittance();

  const tcsTransactions = tcsData?.data || [];

  const handleDelete = (entry: TcsTransaction) => {
    setDeletingEntry(entry);
  };

  const handleDeleteConfirm = async () => {
    if (deletingEntry) {
      try {
        await deleteTcs.mutateAsync(deletingEntry.id);
        setDeletingEntry(null);
      } catch (error) {
        console.error('Failed to delete TCS transaction:', error);
      }
    }
  };

  const handleOpenRemittance = () => {
    setSelectedForRemittance(pendingRemittance.map(t => t.id));
    setRemittanceDetails({
      remittanceDate: new Date().toISOString().split('T')[0],
      challanNumber: '',
      bsrCode: '',
      bankAccountId: '',
    });
    setRemittanceModal(true);
  };

  const handleRemittanceConfirm = async () => {
    if (selectedForRemittance.length > 0 && remittanceDetails.challanNumber) {
      const request: TcsRemittanceRequest = {
        transactionIds: selectedForRemittance,
        remittanceDate: remittanceDetails.remittanceDate,
        challanNumber: remittanceDetails.challanNumber,
        bsrCode: remittanceDetails.bsrCode || undefined,
        bankAccountId: remittanceDetails.bankAccountId || undefined,
      };
      try {
        await recordRemittance.mutateAsync(request);
        setRemittanceModal(false);
        setSelectedForRemittance([]);
      } catch (error) {
        console.error('Failed to record TCS remittance:', error);
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

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  };

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { color: string; icon: typeof Clock; label: string }> = {
      collected: { color: 'bg-blue-100 text-blue-800', icon: Coins, label: 'Collected' },
      remitted: { color: 'bg-green-100 text-green-800', icon: CheckCircle, label: 'Remitted' },
      pending: { color: 'bg-yellow-100 text-yellow-800', icon: Clock, label: 'Pending' },
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

  const getTcsSectionLabel = (section: string) => {
    const sec = TCS_SECTIONS.find(s => s.value === section);
    return sec?.label || section;
  };

  const columns: ColumnDef<TcsTransaction>[] = [
    {
      accessorKey: 'customerName',
      header: 'Customer',
      cell: ({ row }) => {
        const entry = row.original;
        return (
          <div className="flex items-start gap-3">
            <div className="p-2 bg-orange-100 rounded-lg">
              <Building2 className="h-5 w-5 text-orange-600" />
            </div>
            <div>
              <div className="font-medium text-gray-900">{entry.customerName}</div>
              {entry.customerPan && (
                <div className="text-xs text-gray-500 font-mono">PAN: {entry.customerPan}</div>
              )}
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'tcsSection',
      header: 'Section',
      cell: ({ row }) => (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-800">
          {row.original.tcsSection}
        </span>
      ),
    },
    {
      accessorKey: 'invoiceDate',
      header: 'Invoice Date',
      cell: ({ row }) => (
        <div className="text-sm text-gray-900">
          {formatDate(row.original.invoiceDate)}
        </div>
      ),
    },
    {
      accessorKey: 'netSaleValue',
      header: 'Sale Value',
      cell: ({ row }) => (
        <div className="text-right font-medium text-gray-900">
          {formatCurrency(row.original.netSaleValue)}
        </div>
      ),
    },
    {
      accessorKey: 'tcsAmount',
      header: 'TCS Amount',
      cell: ({ row }) => {
        const entry = row.original;
        return (
          <div className="text-right">
            <div className="font-medium text-green-600">{formatCurrency(entry.tcsAmount)}</div>
            <div className="text-xs text-gray-500">{entry.tcsRate}%</div>
          </div>
        );
      },
    },
    {
      accessorKey: 'ytdSaleValue',
      header: 'YTD Sales',
      cell: ({ row }) => {
        const ytd = row.original.ytdSaleValue || 0;
        const threshold = 5000000; // 50 Lakhs threshold
        const aboveThreshold = ytd > threshold;
        return (
          <div className="text-right">
            <div className={cn('font-medium', aboveThreshold ? 'text-orange-600' : 'text-gray-900')}>
              {formatCurrency(ytd)}
            </div>
            {aboveThreshold && (
              <div className="text-xs text-orange-500">Above 50L threshold</div>
            )}
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
        const canDelete = entry.status !== 'remitted';
        return (
          <div className="flex space-x-2">
            {canDelete && (
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
  const totalCollected = tcsTransactions.reduce((sum, t) => sum + t.tcsAmount, 0);
  const totalRemitted = tcsTransactions.filter(t => t.status === 'remitted').reduce((sum, t) => sum + t.tcsAmount, 0);
  const totalPending = tcsTransactions.filter(t => t.status !== 'remitted').reduce((sum, t) => sum + t.tcsAmount, 0);

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
        <div className="text-red-600 mb-4">Failed to load TCS transactions</div>
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
          <h1 className="text-3xl font-bold text-gray-900">TCS Management</h1>
          <p className="text-gray-600 mt-2">
            Manage Tax Collected at Source under Section 206C
          </p>
        </div>
        {pendingRemittance.length > 0 && (
          <button
            onClick={handleOpenRemittance}
            className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 flex items-center gap-2"
          >
            <Banknote className="h-4 w-4" />
            Record Remittance ({pendingRemittance.length})
          </button>
        )}
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
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
          <div>
            <label htmlFor="quarterFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Quarter
            </label>
            <select
              id="quarterFilter"
              value={selectedQuarter}
              onChange={(e) => setSelectedQuarter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">All Quarters</option>
              {QUARTERS.map((q) => (
                <option key={q.value} value={q.value}>
                  {q.label}
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
              <option value="collected">Collected</option>
              <option value="remitted">Remitted</option>
              <option value="pending">Pending</option>
            </select>
          </div>
        </div>
      </div>

      {/* Pending Remittance Alert */}
      {pendingRemittance.length > 0 && (
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
          <div className="flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-amber-600 mt-0.5" />
            <div className="flex-1">
              <h3 className="font-medium text-amber-800">Pending TCS Remittance</h3>
              <p className="text-sm text-amber-700 mt-1">
                {pendingRemittance.length} transactions with TCS totaling{' '}
                {formatCurrency(pendingRemittance.reduce((sum, t) => sum + t.tcsAmount, 0))} are pending remittance.
                TCS must be deposited by the 7th of the following month.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Total TCS Collected</p>
              <p className="text-2xl font-bold text-green-600">{formatCurrency(totalCollected)}</p>
            </div>
            <div className="p-3 bg-green-100 rounded-full">
              <Coins className="h-6 w-6 text-green-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">{tcsTransactions.length} transactions</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">TCS Remitted</p>
              <p className="text-2xl font-bold text-blue-600">{formatCurrency(totalRemitted)}</p>
            </div>
            <div className="p-3 bg-blue-100 rounded-full">
              <CheckCircle className="h-6 w-6 text-blue-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">Deposited to government</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Pending Remittance</p>
              <p className="text-2xl font-bold text-yellow-600">{formatCurrency(totalPending)}</p>
            </div>
            <div className="p-3 bg-yellow-100 rounded-full">
              <Clock className="h-6 w-6 text-yellow-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">Awaiting deposit</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Total Sale Value</p>
              <p className="text-2xl font-bold text-gray-900">
                {formatCurrency(tcsTransactions.reduce((sum, t) => sum + t.netSaleValue, 0))}
              </p>
            </div>
            <div className="p-3 bg-gray-100 rounded-full">
              <Receipt className="h-6 w-6 text-gray-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">On which TCS collected</p>
        </div>
      </div>

      {/* Info Box */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h3 className="font-medium text-blue-800 mb-2">TCS Section 206C(1H) - Sale of Goods</h3>
        <p className="text-sm text-blue-700">
          TCS @ 0.1% is applicable on sale of goods exceeding ₹50 lakhs to a buyer in a financial year.
          TCS collected must be deposited to the government by the 7th of the following month.
          The threshold is tracked on a per-buyer basis for the financial year.
        </p>
      </div>

      {/* Liability by Section */}
      {liabilityReport && liabilityReport.bySection && (
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="font-medium text-gray-900 mb-4">TCS Liability by Section</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {Object.entries(liabilityReport.bySection).map(([section, amount]) => (
              <div key={section} className="p-4 bg-gray-50 rounded-lg">
                <p className="text-sm text-gray-600">{section}</p>
                <p className="text-lg font-bold text-orange-600">{formatCurrency(amount as number)}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={tcsTransactions}
            searchPlaceholder="Search by customer name or PAN..."
          />
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingEntry}
        onClose={() => setDeletingEntry(null)}
        title="Delete TCS Transaction"
        size="sm"
      >
        {deletingEntry && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the TCS transaction for <strong>{deletingEntry.customerName}</strong>
              of <strong>{formatCurrency(deletingEntry.tcsAmount)}</strong>? This action cannot be undone.
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
                disabled={deleteTcs.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteTcs.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Record Remittance Modal */}
      <Modal
        isOpen={remittanceModal}
        onClose={() => setRemittanceModal(false)}
        title="Record TCS Remittance"
        size="md"
      >
        <div className="space-y-4">
          <div className="bg-gray-50 p-4 rounded-lg">
            <p className="text-sm text-gray-600">Remittance Summary:</p>
            <p className="font-medium">{selectedForRemittance.length} transactions selected</p>
            <p className="text-lg font-bold text-green-600">
              {formatCurrency(
                pendingRemittance
                  .filter(t => selectedForRemittance.includes(t.id))
                  .reduce((sum, t) => sum + t.tcsAmount, 0)
              )}
            </p>
          </div>
          <div>
            <label htmlFor="remittanceDate" className="block text-sm font-medium text-gray-700 mb-1">
              Remittance Date *
            </label>
            <input
              id="remittanceDate"
              type="date"
              value={remittanceDetails.remittanceDate}
              onChange={(e) => setRemittanceDetails({ ...remittanceDetails, remittanceDate: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div>
            <label htmlFor="challanNumber" className="block text-sm font-medium text-gray-700 mb-1">
              Challan Number *
            </label>
            <input
              id="challanNumber"
              type="text"
              value={remittanceDetails.challanNumber}
              onChange={(e) => setRemittanceDetails({ ...remittanceDetails, challanNumber: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Enter challan number"
            />
          </div>
          <div>
            <label htmlFor="bsrCode" className="block text-sm font-medium text-gray-700 mb-1">
              BSR Code
            </label>
            <input
              id="bsrCode"
              type="text"
              value={remittanceDetails.bsrCode}
              onChange={(e) => setRemittanceDetails({ ...remittanceDetails, bsrCode: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Enter BSR code (optional)"
            />
          </div>
          <div className="flex justify-end space-x-3">
            <button
              onClick={() => setRemittanceModal(false)}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              onClick={handleRemittanceConfirm}
              disabled={recordRemittance.isPending || !remittanceDetails.challanNumber || !remittanceDetails.remittanceDate}
              className="px-4 py-2 text-sm font-medium text-white bg-primary border border-transparent rounded-md hover:bg-primary/90 disabled:opacity-50"
            >
              {recordRemittance.isPending ? 'Recording...' : 'Record Remittance'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default TcsManagement;

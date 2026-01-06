import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useQueryStates, parseAsString } from 'nuqs';
import { useCompanies } from '@/hooks/api/useCompanies';
import {
  usePendingEsiReturns,
  useFiledEsiReturns,
  useEsiReturnSummary,
  useCreateEsiReturnPayment,
  useRecordEsiPayment,
  useUpdateEsiChallanNumber,
  useGenerateEsiReturnFile,
} from '@/features/statutory/hooks';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/button';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { formatINR } from '@/lib/currency';
import {
  FileText,
  Download,
  RefreshCw,
  CreditCard,
  AlertTriangle,
  CheckCircle,
  Clock,
  Plus,
  Edit,
  FileDown,
  Users,
  Shield,
} from 'lucide-react';
import { format, parseISO } from 'date-fns';
import type { EsiReturnMonthlyStatus } from '@/services/api/types';

/**
 * Helper function to get current financial year
 */
const getCurrentFinancialYear = (): string => {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth();
  if (month < 3) {
    return `${year - 1}-${String(year).slice(2)}`;
  }
  return `${year}-${String(year + 1).slice(2)}`;
};

/**
 * Helper to get financial year options
 */
const getFinancialYearOptions = (): string[] => {
  const currentFY = getCurrentFinancialYear();
  const [startYear] = currentFY.split('-').map(Number);
  const options: string[] = [];
  for (let i = 0; i < 3; i++) {
    const fy = `${startYear - i}-${String(startYear - i + 1).slice(-2)}`;
    options.push(fy);
  }
  return options;
};

/**
 * Get ESI contribution period (Apr-Sep or Oct-Mar)
 */
const getContributionPeriod = (month: number): string => {
  if (month >= 4 && month <= 9) return 'Apr-Sep';
  return 'Oct-Mar';
};

/**
 * ESI Return Management Page
 * Handles ESI monthly contributions, return filing, and challan tracking
 */
const EsiReturnManagement = () => {
  const { data: companies = [] } = useCompanies();
  const fyOptions = useMemo(() => getFinancialYearOptions(), []);

  const [urlState, setUrlState] = useQueryStates(
    {
      companyId: parseAsString,
      financialYear: parseAsString.withDefault(getCurrentFinancialYear()),
      view: parseAsString.withDefault('pending'),
    },
    { history: 'push' }
  );

  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [challanModalOpen, setChallanModalOpen] = useState(false);
  const [selectedMonth, setSelectedMonth] = useState<EsiReturnMonthlyStatus | null>(null);
  const [paymentForm, setPaymentForm] = useState({
    paymentDate: format(new Date(), 'yyyy-MM-dd'),
    paymentMode: 'net_banking',
    bankName: '',
    bankAccountId: '',
    bankReference: '',
    actualAmountPaid: 0,
    challanNumber: '',
  });
  const [newReturnForm, setNewReturnForm] = useState({
    periodMonth: new Date().getMonth() || 12,
    periodYear: new Date().getFullYear(),
    proposedPaymentDate: format(new Date(), 'yyyy-MM-dd'),
  });

  const companyId = urlState.companyId || companies[0]?.id || '';

  // Queries
  const { data: pendingReturns = [], isLoading: pendingLoading } = usePendingEsiReturns(companyId, !!companyId);
  const { data: filedReturns = [], isLoading: filedLoading } = useFiledEsiReturns(companyId, urlState.financialYear, !!companyId);
  const { data: summary } = useEsiReturnSummary(companyId, urlState.financialYear, !!companyId);

  // Mutations
  const createPayment = useCreateEsiReturnPayment();
  const recordPayment = useRecordEsiPayment();
  const updateChallan = useUpdateEsiChallanNumber();
  const generateFile = useGenerateEsiReturnFile();

  // Handle create return payment
  const handleCreatePayment = async () => {
    if (!companyId) return;
    try {
      await createPayment.mutateAsync({
        companyId,
        ...newReturnForm,
      });
      setCreateModalOpen(false);
    } catch (error) {
      console.error('Failed to create ESI return payment:', error);
    }
  };

  // Handle record payment
  const handleRecordPayment = async () => {
    if (!selectedMonth?.statutoryPaymentId) return;
    try {
      await recordPayment.mutateAsync({
        id: selectedMonth.statutoryPaymentId,
        request: paymentForm,
      });
      setPaymentModalOpen(false);
      setSelectedMonth(null);
    } catch (error) {
      console.error('Failed to record payment:', error);
    }
  };

  // Handle update challan number
  const handleUpdateChallan = async () => {
    if (!selectedMonth?.statutoryPaymentId || !paymentForm.challanNumber) return;
    try {
      await updateChallan.mutateAsync({
        id: selectedMonth.statutoryPaymentId,
        challanNumber: paymentForm.challanNumber,
      });
      setChallanModalOpen(false);
      setSelectedMonth(null);
    } catch (error) {
      console.error('Failed to update challan number:', error);
    }
  };

  // Handle generate return file
  const handleGenerateFile = async (month: number, year: number) => {
    if (!companyId) return;
    try {
      const result = await generateFile.mutateAsync({ companyId, month, year });
      // Download the file
      if (result.base64Content) {
        const blob = new Blob([atob(result.base64Content)], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = result.fileName;
        a.click();
        URL.revokeObjectURL(url);
      }
    } catch (error) {
      console.error('Failed to generate ESI return file:', error);
    }
  };

  // Status badge
  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { label: string; className: string; icon: React.ReactNode }> = {
      pending: { label: 'Pending', className: 'bg-yellow-100 text-yellow-800', icon: <Clock className="w-3 h-3" /> },
      paid: { label: 'Paid', className: 'bg-green-100 text-green-800', icon: <CheckCircle className="w-3 h-3" /> },
      filed: { label: 'Filed', className: 'bg-blue-100 text-blue-800', icon: <FileText className="w-3 h-3" /> },
      overdue: { label: 'Overdue', className: 'bg-red-100 text-red-800', icon: <AlertTriangle className="w-3 h-3" /> },
      not_due: { label: 'Not Due', className: 'bg-gray-100 text-gray-600', icon: <Clock className="w-3 h-3" /> },
    };
    const config = statusConfig[status] || statusConfig.pending;
    return (
      <span className={`inline-flex items-center gap-1 px-2 py-1 rounded text-xs font-medium ${config.className}`}>
        {config.icon}
        {config.label}
      </span>
    );
  };

  // Monthly status columns
  const columns: ColumnDef<EsiReturnMonthlyStatus>[] = [
    {
      accessorKey: 'monthName',
      header: 'Wage Month',
      cell: ({ row }) => (
        <div>
          <div className="font-medium">{row.original.monthName} {row.original.year}</div>
          <div className="text-xs text-gray-500">Period: {row.original.contributionPeriod}</div>
        </div>
      ),
    },
    {
      accessorKey: 'employeeCount',
      header: 'Employees',
      cell: ({ row }) => (
        <div className="flex items-center gap-1">
          <Users className="w-4 h-4 text-gray-400" />
          <span className="font-medium">{row.original.employeeCount}</span>
        </div>
      ),
    },
    {
      accessorKey: 'esiDeducted',
      header: 'ESI Deducted',
      cell: ({ row }) => (
        <div className="text-right">
          <div className="font-medium">{formatINR(row.original.esiDeducted)}</div>
          <div className="text-xs text-gray-500">Employee + Employer</div>
        </div>
      ),
    },
    {
      accessorKey: 'esiDeposited',
      header: 'ESI Deposited',
      cell: ({ row }) => (
        <div className="text-right font-medium text-green-600">
          {row.original.esiDeposited > 0 ? formatINR(row.original.esiDeposited) : '—'}
        </div>
      ),
    },
    {
      accessorKey: 'variance',
      header: 'Variance',
      cell: ({ row }) => {
        const variance = row.original.variance;
        if (variance === 0) return <span className="text-gray-400">—</span>;
        return (
          <div className={`text-right font-medium ${variance > 0 ? 'text-red-600' : 'text-green-600'}`}>
            {variance > 0 ? '+' : ''}{formatINR(variance)}
          </div>
        );
      },
    },
    {
      accessorKey: 'dueDate',
      header: 'Due Date',
      cell: ({ row }) => {
        const dueDate = parseISO(row.original.dueDate);
        const isOverdue = new Date() > dueDate && row.original.status !== 'paid' && row.original.status !== 'filed';
        return (
          <div className={isOverdue ? 'text-red-600' : ''}>
            <div className="font-medium">{format(dueDate, 'dd MMM yyyy')}</div>
            {isOverdue && <div className="text-xs">Overdue</div>}
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
      accessorKey: 'challanNumber',
      header: 'Challan No.',
      cell: ({ row }) => (
        <div className="text-sm font-mono">
          {row.original.challanNumber || <span className="text-gray-400">—</span>}
        </div>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const item = row.original;
        return (
          <div className="flex items-center gap-2">
            {/* Generate Return File */}
            <button
              onClick={() => handleGenerateFile(item.month, item.year)}
              className="p-1 text-purple-600 hover:text-purple-800 hover:bg-purple-50 rounded"
              title="Download Return File"
              disabled={generateFile.isPending}
            >
              <FileDown className="w-4 h-4" />
            </button>
            {/* Record Payment */}
            {item.status === 'pending' || item.status === 'overdue' ? (
              <button
                onClick={() => {
                  setSelectedMonth(item);
                  setPaymentForm(prev => ({ ...prev, actualAmountPaid: item.esiDeducted }));
                  setPaymentModalOpen(true);
                }}
                className="p-1 text-green-600 hover:text-green-800 hover:bg-green-50 rounded"
                title="Record Payment"
              >
                <CreditCard className="w-4 h-4" />
              </button>
            ) : null}
            {/* Update Challan Number */}
            {item.status === 'paid' && !item.challanNumber && item.statutoryPaymentId && (
              <button
                onClick={() => {
                  setSelectedMonth(item);
                  setChallanModalOpen(true);
                }}
                className="p-1 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded"
                title="Update Challan Number"
              >
                <Edit className="w-4 h-4" />
              </button>
            )}
          </div>
        );
      },
    },
  ];

  const isLoading = urlState.view === 'pending' ? pendingLoading : filedLoading;
  const displayData = urlState.view === 'pending'
    ? (summary?.monthlyStatus?.filter(m => m.status === 'pending' || m.status === 'overdue') || [])
    : (summary?.monthlyStatus || []);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">ESI Return Management</h1>
          <p className="text-gray-500 mt-1">
            Manage ESI contributions, return filing, and payment tracking
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <CompanyFilterDropdown
            value={urlState.companyId || ''}
            onChange={(value) => setUrlState({ companyId: value || null })}
          />
          <select
            value={urlState.financialYear}
            onChange={(e) => setUrlState({ financialYear: e.target.value })}
            className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {fyOptions.map((fy) => (
              <option key={fy} value={fy}>
                FY {fy}
              </option>
            ))}
          </select>
          <Button onClick={() => setCreateModalOpen(true)} className="flex items-center gap-2">
            <Plus className="w-4 h-4" />
            Create Payment
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Total ESI Deducted</p>
            <p className="text-2xl font-bold text-gray-900">{formatINR(summary.totalEsiDeducted)}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Total Deposited</p>
            <p className="text-2xl font-bold text-green-600">{formatINR(summary.totalEsiDeposited)}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Variance</p>
            <p className={`text-2xl font-bold ${summary.totalVariance > 0 ? 'text-red-600' : 'text-green-600'}`}>
              {formatINR(Math.abs(summary.totalVariance))}
            </p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Paid</p>
            <p className="text-2xl font-bold text-green-600">{summary.paidCount}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Pending / Overdue</p>
            <p className="text-2xl font-bold text-yellow-600">
              {summary.pendingCount} / <span className="text-red-600">{summary.overdueCount}</span>
            </p>
          </div>
        </div>
      )}

      {/* View Toggle */}
      <div className="flex items-center gap-2">
        <span className="text-sm text-gray-500">View:</span>
        {[
          { key: 'pending', label: 'Pending' },
          { key: 'all', label: 'All Months' },
        ].map((view) => (
          <button
            key={view.key}
            onClick={() => setUrlState({ view: view.key })}
            className={`px-3 py-1 text-sm rounded-full ${
              urlState.view === view.key
                ? 'bg-blue-100 text-blue-800'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            {view.label}
          </button>
        ))}
      </div>

      {/* Info Banner */}
      <div className="bg-teal-50 border border-teal-200 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <Shield className="w-5 h-5 text-teal-600 mt-0.5" />
          <div>
            <h4 className="font-medium text-teal-900">ESI Contribution</h4>
            <p className="text-sm text-teal-700 mt-1">
              ESI contributions are due by the 15th of the following month.
              Employees with gross wages up to Rs. 21,000 are covered under ESI.
              Employee contribution: 0.75%, Employer contribution: 3.25%.
              Once covered, employees remain eligible for the full contribution period (Apr-Sep or Oct-Mar).
            </p>
          </div>
        </div>
      </div>

      {/* Monthly Status Table */}
      <DataTable
        columns={columns}
        data={displayData}
        isLoading={isLoading}
        pagination={{
          pageIndex: 0,
          pageSize: 12,
          pageCount: 1,
          onPageChange: () => {},
        }}
      />

      {/* Create Payment Modal */}
      <Modal
        isOpen={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
        title="Create ESI Payment"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Wage Month</label>
              <select
                value={newReturnForm.periodMonth}
                onChange={(e) => setNewReturnForm(prev => ({ ...prev, periodMonth: parseInt(e.target.value) }))}
                className="w-full rounded-md border border-gray-300 px-3 py-2"
              >
                {['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'].map((month, i) => (
                  <option key={i + 1} value={i + 1}>{month}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Year</label>
              <select
                value={newReturnForm.periodYear}
                onChange={(e) => setNewReturnForm(prev => ({ ...prev, periodYear: parseInt(e.target.value) }))}
                className="w-full rounded-md border border-gray-300 px-3 py-2"
              >
                {[2024, 2025, 2026].map((year) => (
                  <option key={year} value={year}>{year}</option>
                ))}
              </select>
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Proposed Payment Date</label>
            <input
              type="date"
              value={newReturnForm.proposedPaymentDate}
              onChange={(e) => setNewReturnForm(prev => ({ ...prev, proposedPaymentDate: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            />
          </div>
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
            <p className="text-sm text-yellow-800">
              <strong>Note:</strong> ESI contributions must be deposited by 15th of the following month.
              Late payment attracts interest.
            </p>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setCreateModalOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleCreatePayment}
              disabled={createPayment.isPending}
              className="flex items-center gap-2"
            >
              {createPayment.isPending ? <RefreshCw className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
              Create
            </Button>
          </div>
        </div>
      </Modal>

      {/* Record Payment Modal */}
      <Modal
        isOpen={paymentModalOpen}
        onClose={() => { setPaymentModalOpen(false); setSelectedMonth(null); }}
        title="Record ESI Payment"
      >
        <div className="space-y-4">
          {selectedMonth && (
            <div className="bg-gray-50 rounded-lg p-3">
              <p className="text-sm text-gray-600">ESI for {selectedMonth.monthName} {selectedMonth.year}</p>
              <p className="text-lg font-semibold">{formatINR(selectedMonth.esiDeducted)}</p>
              <p className="text-xs text-gray-500">Contribution Period: {selectedMonth.contributionPeriod}</p>
            </div>
          )}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Payment Date</label>
            <input
              type="date"
              value={paymentForm.paymentDate}
              onChange={(e) => setPaymentForm(prev => ({ ...prev, paymentDate: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Payment Mode</label>
            <select
              value={paymentForm.paymentMode}
              onChange={(e) => setPaymentForm(prev => ({ ...prev, paymentMode: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            >
              <option value="net_banking">Net Banking (ESIC Portal)</option>
              <option value="neft">NEFT</option>
              <option value="rtgs">RTGS</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Bank Reference</label>
            <input
              type="text"
              value={paymentForm.bankReference}
              onChange={(e) => setPaymentForm(prev => ({ ...prev, bankReference: e.target.value }))}
              placeholder="Transaction reference number"
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Amount Paid</label>
            <input
              type="number"
              value={paymentForm.actualAmountPaid}
              onChange={(e) => setPaymentForm(prev => ({ ...prev, actualAmountPaid: parseFloat(e.target.value) || 0 }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            />
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => { setPaymentModalOpen(false); setSelectedMonth(null); }}>
              Cancel
            </Button>
            <Button
              onClick={handleRecordPayment}
              disabled={recordPayment.isPending}
              className="flex items-center gap-2"
            >
              {recordPayment.isPending ? <RefreshCw className="w-4 h-4 animate-spin" /> : <CreditCard className="w-4 h-4" />}
              Record Payment
            </Button>
          </div>
        </div>
      </Modal>

      {/* Update Challan Number Modal */}
      <Modal
        isOpen={challanModalOpen}
        onClose={() => { setChallanModalOpen(false); setSelectedMonth(null); }}
        title="Update Challan Number"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Enter the challan number received from ESIC portal after payment.
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Challan Number</label>
            <input
              type="text"
              value={paymentForm.challanNumber}
              onChange={(e) => setPaymentForm(prev => ({ ...prev, challanNumber: e.target.value }))}
              placeholder="e.g., CHN12345678901234"
              className="w-full rounded-md border border-gray-300 px-3 py-2 font-mono"
            />
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => { setChallanModalOpen(false); setSelectedMonth(null); }}>
              Cancel
            </Button>
            <Button
              onClick={handleUpdateChallan}
              disabled={updateChallan.isPending || !paymentForm.challanNumber}
              className="flex items-center gap-2"
            >
              {updateChallan.isPending ? <RefreshCw className="w-4 h-4 animate-spin" /> : <Edit className="w-4 h-4" />}
              Update Challan
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default EsiReturnManagement;

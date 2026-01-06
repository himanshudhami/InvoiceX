import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useQueryStates, parseAsString } from 'nuqs';
import { useCompanies } from '@/hooks/api/useCompanies';
import {
  usePendingPfEcrs,
  useFiledPfEcrs,
  usePfEcrSummary,
  useCreatePfEcrPayment,
  useRecordPfPayment,
  useUpdatePfTrrn,
  useGeneratePfEcrFile,
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
} from 'lucide-react';
import { format, parseISO } from 'date-fns';
import type { PfEcrMonthlyStatus } from '@/services/api/types';

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
 * PF ECR Management Page
 * Handles PF ECR (Electronic Challan cum Return) generation, payment, and TRRN tracking
 */
const PfEcrManagement = () => {
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
  const [trrnModalOpen, setTrrnModalOpen] = useState(false);
  const [selectedMonth, setSelectedMonth] = useState<PfEcrMonthlyStatus | null>(null);
  const [paymentForm, setPaymentForm] = useState({
    paymentDate: format(new Date(), 'yyyy-MM-dd'),
    paymentMode: 'net_banking',
    bankName: '',
    bankAccountId: '',
    bankReference: '',
    actualAmountPaid: 0,
    trrn: '',
  });
  const [newEcrForm, setNewEcrForm] = useState({
    periodMonth: new Date().getMonth() || 12,
    periodYear: new Date().getFullYear(),
    proposedPaymentDate: format(new Date(), 'yyyy-MM-dd'),
  });

  const companyId = urlState.companyId || companies[0]?.id || '';

  // Queries
  const { data: pendingEcrs = [], isLoading: pendingLoading } = usePendingPfEcrs(companyId, !!companyId);
  const { data: filedEcrs = [], isLoading: filedLoading } = useFiledPfEcrs(companyId, urlState.financialYear, !!companyId);
  const { data: summary } = usePfEcrSummary(companyId, urlState.financialYear, !!companyId);

  // Mutations
  const createPayment = useCreatePfEcrPayment();
  const recordPayment = useRecordPfPayment();
  const updateTrrn = useUpdatePfTrrn();
  const generateFile = useGeneratePfEcrFile();

  // Handle create ECR payment
  const handleCreatePayment = async () => {
    if (!companyId) return;
    try {
      await createPayment.mutateAsync({
        companyId,
        ...newEcrForm,
      });
      setCreateModalOpen(false);
    } catch (error) {
      console.error('Failed to create PF ECR payment:', error);
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

  // Handle update TRRN
  const handleUpdateTrrn = async () => {
    if (!selectedMonth?.statutoryPaymentId || !paymentForm.trrn) return;
    try {
      await updateTrrn.mutateAsync({
        id: selectedMonth.statutoryPaymentId,
        trrn: paymentForm.trrn,
      });
      setTrrnModalOpen(false);
      setSelectedMonth(null);
    } catch (error) {
      console.error('Failed to update TRRN:', error);
    }
  };

  // Handle generate ECR file
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
      console.error('Failed to generate ECR file:', error);
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
  const columns: ColumnDef<PfEcrMonthlyStatus>[] = [
    {
      accessorKey: 'monthName',
      header: 'Wage Month',
      cell: ({ row }) => (
        <div>
          <div className="font-medium">{row.original.monthName} {row.original.year}</div>
        </div>
      ),
    },
    {
      accessorKey: 'memberCount',
      header: 'Members',
      cell: ({ row }) => (
        <div className="flex items-center gap-1">
          <Users className="w-4 h-4 text-gray-400" />
          <span className="font-medium">{row.original.memberCount}</span>
        </div>
      ),
    },
    {
      accessorKey: 'epfDeducted',
      header: 'EPF Deducted',
      cell: ({ row }) => (
        <div className="text-right font-medium">{formatINR(row.original.epfDeducted)}</div>
      ),
    },
    {
      accessorKey: 'epfDeposited',
      header: 'EPF Deposited',
      cell: ({ row }) => (
        <div className="text-right font-medium text-green-600">
          {row.original.epfDeposited > 0 ? formatINR(row.original.epfDeposited) : '—'}
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
      accessorKey: 'trrn',
      header: 'TRRN',
      cell: ({ row }) => (
        <div className="text-sm font-mono">
          {row.original.trrn || <span className="text-gray-400">—</span>}
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
            {/* Generate ECR File */}
            <button
              onClick={() => handleGenerateFile(item.month, item.year)}
              className="p-1 text-purple-600 hover:text-purple-800 hover:bg-purple-50 rounded"
              title="Download ECR File"
              disabled={generateFile.isPending}
            >
              <FileDown className="w-4 h-4" />
            </button>
            {/* Record Payment */}
            {item.status === 'pending' || item.status === 'overdue' ? (
              <button
                onClick={() => {
                  setSelectedMonth(item);
                  setPaymentForm(prev => ({ ...prev, actualAmountPaid: item.epfDeducted }));
                  setPaymentModalOpen(true);
                }}
                className="p-1 text-green-600 hover:text-green-800 hover:bg-green-50 rounded"
                title="Record Payment"
              >
                <CreditCard className="w-4 h-4" />
              </button>
            ) : null}
            {/* Update TRRN */}
            {item.status === 'paid' && !item.trrn && item.statutoryPaymentId && (
              <button
                onClick={() => {
                  setSelectedMonth(item);
                  setTrrnModalOpen(true);
                }}
                className="p-1 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded"
                title="Update TRRN"
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
  const displayData = urlState.view === 'pending' ? (summary?.monthlyStatus?.filter(m => m.status === 'pending' || m.status === 'overdue') || []) : (summary?.monthlyStatus || []);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">PF ECR Management</h1>
          <p className="text-gray-500 mt-1">
            Manage EPF contributions, ECR file generation, and TRRN tracking
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
            <p className="text-sm text-gray-500">Total EPF Deducted</p>
            <p className="text-2xl font-bold text-gray-900">{formatINR(summary.totalEpfDeducted)}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Total Deposited</p>
            <p className="text-2xl font-bold text-green-600">{formatINR(summary.totalEpfDeposited)}</p>
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
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <FileText className="w-5 h-5 text-blue-600 mt-0.5" />
          <div>
            <h4 className="font-medium text-blue-900">PF ECR Filing</h4>
            <p className="text-sm text-blue-700 mt-1">
              Download the ECR file for each month and upload it to the EPFO Unified Portal.
              PF contributions are due by the 15th of the following month.
              After payment, update the TRRN (Transaction Reference Number) received from the portal.
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
        title="Create PF Payment"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Wage Month</label>
              <select
                value={newEcrForm.periodMonth}
                onChange={(e) => setNewEcrForm(prev => ({ ...prev, periodMonth: parseInt(e.target.value) }))}
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
                value={newEcrForm.periodYear}
                onChange={(e) => setNewEcrForm(prev => ({ ...prev, periodYear: parseInt(e.target.value) }))}
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
              value={newEcrForm.proposedPaymentDate}
              onChange={(e) => setNewEcrForm(prev => ({ ...prev, proposedPaymentDate: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            />
          </div>
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
            <p className="text-sm text-yellow-800">
              <strong>Note:</strong> PF contributions must be deposited by 15th of the following month.
              Late payment attracts interest and damages.
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
        title="Record PF Payment"
      >
        <div className="space-y-4">
          {selectedMonth && (
            <div className="bg-gray-50 rounded-lg p-3">
              <p className="text-sm text-gray-600">PF for {selectedMonth.monthName} {selectedMonth.year}</p>
              <p className="text-lg font-semibold">{formatINR(selectedMonth.epfDeducted)}</p>
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
              <option value="net_banking">Net Banking (EPFO Portal)</option>
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

      {/* Update TRRN Modal */}
      <Modal
        isOpen={trrnModalOpen}
        onClose={() => { setTrrnModalOpen(false); setSelectedMonth(null); }}
        title="Update TRRN"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Enter the TRRN (Transaction Reference Number) received from EPFO portal after payment.
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">TRRN</label>
            <input
              type="text"
              value={paymentForm.trrn}
              onChange={(e) => setPaymentForm(prev => ({ ...prev, trrn: e.target.value }))}
              placeholder="e.g., TRRN1234567890123456"
              className="w-full rounded-md border border-gray-300 px-3 py-2 font-mono"
            />
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => { setTrrnModalOpen(false); setSelectedMonth(null); }}>
              Cancel
            </Button>
            <Button
              onClick={handleUpdateTrrn}
              disabled={updateTrrn.isPending || !paymentForm.trrn}
              className="flex items-center gap-2"
            >
              {updateTrrn.isPending ? <RefreshCw className="w-4 h-4 animate-spin" /> : <Edit className="w-4 h-4" />}
              Update TRRN
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default PfEcrManagement;

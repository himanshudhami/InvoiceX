import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useQueryStates, parseAsString } from 'nuqs';
import { useCompanies } from '@/hooks/api/useCompanies';
import {
  useTdsChallanList,
  useTdsChallanSummary,
  usePendingTdsChallans,
  useCreateTdsChallan,
  useRecordTdsPayment,
  useUpdateTdsCin,
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
  Calendar,
  AlertTriangle,
  CheckCircle,
  Clock,
  Plus,
  Edit,
} from 'lucide-react';
import { format, parseISO } from 'date-fns';
import type { TdsChallan, TdsChallanMonthlyStatus } from '@/services/api/types';

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
 * TDS Challan Management Page
 * Handles TDS Challan 281 generation, payment recording, and CIN updates
 */
const TdsChallanManagement = () => {
  const { data: companies = [] } = useCompanies();
  const fyOptions = useMemo(() => getFinancialYearOptions(), []);

  const [urlState, setUrlState] = useQueryStates(
    {
      companyId: parseAsString,
      financialYear: parseAsString.withDefault(getCurrentFinancialYear()),
      status: parseAsString,
    },
    { history: 'push' }
  );

  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [cinModalOpen, setCinModalOpen] = useState(false);
  const [selectedChallan, setSelectedChallan] = useState<TdsChallan | null>(null);
  const [paymentForm, setPaymentForm] = useState({
    paymentDate: format(new Date(), 'yyyy-MM-dd'),
    paymentMode: 'net_banking',
    bankName: '',
    bsrCode: '',
    cinNumber: '',
    bankReference: '',
    actualAmountPaid: 0,
  });
  const [newChallanForm, setNewChallanForm] = useState({
    tdsType: 'salary' as 'salary' | 'contractor',
    periodMonth: new Date().getMonth() || 12,
    periodYear: new Date().getFullYear(),
    proposedPaymentDate: format(new Date(), 'yyyy-MM-dd'),
  });

  const companyId = urlState.companyId || companies[0]?.id || '';

  // Queries
  const { data: challanResponse, isLoading, error } = useTdsChallanList(
    companyId,
    {
      financialYear: urlState.financialYear,
      status: urlState.status || undefined,
      pageSize: 100,
    },
    !!companyId
  );
  const challanList = challanResponse?.items || [];

  const { data: summary } = useTdsChallanSummary(companyId, urlState.financialYear, !!companyId);
  const { data: pendingChallans = [] } = usePendingTdsChallans(companyId, urlState.financialYear);

  // Mutations
  const createChallan = useCreateTdsChallan();
  const recordPayment = useRecordTdsPayment();
  const updateCin = useUpdateTdsCin();

  // Handle create challan
  const handleCreateChallan = async () => {
    if (!companyId) return;
    try {
      await createChallan.mutateAsync({
        companyId,
        ...newChallanForm,
      });
      setCreateModalOpen(false);
    } catch (error) {
      console.error('Failed to create TDS challan:', error);
    }
  };

  // Handle record payment
  const handleRecordPayment = async () => {
    if (!selectedChallan) return;
    try {
      await recordPayment.mutateAsync({
        id: selectedChallan.id,
        request: paymentForm,
      });
      setPaymentModalOpen(false);
      setSelectedChallan(null);
    } catch (error) {
      console.error('Failed to record payment:', error);
    }
  };

  // Handle update CIN
  const handleUpdateCin = async () => {
    if (!selectedChallan || !paymentForm.cinNumber) return;
    try {
      await updateCin.mutateAsync({
        id: selectedChallan.id,
        request: {
          bsrCode: paymentForm.bsrCode || '',
          cin: paymentForm.cinNumber,
        },
      });
      setCinModalOpen(false);
      setSelectedChallan(null);
    } catch (error) {
      console.error('Failed to update CIN:', error);
    }
  };

  // Status badge
  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { label: string; className: string; icon: React.ReactNode }> = {
      draft: { label: 'Draft', className: 'bg-gray-100 text-gray-800', icon: <FileText className="w-3 h-3" /> },
      pending: { label: 'Pending', className: 'bg-yellow-100 text-yellow-800', icon: <Clock className="w-3 h-3" /> },
      paid: { label: 'Paid', className: 'bg-green-100 text-green-800', icon: <CheckCircle className="w-3 h-3" /> },
      verified: { label: 'Verified', className: 'bg-blue-100 text-blue-800', icon: <CheckCircle className="w-3 h-3" /> },
      overdue: { label: 'Overdue', className: 'bg-red-100 text-red-800', icon: <AlertTriangle className="w-3 h-3" /> },
    };
    const config = statusConfig[status] || statusConfig.draft;
    return (
      <span className={`inline-flex items-center gap-1 px-2 py-1 rounded text-xs font-medium ${config.className}`}>
        {config.icon}
        {config.label}
      </span>
    );
  };

  // Table columns
  const columns: ColumnDef<TdsChallan>[] = [
    {
      accessorKey: 'periodMonth',
      header: 'Period',
      cell: ({ row }) => {
        const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        return (
          <div>
            <div className="font-medium">{monthNames[row.original.periodMonth - 1]} {row.original.periodYear}</div>
            <div className="text-sm text-gray-500">FY {row.original.financialYear}</div>
          </div>
        );
      },
    },
    {
      accessorKey: 'tdsType',
      header: 'TDS Type',
      cell: ({ row }) => (
        <span className="capitalize">{row.original.tdsType}</span>
      ),
    },
    {
      accessorKey: 'basicTax',
      header: 'TDS Amount',
      cell: ({ row }) => (
        <div className="text-right">
          <div className="font-medium">{formatINR(row.original.basicTax)}</div>
          <div className="text-sm text-gray-500">
            + {formatINR(row.original.surcharge + row.original.educationCess)} (S+C)
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'interest',
      header: 'Interest/Fee',
      cell: ({ row }) => (
        <div className="text-right">
          {row.original.interest > 0 || row.original.lateFee > 0 ? (
            <>
              <div className="font-medium text-red-600">{formatINR(row.original.interest)}</div>
              <div className="text-sm text-gray-500">Fee: {formatINR(row.original.lateFee)}</div>
            </>
          ) : (
            <span className="text-gray-400">—</span>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'totalAmount',
      header: 'Total Payable',
      cell: ({ row }) => (
        <div className="text-right font-semibold">{formatINR(row.original.totalAmount)}</div>
      ),
    },
    {
      accessorKey: 'dueDate',
      header: 'Due Date',
      cell: ({ row }) => {
        const dueDate = parseISO(row.original.dueDate);
        const isOverdue = new Date() > dueDate && row.original.status !== 'paid';
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
      accessorKey: 'cinNumber',
      header: 'CIN',
      cell: ({ row }) => (
        <div className="text-sm font-mono">
          {row.original.cinNumber || <span className="text-gray-400">—</span>}
        </div>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const challan = row.original;
        return (
          <div className="flex items-center gap-2">
            {challan.status === 'pending' && (
              <button
                onClick={() => {
                  setSelectedChallan(challan);
                  setPaymentForm(prev => ({ ...prev, actualAmountPaid: challan.totalAmount }));
                  setPaymentModalOpen(true);
                }}
                className="p-1 text-green-600 hover:text-green-800 hover:bg-green-50 rounded"
                title="Record Payment"
              >
                <CreditCard className="w-4 h-4" />
              </button>
            )}
            {challan.status === 'paid' && !challan.cinNumber && (
              <button
                onClick={() => {
                  setSelectedChallan(challan);
                  setCinModalOpen(true);
                }}
                className="p-1 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded"
                title="Update CIN"
              >
                <Edit className="w-4 h-4" />
              </button>
            )}
            <a
              href={`/api/statutory/tds-challan/${challan.id}/download`}
              className="p-1 text-purple-600 hover:text-purple-800 hover:bg-purple-50 rounded"
              title="Download Challan"
              download
            >
              <Download className="w-4 h-4" />
            </a>
          </div>
        );
      },
    },
  ];

  // Monthly status columns
  const monthlyColumns: ColumnDef<TdsChallanMonthlyStatus>[] = [
    {
      accessorKey: 'monthName',
      header: 'Month',
      cell: ({ row }) => (
        <div className="font-medium">{row.original.monthName} {row.original.year}</div>
      ),
    },
    {
      accessorKey: 'tdsDeducted',
      header: 'TDS Deducted',
      cell: ({ row }) => (
        <div className="text-right font-medium">{formatINR(row.original.tdsDeducted)}</div>
      ),
    },
    {
      accessorKey: 'tdsDeposited',
      header: 'TDS Deposited',
      cell: ({ row }) => (
        <div className="text-right font-medium text-green-600">{formatINR(row.original.tdsDeposited)}</div>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status),
    },
    {
      accessorKey: 'dueDate',
      header: 'Due Date',
      cell: ({ row }) => (
        <div className="text-sm">{format(parseISO(row.original.dueDate), 'dd MMM yyyy')}</div>
      ),
    },
    {
      accessorKey: 'cinNumber',
      header: 'CIN',
      cell: ({ row }) => (
        <div className="text-sm font-mono">{row.original.cinNumber || '—'}</div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">TDS Challan Management</h1>
          <p className="text-gray-500 mt-1">
            Manage TDS Challan 281 deposits for salary and contractor payments
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
            Create Challan
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Total Deducted</p>
            <p className="text-2xl font-bold text-gray-900">{formatINR(summary.totalTdsDeducted)}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Total Deposited</p>
            <p className="text-2xl font-bold text-green-600">{formatINR(summary.totalTdsDeposited)}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Variance</p>
            <p className={`text-2xl font-bold ${summary.totalVariance > 0 ? 'text-yellow-600' : 'text-green-600'}`}>
              {formatINR(summary.totalVariance)}
            </p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Pending Challans</p>
            <p className="text-2xl font-bold text-gray-900">{summary.pendingCount}</p>
          </div>
        </div>
      )}

      {/* Monthly Status Summary */}
      {summary?.monthlyStatus && summary.monthlyStatus.length > 0 && (
        <div className="bg-white border rounded-lg p-4">
          <h3 className="text-lg font-semibold mb-4">Monthly TDS Status</h3>
          <DataTable
            columns={monthlyColumns}
            data={summary.monthlyStatus}
            isLoading={false}
            pagination={{
              pageIndex: 0,
              pageSize: 12,
              pageCount: 1,
              onPageChange: () => {},
            }}
          />
        </div>
      )}

      {/* Status Filter */}
      <div className="flex items-center gap-2">
        <span className="text-sm text-gray-500">Filter:</span>
        {['all', 'draft', 'pending', 'paid', 'verified'].map((status) => (
          <button
            key={status}
            onClick={() => setUrlState({ status: status === 'all' ? null : status })}
            className={`px-3 py-1 text-sm rounded-full ${
              (status === 'all' && !urlState.status) || urlState.status === status
                ? 'bg-blue-100 text-blue-800'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            {status.charAt(0).toUpperCase() + status.slice(1)}
          </button>
        ))}
      </div>

      {/* Challans Table */}
      {error ? (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2 text-red-600">
            <AlertTriangle className="w-5 h-5" />
            <p>Failed to load TDS challan data. Please try again.</p>
          </div>
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={challanList}
          isLoading={isLoading}
          pagination={{
            pageIndex: 0,
            pageSize: 100,
            pageCount: 1,
            onPageChange: () => {},
          }}
        />
      )}

      {/* Create Challan Modal */}
      <Modal
        isOpen={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
        title="Create TDS Challan"
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">TDS Type</label>
            <select
              value={newChallanForm.tdsType}
              onChange={(e) => setNewChallanForm(prev => ({ ...prev, tdsType: e.target.value as 'salary' | 'contractor' }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            >
              <option value="salary">Salary (Section 192)</option>
              <option value="contractor">Contractor (Section 194C)</option>
            </select>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Month</label>
              <select
                value={newChallanForm.periodMonth}
                onChange={(e) => setNewChallanForm(prev => ({ ...prev, periodMonth: parseInt(e.target.value) }))}
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
                value={newChallanForm.periodYear}
                onChange={(e) => setNewChallanForm(prev => ({ ...prev, periodYear: parseInt(e.target.value) }))}
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
              value={newChallanForm.proposedPaymentDate}
              onChange={(e) => setNewChallanForm(prev => ({ ...prev, proposedPaymentDate: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2"
            />
          </div>
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
            <p className="text-sm text-yellow-800">
              <strong>Note:</strong> TDS must be deposited by 7th of the following month (30th April for March).
            </p>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setCreateModalOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleCreateChallan}
              disabled={createChallan.isPending}
              className="flex items-center gap-2"
            >
              {createChallan.isPending ? <RefreshCw className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
              Create
            </Button>
          </div>
        </div>
      </Modal>

      {/* Record Payment Modal */}
      <Modal
        isOpen={paymentModalOpen}
        onClose={() => { setPaymentModalOpen(false); setSelectedChallan(null); }}
        title="Record TDS Payment"
      >
        <div className="space-y-4">
          {selectedChallan && (
            <div className="bg-gray-50 rounded-lg p-3">
              <p className="text-sm text-gray-600">Challan for {selectedChallan.tdsType} TDS</p>
              <p className="text-lg font-semibold">{formatINR(selectedChallan.totalAmount)}</p>
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
              <option value="net_banking">Net Banking</option>
              <option value="neft">NEFT</option>
              <option value="rtgs">RTGS</option>
              <option value="debit_card">Debit Card</option>
            </select>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">BSR Code</label>
              <input
                type="text"
                value={paymentForm.bsrCode}
                onChange={(e) => setPaymentForm(prev => ({ ...prev, bsrCode: e.target.value }))}
                placeholder="7 digit code"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">CIN Number</label>
              <input
                type="text"
                value={paymentForm.cinNumber}
                onChange={(e) => setPaymentForm(prev => ({ ...prev, cinNumber: e.target.value }))}
                placeholder="Challan ID Number"
                className="w-full rounded-md border border-gray-300 px-3 py-2"
              />
            </div>
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
            <Button variant="outline" onClick={() => { setPaymentModalOpen(false); setSelectedChallan(null); }}>
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

      {/* Update CIN Modal */}
      <Modal
        isOpen={cinModalOpen}
        onClose={() => { setCinModalOpen(false); setSelectedChallan(null); }}
        title="Update CIN Number"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Enter the Challan Identification Number (CIN) received after payment.
            CIN format: BSR Code (7) + Date (DDMMYYYY) + Serial (5) = 20 digits
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">BSR Code (7 digits)</label>
            <input
              type="text"
              value={paymentForm.bsrCode}
              onChange={(e) => setPaymentForm(prev => ({ ...prev, bsrCode: e.target.value }))}
              placeholder="e.g., 1234567"
              maxLength={7}
              className="w-full rounded-md border border-gray-300 px-3 py-2 font-mono"
            />
            <p className="text-xs text-gray-500 mt-1">Bank branch code from RBI</p>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">CIN Number (20 digits)</label>
            <input
              type="text"
              value={paymentForm.cinNumber}
              onChange={(e) => setPaymentForm(prev => ({ ...prev, cinNumber: e.target.value }))}
              placeholder="e.g., 12345671501202500001"
              maxLength={20}
              className="w-full rounded-md border border-gray-300 px-3 py-2 font-mono"
            />
            <p className="text-xs text-gray-500 mt-1">Full Challan Identification Number from bank</p>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => { setCinModalOpen(false); setSelectedChallan(null); }}>
              Cancel
            </Button>
            <Button
              onClick={handleUpdateCin}
              disabled={updateCin.isPending || !paymentForm.cinNumber || !paymentForm.bsrCode}
              className="flex items-center gap-2"
            >
              {updateCin.isPending ? <RefreshCw className="w-4 h-4 animate-spin" /> : <Edit className="w-4 h-4" />}
              Update CIN
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default TdsChallanManagement;

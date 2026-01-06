import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useQueryStates, parseAsString } from 'nuqs';
import { useCompanies } from '@/hooks/api/useCompanies';
import {
  useForm24QFilingList,
  useForm24QFilingStatistics,
  useCreateForm24QFiling,
  useValidateForm24QFiling,
  useGenerateForm24QFvu,
  useDownloadForm24QFvu,
  useSubmitForm24QFiling,
  useRecordForm24QAcknowledgement,
  useRefreshForm24QFiling,
  useDeleteForm24QFiling,
} from '@/features/statutory/hooks';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/button';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { formatINR } from '@/lib/currency';
import {
  FileText,
  Download,
  CheckCircle,
  RefreshCw,
  Send,
  AlertTriangle,
  Plus,
  FileCheck,
  Clock,
  XCircle,
  Trash2,
} from 'lucide-react';
import { format } from 'date-fns';
import type { Form24QFilingSummary } from '@/services/api/finance/statutory';

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
 * Get current quarter based on date
 */
const getCurrentQuarter = (): string => {
  const now = new Date();
  const month = now.getMonth();
  if (month >= 0 && month <= 2) return 'Q4';
  if (month >= 3 && month <= 5) return 'Q1';
  if (month >= 6 && month <= 8) return 'Q2';
  return 'Q3';
};

const QUARTERS = ['Q1', 'Q2', 'Q3', 'Q4'];

/**
 * Form 24Q Filing Management Page
 * Handles quarterly TDS return filing for salaries (Section 192)
 */
const Form24QFilings = () => {
  const { data: companies = [] } = useCompanies();
  const fyOptions = useMemo(() => getFinancialYearOptions(), []);

  const [urlState, setUrlState] = useQueryStates(
    {
      companyId: parseAsString,
      financialYear: parseAsString.withDefault(getCurrentFinancialYear()),
      status: parseAsString,
      quarter: parseAsString,
    },
    { history: 'push' }
  );

  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [selectedQuarter, setSelectedQuarter] = useState(getCurrentQuarter());
  const [acknowledgeModalOpen, setAcknowledgeModalOpen] = useState(false);
  const [selectedFilingId, setSelectedFilingId] = useState<string | null>(null);
  const [acknowledgementNumber, setAcknowledgementNumber] = useState('');
  const [tokenNumber, setTokenNumber] = useState('');
  const [filingDateInput, setFilingDateInput] = useState('');

  const companyId = urlState.companyId || companies[0]?.id || '';

  // Queries
  const { data: filingsResponse, error } = useForm24QFilingList(companyId, {
    financialYear: urlState.financialYear,
    quarter: urlState.quarter || undefined,
    status: urlState.status || undefined,
    pageSize: 50,
  });
  const filingsList = filingsResponse?.items || [];

  const { data: statistics } = useForm24QFilingStatistics(
    companyId,
    urlState.financialYear,
    !!companyId
  );

  // Mutations
  const createFiling = useCreateForm24QFiling();
  const validateFiling = useValidateForm24QFiling();
  const generateFvu = useGenerateForm24QFvu();
  const downloadFvu = useDownloadForm24QFvu();
  const submitFiling = useSubmitForm24QFiling();
  const recordAcknowledgement = useRecordForm24QAcknowledgement();
  const refreshFiling = useRefreshForm24QFiling();
  const deleteFiling = useDeleteForm24QFiling();

  // Handle create draft filing
  const handleCreateDraft = async () => {
    if (!companyId) return;
    try {
      await createFiling.mutateAsync({
        companyId,
        financialYear: urlState.financialYear,
        quarter: selectedQuarter,
      });
      setCreateModalOpen(false);
    } catch (error) {
      console.error('Failed to create Form 24Q draft:', error);
    }
  };

  // Handle validate
  const handleValidate = async (id: string) => {
    try {
      await validateFiling.mutateAsync(id);
    } catch (error) {
      console.error('Failed to validate filing:', error);
    }
  };

  // Handle generate FVU
  const handleGenerateFvu = async (id: string) => {
    try {
      await generateFvu.mutateAsync({ id });
    } catch (error) {
      console.error('Failed to generate FVU:', error);
    }
  };

  // Handle download FVU
  const handleDownloadFvu = async (id: string, quarter: string, fy: string) => {
    try {
      await downloadFvu.mutateAsync({
        id,
        filename: `Form24Q_${fy}_${quarter}.txt`,
      });
    } catch (error) {
      console.error('Failed to download FVU:', error);
    }
  };

  // Handle submit
  const handleSubmit = async (id: string) => {
    try {
      await submitFiling.mutateAsync({ id });
    } catch (error) {
      console.error('Failed to mark as submitted:', error);
    }
  };

  // Handle record acknowledgement
  const handleRecordAcknowledgement = async () => {
    if (!selectedFilingId || !acknowledgementNumber) return;
    try {
      await recordAcknowledgement.mutateAsync({
        id: selectedFilingId,
        request: {
          acknowledgementNumber,
          tokenNumber: tokenNumber || undefined,
          filingDate: filingDateInput || undefined,
        },
      });
      setAcknowledgeModalOpen(false);
      setSelectedFilingId(null);
      setAcknowledgementNumber('');
      setTokenNumber('');
      setFilingDateInput('');
    } catch (error) {
      console.error('Failed to record acknowledgement:', error);
    }
  };

  // Handle refresh data
  const handleRefresh = async (id: string) => {
    try {
      await refreshFiling.mutateAsync({ id });
    } catch (error) {
      console.error('Failed to refresh data:', error);
    }
  };

  // Handle delete draft
  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this draft filing?')) return;
    try {
      await deleteFiling.mutateAsync(id);
    } catch (error) {
      console.error('Failed to delete filing:', error);
    }
  };

  // Status badge
  const getStatusBadge = (status: string, isOverdue: boolean) => {
    const statusConfig: Record<string, { label: string; className: string; icon: React.ReactNode }> =
      {
        draft: {
          label: 'Draft',
          className: 'bg-gray-100 text-gray-800',
          icon: <FileText className="w-3 h-3" />,
        },
        validated: {
          label: 'Validated',
          className: 'bg-blue-100 text-blue-800',
          icon: <CheckCircle className="w-3 h-3" />,
        },
        fvu_generated: {
          label: 'FVU Ready',
          className: 'bg-purple-100 text-purple-800',
          icon: <FileCheck className="w-3 h-3" />,
        },
        submitted: {
          label: 'Submitted',
          className: 'bg-yellow-100 text-yellow-800',
          icon: <Send className="w-3 h-3" />,
        },
        acknowledged: {
          label: 'Acknowledged',
          className: 'bg-green-100 text-green-800',
          icon: <CheckCircle className="w-3 h-3" />,
        },
        rejected: {
          label: 'Rejected',
          className: 'bg-red-100 text-red-800',
          icon: <XCircle className="w-3 h-3" />,
        },
        revised: {
          label: 'Revised',
          className: 'bg-orange-100 text-orange-800',
          icon: <RefreshCw className="w-3 h-3" />,
        },
      };
    const config = statusConfig[status] || statusConfig.draft;
    return (
      <div className="flex items-center gap-1">
        <span className={`px-2 py-1 rounded text-xs font-medium flex items-center gap-1 ${config.className}`}>
          {config.icon}
          {config.label}
        </span>
        {isOverdue && (
          <span className="px-2 py-1 rounded text-xs font-medium bg-red-100 text-red-800 flex items-center gap-1">
            <Clock className="w-3 h-3" />
            Overdue
          </span>
        )}
      </div>
    );
  };

  // Table columns
  const columns: ColumnDef<Form24QFilingSummary>[] = [
    {
      accessorKey: 'quarter',
      header: 'Quarter',
      cell: ({ row }) => (
        <div>
          <div className="font-medium text-gray-900">{row.original.quarter}</div>
          <div className="text-sm text-gray-500">FY {row.original.financialYear}</div>
        </div>
      ),
    },
    {
      accessorKey: 'tan',
      header: 'TAN',
      cell: ({ row }) => (
        <div>
          <div className="font-mono text-sm">{row.original.tan}</div>
          <div className="text-xs text-gray-500">
            {row.original.formType === 'original' ? 'Original' : `Rev. ${row.original.revisionNumber}`}
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'totalEmployees',
      header: 'Employees',
      cell: ({ row }) => (
        <div className="text-center font-medium">{row.original.totalEmployees}</div>
      ),
    },
    {
      accessorKey: 'totalTdsDeducted',
      header: 'TDS Deducted',
      cell: ({ row }) => (
        <div className="text-right font-medium">{formatINR(row.original.totalTdsDeducted)}</div>
      ),
    },
    {
      accessorKey: 'totalTdsDeposited',
      header: 'TDS Deposited',
      cell: ({ row }) => (
        <div className="text-right">
          <div className="font-medium">{formatINR(row.original.totalTdsDeposited)}</div>
          {row.original.variance !== 0 && (
            <div
              className={`text-xs ${row.original.variance > 0 ? 'text-red-600' : 'text-green-600'}`}
            >
              Variance: {formatINR(row.original.variance)}
            </div>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status, row.original.isOverdue),
    },
    {
      accessorKey: 'dueDate',
      header: 'Due Date',
      cell: ({ row }) => (
        <div className="text-sm">
          <div className={row.original.isOverdue ? 'text-red-600 font-medium' : 'text-gray-600'}>
            {format(new Date(row.original.dueDate), 'dd MMM yyyy')}
          </div>
          {row.original.filingDate && (
            <div className="text-xs text-green-600">
              Filed: {format(new Date(row.original.filingDate), 'dd MMM yyyy')}
            </div>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'acknowledgementNumber',
      header: 'Ack No.',
      cell: ({ row }) =>
        row.original.acknowledgementNumber ? (
          <div className="font-mono text-xs">{row.original.acknowledgementNumber}</div>
        ) : (
          <span className="text-gray-400">—</span>
        ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const filing = row.original;
        return (
          <div className="flex items-center gap-1">
            {/* Refresh Data */}
            {filing.status === 'draft' && (
              <button
                onClick={() => handleRefresh(filing.id)}
                className="p-1 text-gray-600 hover:text-gray-800 hover:bg-gray-100 rounded"
                title="Refresh Data"
              >
                <RefreshCw className="w-4 h-4" />
              </button>
            )}

            {/* Validate */}
            {filing.status === 'draft' && (
              <button
                onClick={() => handleValidate(filing.id)}
                className="p-1 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded"
                title="Validate"
              >
                <CheckCircle className="w-4 h-4" />
              </button>
            )}

            {/* Generate FVU */}
            {filing.status === 'validated' && (
              <button
                onClick={() => handleGenerateFvu(filing.id)}
                className="p-1 text-purple-600 hover:text-purple-800 hover:bg-purple-50 rounded"
                title="Generate FVU"
              >
                <FileCheck className="w-4 h-4" />
              </button>
            )}

            {/* Download FVU */}
            {filing.hasFvuFile && (
              <button
                onClick={() =>
                  handleDownloadFvu(filing.id, filing.quarter, filing.financialYear)
                }
                className="p-1 text-green-600 hover:text-green-800 hover:bg-green-50 rounded"
                title="Download FVU"
              >
                <Download className="w-4 h-4" />
              </button>
            )}

            {/* Mark as Submitted */}
            {filing.status === 'fvu_generated' && (
              <button
                onClick={() => handleSubmit(filing.id)}
                className="p-1 text-yellow-600 hover:text-yellow-800 hover:bg-yellow-50 rounded"
                title="Mark as Submitted"
              >
                <Send className="w-4 h-4" />
              </button>
            )}

            {/* Record Acknowledgement */}
            {filing.status === 'submitted' && (
              <button
                onClick={() => {
                  setSelectedFilingId(filing.id);
                  setAcknowledgeModalOpen(true);
                }}
                className="p-1 text-green-600 hover:text-green-800 hover:bg-green-50 rounded"
                title="Record Acknowledgement"
              >
                <CheckCircle className="w-4 h-4" />
              </button>
            )}

            {/* Delete Draft */}
            {filing.status === 'draft' && (
              <button
                onClick={() => handleDelete(filing.id)}
                className="p-1 text-red-600 hover:text-red-800 hover:bg-red-50 rounded"
                title="Delete Draft"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            )}
          </div>
        );
      },
    },
  ];

  // Quarter status card
  const QuarterCard = ({
    quarter,
    status,
  }: {
    quarter: string;
    status?: {
      status: string;
      hasFiling: boolean;
      isOverdue: boolean;
      dueDate: string;
      tdsDeducted: number;
      acknowledgementNumber?: string;
    };
  }) => {
    const getQuarterColor = () => {
      if (!status?.hasFiling) return 'bg-gray-50 border-gray-200';
      if (status.status === 'acknowledged') return 'bg-green-50 border-green-200';
      if (status.isOverdue) return 'bg-red-50 border-red-200';
      if (status.status === 'fvu_generated' || status.status === 'submitted')
        return 'bg-yellow-50 border-yellow-200';
      return 'bg-blue-50 border-blue-200';
    };

    return (
      <div className={`border rounded-lg p-4 ${getQuarterColor()}`}>
        <div className="flex items-center justify-between mb-2">
          <span className="font-semibold text-gray-900">{quarter}</span>
          {status?.hasFiling ? (
            getStatusBadge(status.status, status.isOverdue)
          ) : (
            <span className="text-xs text-gray-500">Not created</span>
          )}
        </div>
        {status?.hasFiling ? (
          <div className="space-y-1 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-500">TDS:</span>
              <span className="font-medium">{formatINR(status.tdsDeducted)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-500">Due:</span>
              <span className={status.isOverdue ? 'text-red-600' : ''}>
                {format(new Date(status.dueDate), 'dd MMM')}
              </span>
            </div>
            {status.acknowledgementNumber && (
              <div className="text-xs text-green-600 mt-1">
                Ack: {status.acknowledgementNumber}
              </div>
            )}
          </div>
        ) : (
          <div className="text-sm text-gray-500">
            Due: {quarter === 'Q1' ? 'Jul 31' : quarter === 'Q2' ? 'Oct 31' : quarter === 'Q3' ? 'Jan 31' : 'May 31'}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Form 24Q Filing</h1>
          <p className="text-gray-500 mt-1">
            Quarterly TDS returns for salaries (Section 192)
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
            Create Filing
          </Button>
        </div>
      </div>

      {/* Quarter Overview Cards */}
      {statistics && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <QuarterCard quarter="Q1" status={statistics.q1} />
          <QuarterCard quarter="Q2" status={statistics.q2} />
          <QuarterCard quarter="Q3" status={statistics.q3} />
          <QuarterCard quarter="Q4" status={statistics.q4} />
        </div>
      )}

      {/* Statistics Summary */}
      {statistics && (
        <div className="grid grid-cols-2 md:grid-cols-6 gap-4">
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Total Filings</p>
            <p className="text-2xl font-bold text-gray-900">{statistics.totalFilings}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Pending</p>
            <p className="text-2xl font-bold text-yellow-600">{statistics.pendingCount}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Acknowledged</p>
            <p className="text-2xl font-bold text-green-600">{statistics.acknowledgedCount}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Overdue</p>
            <p className="text-2xl font-bold text-red-600">{statistics.overdueCount}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">TDS Deducted</p>
            <p className="text-xl font-bold text-gray-900">{formatINR(statistics.totalTdsDeducted)}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">TDS Deposited</p>
            <p className="text-xl font-bold text-green-600">{formatINR(statistics.totalTdsDeposited)}</p>
          </div>
        </div>
      )}

      {/* Status Filter */}
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-sm text-gray-500">Filter:</span>
        {['all', 'draft', 'validated', 'fvu_generated', 'submitted', 'acknowledged'].map(
          (status) => (
            <button
              key={status}
              onClick={() => setUrlState({ status: status === 'all' ? null : status })}
              className={`px-3 py-1 text-sm rounded-full ${
                (status === 'all' && !urlState.status) || urlState.status === status
                  ? 'bg-blue-100 text-blue-800'
                  : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
              }`}
            >
              {status === 'fvu_generated' ? 'FVU Ready' : status.charAt(0).toUpperCase() + status.slice(1)}
            </button>
          )
        )}
        <span className="text-gray-300">|</span>
        {['all', ...QUARTERS].map((q) => (
          <button
            key={q}
            onClick={() => setUrlState({ quarter: q === 'all' ? null : q })}
            className={`px-3 py-1 text-sm rounded-full ${
              (q === 'all' && !urlState.quarter) || urlState.quarter === q
                ? 'bg-purple-100 text-purple-800'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            {q === 'all' ? 'All Quarters' : q}
          </button>
        ))}
      </div>

      {/* Data Table */}
      {error ? (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2 text-red-600">
            <AlertTriangle className="w-5 h-5" />
            <p>Failed to load Form 24Q filings. Please try again.</p>
          </div>
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={filingsList}
        />
      )}

      {/* Create Filing Modal */}
      <Modal
        isOpen={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
        title="Create Form 24Q Filing"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Create a new Form 24Q draft filing for quarterly TDS returns.
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Financial Year
            </label>
            <div className="text-gray-900 font-medium">FY {urlState.financialYear}</div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Quarter</label>
            <select
              value={selectedQuarter}
              onChange={(e) => setSelectedQuarter(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {QUARTERS.map((q) => (
                <option key={q} value={q}>
                  {q} ({q === 'Q1' ? 'Apr-Jun' : q === 'Q2' ? 'Jul-Sep' : q === 'Q3' ? 'Oct-Dec' : 'Jan-Mar'})
                </option>
              ))}
            </select>
          </div>
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
            <p className="text-sm text-blue-800">
              This will aggregate all salary TDS data from payroll transactions for the selected quarter.
            </p>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setCreateModalOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleCreateDraft}
              disabled={createFiling.isPending}
              className="flex items-center gap-2"
            >
              {createFiling.isPending ? (
                <RefreshCw className="w-4 h-4 animate-spin" />
              ) : (
                <Plus className="w-4 h-4" />
              )}
              Create Draft
            </Button>
          </div>
        </div>
      </Modal>

      {/* Record Acknowledgement Modal */}
      <Modal
        isOpen={acknowledgeModalOpen}
        onClose={() => {
          setAcknowledgeModalOpen(false);
          setSelectedFilingId(null);
          setAcknowledgementNumber('');
          setTokenNumber('');
          setFilingDateInput('');
        }}
        title="Record Acknowledgement"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Enter the acknowledgement details received from NSDL after successful filing.
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Acknowledgement Number <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={acknowledgementNumber}
              onChange={(e) => setAcknowledgementNumber(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="e.g., 1234567890123456"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Token Number
            </label>
            <input
              type="text"
              value={tokenNumber}
              onChange={(e) => setTokenNumber(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Optional"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Filing Date
            </label>
            <input
              type="date"
              value={filingDateInput}
              onChange={(e) => setFilingDateInput(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div className="flex justify-end gap-3">
            <Button
              variant="outline"
              onClick={() => {
                setAcknowledgeModalOpen(false);
                setSelectedFilingId(null);
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleRecordAcknowledgement}
              disabled={recordAcknowledgement.isPending || !acknowledgementNumber}
              className="flex items-center gap-2"
            >
              {recordAcknowledgement.isPending ? (
                <RefreshCw className="w-4 h-4 animate-spin" />
              ) : (
                <CheckCircle className="w-4 h-4" />
              )}
              Record
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default Form24QFilings;

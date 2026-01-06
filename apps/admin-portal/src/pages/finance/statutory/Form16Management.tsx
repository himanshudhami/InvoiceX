import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs';
import { useCompanies } from '@/hooks/api/useCompanies';
import {
  useForm16List,
  useForm16Summary,
  useGenerateForm16,
  useIssueForm16,
  useRegenerateForm16,
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
  Users,
  Send,
  Eye,
  AlertTriangle,
} from 'lucide-react';
import { format } from 'date-fns';
import type { Form16Data } from '@/services/api/types';

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
 * Form 16 Management Page
 * Handles Form 16 generation, viewing, and issuance for employees
 */
const Form16Management = () => {
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

  const [selectedRows, setSelectedRows] = useState<string[]>([]);
  const [generateModalOpen, setGenerateModalOpen] = useState(false);
  const [issueModalOpen, setIssueModalOpen] = useState(false);
  const [selectedForm16, setSelectedForm16] = useState<Form16Data | null>(null);
  const [previewModalOpen, setPreviewModalOpen] = useState(false);

  const companyId = urlState.companyId || companies[0]?.id || '';

  // Queries - use paginated list with financial year filter
  const { data: form16Response, isLoading, error } = useForm16List(
    companyId,
    {
      financialYear: urlState.financialYear,
      status: urlState.status || undefined,
      pageSize: 100,
    }
  );
  const form16List = form16Response?.items || [];

  const { data: summary } = useForm16Summary(companyId, urlState.financialYear, !!companyId);

  // Mutations
  const generateForm16 = useGenerateForm16();
  const issueForm16 = useIssueForm16();
  const regenerateForm16 = useRegenerateForm16();

  // Filter by status if specified
  const filteredList = useMemo(() => {
    if (!urlState.status) return form16List;
    return form16List.filter((item) => item.status === urlState.status);
  }, [form16List, urlState.status]);

  // Handle generate Form 16
  const handleGenerate = async () => {
    if (!companyId) return;
    try {
      await generateForm16.mutateAsync({
        companyId,
        financialYear: urlState.financialYear,
      });
      setGenerateModalOpen(false);
    } catch (error) {
      console.error('Failed to generate Form 16:', error);
    }
  };

  // Handle issue Form 16
  const handleIssue = async (id: string) => {
    try {
      await issueForm16.mutateAsync(id);
    } catch (error) {
      console.error('Failed to issue Form 16:', error);
    }
  };

  // Handle bulk issue (issue one by one)
  const [bulkIssuing, setBulkIssuing] = useState(false);
  const handleBulkIssue = async () => {
    if (selectedRows.length === 0) return;
    setBulkIssuing(true);
    let successCount = 0;
    let failedCount = 0;
    for (const id of selectedRows) {
      try {
        await issueForm16.mutateAsync(id);
        successCount++;
      } catch (error) {
        console.error(`Failed to issue Form 16 ${id}:`, error);
        failedCount++;
      }
    }
    setBulkIssuing(false);
    setSelectedRows([]);
    setIssueModalOpen(false);
  };

  // Handle regenerate
  const handleRegenerate = async (id: string) => {
    try {
      await regenerateForm16.mutateAsync(id);
    } catch (error) {
      console.error('Failed to regenerate Form 16:', error);
    }
  };

  // Status badge
  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { label: string; className: string }> = {
      draft: { label: 'Draft', className: 'bg-gray-100 text-gray-800' },
      generated: { label: 'Generated', className: 'bg-blue-100 text-blue-800' },
      issued: { label: 'Issued', className: 'bg-green-100 text-green-800' },
      revised: { label: 'Revised', className: 'bg-yellow-100 text-yellow-800' },
    };
    const config = statusConfig[status] || statusConfig.draft;
    return (
      <span className={`px-2 py-1 rounded text-xs font-medium ${config.className}`}>
        {config.label}
      </span>
    );
  };

  // Table columns
  const columns: ColumnDef<Form16Data>[] = [
    {
      id: 'select',
      header: ({ table }) => (
        <input
          type="checkbox"
          checked={table.getIsAllRowsSelected()}
          onChange={table.getToggleAllRowsSelectedHandler()}
          className="rounded border-gray-300"
        />
      ),
      cell: ({ row }) => (
        <input
          type="checkbox"
          checked={row.getIsSelected()}
          onChange={row.getToggleSelectedHandler()}
          disabled={row.original.status === 'issued'}
          className="rounded border-gray-300"
        />
      ),
    },
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => (
        <div>
          <div className="font-medium text-gray-900">{row.original.employeeName}</div>
          <div className="text-sm text-gray-500">PAN: {row.original.employeePan}</div>
        </div>
      ),
    },
    {
      accessorKey: 'financialYear',
      header: 'Financial Year',
      cell: ({ row }) => (
        <div>
          <div className="font-medium">FY {row.original.financialYear}</div>
          <div className="text-sm text-gray-500">AY {row.original.assessmentYear}</div>
        </div>
      ),
    },
    {
      accessorKey: 'grossSalary',
      header: 'Gross Salary',
      cell: ({ row }) => (
        <div className="text-right font-medium">{formatINR(row.original.grossSalary)}</div>
      ),
    },
    {
      accessorKey: 'totalTdsDeducted',
      header: 'TDS Deducted',
      cell: ({ row }) => (
        <div className="text-right">
          <div className="font-medium">{formatINR(row.original.totalTdsDeducted)}</div>
          <div className="text-sm text-gray-500">
            Q1: {formatINR(row.original.q1TdsDeducted)} | Q2: {formatINR(row.original.q2TdsDeducted)}
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status),
    },
    {
      accessorKey: 'generatedAt',
      header: 'Generated',
      cell: ({ row }) => (
        <div className="text-sm text-gray-500">
          {row.original.generatedAt
            ? format(new Date(row.original.generatedAt), 'dd MMM yyyy')
            : '—'}
        </div>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const form16 = row.original;
        return (
          <div className="flex items-center gap-2">
            <button
              onClick={() => {
                setSelectedForm16(form16);
                setPreviewModalOpen(true);
              }}
              className="p-1 text-gray-600 hover:text-gray-800 hover:bg-gray-100 rounded"
              title="View"
            >
              <Eye className="w-4 h-4" />
            </button>
            {form16.status === 'generated' && (
              <button
                onClick={() => handleIssue(form16.id)}
                className="p-1 text-green-600 hover:text-green-800 hover:bg-green-50 rounded"
                title="Issue"
                disabled={issueForm16.isPending}
              >
                <Send className="w-4 h-4" />
              </button>
            )}
            {form16.status !== 'issued' && (
              <button
                onClick={() => handleRegenerate(form16.id)}
                className="p-1 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded"
                title="Regenerate"
                disabled={regenerateForm16.isPending}
              >
                <RefreshCw className="w-4 h-4" />
              </button>
            )}
            <a
              href={`/api/tax/form16/${form16.id}/download`}
              className="p-1 text-purple-600 hover:text-purple-800 hover:bg-purple-50 rounded"
              title="Download PDF"
              download
            >
              <Download className="w-4 h-4" />
            </a>
          </div>
        );
      },
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Form 16 Management</h1>
          <p className="text-gray-500 mt-1">
            Generate and issue TDS certificates (Form 16) for employees
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
          <Button onClick={() => setGenerateModalOpen(true)} className="flex items-center gap-2">
            <FileText className="w-4 h-4" />
            Generate Form 16
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Total Employees</p>
            <p className="text-2xl font-bold text-gray-900">{summary.totalEmployees}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Generated</p>
            <p className="text-2xl font-bold text-blue-600">{summary.generated}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Issued</p>
            <p className="text-2xl font-bold text-green-600">{summary.issued}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Pending</p>
            <p className="text-2xl font-bold text-yellow-600">{summary.pending}</p>
          </div>
          <div className="bg-white border rounded-lg p-4">
            <p className="text-sm text-gray-500">Total TDS</p>
            <p className="text-xl font-bold text-gray-900">{formatINR(summary.totalTdsDeducted)}</p>
          </div>
        </div>
      )}

      {/* Status Filter */}
      <div className="flex items-center gap-2">
        <span className="text-sm text-gray-500">Filter:</span>
        {['all', 'draft', 'generated', 'issued'].map((status) => (
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

      {/* Bulk Actions */}
      {selectedRows.length > 0 && (
        <div className="flex items-center gap-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
          <span className="text-blue-800 font-medium">
            {selectedRows.length} items selected
          </span>
          <Button
            onClick={() => setIssueModalOpen(true)}
            variant="outline"
            size="sm"
            className="flex items-center gap-2"
          >
            <Send className="w-4 h-4" />
            Bulk Issue
          </Button>
          <Button
            onClick={() => setSelectedRows([])}
            variant="ghost"
            size="sm"
          >
            Clear Selection
          </Button>
        </div>
      )}

      {/* Data Table */}
      {error ? (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2 text-red-600">
            <AlertTriangle className="w-5 h-5" />
            <p>Failed to load Form 16 data. Please try again.</p>
          </div>
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={filteredList}
          isLoading={isLoading}
          pagination={{
            pageIndex: 0,
            pageSize: 100,
            pageCount: 1,
            onPageChange: () => {},
          }}
          onRowSelectionChange={(rows) => {
            const selectedIds = Object.keys(rows)
              .filter((key) => rows[key])
              .map((index) => filteredList[parseInt(index)]?.id)
              .filter(Boolean);
            setSelectedRows(selectedIds);
          }}
        />
      )}

      {/* Generate Modal */}
      <Modal
        isOpen={generateModalOpen}
        onClose={() => setGenerateModalOpen(false)}
        title="Generate Form 16"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            This will generate Form 16 for all employees who have salary transactions in FY{' '}
            {urlState.financialYear}.
          </p>
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
            <p className="text-sm text-yellow-800">
              <strong>Note:</strong> Form 16 should be issued to employees by June 15th following
              the financial year end.
            </p>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setGenerateModalOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleGenerate}
              disabled={generateForm16.isPending}
              className="flex items-center gap-2"
            >
              {generateForm16.isPending ? (
                <RefreshCw className="w-4 h-4 animate-spin" />
              ) : (
                <FileText className="w-4 h-4" />
              )}
              Generate
            </Button>
          </div>
        </div>
      </Modal>

      {/* Bulk Issue Modal */}
      <Modal
        isOpen={issueModalOpen}
        onClose={() => setIssueModalOpen(false)}
        title="Bulk Issue Form 16"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Are you sure you want to issue {selectedRows.length} Form 16 certificates?
          </p>
          <p className="text-sm text-gray-500">
            Once issued, employees will be able to download their Form 16 from the employee portal.
          </p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setIssueModalOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleBulkIssue}
              disabled={bulkIssuing}
              className="flex items-center gap-2"
            >
              {bulkIssuing ? (
                <RefreshCw className="w-4 h-4 animate-spin" />
              ) : (
                <Send className="w-4 h-4" />
              )}
              Issue {selectedRows.length} Forms
            </Button>
          </div>
        </div>
      </Modal>

      {/* Preview Modal */}
      <Modal
        isOpen={previewModalOpen}
        onClose={() => {
          setPreviewModalOpen(false);
          setSelectedForm16(null);
        }}
        title={`Form 16 - ${selectedForm16?.employeeName}`}
        size="lg"
      >
        {selectedForm16 && (
          <div className="space-y-6">
            {/* Part A - Summary */}
            <div>
              <h4 className="font-semibold text-gray-900 mb-3">Part A - TDS Summary</h4>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                <div>
                  <p className="text-gray-500">Q1 TDS</p>
                  <p className="font-medium">{formatINR(selectedForm16.q1TdsDeducted)}</p>
                </div>
                <div>
                  <p className="text-gray-500">Q2 TDS</p>
                  <p className="font-medium">{formatINR(selectedForm16.q2TdsDeducted)}</p>
                </div>
                <div>
                  <p className="text-gray-500">Q3 TDS</p>
                  <p className="font-medium">{formatINR(selectedForm16.q3TdsDeducted)}</p>
                </div>
                <div>
                  <p className="text-gray-500">Q4 TDS</p>
                  <p className="font-medium">{formatINR(selectedForm16.q4TdsDeducted)}</p>
                </div>
              </div>
              <div className="mt-3 pt-3 border-t">
                <div className="flex justify-between">
                  <span className="text-gray-500">Total TDS Deducted</span>
                  <span className="font-semibold">{formatINR(selectedForm16.totalTdsDeducted)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Total TDS Deposited</span>
                  <span className="font-semibold text-green-600">
                    {formatINR(selectedForm16.totalTdsDeposited)}
                  </span>
                </div>
              </div>
            </div>

            {/* Part B - Salary Details */}
            <div>
              <h4 className="font-semibold text-gray-900 mb-3">Part B - Salary & Tax Computation</h4>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-500">Gross Salary (Section 17(1))</span>
                  <span className="font-medium">{formatINR(selectedForm16.grossSalary)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Standard Deduction</span>
                  <span className="font-medium text-green-600">
                    - {formatINR(selectedForm16.exemptions?.standardDeduction || 0)}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Professional Tax</span>
                  <span className="font-medium text-green-600">
                    - {formatINR(selectedForm16.exemptions?.professionalTax || 0)}
                  </span>
                </div>
                <div className="flex justify-between border-t pt-2">
                  <span className="text-gray-700 font-medium">Net Taxable Income</span>
                  <span className="font-semibold">{formatINR(selectedForm16.netTaxableIncome)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Tax Payable</span>
                  <span className="font-medium">{formatINR(selectedForm16.taxPayable)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Rebate u/s 87A</span>
                  <span className="font-medium text-green-600">
                    - {formatINR(selectedForm16.rebate87a)}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Surcharge</span>
                  <span className="font-medium">{formatINR(selectedForm16.surcharge)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Education Cess (4%)</span>
                  <span className="font-medium">{formatINR(selectedForm16.educationCess)}</span>
                </div>
                <div className="flex justify-between border-t pt-2">
                  <span className="text-gray-900 font-semibold">Net Tax Payable</span>
                  <span className="font-bold text-blue-600">
                    {formatINR(selectedForm16.netTaxPayable)}
                  </span>
                </div>
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-4 border-t">
              <Button variant="outline" onClick={() => setPreviewModalOpen(false)}>
                Close
              </Button>
              <a
                href={`/api/tax/form16/${selectedForm16.id}/download`}
                className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                download
              >
                <Download className="w-4 h-4" />
                Download PDF
              </a>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default Form16Management;

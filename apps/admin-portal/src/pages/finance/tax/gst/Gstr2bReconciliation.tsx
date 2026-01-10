import { useState, useRef } from 'react';
import {
  useGstr2bImportByPeriod,
  useGstr2bReconciliationSummary,
  useGstr2bSupplierSummary,
  useGstr2bItcComparison,
  useGstr2bInvoices,
  useGstr2bInvoice,
  useImportGstr2b,
  useRunGstr2bReconciliation,
  useDeleteGstr2bImport,
  useAcceptGstr2bMismatch,
  useRejectGstr2bInvoice,
  useResetGstr2bAction,
} from '@/features/gst-compliance/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import type {
  Gstr2bInvoiceListItem,
  Gstr2bSupplierSummary as Gstr2bSupplierSummaryType,
  Gstr2bMatchStatus,
  Gstr2bActionStatus,
} from '@/services/api/types';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import {
  Upload,
  RefreshCw,
  Trash2,
  CheckCircle,
  XCircle,
  AlertCircle,
  Clock,
  Building2,
  Calendar,
  FileText,
  Search,
  Filter,
  Link2,
  ChevronLeft,
  ChevronRight as ChevronRightIcon,
  Check,
  X,
  RotateCcw,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import toast from 'react-hot-toast';

// Generate return period options
const generateReturnPeriods = () => {
  const periods = [];
  const now = new Date();
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  for (let i = 0; i < 12; i++) {
    const date = new Date(now.getFullYear(), now.getMonth() - i, 1);
    const month = months[date.getMonth()];
    const year = date.getFullYear();
    periods.push({
      value: `${month}-${year}`,
      label: `${month} ${year}`,
    });
  }
  return periods;
};

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    minimumFractionDigits: 2,
  }).format(amount);
};

const formatDate = (dateStr: string) => {
  const date = new Date(dateStr);
  return date.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
};

const getMatchStatusColor = (status: Gstr2bMatchStatus) => {
  switch (status) {
    case 'matched':
      return 'bg-green-100 text-green-800';
    case 'partial_match':
      return 'bg-yellow-100 text-yellow-800';
    case 'unmatched':
      return 'bg-red-100 text-red-800';
    case 'manual_match':
      return 'bg-blue-100 text-blue-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

const getMatchStatusIcon = (status: Gstr2bMatchStatus) => {
  switch (status) {
    case 'matched':
      return <CheckCircle className="h-4 w-4" />;
    case 'partial_match':
      return <AlertCircle className="h-4 w-4" />;
    case 'unmatched':
      return <XCircle className="h-4 w-4" />;
    case 'manual_match':
      return <Link2 className="h-4 w-4" />;
    default:
      return <Clock className="h-4 w-4" />;
  }
};

const getActionStatusBadge = (status?: Gstr2bActionStatus) => {
  switch (status) {
    case 'accepted':
      return <span className="px-2 py-1 text-xs rounded-full bg-green-100 text-green-800">Accepted</span>;
    case 'rejected':
      return <span className="px-2 py-1 text-xs rounded-full bg-red-100 text-red-800">Rejected</span>;
    default:
      return null;
  }
};

// Summary Card Component
const SummaryCard = ({
  title,
  value,
  subtitle,
  icon: Icon,
  color = 'blue',
}: {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: any;
  color?: 'blue' | 'green' | 'red' | 'yellow' | 'purple';
}) => {
  const colors = {
    blue: 'bg-blue-50 text-blue-600 border-blue-100',
    green: 'bg-green-50 text-green-600 border-green-100',
    red: 'bg-red-50 text-red-600 border-red-100',
    yellow: 'bg-yellow-50 text-yellow-600 border-yellow-100',
    purple: 'bg-purple-50 text-purple-600 border-purple-100',
  };

  return (
    <div className={cn('border rounded-lg p-4', colors[color])}>
      <div className="flex items-center gap-2 mb-2">
        <Icon className="h-5 w-5" />
        <span className="text-sm font-medium">{title}</span>
      </div>
      <div className="text-2xl font-bold">{value}</div>
      {subtitle && <div className="text-sm mt-1 opacity-80">{subtitle}</div>}
    </div>
  );
};

// Invoice Row Component
const InvoiceRow = ({
  invoice,
  onSelect,
}: {
  invoice: Gstr2bInvoiceListItem;
  onSelect: (id: string) => void;
}) => {
  return (
    <tr
      className="hover:bg-gray-50 cursor-pointer"
      onClick={() => onSelect(invoice.id)}
    >
      <td className="px-4 py-3 text-sm text-gray-900">{invoice.supplierGstin}</td>
      <td className="px-4 py-3 text-sm text-gray-500">{invoice.supplierName || '-'}</td>
      <td className="px-4 py-3 text-sm text-gray-900">{invoice.invoiceNumber}</td>
      <td className="px-4 py-3 text-sm text-gray-500">{formatDate(invoice.invoiceDate)}</td>
      <td className="px-4 py-3 text-sm text-right">{formatCurrency(invoice.taxableValue)}</td>
      <td className="px-4 py-3 text-sm text-right">{formatCurrency(invoice.totalItc)}</td>
      <td className="px-4 py-3">
        <div className="flex items-center gap-2">
          <span className={cn('inline-flex items-center gap-1 px-2 py-1 text-xs rounded-full', getMatchStatusColor(invoice.matchStatus))}>
            {getMatchStatusIcon(invoice.matchStatus)}
            {invoice.matchStatus.replace('_', ' ')}
          </span>
          {invoice.matchConfidence && (
            <span className="text-xs text-gray-500">({invoice.matchConfidence}%)</span>
          )}
        </div>
      </td>
      <td className="px-4 py-3">{getActionStatusBadge(invoice.actionStatus)}</td>
    </tr>
  );
};

// Supplier Summary Row
const SupplierRow = ({ supplier }: { supplier: Gstr2bSupplierSummaryType }) => {
  return (
    <tr className="hover:bg-gray-50">
      <td className="px-4 py-3 text-sm text-gray-900">{supplier.supplierGstin}</td>
      <td className="px-4 py-3 text-sm text-gray-500">{supplier.supplierName || '-'}</td>
      <td className="px-4 py-3 text-sm text-center">{supplier.invoiceCount}</td>
      <td className="px-4 py-3 text-sm text-center text-green-600">{supplier.matchedCount}</td>
      <td className="px-4 py-3 text-sm text-center text-red-600">{supplier.unmatchedCount}</td>
      <td className="px-4 py-3 text-sm text-right">{formatCurrency(supplier.totalTaxableValue)}</td>
      <td className="px-4 py-3 text-sm text-right">{formatCurrency(supplier.totalItc)}</td>
      <td className="px-4 py-3">
        <div className="flex items-center gap-2">
          <div className="w-20 h-2 bg-gray-200 rounded-full overflow-hidden">
            <div
              className="h-full bg-green-500 transition-all"
              style={{ width: `${supplier.matchPercentage}%` }}
            />
          </div>
          <span className="text-xs text-gray-600">{supplier.matchPercentage}%</span>
        </div>
      </td>
    </tr>
  );
};

const Gstr2bReconciliation = () => {
  const returnPeriods = generateReturnPeriods();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedReturnPeriod, setSelectedReturnPeriod] = useState(returnPeriods[0].value);
  const [activeTab, setActiveTab] = useState<'invoices' | 'suppliers' | 'itc'>('invoices');

  // Filter state
  const [matchStatusFilter, setMatchStatusFilter] = useState<string>('');
  const [searchTerm, setSearchTerm] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 50;

  // Import modal
  const [showImportModal, setShowImportModal] = useState(false);
  const [importFileName, setImportFileName] = useState('');
  const [importJsonData, setImportJsonData] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Invoice detail drawer
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null);
  const [showInvoiceDrawer, setShowInvoiceDrawer] = useState(false);

  // Reject modal
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [rejectReason, setRejectReason] = useState('');

  const { data: companies = [] } = useCompanies();

  // Get current import
  const { data: currentImport, isLoading: importLoading } = useGstr2bImportByPeriod(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId
  );

  // Reconciliation summary
  const { data: reconciliationSummary } = useGstr2bReconciliationSummary(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId && !!currentImport
  );

  // Supplier summary
  const { data: supplierSummary } = useGstr2bSupplierSummary(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId && activeTab === 'suppliers'
  );

  // ITC comparison
  const { data: itcComparison } = useGstr2bItcComparison(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId && activeTab === 'itc'
  );

  // Invoices
  const { data: invoicesResult } = useGstr2bInvoices(
    currentImport?.id || '',
    {
      pageNumber,
      pageSize,
      matchStatus: matchStatusFilter || undefined,
      searchTerm: searchTerm || undefined,
    },
    !!currentImport?.id && activeTab === 'invoices'
  );

  // Selected invoice
  const { data: selectedInvoice } = useGstr2bInvoice(
    selectedInvoiceId || '',
    !!selectedInvoiceId
  );

  // Mutations
  const importMutation = useImportGstr2b();
  const reconcileMutation = useRunGstr2bReconciliation();
  const deleteMutation = useDeleteGstr2bImport();
  const acceptMutation = useAcceptGstr2bMismatch();
  const rejectMutation = useRejectGstr2bInvoice();
  // const manualMatchMutation = useManualMatchGstr2b();
  const resetMutation = useResetGstr2bAction();

  // Handlers
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setImportFileName(file.name);
      const reader = new FileReader();
      reader.onload = (event) => {
        const content = event.target?.result as string;
        setImportJsonData(content);
      };
      reader.readAsText(file);
    }
  };

  const handleImport = async () => {
    if (!importJsonData || !selectedCompanyId) {
      toast.error('Please select a file to import');
      return;
    }

    await importMutation.mutateAsync({
      companyId: selectedCompanyId,
      returnPeriod: selectedReturnPeriod,
      jsonData: importJsonData,
      fileName: importFileName,
    });

    setShowImportModal(false);
    setImportJsonData('');
    setImportFileName('');
  };

  const handleReconcile = async () => {
    if (!currentImport) return;
    await reconcileMutation.mutateAsync({
      importId: currentImport.id,
      force: true,
    });
  };

  const handleDelete = async () => {
    if (!currentImport) return;
    if (confirm('Are you sure you want to delete this import and all its invoices?')) {
      await deleteMutation.mutateAsync(currentImport.id);
    }
  };

  const handleAccept = async () => {
    if (!selectedInvoiceId) return;
    await acceptMutation.mutateAsync({ invoiceId: selectedInvoiceId });
    setShowInvoiceDrawer(false);
    setSelectedInvoiceId(null);
  };

  const handleReject = async () => {
    if (!selectedInvoiceId || !rejectReason) {
      toast.error('Please provide a reason for rejection');
      return;
    }
    await rejectMutation.mutateAsync({ invoiceId: selectedInvoiceId, reason: rejectReason });
    setShowRejectModal(false);
    setRejectReason('');
    setShowInvoiceDrawer(false);
    setSelectedInvoiceId(null);
  };

  const handleReset = async () => {
    if (!selectedInvoiceId) return;
    await resetMutation.mutateAsync(selectedInvoiceId);
    setShowInvoiceDrawer(false);
    setSelectedInvoiceId(null);
  };

  const handleInvoiceSelect = (id: string) => {
    setSelectedInvoiceId(id);
    setShowInvoiceDrawer(true);
  };

  const totalPages = invoicesResult?.totalCount ? Math.ceil(invoicesResult.totalCount / pageSize) : 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">GSTR-2B Reconciliation</h1>
          <p className="text-gray-500 mt-1">Import and reconcile GSTR-2B with vendor invoices</p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-sm border p-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
            <div className="relative">
              <Building2 className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
              <select
                value={selectedCompanyId}
                onChange={(e) => setSelectedCompanyId(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="">Select Company</option>
                {companies.map((company) => (
                  <option key={company.id} value={company.id}>
                    {company.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Return Period</label>
            <div className="relative">
              <Calendar className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
              <select
                value={selectedReturnPeriod}
                onChange={(e) => setSelectedReturnPeriod(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              >
                {returnPeriods.map((period) => (
                  <option key={period.value} value={period.value}>
                    {period.label}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="flex items-end gap-2">
            <button
              onClick={() => setShowImportModal(true)}
              disabled={!selectedCompanyId || importMutation.isPending}
              className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
            >
              <Upload className="h-4 w-4" />
              Import GSTR-2B
            </button>
          </div>
          {currentImport && (
            <div className="flex items-end gap-2">
              <button
                onClick={handleReconcile}
                disabled={reconcileMutation.isPending}
                className="inline-flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                <RefreshCw className={cn('h-4 w-4', reconcileMutation.isPending && 'animate-spin')} />
                Reconcile
              </button>
              <button
                onClick={handleDelete}
                disabled={deleteMutation.isPending}
                className="inline-flex items-center gap-2 px-4 py-2 border border-red-300 text-red-600 rounded-lg hover:bg-red-50"
              >
                <Trash2 className="h-4 w-4" />
                Delete
              </button>
            </div>
          )}
        </div>
      </div>

      {/* No company selected */}
      {!selectedCompanyId && (
        <div className="bg-white rounded-lg shadow-sm border p-8 text-center">
          <Building2 className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900">Select a Company</h3>
          <p className="text-gray-500 mt-1">Choose a company to view GSTR-2B reconciliation</p>
        </div>
      )}

      {/* No import for period */}
      {selectedCompanyId && !currentImport && !importLoading && (
        <div className="bg-white rounded-lg shadow-sm border p-8 text-center">
          <FileText className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900">No GSTR-2B Import</h3>
          <p className="text-gray-500 mt-1">
            No GSTR-2B data imported for {selectedReturnPeriod}. Import a JSON file to get started.
          </p>
          <button
            onClick={() => setShowImportModal(true)}
            className="mt-4 inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            <Upload className="h-4 w-4" />
            Import GSTR-2B
          </button>
        </div>
      )}

      {/* Import exists - show reconciliation */}
      {currentImport && (
        <>
          {/* Summary Cards */}
          {reconciliationSummary && (
            <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
              <SummaryCard
                title="Total Invoices"
                value={reconciliationSummary.totalInvoices}
                icon={FileText}
                color="blue"
              />
              <SummaryCard
                title="Matched"
                value={reconciliationSummary.matchedInvoices}
                subtitle={`${reconciliationSummary.matchPercentage}%`}
                icon={CheckCircle}
                color="green"
              />
              <SummaryCard
                title="Partial Match"
                value={reconciliationSummary.partialMatchInvoices}
                icon={AlertCircle}
                color="yellow"
              />
              <SummaryCard
                title="Unmatched"
                value={reconciliationSummary.unmatchedInvoices}
                icon={XCircle}
                color="red"
              />
              <SummaryCard
                title="Total ITC"
                value={formatCurrency(reconciliationSummary.totalItcAvailable)}
                subtitle={`Matched: ${formatCurrency(reconciliationSummary.matchedItc)}`}
                icon={FileText}
                color="purple"
              />
            </div>
          )}

          {/* Tabs */}
          <div className="bg-white rounded-lg shadow-sm border">
            <div className="border-b">
              <div className="flex space-x-4 px-4">
                <button
                  onClick={() => setActiveTab('invoices')}
                  className={cn(
                    'px-4 py-3 text-sm font-medium border-b-2 -mb-px',
                    activeTab === 'invoices'
                      ? 'border-blue-600 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700'
                  )}
                >
                  Invoices
                </button>
                <button
                  onClick={() => setActiveTab('suppliers')}
                  className={cn(
                    'px-4 py-3 text-sm font-medium border-b-2 -mb-px',
                    activeTab === 'suppliers'
                      ? 'border-blue-600 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700'
                  )}
                >
                  Supplier Summary
                </button>
                <button
                  onClick={() => setActiveTab('itc')}
                  className={cn(
                    'px-4 py-3 text-sm font-medium border-b-2 -mb-px',
                    activeTab === 'itc'
                      ? 'border-blue-600 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700'
                  )}
                >
                  ITC Comparison
                </button>
              </div>
            </div>

            {/* Invoices Tab */}
            {activeTab === 'invoices' && (
              <div className="p-4">
                {/* Filters */}
                <div className="flex items-center gap-4 mb-4">
                  <div className="flex-1 relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                    <input
                      type="text"
                      placeholder="Search by GSTIN or invoice number..."
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="w-full pl-10 pr-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
                    />
                  </div>
                  <div className="relative">
                    <Filter className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                    <select
                      value={matchStatusFilter}
                      onChange={(e) => setMatchStatusFilter(e.target.value)}
                      className="pl-10 pr-8 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
                    >
                      <option value="">All Statuses</option>
                      <option value="matched">Matched</option>
                      <option value="partial_match">Partial Match</option>
                      <option value="unmatched">Unmatched</option>
                      <option value="manual_match">Manual Match</option>
                    </select>
                  </div>
                </div>

                {/* Table */}
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">GSTIN</th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Supplier</th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Invoice No</th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Taxable</th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Total ITC</th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Match Status</th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Action</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {invoicesResult?.items.map((invoice) => (
                        <InvoiceRow key={invoice.id} invoice={invoice} onSelect={handleInvoiceSelect} />
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                  <div className="flex items-center justify-between mt-4 pt-4 border-t">
                    <div className="text-sm text-gray-500">
                      Showing {((pageNumber - 1) * pageSize) + 1} to {Math.min(pageNumber * pageSize, invoicesResult?.totalCount || 0)} of {invoicesResult?.totalCount || 0} invoices
                    </div>
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                        disabled={pageNumber === 1}
                        className="p-2 border rounded-lg hover:bg-gray-50 disabled:opacity-50"
                      >
                        <ChevronLeft className="h-4 w-4" />
                      </button>
                      <span className="text-sm text-gray-600">
                        Page {pageNumber} of {totalPages}
                      </span>
                      <button
                        onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
                        disabled={pageNumber === totalPages}
                        className="p-2 border rounded-lg hover:bg-gray-50 disabled:opacity-50"
                      >
                        <ChevronRightIcon className="h-4 w-4" />
                      </button>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* Suppliers Tab */}
            {activeTab === 'suppliers' && supplierSummary && (
              <div className="p-4">
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">GSTIN</th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Supplier Name</th>
                        <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Invoices</th>
                        <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Matched</th>
                        <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Unmatched</th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Taxable Value</th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Total ITC</th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Match %</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {supplierSummary.map((supplier) => (
                        <SupplierRow key={supplier.supplierGstin} supplier={supplier} />
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {/* ITC Comparison Tab */}
            {activeTab === 'itc' && itcComparison && (
              <div className="p-4">
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Component</th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">As per GSTR-2B</th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">As per Books</th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Difference</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      <tr>
                        <td className="px-4 py-3 text-sm font-medium text-gray-900">IGST</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.gstr2b.igst)}</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.books.igst)}</td>
                        <td className={cn('px-4 py-3 text-sm text-right font-medium', itcComparison.difference.igst !== 0 ? 'text-red-600' : 'text-green-600')}>
                          {formatCurrency(itcComparison.difference.igst)}
                        </td>
                      </tr>
                      <tr>
                        <td className="px-4 py-3 text-sm font-medium text-gray-900">CGST</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.gstr2b.cgst)}</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.books.cgst)}</td>
                        <td className={cn('px-4 py-3 text-sm text-right font-medium', itcComparison.difference.cgst !== 0 ? 'text-red-600' : 'text-green-600')}>
                          {formatCurrency(itcComparison.difference.cgst)}
                        </td>
                      </tr>
                      <tr>
                        <td className="px-4 py-3 text-sm font-medium text-gray-900">SGST</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.gstr2b.sgst)}</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.books.sgst)}</td>
                        <td className={cn('px-4 py-3 text-sm text-right font-medium', itcComparison.difference.sgst !== 0 ? 'text-red-600' : 'text-green-600')}>
                          {formatCurrency(itcComparison.difference.sgst)}
                        </td>
                      </tr>
                      <tr>
                        <td className="px-4 py-3 text-sm font-medium text-gray-900">Cess</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.gstr2b.cess)}</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.books.cess)}</td>
                        <td className={cn('px-4 py-3 text-sm text-right font-medium', itcComparison.difference.cess !== 0 ? 'text-red-600' : 'text-green-600')}>
                          {formatCurrency(itcComparison.difference.cess)}
                        </td>
                      </tr>
                      <tr className="bg-gray-50 font-semibold">
                        <td className="px-4 py-3 text-sm text-gray-900">Total</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.gstr2b.total)}</td>
                        <td className="px-4 py-3 text-sm text-right">{formatCurrency(itcComparison.books.total)}</td>
                        <td className={cn('px-4 py-3 text-sm text-right', itcComparison.difference.total !== 0 ? 'text-red-600' : 'text-green-600')}>
                          {formatCurrency(itcComparison.difference.total)}
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        </>
      )}

      {/* Import Modal */}
      <Modal
        isOpen={showImportModal}
        onClose={() => setShowImportModal(false)}
        title="Import GSTR-2B JSON"
      >
        <div className="space-y-4 p-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select GSTR-2B JSON File
            </label>
            <input
              ref={fileInputRef}
              type="file"
              accept=".json"
              onChange={handleFileChange}
              className="hidden"
            />
            <div
              onClick={() => fileInputRef.current?.click()}
              className="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center cursor-pointer hover:border-blue-500 transition-colors"
            >
              <Upload className="h-8 w-8 text-gray-400 mx-auto mb-2" />
              {importFileName ? (
                <p className="text-sm text-gray-900">{importFileName}</p>
              ) : (
                <>
                  <p className="text-sm text-gray-600">Click to select a JSON file</p>
                  <p className="text-xs text-gray-500 mt-1">Download from GSTN portal</p>
                </>
              )}
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <button
              onClick={() => setShowImportModal(false)}
              className="px-4 py-2 border rounded-lg hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              onClick={handleImport}
              disabled={!importJsonData || importMutation.isPending}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
            >
              {importMutation.isPending ? 'Importing...' : 'Import'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Invoice Detail Drawer */}
      <Drawer
        isOpen={showInvoiceDrawer}
        onClose={() => {
          setShowInvoiceDrawer(false);
          setSelectedInvoiceId(null);
        }}
        title="Invoice Details"
      >
        {selectedInvoice && (
          <div className="p-4 space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
              <span className={cn('inline-flex items-center gap-1 px-3 py-1 text-sm rounded-full', getMatchStatusColor(selectedInvoice.matchStatus))}>
                {getMatchStatusIcon(selectedInvoice.matchStatus)}
                {selectedInvoice.matchStatus.replace('_', ' ')}
              </span>
              {selectedInvoice.actionStatus && getActionStatusBadge(selectedInvoice.actionStatus)}
            </div>

            {/* Supplier Details */}
            <div>
              <h4 className="text-sm font-medium text-gray-700 mb-2">Supplier</h4>
              <div className="bg-gray-50 rounded-lg p-3 space-y-1">
                <p className="text-sm font-medium">{selectedInvoice.supplierName || 'Unknown'}</p>
                <p className="text-sm text-gray-600">GSTIN: {selectedInvoice.supplierGstin}</p>
                {selectedInvoice.supplierTradeName && (
                  <p className="text-sm text-gray-500">Trade: {selectedInvoice.supplierTradeName}</p>
                )}
              </div>
            </div>

            {/* Invoice Details */}
            <div>
              <h4 className="text-sm font-medium text-gray-700 mb-2">Invoice</h4>
              <div className="bg-gray-50 rounded-lg p-3 space-y-1">
                <p className="text-sm"><span className="text-gray-500">Number:</span> {selectedInvoice.invoiceNumber}</p>
                <p className="text-sm"><span className="text-gray-500">Date:</span> {formatDate(selectedInvoice.invoiceDate)}</p>
                <p className="text-sm"><span className="text-gray-500">Type:</span> {selectedInvoice.invoiceType || 'Regular'}</p>
                <p className="text-sm"><span className="text-gray-500">Place of Supply:</span> {selectedInvoice.placeOfSupply || '-'}</p>
                <p className="text-sm"><span className="text-gray-500">Reverse Charge:</span> {selectedInvoice.reverseCharge ? 'Yes' : 'No'}</p>
              </div>
            </div>

            {/* Amounts */}
            <div>
              <h4 className="text-sm font-medium text-gray-700 mb-2">Amounts</h4>
              <div className="bg-gray-50 rounded-lg p-3 space-y-1">
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">Taxable Value</span>
                  <span className="text-sm font-medium">{formatCurrency(selectedInvoice.taxableValue)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">IGST</span>
                  <span className="text-sm">{formatCurrency(selectedInvoice.igstAmount)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">CGST</span>
                  <span className="text-sm">{formatCurrency(selectedInvoice.cgstAmount)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">SGST</span>
                  <span className="text-sm">{formatCurrency(selectedInvoice.sgstAmount)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">Cess</span>
                  <span className="text-sm">{formatCurrency(selectedInvoice.cessAmount)}</span>
                </div>
                <div className="flex justify-between pt-2 border-t mt-2">
                  <span className="text-sm font-medium">Total Invoice Value</span>
                  <span className="text-sm font-bold">{formatCurrency(selectedInvoice.totalInvoiceValue)}</span>
                </div>
              </div>
            </div>

            {/* ITC */}
            <div>
              <h4 className="text-sm font-medium text-gray-700 mb-2">ITC Available</h4>
              <div className={cn('rounded-lg p-3 space-y-1', selectedInvoice.itcEligible ? 'bg-green-50' : 'bg-red-50')}>
                <p className="text-sm font-medium">{selectedInvoice.itcEligible ? 'Eligible for ITC' : 'Not Eligible for ITC'}</p>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">IGST ITC</span>
                  <span className="text-sm">{formatCurrency(selectedInvoice.itcIgst)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">CGST ITC</span>
                  <span className="text-sm">{formatCurrency(selectedInvoice.itcCgst)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">SGST ITC</span>
                  <span className="text-sm">{formatCurrency(selectedInvoice.itcSgst)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">Cess ITC</span>
                  <span className="text-sm">{formatCurrency(selectedInvoice.itcCess)}</span>
                </div>
              </div>
            </div>

            {/* Matched Invoice */}
            {selectedInvoice.matchedVendorInvoiceId && (
              <div>
                <h4 className="text-sm font-medium text-gray-700 mb-2">Matched Vendor Invoice</h4>
                <div className="bg-blue-50 rounded-lg p-3 space-y-1">
                  <p className="text-sm font-medium">{selectedInvoice.matchedVendorInvoiceNumber}</p>
                  {selectedInvoice.matchConfidence && (
                    <p className="text-sm text-gray-600">Confidence: {selectedInvoice.matchConfidence}%</p>
                  )}
                </div>
              </div>
            )}

            {/* Discrepancies */}
            {selectedInvoice.discrepancies && selectedInvoice.discrepancies.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-gray-700 mb-2">Discrepancies</h4>
                <div className="bg-yellow-50 rounded-lg p-3">
                  <ul className="list-disc list-inside space-y-1">
                    {selectedInvoice.discrepancies.map((disc, idx) => (
                      <li key={idx} className="text-sm text-yellow-800">{disc}</li>
                    ))}
                  </ul>
                </div>
              </div>
            )}

            {/* Action Buttons */}
            {!selectedInvoice.actionStatus && (
              <div className="flex gap-2 pt-4 border-t">
                <button
                  onClick={handleAccept}
                  disabled={acceptMutation.isPending}
                  className="flex-1 inline-flex items-center justify-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
                >
                  <Check className="h-4 w-4" />
                  Accept
                </button>
                <button
                  onClick={() => setShowRejectModal(true)}
                  disabled={rejectMutation.isPending}
                  className="flex-1 inline-flex items-center justify-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50"
                >
                  <X className="h-4 w-4" />
                  Reject
                </button>
              </div>
            )}

            {selectedInvoice.actionStatus && (
              <div className="pt-4 border-t">
                <button
                  onClick={handleReset}
                  disabled={resetMutation.isPending}
                  className="w-full inline-flex items-center justify-center gap-2 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  <RotateCcw className="h-4 w-4" />
                  Reset Action
                </button>
              </div>
            )}
          </div>
        )}
      </Drawer>

      {/* Reject Modal */}
      <Modal
        isOpen={showRejectModal}
        onClose={() => {
          setShowRejectModal(false);
          setRejectReason('');
        }}
        title="Reject Invoice"
      >
        <div className="p-4 space-y-4">
          <p className="text-sm text-gray-600">
            Please provide a reason for rejecting this invoice. This invoice will be marked as not eligible for ITC.
          </p>
          <textarea
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            placeholder="Enter rejection reason..."
            rows={3}
            className="w-full border rounded-lg p-3 focus:ring-2 focus:ring-blue-500"
          />
          <div className="flex justify-end gap-2">
            <button
              onClick={() => {
                setShowRejectModal(false);
                setRejectReason('');
              }}
              className="px-4 py-2 border rounded-lg hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              onClick={handleReject}
              disabled={!rejectReason || rejectMutation.isPending}
              className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50"
            >
              {rejectMutation.isPending ? 'Rejecting...' : 'Reject'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default Gstr2bReconciliation;

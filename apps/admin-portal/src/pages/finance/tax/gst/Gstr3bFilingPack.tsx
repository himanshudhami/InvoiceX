import { useState, useMemo } from 'react';
import {
  useGstr3bByPeriod,
  useGstr3bTable31,
  useGstr3bTable4,
  useGstr3bTable5,
  useGstr3bLineItems,
  useGstr3bSourceDocuments,
  useGstr3bVariance,
  useGstr3bHistory,
  useGenerateGstr3b,
  useReviewGstr3b,
  useFileGstr3b,
  useExportGstr3bJson,
} from '@/features/gst-compliance/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import type {
  Gstr3bFiling,
  Gstr3bRow,
  Gstr3bItcRow,
  Gstr3bLineItem,
  Gstr3bSourceDocument,
} from '@/services/api/types';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import {
  RefreshCw,
  FileCheck,
  Send,
  Download,
  ChevronDown,
  ChevronRight,
  Building2,
  Calendar,
  FileText,
  AlertCircle,
  CheckCircle,
  Clock,
  TrendingUp,
  TrendingDown,
  ArrowRight,
  Receipt,
  Banknote,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// Generate return period options in MMM-YYYY format
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

const getStatusColor = (status: string) => {
  switch (status) {
    case 'filed':
      return 'bg-green-100 text-green-800';
    case 'reviewed':
      return 'bg-blue-100 text-blue-800';
    case 'generated':
      return 'bg-yellow-100 text-yellow-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

const getStatusIcon = (status: string) => {
  switch (status) {
    case 'filed':
      return <CheckCircle className="h-4 w-4" />;
    case 'reviewed':
      return <FileCheck className="h-4 w-4" />;
    case 'generated':
      return <Clock className="h-4 w-4" />;
    default:
      return <AlertCircle className="h-4 w-4" />;
  }
};

// Table Row Component
const GstRow = ({
  label,
  row,
  showTaxable = true,
  onDrilldown,
}: {
  label: string;
  row: Gstr3bRow | Gstr3bItcRow;
  showTaxable?: boolean;
  onDrilldown?: () => void;
}) => {
  const taxableValue = 'taxableValue' in row ? row.taxableValue : 0;
  return (
    <tr className="hover:bg-gray-50">
      <td className="px-4 py-3 text-sm text-gray-900">
        <div className="flex items-center gap-2">
          {label}
          {onDrilldown && (row.sourceCount ?? 0) > 0 && (
            <button
              onClick={onDrilldown}
              className="text-blue-600 hover:text-blue-800 text-xs"
              title={`View ${row.sourceCount} source documents`}
            >
              ({row.sourceCount})
            </button>
          )}
        </div>
      </td>
      {showTaxable && (
        <td className="px-4 py-3 text-sm text-right">{formatCurrency(taxableValue)}</td>
      )}
      <td className="px-4 py-3 text-sm text-right">{formatCurrency(row.igst)}</td>
      <td className="px-4 py-3 text-sm text-right">{formatCurrency(row.cgst)}</td>
      <td className="px-4 py-3 text-sm text-right">{formatCurrency(row.sgst)}</td>
      <td className="px-4 py-3 text-sm text-right">{formatCurrency(row.cess)}</td>
    </tr>
  );
};

const Gstr3bFilingPack = () => {
  const returnPeriods = generateReturnPeriods();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedReturnPeriod, setSelectedReturnPeriod] = useState(returnPeriods[0].value);
  const [expandedTables, setExpandedTables] = useState<Record<string, boolean>>({
    '3.1': true,
    '4': true,
    '5': true,
  });

  // Drill-down state
  const [selectedLineItem, setSelectedLineItem] = useState<Gstr3bLineItem | null>(null);
  const [showSourceDocs, setShowSourceDocs] = useState(false);

  // Workflow modals
  const [showReviewModal, setShowReviewModal] = useState(false);
  const [showFileModal, setShowFileModal] = useState(false);
  const [reviewNotes, setReviewNotes] = useState('');
  const [fileDetails, setFileDetails] = useState({
    arn: '',
    filingDate: new Date().toISOString().split('T')[0],
  });

  const { data: companies = [] } = useCompanies();

  // Queries
  const { data: filing, isLoading: filingLoading, error: filingError } = useGstr3bByPeriod(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId
  );

  const { data: table31, isLoading: table31Loading } = useGstr3bTable31(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId && !filing
  );

  const { data: table4, isLoading: table4Loading } = useGstr3bTable4(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId && !filing
  );

  const { data: table5, isLoading: table5Loading } = useGstr3bTable5(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId && !filing
  );

  const { data: variance } = useGstr3bVariance(
    selectedCompanyId,
    selectedReturnPeriod,
    !!selectedCompanyId
  );

  const { data: lineItems } = useGstr3bLineItems(
    filing?.id || '',
    undefined,
    !!filing?.id
  );

  const { data: sourceDocs } = useGstr3bSourceDocuments(
    selectedLineItem?.id || '',
    !!selectedLineItem?.id && showSourceDocs
  );

  const { data: history } = useGstr3bHistory(
    selectedCompanyId,
    { pageNumber: 1, pageSize: 12 },
    !!selectedCompanyId
  );

  // Mutations
  const generateMutation = useGenerateGstr3b();
  const reviewMutation = useReviewGstr3b();
  const fileMutation = useFileGstr3b();
  const exportMutation = useExportGstr3bJson();

  // Use filing data if exists, otherwise use preview data
  const displayTable31 = filing?.table31 || table31;
  const displayTable4 = filing?.table4 || table4;
  const displayTable5 = filing?.table5 || table5;

  const handleGenerate = (regenerate = false) => {
    generateMutation.mutate({
      companyId: selectedCompanyId,
      returnPeriod: selectedReturnPeriod,
      regenerate,
    });
  };

  const handleReview = () => {
    if (!filing) return;
    reviewMutation.mutate(
      { filingId: filing.id, request: { notes: reviewNotes } },
      {
        onSuccess: () => {
          setShowReviewModal(false);
          setReviewNotes('');
        },
      }
    );
  };

  const handleFile = () => {
    if (!filing) return;
    fileMutation.mutate(
      {
        filingId: filing.id,
        request: fileDetails,
      },
      {
        onSuccess: () => {
          setShowFileModal(false);
          setFileDetails({ arn: '', filingDate: new Date().toISOString().split('T')[0] });
        },
      }
    );
  };

  const handleExport = () => {
    if (!filing) return;
    exportMutation.mutate(filing.id);
  };

  const toggleTable = (tableId: string) => {
    setExpandedTables((prev) => ({ ...prev, [tableId]: !prev[tableId] }));
  };

  const handleDrilldown = (lineItem: Gstr3bLineItem) => {
    setSelectedLineItem(lineItem);
    setShowSourceDocs(true);
  };

  const isLoading = filingLoading || table31Loading || table4Loading || table5Loading;

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">GSTR-3B Filing Pack</h1>
        <p className="text-gray-600">
          Generate and review your monthly GSTR-3B return with drill-down to source documents
        </p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-sm p-4 mb-6">
        <div className="flex flex-wrap gap-4">
          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              <Building2 className="h-4 w-4 inline mr-1" />
              Company
            </label>
            <select
              value={selectedCompanyId}
              onChange={(e) => setSelectedCompanyId(e.target.value)}
              className="w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
            >
              <option value="">Select Company</option>
              {companies.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>

          <div className="min-w-[150px]">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              <Calendar className="h-4 w-4 inline mr-1" />
              Return Period
            </label>
            <select
              value={selectedReturnPeriod}
              onChange={(e) => setSelectedReturnPeriod(e.target.value)}
              className="w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
            >
              {returnPeriods.map((p) => (
                <option key={p.value} value={p.value}>
                  {p.label}
                </option>
              ))}
            </select>
          </div>

          <div className="flex items-end gap-2">
            {!filing ? (
              <button
                onClick={() => handleGenerate(false)}
                disabled={!selectedCompanyId || generateMutation.isLoading}
                className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
              >
                <FileText className="h-4 w-4" />
                {generateMutation.isLoading ? 'Generating...' : 'Generate Filing Pack'}
              </button>
            ) : (
              <>
                <button
                  onClick={() => handleGenerate(true)}
                  disabled={generateMutation.isLoading || filing.status === 'filed'}
                  className="flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 disabled:opacity-50"
                >
                  <RefreshCw className="h-4 w-4" />
                  Regenerate
                </button>

                {filing.status === 'generated' && (
                  <button
                    onClick={() => setShowReviewModal(true)}
                    disabled={reviewMutation.isLoading}
                    className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
                  >
                    <FileCheck className="h-4 w-4" />
                    Mark Reviewed
                  </button>
                )}

                {filing.status === 'reviewed' && (
                  <button
                    onClick={() => setShowFileModal(true)}
                    disabled={fileMutation.isLoading}
                    className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
                  >
                    <Send className="h-4 w-4" />
                    Mark as Filed
                  </button>
                )}

                <button
                  onClick={handleExport}
                  disabled={exportMutation.isLoading}
                  className="flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 disabled:opacity-50"
                >
                  <Download className="h-4 w-4" />
                  Export JSON
                </button>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Filing Status Banner */}
      {filing && (
        <div className="bg-white rounded-lg shadow-sm p-4 mb-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <span
                className={cn(
                  'inline-flex items-center gap-1 px-3 py-1 rounded-full text-sm font-medium',
                  getStatusColor(filing.status)
                )}
              >
                {getStatusIcon(filing.status)}
                {filing.status.charAt(0).toUpperCase() + filing.status.slice(1)}
              </span>
              <span className="text-sm text-gray-600">
                GSTIN: <strong>{filing.gstin}</strong>
              </span>
              <span className="text-sm text-gray-600">
                FY: <strong>{filing.financialYear}</strong>
              </span>
            </div>
            {filing.arn && (
              <div className="text-sm text-gray-600">
                ARN: <strong className="text-green-600">{filing.arn}</strong>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Loading State */}
      {isLoading && (
        <div className="flex justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      )}

      {/* No Company Selected */}
      {!selectedCompanyId && !isLoading && (
        <div className="bg-white rounded-lg shadow-sm p-12 text-center">
          <Building2 className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900">Select a Company</h3>
          <p className="text-gray-500">Choose a company to view or generate GSTR-3B filing pack</p>
        </div>
      )}

      {/* Tables */}
      {selectedCompanyId && !isLoading && (
        <div className="space-y-6">
          {/* Table 3.1 - Outward Supplies */}
          <div className="bg-white rounded-lg shadow-sm">
            <button
              onClick={() => toggleTable('3.1')}
              className="w-full flex items-center justify-between p-4 hover:bg-gray-50"
            >
              <div className="flex items-center gap-2">
                {expandedTables['3.1'] ? (
                  <ChevronDown className="h-5 w-5" />
                ) : (
                  <ChevronRight className="h-5 w-5" />
                )}
                <h2 className="text-lg font-semibold">Table 3.1 - Outward Supplies and Inward Supplies Liable to Reverse Charge</h2>
              </div>
              <Receipt className="h-5 w-5 text-gray-400" />
            </button>
            {expandedTables['3.1'] && displayTable31 && (
              <div className="border-t">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Nature of Supplies
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Taxable Value
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        IGST
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        CGST
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        SGST
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Cess
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    <GstRow
                      label="(a) Outward taxable supplies (other than zero rated, nil rated and exempted)"
                      row={displayTable31.outwardTaxable}
                    />
                    <GstRow
                      label="(b) Outward taxable supplies (zero rated)"
                      row={displayTable31.outwardZeroRated}
                    />
                    <GstRow
                      label="(c) Other outward supplies (nil rated, exempted)"
                      row={displayTable31.otherOutward}
                    />
                    <GstRow
                      label="(d) Inward supplies (liable to reverse charge)"
                      row={displayTable31.inwardRcm}
                    />
                    <GstRow
                      label="(e) Non-GST outward supplies"
                      row={displayTable31.nonGst}
                    />
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Table 4 - ITC */}
          <div className="bg-white rounded-lg shadow-sm">
            <button
              onClick={() => toggleTable('4')}
              className="w-full flex items-center justify-between p-4 hover:bg-gray-50"
            >
              <div className="flex items-center gap-2">
                {expandedTables['4'] ? (
                  <ChevronDown className="h-5 w-5" />
                ) : (
                  <ChevronRight className="h-5 w-5" />
                )}
                <h2 className="text-lg font-semibold">Table 4 - Eligible ITC</h2>
              </div>
              <Banknote className="h-5 w-5 text-gray-400" />
            </button>
            {expandedTables['4'] && displayTable4 && (
              <div className="border-t">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Details
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        IGST
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        CGST
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        SGST
                      </th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Cess
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    <tr className="bg-gray-50 font-medium">
                      <td colSpan={5} className="px-4 py-2 text-sm">(A) ITC Available (whether in full or part)</td>
                    </tr>
                    <GstRow label="(1) Import of goods" row={displayTable4.itcAvailable.importGoods} showTaxable={false} />
                    <GstRow label="(2) Import of services" row={displayTable4.itcAvailable.importServices} showTaxable={false} />
                    <GstRow label="(3) Inward supplies liable to reverse charge" row={displayTable4.itcAvailable.rcmInward} showTaxable={false} />
                    <GstRow label="(4) Inward supplies from ISD" row={displayTable4.itcAvailable.isdInward} showTaxable={false} />
                    <GstRow label="(5) All other ITC" row={displayTable4.itcAvailable.allOtherItc} showTaxable={false} />

                    <tr className="bg-gray-50 font-medium">
                      <td colSpan={5} className="px-4 py-2 text-sm">(D) Ineligible ITC</td>
                    </tr>
                    <GstRow label="(1) As per Section 17(5)" row={displayTable4.itcIneligible.section17_5} showTaxable={false} />
                    <GstRow label="(2) Others" row={displayTable4.itcIneligible.others} showTaxable={false} />
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Variance Summary */}
          {variance && variance.items.length > 0 && (
            <div className="bg-white rounded-lg shadow-sm p-4">
              <h2 className="text-lg font-semibold mb-4">Variance from Previous Period ({variance.previousPeriod})</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {variance.items.map((item, idx) => (
                  <div key={idx} className="p-4 bg-gray-50 rounded-lg">
                    <div className="text-sm text-gray-500">{item.field}</div>
                    <div className="text-xs text-gray-400">{item.tableCode}</div>
                    <div className="mt-2 flex items-center gap-2">
                      <span className="text-lg font-semibold">
                        {formatCurrency(item.previousValue)}
                      </span>
                      {item.variance !== undefined && item.variance !== 0 && (
                        <span
                          className={cn(
                            'flex items-center text-sm',
                            item.variance > 0 ? 'text-green-600' : 'text-red-600'
                          )}
                        >
                          {item.variance > 0 ? (
                            <TrendingUp className="h-4 w-4 mr-1" />
                          ) : (
                            <TrendingDown className="h-4 w-4 mr-1" />
                          )}
                          {item.variancePercentage?.toFixed(1)}%
                        </span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Filing History */}
          {history && history.items && history.items.length > 0 && (
            <div className="bg-white rounded-lg shadow-sm p-4">
              <h2 className="text-lg font-semibold mb-4">Filing History</h2>
              <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
                {history.items.map((h) => (
                  <div
                    key={h.id}
                    className={cn(
                      'p-3 rounded-lg border text-center',
                      h.returnPeriod === selectedReturnPeriod
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-gray-200'
                    )}
                  >
                    <div className="font-medium">{h.returnPeriod}</div>
                    <span
                      className={cn(
                        'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium mt-1',
                        getStatusColor(h.status)
                      )}
                    >
                      {h.status}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Review Modal */}
      <Modal
        isOpen={showReviewModal}
        onClose={() => setShowReviewModal(false)}
        title="Mark Filing as Reviewed"
      >
        <div className="space-y-4">
          <p className="text-sm text-gray-600">
            Confirm that you have reviewed the GSTR-3B filing pack for {selectedReturnPeriod}.
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Review Notes (Optional)
            </label>
            <textarea
              value={reviewNotes}
              onChange={(e) => setReviewNotes(e.target.value)}
              rows={3}
              className="w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
              placeholder="Add any notes about this review..."
            />
          </div>
          <div className="flex justify-end gap-3">
            <button
              onClick={() => setShowReviewModal(false)}
              className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
            >
              Cancel
            </button>
            <button
              onClick={handleReview}
              disabled={reviewMutation.isLoading}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
            >
              {reviewMutation.isLoading ? 'Saving...' : 'Mark as Reviewed'}
            </button>
          </div>
        </div>
      </Modal>

      {/* File Modal */}
      <Modal
        isOpen={showFileModal}
        onClose={() => setShowFileModal(false)}
        title="Mark Filing as Filed with GSTN"
      >
        <div className="space-y-4">
          <p className="text-sm text-gray-600">
            Enter the ARN (Acknowledgment Reference Number) received after filing with GSTN.
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              ARN <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={fileDetails.arn}
              onChange={(e) => setFileDetails({ ...fileDetails, arn: e.target.value })}
              className="w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
              placeholder="Enter ARN from GSTN"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Filing Date <span className="text-red-500">*</span>
            </label>
            <input
              type="date"
              value={fileDetails.filingDate}
              onChange={(e) => setFileDetails({ ...fileDetails, filingDate: e.target.value })}
              className="w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
            />
          </div>
          <div className="flex justify-end gap-3">
            <button
              onClick={() => setShowFileModal(false)}
              className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
            >
              Cancel
            </button>
            <button
              onClick={handleFile}
              disabled={fileMutation.isLoading || !fileDetails.arn}
              className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
            >
              {fileMutation.isLoading ? 'Saving...' : 'Mark as Filed'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Source Documents Drawer */}
      <Drawer
        isOpen={showSourceDocs}
        onClose={() => {
          setShowSourceDocs(false);
          setSelectedLineItem(null);
        }}
        title={`Source Documents - ${selectedLineItem?.description || ''}`}
      >
        {selectedLineItem && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <div className="text-sm text-gray-600">
                Table Code: <strong>{selectedLineItem.tableCode}</strong>
              </div>
              <div className="text-sm text-gray-600 mt-1">
                Total: <strong>{formatCurrency(selectedLineItem.taxableValue + selectedLineItem.igst + selectedLineItem.cgst + selectedLineItem.sgst + selectedLineItem.cess)}</strong>
              </div>
            </div>

            {sourceDocs && sourceDocs.length > 0 ? (
              <div className="space-y-3">
                {sourceDocs.map((doc) => (
                  <div key={doc.id} className="p-4 border rounded-lg">
                    <div className="flex justify-between items-start">
                      <div>
                        <div className="font-medium">{doc.sourceNumber}</div>
                        <div className="text-sm text-gray-500">
                          {doc.partyName} {doc.partyGstin && `(${doc.partyGstin})`}
                        </div>
                        <div className="text-xs text-gray-400">
                          {new Date(doc.sourceDate).toLocaleDateString()}
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="font-medium">{formatCurrency(doc.taxableValue)}</div>
                        <div className="text-xs text-gray-500">
                          GST: {formatCurrency(doc.igst + doc.cgst + doc.sgst + doc.cess)}
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-gray-500">
                No source documents found
              </div>
            )}
          </div>
        )}
      </Drawer>
    </div>
  );
};

export default Gstr3bFilingPack;

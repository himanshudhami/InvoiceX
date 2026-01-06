import { useState, useMemo } from 'react';
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs';
import { useCompanies } from '@/hooks/api/useCompanies';
import {
  useTdsChallanSummary,
  usePfEcrSummary,
  useEsiReturnSummary,
} from '@/features/statutory/hooks';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { formatINR } from '@/lib/currency';
import {
  FileText,
  Building,
  Users,
  AlertTriangle,
  CheckCircle,
  Clock,
  TrendingUp,
  Calendar,
  IndianRupee,
  Download,
  Eye,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns';

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
 * Status badge component
 */
const StatusBadge = ({ status }: { status: string }) => {
  const statusConfig: Record<string, { label: string; className: string }> = {
    paid: { label: 'Paid', className: 'bg-green-100 text-green-800' },
    pending: { label: 'Pending', className: 'bg-yellow-100 text-yellow-800' },
    overdue: { label: 'Overdue', className: 'bg-red-100 text-red-800' },
    draft: { label: 'Draft', className: 'bg-gray-100 text-gray-800' },
    filed: { label: 'Filed', className: 'bg-blue-100 text-blue-800' },
  };
  const config = statusConfig[status?.toLowerCase()] || statusConfig.pending;
  return (
    <span className={`px-2 py-1 rounded text-xs font-medium ${config.className}`}>
      {config.label}
    </span>
  );
};

/**
 * Summary Card component
 */
const SummaryCard = ({
  title,
  icon: Icon,
  color,
  deducted,
  deposited,
  pending,
  overdueCount,
  onClick,
}: {
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  color: string;
  deducted: number;
  deposited: number;
  pending: number;
  overdueCount: number;
  onClick?: () => void;
}) => {
  const colorClasses: Record<string, { bg: string; text: string; border: string }> = {
    blue: { bg: 'bg-blue-50', text: 'text-blue-600', border: 'border-blue-200' },
    green: { bg: 'bg-green-50', text: 'text-green-600', border: 'border-green-200' },
    purple: { bg: 'bg-purple-50', text: 'text-purple-600', border: 'border-purple-200' },
  };
  const classes = colorClasses[color] || colorClasses.blue;

  return (
    <div
      className={`${classes.bg} border ${classes.border} rounded-lg p-5 cursor-pointer hover:shadow-md transition-shadow`}
      onClick={onClick}
    >
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Icon className={`w-5 h-5 ${classes.text}`} />
          <h3 className="font-semibold text-gray-900">{title}</h3>
        </div>
        {overdueCount > 0 && (
          <span className="flex items-center gap-1 text-red-600 text-sm">
            <AlertTriangle className="w-4 h-4" />
            {overdueCount} overdue
          </span>
        )}
      </div>
      <div className="grid grid-cols-3 gap-4 text-sm">
        <div>
          <p className="text-gray-500">Deducted</p>
          <p className="font-semibold text-gray-900">{formatINR(deducted)}</p>
        </div>
        <div>
          <p className="text-gray-500">Deposited</p>
          <p className="font-semibold text-green-600">{formatINR(deposited)}</p>
        </div>
        <div>
          <p className="text-gray-500">Pending</p>
          <p className={`font-semibold ${pending > 0 ? 'text-orange-600' : 'text-gray-600'}`}>
            {formatINR(pending)}
          </p>
        </div>
      </div>
    </div>
  );
};

/**
 * Statutory Compliance Dashboard
 * Shows TDS, PF, and ESI compliance status with monthly breakdowns
 */
const StatutoryDashboard = () => {
  const navigate = useNavigate();
  const { data: companies = [] } = useCompanies();
  const fyOptions = useMemo(() => getFinancialYearOptions(), []);

  const [urlState, setUrlState] = useQueryStates(
    {
      companyId: parseAsString,
      financialYear: parseAsString.withDefault(getCurrentFinancialYear()),
      activeTab: parseAsString.withDefault('overview'),
    },
    { history: 'push' }
  );

  const companyId = urlState.companyId || companies[0]?.id || '';

  // Fetch summaries
  const { data: tdsSummary, isLoading: tdsLoading } = useTdsChallanSummary(
    companyId,
    urlState.financialYear,
    !!companyId
  );
  const { data: pfSummary, isLoading: pfLoading } = usePfEcrSummary(
    companyId,
    urlState.financialYear,
    !!companyId
  );
  const { data: esiSummary, isLoading: esiLoading } = useEsiReturnSummary(
    companyId,
    urlState.financialYear,
    !!companyId
  );

  const isLoading = tdsLoading || pfLoading || esiLoading;

  // Calculate totals
  const totals = useMemo(() => {
    return {
      totalDeducted:
        (tdsSummary?.totalTdsDeducted || 0) +
        (pfSummary?.totalEpfDeducted || 0) +
        (esiSummary?.totalEsiDeducted || 0),
      totalDeposited:
        (tdsSummary?.totalTdsDeposited || 0) +
        (pfSummary?.totalEpfDeposited || 0) +
        (esiSummary?.totalEsiDeposited || 0),
      totalPending:
        (tdsSummary?.totalVariance || 0) +
        (pfSummary?.totalVariance || 0) +
        (esiSummary?.totalVariance || 0),
      overdueCount:
        (tdsSummary?.overdueCount || 0) +
        (pfSummary?.overdueCount || 0) +
        (esiSummary?.overdueCount || 0),
    };
  }, [tdsSummary, pfSummary, esiSummary]);

  const tabs = [
    { id: 'overview', label: 'Overview' },
    { id: 'tds', label: 'TDS Challan' },
    { id: 'pf', label: 'PF ECR' },
    { id: 'esi', label: 'ESI Return' },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Statutory Compliance</h1>
          <p className="text-gray-500 mt-1">
            Manage TDS, PF, and ESI deposits and filings
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
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setUrlState({ activeTab: tab.id })}
              className={`py-4 px-1 border-b-2 font-medium text-sm ${
                urlState.activeTab === tab.id
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
        </div>
      ) : urlState.activeTab === 'overview' ? (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <SummaryCard
              title="TDS (Section 192)"
              icon={IndianRupee}
              color="blue"
              deducted={tdsSummary?.totalTdsDeducted || 0}
              deposited={tdsSummary?.totalTdsDeposited || 0}
              pending={tdsSummary?.totalVariance || 0}
              overdueCount={tdsSummary?.overdueCount || 0}
              onClick={() => setUrlState({ activeTab: 'tds' })}
            />
            <SummaryCard
              title="Provident Fund"
              icon={Building}
              color="green"
              deducted={pfSummary?.totalEpfDeducted || 0}
              deposited={pfSummary?.totalEpfDeposited || 0}
              pending={pfSummary?.totalVariance || 0}
              overdueCount={pfSummary?.overdueCount || 0}
              onClick={() => setUrlState({ activeTab: 'pf' })}
            />
            <SummaryCard
              title="ESI (ESIC)"
              icon={Users}
              color="purple"
              deducted={esiSummary?.totalEsiDeducted || 0}
              deposited={esiSummary?.totalEsiDeposited || 0}
              pending={esiSummary?.totalVariance || 0}
              overdueCount={esiSummary?.overdueCount || 0}
              onClick={() => setUrlState({ activeTab: 'esi' })}
            />
          </div>

          {/* Grand Total */}
          <div className="bg-gray-50 border border-gray-200 rounded-lg p-5">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <TrendingUp className="w-5 h-5 text-gray-600" />
              Total Statutory Compliance - FY {urlState.financialYear}
            </h3>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="text-center p-4 bg-white rounded-lg border">
                <p className="text-gray-500 text-sm">Total Deducted</p>
                <p className="text-xl font-bold text-gray-900">{formatINR(totals.totalDeducted)}</p>
              </div>
              <div className="text-center p-4 bg-white rounded-lg border">
                <p className="text-gray-500 text-sm">Total Deposited</p>
                <p className="text-xl font-bold text-green-600">{formatINR(totals.totalDeposited)}</p>
              </div>
              <div className="text-center p-4 bg-white rounded-lg border">
                <p className="text-gray-500 text-sm">Total Pending</p>
                <p className={`text-xl font-bold ${totals.totalPending > 0 ? 'text-orange-600' : 'text-gray-600'}`}>
                  {formatINR(totals.totalPending)}
                </p>
              </div>
              <div className="text-center p-4 bg-white rounded-lg border">
                <p className="text-gray-500 text-sm">Overdue Items</p>
                <p className={`text-xl font-bold ${totals.overdueCount > 0 ? 'text-red-600' : 'text-gray-600'}`}>
                  {totals.overdueCount}
                </p>
              </div>
            </div>
          </div>

          {/* Quick Links */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <button
              onClick={() => navigate('/statutory/form16')}
              className="flex items-center gap-3 p-4 bg-white border rounded-lg hover:bg-gray-50 transition-colors text-left"
            >
              <FileText className="w-8 h-8 text-blue-600" />
              <div>
                <h4 className="font-medium text-gray-900">Form 16 Generation</h4>
                <p className="text-sm text-gray-500">Generate TDS certificates for employees</p>
              </div>
            </button>
            <button
              onClick={() => navigate('/gst/tds-returns')}
              className="flex items-center gap-3 p-4 bg-white border rounded-lg hover:bg-gray-50 transition-colors text-left"
            >
              <FileText className="w-8 h-8 text-green-600" />
              <div>
                <h4 className="font-medium text-gray-900">Form 24Q Filing</h4>
                <p className="text-sm text-gray-500">File quarterly TDS returns</p>
              </div>
            </button>
            <button
              onClick={() => navigate('/ledger/reports')}
              className="flex items-center gap-3 p-4 bg-white border rounded-lg hover:bg-gray-50 transition-colors text-left"
            >
              <TrendingUp className="w-8 h-8 text-purple-600" />
              <div>
                <h4 className="font-medium text-gray-900">GL Reports</h4>
                <p className="text-sm text-gray-500">View statutory liability reports</p>
              </div>
            </button>
          </div>
        </>
      ) : urlState.activeTab === 'tds' ? (
        <TdsChallanSection summary={tdsSummary} />
      ) : urlState.activeTab === 'pf' ? (
        <PfEcrSection summary={pfSummary} />
      ) : (
        <EsiReturnSection summary={esiSummary} />
      )}
    </div>
  );
};

/**
 * TDS Challan Section
 */
const TdsChallanSection = ({ summary }: { summary: any }) => {
  if (!summary) return <div className="text-gray-500 p-4">No TDS data available</div>;

  return (
    <div className="space-y-4">
      <div className="bg-white border rounded-lg overflow-hidden">
        <div className="px-6 py-4 border-b bg-gray-50">
          <h3 className="font-semibold text-gray-900">TDS Challan 281 - Monthly Status</h3>
          <p className="text-sm text-gray-500">Due date: 7th of following month (30th April for March)</p>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Month</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">TDS Deducted</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">TDS Deposited</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Due Date</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Payment Date</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">CIN</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {summary.monthlyStatus?.map((item: any, index: number) => (
                <tr key={index} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {item.monthName}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-900">
                    {formatINR(item.tdsDeducted)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-green-600">
                    {formatINR(item.tdsDeposited)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center text-gray-500">
                    {item.dueDate ? format(new Date(item.dueDate), 'dd MMM yyyy') : '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center text-gray-500">
                    {item.paymentDate ? format(new Date(item.paymentDate), 'dd MMM yyyy') : '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center text-gray-500 font-mono">
                    {item.cinNumber || '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-center">
                    <StatusBadge status={item.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

/**
 * PF ECR Section
 */
const PfEcrSection = ({ summary }: { summary: any }) => {
  if (!summary) return <div className="text-gray-500 p-4">No PF data available</div>;

  return (
    <div className="space-y-4">
      <div className="bg-white border rounded-lg overflow-hidden">
        <div className="px-6 py-4 border-b bg-gray-50">
          <h3 className="font-semibold text-gray-900">PF ECR - Monthly Status</h3>
          <p className="text-sm text-gray-500">Due date: 15th of following month</p>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Month</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Members</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">EPF Deducted</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">EPF Deposited</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Due Date</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">TRRN</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {summary.monthlyStatus?.map((item: any, index: number) => (
                <tr key={index} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {item.monthName}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-600">
                    {item.memberCount}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-900">
                    {formatINR(item.epfDeducted)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-green-600">
                    {formatINR(item.epfDeposited)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center text-gray-500">
                    {item.dueDate ? format(new Date(item.dueDate), 'dd MMM yyyy') : '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center text-gray-500 font-mono">
                    {item.trrn || '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-center">
                    <StatusBadge status={item.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

/**
 * ESI Return Section
 */
const EsiReturnSection = ({ summary }: { summary: any }) => {
  if (!summary) return <div className="text-gray-500 p-4">No ESI data available</div>;

  return (
    <div className="space-y-4">
      <div className="bg-white border rounded-lg overflow-hidden">
        <div className="px-6 py-4 border-b bg-gray-50">
          <h3 className="font-semibold text-gray-900">ESI Return - Monthly Status</h3>
          <p className="text-sm text-gray-500">Due date: 15th of following month | Contribution periods: Apr-Sep, Oct-Mar</p>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Month</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Period</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Employees</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">ESI Deducted</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">ESI Deposited</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Due Date</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Challan</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {summary.monthlyStatus?.map((item: any, index: number) => (
                <tr key={index} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {item.monthName}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center text-gray-500">
                    <span className="text-xs px-2 py-1 bg-gray-100 rounded">
                      {item.contributionPeriod === 'apr_sep' ? 'Apr-Sep' : 'Oct-Mar'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-600">
                    {item.employeeCount}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-900">
                    {formatINR(item.esiDeducted)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-green-600">
                    {formatINR(item.esiDeposited)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center text-gray-500">
                    {item.dueDate ? format(new Date(item.dueDate), 'dd MMM yyyy') : '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center text-gray-500 font-mono">
                    {item.challanNumber || '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-center">
                    <StatusBadge status={item.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default StatutoryDashboard;

import { useState, useMemo } from 'react';
import { useQueryStates, parseAsString } from 'nuqs';
import { useCompanies } from '@/hooks/api/useCompanies';
import {
  useComplianceDashboard,
  useYearOnYearComparison,
} from '@/features/advance-tax/hooks/useAdvanceTax';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { formatINR } from '@/lib/currency';
import {
  Building,
  AlertTriangle,
  CheckCircle,
  Clock,
  TrendingUp,
  Calendar,
  IndianRupee,
  AlertCircle,
  ChevronRight,
  ArrowUpRight,
  ArrowDownRight,
  Minus,
  Bell,
  FileText,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { format, differenceInDays } from 'date-fns';
import type {
  ComplianceDashboard,
  CompanyComplianceStatus,
  UpcomingDueDate,
  ComplianceAlert,
  YearOnYearComparison,
  YearlyTaxSummary,
} from '@/services/api/types';

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
 * Status badge component for company status
 */
const CompanyStatusBadge = ({ status }: { status: string }) => {
  const statusConfig: Record<string, { label: string; className: string; icon: React.ReactNode }> = {
    on_track: {
      label: 'On Track',
      className: 'bg-green-100 text-green-800',
      icon: <CheckCircle className="w-3.5 h-3.5" />,
    },
    at_risk: {
      label: 'At Risk',
      className: 'bg-yellow-100 text-yellow-800',
      icon: <AlertCircle className="w-3.5 h-3.5" />,
    },
    overdue: {
      label: 'Overdue',
      className: 'bg-red-100 text-red-800',
      icon: <AlertTriangle className="w-3.5 h-3.5" />,
    },
    no_assessment: {
      label: 'No Assessment',
      className: 'bg-gray-100 text-gray-800',
      icon: <Minus className="w-3.5 h-3.5" />,
    },
  };
  const config = statusConfig[status?.toLowerCase()] || statusConfig.no_assessment;
  return (
    <span className={`inline-flex items-center gap-1 px-2 py-1 rounded text-xs font-medium ${config.className}`}>
      {config.icon}
      {config.label}
    </span>
  );
};

/**
 * Alert severity badge
 */
const AlertBadge = ({ severity }: { severity: string }) => {
  const severityConfig: Record<string, { className: string }> = {
    critical: { className: 'bg-red-100 text-red-800 border-red-200' },
    warning: { className: 'bg-yellow-100 text-yellow-800 border-yellow-200' },
    info: { className: 'bg-blue-100 text-blue-800 border-blue-200' },
  };
  const config = severityConfig[severity?.toLowerCase()] || severityConfig.info;
  return (
    <span className={`px-2 py-0.5 rounded text-xs font-medium border ${config.className}`}>
      {severity?.charAt(0).toUpperCase() + severity?.slice(1)}
    </span>
  );
};

/**
 * Summary Stats Card
 */
const StatCard = ({
  title,
  value,
  subValue,
  icon: Icon,
  color,
  trend,
}: {
  title: string;
  value: string | number;
  subValue?: string;
  icon: React.ComponentType<{ className?: string }>;
  color: string;
  trend?: 'up' | 'down' | 'neutral';
}) => {
  const colorClasses: Record<string, { bg: string; text: string; border: string }> = {
    blue: { bg: 'bg-blue-50', text: 'text-blue-600', border: 'border-blue-200' },
    green: { bg: 'bg-green-50', text: 'text-green-600', border: 'border-green-200' },
    orange: { bg: 'bg-orange-50', text: 'text-orange-600', border: 'border-orange-200' },
    red: { bg: 'bg-red-50', text: 'text-red-600', border: 'border-red-200' },
    purple: { bg: 'bg-purple-50', text: 'text-purple-600', border: 'border-purple-200' },
    gray: { bg: 'bg-gray-50', text: 'text-gray-600', border: 'border-gray-200' },
  };
  const classes = colorClasses[color] || colorClasses.blue;

  return (
    <div className={`${classes.bg} border ${classes.border} rounded-lg p-4`}>
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-gray-600">{title}</p>
          <p className={`text-2xl font-bold ${classes.text} mt-1`}>{value}</p>
          {subValue && <p className="text-xs text-gray-500 mt-1">{subValue}</p>}
        </div>
        <div className={`p-2 rounded-lg ${classes.bg}`}>
          <Icon className={`w-5 h-5 ${classes.text}`} />
        </div>
      </div>
      {trend && (
        <div className="mt-2 flex items-center gap-1 text-xs">
          {trend === 'up' && <ArrowUpRight className="w-3 h-3 text-green-600" />}
          {trend === 'down' && <ArrowDownRight className="w-3 h-3 text-red-600" />}
          {trend === 'neutral' && <Minus className="w-3 h-3 text-gray-400" />}
        </div>
      )}
    </div>
  );
};

/**
 * Upcoming Due Dates Card
 */
const UpcomingDueDatesCard = ({ dueDates }: { dueDates: UpcomingDueDate[] }) => {
  if (!dueDates || dueDates.length === 0) {
    return (
      <div className="bg-white border rounded-lg p-6">
        <h3 className="font-semibold text-gray-900 flex items-center gap-2 mb-4">
          <Calendar className="w-5 h-5 text-blue-600" />
          Upcoming Due Dates
        </h3>
        <p className="text-gray-500 text-sm">No upcoming due dates</p>
      </div>
    );
  }

  return (
    <div className="bg-white border rounded-lg p-6">
      <h3 className="font-semibold text-gray-900 flex items-center gap-2 mb-4">
        <Calendar className="w-5 h-5 text-blue-600" />
        Upcoming Due Dates
      </h3>
      <div className="space-y-4">
        {dueDates.slice(0, 4).map((due, index) => {
          const daysUntil = due.daysUntilDue;
          const isOverdue = daysUntil < 0;
          const isUrgent = daysUntil >= 0 && daysUntil <= 7;

          return (
            <div
              key={index}
              className={`p-3 rounded-lg border ${
                isOverdue
                  ? 'bg-red-50 border-red-200'
                  : isUrgent
                    ? 'bg-yellow-50 border-yellow-200'
                    : 'bg-gray-50 border-gray-200'
              }`}
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-medium text-gray-900">
                    Q{due.quarter} - {format(new Date(due.dueDate), 'dd MMM yyyy')}
                  </p>
                  <p className="text-sm text-gray-500">
                    {due.companyCount} {due.companyCount === 1 ? 'company' : 'companies'} •{' '}
                    {formatINR(due.totalAmountDue)} due
                  </p>
                </div>
                <div
                  className={`text-sm font-medium ${
                    isOverdue ? 'text-red-600' : isUrgent ? 'text-yellow-600' : 'text-gray-600'
                  }`}
                >
                  {isOverdue
                    ? `${Math.abs(daysUntil)} days overdue`
                    : daysUntil === 0
                      ? 'Due today'
                      : `${daysUntil} days left`}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

/**
 * Alerts Card
 */
const AlertsCard = ({ alerts }: { alerts: ComplianceAlert[] }) => {
  if (!alerts || alerts.length === 0) {
    return (
      <div className="bg-white border rounded-lg p-6">
        <h3 className="font-semibold text-gray-900 flex items-center gap-2 mb-4">
          <Bell className="w-5 h-5 text-orange-600" />
          Alerts
        </h3>
        <div className="flex items-center gap-2 text-green-600">
          <CheckCircle className="w-5 h-5" />
          <p className="text-sm">No alerts - all companies are compliant</p>
        </div>
      </div>
    );
  }

  const criticalCount = alerts.filter((a) => a.severity === 'critical').length;
  const warningCount = alerts.filter((a) => a.severity === 'warning').length;

  return (
    <div className="bg-white border rounded-lg p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="font-semibold text-gray-900 flex items-center gap-2">
          <Bell className="w-5 h-5 text-orange-600" />
          Alerts
        </h3>
        <div className="flex items-center gap-2 text-xs">
          {criticalCount > 0 && (
            <span className="px-2 py-1 bg-red-100 text-red-800 rounded-full">
              {criticalCount} critical
            </span>
          )}
          {warningCount > 0 && (
            <span className="px-2 py-1 bg-yellow-100 text-yellow-800 rounded-full">
              {warningCount} warning
            </span>
          )}
        </div>
      </div>
      <div className="space-y-3 max-h-80 overflow-y-auto">
        {alerts.slice(0, 10).map((alert, index) => (
          <div
            key={index}
            className={`p-3 rounded-lg border ${
              alert.severity === 'critical'
                ? 'bg-red-50 border-red-200'
                : alert.severity === 'warning'
                  ? 'bg-yellow-50 border-yellow-200'
                  : 'bg-blue-50 border-blue-200'
            }`}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-2 mb-1">
                  <AlertBadge severity={alert.severity} />
                  <span className="text-xs text-gray-500">{alert.companyName}</span>
                </div>
                <p className="text-sm text-gray-900">{alert.message}</p>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

/**
 * Company Status Table
 */
const CompanyStatusTable = ({
  companies,
  onCompanyClick,
}: {
  companies: CompanyComplianceStatus[];
  onCompanyClick?: (companyId: string) => void;
}) => {
  if (!companies || companies.length === 0) {
    return (
      <div className="bg-white border rounded-lg p-6">
        <h3 className="font-semibold text-gray-900 mb-4">Company Status</h3>
        <p className="text-gray-500">No companies found</p>
      </div>
    );
  }

  return (
    <div className="bg-white border rounded-lg overflow-hidden">
      <div className="px-6 py-4 border-b bg-gray-50">
        <h3 className="font-semibold text-gray-900">Company-wise Compliance Status</h3>
      </div>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Company
              </th>
              <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                Quarter
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tax Liability
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tax Paid
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Outstanding
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Interest 234C
              </th>
              <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                Next Due
              </th>
              <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {companies.map((company) => {
              const hasOutstanding = company.totalOutstanding > 0;
              const hasInterest = company.totalInterest234C > 0;

              return (
                <tr key={company.companyId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center gap-2">
                      <Building className="w-4 h-4 text-gray-400" />
                      <span className="text-sm font-medium text-gray-900">{company.companyName}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-center">
                    <CompanyStatusBadge status={company.status} />
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-center text-sm text-gray-600">
                    Q{company.currentQuarter}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-900">
                    {formatINR(company.totalTaxLiability)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-green-600">
                    {formatINR(company.totalTaxPaid)}
                  </td>
                  <td
                    className={`px-6 py-4 whitespace-nowrap text-sm text-right ${hasOutstanding ? 'text-orange-600 font-medium' : 'text-gray-600'}`}
                  >
                    {formatINR(company.totalOutstanding)}
                  </td>
                  <td
                    className={`px-6 py-4 whitespace-nowrap text-sm text-right ${hasInterest ? 'text-red-600 font-medium' : 'text-gray-600'}`}
                  >
                    {formatINR(company.totalInterest234C)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center text-gray-500">
                    {company.nextDueDate ? (
                      <div>
                        <p>{format(new Date(company.nextDueDate), 'dd MMM')}</p>
                        <p className="text-xs text-gray-400">
                          {company.daysUntilNextDue > 0
                            ? `${company.daysUntilNextDue}d`
                            : company.daysUntilNextDue === 0
                              ? 'Today'
                              : `${Math.abs(company.daysUntilNextDue)}d ago`}
                        </p>
                      </div>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-center">
                    <button
                      onClick={() => onCompanyClick?.(company.companyId)}
                      className="text-blue-600 hover:text-blue-800 p-1"
                      title="View Details"
                    >
                      <ChevronRight className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};

/**
 * Year on Year Comparison Section
 */
const YoyComparisonSection = ({
  comparison,
  isLoading,
}: {
  comparison?: YearOnYearComparison;
  isLoading: boolean;
}) => {
  if (isLoading) {
    return (
      <div className="bg-white border rounded-lg p-6">
        <h3 className="font-semibold text-gray-900 flex items-center gap-2 mb-4">
          <TrendingUp className="w-5 h-5 text-purple-600" />
          Year-on-Year Comparison
        </h3>
        <div className="animate-pulse space-y-4">
          <div className="h-4 bg-gray-200 rounded w-3/4"></div>
          <div className="h-4 bg-gray-200 rounded w-1/2"></div>
        </div>
      </div>
    );
  }

  if (!comparison || !comparison.yearlySummaries || comparison.yearlySummaries.length === 0) {
    return (
      <div className="bg-white border rounded-lg p-6">
        <h3 className="font-semibold text-gray-900 flex items-center gap-2 mb-4">
          <TrendingUp className="w-5 h-5 text-purple-600" />
          Year-on-Year Comparison
        </h3>
        <p className="text-gray-500 text-sm">
          Select a company to view year-on-year comparison
        </p>
      </div>
    );
  }

  const renderGrowthIndicator = (value: number | undefined) => {
    if (value === undefined || value === null) return null;
    if (value > 0) {
      return (
        <span className="text-green-600 text-xs flex items-center gap-0.5">
          <ArrowUpRight className="w-3 h-3" />
          {value.toFixed(1)}%
        </span>
      );
    }
    if (value < 0) {
      return (
        <span className="text-red-600 text-xs flex items-center gap-0.5">
          <ArrowDownRight className="w-3 h-3" />
          {Math.abs(value).toFixed(1)}%
        </span>
      );
    }
    return <span className="text-gray-400 text-xs">—</span>;
  };

  return (
    <div className="bg-white border rounded-lg overflow-hidden">
      <div className="px-6 py-4 border-b bg-gray-50">
        <h3 className="font-semibold text-gray-900 flex items-center gap-2">
          <TrendingUp className="w-5 h-5 text-purple-600" />
          Year-on-Year Comparison - {comparison.companyName}
        </h3>
      </div>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Financial Year
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                Estimated Tax
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                Tax Paid
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                Interest 234B
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                Interest 234C
              </th>
              <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">
                Status
              </th>
              <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">
                Growth
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {comparison.yearlySummaries.map((summary, index) => (
              <tr key={summary.financialYear} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  FY {summary.financialYear}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-900">
                  {formatINR(summary.estimatedTax)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-green-600">
                  {formatINR(summary.totalPaid)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-orange-600">
                  {formatINR(summary.interest234B)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-red-600">
                  {formatINR(summary.interest234C)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-center">
                  <CompanyStatusBadge status={summary.status} />
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-center">
                  {renderGrowthIndicator(summary.taxGrowthRate)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {/* Summary insights */}
      <div className="px-6 py-4 bg-gray-50 border-t grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="text-center">
          <p className="text-xs text-gray-500">Avg Tax Liability</p>
          <p className="text-lg font-semibold text-gray-900">
            {formatINR(comparison.averageTaxLiability || 0)}
          </p>
        </div>
        <div className="text-center">
          <p className="text-xs text-gray-500">Total Interest Paid</p>
          <p className="text-lg font-semibold text-red-600">
            {formatINR(comparison.totalInterestPaid || 0)}
          </p>
        </div>
        <div className="text-center">
          <p className="text-xs text-gray-500">Compliance Rate</p>
          <p className="text-lg font-semibold text-green-600">
            {((comparison.complianceRate || 0) * 100).toFixed(0)}%
          </p>
        </div>
        <div className="text-center">
          <p className="text-xs text-gray-500">Tax Growth Trend</p>
          <p className="text-lg font-semibold text-purple-600">
            {comparison.averageTaxGrowth !== undefined
              ? `${comparison.averageTaxGrowth > 0 ? '+' : ''}${comparison.averageTaxGrowth.toFixed(1)}%`
              : '—'}
          </p>
        </div>
      </div>
    </div>
  );
};

/**
 * Advance Tax Compliance Dashboard
 * Multi-company view with due date tracking, alerts, and YoY comparison
 */
const AdvanceTaxComplianceDashboard = () => {
  const navigate = useNavigate();
  const { data: companies = [] } = useCompanies();
  const fyOptions = useMemo(() => getFinancialYearOptions(), []);

  const [urlState, setUrlState] = useQueryStates(
    {
      financialYear: parseAsString.withDefault(getCurrentFinancialYear()),
      activeTab: parseAsString.withDefault('overview'),
      selectedCompany: parseAsString,
    },
    { history: 'push' }
  );

  // Fetch compliance dashboard
  const { data: dashboard, isLoading: dashboardLoading } = useComplianceDashboard(
    urlState.financialYear,
    undefined, // All companies
    true
  );

  // Fetch YoY comparison for selected company
  const { data: yoyComparison, isLoading: yoyLoading } = useYearOnYearComparison(
    urlState.selectedCompany || '',
    5, // Last 5 years
    !!urlState.selectedCompany
  );

  const handleCompanyClick = (companyId: string) => {
    setUrlState({ selectedCompany: companyId, activeTab: 'comparison' });
  };

  const tabs = [
    { id: 'overview', label: 'Overview' },
    { id: 'companies', label: 'Company Status' },
    { id: 'comparison', label: 'Year-on-Year' },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Advance Tax Compliance</h1>
          <p className="text-gray-500 mt-1">
            Track advance tax payments across all companies
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
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
          {urlState.activeTab === 'comparison' && (
            <select
              value={urlState.selectedCompany || ''}
              onChange={(e) => setUrlState({ selectedCompany: e.target.value || null })}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">Select Company</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          )}
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

      {dashboardLoading ? (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
        </div>
      ) : urlState.activeTab === 'overview' ? (
        <div className="space-y-6">
          {/* Summary Stats */}
          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
            <StatCard
              title="Total Companies"
              value={dashboard?.totalCompanies || 0}
              subValue={`${dashboard?.companiesWithAssessments || 0} with assessments`}
              icon={Building}
              color="blue"
            />
            <StatCard
              title="Tax Liability"
              value={formatINR(dashboard?.totalTaxLiability || 0)}
              icon={IndianRupee}
              color="purple"
            />
            <StatCard
              title="Tax Paid"
              value={formatINR(dashboard?.totalTaxPaid || 0)}
              subValue={`${((dashboard?.totalTaxPaid || 0) / (dashboard?.totalTaxLiability || 1) * 100).toFixed(0)}% paid`}
              icon={CheckCircle}
              color="green"
            />
            <StatCard
              title="Outstanding"
              value={formatINR(dashboard?.totalOutstanding || 0)}
              icon={Clock}
              color="orange"
            />
            <StatCard
              title="Interest Liability"
              value={formatINR(dashboard?.totalInterestLiability || 0)}
              icon={AlertTriangle}
              color="red"
            />
            <StatCard
              title="Current Quarter"
              value={`Q${dashboard?.currentQuarter || '-'}`}
              subValue={
                dashboard?.nextDueDate
                  ? `Due: ${format(new Date(dashboard.nextDueDate), 'dd MMM')}`
                  : undefined
              }
              icon={Calendar}
              color="gray"
            />
          </div>

          {/* Two Column Layout */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <UpcomingDueDatesCard dueDates={dashboard?.upcomingDueDates || []} />
            <AlertsCard alerts={dashboard?.alerts || []} />
          </div>

          {/* Quick Summary by Status */}
          <div className="bg-white border rounded-lg p-6">
            <h3 className="font-semibold text-gray-900 mb-4">Companies by Status</h3>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="text-center p-4 bg-green-50 rounded-lg border border-green-200">
                <p className="text-2xl font-bold text-green-600">{dashboard?.companiesFullyPaid || 0}</p>
                <p className="text-sm text-gray-600">Fully Paid</p>
              </div>
              <div className="text-center p-4 bg-yellow-50 rounded-lg border border-yellow-200">
                <p className="text-2xl font-bold text-yellow-600">{dashboard?.companiesPartiallyPaid || 0}</p>
                <p className="text-sm text-gray-600">Partially Paid</p>
              </div>
              <div className="text-center p-4 bg-red-50 rounded-lg border border-red-200">
                <p className="text-2xl font-bold text-red-600">{dashboard?.companiesOverdue || 0}</p>
                <p className="text-sm text-gray-600">Overdue</p>
              </div>
              <div className="text-center p-4 bg-gray-50 rounded-lg border border-gray-200">
                <p className="text-2xl font-bold text-gray-600">{dashboard?.companiesWithoutAssessments || 0}</p>
                <p className="text-sm text-gray-600">No Assessment</p>
              </div>
            </div>
          </div>

          {/* Quick Links */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <button
              onClick={() => navigate('/tax/advance-tax')}
              className="flex items-center gap-3 p-4 bg-white border rounded-lg hover:bg-gray-50 transition-colors text-left"
            >
              <FileText className="w-8 h-8 text-blue-600" />
              <div>
                <h4 className="font-medium text-gray-900">Create Assessment</h4>
                <p className="text-sm text-gray-500">Start new advance tax assessment</p>
              </div>
            </button>
            <button
              onClick={() => setUrlState({ activeTab: 'companies' })}
              className="flex items-center gap-3 p-4 bg-white border rounded-lg hover:bg-gray-50 transition-colors text-left"
            >
              <Building className="w-8 h-8 text-green-600" />
              <div>
                <h4 className="font-medium text-gray-900">View All Companies</h4>
                <p className="text-sm text-gray-500">Detailed company-wise status</p>
              </div>
            </button>
            <button
              onClick={() => setUrlState({ activeTab: 'comparison' })}
              className="flex items-center gap-3 p-4 bg-white border rounded-lg hover:bg-gray-50 transition-colors text-left"
            >
              <TrendingUp className="w-8 h-8 text-purple-600" />
              <div>
                <h4 className="font-medium text-gray-900">Year-on-Year Analysis</h4>
                <p className="text-sm text-gray-500">Compare tax trends over years</p>
              </div>
            </button>
          </div>
        </div>
      ) : urlState.activeTab === 'companies' ? (
        <CompanyStatusTable
          companies={dashboard?.companyStatuses || []}
          onCompanyClick={handleCompanyClick}
        />
      ) : (
        <YoyComparisonSection comparison={yoyComparison} isLoading={yoyLoading} />
      )}
    </div>
  );
};

export default AdvanceTaxComplianceDashboard;

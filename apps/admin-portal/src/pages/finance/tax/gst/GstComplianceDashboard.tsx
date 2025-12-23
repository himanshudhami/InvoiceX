import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useCompanies } from '@/hooks/api/useCompanies';
import {
  useItcBlockedSummary,
  useItcAvailabilityReport,
} from '@/features/gst-compliance/hooks';
import {
  useRcmSummary,
  usePendingRcmTransactions,
} from '@/features/gst-compliance/hooks';
import {
  usePendingTdsReturns,
  useTdsReturnDueDates,
} from '@/features/gst-compliance/hooks';
import {
  useExpiringLdcCertificates,
} from '@/features/gst-compliance/hooks';
import {
  usePendingTcsRemittance,
} from '@/features/gst-compliance/hooks';
import {
  Receipt,
  FileX,
  RotateCcw,
  FileText,
  Award,
  Coins,
  AlertTriangle,
  ArrowRight,
  Calendar,
  Building2,
  TrendingUp,
  Clock,
  CheckCircle,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// Generate current return period (MMYYYY format)
const getCurrentReturnPeriod = () => {
  const now = new Date();
  const month = now.getMonth(); // 0-indexed, so Dec = 11
  const year = now.getFullYear();
  // For current month return period
  const returnMonth = month === 0 ? 12 : month;
  const returnYear = month === 0 ? year - 1 : year;
  return `${returnMonth.toString().padStart(2, '0')}${returnYear}`;
};

// Generate financial year options
const generateFinancialYears = () => {
  const currentYear = new Date().getFullYear();
  const currentMonth = new Date().getMonth() + 1;
  const startYear = currentMonth > 3 ? currentYear : currentYear - 1;

  const years = [];
  for (let i = 0; i < 3; i++) {
    const year = startYear - i;
    years.push({
      value: `${year}-${(year + 1).toString().slice(-2)}`,
      label: `FY ${year}-${(year + 1).toString().slice(-2)}`,
    });
  }
  return years;
};

const GstComplianceDashboard = () => {
  const financialYears = generateFinancialYears();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedFY, setSelectedFY] = useState(financialYears[0].value);
  const returnPeriod = getCurrentReturnPeriod();

  const { data: companies = [] } = useCompanies();

  // Fetch dashboard data
  const { data: itcBlockedSummary } = useItcBlockedSummary(
    selectedCompanyId,
    returnPeriod,
    !!selectedCompanyId
  );

  const { data: rcmSummary } = useRcmSummary(
    selectedCompanyId,
    returnPeriod,
    !!selectedCompanyId
  );

  const { data: pendingRcm = [] } = usePendingRcmTransactions(
    selectedCompanyId,
    !!selectedCompanyId
  );

  const { data: pendingTdsReturns = [] } = usePendingTdsReturns(
    selectedCompanyId,
    !!selectedCompanyId
  );

  const { data: tdsReturnDueDates = [] } = useTdsReturnDueDates(selectedFY);

  const { data: expiringLdcCerts = [] } = useExpiringLdcCertificates(
    selectedCompanyId,
    !!selectedCompanyId
  );

  const { data: pendingTcsRemittance = [] } = usePendingTcsRemittance(
    selectedCompanyId,
    !!selectedCompanyId
  );

  // Format currency
  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(amount);
  };

  // Calculate upcoming due dates
  const upcomingDueDates = useMemo(() => {
    const today = new Date();
    const thirtyDaysFromNow = new Date(today.getTime() + 30 * 24 * 60 * 60 * 1000);

    return tdsReturnDueDates.filter(dd => {
      const dueDate = new Date(dd.dueDate);
      return dueDate >= today && dueDate <= thirtyDaysFromNow;
    }).sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime());
  }, [tdsReturnDueDates]);

  // Quick action cards
  const quickActions = [
    {
      title: 'ITC Blocked',
      description: 'Section 17(5) blocked credits',
      icon: FileX,
      color: 'red',
      link: '/gst/itc-blocked',
      count: itcBlockedSummary?.transactionCount || 0,
      amount: itcBlockedSummary?.totalBlockedItc || 0,
    },
    {
      title: 'RCM Management',
      description: 'Reverse charge mechanism',
      icon: RotateCcw,
      color: 'purple',
      link: '/gst/rcm',
      count: pendingRcm.length,
      amount: rcmSummary?.totalRcmLiability || 0,
    },
    {
      title: 'TDS Returns',
      description: 'Form 26Q & 24Q',
      icon: FileText,
      color: 'blue',
      link: '/gst/tds-returns',
      count: pendingTdsReturns.length,
      amount: pendingTdsReturns.reduce((sum, r) => sum + (r.tdsPayable || 0), 0),
    },
    {
      title: 'LDC Certificates',
      description: 'Lower deduction certificates',
      icon: Award,
      color: 'green',
      link: '/gst/ldc',
      count: expiringLdcCerts.length,
      expiring: true,
    },
    {
      title: 'TCS Collection',
      description: 'Tax collected at source',
      icon: Coins,
      color: 'orange',
      link: '/gst/tcs',
      count: pendingTcsRemittance.length,
      amount: pendingTcsRemittance.reduce((sum, t) => sum + (t.tcsAmount || 0), 0),
    },
    {
      title: 'ITC Reversal',
      description: 'Rule 42/43 reversals',
      icon: TrendingUp,
      color: 'yellow',
      link: '/gst/itc-reversal',
      count: 0,
      amount: 0,
    },
  ];

  const getColorClasses = (color: string) => {
    const colors: Record<string, { bg: string; icon: string; text: string; border: string }> = {
      red: {
        bg: 'bg-red-50',
        icon: 'bg-red-100 text-red-600',
        text: 'text-red-600',
        border: 'border-red-200',
      },
      purple: {
        bg: 'bg-purple-50',
        icon: 'bg-purple-100 text-purple-600',
        text: 'text-purple-600',
        border: 'border-purple-200',
      },
      blue: {
        bg: 'bg-blue-50',
        icon: 'bg-blue-100 text-blue-600',
        text: 'text-blue-600',
        border: 'border-blue-200',
      },
      green: {
        bg: 'bg-green-50',
        icon: 'bg-green-100 text-green-600',
        text: 'text-green-600',
        border: 'border-green-200',
      },
      orange: {
        bg: 'bg-orange-50',
        icon: 'bg-orange-100 text-orange-600',
        text: 'text-orange-600',
        border: 'border-orange-200',
      },
      yellow: {
        bg: 'bg-yellow-50',
        icon: 'bg-yellow-100 text-yellow-600',
        text: 'text-yellow-600',
        border: 'border-yellow-200',
      },
    };
    return colors[color] || colors.blue;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">GST & TDS Compliance</h1>
          <p className="text-gray-600 mt-2">
            Manage GST compliance, TDS returns, RCM, and tax certificates
          </p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
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
              <option value="">Select a company</option>
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
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Return Period
            </label>
            <div className="px-3 py-2 bg-gray-50 border border-gray-300 rounded-md text-gray-700">
              {returnPeriod.slice(0, 2)}/{returnPeriod.slice(2)}
            </div>
          </div>
        </div>
      </div>

      {!selectedCompanyId ? (
        <div className="bg-white rounded-lg shadow p-8 text-center">
          <Building2 className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">Select a Company</h3>
          <p className="text-gray-500">
            Please select a company to view GST compliance dashboard
          </p>
        </div>
      ) : (
        <>
          {/* Alerts Section */}
          {(pendingRcm.length > 0 || expiringLdcCerts.length > 0 || upcomingDueDates.length > 0) && (
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
              <div className="flex items-start gap-3">
                <AlertTriangle className="h-5 w-5 text-amber-600 mt-0.5" />
                <div className="flex-1">
                  <h3 className="font-medium text-amber-800">Action Required</h3>
                  <ul className="mt-2 space-y-1 text-sm text-amber-700">
                    {pendingRcm.length > 0 && (
                      <li>• {pendingRcm.length} RCM transactions pending payment</li>
                    )}
                    {expiringLdcCerts.length > 0 && (
                      <li>• {expiringLdcCerts.length} LDC certificates expiring within 30 days</li>
                    )}
                    {upcomingDueDates.length > 0 && (
                      <li>• {upcomingDueDates.length} TDS return due dates approaching</li>
                    )}
                  </ul>
                </div>
              </div>
            </div>
          )}

          {/* Quick Actions Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {quickActions.map((action) => {
              const colors = getColorClasses(action.color);
              const Icon = action.icon;
              return (
                <Link
                  key={action.title}
                  to={action.link}
                  className={cn(
                    'bg-white rounded-lg shadow p-6 hover:shadow-md transition-shadow border-l-4',
                    colors.border
                  )}
                >
                  <div className="flex items-start justify-between">
                    <div className={cn('p-3 rounded-lg', colors.icon)}>
                      <Icon className="h-6 w-6" />
                    </div>
                    <ArrowRight className="h-5 w-5 text-gray-400" />
                  </div>
                  <h3 className="font-semibold text-gray-900 mt-4">{action.title}</h3>
                  <p className="text-sm text-gray-500 mt-1">{action.description}</p>
                  <div className="mt-4 flex items-center justify-between">
                    {action.expiring ? (
                      <span className={cn('text-sm font-medium', colors.text)}>
                        {action.count} expiring soon
                      </span>
                    ) : (
                      <>
                        <span className={cn('text-lg font-bold', colors.text)}>
                          {action.count} pending
                        </span>
                        {action.amount > 0 && (
                          <span className="text-sm text-gray-500">
                            {formatCurrency(action.amount)}
                          </span>
                        )}
                      </>
                    )}
                  </div>
                </Link>
              );
            })}
          </div>

          {/* Due Dates Section */}
          {upcomingDueDates.length > 0 && (
            <div className="bg-white rounded-lg shadow">
              <div className="p-4 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900 flex items-center gap-2">
                  <Calendar className="h-5 w-5 text-gray-500" />
                  Upcoming Due Dates
                </h2>
              </div>
              <div className="divide-y divide-gray-200">
                {upcomingDueDates.slice(0, 5).map((dueDate, index) => {
                  const date = new Date(dueDate.dueDate);
                  const daysLeft = Math.ceil((date.getTime() - Date.now()) / (1000 * 60 * 60 * 24));
                  const isUrgent = daysLeft <= 7;

                  return (
                    <div key={index} className="p-4 flex items-center justify-between">
                      <div className="flex items-center gap-4">
                        <div className={cn(
                          'p-2 rounded-lg',
                          isUrgent ? 'bg-red-100' : 'bg-blue-100'
                        )}>
                          {isUrgent ? (
                            <Clock className={cn('h-5 w-5', isUrgent ? 'text-red-600' : 'text-blue-600')} />
                          ) : (
                            <CheckCircle className="h-5 w-5 text-blue-600" />
                          )}
                        </div>
                        <div>
                          <p className="font-medium text-gray-900">
                            {dueDate.formType} - {dueDate.quarter}
                          </p>
                          <p className="text-sm text-gray-500">{dueDate.description}</p>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className={cn(
                          'font-medium',
                          isUrgent ? 'text-red-600' : 'text-gray-900'
                        )}>
                          {date.toLocaleDateString('en-IN', {
                            day: '2-digit',
                            month: 'short',
                            year: 'numeric',
                          })}
                        </p>
                        <p className={cn(
                          'text-sm',
                          isUrgent ? 'text-red-500' : 'text-gray-500'
                        )}>
                          {daysLeft} days left
                        </p>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Summary Stats */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* RCM Summary */}
            {rcmSummary && (
              <div className="bg-white rounded-lg shadow p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">RCM Summary</h3>
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-gray-600">Total RCM Liability</span>
                    <span className="font-medium">{formatCurrency(rcmSummary.totalRcmLiability)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600">RCM Paid</span>
                    <span className="font-medium text-green-600">{formatCurrency(rcmSummary.rcmPaid)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600">ITC Claimed</span>
                    <span className="font-medium text-blue-600">{formatCurrency(rcmSummary.itcClaimed)}</span>
                  </div>
                  <div className="border-t pt-3 flex justify-between">
                    <span className="font-medium text-gray-900">Net RCM Pending</span>
                    <span className="font-bold text-purple-600">
                      {formatCurrency(rcmSummary.totalRcmLiability - rcmSummary.rcmPaid)}
                    </span>
                  </div>
                </div>
              </div>
            )}

            {/* ITC Blocked Summary */}
            {itcBlockedSummary && (
              <div className="bg-white rounded-lg shadow p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">ITC Blocked Summary</h3>
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-gray-600">Total Blocked ITC</span>
                    <span className="font-medium text-red-600">{formatCurrency(itcBlockedSummary.totalBlockedItc)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600">Transactions</span>
                    <span className="font-medium">{itcBlockedSummary.transactionCount}</span>
                  </div>
                  {itcBlockedSummary.byCategory && Object.entries(itcBlockedSummary.byCategory).slice(0, 3).map(([category, amount]) => (
                    <div key={category} className="flex justify-between text-sm">
                      <span className="text-gray-500">{category}</span>
                      <span className="text-gray-700">{formatCurrency(amount as number)}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default GstComplianceDashboard;

import { useMemo } from 'react';
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs';
import { useCompanies } from '@/hooks/api/useCompanies';
import { useAssetCostReport } from '@/hooks/api/useAssets';
import { usePnLCalculation } from '@/hooks/usePnLCalculation';
import { useOutstandingLoans } from '@/hooks/api/useLoans';
import { calculateBalanceSheet } from '@/lib/balanceSheetCalculation';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { KPICards } from '@/components/financial-report/KPICards';
import { ProfitWaterfall } from '@/components/financial-report/ProfitWaterfall';
import { ExpenseBreakdown } from '@/components/financial-report/ExpenseBreakdown';
import { TrendAnalysis } from '@/components/financial-report/TrendAnalysis';
import { DepreciationImpact } from '@/components/financial-report/DepreciationImpact';
import { KeyMetrics } from '@/components/financial-report/KeyMetrics';
import { ComplianceView } from '@/components/financial-report/ComplianceView';
import { BalanceSheetView } from '@/components/financial-report/BalanceSheetView';
import { CashFlowView } from '@/components/financial-report/CashFlowView';
import { CashFlowKPICards } from '@/components/financial-report/CashFlowKPICards';
import { CashFlowTrendChart, CashFlowBarChart, CashFlowPieChart } from '@/components/financial-report/CashFlowCharts';
import { useCashFlowCalculation } from '@/hooks/useCashFlowCalculation';

type ReportType = 'pl' | 'balance-sheet' | 'cash-flow';

const CompanyFinancialReport = () => {
  const currentYear = new Date().getFullYear();

  // URL-backed filter state with nuqs - persists on refresh
  const [urlState, setUrlState] = useQueryStates(
    {
      type: parseAsString.withDefault('pl') as any,
      view: parseAsString.withDefault('dashboard') as any,
      company: parseAsString.withDefault(''),
      year: parseAsInteger.withDefault(currentYear),
      month: parseAsInteger.withDefault(0),
      accounting: parseAsString.withDefault('accrual') as any, // 'accrual' or 'cash'
    },
    { history: 'replace' }
  )

  // Derive values from URL state
  const reportType = (urlState.type || 'pl') as ReportType;
  const viewMode = (urlState.view || 'dashboard') as 'dashboard' | 'compliance';
  const selectedCompany = urlState.company;
  const selectedYear = urlState.year;
  // Convert 0 to undefined for "All Months"
  const selectedMonth = urlState.month === 0 ? undefined : urlState.month;
  const accountingMethod = (urlState.accounting || 'accrual') as 'accrual' | 'cash';

  const { data: companies = [] } = useCompanies();
  // Convert empty string to undefined to avoid API issues
  const companyId = selectedCompany && selectedCompany.trim() !== '' ? selectedCompany : undefined;
  const { data: assetReport } = useAssetCostReport(companyId);
  const { data: pnlData, isLoading } = usePnLCalculation(
    companyId,
    selectedYear,
    selectedMonth,
    accountingMethod
  );
  const { data: outstandingLoans = [] } = useOutstandingLoans(companyId);
  const { data: cashFlowData, isLoading: cashFlowLoading } = useCashFlowCalculation(
    companyId,
    selectedYear,
    selectedMonth
  );

  // Calculate Balance Sheet data
  const balanceSheetData = useMemo(() => {
    if (!assetReport) return null;
    const asOfDate = new Date(selectedYear, selectedMonth ? selectedMonth - 1 : 11, selectedMonth ? 1 : 31);
    return calculateBalanceSheet(assetReport, companyId, asOfDate, outstandingLoans);
  }, [assetReport, companyId, outstandingLoans, selectedYear, selectedMonth]);

  // Get selected company name and object
  const selectedCompanyName = useMemo(() => {
    if (!selectedCompany) return undefined;
    return companies.find((c) => c.id === selectedCompany)?.name;
  }, [selectedCompany, companies]);

  const selectedCompanyObj = useMemo(() => {
    if (!selectedCompany) return undefined;
    return companies.find((c) => c.id === selectedCompany);
  }, [selectedCompany, companies]);

  // Generate year options (current year ± 2 years)
  const years = Array.from({ length: 5 }, (_, i) => currentYear - 2 + i);

  // Generate month options
  const months = Array.from({ length: 12 }, (_, i) => ({
    value: i + 1,
    label: new Date(selectedYear, i, 1).toLocaleString('default', { month: 'long' }),
  }));

  const isLoadingData = isLoading || 
    (reportType === 'balance-sheet' && !balanceSheetData) ||
    (reportType === 'cash-flow' && cashFlowLoading);

  if (isLoadingData) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (reportType === 'pl' && !pnlData) {
    return (
      <div className="space-y-6">
        <div className="bg-white rounded-lg shadow p-6">
          <p className="text-gray-600">No financial data available for the selected period.</p>
        </div>
      </div>
    );
  }

  if (reportType === 'balance-sheet' && !balanceSheetData) {
    return (
      <div className="space-y-6">
        <div className="bg-white rounded-lg shadow p-6">
          <p className="text-gray-600">No balance sheet data available.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Financial Reports</h1>
          <p className="text-gray-600 mt-2">
            Comprehensive financial statements for Indian tax compliance
          </p>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-3">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Company</label>
            <CompanyFilterDropdown 
              value={selectedCompany} 
              onChange={(val) => setUrlState({ company: val || '' })} 
            />
          </div>

          {(reportType === 'pl' || reportType === 'balance-sheet' || reportType === 'cash-flow') && (
            <>
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">Year</label>
                <select
                  value={selectedYear}
                  onChange={(e) => setUrlState({ year: parseInt(e.target.value) })}
                  className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                >
                  {years.map((year) => (
                    <option key={year} value={year}>
                      {year}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">Month</label>
                <select
                  value={urlState.month}
                  onChange={(e) => {
                    const value = e.target.value
                    setUrlState({ month: value ? parseInt(value) : 0 })
                  }}
                  className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                >
                  <option value={0}>All Months</option>
                  {months.map((month) => (
                    <option key={month.value} value={month.value}>
                      {month.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Accounting Method Toggle - only for P&L */}
              {reportType === 'pl' && (
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">Accounting Method</label>
                  <div className="flex rounded-md border border-gray-300 overflow-hidden">
                    <button
                      onClick={() => setUrlState({ accounting: 'accrual' })}
                      className={`px-3 py-2 text-sm font-medium transition-colors ${
                        accountingMethod === 'accrual'
                          ? 'bg-primary text-white'
                          : 'bg-white text-gray-700 hover:bg-gray-50'
                      }`}
                      title="Income recognized when invoice is raised (Section 145)"
                    >
                      Accrual
                    </button>
                    <button
                      onClick={() => setUrlState({ accounting: 'cash' })}
                      className={`px-3 py-2 text-sm font-medium border-l border-gray-300 transition-colors ${
                        accountingMethod === 'cash'
                          ? 'bg-primary text-white'
                          : 'bg-white text-gray-700 hover:bg-gray-50'
                      }`}
                      title="Income recognized when payment is received"
                    >
                      Cash
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {/* Report Type Tabs */}
      <div className="bg-white rounded-lg shadow">
        <div className="border-b border-gray-200">
          <nav className="flex -mb-px" aria-label="Tabs">
            <button
              onClick={() => {
                setUrlState({ type: 'pl', view: 'dashboard' });
              }}
              className={`px-6 py-4 text-sm font-medium border-b-2 transition-colors ${
                reportType === 'pl'
                  ? 'border-primary text-primary'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              Profit & Loss Statement
            </button>
            <button
              onClick={() => setUrlState({ type: 'balance-sheet' })}
              className={`px-6 py-4 text-sm font-medium border-b-2 transition-colors ${
                reportType === 'balance-sheet'
                  ? 'border-primary text-primary'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              Balance Sheet
            </button>
            <button
              onClick={() => setUrlState({ type: 'cash-flow' })}
              className={`px-6 py-4 text-sm font-medium border-b-2 transition-colors ${
                reportType === 'cash-flow'
                  ? 'border-primary text-primary'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              Cash Flow Statement
            </button>
          </nav>
        </div>

        <div className="p-6">
          {/* Profit & Loss Report */}
          {reportType === 'pl' && (
            <>
              {/* View Toggle for P&L */}
              <div className="mb-4 flex justify-end">
                <div className="inline-flex rounded-md border border-gray-300 overflow-hidden">
                  <button
                    onClick={() => setUrlState({ view: 'dashboard' })}
                    className={`px-4 py-2 text-sm font-medium transition-colors ${
                      viewMode === 'dashboard'
                        ? 'bg-primary text-white'
                        : 'bg-white text-gray-700 hover:bg-gray-50'
                    }`}
                  >
                    Dashboard
                  </button>
                  <button
                    onClick={() => setUrlState({ view: 'compliance' })}
                    className={`px-4 py-2 text-sm font-medium transition-colors ${
                      viewMode === 'compliance'
                        ? 'bg-primary text-white'
                        : 'bg-white text-gray-700 hover:bg-gray-50'
                    }`}
                  >
                    Compliance
                  </button>
                </div>
              </div>

              {/* Dashboard View */}
              {viewMode === 'dashboard' && pnlData && (
                <div className="space-y-6">
                  {/* KPI Cards */}
                  <KPICards data={pnlData} />

                  {/* Main Charts Row */}
                  <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
                    <ProfitWaterfall data={pnlData} />
                    <ExpenseBreakdown data={pnlData} />
                  </div>

                  {/* Trend Analysis */}
                  <TrendAnalysis data={pnlData} selectedYear={selectedYear} />

                  {/* Depreciation and Metrics Row */}
                  <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
                    <DepreciationImpact data={pnlData} />
                    <KeyMetrics data={pnlData} assetReport={assetReport} />
                  </div>
                </div>
              )}

              {/* Compliance View */}
              {viewMode === 'compliance' && pnlData && (
                <ComplianceView
                  data={pnlData}
                  companyName={selectedCompanyName}
                  company={selectedCompanyObj}
                  selectedYear={selectedYear}
                  selectedMonth={selectedMonth}
                />
              )}
            </>
          )}

          {/* Balance Sheet Report */}
          {reportType === 'balance-sheet' && balanceSheetData && (
            <BalanceSheetView
              data={balanceSheetData}
              companyName={selectedCompanyName}
              asOfDate={new Date(selectedYear, selectedMonth ? selectedMonth - 1 : 11, selectedMonth ? 1 : 31)}
            />
          )}

          {/* Cash Flow Statement */}
          {reportType === 'cash-flow' && (
            <>
              {cashFlowData ? (
                <div className="space-y-6">
                  {/* KPI Cards */}
                  <CashFlowKPICards data={cashFlowData} />

                  {/* Charts Row */}
                  <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
                    <CashFlowTrendChart data={cashFlowData} selectedYear={selectedYear} />
                    <CashFlowBarChart data={cashFlowData} />
                  </div>

                  {/* Pie Chart */}
                  <CashFlowPieChart data={cashFlowData} />

                  {/* Cash Flow Statement View */}
                  <CashFlowView
                    data={cashFlowData}
                    companyName={selectedCompanyName}
                    year={selectedYear}
                    month={selectedMonth}
                  />
                </div>
              ) : (
                <div className="bg-white rounded-lg shadow p-8">
                  <div className="text-center py-12">
                    <p className="text-gray-600">No cash flow data available for the selected period.</p>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default CompanyFinancialReport;


import { PnLData } from '@/lib/pnlCalculation';
import { formatINR } from '@/lib/financialUtils';
import { Company } from '@/services/api/types';
import { generateFinancialReportPDF } from './FinancialReportPDF';
import { useState } from 'react';

interface ComplianceViewProps {
  data: PnLData;
  companyName?: string;
  company?: Company;
  selectedYear: number;
  selectedMonth?: number;
}

export const ComplianceView = ({
  data,
  companyName,
  company,
  selectedYear,
  selectedMonth,
}: ComplianceViewProps) => {
  const [isExporting, setIsExporting] = useState(false);
  
  const periodLabel = selectedMonth
    ? `${new Date(selectedYear, selectedMonth - 1, 1).toLocaleString('default', { month: 'long' })} ${selectedYear}`
    : `Year ${selectedYear}`;

  const handleExportPDF = async () => {
    try {
      setIsExporting(true);
      await generateFinancialReportPDF(data, company, selectedYear, selectedMonth);
    } catch (error) {
      console.error('Error exporting PDF:', error);
      alert('Failed to export PDF. Please try again.');
    } finally {
      setIsExporting(false);
    }
  };

  const handlePrint = () => {
    window.print();
  };

  return (
    <div className="bg-white rounded-lg shadow p-8">
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-900 mb-2">PROFIT & LOSS STATEMENT</h2>
        {companyName && (
          <p className="text-gray-600">Company: {companyName}</p>
        )}
        <p className="text-gray-600">Period: {periodLabel}</p>
      </div>

      <table className="w-full text-sm border-collapse">
        <tbody>
          {/* REVENUE SECTION */}
          <tr>
            <td className="font-semibold text-lg pt-4 pb-2" colSpan={2}>
              REVENUE
            </td>
          </tr>
          <tr className="border-b">
            <td className="pl-4 py-2">Sales/Services (Paid Invoices)</td>
            <td className="text-right font-medium py-2">{formatINR(data.totalIncome)}</td>
          </tr>
          <tr className="border-b">
            <td className="pl-4 py-2 text-gray-500">Other Income</td>
            <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
          </tr>
          <tr className="border-b-2 border-gray-800">
            <td className="text-right font-semibold py-3">TOTAL REVENUE</td>
            <td className="text-right font-semibold py-3">{formatINR(data.totalIncome)}</td>
          </tr>

          {/* OPERATING EXPENSES SECTION */}
          <tr>
            <td className="font-semibold text-lg pt-6 pb-2" colSpan={2}>
              OPERATING EXPENSES
            </td>
          </tr>
          <tr className="border-b">
            <td className="pl-4 py-2">Employee Salaries (Gross)</td>
            <td className="text-right font-medium py-2">{formatINR(data.salaryExpense)}</td>
          </tr>
          {data.totalTDSDeducted > 0 && (
            <tr className="border-b">
              <td className="pl-4 py-2 text-gray-600">Less: TDS Deducted (Section 192/194J)</td>
              <td className="text-right py-2 text-gray-600">-{formatINR(data.totalTDSDeducted)}</td>
            </tr>
          )}
          <tr className="border-b">
            <td className="pl-4 py-2">Maintenance & Repairs</td>
            <td className="text-right font-medium py-2">{formatINR(data.maintenanceExpense)}</td>
          </tr>
          {data.opexAssetExpense > 0 && (
            <tr className="border-b">
              <td className="pl-4 py-2">OPEX Asset Purchases</td>
              <td className="text-right font-medium py-2">{formatINR(data.opexAssetExpense)}</td>
            </tr>
          )}
          {data.subscriptionExpense > 0 && (
            <tr className="border-b">
              <td className="pl-4 py-2">Subscription Expenses (Active Subscriptions)</td>
              <td className="text-right font-medium py-2">{formatINR(data.subscriptionExpense)}</td>
            </tr>
          )}
          {data.loanInterestExpense > 0 && (
            <tr className="border-b">
              <td className="pl-4 py-2">Loan Interest Expense (Section 36(1)(iii))</td>
              <td className="text-right font-medium py-2">{formatINR(data.loanInterestExpense)}</td>
            </tr>
          )}
          <tr className="border-b">
            <td className="pl-4 py-2 text-gray-500">Other Operating Expenses</td>
            <td className="text-right py-2 text-gray-500">{formatINR(data.otherExpense)}</td>
          </tr>
          <tr className="border-b-2 border-gray-800">
            <td className="text-right font-semibold py-3">TOTAL OPERATING EXPENSES</td>
            <td className="text-right font-semibold py-3">{formatINR(data.totalOpex)}</td>
          </tr>

          {/* EBITDA */}
          <tr className="bg-blue-50">
            <td className="text-right font-bold text-lg py-4">EBITDA</td>
            <td className="text-right font-bold text-lg py-4">{formatINR(data.ebitda)}</td>
          </tr>

          {/* DEPRECIATION SECTION */}
          <tr>
            <td className="font-semibold text-lg pt-6 pb-2" colSpan={2}>
              DEPRECIATION SCHEDULE
            </td>
          </tr>
          {data.depreciationByCategory.length > 0 ? (
            data.depreciationByCategory.map((dep, index) => (
              <tr key={index} className="border-b">
                <td className="pl-4 py-2">
                  {dep.category} (@ {dep.rate}%)
                </td>
                <td className="text-right font-medium py-2">{formatINR(dep.amount)}</td>
              </tr>
            ))
          ) : (
            <tr className="border-b">
              <td className="pl-4 py-2 text-gray-500">Depreciation (General)</td>
              <td className="text-right py-2">{formatINR(data.depreciation)}</td>
            </tr>
          )}
          <tr className="border-b-2 border-gray-800">
            <td className="text-right font-semibold py-3">TOTAL DEPRECIATION</td>
            <td className="text-right font-semibold py-3">{formatINR(data.depreciation)}</td>
          </tr>

          {/* EBIT */}
          <tr>
            <td className="text-right font-semibold py-3">EBIT (Earnings Before Interest & Tax)</td>
            <td className="text-right font-semibold py-3">{formatINR(data.netProfit)}</td>
          </tr>

          {/* OTHER ITEMS */}
          <tr>
            <td className="font-semibold text-lg pt-4 pb-2" colSpan={2}>
              OTHER ITEMS
            </td>
          </tr>
          <tr className="border-b">
            <td className="pl-4 py-2 text-gray-500">Interest Expense (Already included in OPEX)</td>
            <td className="text-right py-2 text-gray-500">{formatINR(data.loanInterestExpense)}</td>
          </tr>
          <tr className="border-b">
            <td className="pl-4 py-2 text-gray-500">Provisions</td>
            <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
          </tr>
          <tr className="border-b-2 border-gray-800">
            <td className="text-right font-semibold py-3">PROFIT BEFORE TAX</td>
            <td className="text-right font-semibold py-3">{formatINR(data.netProfit)}</td>
          </tr>

          {/* INCOME TAX */}
          <tr>
            <td className="font-semibold text-lg pt-4 pb-2" colSpan={2}>
              INCOME TAX
            </td>
          </tr>
          <tr className="border-b">
            <td className="pl-4 py-2 text-gray-500">Tax @ 30% (Estimated)</td>
            <td className="text-right py-2 text-gray-500">{formatINR(data.netProfit * 0.3)}</td>
          </tr>

          {/* NET PROFIT */}
          <tr className="bg-green-50">
            <td className="text-right font-bold text-lg py-4">NET PROFIT / (LOSS)</td>
            <td className="text-right font-bold text-lg py-4">{formatINR(data.netProfit)}</td>
          </tr>
        </tbody>
      </table>

      {/* TDS SUMMARY SECTION */}
      {data.totalTDSDeducted > 0 && (
        <div className="mt-8 border-t pt-6">
          <h3 className="font-semibold text-gray-900 mb-3">TDS DEDUCTED AT SOURCE</h3>
          <table className="w-full text-sm">
            <tbody>
              <tr className="border-b">
                <td className="py-2">Total TDS Deducted (Section 192 - Salary & Section 194J - Consulting)</td>
                <td className="text-right font-medium py-2">{formatINR(data.totalTDSDeducted)}</td>
              </tr>
              <tr className="border-b">
                <td className="py-2 text-gray-500 text-xs">Note: TDS deducted from employee/consultant payments</td>
                <td className="text-right py-2 text-gray-500 text-xs">Payable to IT Department</td>
              </tr>
            </tbody>
          </table>
        </div>
      )}

      {/* TAX RECONCILIATION SECTION */}
      <div className="mt-8 border-t pt-6">
        <h3 className="font-semibold text-gray-900 mb-3">TAX RECONCILIATION</h3>
        <table className="w-full text-sm">
          <tbody>
            <tr className="border-b">
              <td className="py-2">Book Profit (Above)</td>
              <td className="text-right font-medium py-2">{formatINR(data.netProfit)}</td>
            </tr>
            <tr className="border-b">
              <td className="py-2 text-gray-500">Add: Non-deductible items</td>
              <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
            </tr>
            <tr className="border-b">
              <td className="py-2 text-gray-500">Less: Tax-exempt income</td>
              <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
            </tr>
            <tr className="border-b-2 border-gray-800">
              <td className="text-right font-semibold py-3">TAXABLE INCOME (per IT Act)</td>
              <td className="text-right font-semibold py-3">{formatINR(data.netProfit)}</td>
            </tr>
            <tr>
              <td className="py-2">Tax @ 30%</td>
              <td className="text-right font-medium py-2">{formatINR(data.netProfit * 0.3)}</td>
            </tr>
          </tbody>
        </table>
      </div>

      {/* NOTES SECTION */}
      <div className="mt-8 border-t pt-6">
        <h3 className="font-semibold text-gray-900 mb-3">NOTES:</h3>
        <ul className="list-disc pl-5 space-y-2 text-sm text-gray-700">
          <li>Depreciation rates per Schedule II, Companies Act 2013</li>
          <li>Salary includes gross salary + employer contributions</li>
          <li>Subscription expenses include only active subscriptions (paused/cancelled subscriptions do not accrue costs)</li>
          <li>All amounts in INR (converted from foreign currencies at fixed rates)</li>
          <li>Pro-rata depreciation applied for assets added during the year</li>
          <li>Subscription costs are prorated for partial months (start/pause/resume/cancel mid-month)</li>
          <li>Tax depreciation as per Income Tax Act rates</li>
          <li>This is an estimated statement. Please consult with a tax advisor for final tax calculations.</li>
        </ul>
      </div>

      {/* EXPORT BUTTONS */}
      <div className="mt-6 flex gap-3">
        <button
          onClick={handleExportPDF}
          disabled={isExporting}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          {isExporting ? (
            <>
              <svg className="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Exporting...
            </>
          ) : (
            <>
              <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              Export PDF
            </>
          )}
        </button>
        <button
          onClick={handlePrint}
          className="px-4 py-2 border border-gray-300 rounded-md hover:bg-gray-50 transition-colors flex items-center gap-2"
        >
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
          </svg>
          Print
        </button>
      </div>
    </div>
  );
};


import { CashFlowData } from '@/lib/cashFlowCalculation';
import { formatINR } from '@/lib/financialUtils';

interface CashFlowViewProps {
  data: CashFlowData;
  companyName?: string;
  year?: number;
  month?: number;
}

export const CashFlowView = ({
  data,
  companyName,
  year,
  month,
}: CashFlowViewProps) => {
  const periodLabel = month
    ? `${new Date(year || new Date().getFullYear(), month - 1, 1).toLocaleDateString('en-IN', { month: 'long', year: 'numeric' })}`
    : `${year || new Date().getFullYear()}`;

  return (
    <div className="bg-white rounded-lg shadow p-8">
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-900 mb-2">CASH FLOW STATEMENT</h2>
        <p className="text-gray-600 text-sm">(As per Accounting Standard 3 - AS-3)</p>
        {companyName && (
          <p className="text-gray-600">Company: {companyName}</p>
        )}
        <p className="text-gray-600">Period: {periodLabel}</p>
      </div>

      <div className="space-y-6">
        {/* OPERATING ACTIVITIES */}
        <div>
          <h3 className="text-xl font-bold text-gray-900 mb-4">CASH FLOW FROM OPERATING ACTIVITIES</h3>
          <table className="w-full text-sm border-collapse">
            <tbody>
              <tr>
                <td className="py-2">Net Profit Before Tax</td>
                <td className="text-right font-medium py-2">{formatINR(data.netProfitBeforeTax)}</td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-600">Adjustments for non-cash items:</td>
                <td className="text-right py-2"></td>
              </tr>
              <tr className="border-b">
                <td className="pl-8 py-2 text-gray-600">Depreciation</td>
                <td className="text-right py-2 text-gray-600">{formatINR(data.adjustmentsForNonCashItems.depreciation)}</td>
              </tr>
              <tr className="border-b">
                <td className="pl-8 py-2 text-gray-600">Loan Interest (added back)</td>
                <td className="text-right py-2 text-gray-600">{formatINR(data.adjustmentsForNonCashItems.loanInterest)}</td>
              </tr>
              <tr className="border-b-2 border-gray-800">
                <td className="text-right font-semibold py-3">Operating Cash Before Working Capital Changes</td>
                <td className="text-right font-semibold py-3">{formatINR(data.netProfitBeforeTax + data.adjustmentsForNonCashItems.depreciation + data.adjustmentsForNonCashItems.loanInterest)}</td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-600">Changes in Working Capital:</td>
                <td className="text-right py-2"></td>
              </tr>
              <tr className="border-b">
                <td className="pl-8 py-2 text-gray-600">Accounts Receivable</td>
                <td className="text-right py-2 text-gray-600">{formatINR(-data.changesInWorkingCapital.accountsReceivable)}</td>
              </tr>
              <tr className="border-b">
                <td className="pl-8 py-2 text-gray-600">Accounts Payable</td>
                <td className="text-right py-2 text-gray-600">{formatINR(data.changesInWorkingCapital.accountsPayable)}</td>
              </tr>
              <tr className="border-b-2 border-gray-800">
                <td className="text-right font-semibold py-3">Net Change in Working Capital</td>
                <td className="text-right font-semibold py-3">{formatINR(data.changesInWorkingCapital.netChange)}</td>
              </tr>
              <tr className="bg-blue-50 border-b-2 border-gray-800">
                <td className="text-right font-bold text-lg py-4">CASH FROM OPERATING ACTIVITIES</td>
                <td className="text-right font-bold text-lg py-4">{formatINR(data.cashFromOperatingActivities)}</td>
              </tr>
            </tbody>
          </table>
        </div>

        {/* INVESTING ACTIVITIES */}
        <div>
          <h3 className="text-xl font-bold text-gray-900 mb-4">CASH FLOW FROM INVESTING ACTIVITIES</h3>
          <table className="w-full text-sm border-collapse">
            <tbody>
              <tr className="border-b">
                <td className="py-2">Purchase of Fixed Assets (CAPEX)</td>
                <td className="text-right font-medium py-2">({formatINR(data.purchaseOfFixedAssets)})</td>
              </tr>
              {data.saleOfFixedAssets > 0 && (
                <tr className="border-b">
                  <td className="py-2">Sale of Fixed Assets</td>
                  <td className="text-right font-medium py-2">{formatINR(data.saleOfFixedAssets)}</td>
                </tr>
              )}
              <tr className="bg-green-50 border-b-2 border-gray-800">
                <td className="text-right font-bold text-lg py-4">CASH FROM INVESTING ACTIVITIES</td>
                <td className="text-right font-bold text-lg py-4">{formatINR(data.cashFromInvestingActivities)}</td>
              </tr>
            </tbody>
          </table>
        </div>

        {/* FINANCING ACTIVITIES */}
        <div>
          <h3 className="text-xl font-bold text-gray-900 mb-4">CASH FLOW FROM FINANCING ACTIVITIES</h3>
          <table className="w-full text-sm border-collapse">
            <tbody>
              {data.loanDisbursements > 0 && (
                <tr className="border-b">
                  <td className="py-2">Loan Disbursements Received</td>
                  <td className="text-right font-medium py-2">{formatINR(data.loanDisbursements)}</td>
                </tr>
              )}
              {data.loanRepayments > 0 && (
                <tr className="border-b">
                  <td className="py-2">Loan Principal Repayments</td>
                  <td className="text-right font-medium py-2">({formatINR(data.loanRepayments)})</td>
                </tr>
              )}
              <tr className="bg-purple-50 border-b-2 border-gray-800">
                <td className="text-right font-bold text-lg py-4">CASH FROM FINANCING ACTIVITIES</td>
                <td className="text-right font-bold text-lg py-4">{formatINR(data.cashFromFinancingActivities)}</td>
              </tr>
            </tbody>
          </table>
        </div>

        {/* NET CASH FLOW */}
        <div>
          <h3 className="text-xl font-bold text-gray-900 mb-4">NET CASH FLOW</h3>
          <table className="w-full text-sm border-collapse">
            <tbody>
              <tr className="border-b">
                <td className="py-2">Net Increase / (Decrease) in Cash and Cash Equivalents</td>
                <td className="text-right font-semibold py-2">{formatINR(data.netIncreaseDecreaseInCash)}</td>
              </tr>
              <tr className="border-b">
                <td className="py-2">Opening Cash and Cash Equivalents</td>
                <td className="text-right font-medium py-2">{formatINR(data.openingCashBalance)}</td>
              </tr>
              <tr className="bg-yellow-50 border-b-2 border-gray-800">
                <td className="text-right font-bold text-lg py-4">CLOSING CASH AND CASH EQUIVALENTS</td>
                <td className="text-right font-bold text-lg py-4">{formatINR(data.closingCashBalance)}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      {/* NOTES SECTION */}
      <div className="mt-8 border-t pt-6">
        <h3 className="font-semibold text-gray-900 mb-3">NOTES:</h3>
        <ul className="list-disc pl-5 space-y-2 text-sm text-gray-700">
          <li>This statement is prepared in accordance with Accounting Standard 3 (AS-3) - Cash Flow Statements</li>
          <li>Operating activities are presented using the indirect method</li>
          <li>Depreciation and loan interest are added back as non-cash items</li>
          <li>Changes in working capital are calculated from accounts receivable and payable</li>
          <li>Investing activities include CAPEX asset purchases and disposals</li>
          <li>Financing activities include loan disbursements and principal repayments</li>
          <li>Opening cash balance is estimated (actual cash balance tracking not yet implemented)</li>
          <li>All amounts in INR (converted from foreign currencies at fixed rates)</li>
        </ul>
      </div>

      {/* EXPORT BUTTONS */}
      <div className="mt-6 flex gap-3">
        <button className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 transition-colors">
          Export PDF
        </button>
        <button className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 transition-colors">
          Download XLSX
        </button>
        <button className="px-4 py-2 border border-gray-300 rounded-md hover:bg-gray-50 transition-colors">
          Print
        </button>
      </div>
    </div>
  );
};





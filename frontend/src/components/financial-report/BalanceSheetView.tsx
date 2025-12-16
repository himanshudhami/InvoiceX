import { BalanceSheetData } from '@/lib/balanceSheetCalculation';
import { formatINR, formatINRDetailed } from '@/lib/financialUtils';

interface BalanceSheetViewProps {
  data: BalanceSheetData;
  companyName?: string;
  asOfDate?: Date;
}

export const BalanceSheetView = ({
  data,
  companyName,
  asOfDate = new Date(),
}: BalanceSheetViewProps) => {
  const dateLabel = asOfDate.toLocaleDateString('en-IN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  return (
    <div className="bg-white rounded-lg shadow p-8">
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-900 mb-2">BALANCE SHEET</h2>
        {companyName && (
          <p className="text-gray-600">Company: {companyName}</p>
        )}
        <p className="text-gray-600">As of: {dateLabel}</p>
        {!data.isBalanced && (
          <p className="text-red-600 text-sm mt-2">
            ⚠️ Balance Sheet is not balanced. Please review the data.
          </p>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
        {/* ASSETS SIDE */}
        <div>
          <h3 className="text-xl font-bold text-gray-900 mb-4">ASSETS</h3>
          
          <table className="w-full text-sm border-collapse">
            <tbody>
              {/* FIXED ASSETS */}
              <tr>
                <td className="font-semibold text-lg pt-4 pb-2" colSpan={2}>
                  FIXED ASSETS
                </td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2">Gross Block (CAPEX Assets)</td>
                <td className="text-right font-medium py-2">{formatINR(data.fixedAssets.grossBlock)}</td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2">Less: Accumulated Depreciation</td>
                <td className="text-right font-medium py-2">({formatINR(data.fixedAssets.accumulatedDepreciation)})</td>
              </tr>
              <tr className="border-b-2 border-gray-800">
                <td className="text-right font-semibold py-3">NET BLOCK</td>
                <td className="text-right font-semibold py-3">{formatINR(data.fixedAssets.netBlock)}</td>
              </tr>

              {/* CURRENT ASSETS */}
              <tr>
                <td className="font-semibold text-lg pt-6 pb-2" colSpan={2}>
                  CURRENT ASSETS
                </td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-500">Cash & Bank Balances</td>
                <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-500">Accounts Receivable</td>
                <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-500">Other Current Assets</td>
                <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
              </tr>
              <tr className="border-b-2 border-gray-800">
                <td className="text-right font-semibold py-3">TOTAL CURRENT ASSETS</td>
                <td className="text-right font-semibold py-3">{formatINR(data.currentAssets.total)}</td>
              </tr>

              {/* TOTAL ASSETS */}
              <tr className="bg-blue-50">
                <td className="text-right font-bold text-lg py-4">TOTAL ASSETS</td>
                <td className="text-right font-bold text-lg py-4">{formatINR(data.totalAssets)}</td>
              </tr>
            </tbody>
          </table>
        </div>

        {/* LIABILITIES & EQUITY SIDE */}
        <div>
          <h3 className="text-xl font-bold text-gray-900 mb-4">LIABILITIES & EQUITY</h3>
          
          <table className="w-full text-sm border-collapse">
            <tbody>
              {/* CURRENT LIABILITIES */}
              <tr>
                <td className="font-semibold text-lg pt-4 pb-2" colSpan={2}>
                  CURRENT LIABILITIES
                </td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-500">Accounts Payable</td>
                <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
              </tr>
              {data.currentLiabilities.loanLiabilities > 0 && (
                <tr className="border-b">
                  <td className="pl-4 py-2">Short-term Loan Liabilities (Due within 12 months)</td>
                  <td className="text-right font-medium py-2">{formatINR(data.currentLiabilities.loanLiabilities)}</td>
                </tr>
              )}
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-500">Other Current Liabilities</td>
                <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
              </tr>
              <tr className="border-b-2 border-gray-800">
                <td className="text-right font-semibold py-3">TOTAL CURRENT LIABILITIES</td>
                <td className="text-right font-semibold py-3">{formatINR(data.currentLiabilities.total)}</td>
              </tr>

              {/* LONG-TERM LIABILITIES */}
              <tr>
                <td className="font-semibold text-lg pt-6 pb-2" colSpan={2}>
                  LONG-TERM LIABILITIES
                </td>
              </tr>
              {data.longTermLiabilities.loanLiabilities > 0 && (
                <tr className="border-b">
                  <td className="pl-4 py-2">Long-term Loan Liabilities (Due after 12 months)</td>
                  <td className="text-right font-medium py-2">{formatINR(data.longTermLiabilities.loanLiabilities)}</td>
                </tr>
              )}
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-500">Other Long-term Liabilities</td>
                <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
              </tr>
              <tr className="border-b-2 border-gray-800">
                <td className="text-right font-semibold py-3">TOTAL LONG-TERM LIABILITIES</td>
                <td className="text-right font-semibold py-3">{formatINR(data.longTermLiabilities.total)}</td>
              </tr>

              <tr className="border-b-2 border-gray-800">
                <td className="text-right font-semibold py-3">TOTAL LIABILITIES</td>
                <td className="text-right font-semibold py-3">{formatINR(data.totalLiabilities)}</td>
              </tr>

              {/* EQUITY */}
              <tr>
                <td className="font-semibold text-lg pt-6 pb-2" colSpan={2}>
                  EQUITY
                </td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-500">Share Capital</td>
                <td className="text-right py-2 text-gray-500">{formatINR(0)}</td>
              </tr>
              <tr className="border-b">
                <td className="pl-4 py-2 text-gray-500">Retained Earnings</td>
                <td className="text-right py-2 text-gray-500">{formatINR(data.equity.total)}</td>
              </tr>
              <tr className="border-b-2 border-gray-800">
                <td className="text-right font-semibold py-3">TOTAL EQUITY</td>
                <td className="text-right font-semibold py-3">{formatINR(data.equity.total)}</td>
              </tr>

              {/* TOTAL LIABILITIES & EQUITY */}
              <tr className="bg-green-50">
                <td className="text-right font-bold text-lg py-4">TOTAL LIABILITIES & EQUITY</td>
                <td className="text-right font-bold text-lg py-4">{formatINR(data.totalLiabilities + data.equity.total)}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      {/* NOTES SECTION */}
      <div className="mt-8 border-t pt-6">
        <h3 className="font-semibold text-gray-900 mb-3">NOTES:</h3>
        <ul className="list-disc pl-5 space-y-2 text-sm text-gray-700">
          <li>Fixed Assets shown at cost less accumulated depreciation (Net Book Value)</li>
          <li>Depreciation calculated per Schedule II, Companies Act 2013</li>
          <li>Only CAPEX assets are capitalized; OPEX assets are expensed in P&L</li>
          <li>Loan liabilities are categorized as current (due within 12 months) or long-term (due after 12 months) based on remaining tenure</li>
          <li>Current Assets and other Liabilities sections are placeholders for future implementation</li>
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


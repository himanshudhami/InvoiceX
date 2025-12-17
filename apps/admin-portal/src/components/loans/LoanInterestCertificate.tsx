import { useMemo } from 'react';
import { useLoans, useTotalInterestPaid } from '@/hooks/api/useLoans';
import { useCompanies } from '@/hooks/api/useCompanies';
import { formatINR } from '@/lib/currency';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Loan } from '@/services/api/types';

interface LoanInterestCertificateProps {
  companyId?: string;
  financialYear: number; // e.g., 2024 for FY 2024-25
}

export const LoanInterestCertificate = ({ companyId, financialYear }: LoanInterestCertificateProps) => {
  const { data: companies = [] } = useCompanies();
  const { data: loansData } = useLoans({ companyId, pageNumber: 1, pageSize: 100 });
  const loans = loansData?.items || [];

  // Calculate financial year dates (April 1 to March 31)
  const fromDate = `${financialYear}-04-01`;
  const toDate = `${financialYear + 1}-03-31`;

  // Filter active/closed loans
  const activeLoans = useMemo(() => {
    return loans.filter(loan => loan.status === 'active' || loan.status === 'closed');
  }, [loans]);

  const selectedCompany = useMemo(() => {
    if (!companyId) return undefined;
    return companies.find(c => c.id === companyId);
  }, [companyId, companies]);

  const handlePrint = () => {
    window.print();
  };

  const handleExportPDF = () => {
    // TODO: Implement PDF export
    alert('PDF export coming soon');
  };

  return (
    <div className="space-y-6 print:space-y-4">
      {/* Header */}
      <div className="flex justify-between items-center print:hidden">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Loan Interest Certificate</h2>
          <p className="text-gray-600 mt-1">
            Annual Interest Paid Statement for Tax Filing (FY {financialYear}-{financialYear + 1})
          </p>
        </div>
        <div className="flex gap-2">
          <Button onClick={handleExportPDF} variant="outline">
            Export PDF
          </Button>
          <Button onClick={handlePrint} variant="outline">
            Print
          </Button>
        </div>
      </div>

      {/* Certificate Content */}
      <Card className="print:shadow-none print:border-0">
        <CardHeader className="print:pb-2">
          <CardTitle className="text-center text-xl print:text-lg">
            CERTIFICATE OF INTEREST PAID ON LOANS
          </CardTitle>
          <p className="text-center text-sm text-gray-600 print:text-xs mt-2">
            Financial Year: {financialYear}-{financialYear + 1} (April 1, {financialYear} to March 31, {financialYear + 1})
          </p>
        </CardHeader>
        <CardContent className="space-y-6 print:space-y-4">
          {/* Company Details */}
          {selectedCompany && (
            <div className="border-b pb-4 print:pb-2">
              <h3 className="font-semibold text-gray-900 mb-2 print:text-sm">Company Details:</h3>
              <div className="grid grid-cols-2 gap-2 text-sm print:text-xs">
                <div>
                  <span className="text-gray-600">Name:</span> {selectedCompany.name}
                </div>
                {selectedCompany.taxNumber && (
                  <div>
                    <span className="text-gray-600">PAN:</span> {selectedCompany.taxNumber}
                  </div>
                )}
                {selectedCompany.addressLine1 && (
                  <div className="col-span-2">
                    <span className="text-gray-600">Address:</span> {selectedCompany.addressLine1}
                    {selectedCompany.addressLine2 && `, ${selectedCompany.addressLine2}`}
                    {selectedCompany.city && `, ${selectedCompany.city}`}
                    {selectedCompany.state && `, ${selectedCompany.state}`}
                    {selectedCompany.zipCode && ` - ${selectedCompany.zipCode}`}
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Interest Details Table */}
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm print:text-xs">
              <thead>
                <tr className="bg-gray-50 print:bg-transparent border-b-2 border-gray-800">
                  <th className="text-left py-2 px-3 font-semibold">S.No.</th>
                  <th className="text-left py-2 px-3 font-semibold">Loan Name</th>
                  <th className="text-left py-2 px-3 font-semibold">Lender Name</th>
                  <th className="text-left py-2 px-3 font-semibold">Loan Account No.</th>
                  <th className="text-right py-2 px-3 font-semibold">Interest Rate (%)</th>
                  <th className="text-right py-2 px-3 font-semibold">Interest Paid (₹)</th>
                </tr>
              </thead>
              <tbody>
                {activeLoans.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="text-center py-8 text-gray-500">
                      No loans found for the selected period
                    </td>
                  </tr>
                ) : (
                  <>
                    {activeLoans.map((loan, index) => (
                      <LoanInterestRow
                        key={loan.id}
                        index={index + 1}
                        loan={loan}
                        fromDate={fromDate}
                        toDate={toDate}
                      />
                    ))}
                    <tr className="border-t-2 border-gray-800 font-bold bg-gray-50 print:bg-transparent">
                      <td colSpan={5} className="text-right py-3 px-3">
                        TOTAL INTEREST PAID:
                      </td>
                      <td className="text-right py-3 px-3">
                        <TotalInterestCell loans={activeLoans} fromDate={fromDate} toDate={toDate} />
                      </td>
                    </tr>
                  </>
                )}
              </tbody>
            </table>
          </div>

          {/* Notes */}
          <div className="border-t pt-4 print:pt-2 space-y-2 text-sm print:text-xs text-gray-700">
            <p className="font-semibold">Notes:</p>
            <ul className="list-disc pl-5 space-y-1">
              <li>
                This certificate is issued for the purpose of Income Tax filing under Section 36(1)(iii) of the Income Tax Act, 1961.
              </li>
              <li>
                Interest paid on loans is deductible as business expense under Section 36(1)(iii) of the Income Tax Act, 1961.
              </li>
              <li>
                TDS (Tax Deducted at Source) on interest, if applicable, should be considered separately for tax compliance.
              </li>
              <li>
                This certificate is based on actual interest payments recorded in the system for the financial year {financialYear}-{financialYear + 1}.
              </li>
            </ul>
          </div>

          {/* Declaration */}
          <div className="border-t pt-4 print:pt-2 text-sm print:text-xs">
            <p className="font-semibold mb-2">Declaration:</p>
            <p className="text-gray-700">
              I hereby declare that the above information is true and correct to the best of my knowledge and belief.
            </p>
            <div className="mt-6 print:mt-4 flex justify-end">
              <div className="text-right">
                <div className="border-t-2 border-gray-800 w-48 print:w-32 mt-12 print:mt-8 mb-2"></div>
                <p className="text-sm print:text-xs text-gray-600">Authorized Signatory</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

// Component to fetch and display interest for a single loan
const LoanInterestRow = ({ 
  index, 
  loan, 
  fromDate, 
  toDate 
}: { 
  index: number; 
  loan: Loan; 
  fromDate: string; 
  toDate: string;
}) => {
  const { data: interestPaid = 0 } = useTotalInterestPaid(loan.id, fromDate, toDate);

  return (
    <tr className="border-b hover:bg-gray-50 print:hover:bg-transparent">
      <td className="py-2 px-3">{index}</td>
      <td className="py-2 px-3">{loan.loanName}</td>
      <td className="py-2 px-3">{loan.lenderName}</td>
      <td className="py-2 px-3">{loan.loanAccountNumber || 'N/A'}</td>
      <td className="text-right py-2 px-3">{loan.interestRate.toFixed(2)}%</td>
      <td className="text-right py-2 px-3 font-medium">{formatINR(interestPaid)}</td>
    </tr>
  );
};

// Component to calculate and display total interest
const TotalInterestCell = ({ 
  loans, 
  fromDate, 
  toDate 
}: { 
  loans: Loan[]; 
  fromDate: string; 
  toDate: string;
}) => {
  // Fetch interest for all loans - hooks must be called unconditionally
  const interestQueries = loans.map(loan => useTotalInterestPaid(loan.id, fromDate, toDate));

  const totalInterest = useMemo(() => {
    return interestQueries.reduce((sum, query) => {
      return sum + (query.data || 0);
    }, 0);
  }, [interestQueries]);

  return <>{formatINR(totalInterest)}</>;
};


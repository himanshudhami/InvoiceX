import { useState, useMemo, useEffect } from 'react';
import { useOutstandingLoans, useRecordEmiPayment, useLoanSchedule } from '@/hooks/api/useLoans';
import { useCompanies } from '@/hooks/api/useCompanies';
import { Loan, CreateEmiPaymentDto, LoanEmiScheduleItemDto } from '@/services/api/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { formatINR } from '@/lib/financialUtils';
import { Button } from '@/components/ui/button';

const EmiPaymentManagement = () => {
  const { data: companies = [] } = useCompanies();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string | undefined>(undefined);
  const selectedCompany = companies.find((c) => c.id === selectedCompanyId);
  const { data: outstandingLoans = [], isLoading } = useOutstandingLoans(selectedCompanyId);
  const recordPayment = useRecordEmiPayment();

  const [selectedLoan, setSelectedLoan] = useState<Loan | null>(null);
  const [paymentData, setPaymentData] = useState<CreateEmiPaymentDto>({
    paymentDate: new Date().toISOString().split('T')[0],
    amount: 0,
    principalAmount: 0,
    interestAmount: 0,
    paymentMethod: 'bank_transfer',
  });

  const { data: schedule } = useLoanSchedule(selectedLoan?.id);

  // Get all pending EMIs for dropdown
  const pendingEmis = useMemo(() => {
    if (!schedule?.scheduleItems) return [];
    return schedule.scheduleItems.filter((item) => item.status === 'pending');
  }, [schedule]);

  // Get next pending EMI (for initial selection)
  const nextEmi = useMemo(() => {
    return pendingEmis.length > 0 ? pendingEmis[0] : null;
  }, [pendingEmis]);

  // State for selected EMI number
  const [selectedEmiNumber, setSelectedEmiNumber] = useState<number | null>(null);

  // Get the currently selected EMI details
  const selectedEmi = useMemo(() => {
    if (!selectedEmiNumber || !schedule?.scheduleItems) return null;
    return schedule.scheduleItems.find((item) => item.emiNumber === selectedEmiNumber);
  }, [selectedEmiNumber, schedule]);

  // Auto-select next pending EMI when loan is selected
  useEffect(() => {
    if (nextEmi && selectedLoan) {
      setSelectedEmiNumber(nextEmi.emiNumber);
    }
  }, [nextEmi, selectedLoan]);

  // Auto-fill payment data when selected EMI changes
  useEffect(() => {
    if (selectedEmi && selectedLoan) {
      setPaymentData({
        paymentDate: new Date().toISOString().split('T')[0],
        amount: selectedEmi.totalEmi,
        principalAmount: selectedEmi.principalAmount,
        interestAmount: selectedEmi.interestAmount,
        paymentMethod: 'bank_transfer',
        emiNumber: selectedEmi.emiNumber,
      });
    }
  }, [selectedEmi, selectedLoan]);

  // Reset EMI selection when loan changes
  useEffect(() => {
    if (!selectedLoan) {
      setSelectedEmiNumber(null);
    }
  }, [selectedLoan]);

  const handlePayment = async () => {
    if (!selectedLoan || !selectedEmiNumber) return;

    try {
      await recordPayment.mutateAsync({
        id: selectedLoan.id,
        data: paymentData,
      });
      setSelectedLoan(null);
      setSelectedEmiNumber(null);
      setPaymentData({
        paymentDate: new Date().toISOString().split('T')[0],
        amount: 0,
        principalAmount: 0,
        interestAmount: 0,
        paymentMethod: 'bank_transfer',
      });
    } catch (error) {
      console.error('Failed to record payment:', error);
    }
  };

  const totalOutstanding = useMemo(() => {
    return outstandingLoans.reduce((sum, loan) => sum + loan.outstandingPrincipal, 0);
  }, [outstandingLoans]);

  const totalMonthlyEmi = useMemo(() => {
    return outstandingLoans.reduce((sum, loan) => sum + loan.emiAmount, 0);
  }, [outstandingLoans]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">EMI Payment Management</h1>
        <p className="text-gray-600 mt-2">Record EMI payments and track loan schedules</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-600">Active Loans</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{outstandingLoans.length}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-600">Total Outstanding</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{formatINR(totalOutstanding)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium text-gray-600">Total Monthly EMI</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{formatINR(totalMonthlyEmi)}</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Filter by Company</CardTitle>
        </CardHeader>
        <CardContent>
          <select
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            value={selectedCompanyId || ''}
            onChange={(e) => {
              setSelectedCompanyId(e.target.value || undefined);
              setSelectedLoan(null);
            }}
          >
            <option value="">All Companies</option>
            {companies.map((company) => (
              <option key={company.id} value={company.id}>
                {company.name}
              </option>
            ))}
          </select>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Outstanding Loans</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="text-center text-gray-500 py-8">Loading loans...</div>
            ) : outstandingLoans.length === 0 ? (
              <div className="text-center text-gray-500 py-8">No outstanding loans</div>
            ) : (
              <div className="space-y-2">
                {outstandingLoans.map((loan) => (
                  <div
                    key={loan.id}
                    className={`p-4 border rounded-lg cursor-pointer transition-colors ${
                      selectedLoan?.id === loan.id ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-gray-300'
                    }`}
                    onClick={() => setSelectedLoan(loan)}
                  >
                    <div className="flex justify-between items-start">
                      <div>
                        <h3 className="font-medium text-gray-900">{loan.loanName}</h3>
                        <p className="text-sm text-gray-600">{loan.lenderName}</p>
                      </div>
                      <div className="text-right">
                        <p className="text-sm font-medium text-gray-900">{formatINR(loan.outstandingPrincipal)}</p>
                        <p className="text-xs text-gray-500">EMI: {formatINR(loan.emiAmount)}</p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Record Payment</CardTitle>
          </CardHeader>
          <CardContent>
            {!selectedLoan ? (
              <div className="text-center text-gray-500 py-8">Select a loan to record payment</div>
            ) : (
              <div className="space-y-4">
                <div>
                  <h3 className="font-medium text-gray-900 mb-2">{selectedLoan.loanName}</h3>
                  <p className="text-sm text-gray-600">{selectedLoan.lenderName}</p>
                </div>

                {pendingEmis.length > 0 ? (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Select EMI *</label>
                    <select
                      className="w-full px-3 py-2 border border-gray-300 rounded-md"
                      value={selectedEmiNumber || ''}
                      onChange={(e) => setSelectedEmiNumber(parseInt(e.target.value) || null)}
                    >
                      <option value="">Select EMI to pay...</option>
                      {pendingEmis.map((emi) => (
                        <option key={emi.emiNumber} value={emi.emiNumber}>
                          EMI #{emi.emiNumber} - Due: {new Date(emi.dueDate).toLocaleDateString('en-IN')} - {formatINR(emi.totalEmi)}
                        </option>
                      ))}
                    </select>
                    {selectedEmi && (
                      <div className="mt-2 p-3 bg-blue-50 border border-blue-200 rounded-md">
                        <p className="text-sm font-medium text-blue-900">EMI #{selectedEmi.emiNumber} Breakdown</p>
                        <div className="grid grid-cols-2 gap-2 mt-1 text-xs text-blue-700">
                          <span>Principal: {formatINR(selectedEmi.principalAmount)}</span>
                          <span>Interest: {formatINR(selectedEmi.interestAmount)}</span>
                          <span>Total: {formatINR(selectedEmi.totalEmi)}</span>
                          <span>Due: {new Date(selectedEmi.dueDate).toLocaleDateString('en-IN')}</span>
                        </div>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="p-3 bg-gray-50 border border-gray-200 rounded-md">
                    <p className="text-sm text-gray-600">No pending EMIs for this loan.</p>
                  </div>
                )}

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Payment Date *</label>
                  <input
                    type="date"
                    className="w-full px-3 py-2 border border-gray-300 rounded-md"
                    value={paymentData.paymentDate}
                    onChange={(e) => setPaymentData({ ...paymentData, paymentDate: e.target.value })}
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Principal *</label>
                    <input
                      type="number"
                      step="0.01"
                      className="w-full px-3 py-2 border border-gray-300 rounded-md"
                      value={paymentData.principalAmount || ''}
                      onChange={(e) => {
                        const principal = parseFloat(e.target.value) || 0;
                        setPaymentData({
                          ...paymentData,
                          principalAmount: principal,
                          amount: principal + paymentData.interestAmount,
                        });
                      }}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Interest *</label>
                    <input
                      type="number"
                      step="0.01"
                      className="w-full px-3 py-2 border border-gray-300 rounded-md"
                      value={paymentData.interestAmount || ''}
                      onChange={(e) => {
                        const interest = parseFloat(e.target.value) || 0;
                        setPaymentData({
                          ...paymentData,
                          interestAmount: interest,
                          amount: paymentData.principalAmount + interest,
                        });
                      }}
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Total Amount</label>
                  <input
                    type="number"
                    step="0.01"
                    className="w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50"
                    value={paymentData.amount || ''}
                    readOnly
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Payment Method</label>
                  <select
                    className="w-full px-3 py-2 border border-gray-300 rounded-md"
                    value={paymentData.paymentMethod}
                    onChange={(e) => setPaymentData({ ...paymentData, paymentMethod: e.target.value as any })}
                  >
                    <option value="bank_transfer">Bank Transfer</option>
                    <option value="cheque">Cheque</option>
                    <option value="cash">Cash</option>
                    <option value="online">Online</option>
                    <option value="other">Other</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
                  <textarea
                    className="w-full px-3 py-2 border border-gray-300 rounded-md"
                    rows={2}
                    value={paymentData.notes || ''}
                    onChange={(e) => setPaymentData({ ...paymentData, notes: e.target.value })}
                  />
                </div>

                <Button
                  onClick={handlePayment}
                  disabled={recordPayment.isPending || !selectedEmiNumber || !paymentData.amount || paymentData.amount <= 0}
                  className="w-full"
                >
                  {recordPayment.isPending ? 'Recording...' : `Record EMI #${selectedEmiNumber || ''} Payment`}
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default EmiPaymentManagement;


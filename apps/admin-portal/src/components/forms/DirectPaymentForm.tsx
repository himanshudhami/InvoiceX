import { useState, useEffect, useMemo } from 'react';
import { Modal } from '@/components/ui/Modal';
import { useCompanies } from '@/hooks/api/useCompanies';
import { useCustomers } from '@/hooks/api/useCustomers';
import { useCreatePayment } from '@/hooks/api/usePayments';
import { CreatePaymentDto, TDS_SECTIONS, getFinancialYear, calculateTds } from '@/services/api/paymentService';
import { Company, Customer } from '@/services/api/types';
import { toInr, formatINR } from '@/lib/financialUtils';
import { Calendar, DollarSign, FileText, Building2, User, Receipt, AlertCircle } from 'lucide-react';

interface DirectPaymentFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

const PAYMENT_TYPES = [
  { value: 'direct_income', label: 'Direct Income' },
  { value: 'advance_received', label: 'Advance Received' },
  { value: 'refund_received', label: 'Refund Received' },
];

const INCOME_CATEGORIES = [
  { value: 'export_services', label: 'Export Services' },
  { value: 'domestic_services', label: 'Domestic Services' },
  { value: 'product_sale', label: 'Product Sale' },
  { value: 'interest', label: 'Interest Income' },
  { value: 'other', label: 'Other' },
];

const CURRENCIES = ['INR', 'USD', 'EUR', 'GBP', 'AUD', 'CAD', 'SGD', 'AED'];

const PAYMENT_METHODS = [
  { value: 'bank_transfer', label: 'Bank Transfer' },
  { value: 'upi', label: 'UPI' },
  { value: 'cheque', label: 'Cheque' },
  { value: 'cash', label: 'Cash' },
  { value: 'credit_card', label: 'Credit Card' },
  { value: 'paypal', label: 'PayPal' },
  { value: 'wise', label: 'Wise' },
  { value: 'other', label: 'Other' },
];

export const DirectPaymentForm = ({
  isOpen,
  onClose,
  onSuccess,
}: DirectPaymentFormProps) => {
  // Form state
  const [companyId, setCompanyId] = useState<string>('');
  const [customerId, setCustomerId] = useState<string>('');
  const [paymentDate, setPaymentDate] = useState(new Date().toISOString().split('T')[0]);
  const [currency, setCurrency] = useState('INR');
  const [grossAmount, setGrossAmount] = useState<number>(0);
  const [amountInInr, setAmountInInr] = useState<number | null>(null);
  const [paymentType, setPaymentType] = useState('direct_income');
  const [incomeCategory, setIncomeCategory] = useState('domestic_services');
  const [paymentMethod, setPaymentMethod] = useState('bank_transfer');
  const [referenceNumber, setReferenceNumber] = useState('');
  const [description, setDescription] = useState('');
  const [notes, setNotes] = useState('');

  // TDS state
  const [tdsApplicable, setTdsApplicable] = useState(false);
  const [tdsSection, setTdsSection] = useState<string>('');
  const [tdsRate, setTdsRate] = useState<number>(0);
  const [tdsAmount, setTdsAmount] = useState<number>(0);
  const [netAmount, setNetAmount] = useState<number>(0);

  const [error, setError] = useState<string | null>(null);

  // API hooks
  const { data: allCompanies = [] } = useCompanies();
  // Scope customers to selected company when set
  const { data: allCustomers = [] } = useCustomers(companyId || undefined);
  const createPayment = useCreatePayment();

  // Filter customers by selected company
  const companies: Company[] = allCompanies;
  const customers: Customer[] = useMemo(() => {
    if (!companyId) return [];
    return allCustomers.filter((c: Customer) => !c.companyId || c.companyId === companyId);
  }, [allCustomers, companyId]);

  // Calculate INR amount for foreign currencies
  useEffect(() => {
    if (currency !== 'INR' && grossAmount > 0) {
      const suggestedInr = toInr(grossAmount, currency);
      if (amountInInr === null) {
        setAmountInInr(suggestedInr);
      }
    } else if (currency === 'INR') {
      setAmountInInr(grossAmount);
    }
  }, [grossAmount, currency]);

  // Calculate TDS when applicable
  useEffect(() => {
    if (tdsApplicable && tdsRate > 0 && grossAmount > 0) {
      const { tdsAmount: calculatedTds, netAmount: calculatedNet } = calculateTds(grossAmount, tdsRate);
      setTdsAmount(calculatedTds);
      setNetAmount(calculatedNet);
    } else {
      setTdsAmount(0);
      setNetAmount(grossAmount);
    }
  }, [tdsApplicable, tdsRate, grossAmount]);

  // Update TDS rate when section changes
  useEffect(() => {
    if (tdsSection && TDS_SECTIONS[tdsSection as keyof typeof TDS_SECTIONS]) {
      setTdsRate(TDS_SECTIONS[tdsSection as keyof typeof TDS_SECTIONS].rate);
    } else {
      setTdsRate(0);
    }
  }, [tdsSection]);

  // Reset customer when company changes
  useEffect(() => {
    setCustomerId('');
  }, [companyId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!companyId) {
      setError('Please select a company');
      return;
    }

    if (!customerId) {
      setError('Please select a customer (required for TDS tracking)');
      return;
    }

    const paymentData: CreatePaymentDto = {
      companyId,
      customerId,
      paymentDate,
      amount: tdsApplicable ? netAmount : grossAmount, // Net amount received
      currency,
      paymentMethod,
      referenceNumber: referenceNumber || undefined,
      description: description || undefined,
      notes: notes || undefined,
      paymentType: paymentType as CreatePaymentDto['paymentType'],
      incomeCategory: incomeCategory as CreatePaymentDto['incomeCategory'],
      tdsApplicable,
      financialYear: getFinancialYear(new Date(paymentDate)),
    };

    // Add INR amount for foreign currencies
    if (currency !== 'INR' && amountInInr) {
      paymentData.amountInInr = amountInInr;
    }

    // Add TDS details if applicable
    if (tdsApplicable) {
      paymentData.tdsSection = tdsSection;
      paymentData.tdsRate = tdsRate;
      paymentData.tdsAmount = tdsAmount;
      paymentData.grossAmount = grossAmount;
    }

    try {
      await createPayment.mutateAsync(paymentData);
      onSuccess?.();
      onClose();
      resetForm();
    } catch (err: any) {
      setError(err.message || 'Failed to record payment');
    }
  };

  const resetForm = () => {
    setCompanyId('');
    setCustomerId('');
    setPaymentDate(new Date().toISOString().split('T')[0]);
    setCurrency('INR');
    setGrossAmount(0);
    setAmountInInr(null);
    setPaymentType('direct_income');
    setIncomeCategory('domestic_services');
    setPaymentMethod('bank_transfer');
    setReferenceNumber('');
    setDescription('');
    setNotes('');
    setTdsApplicable(false);
    setTdsSection('');
    setTdsRate(0);
    setTdsAmount(0);
    setNetAmount(0);
  };

  const isForeignCurrency = currency !== 'INR';
  const suggestedInr = grossAmount > 0 ? toInr(grossAmount, currency) : 0;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Record Direct Payment" size="lg">
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded flex items-center gap-2">
            <AlertCircle className="w-4 h-4" />
            {error}
          </div>
        )}

        <div className="bg-blue-50 border border-blue-200 text-blue-700 px-4 py-3 rounded text-sm">
          Record payments received directly (not linked to an invoice). This is useful for advance payments,
          direct income, or payments from India-based clients where TDS is deducted.
        </div>

        {/* Company & Customer Selection */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Building2 className="w-4 h-4 inline mr-1" />
              Company *
            </label>
            <select
              required
              value={companyId}
              onChange={(e) => setCompanyId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">Select company</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <User className="w-4 h-4 inline mr-1" />
              Customer/Payer *
            </label>
            <select
              required
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              disabled={!companyId}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100"
            >
              <option value="">Select customer</option>
              {customers.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.name} {customer.companyName ? `(${customer.companyName})` : ''}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Payment Classification */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Receipt className="w-4 h-4 inline mr-1" />
              Payment Type
            </label>
            <select
              value={paymentType}
              onChange={(e) => setPaymentType(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              {PAYMENT_TYPES.map((type) => (
                <option key={type.value} value={type.value}>
                  {type.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Income Category
            </label>
            <select
              value={incomeCategory}
              onChange={(e) => setIncomeCategory(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              {INCOME_CATEGORIES.map((cat) => (
                <option key={cat.value} value={cat.value}>
                  {cat.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Payment Date & Currency */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Calendar className="w-4 h-4 inline mr-1" />
              Payment Date *
            </label>
            <input
              type="date"
              required
              value={paymentDate}
              onChange={(e) => setPaymentDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Currency
            </label>
            <select
              value={currency}
              onChange={(e) => {
                setCurrency(e.target.value);
                setAmountInInr(null);
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              {CURRENCIES.map((curr) => (
                <option key={curr} value={curr}>
                  {curr}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Gross Amount */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            <DollarSign className="w-4 h-4 inline mr-1" />
            Gross Amount ({currency}) *
            <span className="text-xs text-gray-500 ml-2">
              (Amount before any TDS deduction)
            </span>
          </label>
          <input
            type="number"
            required
            min="0.01"
            step="0.01"
            value={grossAmount || ''}
            onChange={(e) => setGrossAmount(parseFloat(e.target.value) || 0)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="Enter gross amount"
          />
        </div>

        {/* INR Amount for foreign currencies */}
        {isForeignCurrency && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Amount Received in INR (&#8377;)
              <span className="text-xs text-gray-500 ml-2">
                (Actual amount credited to your bank)
              </span>
            </label>
            <input
              type="number"
              required
              min="0.01"
              step="0.01"
              value={amountInInr || ''}
              onChange={(e) => setAmountInInr(parseFloat(e.target.value) || null)}
              placeholder={`Suggested: ${formatINR(suggestedInr)}`}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
            {suggestedInr > 0 && amountInInr && Math.abs(amountInInr - suggestedInr) > 100 && (
              <p className="text-xs text-yellow-600 mt-1">
                Note: Amount differs from suggested conversion. Ensure this matches your bank statement.
              </p>
            )}
          </div>
        )}

        {/* TDS Section */}
        <div className="border rounded-lg p-4 bg-gray-50">
          <div className="flex items-center gap-2 mb-3">
            <input
              type="checkbox"
              id="tdsApplicable"
              checked={tdsApplicable}
              onChange={(e) => setTdsApplicable(e.target.checked)}
              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            <label htmlFor="tdsApplicable" className="text-sm font-medium text-gray-700">
              TDS was deducted by the payer
            </label>
          </div>

          {tdsApplicable && (
            <div className="space-y-3 mt-3 pt-3 border-t">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    TDS Section
                  </label>
                  <select
                    value={tdsSection}
                    onChange={(e) => setTdsSection(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  >
                    <option value="">Select section</option>
                    {Object.entries(TDS_SECTIONS).map(([section, info]) => (
                      <option key={section} value={section}>
                        {section} - {info.description} ({info.rate}%)
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    TDS Rate (%)
                  </label>
                  <input
                    type="number"
                    min="0"
                    max="100"
                    step="0.1"
                    value={tdsRate}
                    onChange={(e) => setTdsRate(parseFloat(e.target.value) || 0)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>
              </div>

              {/* TDS Calculation Summary */}
              {grossAmount > 0 && tdsRate > 0 && (
                <div className="bg-white p-3 rounded border">
                  <div className="grid grid-cols-3 gap-4 text-sm">
                    <div>
                      <span className="text-gray-600">Gross Amount:</span>
                      <span className="font-medium ml-2">{currency} {grossAmount.toLocaleString()}</span>
                    </div>
                    <div>
                      <span className="text-gray-600">TDS ({tdsRate}%):</span>
                      <span className="font-medium ml-2 text-red-600">- {currency} {tdsAmount.toLocaleString()}</span>
                    </div>
                    <div>
                      <span className="text-gray-600">Net Received:</span>
                      <span className="font-medium ml-2 text-green-600">{currency} {netAmount.toLocaleString()}</span>
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Payment Method & Reference */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Payment Method
            </label>
            <select
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              {PAYMENT_METHODS.map((method) => (
                <option key={method.value} value={method.value}>
                  {method.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Reference Number
            </label>
            <input
              type="text"
              value={referenceNumber}
              onChange={(e) => setReferenceNumber(e.target.value)}
              placeholder="UTR, Cheque No., Transaction ID"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>

        {/* Description */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            <FileText className="w-4 h-4 inline mr-1" />
            Description
          </label>
          <input
            type="text"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Brief description of what this payment is for"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Notes */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Additional Notes
          </label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            placeholder="Any additional notes..."
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Summary */}
        {grossAmount > 0 && (
          <div className="bg-green-50 border border-green-200 p-3 rounded-lg">
            <h4 className="font-medium text-green-800 mb-2">Payment Summary</h4>
            <div className="grid grid-cols-2 gap-2 text-sm text-green-700">
              <div>Financial Year: {getFinancialYear(new Date(paymentDate))}</div>
              <div>Net Amount: {currency} {(tdsApplicable ? netAmount : grossAmount).toLocaleString()}</div>
              {tdsApplicable && <div>TDS Deducted: {currency} {tdsAmount.toLocaleString()}</div>}
              {isForeignCurrency && amountInInr && <div>INR Received: {formatINR(amountInInr)}</div>}
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end space-x-3 pt-4 border-t">
          <button
            type="button"
            onClick={onClose}
            disabled={createPayment.isPending}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={createPayment.isPending || grossAmount <= 0 || !companyId || !customerId}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
          >
            {createPayment.isPending ? 'Recording...' : 'Record Payment'}
          </button>
        </div>
      </form>
    </Modal>
  );
};

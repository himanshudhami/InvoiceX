"use client"

import { useState, useEffect, useMemo } from 'react';
import { useCompanies } from '@/hooks/api/useCompanies';
import { useCustomers } from '@/features/customers/hooks';
import { useCreatePayment } from '@/hooks/api/usePayments';
import { useApplyTags } from '@/features/tags/hooks';
import { CreatePaymentDto, TDS_SECTIONS, getFinancialYear, calculateTds } from '@/services/api/finance/payments/paymentService';
import { Company, Customer } from '@/services/api/types';
import { toInr, formatINR } from '@/lib/financialUtils';
import { Calendar, DollarSign, AlertCircle, Tags } from 'lucide-react';
import { CompanySelect } from '@/components/ui/CompanySelect';
import { CustomerSelect } from '@/components/ui/CustomerSelect';
import { TagPicker } from '@/components/ui/TagPicker';

interface DirectPaymentFormProps {
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
  { value: 'check', label: 'Check' },
  { value: 'cash', label: 'Cash' },
  { value: 'credit_card', label: 'Credit Card' },
  { value: 'paypal', label: 'PayPal' },
  { value: 'other', label: 'Other' },
];

export const DirectPaymentForm = ({
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

  // Tags state
  const [selectedTags, setSelectedTags] = useState<{ tagId: string; allocationPercentage?: number }[]>([]);

  const [error, setError] = useState<string | null>(null);

  // API hooks
  const { data: allCompanies = [] } = useCompanies();
  // Scope customers to selected company when set
  const { data: allCustomers = [] } = useCustomers(companyId || undefined);
  const createPayment = useCreatePayment();
  const applyTags = useApplyTags();

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
      const created = await createPayment.mutateAsync(paymentData);

      // Apply tags if any selected
      if (selectedTags.length > 0 && created?.id) {
        await applyTags.mutateAsync({
          transactionId: created.id,
          transactionType: 'payment',
          tags: selectedTags.map(t => ({
            tagId: t.tagId,
            allocationPercentage: t.allocationPercentage,
          })),
        });
      }

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
    setSelectedTags([]);
  };

  const isForeignCurrency = currency !== 'INR';
  const suggestedInr = grossAmount > 0 ? toInr(grossAmount, currency) : 0;

  return (
    <div className="space-y-6">
      {error && (
        <div className="rounded-md bg-red-50 p-4 text-sm text-red-700 flex items-start gap-2">
          <AlertCircle className="h-4 w-4 mt-0.5" />
          <div>
            <p className="font-medium">Error</p>
            <p>{error}</p>
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Company *</label>
            <CompanySelect
              companies={companies}
              value={companyId}
              onChange={(val) => setCompanyId(val)}
              placeholder="Select company"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Customer *</label>
            <CustomerSelect
              customers={customers}
              value={customerId}
              onChange={(val) => setCustomerId(val)}
              placeholder="Select customer"
              disabled={!companyId}
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Payment Date *</label>
            <div className="relative">
              <Calendar className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
              <input
                type="date"
                value={paymentDate}
                onChange={(e) => setPaymentDate(e.target.value)}
                className="w-full rounded-md border border-gray-300 pl-10 pr-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Currency *</label>
            <select
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {CURRENCIES.map((cur) => (
                <option key={cur} value={cur}>
                  {cur}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Gross Amount *</label>
            <div className="relative">
              <DollarSign className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
              <input
                type="number"
                step="0.01"
                min="0"
                value={grossAmount}
                onChange={(e) => setGrossAmount(parseFloat(e.target.value) || 0)}
                className="w-full rounded-md border border-gray-300 pl-10 pr-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          {isForeignCurrency && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Amount (INR)</label>
              <input
                type="number"
                step="0.01"
                min="0"
                value={amountInInr ?? ''}
                onChange={(e) => setAmountInInr(parseFloat(e.target.value) || 0)}
                className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder={suggestedInr > 0 ? `Suggested: ${formatINR(suggestedInr)}` : ''}
              />
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Net Amount Received *</label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={tdsApplicable ? netAmount : grossAmount}
              onChange={(e) => setNetAmount(parseFloat(e.target.value) || 0)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              disabled={!tdsApplicable}
            />
            <p className="text-xs text-gray-500 mt-1">If TDS applies, net amount = gross - TDS.</p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Payment Type *</label>
            <select
              value={paymentType}
              onChange={(e) => setPaymentType(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {PAYMENT_TYPES.map((type) => (
                <option key={type.value} value={type.value}>
                  {type.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Income Category *</label>
            <select
              value={incomeCategory}
              onChange={(e) => setIncomeCategory(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {INCOME_CATEGORIES.map((category) => (
                <option key={category.value} value={category.value}>
                  {category.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Payment Method *</label>
            <select
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {PAYMENT_METHODS.map((method) => (
                <option key={method.value} value={method.value}>
                  {method.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Reference Number</label>
            <input
              type="text"
              value={referenceNumber}
              onChange={(e) => setReferenceNumber(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Transaction ID, UTR, etc."
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <input
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Short description"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Additional notes (optional)"
          />
        </div>

        {/* Tags */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1 flex items-center gap-1">
            <Tags className="w-4 h-4" />
            Tags
          </label>
          <TagPicker
            value={selectedTags}
            onChange={setSelectedTags}
            transactionAmount={grossAmount}
            placeholder="Add tags (department, project, client...)"
          />
        </div>

        <div className="flex items-center space-x-3">
          <label className="flex items-center space-x-2 text-sm text-gray-700">
            <input
              type="checkbox"
              checked={tdsApplicable}
              onChange={(e) => setTdsApplicable(e.target.checked)}
              className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
            />
            <span>TDS Applicable</span>
          </label>

          {tdsApplicable && (
            <select
              value={tdsSection}
              onChange={(e) => setTdsSection(e.target.value)}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">Select TDS Section</option>
              {Object.entries(TDS_SECTIONS).map(([key, value]) => (
                <option key={key} value={key}>
                  {key} - {value.description}
                </option>
              ))}
            </select>
          )}
        </div>

        {tdsApplicable && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">TDS Rate (%)</label>
              <input
                type="number"
                step="0.01"
                min="0"
                value={tdsRate}
                onChange={(e) => setTdsRate(parseFloat(e.target.value) || 0)}
                className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">TDS Amount</label>
              <input
                type="number"
                step="0.01"
                min="0"
                value={tdsAmount}
                readOnly
                className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm bg-gray-50 text-gray-700"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Net Amount After TDS</label>
              <input
                type="number"
                step="0.01"
                min="0"
                value={netAmount}
                readOnly
                className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm bg-gray-50 text-gray-700"
              />
            </div>
          </div>
        )}

        <div className="flex justify-end space-x-3 pt-4">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
            disabled={createPayment.isPending}
          >
            {createPayment.isPending ? 'Saving...' : 'Save Payment'}
          </button>
        </div>
      </form>
    </div>
  );
}

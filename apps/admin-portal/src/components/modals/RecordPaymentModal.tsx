import { useState, useEffect } from 'react';
import { Modal } from '@/components/ui/Modal';
import { Invoice } from '@/services/api/types';
import { formatCurrency } from '@/lib/currency';
import { toInr, formatINR } from '@/lib/financialUtils';
import { Calendar, DollarSign, FileText } from 'lucide-react';

interface RecordPaymentModalProps {
  isOpen: boolean;
  onClose: () => void;
  invoice: Invoice;
  onSuccess: () => void;
}

export const RecordPaymentModal = ({
  isOpen,
  onClose,
  invoice,
  onSuccess,
}: RecordPaymentModalProps) => {
  const [paymentDate, setPaymentDate] = useState(new Date().toISOString().split('T')[0]);
  const [amount, setAmount] = useState(invoice.totalAmount - (invoice.paidAmount || 0));
  const [amountInInr, setAmountInInr] = useState<number | null>(null);
  const [paymentMethod, setPaymentMethod] = useState('bank_transfer');
  const [referenceNumber, setReferenceNumber] = useState('');
  const [notes, setNotes] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Calculate suggested INR amount when amount or currency changes
  useEffect(() => {
    if (invoice.currency && invoice.currency.toUpperCase() !== 'INR' && amount > 0) {
      const suggestedInr = toInr(amount, invoice.currency);
      if (amountInInr === null) {
        setAmountInInr(suggestedInr);
      }
    } else if (invoice.currency?.toUpperCase() === 'INR') {
      setAmountInInr(amount);
    }
  }, [amount, invoice.currency]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const { invoiceService } = await import('@/services/api/billing/invoiceService');

      const paymentData = {
        amount: amount,
        paymentDate: paymentDate,
        paymentMethod: paymentMethod,
        referenceNumber: referenceNumber || undefined,
        notes: notes || undefined,
        amountInInr: undefined as number | undefined,
      };

      // Include INR amount if invoice currency is not INR
      if (invoice.currency && invoice.currency.toUpperCase() !== 'INR' && amountInInr) {
        paymentData.amountInInr = amountInInr;
      } else if (invoice.currency?.toUpperCase() === 'INR') {
        paymentData.amountInInr = amount;
      }

      await invoiceService.recordPayment(invoice.id, paymentData);
      onSuccess();
      onClose();

      // Reset form
      setAmount(invoice.totalAmount - (invoice.paidAmount || 0));
      setAmountInInr(null);
      setReferenceNumber('');
      setNotes('');
    } catch (err: any) {
      setError(err.message || 'Failed to record payment');
    } finally {
      setIsSubmitting(false);
    }
  };

  const isForeignCurrency = !!(invoice.currency && invoice.currency.toUpperCase() !== 'INR');
  const suggestedInr = invoice.currency ? toInr(amount, invoice.currency) : amount;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Record Payment" size="md">
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            {error}
          </div>
        )}

        {/* Invoice Info */}
        <div className="bg-gray-50 p-4 rounded-lg">
          <div className="flex justify-between items-center mb-2">
            <span className="text-sm text-gray-600">Invoice Number:</span>
            <span className="font-medium">{invoice.invoiceNumber}</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-sm text-gray-600">Total Amount:</span>
            <span className="font-medium">
              {formatCurrency(invoice.totalAmount, invoice.currency)}
            </span>
          </div>
          {invoice.paidAmount && invoice.paidAmount > 0 && (
            <div className="flex justify-between items-center mt-2 pt-2 border-t border-gray-200">
              <span className="text-sm text-gray-600">Already Paid:</span>
              <span className="font-medium text-green-600">
                {formatCurrency(invoice.paidAmount, invoice.currency)}
              </span>
            </div>
          )}
        </div>

        {/* Payment Date */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            <Calendar className="w-4 h-4 inline mr-1" />
            Payment Date
          </label>
          <input
            type="date"
            required
            value={paymentDate}
            onChange={(e) => setPaymentDate(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Payment Amount (in invoice currency) */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            <DollarSign className="w-4 h-4 inline mr-1" />
            Payment Amount ({invoice.currency || 'USD'})
          </label>
          <input
            type="number"
            required
            min="0.01"
            step="0.01"
            value={amount}
            onChange={(e) => {
              const newAmount = parseFloat(e.target.value) || 0;
              setAmount(newAmount);
              // Reset INR amount to let it recalculate
              if (isForeignCurrency) {
                setAmountInInr(null);
              }
            }}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* INR Amount (for foreign currency invoices) */}
        {isForeignCurrency && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <DollarSign className="w-4 h-4 inline mr-1" />
              Amount Received in INR (₹)
              <span className="text-xs text-gray-500 ml-2">
                (Required for accurate cash flow and bank reconciliation)
              </span>
            </label>
            <input
              type="number"
              required
              min="0.01"
              step="0.01"
              value={amountInInr || ''}
              onChange={(e) => setAmountInInr(parseFloat(e.target.value) || null)}
              placeholder={`Suggested: ${formatINR(suggestedInr)} (at 88/USD)`}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
            <p className="text-xs text-gray-500 mt-1">
              Enter the actual INR amount received in your bank account. This should match your bank statement.
            </p>
            {suggestedInr && amountInInr && Math.abs(amountInInr - suggestedInr) > 100 && (
              <p className="text-xs text-yellow-600 mt-1">
                ⚠️ Amount differs from suggested conversion. Ensure this matches your bank statement.
              </p>
            )}
          </div>
        )}

        {/* Payment Method */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Payment Method
          </label>
          <select
            required
            value={paymentMethod}
            onChange={(e) => setPaymentMethod(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="bank_transfer">Bank Transfer</option>
            <option value="cheque">Cheque</option>
            <option value="cash">Cash</option>
            <option value="credit_card">Credit Card</option>
            <option value="paypal">PayPal</option>
            <option value="other">Other</option>
          </select>
        </div>

        {/* Reference Number */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Reference Number
          </label>
          <input
            type="text"
            value={referenceNumber}
            onChange={(e) => setReferenceNumber(e.target.value)}
            placeholder="Transaction ID, Check number, etc."
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Notes */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            <FileText className="w-4 h-4 inline mr-1" />
            Notes
          </label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={3}
            placeholder="Additional payment details..."
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Actions */}
        <div className="flex justify-end space-x-3 pt-4 border-t">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting || amount <= 0 || (isForeignCurrency ? (!amountInInr || amountInInr <= 0) : false)}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
          >
            {isSubmitting ? 'Recording...' : 'Record Payment'}
          </button>
        </div>
      </form>
    </Modal>
  );
};





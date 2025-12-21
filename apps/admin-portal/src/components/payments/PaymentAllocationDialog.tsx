"use client"

import { useState, useEffect, useMemo } from 'react';
import { Modal } from '@/components/ui/Modal';
import { usePaymentAllocationSummary, useBulkAllocatePayment } from '@/hooks/api/usePaymentAllocations';
import { useCompanyInvoicePaymentStatus } from '@/hooks/api/usePaymentAllocations';
import { Payment, Invoice } from '@/services/api/types';
import { formatCurrency } from '@/lib/currency';
import { AlertCircle, Check, FileText, Plus, Trash2 } from 'lucide-react';
import { ALLOCATION_TYPE_LABELS, AllocationTypeEnum } from '@/services/api/paymentAllocationService';

interface PaymentAllocationDialogProps {
  isOpen: boolean;
  onClose: () => void;
  payment: Payment;
  onSuccess?: () => void;
}

interface AllocationRow {
  invoiceId: string;
  invoiceNumber: string;
  invoiceTotal: number;
  balanceDue: number;
  amount: number;
  tdsAmount: number;
  notes: string;
}

export const PaymentAllocationDialog = ({
  isOpen,
  onClose,
  payment,
  onSuccess,
}: PaymentAllocationDialogProps) => {
  const [error, setError] = useState<string | null>(null);
  const [allocations, setAllocations] = useState<AllocationRow[]>([]);

  // Fetch existing allocations for this payment
  const { data: allocationSummary, isLoading: summaryLoading } = usePaymentAllocationSummary(payment.id);

  // Fetch unpaid/partial invoices for the company
  const { data: invoiceStatuses = [], isLoading: invoicesLoading } = useCompanyInvoicePaymentStatus(
    payment.companyId || undefined
  );

  const bulkAllocate = useBulkAllocatePayment();

  // Calculate unallocated amount
  const unallocatedAmount = useMemo(() => {
    if (!allocationSummary) return payment.amount;
    return allocationSummary.unallocated;
  }, [allocationSummary, payment.amount]);

  // Calculate current allocation total
  const currentAllocationTotal = useMemo(() => {
    return allocations.reduce((sum, a) => sum + a.amount, 0);
  }, [allocations]);

  // Available unpaid/partial invoices (excluding already allocated)
  const availableInvoices = useMemo(() => {
    const allocatedInvoiceIds = new Set(allocations.map(a => a.invoiceId));
    const existingAllocationIds = new Set(
      allocationSummary?.allocations.map(a => a.invoiceId).filter(Boolean) || []
    );

    return invoiceStatuses.filter(inv =>
      inv.status !== 'paid' &&
      !allocatedInvoiceIds.has(inv.invoiceId) &&
      !existingAllocationIds.has(inv.invoiceId)
    );
  }, [invoiceStatuses, allocations, allocationSummary]);

  // Reset when dialog opens
  useEffect(() => {
    if (isOpen) {
      setAllocations([]);
      setError(null);
    }
  }, [isOpen]);

  const handleAddAllocation = (invoiceStatus: typeof invoiceStatuses[0]) => {
    const maxAmount = Math.min(invoiceStatus.balanceDue, unallocatedAmount - currentAllocationTotal);

    setAllocations([
      ...allocations,
      {
        invoiceId: invoiceStatus.invoiceId,
        invoiceNumber: invoiceStatus.invoiceNumber || 'N/A',
        invoiceTotal: invoiceStatus.invoiceTotal,
        balanceDue: invoiceStatus.balanceDue,
        amount: maxAmount,
        tdsAmount: 0,
        notes: '',
      },
    ]);
  };

  const handleRemoveAllocation = (index: number) => {
    setAllocations(allocations.filter((_, i) => i !== index));
  };

  const handleAmountChange = (index: number, value: number) => {
    const updated = [...allocations];
    const maxAmount = Math.min(updated[index].balanceDue, unallocatedAmount - currentAllocationTotal + updated[index].amount);
    updated[index].amount = Math.min(Math.max(0, value), maxAmount);
    setAllocations(updated);
  };

  const handleNotesChange = (index: number, value: string) => {
    const updated = [...allocations];
    updated[index].notes = value;
    setAllocations(updated);
  };

  const handleSubmit = async () => {
    setError(null);

    if (allocations.length === 0) {
      setError('Please add at least one invoice allocation');
      return;
    }

    if (currentAllocationTotal > unallocatedAmount) {
      setError('Total allocation exceeds unallocated amount');
      return;
    }

    try {
      await bulkAllocate.mutateAsync({
        companyId: payment.companyId || '',
        paymentId: payment.id,
        allocations: allocations.map(a => ({
          invoiceId: a.invoiceId,
          amount: a.amount,
          tdsAmount: a.tdsAmount,
          notes: a.notes || undefined,
        })),
      });
      onSuccess?.();
      onClose();
    } catch (err: any) {
      setError(err.message || 'Failed to allocate payment');
    }
  };

  const isLoading = summaryLoading || invoicesLoading;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Allocate Payment to Invoices" size="lg">
      <div className="space-y-6">
        {error && (
          <div className="rounded-md bg-red-50 p-4 text-sm text-red-700 flex items-start gap-2">
            <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
            <div>{error}</div>
          </div>
        )}

        {/* Payment Summary */}
        <div className="bg-gray-50 rounded-lg p-4">
          <h3 className="text-sm font-medium text-gray-700 mb-3">Payment Details</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <span className="text-gray-500">Total Amount</span>
              <p className="font-semibold">{formatCurrency(payment.amount, payment.currency)}</p>
            </div>
            <div>
              <span className="text-gray-500">Already Allocated</span>
              <p className="font-semibold text-blue-600">
                {formatCurrency(payment.amount - unallocatedAmount, payment.currency)}
              </p>
            </div>
            <div>
              <span className="text-gray-500">Unallocated</span>
              <p className="font-semibold text-green-600">
                {formatCurrency(unallocatedAmount, payment.currency)}
              </p>
            </div>
            <div>
              <span className="text-gray-500">This Allocation</span>
              <p className="font-semibold text-orange-600">
                {formatCurrency(currentAllocationTotal, payment.currency)}
              </p>
            </div>
          </div>
        </div>

        {/* Existing Allocations */}
        {allocationSummary && allocationSummary.allocations.length > 0 && (
          <div className="border rounded-lg p-4">
            <h3 className="text-sm font-medium text-gray-700 mb-3">Existing Allocations</h3>
            <div className="space-y-2">
              {allocationSummary.allocations.map((alloc) => (
                <div key={alloc.id} className="flex items-center justify-between text-sm bg-gray-50 rounded px-3 py-2">
                  <div className="flex items-center gap-2">
                    <Check className="h-4 w-4 text-green-500" />
                    <span>{alloc.invoiceNumber || 'Direct Allocation'}</span>
                  </div>
                  <span className="font-medium">{formatCurrency(alloc.allocatedAmount, payment.currency)}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* New Allocations */}
        <div className="border rounded-lg p-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-medium text-gray-700">New Allocations</h3>
            {unallocatedAmount - currentAllocationTotal > 0 && availableInvoices.length > 0 && (
              <span className="text-xs text-gray-500">
                Remaining: {formatCurrency(unallocatedAmount - currentAllocationTotal, payment.currency)}
              </span>
            )}
          </div>

          {allocations.length === 0 ? (
            <p className="text-sm text-gray-500 text-center py-4">
              No allocations added yet. Select an invoice below to allocate.
            </p>
          ) : (
            <div className="space-y-3">
              {allocations.map((alloc, index) => (
                <div key={alloc.invoiceId} className="border rounded-lg p-3 bg-blue-50">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-2">
                        <FileText className="h-4 w-4 text-blue-500" />
                        <span className="font-medium text-sm">{alloc.invoiceNumber}</span>
                        <span className="text-xs text-gray-500">
                          (Total: {formatCurrency(alloc.invoiceTotal, payment.currency)} | Due: {formatCurrency(alloc.balanceDue, payment.currency)})
                        </span>
                      </div>
                      <div className="grid grid-cols-2 gap-3">
                        <div>
                          <label className="block text-xs text-gray-600 mb-1">Amount to Allocate</label>
                          <input
                            type="number"
                            step="0.01"
                            min="0"
                            max={alloc.balanceDue}
                            value={alloc.amount}
                            onChange={(e) => handleAmountChange(index, parseFloat(e.target.value) || 0)}
                            className="w-full rounded-md border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-gray-600 mb-1">Notes (optional)</label>
                          <input
                            type="text"
                            value={alloc.notes}
                            onChange={(e) => handleNotesChange(index, e.target.value)}
                            className="w-full rounded-md border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                            placeholder="Optional notes"
                          />
                        </div>
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => handleRemoveAllocation(index)}
                      className="ml-2 p-1.5 text-red-500 hover:bg-red-100 rounded"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Available Invoices to Allocate */}
        {unallocatedAmount - currentAllocationTotal > 0 && (
          <div className="border rounded-lg p-4">
            <h3 className="text-sm font-medium text-gray-700 mb-3">Available Invoices</h3>
            {isLoading ? (
              <p className="text-sm text-gray-500 text-center py-4">Loading invoices...</p>
            ) : availableInvoices.length === 0 ? (
              <p className="text-sm text-gray-500 text-center py-4">
                No unpaid invoices available for allocation
              </p>
            ) : (
              <div className="max-h-48 overflow-y-auto space-y-2">
                {availableInvoices.map((inv) => (
                  <div
                    key={inv.invoiceId}
                    className="flex items-center justify-between text-sm border rounded px-3 py-2 hover:bg-gray-50"
                  >
                    <div>
                      <span className="font-medium">{inv.invoiceNumber}</span>
                      <span className="text-gray-500 ml-2">
                        Due: {formatCurrency(inv.balanceDue, payment.currency)} of {formatCurrency(inv.invoiceTotal, payment.currency)}
                      </span>
                      <span className={`ml-2 text-xs px-2 py-0.5 rounded ${
                        inv.status === 'partial'
                          ? 'bg-yellow-100 text-yellow-800'
                          : 'bg-red-100 text-red-800'
                      }`}>
                        {inv.status}
                      </span>
                    </div>
                    <button
                      type="button"
                      onClick={() => handleAddAllocation(inv)}
                      className="flex items-center gap-1 px-2 py-1 text-blue-600 hover:bg-blue-100 rounded text-xs"
                    >
                      <Plus className="h-3 w-3" />
                      Add
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end space-x-3 pt-4 border-t">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            disabled={allocations.length === 0 || bulkAllocate.isPending}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {bulkAllocate.isPending ? 'Saving...' : `Allocate ${formatCurrency(currentAllocationTotal, payment.currency)}`}
          </button>
        </div>
      </div>
    </Modal>
  );
};

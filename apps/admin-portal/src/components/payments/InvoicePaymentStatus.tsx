"use client"

import { useInvoicePaymentStatus, useAllocationsByInvoice } from '@/hooks/api/usePaymentAllocations';
import { formatCurrency } from '@/lib/currency';
import { Check, AlertCircle, Clock, DollarSign } from 'lucide-react';

interface InvoicePaymentStatusProps {
  invoiceId: string;
  showDetails?: boolean;
  currency?: string;
}

export const InvoicePaymentStatus = ({
  invoiceId,
  showDetails = false,
  currency = 'INR',
}: InvoicePaymentStatusProps) => {
  const { data: status, isLoading: statusLoading } = useInvoicePaymentStatus(invoiceId);
  const { data: allocations = [], isLoading: allocationsLoading } = useAllocationsByInvoice(
    showDetails ? invoiceId : undefined
  );

  const isLoading = statusLoading || (showDetails && allocationsLoading);

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 text-gray-400 text-sm">
        <Clock className="h-4 w-4 animate-pulse" />
        <span>Loading...</span>
      </div>
    );
  }

  if (!status) {
    return (
      <div className="flex items-center gap-2 text-gray-400 text-sm">
        <Clock className="h-4 w-4" />
        <span>No payment data</span>
      </div>
    );
  }

  const statusConfig = {
    paid: {
      icon: Check,
      text: 'Paid',
      bgColor: 'bg-green-100',
      textColor: 'text-green-700',
      iconColor: 'text-green-500',
    },
    partial: {
      icon: AlertCircle,
      text: 'Partial',
      bgColor: 'bg-yellow-100',
      textColor: 'text-yellow-700',
      iconColor: 'text-yellow-500',
    },
    unpaid: {
      icon: Clock,
      text: 'Unpaid',
      bgColor: 'bg-red-100',
      textColor: 'text-red-700',
      iconColor: 'text-red-500',
    },
  };

  const config = statusConfig[status.status] || statusConfig.unpaid;
  const Icon = config.icon;

  return (
    <div className="space-y-2">
      {/* Status Badge */}
      <div className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${config.bgColor} ${config.textColor}`}>
        <Icon className={`h-3.5 w-3.5 ${config.iconColor}`} />
        <span>{config.text}</span>
      </div>

      {/* Payment Summary */}
      <div className="flex items-center gap-4 text-xs text-gray-600">
        <span>
          Paid: <strong className="text-green-600">{formatCurrency(status.totalPaid, currency)}</strong>
        </span>
        {status.balanceDue > 0.01 && (
          <span>
            Due: <strong className="text-red-600">{formatCurrency(status.balanceDue, currency)}</strong>
          </span>
        )}
        {status.paymentCount > 0 && (
          <span className="text-gray-500">
            ({status.paymentCount} payment{status.paymentCount !== 1 ? 's' : ''})
          </span>
        )}
      </div>

      {/* Payment Details */}
      {showDetails && allocations.length > 0 && (
        <div className="mt-2 space-y-1.5 border-l-2 border-gray-200 pl-3">
          {allocations.map((alloc) => (
            <div key={alloc.id} className="flex items-center justify-between text-xs">
              <div className="flex items-center gap-1.5">
                <DollarSign className="h-3 w-3 text-gray-400" />
                <span className="text-gray-400">
                  {new Date(alloc.allocationDate).toLocaleDateString()}
                </span>
                {alloc.notes && (
                  <span className="text-gray-500 truncate max-w-[150px]" title={alloc.notes}>
                    - {alloc.notes}
                  </span>
                )}
              </div>
              <span className="font-medium text-gray-900">{formatCurrency(alloc.allocatedAmount, currency)}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

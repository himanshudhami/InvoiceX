"use client"

import { useState } from 'react';
import { usePaymentAllocationSummary } from '@/hooks/api/usePaymentAllocations';
import { Payment } from '@/services/api/types';
import { formatINR } from '@/lib/financialUtils';
import { Check, AlertCircle, Clock, ChevronDown, ChevronUp, FileText } from 'lucide-react';
import { PaymentAllocationDialog } from './PaymentAllocationDialog';

interface PaymentAllocationStatusProps {
  payment: Payment;
  showDetails?: boolean;
  onAllocationSuccess?: () => void;
}

export const PaymentAllocationStatus = ({
  payment,
  showDetails = false,
  onAllocationSuccess,
}: PaymentAllocationStatusProps) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [showDialog, setShowDialog] = useState(false);

  const { data: summary, isLoading } = usePaymentAllocationSummary(payment.id);

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 text-gray-400 text-sm">
        <Clock className="h-4 w-4 animate-pulse" />
        <span>Loading...</span>
      </div>
    );
  }

  const totalAllocated = summary?.totalAllocated || 0;
  const unallocated = summary?.unallocated ?? payment.amount;
  const allocationCount = summary?.allocationCount || 0;

  // Determine status
  const isFullyAllocated = unallocated <= 0.01;
  const isPartiallyAllocated = totalAllocated > 0 && !isFullyAllocated;
  const isUnallocated = totalAllocated === 0;

  const statusConfig = {
    fully: {
      icon: Check,
      text: 'Fully Allocated',
      bgColor: 'bg-green-100',
      textColor: 'text-green-700',
      iconColor: 'text-green-500',
    },
    partial: {
      icon: AlertCircle,
      text: 'Partially Allocated',
      bgColor: 'bg-yellow-100',
      textColor: 'text-yellow-700',
      iconColor: 'text-yellow-500',
    },
    unallocated: {
      icon: Clock,
      text: 'Unallocated',
      bgColor: 'bg-gray-100',
      textColor: 'text-gray-700',
      iconColor: 'text-gray-500',
    },
  };

  const status = isFullyAllocated ? 'fully' : isPartiallyAllocated ? 'partial' : 'unallocated';
  const config = statusConfig[status];
  const Icon = config.icon;

  return (
    <>
      <div className="space-y-2">
        {/* Status Badge */}
        <div className="flex items-center justify-between">
          <div className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${config.bgColor} ${config.textColor}`}>
            <Icon className={`h-3.5 w-3.5 ${config.iconColor}`} />
            <span>{config.text}</span>
          </div>

          {!isFullyAllocated && (
            <button
              onClick={() => setShowDialog(true)}
              className="text-xs text-blue-600 hover:text-blue-800 font-medium"
            >
              Allocate
            </button>
          )}
        </div>

        {/* Summary */}
        <div className="flex items-center gap-4 text-xs text-gray-600">
          <span>
            Allocated: <strong className="text-gray-900">{formatINR(totalAllocated)}</strong>
          </span>
          {unallocated > 0.01 && (
            <span>
              Remaining: <strong className="text-orange-600">{formatINR(unallocated)}</strong>
            </span>
          )}
          {allocationCount > 0 && (
            <span className="text-gray-500">
              ({allocationCount} invoice{allocationCount !== 1 ? 's' : ''})
            </span>
          )}
        </div>

        {/* Expandable Details */}
        {showDetails && allocationCount > 0 && (
          <div>
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="flex items-center gap-1 text-xs text-gray-500 hover:text-gray-700"
            >
              {isExpanded ? (
                <>
                  <ChevronUp className="h-3 w-3" />
                  Hide details
                </>
              ) : (
                <>
                  <ChevronDown className="h-3 w-3" />
                  Show details
                </>
              )}
            </button>

            {isExpanded && summary?.allocations && (
              <div className="mt-2 space-y-1.5 border-l-2 border-gray-200 pl-3">
                {summary.allocations.map((alloc) => (
                  <div key={alloc.id} className="flex items-center justify-between text-xs">
                    <div className="flex items-center gap-1.5">
                      <FileText className="h-3 w-3 text-gray-400" />
                      <span className="text-gray-700">{alloc.invoiceNumber || 'Direct'}</span>
                      <span className="text-gray-400">
                        ({new Date(alloc.allocationDate).toLocaleDateString()})
                      </span>
                    </div>
                    <span className="font-medium text-gray-900">{formatINR(alloc.allocatedAmount)}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Allocation Dialog */}
      <PaymentAllocationDialog
        isOpen={showDialog}
        onClose={() => setShowDialog(false)}
        payment={payment}
        onSuccess={() => {
          setShowDialog(false);
          onAllocationSuccess?.();
        }}
      />
    </>
  );
};

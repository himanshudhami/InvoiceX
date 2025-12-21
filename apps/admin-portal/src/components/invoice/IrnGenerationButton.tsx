import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { eInvoiceService } from '@/services/api/finance/tax/eInvoiceService';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Loader2, FileCheck, AlertCircle, CheckCircle, XCircle } from 'lucide-react';
import toast from 'react-hot-toast';

interface IrnGenerationButtonProps {
  invoiceId: string;
  invoiceNumber: string;
  eInvoiceStatus: string | null | undefined;
  irn?: string | null;
  onSuccess?: () => void;
  variant?: 'default' | 'outline' | 'ghost' | 'secondary';
  size?: 'default' | 'sm' | 'lg' | 'icon';
}

export function IrnGenerationButton({
  invoiceId,
  invoiceNumber,
  eInvoiceStatus,
  irn,
  onSuccess,
  variant = 'default',
  size = 'default',
}: IrnGenerationButtonProps) {
  const queryClient = useQueryClient();
  const [showDialog, setShowDialog] = useState(false);
  const [cancelDialog, setCancelDialog] = useState(false);
  const [cancelReason, setCancelReason] = useState('');

  const normalizedStatus = (eInvoiceStatus || 'not_applicable').toLowerCase();

  // Check applicability
  const checkApplicabilityMutation = useMutation({
    mutationFn: () => eInvoiceService.checkApplicability(invoiceId),
    onSuccess: (data) => {
      if (data.isApplicable) {
        setShowDialog(true);
      } else {
        toast.error('E-Invoice is not applicable for this invoice');
      }
    },
    onError: (error: Error) => {
      toast.error(`Failed to check applicability: ${error.message}`);
    },
  });

  // Generate IRN mutation
  const generateMutation = useMutation({
    mutationFn: () => eInvoiceService.generateIrn(invoiceId),
    onSuccess: (data) => {
      setShowDialog(false);
      queryClient.invalidateQueries({ queryKey: ['invoice', invoiceId] });
      queryClient.invalidateQueries({ queryKey: ['invoices'] });

      if (data.success) {
        toast.success(`IRN generated successfully!\nIRN: ${data.irn?.substring(0, 20)}...`);
      } else {
        toast.error(`Failed to generate IRN: ${data.errorMessage}`);
      }

      onSuccess?.();
    },
    onError: (error: Error) => {
      toast.error(`Failed to generate IRN: ${error.message}`);
    },
  });

  // Cancel IRN mutation
  const cancelMutation = useMutation({
    mutationFn: () => eInvoiceService.cancelIrn(invoiceId, cancelReason),
    onSuccess: (data) => {
      setCancelDialog(false);
      setCancelReason('');
      queryClient.invalidateQueries({ queryKey: ['invoice', invoiceId] });
      queryClient.invalidateQueries({ queryKey: ['invoices'] });

      if (data.success) {
        toast.success('IRN cancelled successfully');
      } else {
        toast.error(`Failed to cancel IRN: ${data.message}`);
      }

      onSuccess?.();
    },
    onError: (error: Error) => {
      toast.error(`Failed to cancel IRN: ${error.message}`);
    },
  });

  // Queue for generation mutation
  const queueMutation = useMutation({
    mutationFn: () => eInvoiceService.queueForGeneration(invoiceId),
    onSuccess: () => {
      setShowDialog(false);
      queryClient.invalidateQueries({ queryKey: ['einvoice-queue'] });
      toast.success('Invoice added to e-invoice processing queue');
      onSuccess?.();
    },
    onError: (error: Error) => {
      toast.error(`Failed to queue: ${error.message}`);
    },
  });

  const handleClick = () => {
    if (normalizedStatus === 'generated' && irn) {
      // Show cancel option
      setCancelDialog(true);
    } else {
      // Check applicability first
      checkApplicabilityMutation.mutate();
    }
  };

  const getButtonContent = () => {
    if (checkApplicabilityMutation.isPending) {
      return (
        <>
          <Loader2 className="h-4 w-4 animate-spin mr-2" />
          Checking...
        </>
      );
    }

    switch (normalizedStatus) {
      case 'generated':
        return (
          <>
            <CheckCircle className="h-4 w-4 mr-2" />
            IRN Generated
          </>
        );
      case 'cancelled':
        return (
          <>
            <XCircle className="h-4 w-4 mr-2" />
            IRN Cancelled
          </>
        );
      case 'pending':
        return (
          <>
            <Loader2 className="h-4 w-4 animate-spin mr-2" />
            Processing...
          </>
        );
      case 'error':
      case 'failed':
        return (
          <>
            <AlertCircle className="h-4 w-4 mr-2" />
            Retry IRN
          </>
        );
      default:
        return (
          <>
            <FileCheck className="h-4 w-4 mr-2" />
            Generate IRN
          </>
        );
    }
  };

  const isDisabled =
    normalizedStatus === 'pending' ||
    checkApplicabilityMutation.isPending ||
    generateMutation.isPending;

  return (
    <>
      <Button
        variant={normalizedStatus === 'generated' ? 'outline' : variant}
        size={size}
        onClick={handleClick}
        disabled={isDisabled}
      >
        {getButtonContent()}
      </Button>

      {/* Generate IRN Dialog */}
      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Generate E-Invoice IRN</DialogTitle>
            <DialogDescription>
              Generate an Invoice Reference Number (IRN) for invoice {invoiceNumber}
            </DialogDescription>
          </DialogHeader>

          <Alert>
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Important</AlertTitle>
            <AlertDescription>
              Once generated, the IRN is valid for 24 hours for cancellation. After that, you will
              need to issue a credit note to reverse this invoice.
            </AlertDescription>
          </Alert>

          <DialogFooter className="gap-2">
            <Button variant="outline" onClick={() => setShowDialog(false)}>
              Cancel
            </Button>
            <Button
              variant="secondary"
              onClick={() => queueMutation.mutate()}
              disabled={queueMutation.isPending}
            >
              {queueMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : null}
              Add to Queue
            </Button>
            <Button
              onClick={() => generateMutation.mutate()}
              disabled={generateMutation.isPending}
            >
              {generateMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <FileCheck className="h-4 w-4 mr-2" />
              )}
              Generate Now
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Cancel IRN Dialog */}
      <Dialog open={cancelDialog} onOpenChange={setCancelDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Cancel E-Invoice IRN</DialogTitle>
            <DialogDescription>
              Cancel the IRN for invoice {invoiceNumber}. This action can only be performed within
              24 hours of IRN generation.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertTitle>Warning</AlertTitle>
              <AlertDescription>
                Cancelling an IRN is irreversible. You will need to create a new invoice with a new
                number if you want to re-issue.
              </AlertDescription>
            </Alert>

            <div className="space-y-2">
              <label className="text-sm font-medium">Cancellation Reason</label>
              <select
                className="w-full border rounded-md p-2"
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
              >
                <option value="">Select a reason</option>
                <option value="1">Duplicate Invoice</option>
                <option value="2">Data Entry Mistake</option>
                <option value="3">Order Cancelled</option>
                <option value="4">Other</option>
              </select>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelDialog(false)}>
              Keep IRN
            </Button>
            <Button
              variant="destructive"
              onClick={() => cancelMutation.mutate()}
              disabled={!cancelReason || cancelMutation.isPending}
            >
              {cancelMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <XCircle className="h-4 w-4 mr-2" />
              )}
              Cancel IRN
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

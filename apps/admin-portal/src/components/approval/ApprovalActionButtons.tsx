import { useState } from 'react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { CheckCircle, XCircle, Loader2 } from 'lucide-react';
import { useApproveRequest, useRejectRequest } from '@/hooks/api/useApprovalWorkflow';

interface ApprovalActionButtonsProps {
  requestId: string;
  approverId: string;
  onSuccess?: () => void;
  size?: 'sm' | 'md' | 'lg';
  showLabels?: boolean;
}

export function ApprovalActionButtons({
  requestId,
  approverId,
  onSuccess,
  size = 'md',
  showLabels = true,
}: ApprovalActionButtonsProps) {
  const [isApproveOpen, setIsApproveOpen] = useState(false);
  const [isRejectOpen, setIsRejectOpen] = useState(false);
  const [approveComments, setApproveComments] = useState('');
  const [rejectReason, setRejectReason] = useState('');

  const approveMutation = useApproveRequest();
  const rejectMutation = useRejectRequest();

  const handleApprove = async () => {
    await approveMutation.mutateAsync({
      requestId,
      approverId,
      data: { comments: approveComments || undefined },
    });
    setIsApproveOpen(false);
    setApproveComments('');
    onSuccess?.();
  };

  const handleReject = async () => {
    if (!rejectReason.trim()) return;
    await rejectMutation.mutateAsync({
      requestId,
      approverId,
      data: { reason: rejectReason },
    });
    setIsRejectOpen(false);
    setRejectReason('');
    onSuccess?.();
  };

  const buttonSize = size === 'sm' ? 'sm' : size === 'lg' ? 'lg' : 'default';

  return (
    <>
      <div className="flex gap-2">
        <Button
          size={buttonSize}
          onClick={() => setIsApproveOpen(true)}
          className="bg-green-600 hover:bg-green-700"
          disabled={approveMutation.isPending || rejectMutation.isPending}
        >
          <CheckCircle className="h-4 w-4" />
          {showLabels && <span className="ml-1">Approve</span>}
        </Button>
        <Button
          size={buttonSize}
          variant="destructive"
          onClick={() => setIsRejectOpen(true)}
          disabled={approveMutation.isPending || rejectMutation.isPending}
        >
          <XCircle className="h-4 w-4" />
          {showLabels && <span className="ml-1">Reject</span>}
        </Button>
      </div>

      {/* Approve Dialog */}
      <Dialog open={isApproveOpen} onOpenChange={setIsApproveOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Approve Request</DialogTitle>
            <DialogDescription>
              Are you sure you want to approve this request? You can optionally add comments.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Label htmlFor="approve-comments">Comments (optional)</Label>
            <Textarea
              id="approve-comments"
              value={approveComments}
              onChange={(e) => setApproveComments(e.target.value)}
              placeholder="Add any comments..."
              className="mt-2"
              rows={3}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsApproveOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleApprove}
              disabled={approveMutation.isPending}
              className="bg-green-600 hover:bg-green-700"
            >
              {approveMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Approve
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reject Dialog */}
      <Dialog open={isRejectOpen} onOpenChange={setIsRejectOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reject Request</DialogTitle>
            <DialogDescription>
              Please provide a reason for rejecting this request.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Label htmlFor="reject-reason">Reason *</Label>
            <Textarea
              id="reject-reason"
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              placeholder="Enter rejection reason..."
              className="mt-2"
              rows={3}
              required
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsRejectOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleReject}
              disabled={rejectMutation.isPending || !rejectReason.trim()}
            >
              {rejectMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Reject
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

import { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { useCancelSubscription } from '@/hooks/api/useSubscriptions';
import { Subscription } from '@/services/api/types';

interface CancelSubscriptionModalProps {
  subscription: Subscription;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const CancelSubscriptionModal = ({
  subscription,
  isOpen,
  onClose,
  onSuccess,
}: CancelSubscriptionModalProps) => {
  const cancelSub = useCancelSubscription();
  const [cancelledOn, setCancelledOn] = useState(new Date().toISOString().split('T')[0]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await cancelSub.mutateAsync({ id: subscription.id, cancelledOn });
      onSuccess();
      onClose();
    } catch (err) {
      console.error('Failed to cancel subscription:', err);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Cancel ${subscription.name}`} size="md">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <p className="text-sm text-gray-600 mb-4">
            Cancelling this subscription will stop all future cost accrual. This action cannot be undone.
          </p>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Cancellation Date
          </label>
          <input
            type="date"
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={cancelledOn}
            onChange={(e) => setCancelledOn(e.target.value)}
            required
          />
        </div>
        <div className="flex justify-end space-x-3 pt-4">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 rounded-md border border-gray-300 text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={cancelSub.isPending}
            className="px-4 py-2 rounded-md bg-red-600 text-white hover:bg-red-700 disabled:opacity-60"
          >
            {cancelSub.isPending ? 'Cancelling...' : 'Cancel Subscription'}
          </button>
        </div>
      </form>
    </Modal>
  );
};





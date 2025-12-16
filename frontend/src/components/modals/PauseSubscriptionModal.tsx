import { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { usePauseSubscription } from '@/hooks/api/useSubscriptions';
import { Subscription } from '@/services/api/types';

interface PauseSubscriptionModalProps {
  subscription: Subscription;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const PauseSubscriptionModal = ({
  subscription,
  isOpen,
  onClose,
  onSuccess,
}: PauseSubscriptionModalProps) => {
  const pauseSub = usePauseSubscription();
  const [pausedOn, setPausedOn] = useState(new Date().toISOString().split('T')[0]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await pauseSub.mutateAsync({ id: subscription.id, pausedOn });
      onSuccess();
      onClose();
    } catch (err) {
      console.error('Failed to pause subscription:', err);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Pause ${subscription.name}`} size="md">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <p className="text-sm text-gray-600 mb-4">
            Pausing this subscription will stop cost accrual. You can resume it later.
          </p>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Pause Date
          </label>
          <input
            type="date"
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={pausedOn}
            onChange={(e) => setPausedOn(e.target.value)}
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
            disabled={pauseSub.isPending}
            className="px-4 py-2 rounded-md bg-amber-600 text-white hover:bg-amber-700 disabled:opacity-60"
          >
            {pauseSub.isPending ? 'Pausing...' : 'Pause Subscription'}
          </button>
        </div>
      </form>
    </Modal>
  );
};





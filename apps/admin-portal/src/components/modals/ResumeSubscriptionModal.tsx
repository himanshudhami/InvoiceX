import { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { useResumeSubscription } from '@/hooks/api/useSubscriptions';
import { Subscription } from '@/services/api/types';

interface ResumeSubscriptionModalProps {
  subscription: Subscription;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const ResumeSubscriptionModal = ({
  subscription,
  isOpen,
  onClose,
  onSuccess,
}: ResumeSubscriptionModalProps) => {
  const resumeSub = useResumeSubscription();
  const [resumedOn, setResumedOn] = useState(new Date().toISOString().split('T')[0]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await resumeSub.mutateAsync({ id: subscription.id, resumedOn });
      onSuccess();
      onClose();
    } catch (err) {
      console.error('Failed to resume subscription:', err);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Resume ${subscription.name}`} size="md">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <p className="text-sm text-gray-600 mb-4">
            Resuming this subscription will restart cost accrual from the resume date.
          </p>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Resume Date
          </label>
          <input
            type="date"
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={resumedOn}
            onChange={(e) => setResumedOn(e.target.value)}
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
            disabled={resumeSub.isPending}
            className="px-4 py-2 rounded-md bg-green-600 text-white hover:bg-green-700 disabled:opacity-60"
          >
            {resumeSub.isPending ? 'Resuming...' : 'Resume Subscription'}
          </button>
        </div>
      </form>
    </Modal>
  );
};






import { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { useResignEmployee } from '@/hooks/api/useEmployees';
import { Employee } from '@/services/api/types';
import { Textarea } from '@/components/ui/textarea';

interface ResignEmployeeModalProps {
  employee: Employee;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const ResignEmployeeModal = ({
  employee,
  isOpen,
  onClose,
  onSuccess,
}: ResignEmployeeModalProps) => {
  const resignEmployee = useResignEmployee();
  const [lastWorkingDay, setLastWorkingDay] = useState(new Date().toISOString().split('T')[0]);
  const [resignationReason, setResignationReason] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await resignEmployee.mutateAsync({
        id: employee.id,
        data: {
          lastWorkingDay,
          resignationReason: resignationReason || undefined,
        },
      });
      onSuccess();
      onClose();
    } catch (err) {
      console.error('Failed to resign employee:', err);
    }
  };

  const handleClose = () => {
    setLastWorkingDay(new Date().toISOString().split('T')[0]);
    setResignationReason('');
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title={`Resign ${employee.employeeName}`} size="md">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <p className="text-sm text-gray-600 mb-4">
            This will mark {employee.employeeName} as resigned and exclude them from future payroll runs.
            They will still be included in payroll for months up to their last working day.
          </p>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Last Working Day <span className="text-red-500">*</span>
          </label>
          <input
            type="date"
            className="w-full rounded-md border border-gray-300 px-3 py-2 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
            value={lastWorkingDay}
            onChange={(e) => setLastWorkingDay(e.target.value)}
            required
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Resignation Reason (Optional)
          </label>
          <Textarea
            className="w-full rounded-md border border-gray-300 px-3 py-2 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
            value={resignationReason}
            onChange={(e) => setResignationReason(e.target.value)}
            placeholder="e.g., Personal reasons, Career change, Relocation..."
            rows={3}
          />
        </div>
        <div className="flex justify-end space-x-3 pt-4">
          <button
            type="button"
            onClick={handleClose}
            className="px-4 py-2 rounded-md border border-gray-300 text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={resignEmployee.isPending}
            className="px-4 py-2 rounded-md bg-red-600 text-white hover:bg-red-700 disabled:opacity-60"
          >
            {resignEmployee.isPending ? 'Processing...' : 'Resign Employee'}
          </button>
        </div>
      </form>
    </Modal>
  );
};

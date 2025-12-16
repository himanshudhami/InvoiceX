import { useState } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { Subscription } from '@/services/api/types';
import {
  useSubscriptions,
  useCreateSubscription,
  useUpdateSubscription,
  useDeleteSubscription,
  useSubscriptionAssignments,
  useAssignSubscription,
  useRevokeSubscriptionAssignment,
  usePauseSubscription,
  useResumeSubscription,
  useCancelSubscription,
} from '@/hooks/api/useSubscriptions';
import { useEmployees } from '@/hooks/api/useEmployees';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { DataTable } from '../components/ui/DataTable';
import { Drawer } from '@/components/ui/Drawer';
import { Modal } from '@/components/ui/Modal';
import { SubscriptionForm } from '@/components/forms/SubscriptionForm';
import { PauseSubscriptionModal } from '@/components/modals/PauseSubscriptionModal';
import { ResumeSubscriptionModal } from '@/components/modals/ResumeSubscriptionModal';
import { CancelSubscriptionModal } from '@/components/modals/CancelSubscriptionModal';
import { Edit, Trash2, Link2, RefreshCcw, Pause, Play, X } from 'lucide-react';

const getStatusBadgeColor = (status: string) => {
  switch (status?.toLowerCase()) {
    case 'active':
      return 'bg-green-100 text-green-800';
    case 'on_hold':
      return 'bg-yellow-100 text-yellow-800';
    case 'cancelled':
      return 'bg-red-100 text-red-800';
    case 'expired':
      return 'bg-gray-100 text-gray-800';
    case 'trial':
      return 'bg-blue-100 text-blue-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

const formatCurrency = (amount: number | undefined, currency: string = 'USD') => {
  if (!amount) return '-';
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: currency || 'USD',
    maximumFractionDigits: 0,
  }).format(amount);
};

const calculateMonthlyCost = (costPerPeriod: number | undefined, renewalPeriod: string | undefined) => {
  if (!costPerPeriod) return 0;
  switch (renewalPeriod?.toLowerCase()) {
    case 'monthly':
      return costPerPeriod;
    case 'quarterly':
      return costPerPeriod / 3;
    case 'yearly':
      return costPerPeriod / 12;
    default:
      return costPerPeriod;
  }
};

const SubscriptionsManagement = () => {
  const { data, isLoading, error, refetch } = useSubscriptions({ pageNumber: 1, pageSize: 50 });
  const createSub = useCreateSubscription();
  const updateSub = useUpdateSubscription();
  const deleteSub = useDeleteSubscription();
  const assignSub = useAssignSubscription();
  const revokeSub = useRevokeSubscriptionAssignment();
  const { data: employees = [] } = useEmployees();

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [editing, setEditing] = useState<Subscription | null>(null);
  const [toDelete, setToDelete] = useState<Subscription | null>(null);
  const [assigning, setAssigning] = useState<Subscription | null>(null);
  const [toPause, setToPause] = useState<Subscription | null>(null);
  const [toResume, setToResume] = useState<Subscription | null>(null);
  const [toCancel, setToCancel] = useState<Subscription | null>(null);
  const [assignForm, setAssignForm] = useState({
    targetType: 'company',
    companyId: '',
    employeeId: '',
    seatIdentifier: '',
    role: '',
  });
  const assignmentsQuery = useSubscriptionAssignments(assigning?.id ?? '', !!assigning);

  const closeDrawer = () => {
    setIsDrawerOpen(false);
    setEditing(null);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Subscriptions</h1>
        <p className="text-gray-600 mt-2">Manage software and service subscriptions</p>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={
              [
                { header: 'Name', accessorKey: 'name' },
                { header: 'Vendor', accessorKey: 'vendor' },
                { header: 'Plan', accessorKey: 'planName' },
                {
                  header: 'Status',
                  cell: ({ row }) => {
                    const sub = row.original as Subscription;
                    return (
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusBadgeColor(sub.status)}`}>
                        {sub.status}
                      </span>
                    );
                  },
                },
                {
                  header: 'Total Cost',
                  cell: ({ row }) => {
                    const sub = row.original as Subscription;
                    return (
                      <div className="text-right">
                        <div className="font-medium">{formatCurrency(sub.costPerPeriod, sub.currency)}</div>
                        {sub.currency && sub.currency !== 'INR' && (
                          <div className="text-xs text-gray-500">{sub.currency}</div>
                        )}
                      </div>
                    );
                  },
                },
                {
                  header: 'Monthly Cost',
                  cell: ({ row }) => {
                    const sub = row.original as Subscription;
                    const monthly = calculateMonthlyCost(sub.costPerPeriod, sub.renewalPeriod);
                    return <div className="text-right">{formatCurrency(monthly, sub.currency)}</div>;
                  },
                },
                {
                  header: 'Cost/Seat',
                  cell: ({ row }) => {
                    const sub = row.original as Subscription;
                    if (sub.costPerSeat && sub.seatsTotal && sub.seatsTotal > 0) {
                      return <div className="text-right">{formatCurrency(sub.costPerSeat, sub.currency)}</div>;
                    }
                    return <div className="text-right text-gray-400">-</div>;
                  },
                },
                { header: 'Seats', accessorKey: 'seatsTotal' },
                { header: 'Renewal', accessorKey: 'renewalDate' },
                {
                  id: 'actions',
                  header: 'Actions',
                  cell: ({ row }) => {
                    const sub = row.original as Subscription;
                    const isActive = sub.status?.toLowerCase() === 'active';
                    const isPaused = sub.status?.toLowerCase() === 'on_hold';
                    const isCancelled = sub.status?.toLowerCase() === 'cancelled';
                    
                    return (
                      <div className="flex space-x-2">
                        <button
                          onClick={() => {
                            setEditing(sub);
                            setIsDrawerOpen(true);
                          }}
                          className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                          title="Edit"
                        >
                          <Edit size={16} />
                        </button>
                        <button
                          onClick={() => {
                            setAssigning(sub);
                            setAssignForm({ targetType: 'company', companyId: sub.companyId, employeeId: '', seatIdentifier: '', role: '' });
                          }}
                          className="text-amber-600 hover:text-amber-800 p-1 rounded hover:bg-amber-50 transition-colors"
                          title="Assign"
                        >
                          <Link2 size={16} />
                        </button>
                        {isActive && (
                          <button
                            onClick={() => setToPause(sub)}
                            className="text-yellow-600 hover:text-yellow-800 p-1 rounded hover:bg-yellow-50 transition-colors"
                            title="Pause"
                          >
                            <Pause size={16} />
                          </button>
                        )}
                        {isPaused && (
                          <button
                            onClick={() => setToResume(sub)}
                            className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
                            title="Resume"
                          >
                            <Play size={16} />
                          </button>
                        )}
                        {!isCancelled && (
                          <button
                            onClick={() => setToCancel(sub)}
                            className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                            title="Cancel"
                          >
                            <X size={16} />
                          </button>
                        )}
                        <button
                          onClick={() => setToDelete(sub)}
                          className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                          title="Delete"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    );
                  },
                },
              ] as ColumnDef<Subscription, any>[]
            }
            data={data?.items ?? []}
            searchPlaceholder="Search subscriptions..."
            onAdd={() => {
              setEditing(null);
              setIsDrawerOpen(true);
            }}
            addButtonText="Add Subscription"
          />
        </div>
      </div>

      <Drawer
        isOpen={isDrawerOpen}
        title={editing ? 'Edit Subscription' : 'Add Subscription'}
        onClose={closeDrawer}
      >
        <SubscriptionForm
          subscription={editing ?? undefined}
          onSuccess={() => {
            closeDrawer();
            refetch();
          }}
          onCancel={closeDrawer}
        />
      </Drawer>

      <Drawer
        isOpen={!!assigning}
        title={assigning ? `Assign ${assigning.name}` : 'Assign Subscription'}
        onClose={() => setAssigning(null)}
      >
        {assigning && (
          <div className="space-y-4">
            <form
              className="grid grid-cols-1 md:grid-cols-2 gap-4"
              onSubmit={async (e) => {
                e.preventDefault();
                await assignSub.mutateAsync({
                  subscriptionId: assigning.id,
                  data: {
                    targetType: assignForm.targetType as 'employee' | 'company',
                    companyId: assignForm.companyId,
                    employeeId: assignForm.targetType === 'employee' ? assignForm.employeeId : undefined,
                    seatIdentifier: assignForm.seatIdentifier || undefined,
                    role: assignForm.role || undefined,
                  },
                });
                assignmentsQuery.refetch();
              }}
            >
              <div>
                <label className="block text-sm font-medium text-gray-700">Target</label>
                <select
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                  value={assignForm.targetType}
                  onChange={(e) => setAssignForm({ ...assignForm, targetType: e.target.value })}
                >
                  <option value="company">Company</option>
                  <option value="employee">Employee</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Company ID</label>
                <input
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                  value={assignForm.companyId}
                  onChange={(e) => setAssignForm({ ...assignForm, companyId: e.target.value })}
                  required
                />
              </div>
              {assignForm.targetType === 'employee' && (
                <div>
                  <label className="block text-sm font-medium text-gray-700">Employee</label>
                  <select
                    className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                    value={assignForm.employeeId}
                    onChange={(e) => setAssignForm({ ...assignForm, employeeId: e.target.value })}
                    required
                  >
                    <option value="">Select employee</option>
                    {employees.map((emp) => (
                      <option key={emp.id} value={emp.id}>
                        {emp.employeeName} ({emp.employeeId})
                      </option>
                    ))}
                  </select>
                </div>
              )}
              <input
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                placeholder="Seat identifier (email/license)"
                value={assignForm.seatIdentifier}
                onChange={(e) => setAssignForm({ ...assignForm, seatIdentifier: e.target.value })}
              />
              <input
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                placeholder="Role (optional)"
                value={assignForm.role}
                onChange={(e) => setAssignForm({ ...assignForm, role: e.target.value })}
              />
              <button
                type="submit"
                className="md:col-span-2 bg-primary text-white rounded px-4 py-2 hover:bg-primary/90"
              >
                Assign Seat
              </button>
            </form>

            <div className="border-t pt-4">
              <div className="flex items-center justify-between mb-2">
                <h4 className="text-sm font-semibold text-gray-800">Assignments</h4>
                <button
                  onClick={() => assignmentsQuery.refetch()}
                  className="text-gray-500 hover:text-gray-700"
                  title="Refresh"
                >
                  <RefreshCcw size={14} />
                </button>
              </div>
              {assignmentsQuery.isLoading && <div className="text-sm text-gray-500">Loading...</div>}
              {assignmentsQuery.data && assignmentsQuery.data.length === 0 && (
                <div className="text-sm text-gray-500">No assignments</div>
              )}
              {assignmentsQuery.data && assignmentsQuery.data.length > 0 && (
                <div className="space-y-2">
                  {assignmentsQuery.data.map((a) => (
                    <div
                      key={a.id}
                      className="flex items-center justify-between rounded border px-3 py-2"
                    >
                      <div className="text-sm text-gray-800">
                        {a.targetType === 'employee' ? `Employee ${a.employeeId}` : 'Company'}
                        {a.seatIdentifier ? ` • ${a.seatIdentifier}` : ''}
                      </div>
                      {a.revokedOn ? (
                        <span className="text-xs text-gray-500">Revoked</span>
                      ) : (
                        <button
                          onClick={async () => {
                            await revokeSub.mutateAsync({
                              assignmentId: a.id,
                              data: { revokedOn: new Date().toISOString().slice(0, 10) },
                            });
                            assignmentsQuery.refetch();
                          }}
                          className="text-sm text-blue-600 hover:text-blue-800"
                        >
                          Revoke
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </Drawer>

      <Modal
        isOpen={!!toDelete}
        title="Delete subscription"
        onClose={() => setToDelete(null)}
        onConfirm={async () => {
          if (!toDelete) return;
          await deleteSub.mutateAsync(toDelete.id);
          setToDelete(null);
          refetch();
        }}
        confirmText="Delete"
        variant="destructive"
      >
        Are you sure you want to delete this subscription? This action cannot be undone.
      </Modal>

      {toPause && (
        <PauseSubscriptionModal
          subscription={toPause}
          isOpen={!!toPause}
          onClose={() => setToPause(null)}
          onSuccess={() => {
            setToPause(null);
            refetch();
          }}
        />
      )}

      {toResume && (
        <ResumeSubscriptionModal
          subscription={toResume}
          isOpen={!!toResume}
          onClose={() => setToResume(null)}
          onSuccess={() => {
            setToResume(null);
            refetch();
          }}
        />
      )}

      {toCancel && (
        <CancelSubscriptionModal
          subscription={toCancel}
          isOpen={!!toCancel}
          onClose={() => setToCancel(null)}
          onSuccess={() => {
            setToCancel(null);
            refetch();
          }}
        />
      )}
    </div>
  );
};

export default SubscriptionsManagement;






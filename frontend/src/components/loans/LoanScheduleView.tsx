import { useLoanSchedule } from '@/hooks/api/useLoans';
import { formatINR } from '@/lib/financialUtils';

type LoanScheduleViewProps = {
  loanId: string;
};

export const LoanScheduleView = ({ loanId }: LoanScheduleViewProps) => {
  const { data: schedule, isLoading, error } = useLoanSchedule(loanId);

  if (isLoading) {
    return <div className="p-4 text-center text-gray-500">Loading schedule...</div>;
  }

  if (error) {
    return <div className="p-4 text-center text-red-500">Failed to load schedule</div>;
  }

  if (!schedule || !schedule.scheduleItems || schedule.scheduleItems.length === 0) {
    return <div className="p-4 text-center text-gray-500">No schedule available</div>;
  }

  const paidCount = schedule.scheduleItems.filter((item) => item.status === 'paid').length;
  const pendingCount = schedule.scheduleItems.filter((item) => item.status === 'pending').length;
  const totalInterest = schedule.scheduleItems.reduce((sum, item) => sum + item.interestAmount, 0);
  const totalPrincipal = schedule.scheduleItems.reduce((sum, item) => sum + item.principalAmount, 0);

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 p-4 bg-gray-50 rounded-lg">
        <div>
          <p className="text-sm text-gray-600">Total EMIs</p>
          <p className="text-lg font-semibold">{schedule.scheduleItems.length}</p>
        </div>
        <div>
          <p className="text-sm text-gray-600">Paid</p>
          <p className="text-lg font-semibold text-green-600">{paidCount}</p>
        </div>
        <div>
          <p className="text-sm text-gray-600">Pending</p>
          <p className="text-lg font-semibold text-orange-600">{pendingCount}</p>
        </div>
        <div>
          <p className="text-sm text-gray-600">Total Interest</p>
          <p className="text-lg font-semibold">{formatINR(totalInterest)}</p>
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                EMI #
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Due Date
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Principal
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Interest
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Total EMI
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Outstanding
              </th>
              <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {schedule.scheduleItems.map((item) => {
              const isPaid = item.status === 'paid';
              const isOverdue = item.status === 'overdue';
              const dueDate = new Date(item.dueDate);
              const isPastDue = !isPaid && dueDate < new Date();

              return (
                <tr
                  key={item.id}
                  className={isPaid ? 'bg-green-50' : isOverdue || isPastDue ? 'bg-red-50' : ''}
                >
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900">
                    {item.emiNumber}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
                    {dueDate.toLocaleDateString('en-IN', {
                      year: 'numeric',
                      month: 'short',
                      day: 'numeric',
                    })}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-right text-gray-900">
                    {formatINR(item.principalAmount)}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-right text-gray-900">
                    {formatINR(item.interestAmount)}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-right font-medium text-gray-900">
                    {formatINR(item.totalEmi)}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-right text-gray-500">
                    {formatINR(item.outstandingPrincipalAfter)}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-center">
                    <span
                      className={`px-2 py-1 text-xs font-medium rounded-full ${
                        isPaid
                          ? 'bg-green-100 text-green-800'
                          : isOverdue || isPastDue
                            ? 'bg-red-100 text-red-800'
                            : 'bg-yellow-100 text-yellow-800'
                      }`}
                    >
                      {item.status}
                    </span>
                  </td>
                </tr>
              );
            })}
          </tbody>
          <tfoot className="bg-gray-50">
            <tr>
              <td colSpan={2} className="px-4 py-3 text-sm font-medium text-gray-900">
                Total
              </td>
              <td className="px-4 py-3 text-sm text-right font-medium text-gray-900">
                {formatINR(totalPrincipal)}
              </td>
              <td className="px-4 py-3 text-sm text-right font-medium text-gray-900">
                {formatINR(totalInterest)}
              </td>
              <td className="px-4 py-3 text-sm text-right font-medium text-gray-900">
                {formatINR(totalPrincipal + totalInterest)}
              </td>
              <td colSpan={2}></td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  );
};


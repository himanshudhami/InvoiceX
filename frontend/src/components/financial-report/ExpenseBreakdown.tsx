import { PnLData } from '@/lib/pnlCalculation';
import { formatINR } from '@/lib/financialUtils';
import { PieChart as PieChartIcon } from 'lucide-react';
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
  Tooltip,
} from 'recharts';

interface ExpenseBreakdownProps {
  data: PnLData;
}

const COLORS = ['#3b82f6', '#f97316', '#8b5cf6', '#f59e0b', '#10b981', '#ec4899'];

export const ExpenseBreakdown = ({ data }: ExpenseBreakdownProps) => {
  const expenseData = [
    { name: 'Salaries', value: data.salaryExpense },
    { name: 'Maintenance', value: data.maintenanceExpense },
    { name: 'OPEX Assets', value: data.opexAssetExpense },
    { name: 'Subscriptions', value: data.subscriptionExpense },
    { name: 'Depreciation', value: data.depreciation },
    { name: 'Other', value: data.otherExpense },
  ].filter((e) => e.value > 0);

  const total = expenseData.reduce((sum, item) => sum + item.value, 0);

  return (
    <div className="bg-white rounded-lg shadow">
      <div className="border-b border-gray-200 px-6 py-4 flex items-center">
        <PieChartIcon className="w-5 h-5 text-gray-500 mr-2" />
        <h3 className="text-lg font-medium text-gray-900">Expense Distribution</h3>
      </div>
      <div className="p-6">
        {expenseData.length > 0 ? (
          <div className="flex flex-col items-center">
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={expenseData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(1)}%`}
                  outerRadius={100}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {expenseData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip
                  formatter={(value: number) => [
                    formatINR(value),
                    `${((value / total) * 100).toFixed(1)}%`,
                  ]}
                />
                <Legend
                  formatter={(value, entry: any) => {
                    const percentage = ((entry.payload.value / total) * 100).toFixed(1);
                    return `${value}: ${formatINR(entry.payload.value)} (${percentage}%)`;
                  }}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <div className="text-center py-8 text-gray-500">No expense data available</div>
        )}
      </div>
    </div>
  );
};





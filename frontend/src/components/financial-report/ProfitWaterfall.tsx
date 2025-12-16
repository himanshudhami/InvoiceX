import { PnLData } from '@/lib/pnlCalculation';
import { formatINR } from '@/lib/financialUtils';
import { TrendingUp } from 'lucide-react';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  Cell,
} from 'recharts';

interface ProfitWaterfallProps {
  data: PnLData;
}

export const ProfitWaterfall = ({ data }: ProfitWaterfallProps) => {
  const waterfallData = [
    {
      name: 'Income',
      value: data.totalIncome,
      fill: '#10b981',
      type: 'income',
    },
    {
      name: 'Salaries',
      value: -data.salaryExpense,
      fill: '#ef4444',
      type: 'expense',
    },
    {
      name: 'Maintenance',
      value: -data.maintenanceExpense,
      fill: '#f97316',
      type: 'expense',
    },
    {
      name: 'Other OpEx',
      value: -data.otherExpense,
      fill: '#f59e0b',
      type: 'expense',
    },
    {
      name: 'EBITDA',
      value: data.ebitda,
      fill: '#3b82f6',
      type: 'profit',
    },
    {
      name: 'Depreciation',
      value: -data.depreciation,
      fill: '#8b5cf6',
      type: 'expense',
    },
    {
      name: 'Net Profit',
      value: data.netProfit,
      fill: '#06b6d4',
      type: 'profit',
    },
  ].filter((item) => item.value !== 0); // Remove zero values

  return (
    <div className="bg-white rounded-lg shadow">
      <div className="border-b border-gray-200 px-6 py-4 flex items-center">
        <TrendingUp className="w-5 h-5 text-gray-500 mr-2" />
        <h3 className="text-lg font-medium text-gray-900">Profit Waterfall</h3>
      </div>
      <div className="p-6" style={{ minHeight: 350 }}>
        <ResponsiveContainer width="100%" height={350}>
          <BarChart data={waterfallData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
            <XAxis
              dataKey="name"
              stroke="#9ca3af"
              tick={{ fill: '#6b7280', fontSize: 12 }}
            />
            <YAxis
              stroke="#9ca3af"
              tick={{ fill: '#6b7280', fontSize: 12 }}
              tickFormatter={(value) => {
                const absValue = Math.abs(value);
                if (absValue >= 10000000) return `₹${(absValue / 10000000).toFixed(1)}Cr`;
                if (absValue >= 100000) return `₹${(absValue / 100000).toFixed(1)}L`;
                if (absValue >= 1000) return `₹${(absValue / 1000).toFixed(0)}k`;
                return `₹${absValue}`;
              }}
            />
            <Tooltip
              formatter={(value: number) => formatINR(value)}
              contentStyle={{
                backgroundColor: '#fff',
                border: '1px solid #e5e7eb',
                borderRadius: '6px',
              }}
            />
            <Legend />
            <Bar dataKey="value" name="Amount">
              {waterfallData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.fill} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
      <div className="px-6 pb-6 text-sm text-gray-500">
        <p>Visual representation of how income flows through expenses to net profit</p>
      </div>
    </div>
  );
};




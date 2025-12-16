import { PnLData } from '@/lib/pnlCalculation';
import { formatINR } from '@/lib/financialUtils';
import { Calendar } from 'lucide-react';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from 'recharts';

interface TrendAnalysisProps {
  data: PnLData;
  selectedYear: number;
}

export const TrendAnalysis = ({ data, selectedYear }: TrendAnalysisProps) => {
  const monthNames = [
    'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
    'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
  ];

  const chartData = data.monthlyData.map((month) => ({
    ...month,
    monthName: monthNames[month.month - 1],
  }));

  return (
    <div className="bg-white rounded-lg shadow">
      <div className="border-b border-gray-200 px-6 py-4 flex items-center">
        <Calendar className="w-5 h-5 text-gray-500 mr-2" />
        <h3 className="text-lg font-medium text-gray-900">Monthly Trends - {selectedYear}</h3>
      </div>
      <div className="p-6" style={{ minHeight: 300 }}>
        <ResponsiveContainer width="100%" height={300}>
          <LineChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
            <XAxis
              dataKey="monthName"
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
              labelFormatter={(label) => `${label} ${selectedYear}`}
              contentStyle={{
                backgroundColor: '#fff',
                border: '1px solid #e5e7eb',
                borderRadius: '6px',
              }}
            />
            <Legend />
            <Line
              type="monotone"
              dataKey="income"
              name="Income"
              stroke="#10b981"
              strokeWidth={2}
              dot={{ fill: '#10b981', r: 4 }}
            />
            <Line
              type="monotone"
              dataKey="expenses"
              name="Expenses"
              stroke="#ef4444"
              strokeWidth={2}
              dot={{ fill: '#ef4444', r: 4 }}
            />
            <Line
              type="monotone"
              dataKey="profit"
              name="Profit"
              stroke="#3b82f6"
              strokeWidth={2}
              dot={{ fill: '#3b82f6', r: 4 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};




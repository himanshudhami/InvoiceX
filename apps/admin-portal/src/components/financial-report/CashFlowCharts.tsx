import { CashFlowData } from '@/lib/cashFlowCalculation';
import { formatINR } from '@/lib/financialUtils';
import { LineChart, Line, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';

interface CashFlowChartsProps {
  data: CashFlowData;
  selectedYear?: number;
}

const COLORS = ['#3b82f6', '#10b981', '#8b5cf6', '#f59e0b'];

export const CashFlowTrendChart = ({ data, selectedYear }: CashFlowChartsProps) => {
  const chartData = data.monthlyData.map((month) => ({
    month: new Date(selectedYear || new Date().getFullYear(), month.month - 1, 1).toLocaleDateString('en-IN', { month: 'short' }),
    operating: month.operating,
    investing: month.investing,
    financing: month.financing,
    net: month.netCashFlow,
  }));

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">Monthly Cash Flow Trend</h3>
      <ResponsiveContainer width="100%" height={300}>
        <LineChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="month" />
          <YAxis tickFormatter={(value) => formatINR(value)} />
          <Tooltip formatter={(value: number) => formatINR(value)} />
          <Legend />
          <Line type="monotone" dataKey="operating" stroke="#3b82f6" name="Operating" />
          <Line type="monotone" dataKey="investing" stroke="#10b981" name="Investing" />
          <Line type="monotone" dataKey="financing" stroke="#8b5cf6" name="Financing" />
          <Line type="monotone" dataKey="net" stroke="#f59e0b" strokeWidth={2} name="Net Cash Flow" />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
};

export const CashFlowBarChart = ({ data }: CashFlowChartsProps) => {
  const chartData = [
    {
      name: 'Operating',
      value: data.cashFromOperatingActivities,
      color: '#3b82f6',
    },
    {
      name: 'Investing',
      value: data.cashFromInvestingActivities,
      color: '#10b981',
    },
    {
      name: 'Financing',
      value: data.cashFromFinancingActivities,
      color: '#8b5cf6',
    },
  ];

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">Cash Flow by Activity</h3>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="name" />
          <YAxis tickFormatter={(value) => formatINR(value)} />
          <Tooltip formatter={(value: number) => formatINR(value)} />
          <Bar dataKey="value" fill="#3b82f6">
            {chartData.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={entry.color} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
};

export const CashFlowPieChart = ({ data }: CashFlowChartsProps) => {
  const pieData = [
    {
      name: 'Operating',
      value: Math.abs(data.cashFromOperatingActivities),
      color: '#3b82f6',
    },
    {
      name: 'Investing',
      value: Math.abs(data.cashFromInvestingActivities),
      color: '#10b981',
    },
    {
      name: 'Financing',
      value: Math.abs(data.cashFromFinancingActivities),
      color: '#8b5cf6',
    },
  ].filter(item => item.value > 0);

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">Cash Flow Distribution</h3>
      <ResponsiveContainer width="100%" height={300}>
        <PieChart>
          <Pie
            data={pieData}
            cx="50%"
            cy="50%"
            labelLine={false}
            label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
            outerRadius={80}
            fill="#8884d8"
            dataKey="value"
          >
            {pieData.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={entry.color} />
            ))}
          </Pie>
          <Tooltip formatter={(value: number) => formatINR(value)} />
        </PieChart>
      </ResponsiveContainer>
    </div>
  );
};






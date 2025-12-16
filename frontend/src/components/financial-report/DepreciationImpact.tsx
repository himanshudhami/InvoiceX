import { PnLData } from '@/lib/pnlCalculation';
import { formatINR } from '@/lib/financialUtils';
import { Activity } from 'lucide-react';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from 'recharts';

interface DepreciationImpactProps {
  data: PnLData;
}

export const DepreciationImpact = ({ data }: DepreciationImpactProps) => {
  const chartData = data.depreciationByCategory.map((dep) => ({
    category: dep.category,
    amount: dep.amount,
    rate: dep.rate,
  }));

  return (
    <div className="bg-white rounded-lg shadow">
      <div className="border-b border-gray-200 px-6 py-4 flex items-center">
        <Activity className="w-5 h-5 text-gray-500 mr-2" />
        <h3 className="text-lg font-medium text-gray-900">Depreciation Impact</h3>
      </div>
      <div className="p-6">
        <div className="space-y-3 mb-6">
          <div className="flex items-center justify-between p-4 bg-blue-50 rounded-lg border border-blue-100">
            <span className="text-gray-700 font-medium">EBITDA</span>
            <span className="text-2xl font-bold text-blue-700">{formatINR(data.ebitda)}</span>
          </div>
          
          <div className="flex items-center justify-between p-4 bg-purple-50 rounded-lg border border-purple-100">
            <span className="text-gray-700 font-medium">Less: Depreciation</span>
            <span className="text-2xl font-bold text-purple-700">{formatINR(data.depreciation)}</span>
          </div>
          
          <div className="flex items-center justify-between p-4 bg-green-50 rounded-lg border-2 border-green-200">
            <span className="text-gray-700 font-semibold text-lg">Net Profit</span>
            <span className="text-2xl font-bold text-green-700">{formatINR(data.netProfit)}</span>
          </div>
        </div>

        {chartData.length > 0 ? (
          <div>
            <h4 className="text-sm font-medium text-gray-700 mb-3">Depreciation by Asset Category</h4>
            <div style={{ minHeight: 250 }}>
              <ResponsiveContainer width="100%" height={250}>
                <BarChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                  <XAxis
                    dataKey="category"
                    stroke="#9ca3af"
                    tick={{ fill: '#6b7280', fontSize: 11 }}
                    angle={-45}
                    textAnchor="end"
                    height={80}
                  />
                  <YAxis
                    stroke="#9ca3af"
                    tick={{ fill: '#6b7280', fontSize: 12 }}
                    tickFormatter={(value) => {
                      const absValue = Math.abs(value);
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
                  <Bar dataKey="amount" fill="#8b5cf6" name="Depreciation" />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        ) : (
          <div className="text-center py-8 text-gray-500">No depreciation data available</div>
        )}
      </div>
    </div>
  );
};





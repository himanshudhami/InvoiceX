import { PnLData } from '@/lib/pnlCalculation';
import { formatINR } from '@/lib/financialUtils';
import { Target } from 'lucide-react';

interface KeyMetricsProps {
  data: PnLData;
  assetReport?: {
    totalPurchaseCost: number;
    totalNetBookValue: number;
  };
}

export const KeyMetrics = ({ data, assetReport }: KeyMetricsProps) => {
  const profitMargin = data.totalIncome > 0
    ? ((data.netProfit / data.totalIncome) * 100).toFixed(1)
    : '0.0';

  const opexRatio = data.totalIncome > 0
    ? ((data.totalOpex / data.totalIncome) * 100).toFixed(1)
    : '0.0';

  const assetBase = assetReport?.totalPurchaseCost || 0;
  const netAssetValue = assetReport?.totalNetBookValue || 0;

  const roa = netAssetValue > 0
    ? ((data.netProfit / netAssetValue) * 100).toFixed(1)
    : '0.0';

  const metrics = [
    {
      label: 'Profit Margin',
      value: `${profitMargin}%`,
      description: 'Net profit as % of income',
      color: 'text-green-600',
    },
    {
      label: 'OpEx / Income Ratio',
      value: `${opexRatio}%`,
      description: 'Operating expenses as % of income',
      color: 'text-orange-600',
    },
    {
      label: 'Asset Base',
      value: formatINR(assetBase),
      description: 'Total asset purchase cost',
      color: 'text-blue-600',
    },
    {
      label: 'ROA (Return on Assets)',
      value: `${roa}%`,
      description: 'Net profit / Net asset value',
      color: 'text-purple-600',
    },
  ];

  return (
    <div className="bg-white rounded-lg shadow">
      <div className="border-b border-gray-200 px-6 py-4 flex items-center">
        <Target className="w-5 h-5 text-gray-500 mr-2" />
        <h3 className="text-lg font-medium text-gray-900">Key Metrics</h3>
      </div>
      <div className="p-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {metrics.map((metric) => (
            <div key={metric.label} className="border border-gray-200 rounded-lg p-4">
              <div className="text-sm font-medium text-gray-500 mb-1">{metric.label}</div>
              <div className={`text-2xl font-bold ${metric.color} mb-1`}>{metric.value}</div>
              <div className="text-xs text-gray-500">{metric.description}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};





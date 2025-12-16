import { PnLData } from '@/lib/pnlCalculation';
import { formatINR, formatPercentageChange, calculatePercentageChange } from '@/lib/financialUtils';
import { TrendingUp, TrendingDown, DollarSign, TrendingDown as ExpenseIcon, Target, CheckCircle } from 'lucide-react';

interface KPICardsProps {
  data: PnLData;
  previousPeriodData?: PnLData; // For YoY/MoM comparison
}

export const KPICards = ({ data, previousPeriodData }: KPICardsProps) => {
  const incomeChange = previousPeriodData
    ? calculatePercentageChange(data.totalIncome, previousPeriodData.totalIncome)
    : 0;

  const expenseChange = previousPeriodData
    ? calculatePercentageChange(data.totalOpex, previousPeriodData.totalOpex)
    : 0;

  const ebitdaChange = previousPeriodData
    ? calculatePercentageChange(data.ebitda, previousPeriodData.ebitda)
    : 0;

  const profitChange = previousPeriodData
    ? calculatePercentageChange(data.netProfit, previousPeriodData.netProfit)
    : 0;

  const cards = [
    {
      label: 'Total Income',
      value: formatINR(data.totalIncome),
      change: incomeChange,
      color: 'green' as const,
      icon: DollarSign,
    },
    {
      label: 'Operating Expenses',
      value: formatINR(data.totalOpex),
      change: expenseChange,
      color: 'red' as const,
      icon: ExpenseIcon,
    },
    {
      label: 'EBITDA',
      value: formatINR(data.ebitda),
      change: ebitdaChange,
      color: 'blue' as const,
      icon: Target,
    },
    {
      label: 'Net Profit',
      value: formatINR(data.netProfit),
      change: profitChange,
      color: 'green' as const,
      icon: CheckCircle,
    },
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      {cards.map((card) => {
        const Icon = card.icon;
        const isPositive = card.change >= 0;
        const colorClasses = {
          green: {
            bg: 'bg-green-100',
            text: 'text-green-700',
            icon: 'text-green-600',
          },
          red: {
            bg: 'bg-red-100',
            text: 'text-red-700',
            icon: 'text-red-600',
          },
          blue: {
            bg: 'bg-blue-100',
            text: 'text-blue-700',
            icon: 'text-blue-600',
          },
        };

        const colors = colorClasses[card.color];

        return (
          <div key={card.label} className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center">
              <div className={`flex-shrink-0 p-3 ${colors.bg} rounded-full`}>
                <Icon className={`w-6 h-6 ${colors.icon}`} />
              </div>
              <div className="ml-4 flex-1">
                <p className="text-sm font-medium text-gray-500">{card.label}</p>
                <p className="text-2xl font-bold text-gray-900 mt-1">{card.value}</p>
                {previousPeriodData && (
                  <div className="flex items-center mt-2">
                    <div className={`text-xs px-2 py-1 rounded ${colors.bg} ${colors.text}`}>
                      {isPositive ? (
                        <TrendingUp className="w-3 h-3 inline mr-1" />
                      ) : (
                        <TrendingDown className="w-3 h-3 inline mr-1" />
                      )}
                      {formatPercentageChange(card.change)}
                    </div>
                    <span className="text-xs text-gray-500 ml-2">vs previous period</span>
                  </div>
                )}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
};




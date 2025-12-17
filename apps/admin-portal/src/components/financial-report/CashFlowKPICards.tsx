import { CashFlowData } from '@/lib/cashFlowCalculation';
import { formatINR } from '@/lib/financialUtils';
import { TrendingUp, TrendingDown, DollarSign, ArrowDownCircle, ArrowUpCircle, Wallet } from 'lucide-react';

interface CashFlowKPICardsProps {
  data: CashFlowData;
}

export const CashFlowKPICards = ({ data }: CashFlowKPICardsProps) => {
  const cards = [
    {
      label: 'Operating Cash Flow',
      value: formatINR(data.cashFromOperatingActivities),
      color: 'blue' as const,
      icon: DollarSign,
      description: 'Cash from core operations',
    },
    {
      label: 'Investing Cash Flow',
      value: formatINR(data.cashFromInvestingActivities),
      color: 'green' as const,
      icon: ArrowDownCircle,
      description: 'Asset purchases & sales',
    },
    {
      label: 'Financing Cash Flow',
      value: formatINR(data.cashFromFinancingActivities),
      color: 'purple' as const,
      icon: ArrowUpCircle,
      description: 'Loans & repayments',
    },
    {
      label: 'Net Cash Flow',
      value: formatINR(data.netIncreaseDecreaseInCash),
      color: data.netIncreaseDecreaseInCash >= 0 ? 'green' as const : 'red' as const,
      icon: Wallet,
      description: 'Total change in cash',
    },
  ];

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
    purple: {
      bg: 'bg-purple-100',
      text: 'text-purple-700',
      icon: 'text-purple-600',
    },
  };

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      {cards.map((card) => {
        const Icon = card.icon;
        const colors = colorClasses[card.color];

        return (
          <div key={card.label} className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center">
              <div className={`flex-shrink-0 p-3 ${colors.bg} rounded-full`}>
                <Icon className={`w-6 h-6 ${colors.icon}`} />
              </div>
              <div className="ml-4 flex-1">
                <p className="text-sm font-medium text-gray-500">{card.label}</p>
                <p className={`text-2xl font-bold mt-1 ${colors.text}`}>{card.value}</p>
                <p className="text-xs text-gray-500 mt-1">{card.description}</p>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
};






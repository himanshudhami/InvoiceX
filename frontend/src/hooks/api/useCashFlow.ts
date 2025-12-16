import { useQuery } from '@tanstack/react-query';
import { cashFlowService } from '@/services/api/cashFlowService';

// Query keys for proper cache management
export const cashFlowKeys = {
  all: ['cashflow'] as const,
  statement: (companyId?: string, year?: number, month?: number) =>
    [...cashFlowKeys.all, 'statement', companyId, year, month] as const,
};

// Get cash flow statement
export function useCashFlowStatement(companyId?: string, year?: number, month?: number) {
  return useQuery({
    queryKey: cashFlowKeys.statement(companyId, year, month),
    queryFn: () => cashFlowService.getCashFlowStatement({ companyId, year, month }),
    staleTime: 1000 * 60, // 1 minute
    enabled: !!year, // Only fetch if year is provided
  });
}





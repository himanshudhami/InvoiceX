import { useMemo } from 'react';
import { useInvoices } from '@/hooks/api/useInvoices';
import { usePayrollExpenses } from '@/hooks/api/usePayrollExpenses';
import { useAssetCostReport } from '@/hooks/api/useAssets';
import { useSubscriptionMonthlyExpenses } from '@/hooks/api/useSubscriptions';
import { useInterestPayments } from '@/hooks/api/useLoans';
import { calculatePnL, PnLData } from '@/lib/pnlCalculation';
import { useQuery } from '@tanstack/react-query';
import { assetService } from '@/services/api/assetService';
import { paymentService } from '@/services/api/paymentService';

/**
 * Hook to fetch all assets for a company (for OPEX filtering)
 */
const useAllAssets = (companyId?: string) => {
  return useQuery({
    queryKey: ['assets', 'all', companyId || 'all'],
    queryFn: async () => {
      // Use a reasonable page size that won't cause backend validation errors
      const pageSize = 100; // Backend max limit
      const result = await assetService.getPaged({ 
        pageNumber: 1, 
        pageSize,
        ...(companyId && { companyId }), // Pass companyId as filter if provided
      });
      let allAssets = result.items || [];
      
      // If there are more pages, fetch them
      if (result.totalCount > result.items.length) {
        const totalPages = Math.ceil(result.totalCount / result.pageSize);
        const additionalPages = await Promise.all(
          Array.from({ length: totalPages - 1 }, (_, i) =>
            assetService.getPaged({ 
              pageNumber: i + 2, 
              pageSize: result.pageSize,
              ...(companyId && { companyId }), // Pass companyId as filter if provided
            })
          )
        );
        allAssets = [
          ...allAssets,
          ...additionalPages.flatMap(page => page.items || [])
        ];
      }
      
      // Additional client-side filter by company if specified (as backup)
      if (companyId) {
        allAssets = allAssets.filter(asset => asset.companyId === companyId);
      }
      
      return allAssets;
    },
    staleTime: 1000 * 30,
    enabled: true, // Always enabled, but handle empty companyId gracefully
  });
};

export type AccountingMethod = 'accrual' | 'cash';

/**
 * Custom hook to calculate P&L from invoices, salary transactions, and asset data
 * @param accountingMethod - 'accrual' (income when invoiced) or 'cash' (income when received)
 */
export const usePnLCalculation = (
  companyId?: string,
  year?: number,
  month?: number,
  accountingMethod: AccountingMethod = 'accrual'
): {
  data: PnLData | null;
  isLoading: boolean;
  error: Error | null;
} => {
  const { data: invoices = [], isLoading: invoicesLoading } = useInvoices();
  const { data: salaryTransactions = [], isLoading: salariesLoading } = usePayrollExpenses({
    companyId: companyId,
    year: year,
    month: month,
  });
  const { data: assetReport, isLoading: assetsLoading } = useAssetCostReport(companyId || undefined);
  const { data: assets = [], isLoading: allAssetsLoading } = useAllAssets(companyId);
  const { data: subscriptionExpenses = [], isLoading: subscriptionExpensesLoading } = useSubscriptionMonthlyExpenses(
    year || new Date().getFullYear(),
    month,
    companyId || undefined
  );

  // Calculate date range for loan interest payments
  const fromDate = useMemo(() => {
    if (year && month) {
      return new Date(year, month - 1, 1).toISOString().split('T')[0];
    } else if (year) {
      return new Date(year, 0, 1).toISOString().split('T')[0];
    }
    return undefined;
  }, [year, month]);

  const toDate = useMemo(() => {
    if (year && month) {
      const lastDay = new Date(year, month, 0).getDate();
      return new Date(year, month - 1, lastDay).toISOString().split('T')[0];
    } else if (year) {
      return new Date(year, 11, 31).toISOString().split('T')[0];
    }
    return undefined;
  }, [year, month]);

  const { data: loanInterestPayments = [], isLoading: loanInterestLoading } = useInterestPayments(
    companyId,
    fromDate,
    toDate
  );

  // Fetch payments for all invoices to get actual INR amounts
  const { data: allPayments = [], isLoading: paymentsLoading } = useQuery({
    queryKey: ['payments', 'all', companyId],
    queryFn: async () => {
      // Fetch payments for all paid invoices
      const paidInvoices = invoices.filter(inv => inv.status?.toLowerCase() === 'paid');
      const paymentPromises = paidInvoices.map(async (inv) => {
        try {
          return await paymentService.getByInvoiceId(inv.id);
        } catch {
          return [];
        }
      });
      const paymentArrays = await Promise.all(paymentPromises);
      return paymentArrays.flat();
    },
    enabled: !invoicesLoading && invoices.length > 0,
    staleTime: 1000 * 30,
  });

  const pnlData = useMemo(() => {
    if (invoicesLoading || salariesLoading || assetsLoading || allAssetsLoading || subscriptionExpensesLoading || loanInterestLoading || paymentsLoading) {
      return null;
    }

    try {
      return calculatePnL(
        invoices,
        salaryTransactions,
        assetReport,
        companyId,
        year,
        month,
        assets,
        subscriptionExpenses,
        loanInterestPayments,
        allPayments,
        accountingMethod
      );
    } catch (error) {
      console.error('Error calculating P&L:', error);
      return null;
    }
  }, [invoices, salaryTransactions, assetReport, companyId, year, month, assets, subscriptionExpenses, loanInterestPayments, allPayments, accountingMethod, invoicesLoading, salariesLoading, assetsLoading, allAssetsLoading, subscriptionExpensesLoading, loanInterestLoading, paymentsLoading]);

  return {
    data: pnlData,
    isLoading: invoicesLoading || salariesLoading || assetsLoading || allAssetsLoading || subscriptionExpensesLoading || loanInterestLoading || paymentsLoading,
    error: null, // Could be enhanced to capture actual errors
  };
};


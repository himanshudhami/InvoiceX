import { useMemo } from 'react';
import { useInvoices } from '@/hooks/api/useInvoices';
import { usePayrollExpenses } from '@/hooks/api/usePayrollExpenses';
import { useAssetCostReport } from '@/hooks/api/useAssets';
import { useSubscriptionMonthlyExpenses } from '@/hooks/api/useSubscriptions';
import { useInterestPayments } from '@/hooks/api/useLoans';
import { usePnLCalculation } from '@/hooks/usePnLCalculation';
import { calculateCashFlow, CashFlowData } from '@/lib/cashFlowCalculation';
import { useQuery } from '@tanstack/react-query';
import { assetService } from '@/services/api/assetService';
import { invoiceService } from '@/services/api/invoiceService';

/**
 * Hook to fetch all assets for a company (for cash flow filtering)
 */
const useAllAssets = (companyId?: string) => {
  return useQuery({
    queryKey: ['assets', 'all', companyId || 'all'],
    queryFn: async () => {
      const pageSize = 100;
      const result = await assetService.getPaged({ 
        pageNumber: 1, 
        pageSize,
        ...(companyId && { companyId }),
      });
      let allAssets = result.items || [];
      
      if (result.totalCount > result.items.length) {
        const totalPages = Math.ceil(result.totalCount / result.pageSize);
        const additionalPages = await Promise.all(
          Array.from({ length: totalPages - 1 }, (_, i) =>
            assetService.getPaged({ 
              pageNumber: i + 2, 
              pageSize: result.pageSize,
              ...(companyId && { companyId }),
            })
          )
        );
        allAssets = [
          ...allAssets,
          ...additionalPages.flatMap(page => page.items || [])
        ];
      }
      
      if (companyId) {
        allAssets = allAssets.filter(asset => asset.companyId === companyId);
      }
      
      return allAssets;
    },
    staleTime: 1000 * 30,
    enabled: true,
  });
};

/**
 * Hook to fetch all payments
 */
const useAllPayments = () => {
  return useQuery({
    queryKey: ['payments', 'all'],
    queryFn: async () => {
      // Fetch all invoices first to get payment data
      // Note: In a real scenario, you'd have a dedicated payments endpoint
      const invoices = await invoiceService.getAll();
      const payments: any[] = [];

      // Extract payments from invoices
      // This is a simplified approach - in production, fetch payments separately
      for (const invoice of invoices) {
        if (invoice.paidAmount && invoice.paidAmount > 0) {
          // Create a payment-like object from invoice data
          payments.push({
            id: `payment-${invoice.id}`,
            invoiceId: invoice.id,
            amount: invoice.paidAmount,
            paymentDate: invoice.paidAt || invoice.updatedAt || invoice.createdAt,
            paymentMethod: 'bank_transfer', // Default
          });
        }
      }

      return payments;
    },
    staleTime: 1000 * 30,
  });
};

/**
 * Hook to fetch all loan transactions
 */
const useAllLoanTransactions = (companyId?: string, fromDate?: string, toDate?: string) => {
  return useQuery({
    queryKey: ['loan-transactions', 'all', companyId, fromDate, toDate],
    queryFn: async () => {
      // Fetch all loans and their transactions
      // This is simplified - in production, you'd have a dedicated endpoint
      const { loanService } = await import('@/services/api/loanService');
      const loans = await loanService.getPaged({ 
        ...(companyId && { companyId }),
        pageSize: 100,
      });
      
      const allTransactions: any[] = [];
      for (const loan of loans.items || []) {
        // Fetch transactions for each loan
        // This would need a proper endpoint in production
        // For now, return empty array
      }
      
      return allTransactions;
    },
    staleTime: 1000 * 30,
  });
};

/**
 * Custom hook to calculate Cash Flow from various data sources
 */
export const useCashFlowCalculation = (
  companyId?: string,
  year?: number,
  month?: number
): {
  data: CashFlowData | null;
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
  const { data: payments = [], isLoading: paymentsLoading } = useAllPayments();
  const { data: pnlData, isLoading: pnlLoading } = usePnLCalculation(companyId, year, month);
  
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

  // For now, use loan interest payments as all loan transactions
  // In production, you'd fetch all loan transactions separately
  const loanTransactions = loanInterestPayments;

  const cashFlowData = useMemo(() => {
    if (invoicesLoading || salariesLoading || assetsLoading || allAssetsLoading || 
        paymentsLoading || pnlLoading || subscriptionExpensesLoading || loanInterestLoading) {
      return null;
    }

    try {
      return calculateCashFlow(
        pnlData,
        invoices,
        payments as any,
        salaryTransactions,
        assetReport,
        companyId,
        year,
        month,
        assets,
        subscriptionExpenses,
        loanTransactions as any
      );
    } catch (error) {
      console.error('Error calculating Cash Flow:', error);
      return null;
    }
  }, [
    pnlData,
    invoices,
    payments,
    salaryTransactions,
    assetReport,
    companyId,
    year,
    month,
    assets,
    subscriptionExpenses,
    loanTransactions,
    invoicesLoading,
    salariesLoading,
    assetsLoading,
    allAssetsLoading,
    paymentsLoading,
    pnlLoading,
    subscriptionExpensesLoading,
    loanInterestLoading
  ]);

  return {
    data: cashFlowData,
    isLoading: invoicesLoading || salariesLoading || assetsLoading || allAssetsLoading || 
               paymentsLoading || pnlLoading || subscriptionExpensesLoading || loanInterestLoading,
    error: null,
  };
};





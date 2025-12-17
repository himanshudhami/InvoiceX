/**
 * P&L (Profit & Loss) calculation utilities
 * Aggregates income, expenses, and asset costs into financial statements
 */

import { Invoice, EmployeeSalaryTransaction, AssetCostReport, Asset, SubscriptionMonthlyExpense, LoanTransaction, Payment } from '@/services/api/types';
import { toInr } from './financialUtils';

export interface PnLData {
  // Income
  totalIncome: number;
  
  // Operating Expenses
  salaryExpense: number;
  maintenanceExpense: number;
  opexAssetExpense: number; // OPEX asset purchases expensed in the period
  subscriptionExpense: number; // Subscription expenses (only active subscriptions)
  loanInterestExpense: number; // Interest on loans (Section 36(1)(iii) - fully deductible)
  otherExpense: number; // Placeholder for future
  totalOpex: number;
  
  // TDS Deducted (for compliance reporting)
  totalTDSDeducted: number; // Total TDS deducted from all salary/consulting payments
  
  // EBITDA
  ebitda: number;
  
  // Depreciation
  depreciation: number;
  depreciationByCategory: Array<{
    category: string;
    amount: number;
    rate: number;
  }>;
  
  // Net Profit
  netProfit: number;
  
  // Monthly breakdown for trends
  monthlyData: Array<{
    month: number;
    income: number;
    expenses: number;
    depreciation: number;
    profit: number;
  }>;
}

export type AccountingMethod = 'accrual' | 'cash';

/**
 * Calculate P&L from invoices, salary transactions, asset report, assets list, and subscription expenses
 * @param accountingMethod - 'accrual' (income when invoiced) or 'cash' (income when received)
 */
export const calculatePnL = (
  invoices: Invoice[],
  salaryTransactions: EmployeeSalaryTransaction[],
  assetReport: AssetCostReport | undefined,
  companyId?: string,
  year?: number,
  month?: number,
  assets?: Asset[],
  subscriptionExpenses?: SubscriptionMonthlyExpense[],
  loanInterestPayments?: LoanTransaction[],
  payments?: Payment[],
  accountingMethod: AccountingMethod = 'accrual'
): PnLData => {
  let totalIncome = 0;

  if (accountingMethod === 'cash') {
    // CASH BASIS: Income = actual payments received in the period
    // Filter payments by company, year, month
    const filteredPayments = (payments || []).filter((payment) => {
      // Filter by company
      if (companyId && payment.companyId !== companyId) return false;

      // Filter by payment date
      const paymentDate = new Date(payment.paymentDate);
      if (isNaN(paymentDate.getTime())) return false;

      if (year && paymentDate.getFullYear() !== year) return false;
      if (month && paymentDate.getMonth() + 1 !== month) return false;

      return true;
    });

    // Sum all payments received (use amountInInr for accuracy)
    totalIncome = filteredPayments.reduce((sum, payment) => {
      // Prefer amountInInr, fall back to amount
      const amount = payment.amountInInr != null && payment.amountInInr > 0
        ? payment.amountInInr
        : payment.amount || 0;
      return sum + amount;
    }, 0);
  } else {
    // ACCRUAL BASIS: Income = all invoices raised in the period (regardless of payment status)
    // Filter invoices by company, year, month (use invoice date, not payment date)
    const accrualInvoices = invoices.filter((inv) => {
      // Filter by company
      if (companyId && inv.companyId !== companyId) return false;

      // Filter by invoice date (when the income was earned)
      const invoiceDate = new Date(inv.invoiceDate);
      if (isNaN(invoiceDate.getTime())) return false;

      if (year && invoiceDate.getFullYear() !== year) return false;
      if (month && invoiceDate.getMonth() + 1 !== month) return false;

      return true;
    });

    // Calculate total income from invoice amounts (convert to INR)
    totalIncome = accrualInvoices.reduce((sum, inv) => {
      return sum + toInr(inv.totalAmount || 0, inv.currency);
    }, 0);
  }

  // Filter invoices for monthly breakdown (always needed for trends, regardless of accounting method)
  const filteredInvoices = invoices.filter((inv) => {
    // Filter by company
    if (companyId && inv.companyId !== companyId) return false;

    // Filter by year using invoice date
    const invoiceDate = new Date(inv.invoiceDate);
    if (isNaN(invoiceDate.getTime())) return false;

    if (year && invoiceDate.getFullYear() !== year) return false;
    // Don't filter by month for monthly breakdown - we need all months in the year

    return true;
  });

  // Filter salary transactions by company, year, month
  const filteredSalaries = salaryTransactions.filter((tx) => {
    if (companyId && tx.companyId !== companyId) return false;
    if (year && tx.salaryYear !== year) return false;
    if (month && tx.salaryMonth !== month) return false;
    return true;
  });

  // Calculate salary expenses (already in INR based on existing patterns)
  const salaryExpense = filteredSalaries.reduce((sum, tx) => {
    return sum + (tx.grossSalary || 0);
  }, 0);

  // Calculate total TDS deducted (for compliance reporting)
  const totalTDSDeducted = filteredSalaries.reduce((sum, tx) => {
    return sum + (tx.incomeTax || 0); // TDS is stored in incomeTax field
  }, 0);

  // Extract maintenance and depreciation from asset report
  const maintenanceExpense = assetReport?.totalMaintenanceCost || 0;
  
  // Calculate subscription expenses (only active subscriptions)
  let subscriptionExpense = 0;
  if (subscriptionExpenses && subscriptionExpenses.length > 0) {
    // Filter subscription expenses by year and month if specified
    const filteredSubscriptionExpenses = subscriptionExpenses.filter((exp) => {
      if (year && exp.year !== year) return false;
      if (month && exp.month !== month) return false;
      return true;
    });
    // Sum up the costs (already in INR from backend)
    subscriptionExpense = filteredSubscriptionExpenses.reduce((sum, exp) => sum + (exp.totalCostInInr || 0), 0);
  }

  // Calculate loan interest expense (Section 36(1)(iii) - fully deductible)
  let loanInterestExpense = 0;
  if (loanInterestPayments && loanInterestPayments.length > 0) {
    const filteredLoanInterest = loanInterestPayments.filter((tx) => {
      // Only EMI payments and interest accruals
      if (tx.transactionType !== 'emi_payment' && tx.transactionType !== 'interest_accrual') return false;
      
      // Must have interest amount
      if (!tx.interestAmount || tx.interestAmount <= 0) return false;
      
      // Filter by date
      if (!tx.transactionDate) return false;
      const txDate = new Date(tx.transactionDate);
      if (isNaN(txDate.getTime())) return false;
      
      if (year && txDate.getFullYear() !== year) return false;
      if (month && txDate.getMonth() + 1 !== month) return false;
      
      return true;
    });
    
    loanInterestExpense = filteredLoanInterest.reduce((sum, tx) => sum + (tx.interestAmount || 0), 0);
  }
  
  // Calculate OPEX asset expenses - filter OPEX assets by purchase date
  let opexAssetExpense = 0;
  if (assets && assets.length > 0) {
    const filteredOpexAssets = assets.filter((asset) => {
      // Filter by company
      if (companyId && asset.companyId !== companyId) return false;
      
      // Only OPEX assets
      if (!asset.purchaseType || asset.purchaseType.toLowerCase() !== 'opex') return false;
      
      // Must have purchase cost
      if (!asset.purchaseCost || asset.purchaseCost <= 0) return false;
      
      // Filter by purchase date
      if (!asset.purchaseDate) return false;
      
      const purchaseDate = new Date(asset.purchaseDate);
      if (isNaN(purchaseDate.getTime())) return false;
      
      if (year && purchaseDate.getFullYear() !== year) return false;
      if (month && purchaseDate.getMonth() + 1 !== month) return false;
      
      return true;
    });
    
    opexAssetExpense = filteredOpexAssets.reduce((sum, asset) => {
      return sum + toInr(asset.purchaseCost || 0, asset.currency);
    }, 0);
  }
  
  // For depreciation, we need monthly depreciation
  // AssetCostReport provides accumulated depreciation, but we need monthly
  // For now, we'll estimate monthly depreciation from accumulated
  // In a real scenario, you'd calculate this based on asset age and depreciation method
  const totalAccumulatedDepreciation = assetReport?.totalAccumulatedDepreciation || 0;
  
  // Estimate monthly depreciation (this is a simplification)
  // In production, you'd calculate this per asset based on depreciation method and age
  const estimatedMonthlyDepreciation = totalAccumulatedDepreciation > 0
    ? totalAccumulatedDepreciation / 12 // Rough estimate: divide by 12
    : 0;

  // Group depreciation by category from assetReport
  const depreciationByCategory: Array<{ category: string; amount: number; rate: number }> = [];
  
  if (assetReport?.byCategory) {
    // Distribute monthly depreciation proportionally by category
    const totalNetBookValue = assetReport.byCategory.reduce((sum, cat) => sum + cat.netBookValue, 0);
    
    assetReport.byCategory.forEach((cat) => {
      if (cat.netBookValue > 0 && totalNetBookValue > 0) {
        const proportion = cat.netBookValue / totalNetBookValue;
        const categoryDepreciation = estimatedMonthlyDepreciation * proportion;
        
        // Use categoryId as category name (in production, you'd look up the actual category name)
        const categoryName = cat.categoryId ? `Category ${cat.categoryId}` : 'Other';
        
        depreciationByCategory.push({
          category: categoryName,
          amount: categoryDepreciation,
          rate: 15, // Default rate, can be enhanced with actual category mapping
        });
      }
    });
  }

  // If no category breakdown, use total depreciation
  if (depreciationByCategory.length === 0 && estimatedMonthlyDepreciation > 0) {
    depreciationByCategory.push({
      category: 'All Assets',
      amount: estimatedMonthlyDepreciation,
      rate: 15,
    });
  }

  const totalOpex = salaryExpense + maintenanceExpense + opexAssetExpense + subscriptionExpense + loanInterestExpense + 0; // + otherExpense (future)
  const ebitda = totalIncome - totalOpex;
  const netProfit = ebitda - estimatedMonthlyDepreciation;

  // Calculate monthly breakdown for trends
  const monthlyData = calculateMonthlyBreakdown(
    filteredInvoices,
    filteredSalaries,
    assetReport,
    year || new Date().getFullYear(),
    companyId,
    assets,
    subscriptionExpenses,
    loanInterestPayments,
    payments
  );

  return {
    totalIncome,
    salaryExpense,
    totalTDSDeducted,
    maintenanceExpense,
    opexAssetExpense,
    subscriptionExpense,
    loanInterestExpense,
    otherExpense: 0, // Placeholder
    totalOpex,
    ebitda,
    depreciation: estimatedMonthlyDepreciation,
    depreciationByCategory,
    netProfit,
    monthlyData,
  };
};

/**
 * Calculate monthly breakdown for trend analysis
 */
const calculateMonthlyBreakdown = (
  invoices: Invoice[],
  salaryTransactions: EmployeeSalaryTransaction[],
  assetReport: AssetCostReport | undefined,
  year: number,
  companyId?: string,
  assets?: Asset[],
  subscriptionExpenses?: SubscriptionMonthlyExpense[],
  loanInterestPayments?: LoanTransaction[],
  payments?: Payment[]
): Array<{ month: number; income: number; expenses: number; depreciation: number; profit: number }> => {
  const monthlyMap = new Map<number, { income: number; expenses: number; depreciation: number }>();

  // Initialize all months
  for (let m = 1; m <= 12; m++) {
    monthlyMap.set(m, { income: 0, expenses: 0, depreciation: 0 });
  }

  // Aggregate invoices by month (use actual INR amounts from payments when available)
  invoices.forEach((inv) => {
    const dateStr = inv.updatedAt || inv.createdAt;
    if (!dateStr) return;
    
    const d = new Date(dateStr);
    if (isNaN(d.getTime()) || d.getFullYear() !== year) return;
    
    const month = d.getMonth() + 1;
    const existing = monthlyMap.get(month) || { income: 0, expenses: 0, depreciation: 0 };
    
    // Use actual INR amounts from payments if available
    if (payments && payments.length > 0) {
      const invoicePayments = payments.filter(p => p.invoiceId === inv.id);
      if (invoicePayments.length > 0) {
        const actualInrReceived = invoicePayments.reduce((paymentSum, p) => {
          if (p.amountInInr != null && p.amountInInr > 0) {
            return paymentSum + p.amountInInr;
          }
          return paymentSum + toInr(p.amount || 0, inv.currency);
        }, 0);
        existing.income += actualInrReceived;
      } else {
        existing.income += toInr(inv.totalAmount || 0, inv.currency);
      }
    } else {
      existing.income += toInr(inv.totalAmount || 0, inv.currency);
    }
    
    monthlyMap.set(month, existing);
  });

  // Aggregate salary transactions by month
  salaryTransactions.forEach((tx) => {
    if (tx.salaryYear !== year) return;
    const month = tx.salaryMonth;
    const existing = monthlyMap.get(month) || { income: 0, expenses: 0, depreciation: 0 };
    existing.expenses += tx.grossSalary || 0;
    monthlyMap.set(month, existing);
  });

  // Add maintenance expenses (distributed evenly across months for simplicity)
  // In production, you'd track maintenance by actual date
  const monthlyMaintenance = (assetReport?.totalMaintenanceCost || 0) / 12;
  const monthlyDepreciation = (assetReport?.totalAccumulatedDepreciation || 0) / 12;

  // Add subscription expenses by month
  const monthlySubscriptionMap = new Map<number, number>();
  if (subscriptionExpenses && subscriptionExpenses.length > 0) {
    subscriptionExpenses.forEach((exp) => {
      if (exp.year === year) {
        monthlySubscriptionMap.set(exp.month, (monthlySubscriptionMap.get(exp.month) || 0) + (exp.totalCostInInr || 0));
      }
    });
  }

  // Calculate OPEX asset expenses by month
  const monthlyOpexMap = new Map<number, number>();
  if (assets && assets.length > 0) {
    assets.forEach((asset) => {
      // Filter by company
      if (companyId && asset.companyId !== companyId) return;
      
      // Only OPEX assets
      if (!asset.purchaseType || asset.purchaseType.toLowerCase() !== 'opex') return;
      
      // Must have purchase cost and date
      if (!asset.purchaseCost || asset.purchaseCost <= 0 || !asset.purchaseDate) return;
      
      const purchaseDate = new Date(asset.purchaseDate);
      if (isNaN(purchaseDate.getTime()) || purchaseDate.getFullYear() !== year) return;
      
      const month = purchaseDate.getMonth() + 1;
      const existing = monthlyOpexMap.get(month) || 0;
      monthlyOpexMap.set(month, existing + toInr(asset.purchaseCost, asset.currency));
    });
  }

  // Calculate loan interest expenses by month
  const monthlyLoanInterestMap = new Map<number, number>();
  if (loanInterestPayments && loanInterestPayments.length > 0) {
    loanInterestPayments.forEach((tx) => {
      // Only EMI payments and interest accruals
      if (tx.transactionType !== 'emi_payment' && tx.transactionType !== 'interest_accrual') return;
      if (!tx.interestAmount || tx.interestAmount <= 0) return;
      
      if (!tx.transactionDate) return;
      const txDate = new Date(tx.transactionDate);
      if (isNaN(txDate.getTime()) || txDate.getFullYear() !== year) return;
      
      const month = txDate.getMonth() + 1;
      const existing = monthlyLoanInterestMap.get(month) || 0;
      monthlyLoanInterestMap.set(month, existing + (tx.interestAmount || 0));
    });
  }

  // Convert to array and calculate profit
  return Array.from({ length: 12 }, (_, i) => {
    const month = i + 1;
    const data = monthlyMap.get(month) || { income: 0, expenses: 0, depreciation: 0 };
    const monthlyOpex = monthlyOpexMap.get(month) || 0;
    const monthlySubscription = monthlySubscriptionMap.get(month) || 0;
    const monthlyLoanInterest = monthlyLoanInterestMap.get(month) || 0;
    
    const totalExpenses = data.expenses + monthlyMaintenance + monthlyOpex + monthlySubscription + monthlyLoanInterest;
    
    return {
      month,
      income: data.income,
      expenses: totalExpenses,
      depreciation: monthlyDepreciation,
      profit: data.income - totalExpenses - monthlyDepreciation,
    };
  });
};


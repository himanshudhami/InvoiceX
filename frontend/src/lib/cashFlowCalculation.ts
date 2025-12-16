/**
 * Cash Flow Statement calculation utilities
 * Compliant with Indian Accounting Standard 3 (AS-3)
 * Uses indirect method for operating activities
 */

import { Invoice, EmployeeSalaryTransaction, AssetCostReport, Asset, SubscriptionMonthlyExpense, LoanTransaction, Payment } from '@/services/api/types';
import { toInr } from './financialUtils';
import { PnLData } from './pnlCalculation';

export interface CashFlowData {
  // Operating Activities
  netProfitBeforeTax: number;
  adjustmentsForNonCashItems: {
    depreciation: number;
    loanInterest: number; // Added back (non-cash)
  };
  changesInWorkingCapital: {
    accountsReceivable: number;
    accountsPayable: number;
    netChange: number;
  };
  cashFromOperatingActivities: number;
  
  // Investing Activities
  purchaseOfFixedAssets: number;
  saleOfFixedAssets: number;
  cashFromInvestingActivities: number;
  
  // Financing Activities
  loanDisbursements: number;
  loanRepayments: number;
  cashFromFinancingActivities: number;
  
  // Net Cash Flow
  netIncreaseDecreaseInCash: number;
  openingCashBalance: number;
  closingCashBalance: number;
  
  // Monthly breakdown
  monthlyData: Array<{
    month: number;
    operating: number;
    investing: number;
    financing: number;
    netCashFlow: number;
  }>;
  
  // Detailed breakdowns
  operatingDetails: {
    cashReceiptsFromCustomers: number;
    cashPaidToEmployees: number;
    cashPaidForSubscriptions: number;
    cashPaidForOpexAssets: number;
    cashPaidForMaintenance: number;
    tdsPayments: number;
    depreciationAddedBack: number;
    loanInterestAddedBack: number;
  };
  investingDetails: {
    capexAssetPurchases: number;
    assetDisposals: number;
  };
  financingDetails: {
    loanDisbursementsReceived: number;
    loanPrincipalRepayments: number;
    loanInterestPayments: number;
  };
}

/**
 * Calculate Cash Flow Statement from various data sources
 * Uses indirect method for operating activities (AS-3 compliant)
 */
export const calculateCashFlow = (
  pnlData: PnLData | null,
  invoices: Invoice[],
  payments: Payment[],
  salaryTransactions: EmployeeSalaryTransaction[],
  assetReport: AssetCostReport | undefined,
  companyId?: string,
  year?: number,
  month?: number,
  assets?: Asset[],
  subscriptionExpenses?: SubscriptionMonthlyExpense[],
  loanTransactions?: LoanTransaction[]
): CashFlowData | null => {
  if (!pnlData) {
    return null;
  }

  // Calculate date range
  const fromDate = month 
    ? new Date(year || new Date().getFullYear(), month - 1, 1)
    : new Date(year || new Date().getFullYear(), 0, 1);
  const toDate = month
    ? new Date(year || new Date().getFullYear(), month, 0)
    : new Date(year || new Date().getFullYear(), 11, 31);

  // Filter data by company, year, and month
  const filteredInvoices = invoices.filter((inv) => {
    if (companyId && inv.companyId !== companyId) return false;
    if (year) {
      const invDate = new Date(inv.invoiceDate);
      if (invDate.getFullYear() !== year) return false;
      if (month && invDate.getMonth() + 1 !== month) return false;
    }
    return true;
  });

  const filteredPayments = payments.filter((p) => {
    if (!p.paymentDate) return false;
    const payDate = new Date(p.paymentDate);
    if (year && payDate.getFullYear() !== year) return false;
    if (month && payDate.getMonth() + 1 !== month) return false;
    return true;
  });

  const filteredSalaries = salaryTransactions.filter((tx) => {
    if (companyId && tx.companyId !== companyId) return false;
    if (year && tx.salaryYear !== year) return false;
    if (month && tx.salaryMonth !== month) return false;
    // Only include paid transactions with payment dates
    if (tx.status !== 'paid' || !tx.paymentDate) return false;
    return true;
  });

  const filteredLoanTransactions = loanTransactions?.filter((tx) => {
    if (!tx.transactionDate) return false;
    const txDate = new Date(tx.transactionDate);
    if (year && txDate.getFullYear() !== year) return false;
    if (month && txDate.getMonth() + 1 !== month) return false;
    return true;
  }) || [];

  // OPERATING ACTIVITIES (Indirect Method)
  // Start with Net Profit Before Tax
  const netProfitBeforeTax = pnlData.netProfit;

  // Add back non-cash items
  const depreciation = pnlData.depreciation || 0;
  const loanInterest = pnlData.loanInterestExpense || 0;
  const adjustmentsForNonCashItems = depreciation + loanInterest;

  // Calculate cash receipts from customers (paid invoices with payment dates)
  // Use actual INR amount if available, otherwise convert using exchange rate
  const cashReceiptsFromCustomers = filteredPayments.reduce((sum, p) => {
    const invoice = filteredInvoices.find(inv => inv.id === p.invoiceId);
    if (!invoice) return sum;
    
    // Prefer actual INR amount received (for bank reconciliation accuracy)
    // Fall back to conversion if not provided
    if (p.amountInInr != null && p.amountInInr > 0) {
      return sum + p.amountInInr;
    }
    
    // Convert using exchange rate if actual INR not provided
    return sum + toInr(p.amount || 0, invoice.currency || 'INR');
  }, 0);

  // Calculate cash paid to employees (only paid salaries with payment dates)
  const cashPaidToEmployees = filteredSalaries.reduce((sum, tx) => {
    return sum + (tx.netSalary || 0);
  }, 0);

  // Calculate TDS payments
  const tdsPayments = filteredSalaries.reduce((sum, tx) => {
    return sum + (tx.incomeTax || 0);
  }, 0);

  // Calculate cash paid for subscriptions (if payment dates tracked)
  const cashPaidForSubscriptions = subscriptionExpenses?.filter((exp) => {
    if (year && exp.year !== year) return false;
    if (month && exp.month !== month) return false;
    return true;
  }).reduce((sum, exp) => sum + (exp.totalCostInInr || 0), 0) || 0;

  // Calculate cash paid for OPEX assets
  const cashPaidForOpexAssets = assets?.filter((asset) => {
    if (companyId && asset.companyId !== companyId) return false;
    if (!asset.purchaseType || asset.purchaseType.toLowerCase() !== 'opex') return false;
    if (!asset.purchaseDate) return false;
    const purchaseDate = new Date(asset.purchaseDate);
    if (year && purchaseDate.getFullYear() !== year) return false;
    if (month && purchaseDate.getMonth() + 1 !== month) return false;
    return true;
  }).reduce((sum, asset) => {
    return sum + toInr(asset.purchaseCost || 0, asset.currency || 'INR');
  }, 0) || 0;

  // Maintenance costs (from asset report, but may not have payment dates)
  const cashPaidForMaintenance = 0; // Placeholder - maintenance payment dates not tracked

  // Calculate changes in working capital
  // Accounts Receivable: Unpaid invoices
  const accountsReceivable = filteredInvoices
    .filter(inv => inv.status && !['paid', 'cancelled'].includes(inv.status.toLowerCase()))
    .reduce((sum, inv) => {
      const paid = inv.paidAmount || 0;
      return sum + toInr((inv.totalAmount || 0) - paid, inv.currency || 'INR');
    }, 0);

  // Accounts Payable: Not fully tracked yet
  const accountsPayable = 0;

  // For simplicity, we'll use a simplified calculation
  // In production, you'd compare AR/AP at start vs end of period
  const changesInWorkingCapital = accountsPayable - accountsReceivable; // Simplified

  // Operating cash flow (Indirect method)
  const operatingCashBeforeWorkingCapital = netProfitBeforeTax + adjustmentsForNonCashItems;
  const cashFromOperatingActivities = operatingCashBeforeWorkingCapital + changesInWorkingCapital;

  // INVESTING ACTIVITIES
  // CAPEX asset purchases
  const purchaseOfFixedAssets = assets?.filter((asset) => {
    if (companyId && asset.companyId !== companyId) return false;
    if (!asset.purchaseType || asset.purchaseType.toLowerCase() !== 'capex') return false;
    if (!asset.purchaseDate) return false;
    const purchaseDate = new Date(asset.purchaseDate);
    if (year && purchaseDate.getFullYear() !== year) return false;
    if (month && purchaseDate.getMonth() + 1 !== month) return false;
    return true;
  }).reduce((sum, asset) => {
    return sum + toInr(asset.purchaseCost || 0, asset.currency || 'INR');
  }, 0) || 0;

  // Asset disposals (not fully tracked yet)
  const saleOfFixedAssets = 0;

  const cashFromInvestingActivities = saleOfFixedAssets - purchaseOfFixedAssets;

  // FINANCING ACTIVITIES
  // Loan disbursements
  const loanDisbursements = filteredLoanTransactions
    .filter(tx => tx.transactionType === 'disbursement')
    .reduce((sum, tx) => sum + (tx.amount || 0), 0);

  // Loan principal repayments
  const loanRepayments = filteredLoanTransactions
    .filter(tx => ['emi_payment', 'prepayment', 'foreclosure'].includes(tx.transactionType))
    .reduce((sum, tx) => sum + (tx.principalAmount || 0), 0);

  const cashFromFinancingActivities = loanDisbursements - loanRepayments;

  // NET CASH FLOW
  const netIncreaseDecreaseInCash = cashFromOperatingActivities
    + cashFromInvestingActivities
    + cashFromFinancingActivities;

  // Opening and closing cash balance (simplified - would need actual cash balance tracking)
  const openingCashBalance = 0; // Placeholder
  const closingCashBalance = openingCashBalance + netIncreaseDecreaseInCash;

  // Calculate monthly breakdown
  const monthlyData = calculateMonthlyBreakdown(
    invoices,
    payments,
    salaryTransactions,
    assets,
    loanTransactions,
    companyId,
    year || new Date().getFullYear()
  );

  return {
    netProfitBeforeTax,
    adjustmentsForNonCashItems: {
      depreciation,
      loanInterest,
    },
    changesInWorkingCapital: {
      accountsReceivable,
      accountsPayable,
      netChange: changesInWorkingCapital,
    },
    cashFromOperatingActivities,
    purchaseOfFixedAssets,
    saleOfFixedAssets,
    cashFromInvestingActivities,
    loanDisbursements,
    loanRepayments,
    cashFromFinancingActivities,
    netIncreaseDecreaseInCash,
    openingCashBalance,
    closingCashBalance,
    monthlyData,
    operatingDetails: {
      cashReceiptsFromCustomers,
      cashPaidToEmployees,
      cashPaidForSubscriptions,
      cashPaidForOpexAssets,
      cashPaidForMaintenance,
      tdsPayments,
      depreciationAddedBack: depreciation,
      loanInterestAddedBack: loanInterest,
    },
    investingDetails: {
      capexAssetPurchases: purchaseOfFixedAssets,
      assetDisposals: saleOfFixedAssets,
    },
    financingDetails: {
      loanDisbursementsReceived: loanDisbursements,
      loanPrincipalRepayments: loanRepayments,
      loanInterestPayments: loanInterest,
    },
  };
};

/**
 * Calculate monthly breakdown for trend analysis
 */
const calculateMonthlyBreakdown = (
  invoices: Invoice[],
  payments: Payment[],
  salaryTransactions: EmployeeSalaryTransaction[],
  assets: Asset[] | undefined,
  loanTransactions: LoanTransaction[] | undefined,
  companyId: string | undefined,
  year: number
): Array<{
  month: number;
  operating: number;
  investing: number;
  financing: number;
  netCashFlow: number;
}> => {
  const monthlyMap = new Map<number, { operating: number; investing: number; financing: number }>();

  // Initialize all months
  for (let m = 1; m <= 12; m++) {
    monthlyMap.set(m, { operating: 0, investing: 0, financing: 0 });
  }

  // Operating: Cash receipts from customers
  payments.forEach((p) => {
    if (!p.paymentDate) return;
    const payDate = new Date(p.paymentDate);
    if (payDate.getFullYear() !== year) return;
    const month = payDate.getMonth() + 1;
    const existing = monthlyMap.get(month) || { operating: 0, investing: 0, financing: 0 };
    existing.operating += p.amount || 0;
    monthlyMap.set(month, existing);
  });

  // Operating: Cash paid to employees
  salaryTransactions.forEach((tx) => {
    if (tx.salaryYear !== year || tx.status !== 'paid' || !tx.paymentDate) return;
    const month = tx.salaryMonth;
    const existing = monthlyMap.get(month) || { operating: 0, investing: 0, financing: 0 };
    existing.operating -= tx.netSalary || 0; // Negative for cash outflow
    monthlyMap.set(month, existing);
  });

  // Investing: Asset purchases
  assets?.forEach((asset) => {
    if (companyId && asset.companyId !== companyId) return;
    if (!asset.purchaseDate) return;
    const purchaseDate = new Date(asset.purchaseDate);
    if (purchaseDate.getFullYear() !== year) return;
    const month = purchaseDate.getMonth() + 1;
    const existing = monthlyMap.get(month) || { operating: 0, investing: 0, financing: 0 };
    if (asset.purchaseType?.toLowerCase() === 'capex') {
      existing.investing -= toInr(asset.purchaseCost || 0, asset.currency || 'INR');
    }
    monthlyMap.set(month, existing);
  });

  // Financing: Loan transactions
  loanTransactions?.forEach((tx) => {
    if (!tx.transactionDate) return;
    const txDate = new Date(tx.transactionDate);
    if (txDate.getFullYear() !== year) return;
    const month = txDate.getMonth() + 1;
    const existing = monthlyMap.get(month) || { operating: 0, investing: 0, financing: 0 };
    
    if (tx.transactionType === 'disbursement') {
      existing.financing += tx.amount || 0;
    } else if (['emi_payment', 'prepayment', 'foreclosure'].includes(tx.transactionType)) {
      existing.financing -= tx.principalAmount || 0;
    }
    monthlyMap.set(month, existing);
  });

  // Convert to array
  return Array.from({ length: 12 }, (_, i) => {
    const month = i + 1;
    const data = monthlyMap.get(month) || { operating: 0, investing: 0, financing: 0 };
    return {
      month,
      operating: data.operating,
      investing: data.investing,
      financing: data.financing,
      netCashFlow: data.operating + data.investing + data.financing,
    };
  });
};





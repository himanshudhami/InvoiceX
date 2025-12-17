/**
 * Balance Sheet calculation utilities
 * Shows assets, liabilities, and equity for Indian tax compliance
 */

import { AssetCostReport, Asset, Loan } from '@/services/api/types';
import { toInr } from './financialUtils';

export interface BalanceSheetData {
  // Assets
  fixedAssets: {
    grossBlock: number; // Total CAPEX purchase cost
    accumulatedDepreciation: number;
    netBlock: number; // Gross block - accumulated depreciation
  };
  currentAssets: {
    total: number; // Placeholder for future
  };
  totalAssets: number;

  // Liabilities
  currentLiabilities: {
    total: number;
    loanLiabilities: number; // Principal due within 12 months
  };
  longTermLiabilities: {
    total: number;
    loanLiabilities: number; // Principal due after 12 months
  };
  totalLiabilities: number;

  // Equity (placeholder for future)
  equity: {
    total: number;
  };

  // Validation
  isBalanced: boolean; // Assets should equal Liabilities + Equity
}

/**
 * Calculate principal due in next 12 months for a loan
 * Uses a simple estimation: if remaining tenure <= 12 months, all outstanding is current
 * Otherwise, estimates based on EMI amount and remaining tenure
 */
const calculateCurrentLoanLiability = (loan: Loan, asOfDate: Date): number => {
  if (loan.status !== 'active' || loan.outstandingPrincipal <= 0) {
    return 0;
  }

  const loanStartDate = new Date(loan.loanStartDate);
  const monthsElapsed = Math.max(0, 
    (asOfDate.getFullYear() - loanStartDate.getFullYear()) * 12 +
    (asOfDate.getMonth() - loanStartDate.getMonth())
  );
  const remainingMonths = Math.max(0, loan.tenureMonths - monthsElapsed);

  // If loan ends within 12 months, all outstanding is current
  if (remainingMonths <= 12) {
    return loan.outstandingPrincipal;
  }

  // Estimate principal due in next 12 months
  // Approximate: 12 EMIs worth of principal
  // This is a simplification - in reality, we'd need the actual EMI schedule
  const monthlyPrincipalApprox = loan.outstandingPrincipal / remainingMonths;
  const currentLiability = Math.min(loan.outstandingPrincipal, monthlyPrincipalApprox * 12);
  
  return Math.round(currentLiability * 100) / 100;
};

/**
 * Calculate Balance Sheet from asset report and outstanding loans
 */
export const calculateBalanceSheet = (
  assetReport: AssetCostReport | undefined,
  companyId?: string,
  asOfDate?: Date,
  outstandingLoans?: Loan[]
): BalanceSheetData => {
  const asOf = asOfDate || new Date();

  // Fixed Assets (CAPEX only)
  const grossBlock = assetReport?.totalCapexPurchase || 0;
  const accumulatedDepreciation = assetReport?.totalAccumulatedDepreciation || 0;
  const netBlock = Math.max(0, grossBlock - accumulatedDepreciation);

  // Current Assets (placeholder - can be extended with cash, receivables, etc.)
  const currentAssets = {
    total: 0,
  };

  // Total Assets
  const totalAssets = netBlock + currentAssets.total;

  // Calculate loan liabilities
  const loans = outstandingLoans || [];
  let currentLoanLiabilities = 0;
  let longTermLoanLiabilities = 0;

  loans.forEach(loan => {
    const currentLiability = calculateCurrentLoanLiability(loan, asOf);
    currentLoanLiabilities += currentLiability;
    longTermLoanLiabilities += Math.max(0, loan.outstandingPrincipal - currentLiability);
  });

  // Liabilities
  const currentLiabilities = {
    total: currentLoanLiabilities,
    loanLiabilities: currentLoanLiabilities,
  };
  const longTermLiabilities = {
    total: longTermLoanLiabilities,
    loanLiabilities: longTermLoanLiabilities,
  };
  const totalLiabilities = currentLiabilities.total + longTermLiabilities.total;

  // Equity (typically calculated as Assets - Liabilities)
  const equity = {
    total: totalAssets - totalLiabilities,
  };

  // Validation: Assets should equal Liabilities + Equity
  const isBalanced = Math.abs(totalAssets - (totalLiabilities + equity.total)) < 0.01;

  return {
    fixedAssets: {
      grossBlock,
      accumulatedDepreciation,
      netBlock,
    },
    currentAssets,
    totalAssets,
    currentLiabilities,
    longTermLiabilities,
    totalLiabilities,
    equity,
    isBalanced,
  };
};


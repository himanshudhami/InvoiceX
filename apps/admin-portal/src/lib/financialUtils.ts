/**
 * Financial utility functions for currency conversion and formatting
 * Used for India tax-compliant financial reporting
 */

/**
 * Convert any currency to INR
 * Fixed rate: 1 USD = 88 INR (updated rate)
 */
export const toInr = (amount: number, currency?: string): number => {
  if (!currency || currency.toUpperCase() === 'INR') return amount;
  if (currency.toUpperCase() === 'USD') return amount * 88;
  // For other currencies, treat as INR for now (can be extended)
  return amount;
};

/**
 * Format amount as Indian Rupees (INR)
 * Uses Indian number format with lakhs/crores
 */
export const formatINR = (amount: number): string => {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 0,
  }).format(amount);
};

/**
 * Format amount with 2 decimal places for detailed reports
 */
export const formatINRDetailed = (amount: number): string => {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
};

/**
 * Calculate percentage change between two values
 */
export const calculatePercentageChange = (current: number, previous: number): number => {
  if (previous === 0) return current > 0 ? 100 : 0;
  return ((current - previous) / previous) * 100;
};

/**
 * Format percentage with sign
 */
export const formatPercentageChange = (change: number): string => {
  const sign = change >= 0 ? '+' : '';
  return `${sign}${change.toFixed(1)}%`;
};





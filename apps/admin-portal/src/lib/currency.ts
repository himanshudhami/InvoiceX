import { CURRENCIES } from './constants';

/**
 * Get currency symbol for a given currency code
 */
export function getCurrencySymbol(currencyCode: string): string {
  const currency = CURRENCIES.find(c => c.value === currencyCode);
  return currency?.symbol || '$';
}

/**
 * Format a number as currency
 */
export function formatCurrency(
  amount: number,
  currencyCode?: string,
  locale: string = 'en-US'
): string {
  const currency = currencyCode || 'USD';
  const symbol = getCurrencySymbol(currency);

  // Use Intl.NumberFormat for proper formatting
  const formatter = new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  // For currencies that don't have full Intl support, fall back to manual formatting
  if (currency === 'INR' || currency === 'EUR' || currency === 'GBP') {
    return `${symbol}${amount.toFixed(2)}`;
  }

  return formatter.format(amount);
}

/**
 * Parse a currency string back to number
 */
export function parseCurrency(currencyString: string): number {
  // Remove currency symbols and commas, then parse
  const cleaned = currencyString
    .replace(/[$,€£₹]/g, '')
    .replace(/,/g, '')
    .trim();

  const parsed = parseFloat(cleaned);
  return isNaN(parsed) ? 0 : parsed;
}

/**
 * Get currency label for display
 */
export function getCurrencyLabel(currencyCode: string): string {
  const currency = CURRENCIES.find(c => c.value === currencyCode);
  return currency?.label || 'USD ($)';
}

/**
 * Format amount as INR (Indian Rupees)
 */
export function formatINR(amount: number | undefined | null): string {
  if (amount === undefined || amount === null || isNaN(amount)) return '₹0';
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
    minimumFractionDigits: 2,
  }).format(amount);
}

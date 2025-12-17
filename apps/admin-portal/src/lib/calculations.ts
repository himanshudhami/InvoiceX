// Calculation utilities for invoices and quotes

export interface LineItem {
  description: string;
  quantity: number;
  unitPrice: number;
  taxRate?: number;
  discountRate?: number;
}

export interface CalculationResult {
  subtotal: number;
  taxAmount: number;
  discountAmount: number;
  totalAmount: number;
}

/**
 * Calculate line item total (quantity * unitPrice)
 */
export function calculateLineTotal(lineItem: LineItem): number {
  return lineItem.quantity * lineItem.unitPrice;
}

/**
 * Calculate subtotal from line items
 */
export function calculateSubtotal(lineItems: LineItem[]): number {
  return lineItems.reduce((total, item) => total + calculateLineTotal(item), 0);
}

/**
 * Calculate total tax amount from line items
 */
export function calculateTaxAmount(lineItems: LineItem[], defaultTaxRate?: number): number {
  return lineItems.reduce((total, item) => {
    const taxRate = item.taxRate ?? defaultTaxRate ?? 0;
    const lineTotal = calculateLineTotal(item);
    return total + (lineTotal * taxRate / 100);
  }, 0);
}

/**
 * Calculate total discount amount from line items
 */
export function calculateDiscountAmount(lineItems: LineItem[], defaultDiscountRate?: number): number {
  return lineItems.reduce((total, item) => {
    const discountRate = item.discountRate ?? defaultDiscountRate ?? 0;
    const lineTotal = calculateLineTotal(item);
    return total + (lineTotal * discountRate / 100);
  }, 0);
}

/**
 * Calculate all totals for invoice/quote
 */
export function calculateTotals(
  lineItems: LineItem[],
  options: {
    defaultTaxRate?: number;
    defaultDiscountRate?: number;
    globalDiscountAmount?: number;
    globalDiscountType?: 'fixed' | 'percentage';
    globalDiscountValue?: number;
  } = {}
): CalculationResult {
  const {
    defaultTaxRate,
    defaultDiscountRate,
    globalDiscountAmount = 0,
    globalDiscountType = 'fixed',
    globalDiscountValue = 0
  } = options;

  const subtotal = calculateSubtotal(lineItems);
  const lineItemTaxAmount = calculateTaxAmount(lineItems, defaultTaxRate);
  const lineItemDiscountAmount = calculateDiscountAmount(lineItems, defaultDiscountRate);

  // Apply global discount
  let globalDiscount = 0;
  if (globalDiscountType === 'percentage' && globalDiscountValue > 0) {
    globalDiscount = (subtotal - lineItemDiscountAmount) * globalDiscountValue / 100;
  } else if (globalDiscountType === 'fixed' && globalDiscountAmount > 0) {
    globalDiscount = globalDiscountAmount;
  }

  const totalDiscount = lineItemDiscountAmount + globalDiscount;
  const taxableAmount = subtotal - totalDiscount;
  const totalTax = taxableAmount > 0 ? lineItemTaxAmount : 0;
  const totalAmount = taxableAmount + totalTax;

  return {
    subtotal,
    taxAmount: totalTax,
    discountAmount: totalDiscount,
    totalAmount: Math.max(0, totalAmount) // Ensure total is never negative
  };
}

/**
 * Calculate outstanding amount (total - paid)
 */
export function calculateOutstandingAmount(totalAmount: number, paidAmount: number): number {
  return Math.max(0, totalAmount - paidAmount);
}

/**
 * Check if an amount is overdue based on due date
 */
export function isOverdue(totalAmount: number, paidAmount: number, dueDate: string): boolean {
  const outstanding = calculateOutstandingAmount(totalAmount, paidAmount);
  if (outstanding === 0) return false;

  const now = new Date();
  const due = new Date(dueDate);
  return now > due;
}

/**
 * Format percentage for display
 */
export function formatPercentage(rate: number): string {
  return `${rate.toFixed(2)}%`;
}

/**
 * Round to 2 decimal places for currency
 */
export function roundToCurrency(amount: number): number {
  return Math.round(amount * 100) / 100;
}

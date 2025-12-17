// Centralized constants for the invoice application

export const CURRENCIES = [
  { value: 'USD', label: 'USD ($)', symbol: '$' },
  { value: 'EUR', label: 'EUR (€)', symbol: '€' },
  { value: 'GBP', label: 'GBP (£)', symbol: '£' },
  { value: 'INR', label: 'INR (₹)', symbol: '₹' },
  { value: 'CAD', label: 'CAD ($)', symbol: '$' },
  { value: 'AUD', label: 'AUD ($)', symbol: '$' },
] as const;

export const INVOICE_STATUSES = [
  { value: 'draft', label: 'Draft' },
  { value: 'sent', label: 'Sent' },
  { value: 'viewed', label: 'Viewed' },
  { value: 'paid', label: 'Paid' },
  { value: 'overdue', label: 'Overdue' },
  { value: 'cancelled', label: 'Cancelled' },
] as const;

export const QUOTE_STATUSES = [
  { value: 'draft', label: 'Draft' },
  { value: 'sent', label: 'Sent' },
  { value: 'viewed', label: 'Viewed' },
  { value: 'accepted', label: 'Accepted' },
  { value: 'rejected', label: 'Rejected' },
  { value: 'expired', label: 'Expired' },
] as const;

export const PRODUCT_TYPES = [
  { value: 'service', label: 'Service' },
  { value: 'product', label: 'Product' },
  { value: 'digital', label: 'Digital' },
] as const;

export const PRODUCT_CATEGORIES = [
  'General',
  'Software',
  'Hardware',
  'Consulting',
  'Support',
  'Training',
  'Maintenance',
  'Development',
  'Design',
  'Marketing',
  'Legal',
  'Accounting',
  'Other'
] as const;

export const PRODUCT_UNITS = [
  'Each',
  'Hour',
  'Day',
  'Week',
  'Month',
  'Year',
  'Item',
  'Unit',
  'Piece',
  'Box',
  'Package',
  'Service',
  'License',
  'User',
  'GB',
  'MB',
  'TB'
] as const;

export const PAYMENT_TERMS = [
  { value: 0, label: 'Due on receipt' },
  { value: 7, label: 'Net 7 days' },
  { value: 15, label: 'Net 15 days' },
  { value: 30, label: 'Net 30 days' },
  { value: 45, label: 'Net 45 days' },
  { value: 60, label: 'Net 60 days' },
] as const;

// Type exports for better TypeScript support
export type CurrencyCode = typeof CURRENCIES[number]['value'];
export type InvoiceStatus = typeof INVOICE_STATUSES[number]['value'];
export type QuoteStatus = typeof QUOTE_STATUSES[number]['value'];
export type ProductType = typeof PRODUCT_TYPES[number]['value'];
export type ProductCategory = typeof PRODUCT_CATEGORIES[number];
export type ProductUnit = typeof PRODUCT_UNITS[number];

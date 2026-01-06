import type {
  VendorsFilterParams,
  VendorInvoicesFilterParams,
  VendorPaymentsFilterParams,
} from '@/services/api/types';

export const vendorKeys = {
  all: ['vendors'] as const,
  lists: () => [...vendorKeys.all, 'list'] as const,
  list: (companyId?: string) => [...vendorKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: VendorsFilterParams) => [...vendorKeys.lists(), 'paged', params ?? {}] as const,
  details: () => [...vendorKeys.all, 'detail'] as const,
  detail: (id: string) => [...vendorKeys.details(), id] as const,
  outstanding: (companyId: string) => [...vendorKeys.all, 'outstanding', companyId] as const,
  vendorOutstanding: (vendorId: string) => [...vendorKeys.all, 'vendor-outstanding', vendorId] as const,
  aging: (companyId: string, asOfDate?: string) => [...vendorKeys.all, 'aging', companyId, asOfDate] as const,
  tdsSummary: (companyId: string, financialYear?: string) => [...vendorKeys.all, 'tds-summary', companyId, financialYear] as const,
};

export const vendorInvoiceKeys = {
  all: ['vendor-invoices'] as const,
  lists: () => [...vendorInvoiceKeys.all, 'list'] as const,
  list: (companyId?: string) => [...vendorInvoiceKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: VendorInvoicesFilterParams) => [...vendorInvoiceKeys.lists(), 'paged', params ?? {}] as const,
  byVendor: (vendorId: string, params?: VendorInvoicesFilterParams) =>
    [...vendorInvoiceKeys.lists(), 'by-vendor', vendorId, params ?? {}] as const,
  details: () => [...vendorInvoiceKeys.all, 'detail'] as const,
  detail: (id: string) => [...vendorInvoiceKeys.details(), id] as const,
  unpaid: (vendorId: string) => [...vendorInvoiceKeys.all, 'unpaid', vendorId] as const,
};

export const vendorPaymentKeys = {
  all: ['vendor-payments'] as const,
  lists: () => [...vendorPaymentKeys.all, 'list'] as const,
  list: (companyId?: string) => [...vendorPaymentKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: VendorPaymentsFilterParams) => [...vendorPaymentKeys.lists(), 'paged', params ?? {}] as const,
  byVendor: (vendorId: string, params?: VendorPaymentsFilterParams) =>
    [...vendorPaymentKeys.lists(), 'by-vendor', vendorId, params ?? {}] as const,
  details: () => [...vendorPaymentKeys.all, 'detail'] as const,
  detail: (id: string) => [...vendorPaymentKeys.details(), id] as const,
  allocations: (paymentId: string) => [...vendorPaymentKeys.all, 'allocations', paymentId] as const,
};

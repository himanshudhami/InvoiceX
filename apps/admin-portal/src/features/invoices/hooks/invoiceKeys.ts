import type { InvoicesFilterParams } from '@/services/api/types'

export const invoiceKeys = {
  all: ['invoices'] as const,
  lists: () => [...invoiceKeys.all, 'list'] as const,
  list: (companyId?: string) => [...invoiceKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: InvoicesFilterParams) => [...invoiceKeys.lists(), 'paged', params ?? {}] as const,
  details: () => [...invoiceKeys.all, 'detail'] as const,
  detail: (id: string) => [...invoiceKeys.details(), id] as const,
}

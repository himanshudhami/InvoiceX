import type { CustomersFilterParams } from '@/services/api/types'

export const customerKeys = {
  all: ['customers'] as const,
  lists: () => [...customerKeys.all, 'list'] as const,
  list: (companyId?: string) => [...customerKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: CustomersFilterParams) => [...customerKeys.lists(), 'paged', params ?? {}] as const,
  details: () => [...customerKeys.all, 'detail'] as const,
  detail: (id: string) => [...customerKeys.details(), id] as const,
}

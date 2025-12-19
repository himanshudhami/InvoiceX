import type { ProductsFilterParams } from '@/services/api/types'

export const productKeys = {
  all: ['products'] as const,
  lists: () => [...productKeys.all, 'list'] as const,
  list: (companyId?: string) => [...productKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: ProductsFilterParams) => [...productKeys.lists(), 'paged', params ?? {}] as const,
  details: () => [...productKeys.all, 'detail'] as const,
  detail: (id: string) => [...productKeys.details(), id] as const,
}

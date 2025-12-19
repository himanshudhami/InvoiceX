import type { QuotesFilterParams } from '@/services/api/types'

export const quoteKeys = {
  all: ['quotes'] as const,
  lists: () => [...quoteKeys.all, 'list'] as const,
  list: (companyId?: string) => [...quoteKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: QuotesFilterParams) => [...quoteKeys.lists(), 'paged', params ?? {}] as const,
  details: () => [...quoteKeys.all, 'detail'] as const,
  detail: (id: string) => [...quoteKeys.details(), id] as const,
}

import type { PaginationParams } from '@/services/api/types'

export const quoteKeys = {
  all: ['quotes'] as const,
  lists: () => [...quoteKeys.all, 'list'] as const,
  list: (params?: PaginationParams) => [...quoteKeys.lists(), params ?? {}] as const,
  details: () => [...quoteKeys.all, 'detail'] as const,
  detail: (id: string) => [...quoteKeys.details(), id] as const,
}

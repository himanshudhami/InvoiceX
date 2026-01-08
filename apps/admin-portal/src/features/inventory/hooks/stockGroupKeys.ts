import type { StockGroupFilterParams } from '@/services/api/types';

export const stockGroupKeys = {
  all: ['stockGroups'] as const,
  lists: () => [...stockGroupKeys.all, 'list'] as const,
  list: (companyId?: string) => [...stockGroupKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: StockGroupFilterParams) => [...stockGroupKeys.lists(), 'paged', params ?? {}] as const,
  hierarchy: (companyId?: string) => [...stockGroupKeys.lists(), 'hierarchy', { companyId: companyId || 'all' }] as const,
  active: (companyId?: string) => [...stockGroupKeys.lists(), 'active', { companyId: companyId || 'all' }] as const,
  details: () => [...stockGroupKeys.all, 'detail'] as const,
  detail: (id: string) => [...stockGroupKeys.details(), id] as const,
  path: (id: string) => [...stockGroupKeys.all, 'path', id] as const,
};

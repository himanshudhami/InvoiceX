import type { StockItemFilterParams } from '@/services/api/types';

export const stockItemKeys = {
  all: ['stockItems'] as const,
  lists: () => [...stockItemKeys.all, 'list'] as const,
  list: (companyId?: string) => [...stockItemKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: StockItemFilterParams) => [...stockItemKeys.lists(), 'paged', params ?? {}] as const,
  active: (companyId?: string) => [...stockItemKeys.lists(), 'active', { companyId: companyId || 'all' }] as const,
  lowStock: (companyId?: string) => [...stockItemKeys.lists(), 'lowStock', { companyId: companyId || 'all' }] as const,
  byGroup: (groupId: string, companyId?: string) => [...stockItemKeys.lists(), 'byGroup', groupId, { companyId: companyId || 'all' }] as const,
  details: () => [...stockItemKeys.all, 'detail'] as const,
  detail: (id: string) => [...stockItemKeys.details(), id] as const,
  position: (id: string) => [...stockItemKeys.all, 'position', id] as const,
};

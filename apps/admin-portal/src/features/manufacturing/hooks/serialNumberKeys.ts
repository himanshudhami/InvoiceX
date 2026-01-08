import type { SerialNumberFilterParams } from '@/services/api/types';

export const serialNumberKeys = {
  all: ['serialNumbers'] as const,
  lists: () => [...serialNumberKeys.all, 'list'] as const,
  list: (companyId?: string) => [...serialNumberKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: SerialNumberFilterParams) => [...serialNumberKeys.lists(), 'paged', params ?? {}] as const,
  byStockItem: (stockItemId: string) => [...serialNumberKeys.lists(), 'byStockItem', stockItemId] as const,
  available: (stockItemId: string, warehouseId?: string) =>
    [...serialNumberKeys.lists(), 'available', stockItemId, { warehouseId: warehouseId || 'all' }] as const,
  details: () => [...serialNumberKeys.all, 'detail'] as const,
  detail: (id: string) => [...serialNumberKeys.details(), id] as const,
};

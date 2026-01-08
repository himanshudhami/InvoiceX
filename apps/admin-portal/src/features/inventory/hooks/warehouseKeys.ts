import type { WarehouseFilterParams } from '@/services/api/types';

export const warehouseKeys = {
  all: ['warehouses'] as const,
  lists: () => [...warehouseKeys.all, 'list'] as const,
  list: (companyId?: string) => [...warehouseKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: WarehouseFilterParams) => [...warehouseKeys.lists(), 'paged', params ?? {}] as const,
  active: (companyId?: string) => [...warehouseKeys.lists(), 'active', { companyId: companyId || 'all' }] as const,
  default: (companyId?: string) => [...warehouseKeys.all, 'default', { companyId: companyId || 'all' }] as const,
  details: () => [...warehouseKeys.all, 'detail'] as const,
  detail: (id: string) => [...warehouseKeys.details(), id] as const,
};

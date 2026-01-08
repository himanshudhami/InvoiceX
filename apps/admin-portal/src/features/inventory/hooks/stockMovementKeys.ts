import type { StockMovementFilterParams } from '@/services/api/types';

export const stockMovementKeys = {
  all: ['stockMovements'] as const,
  lists: () => [...stockMovementKeys.all, 'list'] as const,
  list: (companyId?: string) => [...stockMovementKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: StockMovementFilterParams) => [...stockMovementKeys.lists(), 'paged', params ?? {}] as const,
  details: () => [...stockMovementKeys.all, 'detail'] as const,
  detail: (id: string) => [...stockMovementKeys.details(), id] as const,
  ledger: (itemId: string, params?: { warehouseId?: string; fromDate?: string; toDate?: string }) =>
    [...stockMovementKeys.all, 'ledger', itemId, params ?? {}] as const,
  position: (itemId: string, warehouseId?: string) =>
    [...stockMovementKeys.all, 'position', itemId, { warehouseId: warehouseId || 'all' }] as const,
};

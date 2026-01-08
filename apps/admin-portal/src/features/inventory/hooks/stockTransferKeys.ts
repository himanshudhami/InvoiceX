import type { StockTransferFilterParams, TransferStatus } from '@/services/api/types';

export const stockTransferKeys = {
  all: ['stockTransfers'] as const,
  lists: () => [...stockTransferKeys.all, 'list'] as const,
  list: (companyId?: string) => [...stockTransferKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: StockTransferFilterParams) => [...stockTransferKeys.lists(), 'paged', params ?? {}] as const,
  pending: (companyId?: string) => [...stockTransferKeys.lists(), 'pending', { companyId: companyId || 'all' }] as const,
  byStatus: (status: TransferStatus, companyId?: string) =>
    [...stockTransferKeys.lists(), 'byStatus', status, { companyId: companyId || 'all' }] as const,
  details: () => [...stockTransferKeys.all, 'detail'] as const,
  detail: (id: string) => [...stockTransferKeys.details(), id] as const,
};

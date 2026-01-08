export const productionOrderKeys = {
  all: ['productionOrders'] as const,
  lists: () => [...productionOrderKeys.all, 'list'] as const,
  list: (companyId?: string) => [...productionOrderKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: Record<string, unknown>) => [...productionOrderKeys.lists(), 'paged', params ?? {}] as const,
  byStatus: (status: string, companyId?: string) =>
    [...productionOrderKeys.lists(), 'byStatus', status, { companyId: companyId || 'all' }] as const,
  details: () => [...productionOrderKeys.all, 'detail'] as const,
  detail: (id: string) => [...productionOrderKeys.details(), id] as const,
};

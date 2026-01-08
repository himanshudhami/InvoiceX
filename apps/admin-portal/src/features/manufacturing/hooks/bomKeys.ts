export const bomKeys = {
  all: ['boms'] as const,
  lists: () => [...bomKeys.all, 'list'] as const,
  list: (companyId?: string) => [...bomKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: Record<string, unknown>) => [...bomKeys.lists(), 'paged', params ?? {}] as const,
  active: (companyId?: string) => [...bomKeys.lists(), 'active', { companyId: companyId || 'all' }] as const,
  byFinishedGood: (finishedGoodId: string) => [...bomKeys.lists(), 'byFinishedGood', finishedGoodId] as const,
  activeForProduct: (finishedGoodId: string) => [...bomKeys.lists(), 'activeForProduct', finishedGoodId] as const,
  details: () => [...bomKeys.all, 'detail'] as const,
  detail: (id: string) => [...bomKeys.details(), id] as const,
};

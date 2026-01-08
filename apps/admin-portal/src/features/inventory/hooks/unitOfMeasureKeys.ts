import type { UnitOfMeasureFilterParams } from '@/services/api/types';

export const unitOfMeasureKeys = {
  all: ['unitsOfMeasure'] as const,
  lists: () => [...unitOfMeasureKeys.all, 'list'] as const,
  list: (companyId?: string) => [...unitOfMeasureKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: UnitOfMeasureFilterParams) => [...unitOfMeasureKeys.lists(), 'paged', params ?? {}] as const,
  system: () => [...unitOfMeasureKeys.lists(), 'system'] as const,
  details: () => [...unitOfMeasureKeys.all, 'detail'] as const,
  detail: (id: string) => [...unitOfMeasureKeys.details(), id] as const,
};

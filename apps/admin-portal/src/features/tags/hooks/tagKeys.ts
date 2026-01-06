import type { TagsFilterParams, AttributionRulesFilterParams } from '@/services/api/types';

/**
 * Query keys for tags and attribution rules
 */
export const tagKeys = {
  // Base keys
  all: ['tags'] as const,
  lists: () => [...tagKeys.all, 'list'] as const,
  list: (companyId?: string) => [...tagKeys.lists(), companyId] as const,
  paged: (params: TagsFilterParams) => [...tagKeys.lists(), 'paged', params] as const,
  details: () => [...tagKeys.all, 'detail'] as const,
  detail: (id: string) => [...tagKeys.details(), id] as const,

  // Group queries
  groups: () => [...tagKeys.all, 'group'] as const,
  byGroup: (companyId: string, tagGroup: string) => [...tagKeys.groups(), companyId, tagGroup] as const,

  // Hierarchy
  hierarchy: (companyId: string, tagGroup?: string) => [...tagKeys.all, 'hierarchy', companyId, tagGroup] as const,

  // Summaries
  summaries: (companyId: string) => [...tagKeys.all, 'summaries', companyId] as const,

  // Transaction tags
  transactionTags: () => [...tagKeys.all, 'transaction-tags'] as const,
  forTransaction: (transactionId: string, transactionType: string) =>
    [...tagKeys.transactionTags(), transactionId, transactionType] as const,
};

export const attributionRuleKeys = {
  // Base keys
  all: ['attribution-rules'] as const,
  lists: () => [...attributionRuleKeys.all, 'list'] as const,
  list: (companyId?: string) => [...attributionRuleKeys.lists(), companyId] as const,
  paged: (params: AttributionRulesFilterParams) => [...attributionRuleKeys.lists(), 'paged', params] as const,
  details: () => [...attributionRuleKeys.all, 'detail'] as const,
  detail: (id: string) => [...attributionRuleKeys.details(), id] as const,

  // Active rules
  active: (companyId: string) => [...attributionRuleKeys.all, 'active', companyId] as const,
  forType: (companyId: string, transactionType: string) =>
    [...attributionRuleKeys.all, 'for-type', companyId, transactionType] as const,

  // Performance
  performance: (companyId: string) => [...attributionRuleKeys.all, 'performance', companyId] as const,
};

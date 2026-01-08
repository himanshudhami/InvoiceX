import type {
  PartiesFilterParams,
  TdsSectionRulesFilterParams,
} from '@/services/api/types';

export const partyKeys = {
  all: ['parties'] as const,
  lists: () => [...partyKeys.all, 'list'] as const,
  list: (companyId?: string) => [...partyKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: PartiesFilterParams) => [...partyKeys.lists(), 'paged', params ?? {}] as const,
  vendors: (params?: Omit<PartiesFilterParams, 'isVendor'>) =>
    [...partyKeys.lists(), 'vendors', params ?? {}] as const,
  customers: (params?: Omit<PartiesFilterParams, 'isCustomer'>) =>
    [...partyKeys.lists(), 'customers', params ?? {}] as const,
  details: () => [...partyKeys.all, 'detail'] as const,
  detail: (id: string) => [...partyKeys.details(), id] as const,
  vendorProfile: (partyId: string) => [...partyKeys.all, 'vendor-profile', partyId] as const,
  customerProfile: (partyId: string) => [...partyKeys.all, 'customer-profile', partyId] as const,
  tags: (partyId: string) => [...partyKeys.all, 'tags', partyId] as const,
  tdsConfiguration: (partyId: string) => [...partyKeys.all, 'tds-configuration', partyId] as const,
  outstanding: (companyId: string, isVendor?: boolean) =>
    [...partyKeys.all, 'outstanding', companyId, { isVendor }] as const,
  partyOutstanding: (partyId: string) => [...partyKeys.all, 'party-outstanding', partyId] as const,
  aging: (companyId: string, isVendor?: boolean, asOfDate?: string) =>
    [...partyKeys.all, 'aging', companyId, { isVendor, asOfDate }] as const,
  tdsSummary: (companyId: string, financialYear?: string) =>
    [...partyKeys.all, 'tds-summary', companyId, financialYear] as const,
};

export const tdsSectionRuleKeys = {
  all: ['tds-section-rules'] as const,
  lists: () => [...tdsSectionRuleKeys.all, 'list'] as const,
  list: (companyId: string) => [...tdsSectionRuleKeys.lists(), companyId] as const,
  paged: (params?: TdsSectionRulesFilterParams) => [...tdsSectionRuleKeys.lists(), 'paged', params ?? {}] as const,
  details: () => [...tdsSectionRuleKeys.all, 'detail'] as const,
  detail: (id: string) => [...tdsSectionRuleKeys.details(), id] as const,
  bySection: (companyId: string, tdsSection: string) =>
    [...tdsSectionRuleKeys.all, 'by-section', companyId, tdsSection] as const,
};

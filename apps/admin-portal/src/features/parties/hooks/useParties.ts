import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { partyService, tdsSectionRuleService } from '@/services/api/parties/partyService';
import type {
  Party,
  PartyListItem,
  CreatePartyDto,
  UpdatePartyDto,
  PartiesFilterParams,
  CreatePartyVendorProfileDto,
  UpdatePartyVendorProfileDto,
  CreatePartyCustomerProfileDto,
  UpdatePartyCustomerProfileDto,
  TdsSectionRule,
  CreateTdsSectionRuleDto,
  UpdateTdsSectionRuleDto,
  TdsSectionRulesFilterParams,
} from '@/services/api/types';
import { partyKeys, tdsSectionRuleKeys } from './partyKeys';

// ==================== Party Queries ====================

/**
 * Fetch all parties (use usePartiesPaged for better performance)
 * @deprecated Use usePartiesPaged for server-side pagination
 */
export const useParties = (companyId?: string) => {
  return useQuery({
    queryKey: partyKeys.list(companyId),
    queryFn: () => partyService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated parties with server-side filtering
 */
export const usePartiesPaged = (params: PartiesFilterParams = {}) => {
  return useQuery({
    queryKey: partyKeys.paged(params),
    queryFn: () => partyService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch paginated vendors only
 */
export const useVendorsPaged = (params: Omit<PartiesFilterParams, 'isVendor'> = {}) => {
  return useQuery({
    queryKey: partyKeys.vendors(params),
    queryFn: () => partyService.getVendors(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch paginated customers only
 */
export const useCustomersPaged = (params: Omit<PartiesFilterParams, 'isCustomer'> = {}) => {
  return useQuery({
    queryKey: partyKeys.customers(params),
    queryFn: () => partyService.getCustomers(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single party by ID with profiles and tags
 */
export const useParty = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: partyKeys.detail(id),
    queryFn: () => partyService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

// ==================== Party Mutations ====================

/**
 * Create party mutation
 */
export const useCreateParty = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreatePartyDto) => partyService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: partyKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to create party:', error);
    },
  });
};

/**
 * Update party mutation
 */
export const useUpdateParty = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePartyDto }) =>
      partyService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: partyKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: partyKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to update party:', error);
    },
  });
};

/**
 * Delete party mutation
 */
export const useDeleteParty = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => partyService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: partyKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete party:', error);
    },
  });
};

// ==================== Profile Queries & Mutations ====================

/**
 * Fetch vendor profile for a party
 */
export const useVendorProfile = (partyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: partyKeys.vendorProfile(partyId),
    queryFn: () => partyService.getVendorProfile(partyId),
    enabled: enabled && !!partyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Create/Update vendor profile mutation
 */
export const useUpsertVendorProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ partyId, data, isUpdate }: {
      partyId: string;
      data: CreatePartyVendorProfileDto | UpdatePartyVendorProfileDto;
      isUpdate: boolean;
    }) => isUpdate
      ? partyService.updateVendorProfile(partyId, data)
      : partyService.createVendorProfile(partyId, data),
    onSuccess: (_, { partyId }) => {
      queryClient.invalidateQueries({ queryKey: partyKeys.vendorProfile(partyId) });
      queryClient.invalidateQueries({ queryKey: partyKeys.detail(partyId) });
    },
    onError: (error) => {
      console.error('Failed to upsert vendor profile:', error);
    },
  });
};

/**
 * Fetch customer profile for a party
 */
export const useCustomerProfile = (partyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: partyKeys.customerProfile(partyId),
    queryFn: () => partyService.getCustomerProfile(partyId),
    enabled: enabled && !!partyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Create/Update customer profile mutation
 */
export const useUpsertCustomerProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ partyId, data, isUpdate }: {
      partyId: string;
      data: CreatePartyCustomerProfileDto | UpdatePartyCustomerProfileDto;
      isUpdate: boolean;
    }) => isUpdate
      ? partyService.updateCustomerProfile(partyId, data)
      : partyService.createCustomerProfile(partyId, data),
    onSuccess: (_, { partyId }) => {
      queryClient.invalidateQueries({ queryKey: partyKeys.customerProfile(partyId) });
      queryClient.invalidateQueries({ queryKey: partyKeys.detail(partyId) });
    },
    onError: (error) => {
      console.error('Failed to upsert customer profile:', error);
    },
  });
};

// ==================== Tag Queries & Mutations ====================

/**
 * Fetch tags for a party
 */
export const usePartyTags = (partyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: partyKeys.tags(partyId),
    queryFn: () => partyService.getTags(partyId),
    enabled: enabled && !!partyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Add tag to party mutation
 */
export const useAddPartyTag = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ partyId, tagId, source }: { partyId: string; tagId: string; source?: string }) =>
      partyService.addTag(partyId, tagId, source),
    onSuccess: (_, { partyId }) => {
      queryClient.invalidateQueries({ queryKey: partyKeys.tags(partyId) });
      queryClient.invalidateQueries({ queryKey: partyKeys.detail(partyId) });
      queryClient.invalidateQueries({ queryKey: partyKeys.tdsConfiguration(partyId) });
    },
    onError: (error) => {
      console.error('Failed to add tag to party:', error);
    },
  });
};

/**
 * Remove tag from party mutation
 */
export const useRemovePartyTag = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ partyId, tagId }: { partyId: string; tagId: string }) =>
      partyService.removeTag(partyId, tagId),
    onSuccess: (_, { partyId }) => {
      queryClient.invalidateQueries({ queryKey: partyKeys.tags(partyId) });
      queryClient.invalidateQueries({ queryKey: partyKeys.detail(partyId) });
      queryClient.invalidateQueries({ queryKey: partyKeys.tdsConfiguration(partyId) });
    },
    onError: (error) => {
      console.error('Failed to remove tag from party:', error);
    },
  });
};

// ==================== TDS Configuration ====================

/**
 * Fetch auto-detected TDS configuration for a party
 */
export const useTdsConfiguration = (partyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: partyKeys.tdsConfiguration(partyId),
    queryFn: () => partyService.getTdsConfiguration(partyId),
    enabled: enabled && !!partyId,
    staleTime: 2 * 60 * 1000,
  });
};

// ==================== Outstanding & Aging ====================

/**
 * Fetch party outstanding balances for a company
 */
export const usePartyOutstanding = (companyId: string, isVendor?: boolean, enabled: boolean = true) => {
  return useQuery({
    queryKey: partyKeys.outstanding(companyId, isVendor),
    queryFn: () => partyService.getOutstanding(companyId, isVendor),
    enabled: enabled && !!companyId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch outstanding for a specific party
 */
export const usePartyOutstandingDetail = (partyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: partyKeys.partyOutstanding(partyId),
    queryFn: () => partyService.getPartyOutstanding(partyId),
    enabled: enabled && !!partyId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch party aging summary
 */
export const usePartyAging = (companyId: string, isVendor?: boolean, asOfDate?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: partyKeys.aging(companyId, isVendor, asOfDate),
    queryFn: () => partyService.getAgingSummary(companyId, isVendor, asOfDate),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch party TDS summary
 */
export const usePartyTdsSummary = (companyId: string, financialYear?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: partyKeys.tdsSummary(companyId, financialYear),
    queryFn: () => partyService.getTdsSummary(companyId, financialYear),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== TDS Section Rules ====================

/**
 * Fetch all TDS section rules for a company
 */
export const useTdsSectionRules = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: tdsSectionRuleKeys.list(companyId),
    queryFn: () => tdsSectionRuleService.getAll(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch paginated TDS section rules
 */
export const useTdsSectionRulesPaged = (params: TdsSectionRulesFilterParams = {}) => {
  return useQuery({
    queryKey: tdsSectionRuleKeys.paged(params),
    queryFn: () => tdsSectionRuleService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single TDS section rule
 */
export const useTdsSectionRule = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: tdsSectionRuleKeys.detail(id),
    queryFn: () => tdsSectionRuleService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Create TDS section rule mutation
 */
export const useCreateTdsSectionRule = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateTdsSectionRuleDto) => tdsSectionRuleService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tdsSectionRuleKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to create TDS section rule:', error);
    },
  });
};

/**
 * Update TDS section rule mutation
 */
export const useUpdateTdsSectionRule = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTdsSectionRuleDto }) =>
      tdsSectionRuleService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: tdsSectionRuleKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: tdsSectionRuleKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to update TDS section rule:', error);
    },
  });
};

/**
 * Delete TDS section rule mutation
 */
export const useDeleteTdsSectionRule = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => tdsSectionRuleService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tdsSectionRuleKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete TDS section rule:', error);
    },
  });
};

/**
 * Seed default TDS rules mutation
 */
export const useSeedTdsSectionRules = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (companyId: string) => tdsSectionRuleService.seedDefaults(companyId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tdsSectionRuleKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to seed TDS section rules:', error);
    },
  });
};

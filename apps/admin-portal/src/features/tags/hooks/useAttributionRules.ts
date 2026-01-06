import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { attributionRuleService } from '@/services/api/tags';
import type {
  AttributionRule,
  CreateAttributionRuleDto,
  UpdateAttributionRuleDto,
  AttributionRulesFilterParams,
} from '@/services/api/types';
import { attributionRuleKeys } from './tagKeys';

/**
 * Fetch all attribution rules for a company
 */
export const useAttributionRules = (companyId?: string) => {
  return useQuery({
    queryKey: attributionRuleKeys.list(companyId),
    queryFn: () => attributionRuleService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated attribution rules with filtering
 */
export const useAttributionRulesPaged = (params: AttributionRulesFilterParams = {}) => {
  return useQuery({
    queryKey: attributionRuleKeys.paged(params),
    queryFn: () => attributionRuleService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single attribution rule by ID
 */
export const useAttributionRule = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: attributionRuleKeys.detail(id),
    queryFn: () => attributionRuleService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch active rules for a company
 */
export const useActiveAttributionRules = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: attributionRuleKeys.active(companyId),
    queryFn: () => attributionRuleService.getActiveRules(companyId),
    enabled: enabled && !!companyId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch rules for a specific transaction type
 */
export const useRulesForType = (
  companyId: string,
  transactionType: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: attributionRuleKeys.forType(companyId, transactionType),
    queryFn: () => attributionRuleService.getRulesForType(companyId, transactionType),
    enabled: enabled && !!companyId && !!transactionType,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch rule performance metrics
 */
export const useRulePerformance = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: attributionRuleKeys.performance(companyId),
    queryFn: () => attributionRuleService.getPerformance(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Create attribution rule mutation
 */
export const useCreateAttributionRule = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateAttributionRuleDto) => attributionRuleService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: attributionRuleKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to create attribution rule:', error);
    },
  });
};

/**
 * Update attribution rule mutation
 */
export const useUpdateAttributionRule = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAttributionRuleDto }) =>
      attributionRuleService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: attributionRuleKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: attributionRuleKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to update attribution rule:', error);
    },
  });
};

/**
 * Delete attribution rule mutation
 */
export const useDeleteAttributionRule = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => attributionRuleService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: attributionRuleKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete attribution rule:', error);
    },
  });
};

/**
 * Test a rule against a transaction (dry run)
 */
export const useTestAttributionRule = () => {
  return useMutation({
    mutationFn: ({
      ruleId,
      transactionId,
      transactionType,
    }: {
      ruleId: string;
      transactionId: string;
      transactionType: string;
    }) => attributionRuleService.testRule(ruleId, transactionId, transactionType),
    onError: (error) => {
      console.error('Failed to test attribution rule:', error);
    },
  });
};

/**
 * Reorder rule priorities mutation
 */
export const useReorderRulePriorities = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      companyId,
      priorities,
    }: {
      companyId: string;
      priorities: Array<{ ruleId: string; newPriority: number }>;
    }) => attributionRuleService.reorderPriorities(companyId, priorities),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: attributionRuleKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to reorder rule priorities:', error);
    },
  });
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { payrollService } from '@/services/api/hr/payroll/payrollService'
import type {
  CreateCalculationRuleDto,
  UpdateCalculationRuleDto,
} from '@/features/payroll/types/payroll'
import { payrollKeys } from './payrollKeys'

/**
 * Hook to fetch calculation rules with pagination
 */
export const useCalculationRules = (params: {
  companyId?: string;
  componentType?: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
} = {}) => {
  return useQuery({
    queryKey: payrollKeys.calculationRules(params),
    queryFn: () => payrollService.getCalculationRules(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
  })
}

/**
 * Hook to fetch a single calculation rule
 */
export const useCalculationRule = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.calculationRule(id),
    queryFn: () => payrollService.getCalculationRuleById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
  })
}

/**
 * Hook to fetch active rules for a company
 */
export const useActiveCalculationRules = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.activeCalculationRules(companyId),
    queryFn: () => payrollService.getActiveRulesByCompany(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
  })
}

/**
 * Hook to fetch formula variables
 */
export const useFormulaVariables = () => {
  return useQuery({
    queryKey: payrollKeys.formulaVariables(),
    queryFn: () => payrollService.getFormulaVariables(),
    staleTime: 30 * 60 * 1000, // Variables change rarely
    gcTime: 60 * 60 * 1000,
  })
}

/**
 * Hook to fetch calculation rule templates
 */
export const useCalculationRuleTemplates = () => {
  return useQuery({
    queryKey: payrollKeys.calculationRuleTemplates(),
    queryFn: () => payrollService.getCalculationRuleTemplates(),
    staleTime: 30 * 60 * 1000,
    gcTime: 60 * 60 * 1000,
  })
}

/**
 * Hook to create a calculation rule
 */
export const useCreateCalculationRule = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateCalculationRuleDto) =>
      payrollService.createCalculationRule(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.calculationRulesBase() })
      toast.success('Calculation rule created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create calculation rule')
    },
  })
}

/**
 * Hook to update a calculation rule
 */
export const useUpdateCalculationRule = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCalculationRuleDto }) =>
      payrollService.updateCalculationRule(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.calculationRule(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.calculationRulesBase() })
      toast.success('Calculation rule updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update calculation rule')
    },
  })
}

/**
 * Hook to delete a calculation rule
 */
export const useDeleteCalculationRule = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => payrollService.deleteCalculationRule(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.calculationRulesBase() })
      queryClient.removeQueries({ queryKey: payrollKeys.calculationRule(id) })
      toast.success('Calculation rule deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete calculation rule')
    },
  })
}

/**
 * Hook to validate a formula expression
 */
export const useValidateFormula = () => {
  return useMutation({
    mutationFn: ({ expression, sampleValues }: { expression: string; sampleValues?: Record<string, number> }) =>
      payrollService.validateFormula(expression, sampleValues),
  })
}

/**
 * Hook to preview rule calculation
 */
export const usePreviewRuleCalculation = () => {
  return useMutation({
    mutationFn: (params: {
      ruleId?: string;
      rule?: CreateCalculationRuleDto;
      employeeId?: string;
      customValues?: Record<string, number>;
    }) => payrollService.previewRuleCalculation(params),
  })
}

/**
 * Hook to create a rule from a template
 */
export const useCreateRuleFromTemplate = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ templateId, companyId }: { templateId: string; companyId: string }) =>
      payrollService.createRuleFromTemplate(templateId, companyId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.calculationRulesBase() })
      toast.success('Rule created from template successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create rule from template')
    },
  })
}

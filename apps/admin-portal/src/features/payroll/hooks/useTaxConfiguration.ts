import { useQuery } from '@tanstack/react-query'
import { payrollService } from '@/services/api/payrollService'
import type { TaxSlab, ProfessionalTaxSlab } from '@/features/payroll/types/payroll'
import { payrollKeys } from './payrollKeys'

export const useTaxSlabs = (regime?: string, financialYear?: string) => {
  return useQuery({
    queryKey: payrollKeys.taxSlabs(regime, financialYear),
    queryFn: () => payrollService.getTaxSlabs(regime, financialYear),
    staleTime: 60 * 60 * 1000, // 1 hour - tax slabs don't change often
    gcTime: 24 * 60 * 60 * 1000, // 24 hours
  })
}

export const useTaxSlabForIncome = (
  income: number,
  regime: string = 'new',
  financialYear: string = '2024-25',
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: payrollKeys.taxSlabForIncome(income, regime, financialYear),
    queryFn: () => payrollService.getTaxSlabForIncome(income, regime, financialYear),
    enabled: enabled && income > 0,
    staleTime: 60 * 60 * 1000,
    gcTime: 24 * 60 * 60 * 1000,
  })
}

export const useProfessionalTaxSlabs = (state?: string) => {
  return useQuery({
    queryKey: payrollKeys.professionalTaxSlabs(state),
    queryFn: () => payrollService.getProfessionalTaxSlabs(state),
    staleTime: 60 * 60 * 1000,
    gcTime: 24 * 60 * 60 * 1000,
  })
}

export const useProfessionalTaxSlabForIncome = (
  monthlyIncome: number,
  state: string = 'Karnataka',
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: payrollKeys.professionalTaxSlabForIncome(monthlyIncome, state),
    queryFn: () => payrollService.getProfessionalTaxSlabForIncome(monthlyIncome, state),
    enabled: enabled && monthlyIncome > 0,
    staleTime: 60 * 60 * 1000,
    gcTime: 24 * 60 * 60 * 1000,
  })
}





import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { payrollService } from '@/services/api/payrollService'
import type {
  EmployeeTaxDeclaration,
  CreateEmployeeTaxDeclarationDto,
  UpdateEmployeeTaxDeclarationDto,
} from '@/features/payroll/types/payroll'
import { payrollKeys } from './payrollKeys'
import type { PaginationParams } from '@/services/api/types'

export const useTaxDeclarations = (params: PaginationParams & {
  companyId?: string;
  employeeId?: string;
  financialYear?: string;
  status?: string;
  taxRegime?: string;
} = {}) => {
  return useQuery({
    queryKey: payrollKeys.taxDeclarationList(params),
    queryFn: () => payrollService.getTaxDeclarations(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useTaxDeclaration = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.taxDeclaration(id),
    queryFn: () => payrollService.getTaxDeclarationById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useEmployeeTaxDeclaration = (
  employeeId: string,
  financialYear?: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: payrollKeys.employeeTaxDeclaration(employeeId, financialYear),
    queryFn: () => payrollService.getTaxDeclarationByEmployeeId(employeeId, financialYear),
    enabled: enabled && !!employeeId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const usePendingVerifications = (financialYear?: string) => {
  return useQuery({
    queryKey: payrollKeys.pendingVerifications(financialYear),
    queryFn: () => payrollService.getPendingVerifications(financialYear),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  })
}

export const useCreateTaxDeclaration = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateEmployeeTaxDeclarationDto) => payrollService.createTaxDeclaration(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.taxDeclarations() })
      queryClient.invalidateQueries({ queryKey: payrollKeys.employeeTaxDeclaration(variables.employeeId, variables.financialYear) })
      toast.success('Tax declaration created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create tax declaration')
    },
  })
}

export const useUpdateTaxDeclaration = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEmployeeTaxDeclarationDto }) =>
      payrollService.updateTaxDeclaration(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.taxDeclaration(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.taxDeclarations() })
      toast.success('Tax declaration updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update tax declaration')
    },
  })
}

export const useSubmitTaxDeclaration = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => payrollService.submitTaxDeclaration(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.taxDeclaration(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.taxDeclarations() })
      queryClient.invalidateQueries({ queryKey: payrollKeys.pendingVerifications() })
      toast.success('Tax declaration submitted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to submit tax declaration')
    },
  })
}

export const useVerifyTaxDeclaration = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, verifiedBy }: { id: string; verifiedBy?: string }) =>
      payrollService.verifyTaxDeclaration(id, verifiedBy),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.taxDeclaration(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.taxDeclarations() })
      queryClient.invalidateQueries({ queryKey: payrollKeys.pendingVerifications() })
      toast.success('Tax declaration verified successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to verify tax declaration')
    },
  })
}

export const useLockTaxDeclarations = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (financialYear: string) => payrollService.lockTaxDeclarations(financialYear),
    onSuccess: (_, financialYear) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.taxDeclarations() })
      toast.success(`Tax declarations for FY ${financialYear} locked successfully`)
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to lock tax declarations')
    },
  })
}

export const useDeleteTaxDeclaration = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => payrollService.deleteTaxDeclaration(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.taxDeclarations() })
      queryClient.removeQueries({ queryKey: payrollKeys.taxDeclaration(id) })
      toast.success('Tax declaration deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete tax declaration')
    },
  })
}




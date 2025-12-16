import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { payrollService } from '@/services/api/payrollService'
import type {
  EmployeeSalaryStructure,
  CreateEmployeeSalaryStructureDto,
  UpdateEmployeeSalaryStructureDto,
  SalaryBreakdown,
} from '@/features/payroll/types/payroll'
import { payrollKeys } from './payrollKeys'
import type { PaginationParams } from '@/services/api/types'

export const useSalaryStructures = (params: PaginationParams & {
  companyId?: string;
  employeeId?: string;
  isActive?: boolean;
} = {}) => {
  return useQuery({
    queryKey: payrollKeys.salaryStructureList(params),
    queryFn: () => payrollService.getSalaryStructures(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useSalaryStructure = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.salaryStructure(id),
    queryFn: () => payrollService.getSalaryStructureById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useCurrentSalaryStructure = (employeeId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.currentSalaryStructure(employeeId),
    queryFn: () => payrollService.getCurrentSalaryStructureByEmployeeId(employeeId),
    enabled: enabled && !!employeeId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useSalaryStructureHistory = (employeeId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.salaryStructureHistory(employeeId),
    queryFn: () => payrollService.getSalaryStructureHistory(employeeId),
    enabled: enabled && !!employeeId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useEffectiveSalaryStructure = (
  employeeId: string,
  asOfDate: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: [...payrollKeys.salaryStructures(), 'effective', employeeId, asOfDate],
    queryFn: () => payrollService.getEffectiveSalaryStructure(employeeId, asOfDate),
    enabled: enabled && !!employeeId && !!asOfDate,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useSalaryBreakdown = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.salaryBreakdown(id),
    queryFn: () => payrollService.getSalaryBreakdown(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useCreateSalaryStructure = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateEmployeeSalaryStructureDto) => payrollService.createSalaryStructure(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.salaryStructures() })
      queryClient.invalidateQueries({ queryKey: payrollKeys.currentSalaryStructure(variables.employeeId) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.salaryStructureHistory(variables.employeeId) })
      toast.success('Salary structure created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create salary structure')
    },
  })
}

export const useUpdateSalaryStructure = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEmployeeSalaryStructureDto }) =>
      payrollService.updateSalaryStructure(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.salaryStructure(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.salaryStructures() })
      toast.success('Salary structure updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update salary structure')
    },
  })
}

export const useDeleteSalaryStructure = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => payrollService.deleteSalaryStructure(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.salaryStructures() })
      queryClient.removeQueries({ queryKey: payrollKeys.salaryStructure(id) })
      toast.success('Salary structure deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete salary structure')
    },
  })
}




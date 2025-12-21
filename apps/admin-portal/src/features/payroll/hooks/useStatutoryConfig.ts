import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { payrollService } from '@/services/api/hr/payroll/payrollService'
import type {
  CompanyStatutoryConfig,
  CreateCompanyStatutoryConfigDto,
  UpdateCompanyStatutoryConfigDto,
} from '@/features/payroll/types/payroll'
import { payrollKeys } from './payrollKeys'
import type { PaginationParams } from '@/services/api/types'

export const useStatutoryConfigs = (params: PaginationParams & {
  companyId?: string;
  isActive?: boolean;
  pfEnabled?: boolean;
  esiEnabled?: boolean;
  ptEnabled?: boolean;
} = {}) => {
  return useQuery({
    queryKey: payrollKeys.statutoryConfigList(params),
    queryFn: () => payrollService.getStatutoryConfigs(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useStatutoryConfig = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.statutoryConfig(id),
    queryFn: () => payrollService.getStatutoryConfigById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useCompanyStatutoryConfig = (companyId: string, enabled: boolean = true) => {
  return useQuery<CompanyStatutoryConfig | null>({
    queryKey: payrollKeys.companyStatutoryConfig(companyId),
    queryFn: () => payrollService.getStatutoryConfigByCompanyId(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    retry: false, // Don't retry - service handles 404s gracefully
  })
}

export const useActiveStatutoryConfigs = () => {
  return useQuery({
    queryKey: payrollKeys.activeStatutoryConfigs(),
    queryFn: () => payrollService.getActiveStatutoryConfigs(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useCreateStatutoryConfig = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateCompanyStatutoryConfigDto) => payrollService.createStatutoryConfig(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.statutoryConfigs() })
      queryClient.invalidateQueries({ queryKey: payrollKeys.companyStatutoryConfig(variables.companyId) })
      toast.success('Statutory config created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create statutory config')
    },
  })
}

export const useUpdateStatutoryConfig = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCompanyStatutoryConfigDto }) =>
      payrollService.updateStatutoryConfig(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.statutoryConfig(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.statutoryConfigs() })
      toast.success('Statutory config updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update statutory config')
    },
  })
}

export const useDeleteStatutoryConfig = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => payrollService.deleteStatutoryConfig(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.statutoryConfigs() })
      queryClient.removeQueries({ queryKey: payrollKeys.statutoryConfig(id) })
      toast.success('Statutory config deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete statutory config')
    },
  })
}





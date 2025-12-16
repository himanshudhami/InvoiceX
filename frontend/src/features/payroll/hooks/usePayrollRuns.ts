import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { payrollService } from '@/services/api/payrollService'
import type {
  PayrollRun,
  CreatePayrollRunDto,
  UpdatePayrollRunDto,
  ProcessPayrollDto,
  PayrollRunSummary,
  PayrollPreview,
} from '@/features/payroll/types/payroll'
import { payrollKeys } from './payrollKeys'
import type { PaginationParams } from '@/services/api/types'

export const usePayrollRuns = (params: PaginationParams & {
  companyId?: string;
  payrollMonth?: number;
  payrollYear?: number;
  status?: string;
} = {}) => {
  return useQuery({
    queryKey: payrollKeys.runList(params),
    queryFn: () => payrollService.getPayrollRuns(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const usePayrollRun = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.run(id),
    queryFn: () => payrollService.getPayrollRunById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const usePayrollRunSummary = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.runSummary(id),
    queryFn: () => payrollService.getPayrollRunSummary(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useCreatePayrollRun = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreatePayrollRunDto) => payrollService.createPayrollRun(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.runs() })
      toast.success('Payroll run created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create payroll run')
    },
  })
}

export const useProcessPayroll = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: ProcessPayrollDto) => payrollService.processPayroll(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.runs() })
      queryClient.invalidateQueries({ queryKey: payrollKeys.transactions() })
      toast.success('Payroll processed successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to process payroll')
    },
  })
}

export const useApprovePayrollRun = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, approvedBy }: { id: string; approvedBy?: string }) =>
      payrollService.approvePayrollRun(id, approvedBy),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.run(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.runs() })
      toast.success('Payroll run approved successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to approve payroll run')
    },
  })
}

export const useMarkPayrollAsPaid = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePayrollRunDto }) =>
      payrollService.markPayrollAsPaid(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.run(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.runs() })
      toast.success('Payroll marked as paid successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to mark payroll as paid')
    },
  })
}

export const usePayrollPreview = (params: {
  companyId: string;
  payrollMonth: number;
  payrollYear: number;
  includeContractors?: boolean;
}, enabled: boolean = true) => {
  return useQuery({
    queryKey: [...payrollKeys.all, 'preview', params],
    queryFn: () => payrollService.getPayrollPreview(params),
    enabled: enabled && !!params.companyId,
    staleTime: 30 * 1000, // 30 seconds - preview data can change
    gcTime: 60 * 1000,
  })
}




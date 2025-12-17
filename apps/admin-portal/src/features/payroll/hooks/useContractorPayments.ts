import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { payrollService } from '@/services/api/payrollService'
import type {
  ContractorPayment,
  CreateContractorPaymentDto,
  UpdateContractorPaymentDto,
  ContractorPaymentSummary,
} from '@/features/payroll/types/payroll'
import { payrollKeys } from './payrollKeys'
import type { PaginationParams } from '@/services/api/types'

export const useContractorPayments = (params: PaginationParams & {
  companyId?: string;
  employeeId?: string;
  paymentMonth?: number;
  paymentYear?: number;
  status?: string;
} = {}) => {
  return useQuery({
    queryKey: payrollKeys.contractorPaymentList(params),
    queryFn: () => payrollService.getContractorPayments(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useContractorPayment = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.contractorPayment(id),
    queryFn: () => payrollService.getContractorPaymentById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useContractorPaymentSummary = (
  paymentMonth: number,
  paymentYear: number,
  companyId?: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: payrollKeys.contractorSummary(paymentMonth, paymentYear, companyId),
    queryFn: () => payrollService.getContractorPaymentSummary(paymentMonth, paymentYear, companyId),
    enabled: enabled,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useContractorYtdSummary = (
  employeeId: string,
  financialYear: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: payrollKeys.contractorYtd(employeeId, financialYear),
    queryFn: () => payrollService.getContractorYtdSummary(employeeId, financialYear),
    enabled: enabled && !!employeeId && !!financialYear,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useCreateContractorPayment = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateContractorPaymentDto) => payrollService.createContractorPayment(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.contractorPayments() })
      toast.success('Contractor payment created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create contractor payment')
    },
  })
}

export const useUpdateContractorPayment = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateContractorPaymentDto }) =>
      payrollService.updateContractorPayment(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.contractorPayment(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.contractorPayments() })
      toast.success('Contractor payment updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update contractor payment')
    },
  })
}

export const useDeleteContractorPayment = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => payrollService.deleteContractorPayment(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.contractorPayments() })
      queryClient.removeQueries({ queryKey: payrollKeys.contractorPayment(id) })
      toast.success('Contractor payment deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete contractor payment')
    },
  })
}

export const useApproveContractorPayment = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => payrollService.approveContractorPayment(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.contractorPayment(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.contractorPayments() })
      toast.success('Contractor payment approved successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to approve contractor payment')
    },
  })
}

export const useMarkContractorPaymentAsPaid = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateContractorPaymentDto }) =>
      payrollService.markContractorPaymentAsPaid(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.contractorPayment(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.contractorPayments() })
      toast.success('Contractor payment marked as paid successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to mark contractor payment as paid')
    },
  })
}





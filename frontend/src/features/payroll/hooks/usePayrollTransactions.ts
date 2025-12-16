import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { payrollService } from '@/services/api/payrollService'
import type {
  PayrollTransaction,
  TdsOverrideDto,
} from '@/features/payroll/types/payroll'
import { payrollKeys } from './payrollKeys'
import type { PaginationParams } from '@/services/api/types'

export const usePayrollTransactions = (params: PaginationParams & {
  payrollRunId?: string;
  companyId?: string;
  employeeId?: string;
  payrollMonth?: number;
  payrollYear?: number;
  status?: string;
} = {}) => {
  return useQuery({
    queryKey: payrollKeys.transactionList(params),
    queryFn: () => payrollService.getPayrollTransactions(params),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const usePayrollTransaction = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.transaction(id),
    queryFn: () => payrollService.getPayrollTransactionById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

export const useOverrideTds = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ transactionId, data }: { transactionId: string; data: TdsOverrideDto }) =>
      payrollService.overrideTds(transactionId, data),
    onSuccess: (_, { transactionId }) => {
      queryClient.invalidateQueries({ queryKey: payrollKeys.transaction(transactionId) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.transactions() })
      toast.success('TDS override applied successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to override TDS')
    },
  })
}





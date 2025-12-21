import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

import { customerService } from '@/services/api/crm/customerService'
import type {
  Customer,
  CreateCustomerDto,
  UpdateCustomerDto,
  CustomersFilterParams,
} from '@/services/api/types'
import { customerKeys } from './customerKeys'

/**
 * Fetch all customers (use useCustomersPaged for better performance)
 * @param companyId Optional company ID to filter by (for multi-company users)
 * @deprecated Use useCustomersPaged for server-side pagination
 */
export const useCustomers = (companyId?: string) => {
  return useQuery({
    queryKey: customerKeys.list(companyId),
    queryFn: () => customerService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Fetch paginated customers with server-side filtering
 * This is the recommended hook for listing customers
 */
export const useCustomersPaged = (params: CustomersFilterParams = {}) => {
  return useQuery({
    queryKey: customerKeys.paged(params),
    queryFn: () => customerService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000, // 30 seconds
  })
}

/**
 * Fetch single customer by ID
 */
export const useCustomer = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: customerKeys.detail(id),
    queryFn: () => customerService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Create customer mutation
 */
export const useCreateCustomer = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateCustomerDto) => customerService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() })
    },
    onError: (error) => {
      console.error('Failed to create customer:', error)
    },
  })
}

/**
 * Update customer mutation
 */
export const useUpdateCustomer = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCustomerDto }) =>
      customerService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() })
    },
    onError: (error) => {
      console.error('Failed to update customer:', error)
    },
  })
}

/**
 * Delete customer mutation
 */
export const useDeleteCustomer = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => customerService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() })
    },
    onError: (error) => {
      console.error('Failed to delete customer:', error)
    },
  })
}

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

import { customerService } from '@/services/api/customerService'
import type {
  Customer,
  CreateCustomerDto,
  UpdateCustomerDto,
  PaginationParams,
} from '@/services/api/types'
import { customerKeys } from './customerKeys'
import { usePaginatedData } from '@/shared/utils/pagination'

/**
 * Fetch all customers
 */
export const useCustomers = () => {
  return useQuery({
    queryKey: customerKeys.lists(),
    queryFn: () => customerService.getAll(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Paginated customers with client-side pagination
 */
export const useCustomersPaged = (params: PaginationParams = {}) => {
  const { data: customers = [], ...rest } = useCustomers()

  const paginated = usePaginatedData<Customer>(
    customers,
    params,
    ['name', 'companyName', 'email', 'phone']
  )

  return {
    ...rest,
    data: {
      items: paginated.items,
      totalCount: paginated.totalCount,
      pageNumber: paginated.pageNumber,
      pageSize: paginated.pageSize,
      totalPages: paginated.pageCount,
      hasPrevious: paginated.hasPrevious,
      hasNext: paginated.hasNext,
    },
  }
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

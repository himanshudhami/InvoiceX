import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

import { productService } from '@/services/api/productService'
import type {
  Product,
  CreateProductDto,
  UpdateProductDto,
  PaginationParams,
} from '@/services/api/types'
import { productKeys } from './productKeys'
import { usePaginatedData } from '@/shared/utils/pagination'

/**
 * Fetch all products
 */
export const useProducts = () => {
  return useQuery({
    queryKey: productKeys.lists(),
    queryFn: () => productService.getAll(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Paginated products with client-side pagination
 */
export const useProductsPaged = (params: PaginationParams = {}) => {
  const { data: products = [], ...rest } = useProducts()

  const paginated = usePaginatedData<Product>(
    products,
    params,
    ['name', 'description', 'sku']
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
 * Fetch single product by ID
 */
export const useProduct = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: productKeys.detail(id),
    queryFn: () => productService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Create product mutation
 */
export const useCreateProduct = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateProductDto) => productService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.lists() })
    },
    onError: (error) => {
      console.error('Failed to create product:', error)
    },
  })
}

/**
 * Update product mutation
 */
export const useUpdateProduct = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProductDto }) =>
      productService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: productKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: productKeys.lists() })
    },
    onError: (error) => {
      console.error('Failed to update product:', error)
    },
  })
}

/**
 * Delete product mutation
 */
export const useDeleteProduct = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => productService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.lists() })
    },
    onError: (error) => {
      console.error('Failed to delete product:', error)
    },
  })
}

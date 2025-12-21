import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

import { productService } from '@/services/api/catalog/productService'
import type {
  Product,
  CreateProductDto,
  UpdateProductDto,
  ProductsFilterParams,
} from '@/services/api/types'
import { productKeys } from './productKeys'

/**
 * Fetch all products (use useProductsPaged for better performance)
 * @param companyId Optional company ID to filter by (for multi-company users)
 * @deprecated Use useProductsPaged for server-side pagination
 */
export const useProducts = (companyId?: string) => {
  return useQuery({
    queryKey: productKeys.list(companyId),
    queryFn: () => productService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Fetch paginated products with server-side filtering
 * This is the recommended hook for listing products
 */
export const useProductsPaged = (params: ProductsFilterParams = {}) => {
  return useQuery({
    queryKey: productKeys.paged(params),
    queryFn: () => productService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000, // 30 seconds
  })
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

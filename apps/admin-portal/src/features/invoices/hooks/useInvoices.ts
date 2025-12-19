import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'

import { invoiceService } from '@/services/api/invoiceService'
import type {
  Invoice,
  CreateInvoiceDto,
  UpdateInvoiceDto,
  InvoicesFilterParams,
} from '@/services/api/types'
import { invoiceKeys } from './invoiceKeys'

/**
 * Fetch all invoices (use useInvoicesPaged for better performance)
 * @param companyId Optional company ID to filter by (for multi-company users)
 * @deprecated Use useInvoicesPaged for server-side pagination
 */
export const useInvoices = (companyId?: string) => {
  return useQuery({
    queryKey: invoiceKeys.list(companyId),
    queryFn: () => invoiceService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Fetch paginated invoices with server-side filtering
 * This is the recommended hook for listing invoices
 */
export const useInvoicesPaged = (params: InvoicesFilterParams = {}) => {
  return useQuery({
    queryKey: invoiceKeys.paged(params),
    queryFn: () => invoiceService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000, // 30 seconds
  })
}

/**
 * Fetch single invoice by ID
 */
export const useInvoice = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: invoiceKeys.detail(id),
    queryFn: () => invoiceService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Create invoice mutation
 */
export const useCreateInvoice = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateInvoiceDto) => invoiceService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() })
      toast.success('Invoice created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create invoice')
    },
  })
}

/**
 * Update invoice mutation
 */
export const useUpdateInvoice = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateInvoiceDto }) =>
      invoiceService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: invoiceKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() })
      toast.success('Invoice updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update invoice')
    },
  })
}

/**
 * Delete invoice mutation
 */
export const useDeleteInvoice = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => invoiceService.delete(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() })
      queryClient.removeQueries({ queryKey: invoiceKeys.detail(id) })
      toast.success('Invoice deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete invoice')
    },
  })
}

/**
 * Duplicate invoice mutation
 */
export const useDuplicateInvoice = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => invoiceService.duplicate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() })
      toast.success('Invoice duplicated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to duplicate invoice')
    },
  })
}

/**
 * Get invoice items for a specific invoice
 * Items are embedded in the invoice, so we just extract them.
 */
export const useInvoiceItems = (invoiceId: string) => {
  const { data: invoice, isLoading, error } = useInvoice(invoiceId)

  return {
    data: invoice?.items ?? [],
    isLoading,
    error,
  }
}

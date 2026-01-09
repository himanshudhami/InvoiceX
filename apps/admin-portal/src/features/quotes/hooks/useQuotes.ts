import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'

import { quoteService } from '@/services/api/billing/quoteService'
import type {
  Quote,
  CreateQuoteDto,
  UpdateQuoteDto,
  QuotesFilterParams,
} from '@/services/api/types'
import { quoteKeys } from './quoteKeys'

/**
 * Fetch all quotes (use useQuotesPaged for better performance)
 * @param companyId Optional company ID to filter by (for multi-company users)
 * @deprecated Use useQuotesPaged for server-side pagination
 */
export const useQuotes = (companyId?: string) => {
  return useQuery({
    queryKey: quoteKeys.list(companyId),
    queryFn: () => quoteService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Fetch paginated quotes with server-side filtering
 * This is the recommended hook for listing quotes
 */
export const useQuotesPaged = (params: QuotesFilterParams = {}) => {
  return useQuery({
    queryKey: quoteKeys.paged(params),
    queryFn: () => quoteService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000, // 30 seconds
  })
}

/**
 * Fetch single quote by ID
 */
export const useQuote = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: quoteKeys.detail(id),
    queryFn: () => quoteService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Create quote mutation
 */
export const useCreateQuote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateQuoteDto) => quoteService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create quote')
    },
  })
}

/**
 * Update quote mutation
 */
export const useUpdateQuote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateQuoteDto }) =>
      quoteService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update quote')
    },
  })
}

/**
 * Delete quote mutation
 */
export const useDeleteQuote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => quoteService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete quote')
    },
  })
}

/**
 * Duplicate quote mutation
 */
export const useDuplicateQuote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => quoteService.duplicate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote duplicated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to duplicate quote')
    },
  })
}

/**
 * Send quote mutation
 */
export const useSendQuote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => quoteService.send(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote sent successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to send quote')
    },
  })
}

/**
 * Accept quote mutation
 */
export const useAcceptQuote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => quoteService.accept(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote accepted')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to accept quote')
    },
  })
}

/**
 * Reject quote mutation
 */
export const useRejectQuote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id }: { id: string }) => quoteService.reject(id),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote rejected')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to reject quote')
    },
  })
}

/**
 * Convert quote to invoice mutation
 */
export const useConvertQuoteToInvoice = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => quoteService.convertToInvoice(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
      toast.success('Quote converted to invoice')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to convert quote to invoice')
    },
  })
}

/**
 * Get quote items for a specific quote
 * Items are embedded in the quote, so we just extract them.
 */
export const useQuoteItems = (quoteId: string) => {
  const { data: quote, isLoading, error } = useQuote(quoteId)

  return {
    data: quote?.items ?? [],
    isLoading,
    error,
  }
}

/**
 * Create quote item mutation
 */
export const useCreateQuoteItem = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: Omit<import('@/services/api/types').QuoteItem, 'id' | 'createdAt' | 'updatedAt'>) => {
      const newItem = await quoteService.createQuoteItem(data)
      return newItem
    },
    onSuccess: (_, variables) => {
      if (variables.quoteId) {
        queryClient.invalidateQueries({ queryKey: quoteKeys.detail(variables.quoteId) })
      }
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote item added successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to add quote item')
    },
  })
}

/**
 * Update quote item mutation
 */
export const useUpdateQuoteItem = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, data }: { id: string; data: Partial<import('@/services/api/types').QuoteItem> }) => {
      await quoteService.updateQuoteItem(id, data)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote item updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update quote item')
    },
  })
}

/**
 * Delete quote item mutation
 */
export const useDeleteQuoteItem = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      await quoteService.deleteQuoteItem(id)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: quoteKeys.lists() })
      toast.success('Quote item deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete quote item')
    },
  })
}

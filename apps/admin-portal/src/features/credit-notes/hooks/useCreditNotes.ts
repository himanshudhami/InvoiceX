import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'

import { creditNoteService } from '@/services/api/billing/creditNoteService'
import type {
  CreditNote,
  CreateCreditNoteDto,
  UpdateCreditNoteDto,
  CreditNotesFilterParams,
  CreateCreditNoteFromInvoice,
} from '@/services/api/types'
import { creditNoteKeys } from './creditNoteKeys'
import { invoiceKeys } from '@/features/invoices/hooks/invoiceKeys'

/**
 * Fetch all credit notes
 * @deprecated Use useCreditNotesPaged for server-side pagination
 */
export const useCreditNotes = (companyId?: string) => {
  return useQuery({
    queryKey: creditNoteKeys.list(companyId),
    queryFn: () => creditNoteService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Fetch paginated credit notes with server-side filtering
 */
export const useCreditNotesPaged = (params: CreditNotesFilterParams = {}) => {
  return useQuery({
    queryKey: creditNoteKeys.paged(params),
    queryFn: () => creditNoteService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  })
}

/**
 * Fetch single credit note by ID
 */
export const useCreditNote = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: creditNoteKeys.detail(id),
    queryFn: () => creditNoteService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Fetch credit notes for a specific invoice
 */
export const useCreditNotesByInvoice = (invoiceId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: creditNoteKeys.byInvoice(invoiceId),
    queryFn: () => creditNoteService.getByInvoiceId(invoiceId),
    enabled: enabled && !!invoiceId,
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Get next credit note number for a company
 */
export const useNextCreditNoteNumber = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: creditNoteKeys.nextNumber(companyId),
    queryFn: () => creditNoteService.generateNextNumber(companyId),
    enabled: enabled && !!companyId,
    staleTime: 0, // Always fetch fresh
  })
}

/**
 * Create credit note mutation
 */
export const useCreateCreditNote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateCreditNoteDto) => creditNoteService.create(data),
    onSuccess: (creditNote) => {
      queryClient.invalidateQueries({ queryKey: creditNoteKeys.lists() })
      // Also invalidate the related invoice since it now has credit notes
      if (creditNote.originalInvoiceId) {
        queryClient.invalidateQueries({ queryKey: invoiceKeys.detail(creditNote.originalInvoiceId) })
      }
      toast.success('Credit note created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create credit note')
    },
  })
}

/**
 * Create credit note from invoice - the primary workflow
 */
export const useCreateCreditNoteFromInvoice = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateCreditNoteFromInvoice) => creditNoteService.createFromInvoice(data),
    onSuccess: (creditNote) => {
      queryClient.invalidateQueries({ queryKey: creditNoteKeys.lists() })
      // Invalidate both the invoice detail and the by-invoice list
      if (creditNote.originalInvoiceId) {
        queryClient.invalidateQueries({ queryKey: invoiceKeys.detail(creditNote.originalInvoiceId) })
        queryClient.invalidateQueries({ queryKey: creditNoteKeys.byInvoice(creditNote.originalInvoiceId) })
      }
      toast.success('Credit note created from invoice successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create credit note from invoice')
    },
  })
}

/**
 * Update credit note mutation
 */
export const useUpdateCreditNote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCreditNoteDto }) =>
      creditNoteService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: creditNoteKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: creditNoteKeys.lists() })
      toast.success('Credit note updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update credit note')
    },
  })
}

/**
 * Delete credit note mutation
 */
export const useDeleteCreditNote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => creditNoteService.delete(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: creditNoteKeys.lists() })
      queryClient.removeQueries({ queryKey: creditNoteKeys.detail(id) })
      toast.success('Credit note deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete credit note')
    },
  })
}

/**
 * Issue credit note mutation (change status from draft to issued)
 */
export const useIssueCreditNote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => creditNoteService.issue(id),
    onSuccess: (creditNote, id) => {
      queryClient.invalidateQueries({ queryKey: creditNoteKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: creditNoteKeys.lists() })
      // Update invoice totals
      if (creditNote.originalInvoiceId) {
        queryClient.invalidateQueries({ queryKey: invoiceKeys.detail(creditNote.originalInvoiceId) })
      }
      toast.success('Credit note issued successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to issue credit note')
    },
  })
}

/**
 * Cancel credit note mutation
 */
export const useCancelCreditNote = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      creditNoteService.cancel(id, reason),
    onSuccess: (creditNote, { id }) => {
      queryClient.invalidateQueries({ queryKey: creditNoteKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: creditNoteKeys.lists() })
      // Update invoice totals
      if (creditNote.originalInvoiceId) {
        queryClient.invalidateQueries({ queryKey: invoiceKeys.detail(creditNote.originalInvoiceId) })
      }
      toast.success('Credit note cancelled successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to cancel credit note')
    },
  })
}

/**
 * Get credit note items for a specific credit note
 */
export const useCreditNoteItems = (creditNoteId: string) => {
  const { data: creditNote, isLoading, error } = useCreditNote(creditNoteId)

  return {
    data: creditNote?.items ?? [],
    isLoading,
    error,
  }
}

import type { CreditNotesFilterParams } from '@/services/api/types'

export const creditNoteKeys = {
  all: ['credit-notes'] as const,
  lists: () => [...creditNoteKeys.all, 'list'] as const,
  list: (companyId?: string) => [...creditNoteKeys.lists(), { companyId: companyId || 'all' }] as const,
  paged: (params?: CreditNotesFilterParams) => [...creditNoteKeys.lists(), 'paged', params ?? {}] as const,
  details: () => [...creditNoteKeys.all, 'detail'] as const,
  detail: (id: string) => [...creditNoteKeys.details(), id] as const,
  byInvoice: (invoiceId: string) => [...creditNoteKeys.all, 'by-invoice', invoiceId] as const,
  nextNumber: (companyId: string) => [...creditNoteKeys.all, 'next-number', companyId] as const,
}

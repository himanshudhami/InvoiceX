import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  paymentAllocationService,
  CreatePaymentAllocationDto,
  UpdatePaymentAllocationDto,
  BulkAllocationDto,
  PaymentAllocationFilterParams,
} from '@/services/api/paymentAllocationService';

const QUERY_KEYS = {
  all: ['payment-allocations'] as const,
  lists: () => [...QUERY_KEYS.all, 'list'] as const,
  list: (params?: PaymentAllocationFilterParams) => [...QUERY_KEYS.lists(), params] as const,
  details: () => [...QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...QUERY_KEYS.details(), id] as const,
  byPayment: (paymentId: string) => [...QUERY_KEYS.all, 'by-payment', paymentId] as const,
  byInvoice: (invoiceId: string) => [...QUERY_KEYS.all, 'by-invoice', invoiceId] as const,
  byCompany: (companyId: string) => [...QUERY_KEYS.all, 'by-company', companyId] as const,
  summary: (paymentId: string) => [...QUERY_KEYS.all, 'summary', paymentId] as const,
  unallocated: (paymentId: string) => [...QUERY_KEYS.all, 'unallocated', paymentId] as const,
  invoiceStatus: (invoiceId: string) => [...QUERY_KEYS.all, 'invoice-status', invoiceId] as const,
  companyInvoiceStatus: (companyId: string, fy?: string) => [...QUERY_KEYS.all, 'company-invoice-status', companyId, fy] as const,
};

export function usePaymentAllocations(params?: PaymentAllocationFilterParams) {
  return useQuery({
    queryKey: QUERY_KEYS.list(params),
    queryFn: () => paymentAllocationService.getPaged(params),
    staleTime: 1000 * 30,
  });
}

export function usePaymentAllocation(id: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.detail(id!),
    queryFn: () => paymentAllocationService.getById(id!),
    enabled: !!id,
    staleTime: 1000 * 30,
  });
}

export function useAllocationsByPayment(paymentId: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.byPayment(paymentId!),
    queryFn: () => paymentAllocationService.getByPaymentId(paymentId!),
    enabled: !!paymentId,
    staleTime: 1000 * 30,
  });
}

export function useAllocationsByInvoice(invoiceId: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.byInvoice(invoiceId!),
    queryFn: () => paymentAllocationService.getByInvoiceId(invoiceId!),
    enabled: !!invoiceId,
    staleTime: 1000 * 30,
  });
}

export function useAllocationsByCompany(companyId: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.byCompany(companyId!),
    queryFn: () => paymentAllocationService.getByCompanyId(companyId!),
    enabled: !!companyId,
    staleTime: 1000 * 30,
  });
}

export function usePaymentAllocationSummary(paymentId: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.summary(paymentId!),
    queryFn: () => paymentAllocationService.getPaymentAllocationSummary(paymentId!),
    enabled: !!paymentId,
    staleTime: 1000 * 30,
  });
}

export function useUnallocatedAmount(paymentId: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.unallocated(paymentId!),
    queryFn: () => paymentAllocationService.getUnallocatedAmount(paymentId!),
    enabled: !!paymentId,
    staleTime: 1000 * 30,
  });
}

export function useInvoicePaymentStatus(invoiceId: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.invoiceStatus(invoiceId!),
    queryFn: () => paymentAllocationService.getInvoicePaymentStatus(invoiceId!),
    enabled: !!invoiceId,
    staleTime: 1000 * 30,
  });
}

export function useCompanyInvoicePaymentStatus(companyId: string | undefined, financialYear?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.companyInvoiceStatus(companyId!, financialYear),
    queryFn: () => paymentAllocationService.getCompanyInvoicePaymentStatus(companyId!, financialYear),
    enabled: !!companyId,
    staleTime: 1000 * 60, // 1 minute for summary data
  });
}

export function useCreatePaymentAllocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreatePaymentAllocationDto) => paymentAllocationService.create(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      if (variables.paymentId) {
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.byPayment(variables.paymentId) });
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.summary(variables.paymentId) });
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.unallocated(variables.paymentId) });
      }
      if (variables.invoiceId) {
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.byInvoice(variables.invoiceId) });
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.invoiceStatus(variables.invoiceId) });
      }
    },
  });
}

export function useUpdatePaymentAllocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePaymentAllocationDto }) =>
      paymentAllocationService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.invalidateQueries({ queryKey: ['payments'] });
    },
  });
}

export function useDeletePaymentAllocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => paymentAllocationService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.invalidateQueries({ queryKey: ['payments'] });
    },
  });
}

export function useBulkAllocatePayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BulkAllocationDto) => paymentAllocationService.allocatePaymentBulk(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.byPayment(variables.paymentId) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.summary(variables.paymentId) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.unallocated(variables.paymentId) });
      // Invalidate invoice statuses for all affected invoices
      variables.allocations.forEach((alloc) => {
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.byInvoice(alloc.invoiceId) });
        queryClient.invalidateQueries({ queryKey: QUERY_KEYS.invoiceStatus(alloc.invoiceId) });
      });
    },
  });
}

export function useRemoveAllAllocations() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (paymentId: string) => paymentAllocationService.removeAllAllocations(paymentId),
    onSuccess: (_, paymentId) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.byPayment(paymentId) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.summary(paymentId) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.unallocated(paymentId) });
    },
  });
}

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { paymentService, CreatePaymentDto, UpdatePaymentDto, PaymentFilterParams } from '@/services/api/finance/payments/paymentService';

const QUERY_KEYS = {
  all: ['payments'] as const,
  lists: () => [...QUERY_KEYS.all, 'list'] as const,
  list: (params?: PaymentFilterParams) => [...QUERY_KEYS.lists(), params] as const,
  details: () => [...QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...QUERY_KEYS.details(), id] as const,
  byInvoice: (invoiceId: string) => [...QUERY_KEYS.all, 'by-invoice', invoiceId] as const,
  byCompany: (companyId: string) => [...QUERY_KEYS.all, 'by-company', companyId] as const,
  byCustomer: (customerId: string) => [...QUERY_KEYS.all, 'by-customer', customerId] as const,
  byFinancialYear: (fy: string, companyId?: string) => [...QUERY_KEYS.all, 'by-fy', fy, companyId] as const,
  incomeSummary: (params?: { companyId?: string; financialYear?: string; year?: number; month?: number }) =>
    [...QUERY_KEYS.all, 'income-summary', params] as const,
  tdsSummary: (financialYear: string, companyId?: string) =>
    [...QUERY_KEYS.all, 'tds-summary', financialYear, companyId] as const,
};

export function usePayments(params?: PaymentFilterParams) {
  return useQuery({
    queryKey: QUERY_KEYS.list(params),
    queryFn: () => paymentService.getPaged(params),
    staleTime: 1000 * 30,
  });
}

export function usePayment(id: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.detail(id!),
    queryFn: () => paymentService.getById(id!),
    enabled: !!id,
    staleTime: 1000 * 30,
  });
}

export function useAllPayments() {
  return useQuery({
    queryKey: [...QUERY_KEYS.all, 'all'],
    queryFn: () => paymentService.getAll(),
    staleTime: 1000 * 30,
  });
}

export function usePaymentsByInvoice(invoiceId: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.byInvoice(invoiceId!),
    queryFn: () => paymentService.getByInvoiceId(invoiceId!),
    enabled: !!invoiceId,
    staleTime: 1000 * 30,
  });
}

export function usePaymentsByCompany(companyId: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.byCompany(companyId!),
    queryFn: () => paymentService.getByCompanyId(companyId!),
    enabled: !!companyId,
    staleTime: 1000 * 30,
  });
}

export function usePaymentsByCustomer(customerId: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.byCustomer(customerId!),
    queryFn: () => paymentService.getByCustomerId(customerId!),
    enabled: !!customerId,
    staleTime: 1000 * 30,
  });
}

export function usePaymentsByFinancialYear(financialYear: string | undefined, companyId?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.byFinancialYear(financialYear!, companyId),
    queryFn: () => paymentService.getByFinancialYear(financialYear!, companyId),
    enabled: !!financialYear,
    staleTime: 1000 * 30,
  });
}

export function useIncomeSummary(params?: {
  companyId?: string;
  financialYear?: string;
  year?: number;
  month?: number;
}) {
  return useQuery({
    queryKey: QUERY_KEYS.incomeSummary(params),
    queryFn: () => paymentService.getIncomeSummary(params),
    staleTime: 1000 * 60, // 1 minute for summary data
  });
}

export function useTdsSummary(financialYear: string | undefined, companyId?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.tdsSummary(financialYear!, companyId),
    queryFn: () => paymentService.getTdsSummary(financialYear!, companyId),
    enabled: !!financialYear,
    staleTime: 1000 * 60, // 1 minute for summary data
  });
}

export function useCreatePayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreatePaymentDto) => paymentService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
    },
  });
}

export function useUpdatePayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePaymentDto }) =>
      paymentService.update(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(variables.id) });
    },
  });
}

export function useDeletePayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => paymentService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
    },
  });
}





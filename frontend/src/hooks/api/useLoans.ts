import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { loanService } from '@/services/api/loanService';
import {
  Loan,
  LoanScheduleDto,
  CreateLoanDto,
  UpdateLoanDto,
  CreateEmiPaymentDto,
  PrepaymentDto,
  LoanTransaction,
  PaginationParams,
} from '@/services/api/types';

const QUERY_KEYS = {
  all: ['loans'] as const,
  lists: () => [...QUERY_KEYS.all, 'list'] as const,
  list: (params: PaginationParams & { companyId?: string; status?: string; loanType?: string; assetId?: string }) =>
    [...QUERY_KEYS.lists(), params] as const,
  details: () => [...QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...QUERY_KEYS.details(), id] as const,
  schedule: (id: string) => [...QUERY_KEYS.detail(id), 'schedule'] as const,
  outstanding: (companyId?: string) => [...QUERY_KEYS.all, 'outstanding', companyId] as const,
  interest: (id: string, fromDate?: string, toDate?: string) =>
    [...QUERY_KEYS.detail(id), 'interest', fromDate, toDate] as const,
  interestPayments: (companyId?: string, fromDate?: string, toDate?: string) =>
    [...QUERY_KEYS.all, 'interest-payments', companyId, fromDate, toDate] as const,
};

export function useLoans(params: PaginationParams & { companyId?: string; status?: string; loanType?: string; assetId?: string } = {}) {
  return useQuery({
    queryKey: QUERY_KEYS.list(params),
    queryFn: () => loanService.getPaged(params),
    staleTime: 1000 * 30, // 30 seconds
  });
}

export function useLoan(id: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.detail(id!),
    queryFn: () => loanService.getById(id!),
    enabled: !!id,
    staleTime: 1000 * 30,
  });
}

export function useLoanSchedule(id: string | undefined) {
  return useQuery({
    queryKey: QUERY_KEYS.schedule(id!),
    queryFn: () => loanService.getSchedule(id!),
    enabled: !!id,
    staleTime: 1000 * 30,
  });
}

export function useOutstandingLoans(companyId?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.outstanding(companyId),
    queryFn: () => loanService.getOutstanding(companyId),
    staleTime: 1000 * 30,
  });
}

export function useTotalInterestPaid(id: string | undefined, fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.interest(id!, fromDate, toDate),
    queryFn: () => loanService.getTotalInterestPaid(id!, fromDate, toDate),
    enabled: !!id,
    staleTime: 1000 * 30,
  });
}

export function useCreateLoan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateLoanDto) => loanService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.lists() });
    },
  });
}

export function useUpdateLoan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateLoanDto }) => loanService.update(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.lists() });
    },
  });
}

export function useDeleteLoan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => loanService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.lists() });
    },
  });
}

export function useRecordEmiPayment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateEmiPaymentDto }) => loanService.recordEmiPayment(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.schedule(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.outstanding() });
    },
  });
}

export function useRecordPrepayment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: PrepaymentDto }) => loanService.recordPrepayment(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.schedule(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.outstanding() });
    },
  });
}

export function useForecloseLoan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string }) => loanService.foreclose(id, notes),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.schedule(variables.id) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.outstanding() });
    },
  });
}

export function useInterestPayments(companyId?: string, fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.interestPayments(companyId, fromDate, toDate),
    queryFn: () => loanService.getInterestPayments(companyId, fromDate, toDate),
    staleTime: 1000 * 30,
  });
}


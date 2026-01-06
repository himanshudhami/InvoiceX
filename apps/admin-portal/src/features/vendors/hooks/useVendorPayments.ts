import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { vendorPaymentService } from '@/services/api/finance/ap/vendorPaymentService';
import type {
  VendorPayment,
  CreateVendorPaymentDto,
  UpdateVendorPaymentDto,
  VendorPaymentsFilterParams,
  CreateVendorPaymentAllocationDto,
} from '@/services/api/types';
import { vendorPaymentKeys, vendorKeys, vendorInvoiceKeys } from './vendorKeys';

/**
 * Fetch all vendor payments
 * @deprecated Use useVendorPaymentsPaged for server-side pagination
 */
export const useVendorPayments = (companyId?: string) => {
  return useQuery({
    queryKey: vendorPaymentKeys.list(companyId),
    queryFn: () => vendorPaymentService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated vendor payments with server-side filtering
 */
export const useVendorPaymentsPaged = (params: VendorPaymentsFilterParams = {}) => {
  return useQuery({
    queryKey: vendorPaymentKeys.paged(params),
    queryFn: () => vendorPaymentService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch vendor payments by vendor ID
 */
export const useVendorPaymentsByVendor = (vendorId: string, params: VendorPaymentsFilterParams = {}, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorPaymentKeys.byVendor(vendorId, params),
    queryFn: () => vendorPaymentService.getByVendor(vendorId, params),
    enabled: enabled && !!vendorId,
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single vendor payment by ID
 */
export const useVendorPayment = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorPaymentKeys.detail(id),
    queryFn: () => vendorPaymentService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch payment allocations
 */
export const useVendorPaymentAllocations = (paymentId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorPaymentKeys.allocations(paymentId),
    queryFn: () => vendorPaymentService.getAllocations(paymentId),
    enabled: enabled && !!paymentId,
    staleTime: 30 * 1000,
  });
};

/**
 * Create vendor payment mutation
 */
export const useCreateVendorPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateVendorPaymentDto) => vendorPaymentService.create(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.lists() });
      if (result.vendorId) {
        queryClient.invalidateQueries({ queryKey: vendorKeys.vendorOutstanding(result.vendorId) });
        queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.unpaid(result.vendorId) });
      }
    },
    onError: (error) => {
      console.error('Failed to create vendor payment:', error);
    },
  });
};

/**
 * Update vendor payment mutation
 */
export const useUpdateVendorPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateVendorPaymentDto }) =>
      vendorPaymentService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to update vendor payment:', error);
    },
  });
};

/**
 * Delete vendor payment mutation
 */
export const useDeleteVendorPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vendorPaymentService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete vendor payment:', error);
    },
  });
};

/**
 * Approve vendor payment mutation
 */
export const useApproveVendorPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vendorPaymentService.approve(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to approve vendor payment:', error);
    },
  });
};

/**
 * Process vendor payment mutation
 */
export const useProcessVendorPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vendorPaymentService.process(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to process vendor payment:', error);
    },
  });
};

/**
 * Cancel vendor payment mutation
 */
export const useCancelVendorPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vendorPaymentService.cancel(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to cancel vendor payment:', error);
    },
  });
};

/**
 * Add allocation to vendor payment
 */
export const useAddVendorPaymentAllocation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ paymentId, data }: { paymentId: string; data: CreateVendorPaymentAllocationDto }) =>
      vendorPaymentService.addAllocation(paymentId, data),
    onSuccess: (_, { paymentId }) => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.allocations(paymentId) });
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.detail(paymentId) });
    },
    onError: (error) => {
      console.error('Failed to add allocation:', error);
    },
  });
};

/**
 * Remove allocation from vendor payment
 */
export const useRemoveVendorPaymentAllocation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ paymentId, allocationId }: { paymentId: string; allocationId: string }) =>
      vendorPaymentService.removeAllocation(paymentId, allocationId),
    onSuccess: (_, { paymentId }) => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.allocations(paymentId) });
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.detail(paymentId) });
    },
    onError: (error) => {
      console.error('Failed to remove allocation:', error);
    },
  });
};

/**
 * Auto-allocate vendor payment
 */
export const useAutoAllocateVendorPayment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (paymentId: string) => vendorPaymentService.autoAllocate(paymentId),
    onSuccess: (_, paymentId) => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.allocations(paymentId) });
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.detail(paymentId) });
    },
    onError: (error) => {
      console.error('Failed to auto-allocate payment:', error);
    },
  });
};

/**
 * Mark TDS as deposited
 */
export const useMarkTdsDeposited = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, challanNumber, depositDate }: { id: string; challanNumber: string; depositDate: string }) =>
      vendorPaymentService.markTdsDeposited(id, challanNumber, depositDate),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to mark TDS as deposited:', error);
    },
  });
};

/**
 * Bulk approve vendor payments
 */
export const useBulkApproveVendorPayments = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ids: string[]) => vendorPaymentService.bulkApprove(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to bulk approve vendor payments:', error);
    },
  });
};

/**
 * Bulk process vendor payments
 */
export const useBulkProcessVendorPayments = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ids: string[]) => vendorPaymentService.bulkProcess(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorPaymentKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to bulk process vendor payments:', error);
    },
  });
};

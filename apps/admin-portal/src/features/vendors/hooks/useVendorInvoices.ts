import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { vendorInvoiceService } from '@/services/api/finance/ap/vendorInvoiceService';
import type {
  VendorInvoice,
  CreateVendorInvoiceDto,
  UpdateVendorInvoiceDto,
  VendorInvoicesFilterParams,
} from '@/services/api/types';
import { vendorInvoiceKeys, vendorKeys } from './vendorKeys';

/**
 * Fetch all vendor invoices
 * @deprecated Use useVendorInvoicesPaged for server-side pagination
 */
export const useVendorInvoices = (companyId?: string) => {
  return useQuery({
    queryKey: vendorInvoiceKeys.list(companyId),
    queryFn: () => vendorInvoiceService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated vendor invoices with server-side filtering
 */
export const useVendorInvoicesPaged = (params: VendorInvoicesFilterParams = {}) => {
  return useQuery({
    queryKey: vendorInvoiceKeys.paged(params),
    queryFn: () => vendorInvoiceService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch vendor invoices by vendor ID
 */
export const useVendorInvoicesByVendor = (vendorId: string, params: VendorInvoicesFilterParams = {}, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorInvoiceKeys.byVendor(vendorId, params),
    queryFn: () => vendorInvoiceService.getByVendor(vendorId, params),
    enabled: enabled && !!vendorId,
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single vendor invoice by ID
 */
export const useVendorInvoice = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorInvoiceKeys.detail(id),
    queryFn: () => vendorInvoiceService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch unpaid vendor invoices for a vendor (for payment allocation)
 */
export const useUnpaidVendorInvoices = (vendorId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorInvoiceKeys.unpaid(vendorId),
    queryFn: () => vendorInvoiceService.getUnpaidByVendor(vendorId),
    enabled: enabled && !!vendorId,
    staleTime: 30 * 1000,
  });
};

/**
 * Create vendor invoice mutation
 */
export const useCreateVendorInvoice = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateVendorInvoiceDto) => vendorInvoiceService.create(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.lists() });
      if (result.vendorId) {
        queryClient.invalidateQueries({ queryKey: vendorKeys.vendorOutstanding(result.vendorId) });
      }
    },
    onError: (error) => {
      console.error('Failed to create vendor invoice:', error);
    },
  });
};

/**
 * Update vendor invoice mutation
 */
export const useUpdateVendorInvoice = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateVendorInvoiceDto }) =>
      vendorInvoiceService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to update vendor invoice:', error);
    },
  });
};

/**
 * Delete vendor invoice mutation
 */
export const useDeleteVendorInvoice = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vendorInvoiceService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete vendor invoice:', error);
    },
  });
};

/**
 * Approve vendor invoice mutation
 */
export const useApproveVendorInvoice = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vendorInvoiceService.approve(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to approve vendor invoice:', error);
    },
  });
};

/**
 * Reject vendor invoice mutation
 */
export const useRejectVendorInvoice = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      vendorInvoiceService.reject(id, reason),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to reject vendor invoice:', error);
    },
  });
};

/**
 * Post vendor invoice to ledger mutation
 */
export const usePostVendorInvoice = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vendorInvoiceService.post(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to post vendor invoice:', error);
    },
  });
};

/**
 * Cancel vendor invoice mutation
 */
export const useCancelVendorInvoice = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vendorInvoiceService.cancel(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to cancel vendor invoice:', error);
    },
  });
};

/**
 * Bulk approve vendor invoices mutation
 */
export const useBulkApproveVendorInvoices = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ids: string[]) => vendorInvoiceService.bulkApprove(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to bulk approve vendor invoices:', error);
    },
  });
};

/**
 * Bulk post vendor invoices mutation
 */
export const useBulkPostVendorInvoices = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ids: string[]) => vendorInvoiceService.bulkPost(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorInvoiceKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to bulk post vendor invoices:', error);
    },
  });
};

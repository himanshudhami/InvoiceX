import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { vendorService } from '@/services/api/finance/ap/vendorService';
import type {
  Vendor,
  CreateVendorDto,
  UpdateVendorDto,
  VendorsFilterParams,
} from '@/services/api/types';
import { vendorKeys } from './vendorKeys';

/**
 * Fetch all vendors (use useVendorsPaged for better performance)
 * @deprecated Use useVendorsPaged for server-side pagination
 */
export const useVendors = (companyId?: string) => {
  return useQuery({
    queryKey: vendorKeys.list(companyId),
    queryFn: () => vendorService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated vendors with server-side filtering
 */
export const useVendorsPaged = (params: VendorsFilterParams = {}) => {
  return useQuery({
    queryKey: vendorKeys.paged(params),
    queryFn: () => vendorService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single vendor by ID
 */
export const useVendor = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorKeys.detail(id),
    queryFn: () => vendorService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Create vendor mutation
 */
export const useCreateVendor = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateVendorDto) => vendorService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to create vendor:', error);
    },
  });
};

/**
 * Update vendor mutation
 */
export const useUpdateVendor = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateVendorDto }) =>
      vendorService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: vendorKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: vendorKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to update vendor:', error);
    },
  });
};

/**
 * Delete vendor mutation
 */
export const useDeleteVendor = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vendorService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to delete vendor:', error);
    },
  });
};

/**
 * Fetch vendor outstanding balances for a company
 */
export const useVendorOutstanding = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorKeys.outstanding(companyId),
    queryFn: () => vendorService.getOutstanding(companyId),
    enabled: enabled && !!companyId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch outstanding for a specific vendor
 */
export const useVendorOutstandingDetail = (vendorId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorKeys.vendorOutstanding(vendorId),
    queryFn: () => vendorService.getVendorOutstanding(vendorId),
    enabled: enabled && !!vendorId,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch vendor aging summary
 */
export const useVendorAging = (companyId: string, asOfDate?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorKeys.aging(companyId, asOfDate),
    queryFn: () => vendorService.getAgingSummary(companyId, asOfDate),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch vendor TDS summary
 */
export const useVendorTdsSummary = (companyId: string, financialYear?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: vendorKeys.tdsSummary(companyId, financialYear),
    queryFn: () => vendorService.getTdsSummary(companyId, financialYear),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

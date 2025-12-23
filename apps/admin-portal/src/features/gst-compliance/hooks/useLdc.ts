import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';

import { ldcService } from '@/services/api/gst';
import type { CreateLdcDto, UpdateLdcDto, PaginationParams } from '@/services/api/types';
import { gstKeys } from './gstKeys';

// ==================== LDC Queries ====================

/**
 * Fetch all LDC certificates
 */
export const useLdcCertificates = (companyId?: string) => {
  return useQuery({
    queryKey: gstKeys.ldc.list(companyId),
    queryFn: () => ldcService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch paged LDC certificates
 */
export const useLdcCertificatesPaged = (
  params: PaginationParams & {
    companyId?: string;
    status?: string;
    tdsSection?: string;
    deducteePan?: string;
  } = {}
) => {
  return useQuery({
    queryKey: gstKeys.ldc.paged(params),
    queryFn: () => ldcService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single LDC certificate
 */
export const useLdcCertificate = (id: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.ldc.detail(id),
    queryFn: () => ldcService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch active LDC certificates
 */
export const useActiveLdcCertificates = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.ldc.active(companyId),
    queryFn: () => ldcService.getActive(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch expiring LDC certificates (within 30 days)
 */
export const useExpiringLdcCertificates = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.ldc.expiring(companyId),
    queryFn: () => ldcService.getExpiring(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch LDC certificates by deductee PAN
 */
export const useLdcByDeducteePan = (companyId: string, pan: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.ldc.byPan(companyId, pan),
    queryFn: () => ldcService.getByDeducteePan(companyId, pan),
    enabled: enabled && !!companyId && !!pan,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch LDC usage records
 */
export const useLdcUsageRecords = (certificateId: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.ldc.usage(certificateId),
    queryFn: () => ldcService.getUsageRecords(certificateId),
    enabled: enabled && !!certificateId,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== LDC Mutations ====================

/**
 * Create LDC certificate
 */
export const useCreateLdcCertificate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateLdcDto) => ldcService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: gstKeys.ldc.lists() });
      toast.success('LDC certificate created successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to create LDC certificate');
    },
  });
};

/**
 * Update LDC certificate
 */
export const useUpdateLdcCertificate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateLdcDto }) => ldcService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.ldc.detail(id) });
      queryClient.invalidateQueries({ queryKey: gstKeys.ldc.lists() });
      toast.success('LDC certificate updated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to update LDC certificate');
    },
  });
};

/**
 * Delete LDC certificate
 */
export const useDeleteLdcCertificate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => ldcService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: gstKeys.ldc.lists() });
      toast.success('LDC certificate deleted');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to delete LDC certificate');
    },
  });
};

/**
 * Cancel LDC certificate
 */
export const useCancelLdcCertificate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) => ldcService.cancel(id, reason),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.ldc.detail(result.id) });
      queryClient.invalidateQueries({ queryKey: gstKeys.ldc.lists() });
      toast.success('LDC certificate cancelled');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to cancel LDC certificate');
    },
  });
};

/**
 * Validate LDC for a transaction
 */
export const useValidateLdc = () => {
  return useMutation({
    mutationFn: ({
      companyId,
      deducteePan,
      section,
      transactionDate,
      amount,
    }: {
      companyId: string;
      deducteePan: string;
      section: string;
      transactionDate: string;
      amount: number;
    }) => ldcService.validate(companyId, deducteePan, section, transactionDate, amount),
  });
};

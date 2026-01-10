import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';

import { gstr3bService } from '@/services/api/gst';
import type {
  GenerateGstr3bRequest,
  ReviewGstr3bRequest,
  FileGstr3bRequest,
} from '@/services/api/types';
import { gstKeys } from './gstKeys';

// ==================== Filing Queries ====================

/**
 * Fetch GSTR-3B filing by ID
 */
export const useGstr3bFiling = (id: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.gstr3b.filing(id),
    queryFn: () => gstr3bService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch GSTR-3B filing by period
 */
export const useGstr3bByPeriod = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr3b.byPeriod(companyId, returnPeriod),
    queryFn: () => gstr3bService.getByPeriod(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 5 * 60 * 1000,
    retry: false, // Don't retry on 404
  });
};

// ==================== Table Preview Queries ====================

/**
 * Fetch Table 3.1 - Outward supplies (preview)
 */
export const useGstr3bTable31 = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr3b.table31(companyId, returnPeriod),
    queryFn: () => gstr3bService.getTable31(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch Table 4 - ITC (preview)
 */
export const useGstr3bTable4 = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr3b.table4(companyId, returnPeriod),
    queryFn: () => gstr3bService.getTable4(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch Table 5 - Exempt supplies (preview)
 */
export const useGstr3bTable5 = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr3b.table5(companyId, returnPeriod),
    queryFn: () => gstr3bService.getTable5(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 2 * 60 * 1000,
  });
};

// ==================== Drill-down Queries ====================

/**
 * Fetch line items for a filing
 */
export const useGstr3bLineItems = (
  filingId: string,
  tableCode?: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr3b.lineItems(filingId, tableCode),
    queryFn: () => gstr3bService.getLineItems(filingId, tableCode),
    enabled: enabled && !!filingId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch source documents for a line item (drill-down)
 */
export const useGstr3bSourceDocuments = (lineItemId: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.gstr3b.sourceDocuments(lineItemId),
    queryFn: () => gstr3bService.getSourceDocuments(lineItemId),
    enabled: enabled && !!lineItemId,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== Variance Query ====================

/**
 * Fetch variance compared to previous period
 */
export const useGstr3bVariance = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr3b.variance(companyId, returnPeriod),
    queryFn: () => gstr3bService.getVariance(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 5 * 60 * 1000,
  });
};

// ==================== History Query ====================

/**
 * Fetch filing history for a company
 */
export const useGstr3bHistory = (
  companyId: string,
  params?: {
    pageNumber?: number;
    pageSize?: number;
    financialYear?: string;
    status?: string;
  },
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr3b.history(companyId, params),
    queryFn: () => gstr3bService.getHistory(companyId, params),
    enabled: enabled && !!companyId,
    staleTime: 30 * 1000,
    keepPreviousData: true,
  });
};

// ==================== Mutations ====================

/**
 * Generate GSTR-3B filing pack
 */
export const useGenerateGstr3b = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: GenerateGstr3bRequest) => gstr3bService.generate(request),
    onSuccess: (result, variables) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr3b.filings() });
      queryClient.invalidateQueries({
        queryKey: gstKeys.gstr3b.byPeriod(variables.companyId, variables.returnPeriod),
      });
      queryClient.invalidateQueries({
        queryKey: gstKeys.gstr3b.history(variables.companyId),
      });
      toast.success('GSTR-3B filing pack generated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to generate GSTR-3B filing pack');
    },
  });
};

/**
 * Mark filing as reviewed
 */
export const useReviewGstr3b = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      filingId,
      request,
    }: {
      filingId: string;
      request?: ReviewGstr3bRequest;
    }) => gstr3bService.markAsReviewed(filingId, request),
    onSuccess: (_, { filingId }) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr3b.filing(filingId) });
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr3b.filings() });
      toast.success('Filing marked as reviewed');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to mark filing as reviewed');
    },
  });
};

/**
 * Mark filing as filed (with ARN)
 */
export const useFileGstr3b = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      filingId,
      request,
    }: {
      filingId: string;
      request: FileGstr3bRequest;
    }) => gstr3bService.markAsFiled(filingId, request),
    onSuccess: (_, { filingId }) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr3b.filing(filingId) });
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr3b.filings() });
      toast.success('Filing marked as filed with GSTN');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to mark filing as filed');
    },
  });
};

/**
 * Export filing to JSON
 */
export const useExportGstr3bJson = () => {
  return useMutation({
    mutationFn: (filingId: string) => gstr3bService.exportJson(filingId),
    onSuccess: (jsonData) => {
      // Create a blob and download
      const blob = new Blob([jsonData], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'gstr3b-filing.json';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      toast.success('GSTR-3B JSON exported successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to export GSTR-3B JSON');
    },
  });
};

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';

import { gstr2bService } from '@/services/api/gst';
import type {
  ImportGstr2bRequest,
  RunReconciliationRequest,
  AcceptMismatchRequest,
  RejectInvoiceRequest,
  ManualMatchRequest,
} from '@/services/api/types';
import { gstKeys } from './gstKeys';

// ==================== Import Queries ====================

/**
 * Fetch GSTR-2B import by ID
 */
export const useGstr2bImport = (id: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.gstr2b.import(id),
    queryFn: () => gstr2bService.getImportById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch GSTR-2B import by period
 */
export const useGstr2bImportByPeriod = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr2b.importByPeriod(companyId, returnPeriod),
    queryFn: () => gstr2bService.getImportByPeriod(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 5 * 60 * 1000,
    retry: false, // Don't retry on 404
  });
};

/**
 * Fetch GSTR-2B imports for a company (paged)
 */
export const useGstr2bImports = (
  companyId: string,
  params?: {
    pageNumber?: number;
    pageSize?: number;
    status?: string;
  },
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr2b.importList(companyId, params),
    queryFn: () => gstr2bService.getImports(companyId, params),
    enabled: enabled && !!companyId,
    staleTime: 30 * 1000,
  });
};

// ==================== Reconciliation Queries ====================

/**
 * Fetch reconciliation summary
 */
export const useGstr2bReconciliationSummary = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr2b.reconciliationSummary(companyId, returnPeriod),
    queryFn: () => gstr2bService.getReconciliationSummary(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch supplier-wise summary
 */
export const useGstr2bSupplierSummary = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr2b.supplierSummary(companyId, returnPeriod),
    queryFn: () => gstr2bService.getSupplierSummary(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Fetch ITC comparison (GSTR-2B vs Books)
 */
export const useGstr2bItcComparison = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr2b.itcComparison(companyId, returnPeriod),
    queryFn: () => gstr2bService.getItcComparison(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 2 * 60 * 1000,
  });
};

// ==================== Invoice Queries ====================

/**
 * Fetch invoices for an import (paged)
 */
export const useGstr2bInvoices = (
  importId: string,
  params?: {
    pageNumber?: number;
    pageSize?: number;
    matchStatus?: string;
    invoiceType?: string;
    searchTerm?: string;
  },
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr2b.invoices(importId, params),
    queryFn: () => gstr2bService.getInvoices(importId, params),
    enabled: enabled && !!importId,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch invoice by ID
 */
export const useGstr2bInvoice = (id: string, enabled = true) => {
  return useQuery({
    queryKey: gstKeys.gstr2b.invoice(id),
    queryFn: () => gstr2bService.getInvoiceById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch unmatched invoices for a period
 */
export const useGstr2bMismatches = (
  companyId: string,
  returnPeriod: string,
  enabled = true
) => {
  return useQuery({
    queryKey: gstKeys.gstr2b.mismatches(companyId, returnPeriod),
    queryFn: () => gstr2bService.getUnmatchedInvoices(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 30 * 1000,
  });
};

// ==================== Mutations ====================

/**
 * Import GSTR-2B JSON data
 */
export const useImportGstr2b = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ImportGstr2bRequest) => gstr2bService.import(request),
    onSuccess: (result, variables) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.imports() });
      queryClient.invalidateQueries({
        queryKey: gstKeys.gstr2b.importByPeriod(variables.companyId, variables.returnPeriod),
      });
      queryClient.invalidateQueries({
        queryKey: gstKeys.gstr2b.importList(variables.companyId),
      });
      toast.success(`GSTR-2B imported: ${result.totalInvoices} invoices`);
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to import GSTR-2B data');
    },
  });
};

/**
 * Run reconciliation
 */
export const useRunGstr2bReconciliation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RunReconciliationRequest) => gstr2bService.runReconciliation(request),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.all });
      toast.success(
        `Reconciliation complete: ${result.matchedInvoices}/${result.totalInvoices} matched`
      );
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to run reconciliation');
    },
  });
};

/**
 * Delete an import
 */
export const useDeleteGstr2bImport = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => gstr2bService.deleteImport(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.all });
      toast.success('Import deleted');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to delete import');
    },
  });
};

/**
 * Accept a mismatch
 */
export const useAcceptGstr2bMismatch = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: AcceptMismatchRequest) => gstr2bService.acceptMismatch(request),
    onSuccess: (_, { invoiceId }) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.invoice(invoiceId) });
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.all });
      toast.success('Mismatch accepted');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to accept mismatch');
    },
  });
};

/**
 * Reject an invoice
 */
export const useRejectGstr2bInvoice = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RejectInvoiceRequest) => gstr2bService.rejectInvoice(request),
    onSuccess: (_, { invoiceId }) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.invoice(invoiceId) });
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.all });
      toast.success('Invoice rejected');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to reject invoice');
    },
  });
};

/**
 * Manual match
 */
export const useManualMatchGstr2b = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ManualMatchRequest) => gstr2bService.manualMatch(request),
    onSuccess: (_, { gstr2bInvoiceId }) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.invoice(gstr2bInvoiceId) });
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.all });
      toast.success('Manual match created');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to create manual match');
    },
  });
};

/**
 * Reset action (undo accept/reject)
 */
export const useResetGstr2bAction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (invoiceId: string) => gstr2bService.resetAction(invoiceId),
    onSuccess: (_, invoiceId) => {
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.invoice(invoiceId) });
      queryClient.invalidateQueries({ queryKey: gstKeys.gstr2b.all });
      toast.success('Action reset');
    },
    onError: (error: any) => {
      toast.error(error?.message || 'Failed to reset action');
    },
  });
};

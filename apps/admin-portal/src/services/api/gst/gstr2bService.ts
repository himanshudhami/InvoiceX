import { apiClient } from '../client';
import type {
  Gstr2bImport,
  Gstr2bInvoice,
  Gstr2bInvoiceListItem,
  Gstr2bReconciliationSummary,
  Gstr2bSupplierSummary,
  Gstr2bItcComparison,
  ImportGstr2bRequest,
  RunReconciliationRequest,
  AcceptMismatchRequest,
  RejectInvoiceRequest,
  ManualMatchRequest,
  PagedResponse,
} from '../types';

/**
 * GSTR-2B Ingestion & Reconciliation Service
 *
 * Handles import of GSTR-2B JSON data and reconciliation with vendor invoices
 */
export class Gstr2bService {
  private readonly endpoint = 'gst/gstr2b';

  // ==================== Import ====================

  /**
   * Import GSTR-2B JSON data
   */
  async import(request: ImportGstr2bRequest): Promise<Gstr2bImport> {
    return apiClient.post<Gstr2bImport>(`${this.endpoint}/import`, request);
  }

  /**
   * Get import by ID
   */
  async getImportById(id: string): Promise<Gstr2bImport> {
    return apiClient.get<Gstr2bImport>(`${this.endpoint}/import/${id}`);
  }

  /**
   * Get import by period
   */
  async getImportByPeriod(companyId: string, returnPeriod: string): Promise<Gstr2bImport> {
    return apiClient.get<Gstr2bImport>(
      `${this.endpoint}/import/period/${companyId}/${returnPeriod}`
    );
  }

  /**
   * Get imports for a company (paged)
   */
  async getImports(
    companyId: string,
    params?: {
      pageNumber?: number;
      pageSize?: number;
      status?: string;
    }
  ): Promise<PagedResponse<Gstr2bImport>> {
    const queryParams = new URLSearchParams();
    if (params?.pageNumber) queryParams.set('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString());
    if (params?.status) queryParams.set('status', params.status);

    const query = queryParams.toString();
    return apiClient.get<PagedResponse<Gstr2bImport>>(
      `${this.endpoint}/imports/${companyId}${query ? `?${query}` : ''}`
    );
  }

  /**
   * Delete an import and all its invoices
   */
  async deleteImport(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/imports/${id}`);
  }

  // ==================== Reconciliation ====================

  /**
   * Run reconciliation on an import
   */
  async runReconciliation(request: RunReconciliationRequest): Promise<Gstr2bReconciliationSummary> {
    return apiClient.post<Gstr2bReconciliationSummary>(
      `${this.endpoint}/reconcile`,
      request
    );
  }

  /**
   * Get reconciliation summary for a period
   */
  async getReconciliationSummary(
    companyId: string,
    returnPeriod: string
  ): Promise<Gstr2bReconciliationSummary> {
    return apiClient.get<Gstr2bReconciliationSummary>(
      `${this.endpoint}/reconciliation-summary/${companyId}/${returnPeriod}`
    );
  }

  /**
   * Get supplier-wise summary for a period
   */
  async getSupplierSummary(
    companyId: string,
    returnPeriod: string
  ): Promise<Gstr2bSupplierSummary[]> {
    return apiClient.get<Gstr2bSupplierSummary[]>(
      `${this.endpoint}/supplier-summary/${companyId}/${returnPeriod}`
    );
  }

  /**
   * Get ITC comparison (GSTR-2B vs Books)
   */
  async getItcComparison(companyId: string, returnPeriod: string): Promise<Gstr2bItcComparison> {
    return apiClient.get<Gstr2bItcComparison>(
      `${this.endpoint}/itc-comparison/${companyId}/${returnPeriod}`
    );
  }

  // ==================== Invoices ====================

  /**
   * Get invoices for an import (paged)
   */
  async getInvoices(
    importId: string,
    params?: {
      pageNumber?: number;
      pageSize?: number;
      matchStatus?: string;
      invoiceType?: string;
      searchTerm?: string;
    }
  ): Promise<PagedResponse<Gstr2bInvoiceListItem>> {
    const queryParams = new URLSearchParams();
    if (params?.pageNumber) queryParams.set('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString());
    if (params?.matchStatus) queryParams.set('matchStatus', params.matchStatus);
    if (params?.invoiceType) queryParams.set('invoiceType', params.invoiceType);
    if (params?.searchTerm) queryParams.set('searchTerm', params.searchTerm);

    const query = queryParams.toString();
    return apiClient.get<PagedResponse<Gstr2bInvoiceListItem>>(
      `${this.endpoint}/invoices/${importId}${query ? `?${query}` : ''}`
    );
  }

  /**
   * Get invoice by ID
   */
  async getInvoiceById(id: string): Promise<Gstr2bInvoice> {
    return apiClient.get<Gstr2bInvoice>(`${this.endpoint}/invoice/${id}`);
  }

  /**
   * Get unmatched invoices for a period
   */
  async getUnmatchedInvoices(
    companyId: string,
    returnPeriod: string
  ): Promise<Gstr2bInvoiceListItem[]> {
    return apiClient.get<Gstr2bInvoiceListItem[]>(
      `${this.endpoint}/mismatches/${companyId}/${returnPeriod}`
    );
  }

  // ==================== Actions ====================

  /**
   * Accept a mismatch
   */
  async acceptMismatch(request: AcceptMismatchRequest): Promise<void> {
    return apiClient.post(`${this.endpoint}/accept`, request);
  }

  /**
   * Reject an invoice
   */
  async rejectInvoice(request: RejectInvoiceRequest): Promise<void> {
    return apiClient.post(`${this.endpoint}/reject`, request);
  }

  /**
   * Manually match a GSTR-2B invoice to a vendor invoice
   */
  async manualMatch(request: ManualMatchRequest): Promise<void> {
    return apiClient.post(`${this.endpoint}/manual-match`, request);
  }

  /**
   * Reset action (undo accept/reject)
   */
  async resetAction(invoiceId: string): Promise<void> {
    return apiClient.post(`${this.endpoint}/reset/${invoiceId}`, {});
  }
}

export const gstr2bService = new Gstr2bService();

import { apiClient } from '../client';
import type {
  Gstr3bFiling,
  Gstr3bTable31,
  Gstr3bTable4,
  Gstr3bTable5,
  Gstr3bLineItem,
  Gstr3bSourceDocument,
  Gstr3bVarianceSummary,
  Gstr3bFilingHistory,
  GenerateGstr3bRequest,
  ReviewGstr3bRequest,
  FileGstr3bRequest,
  PagedResponse,
} from '../types';

/**
 * GSTR-3B Filing Pack Service
 *
 * Handles GSTR-3B generation, tables preview, drill-down, and filing workflow
 */
export class Gstr3bService {
  private readonly endpoint = 'gst/gstr3b';

  // ==================== Filing Generation ====================

  /**
   * Generate GSTR-3B filing pack for a period
   */
  async generate(request: GenerateGstr3bRequest): Promise<Gstr3bFiling> {
    return apiClient.post<Gstr3bFiling>(`${this.endpoint}/generate`, request);
  }

  /**
   * Get filing by ID
   */
  async getById(id: string): Promise<Gstr3bFiling> {
    return apiClient.get<Gstr3bFiling>(`${this.endpoint}/${id}`);
  }

  /**
   * Get filing for a specific period
   */
  async getByPeriod(companyId: string, returnPeriod: string): Promise<Gstr3bFiling> {
    return apiClient.get<Gstr3bFiling>(`${this.endpoint}/${companyId}/${returnPeriod}`);
  }

  // ==================== Individual Tables ====================

  /**
   * Build Table 3.1 - Outward supplies (preview without saving)
   */
  async getTable31(companyId: string, returnPeriod: string): Promise<Gstr3bTable31> {
    return apiClient.get<Gstr3bTable31>(
      `${this.endpoint}/table/3.1/${companyId}/${returnPeriod}`
    );
  }

  /**
   * Build Table 4 - ITC (preview without saving)
   */
  async getTable4(companyId: string, returnPeriod: string): Promise<Gstr3bTable4> {
    return apiClient.get<Gstr3bTable4>(
      `${this.endpoint}/table/4/${companyId}/${returnPeriod}`
    );
  }

  /**
   * Build Table 5 - Exempt supplies (preview without saving)
   */
  async getTable5(companyId: string, returnPeriod: string): Promise<Gstr3bTable5> {
    return apiClient.get<Gstr3bTable5>(
      `${this.endpoint}/table/5/${companyId}/${returnPeriod}`
    );
  }

  // ==================== Drill-down ====================

  /**
   * Get line items for a filing (optionally filtered by table)
   */
  async getLineItems(filingId: string, tableCode?: string): Promise<Gstr3bLineItem[]> {
    const params = tableCode ? `?tableCode=${encodeURIComponent(tableCode)}` : '';
    return apiClient.get<Gstr3bLineItem[]>(`${this.endpoint}/${filingId}/line-items${params}`);
  }

  /**
   * Get source documents for a line item (drill-down)
   */
  async getSourceDocuments(lineItemId: string): Promise<Gstr3bSourceDocument[]> {
    return apiClient.get<Gstr3bSourceDocument[]>(`${this.endpoint}/drilldown/${lineItemId}`);
  }

  // ==================== Variance ====================

  /**
   * Get variance compared to previous period
   */
  async getVariance(companyId: string, returnPeriod: string): Promise<Gstr3bVarianceSummary> {
    return apiClient.get<Gstr3bVarianceSummary>(
      `${this.endpoint}/variance/${companyId}/${returnPeriod}`
    );
  }

  // ==================== Filing Workflow ====================

  /**
   * Mark filing as reviewed
   */
  async markAsReviewed(filingId: string, request?: ReviewGstr3bRequest): Promise<void> {
    return apiClient.post(`${this.endpoint}/${filingId}/review`, request || {});
  }

  /**
   * Mark filing as filed (with ARN from GSTN)
   */
  async markAsFiled(filingId: string, request: FileGstr3bRequest): Promise<void> {
    return apiClient.post(`${this.endpoint}/${filingId}/filed`, request);
  }

  // ==================== History ====================

  /**
   * Get filing history for a company
   */
  async getHistory(
    companyId: string,
    params?: {
      pageNumber?: number;
      pageSize?: number;
      financialYear?: string;
      status?: string;
    }
  ): Promise<PagedResponse<Gstr3bFilingHistory>> {
    const queryParams = new URLSearchParams();
    if (params?.pageNumber) queryParams.set('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString());
    if (params?.financialYear) queryParams.set('financialYear', params.financialYear);
    if (params?.status) queryParams.set('status', params.status);

    const query = queryParams.toString();
    return apiClient.get<PagedResponse<Gstr3bFilingHistory>>(
      `${this.endpoint}/history/${companyId}${query ? `?${query}` : ''}`
    );
  }

  // ==================== Export ====================

  /**
   * Export filing to JSON (GSTN format)
   */
  async exportJson(filingId: string): Promise<string> {
    return apiClient.get<string>(`${this.endpoint}/${filingId}/export/json`);
  }
}

export const gstr3bService = new Gstr3bService();

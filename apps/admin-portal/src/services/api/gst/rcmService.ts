import { apiClient } from '../client';
import {
  RcmTransaction,
  CreateRcmTransactionDto,
  RcmPaymentRequest,
  RcmItcClaimRequest,
  RcmSummary,
  PagedResponse,
  PaginationParams,
} from '../types';

/**
 * RCM (Reverse Charge Mechanism) Service
 *
 * Handles RCM transactions for:
 * - Legal services
 * - Security services
 * - GTA (Goods Transport Agency)
 * - Import of services
 */
export class RcmService {
  private readonly endpoint = 'gst/rcm';

  /**
   * Get all RCM transactions for a company
   */
  async getAll(companyId?: string): Promise<RcmTransaction[]> {
    const params = companyId ? `?companyId=${companyId}` : '';
    return apiClient.get<RcmTransaction[]>(`${this.endpoint}${params}`);
  }

  /**
   * Get paged RCM transactions
   */
  async getPaged(
    params?: PaginationParams & {
      companyId?: string;
      status?: string;
      rcmCategory?: string;
      returnPeriod?: string;
    }
  ): Promise<PagedResponse<RcmTransaction>> {
    return apiClient.getPaged<RcmTransaction>(this.endpoint, params);
  }

  /**
   * Get RCM transaction by ID
   */
  async getById(id: string): Promise<RcmTransaction> {
    return apiClient.get<RcmTransaction>(`${this.endpoint}/${id}`);
  }

  /**
   * Create new RCM transaction
   */
  async create(data: CreateRcmTransactionDto): Promise<RcmTransaction> {
    return apiClient.post<RcmTransaction>(this.endpoint, data);
  }

  /**
   * Update RCM transaction
   */
  async update(id: string, data: Partial<CreateRcmTransactionDto>): Promise<RcmTransaction> {
    return apiClient.put<RcmTransaction>(`${this.endpoint}/${id}`, data);
  }

  /**
   * Delete RCM transaction
   */
  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  /**
   * Get pending RCM transactions (not yet paid)
   */
  async getPending(companyId: string): Promise<RcmTransaction[]> {
    return apiClient.get<RcmTransaction[]>(`${this.endpoint}/pending/${companyId}`);
  }

  /**
   * Get RCM transactions pending ITC claim
   */
  async getPendingItcClaim(companyId: string): Promise<RcmTransaction[]> {
    return apiClient.get<RcmTransaction[]>(`${this.endpoint}/pending-itc/${companyId}`);
  }

  /**
   * Record RCM payment (Stage 1)
   */
  async recordPayment(request: RcmPaymentRequest): Promise<RcmTransaction> {
    return apiClient.post<RcmTransaction>(`${this.endpoint}/payment`, request);
  }

  /**
   * Claim ITC on RCM (Stage 2)
   */
  async claimItc(request: RcmItcClaimRequest): Promise<RcmTransaction> {
    return apiClient.post<RcmTransaction>(`${this.endpoint}/claim-itc`, request);
  }

  /**
   * Get RCM summary for a return period
   */
  async getSummary(companyId: string, returnPeriod: string): Promise<RcmSummary> {
    return apiClient.get<RcmSummary>(`${this.endpoint}/summary/${companyId}/${returnPeriod}`);
  }

  /**
   * Get RCM transactions by category
   */
  async getByCategory(companyId: string, category: string): Promise<RcmTransaction[]> {
    return apiClient.get<RcmTransaction[]>(
      `${this.endpoint}/by-category/${companyId}/${category}`
    );
  }
}

export const rcmService = new RcmService();

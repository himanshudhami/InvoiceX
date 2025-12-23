import { apiClient } from '../client';
import {
  TcsTransaction,
  CreateTcsTransactionDto,
  TcsRemittanceRequest,
  TcsSummary,
  PagedResponse,
  PaginationParams,
} from '../types';

/**
 * TCS (Tax Collected at Source) Service
 *
 * Handles TCS operations under Section 206C(1H):
 * - Collection on sale of goods > Rs.50 lakhs
 * - Remittance tracking
 * - Summary reports
 */
export class TcsService {
  private readonly endpoint = 'tax/tcs';

  /**
   * Get all TCS transactions for a company
   */
  async getAll(companyId?: string): Promise<TcsTransaction[]> {
    const params = companyId ? `?companyId=${companyId}` : '';
    return apiClient.get<TcsTransaction[]>(`${this.endpoint}${params}`);
  }

  /**
   * Get paged TCS transactions
   */
  async getPaged(
    params?: PaginationParams & {
      companyId?: string;
      status?: string;
      financialYear?: string;
      quarter?: string;
      customerPan?: string;
    }
  ): Promise<PagedResponse<TcsTransaction>> {
    return apiClient.getPaged<TcsTransaction>(this.endpoint, params);
  }

  /**
   * Get TCS transaction by ID
   */
  async getById(id: string): Promise<TcsTransaction> {
    return apiClient.get<TcsTransaction>(`${this.endpoint}/${id}`);
  }

  /**
   * Create new TCS transaction (collection)
   */
  async create(data: CreateTcsTransactionDto): Promise<TcsTransaction> {
    return apiClient.post<TcsTransaction>(this.endpoint, data);
  }

  /**
   * Update TCS transaction
   */
  async update(id: string, data: Partial<CreateTcsTransactionDto>): Promise<TcsTransaction> {
    return apiClient.put<TcsTransaction>(`${this.endpoint}/${id}`, data);
  }

  /**
   * Delete TCS transaction
   */
  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  /**
   * Get pending TCS remittances
   */
  async getPendingRemittance(companyId: string): Promise<TcsTransaction[]> {
    return apiClient.get<TcsTransaction[]>(`${this.endpoint}/pending/${companyId}`);
  }

  /**
   * Record TCS remittance (bulk)
   */
  async recordRemittance(request: TcsRemittanceRequest): Promise<TcsTransaction[]> {
    return apiClient.post<TcsTransaction[]>(`${this.endpoint}/remittance`, request);
  }

  /**
   * Get TCS summary for a period
   */
  async getSummary(
    companyId: string,
    financialYear: string,
    quarter?: string
  ): Promise<TcsSummary> {
    const params = quarter ? `?quarter=${quarter}` : '';
    return apiClient.get<TcsSummary>(
      `${this.endpoint}/summary/${companyId}/${financialYear}${params}`
    );
  }

  /**
   * Get TCS transactions by customer
   */
  async getByCustomer(companyId: string, customerId: string): Promise<TcsTransaction[]> {
    return apiClient.get<TcsTransaction[]>(
      `${this.endpoint}/by-customer/${companyId}/${customerId}`
    );
  }

  /**
   * Calculate TCS for a sale
   * Returns applicable TCS amount based on threshold tracking
   */
  async calculateTcs(
    companyId: string,
    customerId: string,
    saleAmount: number,
    financialYear: string
  ): Promise<{
    isApplicable: boolean;
    tcsAmount: number;
    tcsRate: number;
    ytdSales: number;
    threshold: number;
  }> {
    return apiClient.post(`${this.endpoint}/calculate`, {
      companyId,
      customerId,
      saleAmount,
      financialYear,
    });
  }

  /**
   * Get TCS liability report
   */
  async getLiabilityReport(
    companyId: string,
    financialYear: string
  ): Promise<{
    collected: number;
    remitted: number;
    pending: number;
    dueDate: string;
    isOverdue: boolean;
  }> {
    return apiClient.get(`${this.endpoint}/liability/${companyId}/${financialYear}`);
  }
}

export const tcsService = new TcsService();

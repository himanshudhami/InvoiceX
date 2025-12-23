import { apiClient } from '../client';
import {
  LowerDeductionCertificate,
  CreateLdcDto,
  UpdateLdcDto,
  LdcValidationResult,
  LdcUsageRecord,
  PagedResponse,
  PaginationParams,
} from '../types';

/**
 * Lower Deduction Certificate (LDC) Service
 *
 * Handles Form 13 certificates for reduced TDS rates:
 * - Certificate CRUD operations
 * - Validation for transactions
 * - Usage tracking
 */
export class LdcService {
  private readonly endpoint = 'tax/ldc';

  /**
   * Get all LDC certificates for a company
   */
  async getAll(companyId?: string): Promise<LowerDeductionCertificate[]> {
    const params = companyId ? `?companyId=${companyId}` : '';
    return apiClient.get<LowerDeductionCertificate[]>(`${this.endpoint}${params}`);
  }

  /**
   * Get paged LDC certificates
   */
  async getPaged(
    params?: PaginationParams & {
      companyId?: string;
      status?: string;
      tdsSection?: string;
      deducteePan?: string;
    }
  ): Promise<PagedResponse<LowerDeductionCertificate>> {
    return apiClient.getPaged<LowerDeductionCertificate>(this.endpoint, params);
  }

  /**
   * Get LDC certificate by ID
   */
  async getById(id: string): Promise<LowerDeductionCertificate> {
    return apiClient.get<LowerDeductionCertificate>(`${this.endpoint}/${id}`);
  }

  /**
   * Create new LDC certificate
   */
  async create(data: CreateLdcDto): Promise<LowerDeductionCertificate> {
    return apiClient.post<LowerDeductionCertificate>(this.endpoint, data);
  }

  /**
   * Update LDC certificate
   */
  async update(id: string, data: UpdateLdcDto): Promise<LowerDeductionCertificate> {
    return apiClient.put<LowerDeductionCertificate>(`${this.endpoint}/${id}`, data);
  }

  /**
   * Delete LDC certificate
   */
  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  /**
   * Get active LDC certificates for a company
   */
  async getActive(companyId: string): Promise<LowerDeductionCertificate[]> {
    return apiClient.get<LowerDeductionCertificate[]>(`${this.endpoint}/active/${companyId}`);
  }

  /**
   * Get LDC certificates by deductee PAN
   */
  async getByDeducteePan(companyId: string, pan: string): Promise<LowerDeductionCertificate[]> {
    return apiClient.get<LowerDeductionCertificate[]>(
      `${this.endpoint}/by-pan/${companyId}/${pan}`
    );
  }

  /**
   * Validate LDC for a transaction
   */
  async validate(
    companyId: string,
    deducteePan: string,
    section: string,
    transactionDate: string,
    amount: number
  ): Promise<LdcValidationResult> {
    return apiClient.post<LdcValidationResult>(`${this.endpoint}/validate`, {
      companyId,
      deducteePan,
      section,
      transactionDate,
      amount,
    });
  }

  /**
   * Check if valid LDC exists for a deductee
   */
  async hasValidCertificate(
    companyId: string,
    deducteePan: string,
    section: string,
    transactionDate: string
  ): Promise<boolean> {
    const result = await apiClient.get<{ hasValidCertificate: boolean }>(
      `${this.endpoint}/check/${companyId}/${deducteePan}/${section}?date=${transactionDate}`
    );
    return result.hasValidCertificate;
  }

  /**
   * Get usage records for a certificate
   */
  async getUsageRecords(certificateId: string): Promise<LdcUsageRecord[]> {
    return apiClient.get<LdcUsageRecord[]>(`${this.endpoint}/${certificateId}/usage`);
  }

  /**
   * Get expiring certificates (within 30 days)
   */
  async getExpiring(companyId: string): Promise<LowerDeductionCertificate[]> {
    return apiClient.get<LowerDeductionCertificate[]>(`${this.endpoint}/expiring/${companyId}`);
  }

  /**
   * Cancel a certificate
   */
  async cancel(id: string, reason?: string): Promise<LowerDeductionCertificate> {
    return apiClient.post<LowerDeductionCertificate>(`${this.endpoint}/${id}/cancel`, { reason });
  }
}

export const ldcService = new LdcService();

import { apiClient } from './client';
import {
  EInvoiceCredentials,
  SaveEInvoiceCredentialsDto,
  EInvoiceGenerationResult,
  EInvoiceAuditLog,
  EInvoiceQueueItem,
  PagedResponse,
} from './types';

/**
 * E-Invoice API service for IRN generation, cancellation, and management
 */
export class EInvoiceService {
  private readonly baseEndpoint = 'einvoice';

  // ==================== IRN Operations ====================

  async generateIrn(invoiceId: string): Promise<EInvoiceGenerationResult> {
    return apiClient.post<EInvoiceGenerationResult, Record<string, never>>(
      `${this.baseEndpoint}/generate/${invoiceId}`,
      {}
    );
  }

  async cancelIrn(invoiceId: string, reason: string, reasonCode?: string): Promise<{ success: boolean; message: string }> {
    return apiClient.post<{ success: boolean; message: string }, { reason: string; reasonCode?: string }>(
      `${this.baseEndpoint}/cancel/${invoiceId}`,
      { reason, reasonCode }
    );
  }

  async checkApplicability(invoiceId: string): Promise<{ invoiceId: string; isApplicable: boolean }> {
    return apiClient.get<{ invoiceId: string; isApplicable: boolean }>(
      `${this.baseEndpoint}/applicable/${invoiceId}`
    );
  }

  async queueForGeneration(invoiceId: string, priority = 5): Promise<{ queueId: string; message: string }> {
    return apiClient.post<{ queueId: string; message: string }, Record<string, never>>(
      `${this.baseEndpoint}/queue/${invoiceId}`,
      {},
      { priority }
    );
  }

  // ==================== Credentials Management ====================

  async getCredentials(companyId: string): Promise<EInvoiceCredentials[]> {
    return apiClient.get<EInvoiceCredentials[]>(
      `${this.baseEndpoint}/credentials/company/${companyId}`
    );
  }

  async saveCredentials(data: SaveEInvoiceCredentialsDto): Promise<EInvoiceCredentials> {
    return apiClient.post<EInvoiceCredentials, SaveEInvoiceCredentialsDto>(
      `${this.baseEndpoint}/credentials`,
      data
    );
  }

  async deleteCredentials(id: string): Promise<{ message: string }> {
    return apiClient.delete<{ message: string }>(`${this.baseEndpoint}/credentials/${id}`);
  }

  // ==================== Audit Log ====================

  async getAuditLog(
    companyId: string,
    params: {
      pageNumber?: number;
      pageSize?: number;
      actionType?: string;
      fromDate?: string;
      toDate?: string;
    } = {}
  ): Promise<PagedResponse<EInvoiceAuditLog>> {
    return apiClient.get<PagedResponse<EInvoiceAuditLog>>(
      `${this.baseEndpoint}/audit/company/${companyId}`,
      params
    );
  }

  async getInvoiceAuditLog(invoiceId: string): Promise<EInvoiceAuditLog[]> {
    return apiClient.get<EInvoiceAuditLog[]>(
      `${this.baseEndpoint}/audit/invoice/${invoiceId}`
    );
  }

  async getErrors(companyId: string, limit = 50): Promise<EInvoiceAuditLog[]> {
    return apiClient.get<EInvoiceAuditLog[]>(
      `${this.baseEndpoint}/audit/errors/${companyId}`,
      { limit }
    );
  }

  // ==================== Queue Management ====================

  async getQueueStatus(companyId: string, status?: string): Promise<EInvoiceQueueItem[]> {
    return apiClient.get<EInvoiceQueueItem[]>(
      `${this.baseEndpoint}/queue/company/${companyId}`,
      status ? { status } : {}
    );
  }

  async getQueueByInvoice(invoiceId: string): Promise<EInvoiceQueueItem> {
    return apiClient.get<EInvoiceQueueItem>(
      `${this.baseEndpoint}/queue/invoice/${invoiceId}`
    );
  }

  async cancelQueueItem(id: string): Promise<{ message: string }> {
    return apiClient.post<{ message: string }, Record<string, never>>(
      `${this.baseEndpoint}/queue/${id}/cancel`,
      {}
    );
  }
}

// Singleton instance
export const eInvoiceService = new EInvoiceService();

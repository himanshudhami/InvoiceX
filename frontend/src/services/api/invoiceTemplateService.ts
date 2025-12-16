import { apiClient } from './client';
import { InvoiceTemplate, CreateInvoiceTemplateDto, UpdateInvoiceTemplateDto, PagedResponse, PaginationParams } from './types';

/**
 * Invoice Template API service following SRP - handles only template-related API calls
 */
export class InvoiceTemplateService {
  private readonly endpoint = 'invoicetemplates';

  async getAll(): Promise<InvoiceTemplate[]> {
    return apiClient.get<InvoiceTemplate[]>(this.endpoint);
  }

  async getById(id: string): Promise<InvoiceTemplate> {
    return apiClient.get<InvoiceTemplate>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: PaginationParams = {}): Promise<PagedResponse<InvoiceTemplate>> {
    return apiClient.getPaged<InvoiceTemplate>(this.endpoint, params);
  }

  async create(data: CreateInvoiceTemplateDto): Promise<InvoiceTemplate> {
    return apiClient.post<InvoiceTemplate, CreateInvoiceTemplateDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateInvoiceTemplateDto): Promise<void> {
    return apiClient.put<void, UpdateInvoiceTemplateDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const invoiceTemplateService = new InvoiceTemplateService();
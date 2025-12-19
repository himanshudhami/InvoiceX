import { apiClient } from './client';
import { Invoice, InvoiceItem, CreateInvoiceDto, UpdateInvoiceDto, PagedResponse, InvoicesFilterParams, Payment } from './types';

export interface RecordPaymentDto {
  amount: number;
  amountInInr?: number;
  paymentDate: string;
  paymentMethod: string;
  referenceNumber?: string;
  notes?: string;
}

/**
 * Invoice API service following SRP - handles only invoice-related API calls
 */
export class InvoiceService {
  private readonly endpoint = 'invoices';
  private readonly itemsEndpoint = 'invoiceitems';

  // Invoice operations
  async getAll(companyId?: string): Promise<Invoice[]> {
    const params = companyId ? { companyId } : undefined;
    // Note: Items are fetched only when viewing/editing a single invoice (getById)
    // to avoid N+1 query performance issues in list views
    return apiClient.get<Invoice[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<Invoice> {
    const invoice = await apiClient.get<Invoice>(`${this.endpoint}/${id}`);

    // Fetch items for this invoice
    try {
      const items = await this.getInvoiceItems(id);
      return { ...invoice, items };
    } catch (error) {
      console.error(`Failed to fetch items for invoice ${id}:`, error);
      return { ...invoice, items: [] };
    }
  }

  async getPaged(params: InvoicesFilterParams = {}): Promise<PagedResponse<Invoice>> {
    return apiClient.getPaged<Invoice>(this.endpoint, params);
  }

  async create(data: CreateInvoiceDto): Promise<Invoice> {
    return apiClient.post<Invoice, CreateInvoiceDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateInvoiceDto): Promise<void> {
    return apiClient.put<void, UpdateInvoiceDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async duplicate(id: string): Promise<Invoice> {
    return apiClient.post<Invoice>(`${this.endpoint}/${id}/duplicate`, {});
  }

  // Invoice items operations
  async getInvoiceItems(invoiceId: string): Promise<InvoiceItem[]> {
    // Backend returns { data: T[], currentPage, totalCount, ... } format
    // We need to map this to our expected { items: T[], pageNumber, ... } format
    const backendResponse = await apiClient.get<{
      data: InvoiceItem[];
      currentPage: number;
      totalCount: number;
      pageSize: number;
      totalPages: number;
      hasPrevious: boolean;
      hasNext: boolean;
    }>(`${this.itemsEndpoint}/paged?invoiceId=${invoiceId}&pageSize=100`);
    
    return backendResponse.data;
  }


  async createInvoiceItem(data: Omit<InvoiceItem, 'id' | 'createdAt' | 'updatedAt'>): Promise<InvoiceItem> {
    return apiClient.post<InvoiceItem>(this.itemsEndpoint, data);
  }

  async updateInvoiceItem(id: string, data: Partial<InvoiceItem>): Promise<void> {
    return apiClient.put<void>(`${this.itemsEndpoint}/${id}`, data);
  }

  async deleteInvoiceItem(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.itemsEndpoint}/${id}`);
  }

  // Payment operations
  async recordPayment(invoiceId: string, data: RecordPaymentDto): Promise<Payment> {
    return apiClient.post<Payment>(`${this.endpoint}/${invoiceId}/payments`, data);
  }

  async getPayments(invoiceId: string): Promise<Payment[]> {
    return apiClient.get<Payment[]>(`${this.endpoint}/${invoiceId}/payments`);
  }
}

// Singleton instance
export const invoiceService = new InvoiceService();
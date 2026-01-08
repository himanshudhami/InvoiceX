import { apiClient } from '../../client';
import type {
  VendorInvoice,
  CreateVendorInvoiceDto,
  UpdateVendorInvoiceDto,
  VendorInvoicesFilterParams,
  PagedResponse,
} from '../../types';

/**
 * Vendor Invoice API service - handles purchase invoices / bills
 */
export class VendorInvoiceService {
  private readonly endpoint = 'vendor-invoices';

  async getAll(companyId?: string): Promise<VendorInvoice[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<VendorInvoice[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<VendorInvoice> {
    return apiClient.get<VendorInvoice>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: VendorInvoicesFilterParams = {}): Promise<PagedResponse<VendorInvoice>> {
    return apiClient.getPaged<VendorInvoice>(this.endpoint, params);
  }

  async getByVendor(partyId: string, params: VendorInvoicesFilterParams = {}): Promise<PagedResponse<VendorInvoice>> {
    return apiClient.getPaged<VendorInvoice>(this.endpoint, { ...params, partyId });
  }

  async create(data: CreateVendorInvoiceDto): Promise<VendorInvoice> {
    return apiClient.post<VendorInvoice, CreateVendorInvoiceDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateVendorInvoiceDto): Promise<void> {
    return apiClient.put<void, UpdateVendorInvoiceDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  // Workflow actions
  async approve(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/approve`, {});
  }

  async reject(id: string, reason: string): Promise<void> {
    return apiClient.post<void, { reason: string }>(`${this.endpoint}/${id}/reject`, { reason });
  }

  async post(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/post`, {});
  }

  async cancel(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/cancel`, {});
  }

  // Get unpaid invoices for a vendor (for payment allocation)
  async getUnpaidByVendor(vendorId: string): Promise<VendorInvoice[]> {
    return apiClient.get<VendorInvoice[]>(`${this.endpoint}/unpaid`, { vendorId });
  }

  // Bulk operations
  async bulkApprove(ids: string[]): Promise<{ successCount: number; failedCount: number }> {
    return apiClient.post<{ successCount: number; failedCount: number }, { ids: string[] }>(
      `${this.endpoint}/bulk-approve`,
      { ids }
    );
  }

  async bulkPost(ids: string[]): Promise<{ successCount: number; failedCount: number }> {
    return apiClient.post<{ successCount: number; failedCount: number }, { ids: string[] }>(
      `${this.endpoint}/bulk-post`,
      { ids }
    );
  }
}

// Singleton instance
export const vendorInvoiceService = new VendorInvoiceService();

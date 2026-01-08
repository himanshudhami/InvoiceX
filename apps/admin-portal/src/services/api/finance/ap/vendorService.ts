import { apiClient } from '../../client';
import type {
  Vendor,
  CreateVendorDto,
  UpdateVendorDto,
  VendorsFilterParams,
  VendorOutstanding,
  VendorAgingSummary,
  VendorTdsSummary,
  VendorPaymentSummary,
  PagedResponse,
} from '../../types';

/**
 * Vendor API service - handles vendor (supplier/creditor) related API calls
 */
export class VendorService {
  private readonly endpoint = 'vendors';

  async getAll(companyId?: string): Promise<Vendor[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<Vendor[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<Vendor> {
    return apiClient.get<Vendor>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: VendorsFilterParams = {}): Promise<PagedResponse<Vendor>> {
    return apiClient.getPaged<Vendor>(this.endpoint, params);
  }

  async create(data: CreateVendorDto): Promise<Vendor> {
    return apiClient.post<Vendor, CreateVendorDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateVendorDto): Promise<void> {
    return apiClient.put<void, UpdateVendorDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  // Vendor Outstanding
  async getOutstanding(companyId: string): Promise<VendorOutstanding[]> {
    return apiClient.get<VendorOutstanding[]>(`${this.endpoint}/outstanding`, { companyId });
  }

  async getVendorOutstanding(vendorId: string): Promise<VendorOutstanding> {
    return apiClient.get<VendorOutstanding>(`${this.endpoint}/${vendorId}/outstanding`);
  }

  // Vendor Aging
  async getAgingSummary(companyId: string, asOfDate?: string): Promise<VendorAgingSummary[]> {
    const params: Record<string, string> = { companyId };
    if (asOfDate) params.asOfDate = asOfDate;
    return apiClient.get<VendorAgingSummary[]>(`${this.endpoint}/aging`, params);
  }

  // TDS Summary
  async getTdsSummary(companyId: string, financialYear?: string): Promise<VendorTdsSummary[]> {
    const params: Record<string, string> = { companyId };
    if (financialYear) params.financialYear = financialYear;
    return apiClient.get<VendorTdsSummary[]>(`${this.endpoint}/tds-summary`, params);
  }

  // Payment Summary
  async getPaymentSummary(companyId: string): Promise<VendorPaymentSummary> {
    return apiClient.get<VendorPaymentSummary>(`${this.endpoint}/payment-summary`, { companyId });
  }
}

// Singleton instance
export const vendorService = new VendorService();

import { apiClient } from '../../client';
import type {
  VendorPayment,
  CreateVendorPaymentDto,
  UpdateVendorPaymentDto,
  VendorPaymentsFilterParams,
  VendorPaymentAllocation,
  CreateVendorPaymentAllocationDto,
  PagedResponse,
} from '../../types';

/**
 * Vendor Payment API service - handles outgoing payments to vendors
 */
export class VendorPaymentService {
  private readonly endpoint = 'vendor-payments';

  async getAll(companyId?: string): Promise<VendorPayment[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<VendorPayment[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<VendorPayment> {
    return apiClient.get<VendorPayment>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: VendorPaymentsFilterParams = {}): Promise<PagedResponse<VendorPayment>> {
    return apiClient.getPaged<VendorPayment>(this.endpoint, params);
  }

  async getByVendor(vendorId: string, params: VendorPaymentsFilterParams = {}): Promise<PagedResponse<VendorPayment>> {
    return apiClient.getPaged<VendorPayment>(this.endpoint, { ...params, vendorId });
  }

  async create(data: CreateVendorPaymentDto): Promise<VendorPayment> {
    return apiClient.post<VendorPayment, CreateVendorPaymentDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateVendorPaymentDto): Promise<void> {
    return apiClient.put<void, UpdateVendorPaymentDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  // Workflow actions
  async approve(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/approve`, {});
  }

  async process(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/process`, {});
  }

  async cancel(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/cancel`, {});
  }

  // Allocations
  async getAllocations(paymentId: string): Promise<VendorPaymentAllocation[]> {
    return apiClient.get<VendorPaymentAllocation[]>(`${this.endpoint}/${paymentId}/allocations`);
  }

  async addAllocation(paymentId: string, data: CreateVendorPaymentAllocationDto): Promise<VendorPaymentAllocation> {
    return apiClient.post<VendorPaymentAllocation, CreateVendorPaymentAllocationDto>(
      `${this.endpoint}/${paymentId}/allocations`,
      data
    );
  }

  async removeAllocation(paymentId: string, allocationId: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${paymentId}/allocations/${allocationId}`);
  }

  async autoAllocate(paymentId: string): Promise<VendorPaymentAllocation[]> {
    return apiClient.post<VendorPaymentAllocation[], object>(
      `${this.endpoint}/${paymentId}/auto-allocate`,
      {}
    );
  }

  // TDS operations
  async markTdsDeposited(id: string, challanNumber: string, depositDate: string): Promise<void> {
    return apiClient.post<void, { challanNumber: string; depositDate: string }>(
      `${this.endpoint}/${id}/tds-deposited`,
      { challanNumber, depositDate }
    );
  }

  // Bulk operations
  async bulkApprove(ids: string[]): Promise<{ successCount: number; failedCount: number }> {
    return apiClient.post<{ successCount: number; failedCount: number }, { ids: string[] }>(
      `${this.endpoint}/bulk-approve`,
      { ids }
    );
  }

  async bulkProcess(ids: string[]): Promise<{ successCount: number; failedCount: number }> {
    return apiClient.post<{ successCount: number; failedCount: number }, { ids: string[] }>(
      `${this.endpoint}/bulk-process`,
      { ids }
    );
  }
}

// Singleton instance
export const vendorPaymentService = new VendorPaymentService();

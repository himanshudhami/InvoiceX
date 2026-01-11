import { apiClient } from '../../client';
import { PagedResponse } from '../../types';

// Types for payment allocation
export interface PaymentAllocation {
  id: string;
  companyId: string;
  paymentId: string;
  invoiceId?: string;
  allocatedAmount: number;
  currency: string;
  amountInInr?: number;
  exchangeRate: number;
  allocationDate: string;
  allocationType: AllocationTypeEnum;
  tdsAllocated: number;
  notes?: string;
  createdBy?: string;
  createdAt: string;
  updatedAt: string;
}

export type AllocationTypeEnum =
  | 'invoice_settlement'
  | 'advance_adjustment'
  | 'credit_note'
  | 'refund'
  | 'write_off';

export interface CreatePaymentAllocationDto {
  companyId: string;
  paymentId: string;
  invoiceId?: string;
  allocatedAmount: number;
  currency?: string;
  amountInInr?: number;
  exchangeRate?: number;
  allocationDate?: string;
  allocationType?: AllocationTypeEnum;
  tdsAllocated?: number;
  notes?: string;
  createdBy?: string;
}

export interface UpdatePaymentAllocationDto {
  invoiceId?: string;
  allocatedAmount: number;
  currency?: string;
  amountInInr?: number;
  exchangeRate?: number;
  allocationDate?: string;
  allocationType?: AllocationTypeEnum;
  tdsAllocated?: number;
  notes?: string;
}

export interface AllocationItem {
  invoiceId: string;
  amount: number;
  tdsAmount?: number;
  notes?: string;
}

export interface BulkAllocationDto {
  companyId: string;
  paymentId: string;
  allocations: AllocationItem[];
  createdBy?: string;
}

export interface InvoicePaymentStatus {
  invoiceId: string;
  invoiceNumber?: string;
  invoiceTotal: number;
  currency?: string;
  totalPaid: number;
  balanceDue: number;
  status: 'unpaid' | 'partial' | 'paid';
  paymentCount: number;
  lastPaymentDate?: string;
}

export interface AllocationDetail {
  id: string;
  invoiceId?: string;
  invoiceNumber?: string;
  allocatedAmount: number;
  tdsAllocated: number;
  allocationDate: string;
  allocationType: string;
  notes?: string;
}

export interface PaymentAllocationSummary {
  paymentId: string;
  paymentAmount: number;
  totalAllocated: number;
  unallocated: number;
  allocationCount: number;
  allocations: AllocationDetail[];
}

export interface PaymentAllocationFilterParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
  companyId?: string;
  paymentId?: string;
  invoiceId?: string;
}

export class PaymentAllocationService {
  private readonly endpoint = 'paymentallocations';

  // ==================== Basic CRUD ====================

  async getPaged(params?: PaymentAllocationFilterParams): Promise<PagedResponse<PaymentAllocation>> {
    return apiClient.getPaged<PaymentAllocation>(`${this.endpoint}/paged`, params);
  }

  async getAll(): Promise<PaymentAllocation[]> {
    return apiClient.get<PaymentAllocation[]>(this.endpoint);
  }

  async getById(id: string): Promise<PaymentAllocation> {
    return apiClient.get<PaymentAllocation>(`${this.endpoint}/${id}`);
  }

  async create(data: CreatePaymentAllocationDto): Promise<PaymentAllocation> {
    return apiClient.post<PaymentAllocation>(this.endpoint, data);
  }

  async update(id: string, data: UpdatePaymentAllocationDto): Promise<void> {
    return apiClient.put<void>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  // ==================== Query Operations ====================

  async getByPaymentId(paymentId: string): Promise<PaymentAllocation[]> {
    return apiClient.get<PaymentAllocation[]>(`${this.endpoint}/by-payment/${paymentId}`);
  }

  async getByInvoiceId(invoiceId: string): Promise<PaymentAllocation[]> {
    return apiClient.get<PaymentAllocation[]>(`${this.endpoint}/by-invoice/${invoiceId}`);
  }

  async getByCompanyId(companyId: string): Promise<PaymentAllocation[]> {
    return apiClient.get<PaymentAllocation[]>(`${this.endpoint}/by-company/${companyId}`);
  }

  // ==================== Allocation Operations ====================

  async allocatePaymentBulk(data: BulkAllocationDto): Promise<PaymentAllocation[]> {
    return apiClient.post<PaymentAllocation[]>(`${this.endpoint}/bulk`, data);
  }

  async getUnallocatedAmount(paymentId: string): Promise<number> {
    const result = await apiClient.get<{ unallocatedAmount: number }>(`${this.endpoint}/unallocated/${paymentId}`);
    return result.unallocatedAmount;
  }

  async getPaymentAllocationSummary(paymentId: string): Promise<PaymentAllocationSummary> {
    return apiClient.get<PaymentAllocationSummary>(`${this.endpoint}/summary/${paymentId}`);
  }

  // ==================== Invoice Status ====================

  async getInvoicePaymentStatus(invoiceId: string): Promise<InvoicePaymentStatus> {
    return apiClient.get<InvoicePaymentStatus>(`${this.endpoint}/invoice-status/${invoiceId}`);
  }

  async getCompanyInvoicePaymentStatus(companyId: string, financialYear?: string): Promise<InvoicePaymentStatus[]> {
    const params = financialYear ? `?financialYear=${financialYear}` : '';
    return apiClient.get<InvoicePaymentStatus[]>(`${this.endpoint}/company-invoice-status/${companyId}${params}`);
  }

  // ==================== Bulk Operations ====================

  async removeAllAllocations(paymentId: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/by-payment/${paymentId}`);
  }
}

export const paymentAllocationService = new PaymentAllocationService();

// Allocation type labels for UI
export const ALLOCATION_TYPE_LABELS: Record<AllocationTypeEnum, string> = {
  'invoice_settlement': 'Invoice Settlement',
  'advance_adjustment': 'Advance Adjustment',
  'credit_note': 'Credit Note',
  'refund': 'Refund',
  'write_off': 'Write Off',
};

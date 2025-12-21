import { apiClient } from '../../client';
import { Payment, IncomeSummary, TdsSummary, PaymentsFilterParams, PagedResponse } from '../../types';

// Payment types for classification
export type PaymentType = 'invoice_payment' | 'advance_received' | 'direct_income' | 'refund_received';
export type IncomeCategory = 'export_services' | 'domestic_services' | 'product_sale' | 'interest' | 'other';

// TDS sections for Indian compliance
export const TDS_SECTIONS = {
  '194J': { rate: 10, description: 'Professional/Technical Services' },
  '194C': { rate: 2, description: 'Contractor Payments' },
  '194H': { rate: 5, description: 'Commission/Brokerage' },
  '194O': { rate: 1, description: 'E-commerce Operator' },
} as const;

export interface CreatePaymentDto {
  // Linking
  invoiceId?: string;
  companyId?: string;
  customerId?: string;
  // Payment details
  paymentDate: string;
  amount: number;
  amountInInr?: number;
  currency?: string;
  paymentMethod?: string;
  referenceNumber?: string;
  notes?: string;
  description?: string;
  // Classification
  paymentType?: PaymentType;
  incomeCategory?: IncomeCategory;
  // TDS tracking
  tdsApplicable?: boolean;
  tdsSection?: string;
  tdsRate?: number;
  tdsAmount?: number;
  grossAmount?: number;
  // Financial year (auto-calculated if not provided)
  financialYear?: string;
}

export interface UpdatePaymentDto extends CreatePaymentDto {}

// Re-export PaymentsFilterParams for backward compatibility
export type { PaymentsFilterParams } from '../../types';
// Alias for backward compatibility
export type PaymentFilterParams = PaymentsFilterParams;

export class PaymentService {
  private readonly endpoint = 'payments';

  async getPaged(params?: PaymentsFilterParams): Promise<PagedResponse<Payment>> {
    return apiClient.getPaged<Payment>(this.endpoint, params);
  }

  async getAll(): Promise<Payment[]> {
    return apiClient.get<Payment[]>(this.endpoint);
  }

  async getById(id: string): Promise<Payment> {
    return apiClient.get<Payment>(`${this.endpoint}/${id}`);
  }

  async create(data: CreatePaymentDto): Promise<Payment> {
    return apiClient.post<Payment>(this.endpoint, data);
  }

  async update(id: string, data: UpdatePaymentDto): Promise<Payment> {
    return apiClient.put<Payment>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  // Get payments by invoice
  async getByInvoiceId(invoiceId: string): Promise<Payment[]> {
    return apiClient.get<Payment[]>(`${this.endpoint}/by-invoice/${invoiceId}`);
  }

  // Get payments by company
  async getByCompanyId(companyId: string): Promise<Payment[]> {
    return apiClient.get<Payment[]>(`${this.endpoint}/by-company/${companyId}`);
  }

  // Get payments by customer
  async getByCustomerId(customerId: string): Promise<Payment[]> {
    return apiClient.get<Payment[]>(`${this.endpoint}/by-customer/${customerId}`);
  }

  // Get payments by financial year
  async getByFinancialYear(financialYear: string, companyId?: string): Promise<Payment[]> {
    const params = companyId ? `?companyId=${companyId}` : '';
    return apiClient.get<Payment[]>(`${this.endpoint}/by-financial-year/${financialYear}${params}`);
  }

  // Get income summary for financial reports
  async getIncomeSummary(params?: {
    companyId?: string;
    financialYear?: string;
    year?: number;
    month?: number;
  }): Promise<IncomeSummary> {
    const queryParams = new URLSearchParams();
    if (params?.companyId) queryParams.append('companyId', params.companyId);
    if (params?.financialYear) queryParams.append('financialYear', params.financialYear);
    if (params?.year) queryParams.append('year', params.year.toString());
    if (params?.month) queryParams.append('month', params.month.toString());
    const query = queryParams.toString();
    return apiClient.get<IncomeSummary>(`${this.endpoint}/income-summary${query ? `?${query}` : ''}`);
  }

  // Get TDS summary for compliance reporting
  async getTdsSummary(financialYear: string, companyId?: string): Promise<TdsSummary[]> {
    const params = new URLSearchParams({ financialYear });
    if (companyId) params.append('companyId', companyId);
    return apiClient.get<TdsSummary[]>(`${this.endpoint}/tds-summary?${params.toString()}`);
  }
}

export const paymentService = new PaymentService();

// Utility function to calculate Indian financial year from a date
export function getFinancialYear(date: Date): string {
  const month = date.getMonth() + 1; // 1-12
  const year = date.getFullYear();
  // Indian FY runs Apr-Mar
  if (month >= 4) {
    return `${year}-${(year + 1).toString().slice(-2)}`;
  } else {
    return `${year - 1}-${year.toString().slice(-2)}`;
  }
}

// Utility function to calculate TDS amount
export function calculateTds(grossAmount: number, tdsRate: number): { tdsAmount: number; netAmount: number } {
  const tdsAmount = Math.round((grossAmount * tdsRate / 100) * 100) / 100;
  const netAmount = grossAmount - tdsAmount;
  return { tdsAmount, netAmount };
}





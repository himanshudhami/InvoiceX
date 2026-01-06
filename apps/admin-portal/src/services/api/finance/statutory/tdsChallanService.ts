import { apiClient } from '../../client';
import type {
  TdsChallan,
  TdsChallanSummary,
  CreateTdsChallanRequest,
  RecordTdsPaymentRequest,
  UpdateCinRequest,
  TdsChallanFilterParams,
  PagedResponse,
} from '../../types';

/**
 * TDS Challan 281 Service - Handles TDS deposit and challan management
 * Follows SRP: Single responsibility for TDS Challan API operations
 */
export class TdsChallanService {
  private readonly endpoint = 'statutory/tds-challan';

  /**
   * Get paginated list of TDS challans
   */
  async getPaged(companyId: string, params?: TdsChallanFilterParams): Promise<PagedResponse<TdsChallan>> {
    const queryParams = {
      pageNumber: params?.pageNumber || 1,
      pageSize: params?.pageSize || 10,
      ...(params?.financialYear && { financialYear: params.financialYear }),
      ...(params?.status && { status: params.status }),
      ...(params?.searchTerm && { searchTerm: params.searchTerm }),
    };
    return apiClient.get<PagedResponse<TdsChallan>>(`${this.endpoint}/${companyId}/paged`, queryParams);
  }

  /**
   * Get TDS challan by ID
   */
  async getById(id: string): Promise<TdsChallan> {
    return apiClient.get<TdsChallan>(`${this.endpoint}/detail/${id}`);
  }

  /**
   * Generate TDS challan for a period
   */
  async generate(
    companyId: string,
    periodYear: number,
    periodMonth: number,
    challanType: string = 'salary'
  ): Promise<TdsChallan> {
    return apiClient.get<TdsChallan>(
      `${this.endpoint}/${companyId}/generate/${periodYear}/${periodMonth}`,
      { challanType }
    );
  }

  /**
   * Preview TDS challan with interest calculation
   */
  async preview(
    companyId: string,
    periodYear: number,
    periodMonth: number,
    challanType: string = 'salary',
    paymentDate?: string
  ): Promise<any> {
    return apiClient.get<any>(
      `${this.endpoint}/${companyId}/preview/${periodYear}/${periodMonth}`,
      { challanType, paymentDate }
    );
  }

  /**
   * Create TDS challan payment record
   */
  async create(request: CreateTdsChallanRequest): Promise<TdsChallan> {
    return apiClient.post<TdsChallan>(`${this.endpoint}/create`, request);
  }

  /**
   * Record TDS deposit with bank/BSR details
   */
  async recordDeposit(paymentId: string, request: RecordTdsPaymentRequest): Promise<TdsChallan> {
    return apiClient.post<TdsChallan>(`${this.endpoint}/${paymentId}/deposit`, request);
  }

  /**
   * Update CIN (Challan Identification Number) after bank confirmation.
   * CIN is required for Form 24Q/26Q quarterly TDS return filing.
   * CIN format: BSR Code (7) + Deposit Date (DDMMYYYY) + Serial Number (5)
   */
  async updateCin(paymentId: string, request: UpdateCinRequest): Promise<TdsChallan> {
    return apiClient.post<TdsChallan>(`${this.endpoint}/${paymentId}/update-cin`, request);
  }

  /**
   * Get pending TDS challans
   */
  async getPending(companyId: string, financialYear?: string): Promise<TdsChallan[]> {
    const params = financialYear ? { financialYear } : {};
    return apiClient.get<TdsChallan[]>(`${this.endpoint}/${companyId}/pending`, params);
  }

  /**
   * Get overdue TDS challans
   */
  async getOverdue(companyId: string): Promise<TdsChallan[]> {
    return apiClient.get<TdsChallan[]>(`${this.endpoint}/${companyId}/overdue`);
  }

  /**
   * Get paid TDS challans for a financial year
   */
  async getPaid(companyId: string, financialYear: string, quarter?: string): Promise<TdsChallan[]> {
    const params = quarter ? { quarter } : {};
    return apiClient.get<TdsChallan[]>(`${this.endpoint}/${companyId}/paid/${financialYear}`, params);
  }

  /**
   * Get TDS challan summary for a financial year
   */
  async getSummary(companyId: string, financialYear: string): Promise<TdsChallanSummary> {
    return apiClient.get<TdsChallanSummary>(`${this.endpoint}/${companyId}/summary/${financialYear}`);
  }

  /**
   * Calculate interest on late TDS payment
   */
  async calculateInterest(
    tdsAmount: number,
    dueDate: string,
    paymentDate: string
  ): Promise<{ interestAmount: number; totalPayable: number; daysLate: number; monthsLate: number }> {
    return apiClient.get<{ interestAmount: number; totalPayable: number; daysLate: number; monthsLate: number }>(
      `${this.endpoint}/calculate-interest`,
      { tdsAmount, dueDate, paymentDate }
    );
  }

  /**
   * Get TDS due date for a period
   */
  async getDueDate(
    periodYear: number,
    periodMonth: number
  ): Promise<{ dueDate: string; isOverdue: boolean; daysUntilDue: number; daysOverdue: number }> {
    return apiClient.get<{ dueDate: string; isOverdue: boolean; daysUntilDue: number; daysOverdue: number }>(
      `${this.endpoint}/due-date/${periodYear}/${periodMonth}`
    );
  }

  /**
   * Reconcile TDS deducted vs deposited
   */
  async reconcile(companyId: string, financialYear: string, quarter?: string): Promise<any> {
    const params = quarter ? { quarter } : {};
    return apiClient.get<any>(`${this.endpoint}/${companyId}/reconcile/${financialYear}`, params);
  }
}

export const tdsChallanService = new TdsChallanService();

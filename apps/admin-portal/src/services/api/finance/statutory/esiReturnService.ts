import { apiClient } from '../../client';
import type {
  EsiReturnData,
  EsiReturnPreview,
  EsiReturnFileResult,
  EsiReturnSummary,
  CreateEsiReturnRequest,
  RecordEsiPaymentRequest,
  EsiReturnFilterParams,
  PagedResponse,
} from '../../types';

/**
 * ESI Return Service - Handles ESIC monthly return operations
 * Follows SRP: Single responsibility for ESI Return API operations
 */
export class EsiReturnService {
  private readonly endpoint = 'statutory/esi-return';

  /**
   * Generate ESI return data for a period
   */
  async generate(
    companyId: string,
    periodYear: number,
    periodMonth: number
  ): Promise<EsiReturnData> {
    return apiClient.get<EsiReturnData>(
      `${this.endpoint}/${companyId}/generate/${periodYear}/${periodMonth}`
    );
  }

  /**
   * Generate ESI return from a specific payroll run
   */
  async generateFromPayrollRun(payrollRunId: string): Promise<EsiReturnData> {
    return apiClient.get<EsiReturnData>(`${this.endpoint}/payroll-run/${payrollRunId}`);
  }

  /**
   * Preview ESI return with interest calculation
   */
  async preview(
    companyId: string,
    periodYear: number,
    periodMonth: number
  ): Promise<EsiReturnPreview> {
    return apiClient.get<EsiReturnPreview>(
      `${this.endpoint}/${companyId}/preview/${periodYear}/${periodMonth}`
    );
  }

  /**
   * Generate ESI return file for ESIC upload
   */
  async generateFile(
    companyId: string,
    periodYear: number,
    periodMonth: number
  ): Promise<EsiReturnFileResult> {
    return apiClient.get<EsiReturnFileResult>(
      `${this.endpoint}/${companyId}/file/${periodYear}/${periodMonth}`
    );
  }

  /**
   * Create statutory payment record for ESI return
   */
  async createPayment(request: CreateEsiReturnRequest): Promise<any> {
    return apiClient.post<any>(`${this.endpoint}/create`, request);
  }

  /**
   * Record ESI payment
   */
  async recordPayment(paymentId: string, request: RecordEsiPaymentRequest): Promise<any> {
    return apiClient.post<any>(`${this.endpoint}/${paymentId}/payment`, request);
  }

  /**
   * Update challan number after ESIC filing
   */
  async updateChallanNumber(paymentId: string, challanNumber: string): Promise<any> {
    return apiClient.post<any>(
      `${this.endpoint}/${paymentId}/challan?challanNumber=${encodeURIComponent(challanNumber)}`,
      {}
    );
  }

  /**
   * Get pending ESI returns
   */
  async getPending(companyId: string, financialYear?: string): Promise<any[]> {
    const params = financialYear ? { financialYear } : {};
    return apiClient.get<any[]>(`${this.endpoint}/${companyId}/pending`, params);
  }

  /**
   * Get filed ESI returns for a financial year
   */
  async getFiled(companyId: string, financialYear: string): Promise<any[]> {
    return apiClient.get<any[]>(`${this.endpoint}/${companyId}/filed/${financialYear}`);
  }

  /**
   * Get ESI return summary for a financial year
   */
  async getSummary(companyId: string, financialYear: string): Promise<EsiReturnSummary> {
    return apiClient.get<EsiReturnSummary>(
      `${this.endpoint}/${companyId}/summary/${financialYear}`
    );
  }

  /**
   * Get ESI return detail by ID
   */
  async getById(paymentId: string): Promise<any> {
    return apiClient.get<any>(`${this.endpoint}/detail/${paymentId}`);
  }

  /**
   * Calculate interest on late ESI payment (12% per annum)
   */
  async calculateInterest(
    esiAmount: number,
    dueDate: string,
    paymentDate: string
  ): Promise<{ interestAmount: number; totalPayable: number; daysLate: number }> {
    return apiClient.get<{ interestAmount: number; totalPayable: number; daysLate: number }>(
      `${this.endpoint}/calculate-interest`,
      { esiAmount, dueDate, paymentDate }
    );
  }

  /**
   * Get ESI due date for a period (15th of following month)
   */
  async getDueDate(
    periodYear: number,
    periodMonth: number
  ): Promise<{
    dueDate: string;
    isOverdue: boolean;
    daysUntilDue: number;
    contributionPeriod: string;
  }> {
    return apiClient.get<{
      dueDate: string;
      isOverdue: boolean;
      daysUntilDue: number;
      contributionPeriod: string;
    }>(`${this.endpoint}/due-date/${periodYear}/${periodMonth}`);
  }

  /**
   * Reconcile ESI deducted vs deposited
   */
  async reconcile(companyId: string, financialYear: string): Promise<any> {
    return apiClient.get<any>(`${this.endpoint}/${companyId}/reconcile/${financialYear}`);
  }

  /**
   * Get download URL for ESI return file
   */
  getDownloadUrl(companyId: string, periodYear: number, periodMonth: number): string {
    return `${this.endpoint}/${companyId}/download/${periodYear}/${periodMonth}`;
  }
}

export const esiReturnService = new EsiReturnService();

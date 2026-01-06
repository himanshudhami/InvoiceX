import { apiClient } from '../../client';
import type {
  PfEcrData,
  PfEcrPreview,
  PfEcrFileResult,
  PfEcrSummary,
  CreatePfEcrPaymentRequest,
  RecordPfPaymentRequest,
  PfEcrFilterParams,
  PagedResponse,
} from '../../types';

/**
 * PF ECR Service - Handles EPFO Electronic Challan cum Return operations
 * Follows SRP: Single responsibility for PF ECR API operations
 */
export class PfEcrService {
  private readonly endpoint = 'statutory/pf-ecr';

  /**
   * Generate PF ECR data for a period
   */
  async generate(
    companyId: string,
    periodYear: number,
    periodMonth: number
  ): Promise<PfEcrData> {
    return apiClient.get<PfEcrData>(
      `${this.endpoint}/${companyId}/generate/${periodYear}/${periodMonth}`
    );
  }

  /**
   * Generate PF ECR from a specific payroll run
   */
  async generateFromPayrollRun(payrollRunId: string): Promise<PfEcrData> {
    return apiClient.get<PfEcrData>(`${this.endpoint}/payroll-run/${payrollRunId}`);
  }

  /**
   * Preview PF ECR with interest/damages calculation
   */
  async preview(
    companyId: string,
    periodYear: number,
    periodMonth: number
  ): Promise<PfEcrPreview> {
    return apiClient.get<PfEcrPreview>(
      `${this.endpoint}/${companyId}/preview/${periodYear}/${periodMonth}`
    );
  }

  /**
   * Generate PF ECR text file for EPFO upload
   */
  async generateFile(
    companyId: string,
    periodYear: number,
    periodMonth: number
  ): Promise<PfEcrFileResult> {
    return apiClient.get<PfEcrFileResult>(
      `${this.endpoint}/${companyId}/file/${periodYear}/${periodMonth}`
    );
  }

  /**
   * Create statutory payment record for PF ECR
   */
  async createPayment(request: CreatePfEcrPaymentRequest): Promise<any> {
    return apiClient.post<any>(`${this.endpoint}/create`, request);
  }

  /**
   * Record PF payment
   */
  async recordPayment(paymentId: string, request: RecordPfPaymentRequest): Promise<any> {
    return apiClient.post<any>(`${this.endpoint}/${paymentId}/payment`, request);
  }

  /**
   * Update TRRN (Transaction Reference Return Number)
   */
  async updateTrrn(paymentId: string, trrn: string): Promise<any> {
    return apiClient.post<any>(
      `${this.endpoint}/${paymentId}/trrn?trrn=${encodeURIComponent(trrn)}`,
      {}
    );
  }

  /**
   * Get pending PF ECRs
   */
  async getPending(companyId: string, financialYear?: string): Promise<any[]> {
    const params = financialYear ? { financialYear } : {};
    return apiClient.get<any[]>(`${this.endpoint}/${companyId}/pending`, params);
  }

  /**
   * Get filed PF ECRs for a financial year
   */
  async getFiled(companyId: string, financialYear: string): Promise<any[]> {
    return apiClient.get<any[]>(`${this.endpoint}/${companyId}/filed/${financialYear}`);
  }

  /**
   * Get PF ECR summary for a financial year
   */
  async getSummary(companyId: string, financialYear: string): Promise<PfEcrSummary> {
    return apiClient.get<PfEcrSummary>(
      `${this.endpoint}/${companyId}/summary/${financialYear}`
    );
  }

  /**
   * Get PF ECR detail by ID
   */
  async getById(paymentId: string): Promise<any> {
    return apiClient.get<any>(`${this.endpoint}/detail/${paymentId}`);
  }

  /**
   * Calculate interest on late PF payment (1% per month)
   */
  async calculateInterest(
    pfAmount: number,
    dueDate: string,
    paymentDate: string
  ): Promise<{ interestAmount: number; damagesAmount: number; totalPayable: number; monthsLate: number }> {
    return apiClient.get<{ interestAmount: number; damagesAmount: number; totalPayable: number; monthsLate: number }>(
      `${this.endpoint}/calculate-interest`,
      { pfAmount, dueDate, paymentDate }
    );
  }

  /**
   * Get PF due date for a period (15th of following month)
   */
  async getDueDate(
    periodYear: number,
    periodMonth: number
  ): Promise<{ dueDate: string; isOverdue: boolean; daysUntilDue: number }> {
    return apiClient.get<{ dueDate: string; isOverdue: boolean; daysUntilDue: number }>(
      `${this.endpoint}/due-date/${periodYear}/${periodMonth}`
    );
  }

  /**
   * Reconcile PF deducted vs deposited
   */
  async reconcile(companyId: string, financialYear: string): Promise<any> {
    return apiClient.get<any>(`${this.endpoint}/${companyId}/reconcile/${financialYear}`);
  }

  /**
   * Get download URL for ECR file
   */
  getDownloadUrl(companyId: string, periodYear: number, periodMonth: number): string {
    return `${this.endpoint}/${companyId}/download/${periodYear}/${periodMonth}`;
  }
}

export const pfEcrService = new PfEcrService();

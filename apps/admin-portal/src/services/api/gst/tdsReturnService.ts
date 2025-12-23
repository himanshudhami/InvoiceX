import { apiClient } from '../client';
import {
  Form26QData,
  Form26QSummary,
  Form24QData,
  Form24QSummary,
  Form24QAnnexureII,
  TdsReturnValidationResult,
  TdsReturnDueDate,
  PendingTdsReturn,
  MarkReturnFiledRequest,
  TdsReturnFilingHistory,
  ChallanDetail,
  ChallanReconciliationResult,
  CombinedTdsSummary,
} from '../types';

/**
 * TDS Returns Service
 *
 * Handles TDS return preparation:
 * - Form 26Q (Non-salary TDS) - 194A, 194C, 194H, 194I, 194J, etc.
 * - Form 24Q (Salary TDS) - Section 192
 * - Challan reconciliation
 * - Due dates and filing status
 */
export class TdsReturnService {
  private readonly endpoint = 'tax/tdsreturns';

  // ==================== Form 26Q (Non-Salary TDS) ====================

  /**
   * Generate Form 26Q data for non-salary TDS return
   */
  async getForm26Q(companyId: string, financialYear: string, quarter: string): Promise<Form26QData> {
    return apiClient.get<Form26QData>(
      `${this.endpoint}/26q/${companyId}/${financialYear}/${quarter}`
    );
  }

  /**
   * Validate Form 26Q data before filing
   */
  async validateForm26Q(
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<TdsReturnValidationResult> {
    return apiClient.get<TdsReturnValidationResult>(
      `${this.endpoint}/26q/${companyId}/${financialYear}/${quarter}/validate`
    );
  }

  /**
   * Get Form 26Q summary with section-wise and month-wise breakdown
   */
  async getForm26QSummary(
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<Form26QSummary> {
    return apiClient.get<Form26QSummary>(
      `${this.endpoint}/26q/${companyId}/${financialYear}/${quarter}/summary`
    );
  }

  // ==================== Form 24Q (Salary TDS) ====================

  /**
   * Generate Form 24Q data for salary TDS return
   */
  async getForm24Q(companyId: string, financialYear: string, quarter: string): Promise<Form24QData> {
    return apiClient.get<Form24QData>(
      `${this.endpoint}/24q/${companyId}/${financialYear}/${quarter}`
    );
  }

  /**
   * Validate Form 24Q data before filing
   */
  async validateForm24Q(
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<TdsReturnValidationResult> {
    return apiClient.get<TdsReturnValidationResult>(
      `${this.endpoint}/24q/${companyId}/${financialYear}/${quarter}/validate`
    );
  }

  /**
   * Get Form 24Q summary with employee count and month-wise breakdown
   */
  async getForm24QSummary(
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<Form24QSummary> {
    return apiClient.get<Form24QSummary>(
      `${this.endpoint}/24q/${companyId}/${financialYear}/${quarter}/summary`
    );
  }

  /**
   * Generate Form 24Q Annexure II (Q4 only - annual salary details)
   */
  async getForm24QAnnexureII(companyId: string, financialYear: string): Promise<Form24QAnnexureII> {
    return apiClient.get<Form24QAnnexureII>(
      `${this.endpoint}/24q/${companyId}/${financialYear}/annexure-ii`
    );
  }

  // ==================== Challan Reconciliation ====================

  /**
   * Get challan details for TDS deposits in a quarter
   */
  async getChallans(
    companyId: string,
    financialYear: string,
    quarter: string,
    formType?: string
  ): Promise<ChallanDetail[]> {
    const params = formType ? `?formType=${formType}` : '';
    return apiClient.get<ChallanDetail[]>(
      `${this.endpoint}/challans/${companyId}/${financialYear}/${quarter}${params}`
    );
  }

  /**
   * Reconcile TDS deducted with challans deposited
   */
  async reconcileChallans(
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<ChallanReconciliationResult> {
    return apiClient.get<ChallanReconciliationResult>(
      `${this.endpoint}/challans/${companyId}/${financialYear}/${quarter}/reconcile`
    );
  }

  // ==================== Due Dates & Pending Returns ====================

  /**
   * Get TDS return due dates for a financial year
   */
  async getDueDates(financialYear: string): Promise<TdsReturnDueDate[]> {
    return apiClient.get<TdsReturnDueDate[]>(`${this.endpoint}/due-dates/${financialYear}`);
  }

  /**
   * Get pending/overdue TDS returns for a company
   */
  async getPendingReturns(companyId: string): Promise<PendingTdsReturn[]> {
    return apiClient.get<PendingTdsReturn[]>(`${this.endpoint}/pending/${companyId}`);
  }

  // ==================== Filing Status ====================

  /**
   * Mark a TDS return as filed
   */
  async markReturnFiled(request: MarkReturnFiledRequest): Promise<void> {
    return apiClient.post<void>(`${this.endpoint}/mark-filed`, request);
  }

  /**
   * Get filing history for a company
   */
  async getFilingHistory(companyId: string, financialYear?: string): Promise<TdsReturnFilingHistory[]> {
    const params = financialYear ? `?financialYear=${financialYear}` : '';
    return apiClient.get<TdsReturnFilingHistory[]>(
      `${this.endpoint}/filing-history/${companyId}${params}`
    );
  }

  // ==================== Combined Reports ====================

  /**
   * Get combined TDS summary for both salary and non-salary
   */
  async getCombinedSummary(
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<CombinedTdsSummary> {
    return apiClient.get<CombinedTdsSummary>(
      `${this.endpoint}/combined-summary/${companyId}/${financialYear}/${quarter}`
    );
  }
}

export const tdsReturnService = new TdsReturnService();

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

  // ==================== FVU File Download ====================

  /**
   * Download Form 26Q FVU text file for non-salary TDS
   */
  async downloadForm26Q(
    companyId: string,
    financialYear: string,
    quarter: string,
    isCorrection: boolean = false
  ): Promise<Blob> {
    const params = isCorrection ? '?isCorrection=true' : '';
    const response = await fetch(
      `${import.meta.env.VITE_API_URL}/${this.endpoint}/26q/${companyId}/${financialYear}/${quarter}/download${params}`,
      {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      }
    );
    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || 'Failed to download Form 26Q');
    }
    return response.blob();
  }

  /**
   * Download Form 24Q FVU text file for salary TDS
   */
  async downloadForm24Q(
    companyId: string,
    financialYear: string,
    quarter: string,
    isCorrection: boolean = false
  ): Promise<Blob> {
    const params = isCorrection ? '?isCorrection=true' : '';
    const response = await fetch(
      `${import.meta.env.VITE_API_URL}/${this.endpoint}/24q/${companyId}/${financialYear}/${quarter}/download${params}`,
      {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      }
    );
    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || 'Failed to download Form 24Q');
    }
    return response.blob();
  }

  /**
   * Validate TDS return data before FVU file generation
   */
  async validateForFvu(
    formType: string,
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<FvuValidationResult> {
    return apiClient.get<FvuValidationResult>(
      `${this.endpoint}/${formType}/${companyId}/${financialYear}/${quarter}/validate-fvu`
    );
  }
}

// FVU Validation Result type
export interface FvuValidationResult {
  isValid: boolean;
  canGenerate: boolean;
  formType: string;
  financialYear: string;
  quarter: string;
  errors: FvuValidationError[];
  warnings: FvuValidationWarning[];
  summary: FvuValidationSummary;
}

export interface FvuValidationError {
  code: string;
  message: string;
  field?: string;
  recordIdentifier?: string;
  recordType?: string;
  suggestedFix?: string;
}

export interface FvuValidationWarning {
  code: string;
  message: string;
  field?: string;
  recordIdentifier?: string;
  impact?: string;
}

export interface FvuValidationSummary {
  totalRecords: number;
  validRecords: number;
  invalidRecords: number;
  recordsWithWarnings: number;
  totalTdsAmount: number;
  totalGrossAmount: number;
  uniqueDeductees: number;
  challanCount: number;
  hasValidTan: boolean;
  invalidPanCount: number;
  challansReconciled: boolean;
}

export const tdsReturnService = new TdsReturnService();

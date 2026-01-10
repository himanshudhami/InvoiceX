import { apiClient } from '../../client';
import type {
  AdvanceTaxAssessment,
  AdvanceTaxSchedule,
  AdvanceTaxPayment,
  AdvanceTaxScenario,
  AdvanceTaxTracker,
  InterestCalculation,
  TaxComputation,
  TdsTcsPreview,
  AdvanceTaxRevision,
  RevisionStatus,
  MatComputation,
  MatCreditSummary,
  MatCreditRegister,
  MatCreditUtilization,
  CreateAdvanceTaxAssessmentRequest,
  UpdateAdvanceTaxAssessmentRequest,
  RecordAdvanceTaxPaymentRequest,
  RunScenarioRequest,
  RefreshYtdRequest,
  CreateRevisionRequest,
  YtdFinancials,
} from '../../types';

/**
 * Advance Tax API Service (Section 207 - Corporate)
 *
 * Handles advance tax assessment, quarterly payment schedules,
 * interest calculations (234B/234C), and what-if scenario analysis
 */
export class AdvanceTaxService {
  private readonly endpoint = 'tax/advance-tax';

  // ==================== Assessment Operations ====================

  /**
   * Compute and create advance tax assessment
   */
  async computeAssessment(request: CreateAdvanceTaxAssessmentRequest): Promise<AdvanceTaxAssessment> {
    return apiClient.post<AdvanceTaxAssessment>(`${this.endpoint}/compute`, request);
  }

  /**
   * Get assessment by ID
   */
  async getAssessmentById(id: string): Promise<AdvanceTaxAssessment> {
    return apiClient.get<AdvanceTaxAssessment>(`${this.endpoint}/assessment/${id}`);
  }

  /**
   * Get assessment for company and financial year
   */
  async getAssessment(companyId: string, financialYear: string): Promise<AdvanceTaxAssessment> {
    return apiClient.get<AdvanceTaxAssessment>(
      `${this.endpoint}/${companyId}/${financialYear}`
    );
  }

  /**
   * Get all assessments for a company
   */
  async getAssessmentsByCompany(companyId: string): Promise<AdvanceTaxAssessment[]> {
    return apiClient.get<AdvanceTaxAssessment[]>(`${this.endpoint}/company/${companyId}`);
  }

  /**
   * Update assessment projections
   */
  async updateAssessment(
    id: string,
    request: UpdateAdvanceTaxAssessmentRequest
  ): Promise<AdvanceTaxAssessment> {
    return apiClient.put<AdvanceTaxAssessment>(`${this.endpoint}/assessment/${id}`, request);
  }

  /**
   * Activate assessment (move from draft to active)
   */
  async activateAssessment(id: string): Promise<AdvanceTaxAssessment> {
    return apiClient.post<AdvanceTaxAssessment>(`${this.endpoint}/assessment/${id}/activate`, {});
  }

  /**
   * Finalize assessment (after FY end)
   */
  async finalizeAssessment(id: string): Promise<AdvanceTaxAssessment> {
    return apiClient.post<AdvanceTaxAssessment>(`${this.endpoint}/assessment/${id}/finalize`, {});
  }

  /**
   * Delete assessment (draft only)
   */
  async deleteAssessment(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/assessment/${id}`);
  }

  /**
   * Refresh YTD actuals from ledger
   */
  async refreshYtd(request: RefreshYtdRequest): Promise<AdvanceTaxAssessment> {
    return apiClient.post<AdvanceTaxAssessment>(`${this.endpoint}/assessment/refresh-ytd`, request);
  }

  /**
   * Preview YTD financials with trend-based projections
   */
  async getYtdFinancialsPreview(companyId: string, financialYear: string): Promise<YtdFinancials> {
    return apiClient.get<YtdFinancials>(
      `${this.endpoint}/ytd-preview/${companyId}/${financialYear}`
    );
  }

  /**
   * Refresh TDS receivable and TCS credit from modules
   */
  async refreshTdsTcs(assessmentId: string): Promise<AdvanceTaxAssessment> {
    return apiClient.post<AdvanceTaxAssessment>(
      `${this.endpoint}/assessment/${assessmentId}/refresh-tds-tcs`,
      {}
    );
  }

  /**
   * Preview TDS/TCS values from modules (without saving)
   */
  async getTdsTcsPreview(companyId: string, financialYear: string): Promise<TdsTcsPreview> {
    return apiClient.get<TdsTcsPreview>(
      `${this.endpoint}/tds-tcs-preview/${companyId}/${financialYear}`
    );
  }

  // ==================== Schedule Operations ====================

  /**
   * Get payment schedule for an assessment
   */
  async getPaymentSchedule(assessmentId: string): Promise<AdvanceTaxSchedule[]> {
    return apiClient.get<AdvanceTaxSchedule[]>(`${this.endpoint}/schedule/${assessmentId}`);
  }

  /**
   * Recalculate schedules after changes
   */
  async recalculateSchedules(assessmentId: string): Promise<AdvanceTaxSchedule[]> {
    return apiClient.post<AdvanceTaxSchedule[]>(
      `${this.endpoint}/schedule/${assessmentId}/recalculate`,
      {}
    );
  }

  // ==================== Payment Operations ====================

  /**
   * Record advance tax payment
   */
  async recordPayment(request: RecordAdvanceTaxPaymentRequest): Promise<AdvanceTaxPayment> {
    return apiClient.post<AdvanceTaxPayment>(`${this.endpoint}/payment`, request);
  }

  /**
   * Get payments for an assessment
   */
  async getPayments(assessmentId: string): Promise<AdvanceTaxPayment[]> {
    return apiClient.get<AdvanceTaxPayment[]>(`${this.endpoint}/payments/${assessmentId}`);
  }

  /**
   * Delete a payment
   */
  async deletePayment(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/payment/${id}`);
  }

  // ==================== Interest Calculations ====================

  /**
   * Get interest liability breakdown
   */
  async getInterestBreakdown(assessmentId: string): Promise<InterestCalculation> {
    return apiClient.get<InterestCalculation>(`${this.endpoint}/interest/${assessmentId}`);
  }

  // ==================== Scenario Analysis ====================

  /**
   * Run a what-if scenario
   */
  async runScenario(request: RunScenarioRequest): Promise<AdvanceTaxScenario> {
    return apiClient.post<AdvanceTaxScenario>(`${this.endpoint}/scenario`, request);
  }

  /**
   * Get scenarios for an assessment
   */
  async getScenarios(assessmentId: string): Promise<AdvanceTaxScenario[]> {
    return apiClient.get<AdvanceTaxScenario[]>(`${this.endpoint}/scenarios/${assessmentId}`);
  }

  /**
   * Delete a scenario
   */
  async deleteScenario(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/scenario/${id}`);
  }

  // ==================== Dashboard & Reports ====================

  /**
   * Get advance tax tracker/dashboard
   */
  async getTracker(companyId: string, financialYear: string): Promise<AdvanceTaxTracker> {
    return apiClient.get<AdvanceTaxTracker>(
      `${this.endpoint}/tracker/${companyId}/${financialYear}`
    );
  }

  /**
   * Get tax computation breakdown
   */
  async getTaxComputation(assessmentId: string): Promise<TaxComputation> {
    return apiClient.get<TaxComputation>(`${this.endpoint}/computation/${assessmentId}`);
  }

  /**
   * Get assessments with pending payments
   */
  async getPendingPayments(companyId?: string): Promise<AdvanceTaxAssessment[]> {
    const query = companyId ? `?companyId=${companyId}` : '';
    return apiClient.get<AdvanceTaxAssessment[]>(`${this.endpoint}/pending${query}`);
  }

  // ==================== Revision Operations ====================

  /**
   * Create a revision with updated projections
   */
  async createRevision(request: CreateRevisionRequest): Promise<AdvanceTaxRevision> {
    return apiClient.post<AdvanceTaxRevision>(`${this.endpoint}/revision`, request);
  }

  /**
   * Get all revisions for an assessment
   */
  async getRevisions(assessmentId: string): Promise<AdvanceTaxRevision[]> {
    return apiClient.get<AdvanceTaxRevision[]>(`${this.endpoint}/revisions/${assessmentId}`);
  }

  /**
   * Get revision status (for dashboard prompt)
   */
  async getRevisionStatus(assessmentId: string): Promise<RevisionStatus> {
    return apiClient.get<RevisionStatus>(`${this.endpoint}/revision-status/${assessmentId}`);
  }

  // ==================== MAT Credit Operations ====================

  /**
   * Get MAT computation for an assessment
   */
  async getMatComputation(assessmentId: string): Promise<MatComputation> {
    return apiClient.get<MatComputation>(`${this.endpoint}/mat-computation/${assessmentId}`);
  }

  /**
   * Get available MAT credits summary for a company
   */
  async getMatCreditSummary(companyId: string, financialYear: string): Promise<MatCreditSummary> {
    return apiClient.get<MatCreditSummary>(
      `${this.endpoint}/mat-credit-summary/${companyId}/${financialYear}`
    );
  }

  /**
   * Get all MAT credit entries for a company
   */
  async getMatCredits(companyId: string): Promise<MatCreditRegister[]> {
    return apiClient.get<MatCreditRegister[]>(`${this.endpoint}/mat-credits/${companyId}`);
  }

  /**
   * Get MAT credit utilization history
   */
  async getMatCreditUtilizations(matCreditId: string): Promise<MatCreditUtilization[]> {
    return apiClient.get<MatCreditUtilization[]>(
      `${this.endpoint}/mat-credit-utilizations/${matCreditId}`
    );
  }
}

// Singleton instance
export const advanceTaxService = new AdvanceTaxService();

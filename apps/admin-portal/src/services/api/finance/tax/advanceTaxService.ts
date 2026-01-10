import { apiClient } from '../../client';
import type {
  AdvanceTaxAssessment,
  AdvanceTaxSchedule,
  AdvanceTaxPayment,
  AdvanceTaxScenario,
  AdvanceTaxTracker,
  InterestCalculation,
  TaxComputation,
  CreateAdvanceTaxAssessmentRequest,
  UpdateAdvanceTaxAssessmentRequest,
  RecordAdvanceTaxPaymentRequest,
  RunScenarioRequest,
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
}

// Singleton instance
export const advanceTaxService = new AdvanceTaxService();

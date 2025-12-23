import { apiClient } from '../client';
import {
  ItcBlockedCategory,
  ItcBlockedCheckRequest,
  ItcBlockedCheckResult,
  ItcBlockedRequest,
  ItcBlockedSummary,
  ItcAvailabilityReport,
  CreditNoteGstRequest,
  DebitNoteGstRequest,
  ItcReversalCalculationRequest,
  ItcReversalCalculation,
  ItcReversalRequest,
  UtgstRequest,
  GstTdsRequest,
  GstTcsRequest,
  GstPostingResult,
} from '../types';

/**
 * GST Posting Service
 *
 * Handles GST compliance operations:
 * - ITC Blocked (Section 17(5))
 * - Credit/Debit Note GST adjustments
 * - ITC Reversal (Rule 42/43)
 * - UTGST posting
 * - GST TDS/TCS (Section 51/52)
 */
export class GstPostingService {
  private readonly endpoint = 'gst/gstposting';

  // ==================== ITC Blocked (Section 17(5)) ====================

  /**
   * Get all blocked ITC categories under Section 17(5)
   */
  async getBlockedCategories(): Promise<ItcBlockedCategory[]> {
    return apiClient.get<ItcBlockedCategory[]>(`${this.endpoint}/itc-blocked/categories`);
  }

  /**
   * Check if ITC is blocked for a given expense/HSN code
   */
  async checkItcBlocked(request: ItcBlockedCheckRequest): Promise<ItcBlockedCheckResult> {
    return apiClient.post<ItcBlockedCheckResult>(`${this.endpoint}/itc-blocked/check`, request);
  }

  /**
   * Post ITC blocked journal entry
   */
  async postItcBlocked(request: ItcBlockedRequest): Promise<GstPostingResult> {
    return apiClient.post<GstPostingResult>(`${this.endpoint}/itc-blocked`, request);
  }

  /**
   * Get ITC blocked summary for a return period
   */
  async getItcBlockedSummary(companyId: string, returnPeriod: string): Promise<ItcBlockedSummary> {
    return apiClient.get<ItcBlockedSummary>(
      `${this.endpoint}/itc-blocked/summary/${companyId}/${returnPeriod}`
    );
  }

  // ==================== Credit/Debit Notes ====================

  /**
   * Post GST adjustment for credit note
   */
  async postCreditNoteGst(request: CreditNoteGstRequest): Promise<GstPostingResult> {
    return apiClient.post<GstPostingResult>(`${this.endpoint}/credit-note`, request);
  }

  /**
   * Post GST adjustment for debit note
   */
  async postDebitNoteGst(request: DebitNoteGstRequest): Promise<GstPostingResult> {
    return apiClient.post<GstPostingResult>(`${this.endpoint}/debit-note`, request);
  }

  // ==================== ITC Reversal (Rule 42/43) ====================

  /**
   * Calculate ITC reversal amount per Rule 42/43
   */
  async calculateItcReversal(request: ItcReversalCalculationRequest): Promise<ItcReversalCalculation> {
    return apiClient.post<ItcReversalCalculation>(`${this.endpoint}/itc-reversal/calculate`, request);
  }

  /**
   * Post ITC reversal journal entry
   */
  async postItcReversal(request: ItcReversalRequest): Promise<GstPostingResult> {
    return apiClient.post<GstPostingResult>(`${this.endpoint}/itc-reversal`, request);
  }

  // ==================== UTGST ====================

  /**
   * Post UTGST entry for Union Territory transactions
   */
  async postUtgst(request: UtgstRequest): Promise<GstPostingResult> {
    return apiClient.post<GstPostingResult>(`${this.endpoint}/utgst`, request);
  }

  // ==================== GST TDS/TCS ====================

  /**
   * Post GST TDS (Section 51)
   */
  async postGstTds(request: GstTdsRequest): Promise<GstPostingResult> {
    return apiClient.post<GstPostingResult>(`${this.endpoint}/tds`, request);
  }

  /**
   * Post GST TCS (Section 52)
   */
  async postGstTcs(request: GstTcsRequest): Promise<GstPostingResult> {
    return apiClient.post<GstPostingResult>(`${this.endpoint}/tcs`, request);
  }

  // ==================== Reports ====================

  /**
   * Get ITC availability report for a return period
   */
  async getItcAvailabilityReport(companyId: string, returnPeriod: string): Promise<ItcAvailabilityReport> {
    return apiClient.get<ItcAvailabilityReport>(
      `${this.endpoint}/itc-report/${companyId}/${returnPeriod}`
    );
  }
}

export const gstPostingService = new GstPostingService();

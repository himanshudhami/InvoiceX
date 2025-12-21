import { apiClient } from '../../client';
import {
  TaxRulePack,
  CreateTaxRulePackDto,
  TdsSectionRate,
  TdsCalculationRequest,
  TdsCalculationResult,
  IncomeTaxCalculationRequest,
  IncomeTaxCalculationResult,
  PfEsiRates,
} from '../../types';

/**
 * Tax Rule Pack API service for managing versioned tax configurations
 * Supports TDS rates, income tax slabs, PF/ESI rates for Indian compliance
 */
export class TaxRulePackService {
  private readonly baseEndpoint = 'tax-rule-packs';

  // ==================== Rule Pack CRUD ====================

  async getAll(): Promise<TaxRulePack[]> {
    return apiClient.get<TaxRulePack[]>(this.baseEndpoint);
  }

  async getById(id: string): Promise<TaxRulePack> {
    return apiClient.get<TaxRulePack>(`${this.baseEndpoint}/${id}`);
  }

  async getActiveForFy(financialYear: string): Promise<TaxRulePack> {
    return apiClient.get<TaxRulePack>(`${this.baseEndpoint}/active/${financialYear}`);
  }

  async getActiveForDate(date: string): Promise<TaxRulePack> {
    return apiClient.get<TaxRulePack>(`${this.baseEndpoint}/active/date/${date}`);
  }

  async create(data: CreateTaxRulePackDto): Promise<TaxRulePack> {
    return apiClient.post<TaxRulePack, CreateTaxRulePackDto>(this.baseEndpoint, data);
  }

  async activate(id: string): Promise<{ message: string }> {
    return apiClient.post<{ message: string }, Record<string, never>>(
      `${this.baseEndpoint}/${id}/activate`,
      {}
    );
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.baseEndpoint}/${id}`);
  }

  // ==================== Calculations ====================

  async calculateTds(request: TdsCalculationRequest): Promise<TdsCalculationResult> {
    return apiClient.post<TdsCalculationResult, TdsCalculationRequest>(
      `${this.baseEndpoint}/calculate/tds`,
      request
    );
  }

  async calculateIncomeTax(request: IncomeTaxCalculationRequest): Promise<IncomeTaxCalculationResult> {
    return apiClient.post<IncomeTaxCalculationResult, IncomeTaxCalculationRequest>(
      `${this.baseEndpoint}/calculate/income-tax`,
      request
    );
  }

  // ==================== Rates ====================

  async getPfEsiRates(financialYear: string): Promise<PfEsiRates> {
    return apiClient.get<PfEsiRates>(`${this.baseEndpoint}/pf-esi-rates/${financialYear}`);
  }

  async getCurrentFy(): Promise<{ financialYear: string }> {
    return apiClient.get<{ financialYear: string }>(`${this.baseEndpoint}/current-fy`);
  }
}

// Singleton instance
export const taxRulePackService = new TaxRulePackService();

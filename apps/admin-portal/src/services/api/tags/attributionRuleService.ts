import { apiClient } from '../client';
import type {
  AttributionRule,
  CreateAttributionRuleDto,
  UpdateAttributionRuleDto,
  AttributionRulesFilterParams,
  RulePerformanceSummary,
  AutoAttributionResult,
  PagedResponse,
} from '../types';

/**
 * Attribution Rule API service - handles auto-tagging rule management
 */
export class AttributionRuleService {
  private readonly endpoint = 'attribution-rules';

  // ==================== Rule CRUD ====================

  async getAll(companyId?: string): Promise<AttributionRule[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<AttributionRule[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<AttributionRule> {
    return apiClient.get<AttributionRule>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: AttributionRulesFilterParams = {}): Promise<PagedResponse<AttributionRule>> {
    return apiClient.getPaged<AttributionRule>(this.endpoint, params);
  }

  async create(data: CreateAttributionRuleDto): Promise<AttributionRule> {
    return apiClient.post<AttributionRule, CreateAttributionRuleDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateAttributionRuleDto): Promise<void> {
    return apiClient.put<void, UpdateAttributionRuleDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  // ==================== Rule Queries ====================

  async getActiveRules(companyId: string): Promise<AttributionRule[]> {
    return apiClient.get<AttributionRule[]>(`${this.endpoint}/active`, { companyId });
  }

  async getRulesForType(companyId: string, transactionType: string): Promise<AttributionRule[]> {
    return apiClient.get<AttributionRule[]>(
      `${this.endpoint}/for-type/${transactionType}`,
      { companyId }
    );
  }

  // ==================== Rule Performance ====================

  async getPerformance(companyId: string): Promise<RulePerformanceSummary[]> {
    return apiClient.get<RulePerformanceSummary[]>(`${this.endpoint}/performance`, { companyId });
  }

  // ==================== Rule Testing ====================

  async testRule(
    ruleId: string,
    transactionId: string,
    transactionType: string
  ): Promise<AutoAttributionResult> {
    return apiClient.post<AutoAttributionResult, { transactionId: string; transactionType: string }>(
      `${this.endpoint}/${ruleId}/test`,
      { transactionId, transactionType }
    );
  }

  // ==================== Bulk Operations ====================

  async reorderPriorities(
    companyId: string,
    priorities: Array<{ ruleId: string; newPriority: number }>
  ): Promise<void> {
    return apiClient.post<void, { companyId: string; priorities: typeof priorities }>(
      `${this.endpoint}/reorder`,
      { companyId, priorities }
    );
  }
}

// Singleton instance
export const attributionRuleService = new AttributionRuleService();

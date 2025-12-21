import { apiClient } from '../../client';
import { CashFlowStatementDto } from '../../types';

/**
 * Cash Flow API service for retrieving cash flow statements (AS-3 compliant)
 */
export class CashFlowService {
  private readonly endpoint = 'cashflow';

  async getCashFlowStatement(params?: {
    companyId?: string;
    year?: number;
    month?: number;
  }): Promise<CashFlowStatementDto> {
    return apiClient.get<CashFlowStatementDto>(this.endpoint, params);
  }
}

// Singleton instance
export const cashFlowService = new CashFlowService();






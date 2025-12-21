import { apiClient } from '../../client';
import {
  TdsReceivable,
  CreateTdsReceivableDto,
  UpdateTdsReceivableDto,
  Match26AsDto,
  UpdateTdsStatusDto,
  TdsReceivableSummary,
  TdsReceivableFilterParams,
  PagedResponse
} from '../../types';

export class TdsReceivableService {
  private readonly endpoint = 'tdsreceivable';

  async getPaged(params?: TdsReceivableFilterParams): Promise<PagedResponse<TdsReceivable>> {
    return apiClient.getPaged<TdsReceivable>(this.endpoint, params);
  }

  async getAll(): Promise<TdsReceivable[]> {
    return apiClient.get<TdsReceivable[]>(this.endpoint);
  }

  async getById(id: string): Promise<TdsReceivable> {
    return apiClient.get<TdsReceivable>(`${this.endpoint}/${id}`);
  }

  async create(data: CreateTdsReceivableDto): Promise<TdsReceivable> {
    return apiClient.post<TdsReceivable>(this.endpoint, data);
  }

  async update(id: string, data: UpdateTdsReceivableDto): Promise<TdsReceivable> {
    return apiClient.put<TdsReceivable>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  // Get TDS receivables by company and financial year
  async getByCompanyAndFY(companyId: string, financialYear: string): Promise<TdsReceivable[]> {
    return apiClient.get<TdsReceivable[]>(
      `${this.endpoint}/by-company/${companyId}/fy/${financialYear}`
    );
  }

  // Get TDS receivables by company, financial year and quarter
  async getByCompanyFYQuarter(
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<TdsReceivable[]> {
    return apiClient.get<TdsReceivable[]>(
      `${this.endpoint}/by-company/${companyId}/fy/${financialYear}/q/${quarter}`
    );
  }

  // Get unmatched TDS receivables for a company
  async getUnmatched(companyId: string): Promise<TdsReceivable[]> {
    return apiClient.get<TdsReceivable[]>(`${this.endpoint}/unmatched/${companyId}`);
  }

  // Get TDS receivable summary for a financial year
  async getSummary(companyId: string, financialYear: string): Promise<TdsReceivableSummary> {
    return apiClient.get<TdsReceivableSummary>(
      `${this.endpoint}/summary/${companyId}/${financialYear}`
    );
  }

  // Match TDS entry with Form 26AS
  async matchWith26As(id: string, data: Match26AsDto): Promise<TdsReceivable> {
    return apiClient.post<TdsReceivable>(`${this.endpoint}/${id}/match-26as`, data);
  }

  // Update TDS status
  async updateStatus(id: string, data: UpdateTdsStatusDto): Promise<TdsReceivable> {
    return apiClient.post<TdsReceivable>(`${this.endpoint}/${id}/update-status`, data);
  }
}

export const tdsReceivableService = new TdsReceivableService();

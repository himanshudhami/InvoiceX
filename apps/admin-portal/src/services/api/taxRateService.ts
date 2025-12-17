import { apiClient } from './client';
import { TaxRate, CreateTaxRateDto, UpdateTaxRateDto, PagedResponse, PaginationParams } from './types';

/**
 * Tax Rate API service following SRP - handles only tax rate-related API calls
 */
export class TaxRateService {
  private readonly endpoint = 'taxrates';

  async getAll(): Promise<TaxRate[]> {
    return apiClient.get<TaxRate[]>(this.endpoint);
  }

  async getById(id: string): Promise<TaxRate> {
    return apiClient.get<TaxRate>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: PaginationParams = {}): Promise<PagedResponse<TaxRate>> {
    return apiClient.getPaged<TaxRate>(this.endpoint, params);
  }

  async create(data: CreateTaxRateDto): Promise<TaxRate> {
    return apiClient.post<TaxRate, CreateTaxRateDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateTaxRateDto): Promise<void> {
    return apiClient.put<void, UpdateTaxRateDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const taxRateService = new TaxRateService();
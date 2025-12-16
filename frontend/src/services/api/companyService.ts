import { apiClient } from './client';
import { Company, CreateCompanyDto, UpdateCompanyDto, PagedResponse, PaginationParams } from './types';

/**
 * Company API service following SRP - handles only company-related API calls
 */
export class CompanyService {
  private readonly endpoint = 'companies';

  async getAll(): Promise<Company[]> {
    return apiClient.get<Company[]>(this.endpoint);
  }

  async getById(id: string): Promise<Company> {
    return apiClient.get<Company>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: PaginationParams = {}): Promise<PagedResponse<Company>> {
    return apiClient.getPaged<Company>(this.endpoint, params);
  }

  async create(data: CreateCompanyDto): Promise<Company> {
    return apiClient.post<Company, CreateCompanyDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateCompanyDto): Promise<void> {
    return apiClient.put<void, UpdateCompanyDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const companyService = new CompanyService();
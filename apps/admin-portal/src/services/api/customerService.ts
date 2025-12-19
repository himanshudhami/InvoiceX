import { apiClient } from './client';
import { Customer, CreateCustomerDto, UpdateCustomerDto, PagedResponse, CustomersFilterParams } from './types';

/**
 * Customer API service following SRP - handles only customer-related API calls
 */
export class CustomerService {
  private readonly endpoint = 'customers';

  async getAll(companyId?: string): Promise<Customer[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<Customer[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<Customer> {
    return apiClient.get<Customer>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: CustomersFilterParams = {}): Promise<PagedResponse<Customer>> {
    return apiClient.getPaged<Customer>(this.endpoint, params);
  }

  async create(data: CreateCustomerDto): Promise<Customer> {
    return apiClient.post<Customer, CreateCustomerDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateCustomerDto): Promise<void> {
    return apiClient.put<void, UpdateCustomerDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const customerService = new CustomerService();
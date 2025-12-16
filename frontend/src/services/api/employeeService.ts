import { apiClient } from './client';
import { Employee, CreateEmployeeDto, UpdateEmployeeDto, PagedResponse, EmployeesFilterParams, BulkEmployeesDto, BulkUploadResult } from './types';

/**
 * Employee API service following SRP - handles only employee-related API calls
 */
export class EmployeeService {
  private readonly endpoint = 'employees';

  async getAll(): Promise<Employee[]> {
    return apiClient.get<Employee[]>(this.endpoint);
  }

  async getById(id: string): Promise<Employee> {
    return apiClient.get<Employee>(`${this.endpoint}/${id}`);
  }

  async getByEmployeeId(employeeId: string): Promise<Employee> {
    return apiClient.get<Employee>(`${this.endpoint}/by-employee-id/${employeeId}`);
  }

  async getPaged(params: EmployeesFilterParams = {}): Promise<PagedResponse<Employee>> {
    return apiClient.getPaged<Employee>(this.endpoint, params);
  }

  async create(data: CreateEmployeeDto): Promise<Employee> {
    return apiClient.post<Employee, CreateEmployeeDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateEmployeeDto): Promise<void> {
    return apiClient.put<void, UpdateEmployeeDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async checkExists(id: string): Promise<boolean> {
    return apiClient.get<boolean>(`${this.endpoint}/${id}/exists`);
  }

  async checkEmployeeIdUnique(employeeId: string, excludeId?: string): Promise<boolean> {
    const params = new URLSearchParams({ employeeId });
    if (excludeId) {
      params.append('excludeId', excludeId);
    }
    return apiClient.get<boolean>(`${this.endpoint}/check-employee-id-unique?${params}`);
  }

  async checkEmailUnique(email: string, excludeId?: string): Promise<boolean> {
    const params = new URLSearchParams({ email });
    if (excludeId) {
      params.append('excludeId', excludeId);
    }
    return apiClient.get<boolean>(`${this.endpoint}/check-email-unique?${params}`);
  }

  async bulkCreate(data: BulkEmployeesDto): Promise<BulkUploadResult> {
    return apiClient.post<BulkUploadResult, BulkEmployeesDto>(`${this.endpoint}/bulk`, data);
  }
}

// Singleton instance
export const employeeService = new EmployeeService();

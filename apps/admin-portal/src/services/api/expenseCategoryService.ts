import { apiClient } from './client';
import { PagedResponse, PaginationParams } from './types';

/**
 * Expense Category types
 */
export interface ExpenseCategory {
  id: string;
  companyId: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
  maxAmount?: number;
  requiresReceipt: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateExpenseCategoryDto {
  companyId: string;
  name: string;
  code: string;
  description?: string;
  isActive?: boolean;
  maxAmount?: number;
  requiresReceipt?: boolean;
}

export interface UpdateExpenseCategoryDto {
  name?: string;
  code?: string;
  description?: string;
  isActive?: boolean;
  maxAmount?: number;
  requiresReceipt?: boolean;
}

export interface ExpenseCategorySelectDto {
  id: string;
  name: string;
  code: string;
  maxAmount?: number;
  requiresReceipt: boolean;
}

/**
 * Expense Category API service
 */
export class ExpenseCategoryService {
  private readonly endpoint = 'expensecategories';

  async getByCompany(companyId: string): Promise<ExpenseCategory[]> {
    return apiClient.get<ExpenseCategory[]>(`${this.endpoint}/company/${companyId}`);
  }

  async getSelectList(companyId: string): Promise<ExpenseCategorySelectDto[]> {
    return apiClient.get<ExpenseCategorySelectDto[]>(`${this.endpoint}/company/${companyId}/select`);
  }

  async getPaged(params: PaginationParams & { companyId?: string } = {}): Promise<PagedResponse<ExpenseCategory>> {
    return apiClient.getPaged<ExpenseCategory>(this.endpoint, params);
  }

  async getById(id: string): Promise<ExpenseCategory> {
    return apiClient.get<ExpenseCategory>(`${this.endpoint}/${id}`);
  }

  async create(data: CreateExpenseCategoryDto): Promise<ExpenseCategory> {
    return apiClient.post<ExpenseCategory, CreateExpenseCategoryDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateExpenseCategoryDto): Promise<void> {
    return apiClient.put<void, UpdateExpenseCategoryDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async seedDefaults(companyId: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/seed/${companyId}`, {});
  }
}

// Singleton instance
export const expenseCategoryService = new ExpenseCategoryService();

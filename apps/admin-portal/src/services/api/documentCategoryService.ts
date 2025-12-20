import { apiClient } from './client';
import { PagedResponse, PaginationParams } from './types';

/**
 * Document Category types
 */
export interface DocumentCategory {
  id: string;
  companyId: string;
  name: string;
  code: string;
  description?: string;
  isSystem: boolean;
  isActive: boolean;
  requiresFinancialYear: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDocumentCategoryDto {
  companyId: string;
  name: string;
  code: string;
  description?: string;
  isSystem?: boolean;
  isActive?: boolean;
  requiresFinancialYear?: boolean;
  displayOrder?: number;
}

export interface UpdateDocumentCategoryDto {
  name?: string;
  code?: string;
  description?: string;
  isActive?: boolean;
  requiresFinancialYear?: boolean;
  displayOrder?: number;
}

export interface DocumentCategorySelectDto {
  id: string;
  name: string;
  code: string;
}

/**
 * Document Category API service
 */
export class DocumentCategoryService {
  private readonly endpoint = 'documentcategories';

  async getByCompany(companyId: string): Promise<DocumentCategory[]> {
    return apiClient.get<DocumentCategory[]>(`${this.endpoint}/company/${companyId}`);
  }

  async getSelectList(companyId: string): Promise<DocumentCategorySelectDto[]> {
    return apiClient.get<DocumentCategorySelectDto[]>(`${this.endpoint}/company/${companyId}/select`);
  }

  async getPaged(params: PaginationParams & { companyId?: string } = {}): Promise<PagedResponse<DocumentCategory>> {
    return apiClient.getPaged<DocumentCategory>(this.endpoint, params);
  }

  async getById(id: string): Promise<DocumentCategory> {
    return apiClient.get<DocumentCategory>(`${this.endpoint}/${id}`);
  }

  async create(data: CreateDocumentCategoryDto): Promise<DocumentCategory> {
    return apiClient.post<DocumentCategory, CreateDocumentCategoryDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateDocumentCategoryDto): Promise<void> {
    return apiClient.put<void, UpdateDocumentCategoryDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async seedDefaults(companyId: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/seed/${companyId}`, {});
  }
}

// Singleton instance
export const documentCategoryService = new DocumentCategoryService();

import { apiClient } from './client';
import { Product, CreateProductDto, UpdateProductDto, PagedResponse, PaginationParams } from './types';

/**
 * Product API service following SRP - handles only product-related API calls
 */
export class ProductService {
  private readonly endpoint = 'products';

  async getAll(): Promise<Product[]> {
    return apiClient.get<Product[]>(this.endpoint);
  }

  async getById(id: string): Promise<Product> {
    return apiClient.get<Product>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: PaginationParams = {}): Promise<PagedResponse<Product>> {
    return apiClient.getPaged<Product>(this.endpoint, params);
  }

  async create(data: CreateProductDto): Promise<Product> {
    return apiClient.post<Product, CreateProductDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateProductDto): Promise<void> {
    return apiClient.put<void, UpdateProductDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const productService = new ProductService();
import { apiClient } from '../client';
import type {
  Warehouse,
  CreateWarehouseDto,
  UpdateWarehouseDto,
  PagedResponse,
  WarehouseFilterParams,
} from '../types';

/**
 * Warehouse API service
 */
export class WarehouseService {
  private readonly endpoint = 'inventory/warehouses';

  async getAll(companyId?: string): Promise<Warehouse[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<Warehouse[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<Warehouse> {
    return apiClient.get<Warehouse>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: WarehouseFilterParams = {}): Promise<PagedResponse<Warehouse>> {
    return apiClient.getPaged<Warehouse>(this.endpoint, params);
  }

  async getActive(companyId?: string): Promise<Warehouse[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<Warehouse[]>(`${this.endpoint}/active`, params);
  }

  async getDefault(companyId?: string): Promise<Warehouse | null> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<Warehouse | null>(`${this.endpoint}/default`, params);
  }

  async create(data: CreateWarehouseDto): Promise<Warehouse> {
    return apiClient.post<Warehouse, CreateWarehouseDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateWarehouseDto): Promise<void> {
    return apiClient.put<void, UpdateWarehouseDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async setDefault(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/set-default`, {});
  }
}

// Singleton instance
export const warehouseService = new WarehouseService();

import { apiClient } from '../client';
import type {
  StockGroup,
  CreateStockGroupDto,
  UpdateStockGroupDto,
  PagedResponse,
  StockGroupFilterParams,
} from '../types';

/**
 * Stock Group API service
 */
export class StockGroupService {
  private readonly endpoint = 'inventory/stockgroups';

  async getAll(companyId?: string): Promise<StockGroup[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockGroup[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<StockGroup> {
    return apiClient.get<StockGroup>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: StockGroupFilterParams = {}): Promise<PagedResponse<StockGroup>> {
    return apiClient.getPaged<StockGroup>(this.endpoint, params);
  }

  async getHierarchy(companyId?: string): Promise<StockGroup[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockGroup[]>(`${this.endpoint}/hierarchy`, params);
  }

  async getActive(companyId?: string): Promise<StockGroup[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockGroup[]>(`${this.endpoint}/active`, params);
  }

  async getPath(id: string): Promise<string> {
    return apiClient.get<string>(`${this.endpoint}/${id}/path`);
  }

  async create(data: CreateStockGroupDto): Promise<StockGroup> {
    return apiClient.post<StockGroup, CreateStockGroupDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateStockGroupDto): Promise<void> {
    return apiClient.put<void, UpdateStockGroupDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const stockGroupService = new StockGroupService();

import { apiClient } from '../client';
import type {
  StockItem,
  CreateStockItemDto,
  UpdateStockItemDto,
  PagedResponse,
  StockItemFilterParams,
  StockPositionDto,
} from '../types';

/**
 * Stock Item API service
 */
export class StockItemService {
  private readonly endpoint = 'inventory/stockitems';

  async getAll(companyId?: string): Promise<StockItem[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockItem[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<StockItem> {
    return apiClient.get<StockItem>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: StockItemFilterParams = {}): Promise<PagedResponse<StockItem>> {
    return apiClient.getPaged<StockItem>(this.endpoint, params);
  }

  async getActive(companyId?: string): Promise<StockItem[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockItem[]>(`${this.endpoint}/active`, params);
  }

  async getLowStock(companyId?: string): Promise<StockItem[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockItem[]>(`${this.endpoint}/low-stock`, params);
  }

  async getByGroup(stockGroupId: string, companyId?: string): Promise<StockItem[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockItem[]>(`${this.endpoint}/by-group/${stockGroupId}`, params);
  }

  async getPosition(id: string): Promise<StockPositionDto[]> {
    return apiClient.get<StockPositionDto[]>(`${this.endpoint}/${id}/position`);
  }

  async create(data: CreateStockItemDto): Promise<StockItem> {
    return apiClient.post<StockItem, CreateStockItemDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateStockItemDto): Promise<void> {
    return apiClient.put<void, UpdateStockItemDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const stockItemService = new StockItemService();

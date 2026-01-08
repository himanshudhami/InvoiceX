import { apiClient } from '../client';
import type {
  ProductionOrder,
  CreateProductionOrderDto,
  UpdateProductionOrderDto,
  ReleaseProductionOrderDto,
  StartProductionOrderDto,
  CompleteProductionOrderDto,
  CancelProductionOrderDto,
  ConsumeItemDto,
  PagedProductionOrderResponse,
} from '../types';

/**
 * Production Order API service
 */
export class ProductionOrderService {
  private readonly endpoint = 'manufacturing/productionorders';

  async getAll(companyId?: string): Promise<ProductionOrder[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<ProductionOrder[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<ProductionOrder> {
    return apiClient.get<ProductionOrder>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: {
    pageNumber?: number;
    pageSize?: number;
    companyId?: string;
    searchTerm?: string;
    status?: string;
    bomId?: string;
    finishedGoodId?: string;
    warehouseId?: string;
    fromDate?: string;
    toDate?: string;
  } = {}): Promise<PagedProductionOrderResponse> {
    const queryParams = new URLSearchParams();
    if (params.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params.companyId) queryParams.append('companyId', params.companyId);
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params.status) queryParams.append('status', params.status);
    if (params.bomId) queryParams.append('bomId', params.bomId);
    if (params.finishedGoodId) queryParams.append('finishedGoodId', params.finishedGoodId);
    if (params.warehouseId) queryParams.append('warehouseId', params.warehouseId);
    if (params.fromDate) queryParams.append('fromDate', params.fromDate);
    if (params.toDate) queryParams.append('toDate', params.toDate);

    const url = queryParams.toString() ? `${this.endpoint}/paged?${queryParams}` : `${this.endpoint}/paged`;
    return apiClient.get<PagedProductionOrderResponse>(url);
  }

  async getByStatus(status: string, companyId?: string): Promise<ProductionOrder[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<ProductionOrder[]>(`${this.endpoint}/by-status/${status}`, params);
  }

  async create(data: CreateProductionOrderDto): Promise<ProductionOrder> {
    return apiClient.post<ProductionOrder, CreateProductionOrderDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateProductionOrderDto): Promise<void> {
    return apiClient.put<void, UpdateProductionOrderDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async release(id: string, data: ReleaseProductionOrderDto = {}): Promise<void> {
    return apiClient.post<void, ReleaseProductionOrderDto>(`${this.endpoint}/${id}/release`, data);
  }

  async start(id: string, data: StartProductionOrderDto = {}): Promise<void> {
    return apiClient.post<void, StartProductionOrderDto>(`${this.endpoint}/${id}/start`, data);
  }

  async complete(id: string, data: CompleteProductionOrderDto): Promise<void> {
    return apiClient.post<void, CompleteProductionOrderDto>(`${this.endpoint}/${id}/complete`, data);
  }

  async cancel(id: string, data: CancelProductionOrderDto = {}): Promise<void> {
    return apiClient.post<void, CancelProductionOrderDto>(`${this.endpoint}/${id}/cancel`, data);
  }

  async consumeItem(id: string, data: ConsumeItemDto): Promise<void> {
    return apiClient.post<void, ConsumeItemDto>(`${this.endpoint}/${id}/consume`, data);
  }
}

// Singleton instance
export const productionOrderService = new ProductionOrderService();

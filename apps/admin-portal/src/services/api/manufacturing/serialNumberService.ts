import { apiClient } from '../client';
import type {
  SerialNumber,
  CreateSerialNumberDto,
  UpdateSerialNumberDto,
  BulkCreateSerialNumberDto,
  MarkSerialAsSoldDto,
  SerialNumberFilterParams,
  PagedSerialNumberResponse,
} from '../types';

/**
 * Serial Number API service
 */
export class SerialNumberService {
  private readonly endpoint = 'inventory/serialnumbers';

  async getAll(companyId?: string): Promise<SerialNumber[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<SerialNumber[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<SerialNumber> {
    return apiClient.get<SerialNumber>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: SerialNumberFilterParams = {}): Promise<PagedSerialNumberResponse> {
    const queryParams = new URLSearchParams();
    if (params.companyId) queryParams.append('companyId', params.companyId);
    if (params.stockItemId) queryParams.append('stockItemId', params.stockItemId);
    if (params.warehouseId) queryParams.append('warehouseId', params.warehouseId);
    if (params.status) queryParams.append('status', params.status);
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());

    const url = queryParams.toString() ? `${this.endpoint}/paged?${queryParams}` : `${this.endpoint}/paged`;
    return apiClient.get<PagedSerialNumberResponse>(url);
  }

  async getByStockItem(stockItemId: string): Promise<SerialNumber[]> {
    return apiClient.get<SerialNumber[]>(`${this.endpoint}/by-item/${stockItemId}`);
  }

  async getAvailable(stockItemId: string, warehouseId?: string): Promise<SerialNumber[]> {
    const params = warehouseId ? { warehouseId } : undefined;
    return apiClient.get<SerialNumber[]>(`${this.endpoint}/available/${stockItemId}`, params);
  }

  async create(data: CreateSerialNumberDto): Promise<SerialNumber> {
    return apiClient.post<SerialNumber, CreateSerialNumberDto>(this.endpoint, data);
  }

  async bulkCreate(data: BulkCreateSerialNumberDto): Promise<SerialNumber[]> {
    return apiClient.post<SerialNumber[], BulkCreateSerialNumberDto>(`${this.endpoint}/bulk`, data);
  }

  async update(id: string, data: UpdateSerialNumberDto): Promise<void> {
    return apiClient.put<void, UpdateSerialNumberDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async markAsSold(id: string, data: MarkSerialAsSoldDto): Promise<void> {
    return apiClient.post<void, MarkSerialAsSoldDto>(`${this.endpoint}/${id}/mark-sold`, data);
  }

  async updateStatus(id: string, status: string): Promise<void> {
    return apiClient.patch<void, string>(`${this.endpoint}/${id}/status`, status);
  }
}

// Singleton instance
export const serialNumberService = new SerialNumberService();

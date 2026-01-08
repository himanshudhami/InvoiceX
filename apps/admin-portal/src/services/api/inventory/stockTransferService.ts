import { apiClient } from '../client';
import type {
  StockTransfer,
  CreateStockTransferDto,
  UpdateStockTransferDto,
  PagedResponse,
  StockTransferFilterParams,
  TransferStatus,
} from '../types';

/**
 * Stock Transfer API service
 */
export class StockTransferService {
  private readonly endpoint = 'inventory/stocktransfers';

  async getAll(companyId?: string): Promise<StockTransfer[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockTransfer[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<StockTransfer> {
    return apiClient.get<StockTransfer>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: StockTransferFilterParams = {}): Promise<PagedResponse<StockTransfer>> {
    return apiClient.getPaged<StockTransfer>(this.endpoint, params);
  }

  async getPending(companyId?: string): Promise<StockTransfer[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockTransfer[]>(`${this.endpoint}/pending`, params);
  }

  async getByStatus(status: TransferStatus, companyId?: string): Promise<StockTransfer[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockTransfer[]>(`${this.endpoint}/by-status/${status}`, params);
  }

  async create(data: CreateStockTransferDto): Promise<StockTransfer> {
    return apiClient.post<StockTransfer, CreateStockTransferDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateStockTransferDto): Promise<void> {
    return apiClient.put<void, UpdateStockTransferDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async dispatch(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/dispatch`, {});
  }

  async complete(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/complete`, {});
  }

  async cancel(id: string): Promise<void> {
    return apiClient.post<void, object>(`${this.endpoint}/${id}/cancel`, {});
  }
}

// Singleton instance
export const stockTransferService = new StockTransferService();

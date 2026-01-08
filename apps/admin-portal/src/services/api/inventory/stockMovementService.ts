import { apiClient } from '../client';
import type {
  StockMovement,
  CreateStockMovementDto,
  PagedResponse,
  StockMovementFilterParams,
  StockLedgerEntry,
  StockPositionDto,
} from '../types';

/**
 * Stock Movement API service
 */
export class StockMovementService {
  private readonly endpoint = 'inventory/stockmovements';

  async getAll(companyId?: string): Promise<StockMovement[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<StockMovement[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<StockMovement> {
    return apiClient.get<StockMovement>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: StockMovementFilterParams = {}): Promise<PagedResponse<StockMovement>> {
    return apiClient.getPaged<StockMovement>(this.endpoint, params);
  }

  async getLedger(
    stockItemId: string,
    params?: { warehouseId?: string; fromDate?: string; toDate?: string }
  ): Promise<StockLedgerEntry[]> {
    return apiClient.get<StockLedgerEntry[]>(`${this.endpoint}/ledger/${stockItemId}`, params);
  }

  async getPosition(stockItemId: string, warehouseId?: string): Promise<StockPositionDto> {
    const params = warehouseId ? { warehouseId } : undefined;
    return apiClient.get<StockPositionDto>(`${this.endpoint}/position/${stockItemId}`, params);
  }

  async create(data: CreateStockMovementDto): Promise<StockMovement> {
    return apiClient.post<StockMovement, CreateStockMovementDto>(this.endpoint, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const stockMovementService = new StockMovementService();

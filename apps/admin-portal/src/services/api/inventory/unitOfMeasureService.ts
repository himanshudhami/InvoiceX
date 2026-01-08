import { apiClient } from '../client';
import type {
  UnitOfMeasure,
  CreateUnitOfMeasureDto,
  UpdateUnitOfMeasureDto,
  PagedResponse,
  UnitOfMeasureFilterParams,
} from '../types';

/**
 * Unit of Measure API service
 */
export class UnitOfMeasureService {
  private readonly endpoint = 'inventory/unitsofmeasure';

  async getAll(companyId?: string): Promise<UnitOfMeasure[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<UnitOfMeasure[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<UnitOfMeasure> {
    return apiClient.get<UnitOfMeasure>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: UnitOfMeasureFilterParams = {}): Promise<PagedResponse<UnitOfMeasure>> {
    return apiClient.getPaged<UnitOfMeasure>(this.endpoint, params);
  }

  async getSystemUnits(): Promise<UnitOfMeasure[]> {
    return apiClient.get<UnitOfMeasure[]>(`${this.endpoint}/system`);
  }

  async create(data: CreateUnitOfMeasureDto): Promise<UnitOfMeasure> {
    return apiClient.post<UnitOfMeasure, CreateUnitOfMeasureDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateUnitOfMeasureDto): Promise<void> {
    return apiClient.put<void, UpdateUnitOfMeasureDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const unitOfMeasureService = new UnitOfMeasureService();

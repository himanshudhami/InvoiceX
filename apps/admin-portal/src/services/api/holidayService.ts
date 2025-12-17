import { apiClient } from './client';
import {
  Holiday,
  CreateHolidayDto,
  UpdateHolidayDto,
  BulkHolidaysDto,
  HolidayFilterParams,
  PagedResponse,
  BulkUploadResult,
} from './types';

export class HolidayService {
  private readonly endpoint = 'holidays';

  async getAll(params: HolidayFilterParams = {}): Promise<Holiday[]> {
    const queryParams = new URLSearchParams();
    if (params.companyId) queryParams.append('companyId', params.companyId);
    if (params.year) queryParams.append('year', params.year.toString());
    if (params.isOptional !== undefined) queryParams.append('isOptional', params.isOptional.toString());
    const query = queryParams.toString();
    return apiClient.get<Holiday[]>(`${this.endpoint}${query ? `?${query}` : ''}`);
  }

  async getPaged(params: HolidayFilterParams = {}): Promise<PagedResponse<Holiday>> {
    return apiClient.getPaged<Holiday>(this.endpoint, params);
  }

  async getById(id: string): Promise<Holiday> {
    return apiClient.get<Holiday>(`${this.endpoint}/${id}`);
  }

  async getByYear(companyId: string, year: number): Promise<Holiday[]> {
    return apiClient.get<Holiday[]>(`${this.endpoint}/company/${companyId}/year/${year}`);
  }

  async getUpcoming(companyId: string, days: number = 30): Promise<Holiday[]> {
    return apiClient.get<Holiday[]>(`${this.endpoint}/company/${companyId}/upcoming?days=${days}`);
  }

  async create(data: CreateHolidayDto): Promise<Holiday> {
    return apiClient.post<Holiday, CreateHolidayDto>(this.endpoint, data);
  }

  async bulkCreate(data: BulkHolidaysDto): Promise<BulkUploadResult> {
    return apiClient.post<BulkUploadResult, BulkHolidaysDto>(`${this.endpoint}/bulk`, data);
  }

  async update(id: string, data: UpdateHolidayDto): Promise<void> {
    return apiClient.put<void, UpdateHolidayDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async copyToNextYear(companyId: string, sourceYear: number): Promise<{ copied: number }> {
    return apiClient.post<{ copied: number }, { companyId: string; sourceYear: number }>(
      `${this.endpoint}/copy-year`,
      { companyId, sourceYear }
    );
  }
}

export const holidayService = new HolidayService();

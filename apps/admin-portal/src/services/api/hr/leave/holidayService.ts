import { apiClient } from '../../client';
import {
  Holiday,
  CreateHolidayDto,
  UpdateHolidayDto,
  BulkHolidaysDto,
  HolidayFilterParams,
  PagedResponse,
  BulkUploadResult,
} from '../../types';

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
    // Backend expects companyId as a query parameter, not in the body
    const queryParams = new URLSearchParams();
    if (data.companyId) queryParams.append('companyId', data.companyId);
    const query = queryParams.toString();
    return apiClient.post<Holiday, CreateHolidayDto>(`${this.endpoint}${query ? `?${query}` : ''}`, data);
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
    // Backend expects query params: fromYear, toYear, companyId
    const toYear = sourceYear + 1;
    const queryParams = new URLSearchParams();
    queryParams.append('fromYear', sourceYear.toString());
    queryParams.append('toYear', toYear.toString());
    queryParams.append('companyId', companyId);
    // Returns array of created holidays - map to copied count
    const holidays = await apiClient.post<any[]>(`${this.endpoint}/copy?${queryParams.toString()}`);
    return { copied: Array.isArray(holidays) ? holidays.length : 0 };
  }
}

export const holidayService = new HolidayService();

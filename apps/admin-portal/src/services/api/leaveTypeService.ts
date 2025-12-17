import { apiClient } from './client';
import {
  LeaveType,
  CreateLeaveTypeDto,
  UpdateLeaveTypeDto,
  PagedResponse,
  PaginationParams,
} from './types';

export class LeaveTypeService {
  private readonly endpoint = 'leave-types';

  async getAll(companyId?: string): Promise<LeaveType[]> {
    const params = companyId ? `?companyId=${companyId}` : '';
    return apiClient.get<LeaveType[]>(`${this.endpoint}${params}`);
  }

  async getPaged(params: PaginationParams & { companyId?: string } = {}): Promise<PagedResponse<LeaveType>> {
    return apiClient.getPaged<LeaveType>(this.endpoint, params);
  }

  async getById(id: string): Promise<LeaveType> {
    return apiClient.get<LeaveType>(`${this.endpoint}/${id}`);
  }

  async create(data: CreateLeaveTypeDto): Promise<LeaveType> {
    return apiClient.post<LeaveType, CreateLeaveTypeDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateLeaveTypeDto): Promise<void> {
    return apiClient.put<void, UpdateLeaveTypeDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async toggleActive(id: string): Promise<void> {
    return apiClient.post<void, {}>(`${this.endpoint}/${id}/toggle-active`, {});
  }
}

export const leaveTypeService = new LeaveTypeService();

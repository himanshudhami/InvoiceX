import { apiClient } from './client';
import {
  LeaveApplication,
  CreateLeaveApplicationDto,
  UpdateLeaveApplicationDto,
  ApproveLeaveDto,
  RejectLeaveDto,
  LeaveApplicationFilterParams,
  PagedResponse,
  LeaveCalendarEntry,
} from './types';

export class LeaveApplicationService {
  private readonly endpoint = 'leave-applications';

  async getAll(params: LeaveApplicationFilterParams = {}): Promise<LeaveApplication[]> {
    const queryParams = new URLSearchParams();
    if (params.companyId) queryParams.append('companyId', params.companyId);
    if (params.employeeId) queryParams.append('employeeId', params.employeeId);
    if (params.leaveTypeId) queryParams.append('leaveTypeId', params.leaveTypeId);
    if (params.status) queryParams.append('status', params.status);
    if (params.fromDate) queryParams.append('fromDate', params.fromDate);
    if (params.toDate) queryParams.append('toDate', params.toDate);
    const query = queryParams.toString();
    return apiClient.get<LeaveApplication[]>(`${this.endpoint}${query ? `?${query}` : ''}`);
  }

  async getPaged(params: LeaveApplicationFilterParams = {}): Promise<PagedResponse<LeaveApplication>> {
    return apiClient.getPaged<LeaveApplication>(this.endpoint, params);
  }

  async getById(id: string): Promise<LeaveApplication> {
    return apiClient.get<LeaveApplication>(`${this.endpoint}/${id}`);
  }

  async getPendingApprovals(companyId?: string): Promise<LeaveApplication[]> {
    const params = companyId ? `?companyId=${companyId}` : '';
    return apiClient.get<LeaveApplication[]>(`${this.endpoint}/pending${params}`);
  }

  async getByEmployee(employeeId: string): Promise<LeaveApplication[]> {
    return apiClient.get<LeaveApplication[]>(`${this.endpoint}/employee/${employeeId}`);
  }

  async create(data: CreateLeaveApplicationDto): Promise<LeaveApplication> {
    return apiClient.post<LeaveApplication, CreateLeaveApplicationDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateLeaveApplicationDto): Promise<void> {
    return apiClient.put<void, UpdateLeaveApplicationDto>(`${this.endpoint}/${id}`, data);
  }

  async approve(id: string, data: ApproveLeaveDto): Promise<void> {
    return apiClient.post<void, ApproveLeaveDto>(`${this.endpoint}/${id}/approve`, data);
  }

  async reject(id: string, data: RejectLeaveDto): Promise<void> {
    return apiClient.post<void, RejectLeaveDto>(`${this.endpoint}/${id}/reject`, data);
  }

  async cancel(id: string): Promise<void> {
    return apiClient.post<void, {}>(`${this.endpoint}/${id}/cancel`, {});
  }

  async withdraw(id: string): Promise<void> {
    return apiClient.post<void, {}>(`${this.endpoint}/${id}/withdraw`, {});
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async getCalendar(companyId: string, year: number, month: number): Promise<LeaveCalendarEntry[]> {
    return apiClient.get<LeaveCalendarEntry[]>(
      `${this.endpoint}/calendar?companyId=${companyId}&year=${year}&month=${month}`
    );
  }
}

export const leaveApplicationService = new LeaveApplicationService();

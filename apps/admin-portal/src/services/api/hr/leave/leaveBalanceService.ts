import { apiClient } from '../../client';
import {
  EmployeeLeaveBalance,
  CreateLeaveBalanceDto,
  UpdateLeaveBalanceDto,
  AdjustLeaveBalanceDto,
  LeaveBalanceFilterParams,
  PagedResponse,
  LeaveSummary,
} from '../../types';

export class LeaveBalanceService {
  private readonly endpoint = 'leave-balances';

  async getAll(params: LeaveBalanceFilterParams = {}): Promise<EmployeeLeaveBalance[]> {
    const queryParams = new URLSearchParams();
    if (params.companyId) queryParams.append('companyId', params.companyId);
    if (params.employeeId) queryParams.append('employeeId', params.employeeId);
    if (params.leaveTypeId) queryParams.append('leaveTypeId', params.leaveTypeId);
    if (params.financialYear) queryParams.append('financialYear', params.financialYear);
    const query = queryParams.toString();
    return apiClient.get<EmployeeLeaveBalance[]>(`${this.endpoint}${query ? `?${query}` : ''}`);
  }

  async getPaged(params: LeaveBalanceFilterParams = {}): Promise<PagedResponse<EmployeeLeaveBalance>> {
    return apiClient.getPaged<EmployeeLeaveBalance>(this.endpoint, params);
  }

  async getById(id: string): Promise<EmployeeLeaveBalance> {
    return apiClient.get<EmployeeLeaveBalance>(`${this.endpoint}/${id}`);
  }

  async getByEmployee(employeeId: string, financialYear?: string): Promise<EmployeeLeaveBalance[]> {
    const params = financialYear ? `?financialYear=${financialYear}` : '';
    return apiClient.get<EmployeeLeaveBalance[]>(`${this.endpoint}/employee/${employeeId}${params}`);
  }

  async getEmployeeSummary(employeeId: string, financialYear: string): Promise<LeaveSummary> {
    return apiClient.get<LeaveSummary>(`${this.endpoint}/employee/${employeeId}/summary?financialYear=${financialYear}`);
  }

  async create(data: CreateLeaveBalanceDto): Promise<EmployeeLeaveBalance> {
    return apiClient.post<EmployeeLeaveBalance, CreateLeaveBalanceDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateLeaveBalanceDto): Promise<void> {
    return apiClient.put<void, UpdateLeaveBalanceDto>(`${this.endpoint}/${id}`, data);
  }

  async adjust(id: string, data: AdjustLeaveBalanceDto): Promise<void> {
    return apiClient.post<void, AdjustLeaveBalanceDto>(`${this.endpoint}/${id}/adjust`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async initializeForYear(companyId: string, financialYear: string): Promise<{ created: number }> {
    return apiClient.post<{ created: number }, { companyId: string; financialYear: string }>(
      `${this.endpoint}/initialize`,
      { companyId, financialYear }
    );
  }

  async carryForward(companyId: string, fromYear: string, toYear: string): Promise<{ processed: number }> {
    return apiClient.post<{ processed: number }, { companyId: string; fromYear: string; toYear: string }>(
      `${this.endpoint}/carry-forward`,
      { companyId, fromYear, toYear }
    );
  }
}

export const leaveBalanceService = new LeaveBalanceService();

import { apiClient } from './client';
import {
  Subscription,
  SubscriptionAssignment,
  CreateSubscriptionDto,
  UpdateSubscriptionDto,
  CreateSubscriptionAssignmentDto,
  RevokeSubscriptionAssignmentDto,
  SubscriptionMonthlyExpense,
  SubscriptionCostReport,
  PagedResponse,
  PaginationParams,
} from './types';

export class SubscriptionService {
  private readonly endpoint = 'subscriptions';

  async getById(id: string): Promise<Subscription> {
    return apiClient.get<Subscription>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: PaginationParams = {}): Promise<PagedResponse<Subscription>> {
    return apiClient.getPaged<Subscription>(this.endpoint, params);
  }

  async create(data: CreateSubscriptionDto): Promise<Subscription> {
    return apiClient.post<Subscription, CreateSubscriptionDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateSubscriptionDto): Promise<void> {
    return apiClient.put<void, UpdateSubscriptionDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async getAssignments(id: string): Promise<SubscriptionAssignment[]> {
    return apiClient.get<SubscriptionAssignment[]>(`${this.endpoint}/${id}/assignments`);
  }

  async assign(id: string, data: CreateSubscriptionAssignmentDto): Promise<SubscriptionAssignment> {
    return apiClient.post<SubscriptionAssignment, CreateSubscriptionAssignmentDto>(`${this.endpoint}/${id}/assign`, data);
  }

  async revokeAssignment(assignmentId: string, data: RevokeSubscriptionAssignmentDto): Promise<void> {
    return apiClient.post<void, RevokeSubscriptionAssignmentDto>(
      `${this.endpoint}/assignments/${assignmentId}/revoke`,
      data,
    );
  }

  async pause(id: string, pausedOn?: string): Promise<void> {
    return apiClient.post<void, { pausedOn?: string; notes?: string }>(`${this.endpoint}/${id}/pause`, {
      pausedOn,
    });
  }

  async resume(id: string, resumedOn?: string): Promise<void> {
    return apiClient.post<void, { resumedOn?: string; notes?: string }>(`${this.endpoint}/${id}/resume`, {
      resumedOn,
    });
  }

  async cancel(id: string, cancelledOn?: string): Promise<void> {
    return apiClient.post<void, { cancelledOn?: string; notes?: string }>(`${this.endpoint}/${id}/cancel`, {
      cancelledOn,
    });
  }

  async getMonthlyExpenses(year: number, month?: number, companyId?: string): Promise<SubscriptionMonthlyExpense[]> {
    const params = new URLSearchParams();
    params.append('year', year.toString());
    if (month) params.append('month', month.toString());
    if (companyId) params.append('companyId', companyId);
    return apiClient.get<SubscriptionMonthlyExpense[]>(`${this.endpoint}/expenses/monthly?${params.toString()}`);
  }

  async getCostReport(companyId?: string): Promise<SubscriptionCostReport> {
    const params = companyId ? `?companyId=${companyId}` : '';
    return apiClient.get<SubscriptionCostReport>(`${this.endpoint}/expenses/report${params}`);
  }
}

export const subscriptionService = new SubscriptionService();






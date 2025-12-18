import { apiClient } from './client';
import {
  ApprovalWorkflowTemplate,
  ApprovalWorkflowTemplateDetail,
  ApprovalRequestDetail,
  PendingApproval,
  ApprovalRequest,
  CreateApprovalTemplateDto,
  UpdateApprovalTemplateDto,
  CreateApprovalStepDto,
  UpdateApprovalStepDto,
  ReorderStepsDto,
  ApproveRequestDto,
  RejectRequestDto,
  ApprovalWorkflowStep,
} from 'shared-types';

// Approval Template Service
export class ApprovalTemplateService {
  private readonly endpoint = 'approval-templates';

  async getByCompany(companyId: string, activityType?: string): Promise<ApprovalWorkflowTemplate[]> {
    const params: Record<string, string> = { companyId };
    if (activityType) params.activityType = activityType;
    return apiClient.get<ApprovalWorkflowTemplate[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<ApprovalWorkflowTemplateDetail> {
    return apiClient.get<ApprovalWorkflowTemplateDetail>(`${this.endpoint}/${id}`);
  }

  async create(data: CreateApprovalTemplateDto): Promise<ApprovalWorkflowTemplate> {
    return apiClient.post<ApprovalWorkflowTemplate, CreateApprovalTemplateDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateApprovalTemplateDto): Promise<ApprovalWorkflowTemplate> {
    return apiClient.put<ApprovalWorkflowTemplate, UpdateApprovalTemplateDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async setAsDefault(id: string): Promise<void> {
    return apiClient.post<void, {}>(`${this.endpoint}/${id}/set-default`, {});
  }

  // Step operations
  async addStep(templateId: string, data: CreateApprovalStepDto): Promise<ApprovalWorkflowStep> {
    return apiClient.post<ApprovalWorkflowStep, CreateApprovalStepDto>(
      `${this.endpoint}/${templateId}/steps`,
      data
    );
  }

  async updateStep(templateId: string, stepId: string, data: UpdateApprovalStepDto): Promise<ApprovalWorkflowStep> {
    return apiClient.put<ApprovalWorkflowStep, UpdateApprovalStepDto>(
      `${this.endpoint}/${templateId}/steps/${stepId}`,
      data
    );
  }

  async deleteStep(templateId: string, stepId: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${templateId}/steps/${stepId}`);
  }

  async reorderSteps(templateId: string, data: ReorderStepsDto): Promise<void> {
    return apiClient.post<void, ReorderStepsDto>(`${this.endpoint}/${templateId}/reorder-steps`, data);
  }
}

// Approval Workflow Service
export class ApprovalWorkflowService {
  private readonly endpoint = 'approvals';

  async getPendingApprovals(employeeId: string): Promise<PendingApproval[]> {
    return apiClient.get<PendingApproval[]>(`${this.endpoint}/pending`, { employeeId });
  }

  async getPendingApprovalsCount(employeeId: string): Promise<number> {
    return apiClient.get<number>(`${this.endpoint}/pending/count`, { employeeId });
  }

  async getRequestDetails(requestId: string): Promise<ApprovalRequestDetail> {
    return apiClient.get<ApprovalRequestDetail>(`${this.endpoint}/${requestId}`);
  }

  async getActivityApprovalStatus(activityType: string, activityId: string): Promise<ApprovalRequestDetail | null> {
    return apiClient.get<ApprovalRequestDetail | null>(`${this.endpoint}/activity/${activityType}/${activityId}`);
  }

  async getRequestsByRequestor(requestorId: string, status?: string): Promise<ApprovalRequest[]> {
    const params: Record<string, string> = {};
    if (status) params.status = status;
    return apiClient.get<ApprovalRequest[]>(`${this.endpoint}/by-requestor/${requestorId}`, params);
  }

  async approve(requestId: string, approverId: string, data: ApproveRequestDto): Promise<ApprovalRequestDetail> {
    return apiClient.post<ApprovalRequestDetail, ApproveRequestDto>(
      `${this.endpoint}/${requestId}/approve?approverId=${approverId}`,
      data
    );
  }

  async reject(requestId: string, approverId: string, data: RejectRequestDto): Promise<ApprovalRequestDetail> {
    return apiClient.post<ApprovalRequestDetail, RejectRequestDto>(
      `${this.endpoint}/${requestId}/reject?approverId=${approverId}`,
      data
    );
  }

  async cancel(requestId: string, requestorId: string): Promise<void> {
    return apiClient.post<void, {}>(`${this.endpoint}/${requestId}/cancel?requestorId=${requestorId}`, {});
  }
}

export const approvalTemplateService = new ApprovalTemplateService();
export const approvalWorkflowService = new ApprovalWorkflowService();

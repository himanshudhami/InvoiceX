import { apiClient } from '../../client';
import { PagedResponse, PaginationParams } from '../../types';

/**
 * Expense Claim Status
 */
export const ExpenseClaimStatus = {
  Draft: 'draft',
  Submitted: 'submitted',
  PendingApproval: 'pending_approval',
  Approved: 'approved',
  Rejected: 'rejected',
  Reimbursed: 'reimbursed',
  Cancelled: 'cancelled',
} as const;

export type ExpenseClaimStatusType = typeof ExpenseClaimStatus[keyof typeof ExpenseClaimStatus];

/**
 * Expense Claim types
 */
export interface ExpenseClaim {
  id: string;
  companyId: string;
  employeeId: string;
  employeeName?: string;
  claimNumber: string;
  title: string;
  description?: string;
  categoryId: string;
  categoryName?: string;
  expenseDate: string;
  amount: number;
  currency: string;
  status: ExpenseClaimStatusType;
  submittedAt?: string;
  approvedAt?: string;
  approvedBy?: string;
  approvedByName?: string;
  rejectedAt?: string;
  rejectedBy?: string;
  rejectionReason?: string;
  reimbursedAt?: string;
  reimbursementReference?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ExpenseAttachment {
  id: string;
  expenseId: string;
  fileStorageId: string;
  originalFilename: string;
  storagePath: string;
  mimeType: string;
  fileSize: number;
  description?: string;
  isPrimary: boolean;
  createdAt: string;
}

export interface CreateExpenseClaimDto {
  title: string;
  description?: string;
  categoryId: string;
  expenseDate: string;
  amount: number;
  currency?: string;
}

export interface UpdateExpenseClaimDto {
  title?: string;
  description?: string;
  categoryId?: string;
  expenseDate?: string;
  amount?: number;
}

export interface RejectExpenseClaimDto {
  reason: string;
}

export interface ReimburseExpenseClaimDto {
  reimbursementReference?: string;
}

export interface ExpenseClaimFilterParams extends PaginationParams {
  companyId?: string;
  employeeId?: string;
  categoryId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

/**
 * Expense Claim API service for admin portal
 */
export class ExpenseClaimService {
  private readonly endpoint = 'expenseclaims';

  async getPaged(params: ExpenseClaimFilterParams = {}): Promise<PagedResponse<ExpenseClaim>> {
    return apiClient.getPaged<ExpenseClaim>(this.endpoint, params);
  }

  async getById(id: string): Promise<ExpenseClaim> {
    return apiClient.get<ExpenseClaim>(`${this.endpoint}/${id}`);
  }

  async getAttachments(id: string): Promise<ExpenseAttachment[]> {
    return apiClient.get<ExpenseAttachment[]>(`${this.endpoint}/${id}/attachments`);
  }

  async approve(id: string): Promise<ExpenseClaim> {
    return apiClient.post<ExpenseClaim, object>(`${this.endpoint}/${id}/approve`, {});
  }

  async reject(id: string, data: RejectExpenseClaimDto): Promise<ExpenseClaim> {
    return apiClient.post<ExpenseClaim, RejectExpenseClaimDto>(`${this.endpoint}/${id}/reject`, data);
  }

  async reimburse(id: string, data: ReimburseExpenseClaimDto): Promise<ExpenseClaim> {
    return apiClient.post<ExpenseClaim, ReimburseExpenseClaimDto>(`${this.endpoint}/${id}/reimburse`, data);
  }
}

// Singleton instance
export const expenseClaimService = new ExpenseClaimService();

import { apiClient } from './client'

// Expense Status Constants
export const ExpenseClaimStatus = {
  Draft: 'draft',
  Submitted: 'submitted',
  PendingApproval: 'pending_approval',
  Approved: 'approved',
  Rejected: 'rejected',
  Reimbursed: 'reimbursed',
  Cancelled: 'cancelled',
} as const

export type ExpenseClaimStatusType = typeof ExpenseClaimStatus[keyof typeof ExpenseClaimStatus]

// Types
export interface ExpenseCategory {
  id: string
  name: string
  code: string
  maxAmount?: number
  requiresReceipt: boolean
  // GST defaults
  isGstApplicable: boolean
  defaultGstRate: number
  defaultHsnSac?: string
  itcEligible: boolean
}

export interface ExpenseClaim {
  id: string
  companyId: string
  employeeId: string
  claimNumber: string
  title: string
  description?: string
  categoryId: string
  categoryName?: string
  expenseDate: string
  amount: number
  currency: string
  status: ExpenseClaimStatusType
  submittedAt?: string
  approvedAt?: string
  approvedByName?: string
  rejectedAt?: string
  rejectionReason?: string
  reimbursedAt?: string
  reimbursementReference?: string
  // GST/ITC Information
  vendorName?: string
  vendorGstin?: string
  invoiceNumber?: string
  invoiceDate?: string
  isGstApplicable: boolean
  supplyType: 'intra_state' | 'inter_state'
  hsnSacCode?: string
  gstRate: number
  baseAmount?: number
  cgstRate: number
  cgstAmount: number
  sgstRate: number
  sgstAmount: number
  igstRate: number
  igstAmount: number
  totalGstAmount: number
  itcEligible: boolean
  itcClaimed: boolean
  itcClaimedInReturn?: string
  createdAt: string
  updatedAt: string
}

export interface ExpenseAttachment {
  id: string
  expenseId: string
  fileStorageId: string
  originalFilename: string
  downloadUrl: string
  mimeType: string
  fileSize: number
  description?: string
  isPrimary: boolean
  createdAt: string
}

export interface CreateExpenseClaimRequest {
  title: string
  description?: string
  categoryId: string
  expenseDate: string
  amount: number
  currency?: string
  // GST fields
  vendorName?: string
  vendorGstin?: string
  invoiceNumber?: string
  invoiceDate?: string
  isGstApplicable?: boolean
  supplyType?: 'intra_state' | 'inter_state'
  hsnSacCode?: string
  gstRate?: number
  baseAmount?: number
  cgstAmount?: number
  sgstAmount?: number
  igstAmount?: number
}

export interface UpdateExpenseClaimRequest {
  title?: string
  description?: string
  categoryId?: string
  expenseDate?: string
  amount?: number
  // GST fields
  vendorName?: string
  vendorGstin?: string
  invoiceNumber?: string
  invoiceDate?: string
  isGstApplicable?: boolean
  supplyType?: 'intra_state' | 'inter_state'
  hsnSacCode?: string
  gstRate?: number
  baseAmount?: number
  cgstAmount?: number
  sgstAmount?: number
  igstAmount?: number
}

export interface AddAttachmentRequest {
  fileStorageId: string
  description?: string
  isPrimary?: boolean
}

export interface PagedResponse<T> {
  data: T[]
  totalCount: number
  currentPage: number
  pageSize: number
  totalPages: number
  hasPrevious: boolean
  hasNext: boolean
}

// API Functions
export const expenseApi = {
  // Get expense categories for dropdown
  getCategories: async (): Promise<ExpenseCategory[]> => {
    const response = await apiClient.get('/portal/expenses/categories')
    return response.data
  },

  // Get my expenses (paginated)
  getMyExpenses: async (
    pageNumber = 1,
    pageSize = 10,
    status?: string
  ): Promise<PagedResponse<ExpenseClaim>> => {
    const params = new URLSearchParams()
    params.append('pageNumber', String(pageNumber))
    params.append('pageSize', String(pageSize))
    if (status) params.append('status', status)

    const response = await apiClient.get(`/portal/expenses?${params.toString()}`)
    return response.data
  },

  // Get expense by ID
  getById: async (id: string): Promise<ExpenseClaim> => {
    const response = await apiClient.get(`/portal/expenses/${id}`)
    return response.data
  },

  // Create new expense claim (draft)
  create: async (data: CreateExpenseClaimRequest): Promise<ExpenseClaim> => {
    const response = await apiClient.post('/portal/expenses', data)
    return response.data
  },

  // Update draft expense claim
  update: async (id: string, data: UpdateExpenseClaimRequest): Promise<ExpenseClaim> => {
    const response = await apiClient.put(`/portal/expenses/${id}`, data)
    return response.data
  },

  // Submit expense claim for approval
  submit: async (id: string): Promise<ExpenseClaim> => {
    const response = await apiClient.post(`/portal/expenses/${id}/submit`)
    return response.data
  },

  // Cancel expense claim
  cancel: async (id: string): Promise<void> => {
    await apiClient.post(`/portal/expenses/${id}/cancel`)
  },

  // Delete draft expense claim
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/portal/expenses/${id}`)
  },

  // Get attachments for expense claim
  getAttachments: async (id: string): Promise<ExpenseAttachment[]> => {
    const response = await apiClient.get(`/portal/expenses/${id}/attachments`)
    return response.data
  },

  // Add attachment to expense claim
  addAttachment: async (id: string, data: AddAttachmentRequest): Promise<ExpenseAttachment> => {
    const response = await apiClient.post(`/portal/expenses/${id}/attachments`, data)
    return response.data
  },

  // Remove attachment from expense claim
  removeAttachment: async (expenseId: string, attachmentId: string): Promise<void> => {
    await apiClient.delete(`/portal/expenses/${expenseId}/attachments/${attachmentId}`)
  },

  // Upload file (returns file storage ID for attachment)
  uploadFile: async (file: File): Promise<{ id: string; storagePath: string }> => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('entityType', 'expense_attachment')

    const response = await apiClient.post('/files/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    })
    return response.data
  },

  // Get file download URL
  getDownloadUrl: (storagePath: string): string => {
    const token = localStorage.getItem('portal_access_token')
    const baseUrl = import.meta.env.VITE_API_URL || '/api'
    return `${baseUrl}/files/download/${encodeURIComponent(storagePath)}?token=${token}`
  },
}

// Manager Expense API
export const managerExpenseApi = {
  // Get pending expenses from direct reports
  getPendingExpenses: async (): Promise<ExpenseClaim[]> => {
    const response = await apiClient.get('/manager/expenses/pending')
    return response.data
  },

  // Get expense claim by ID
  getById: async (id: string): Promise<ExpenseClaim> => {
    const response = await apiClient.get(`/manager/expenses/${id}`)
    return response.data
  },

  // Get attachments
  getAttachments: async (id: string): Promise<ExpenseAttachment[]> => {
    const response = await apiClient.get(`/manager/expenses/${id}/attachments`)
    return response.data
  },

  // Approve expense claim
  approve: async (id: string): Promise<ExpenseClaim> => {
    const response = await apiClient.post(`/manager/expenses/${id}/approve`)
    return response.data
  },

  // Reject expense claim
  reject: async (id: string, reason: string): Promise<ExpenseClaim> => {
    const response = await apiClient.post(`/manager/expenses/${id}/reject`, { reason })
    return response.data
  },
}

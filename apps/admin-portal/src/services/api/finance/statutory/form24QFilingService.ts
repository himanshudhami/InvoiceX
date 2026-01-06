import { apiClient } from '../../client';
import type { PagedResponse } from '../../types';

// ==================== Types ====================

export interface Form24QFiling {
  id: string;
  companyId: string;
  companyName: string;
  financialYear: string;
  quarter: string;
  tan: string;
  formType: string;
  originalFilingId?: string;
  revisionNumber: number;
  totalEmployees: number;
  totalSalaryPaid: number;
  totalTdsDeducted: number;
  totalTdsDeposited: number;
  variance: number;
  status: string;
  hasValidationErrors: boolean;
  validationErrorCount: number;
  validationWarningCount: number;
  hasFvuFile: boolean;
  fvuGeneratedAt?: string;
  fvuVersion?: string;
  filingDate?: string;
  acknowledgementNumber?: string;
  tokenNumber?: string;
  provisionalReceiptNumber?: string;
  dueDate: string;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Form24QFilingSummary {
  id: string;
  financialYear: string;
  quarter: string;
  tan: string;
  formType: string;
  revisionNumber: number;
  totalEmployees: number;
  totalTdsDeducted: number;
  totalTdsDeposited: number;
  variance: number;
  status: string;
  hasFvuFile: boolean;
  acknowledgementNumber?: string;
  filingDate?: string;
  dueDate: string;
  isOverdue: boolean;
  createdAt: string;
}

export interface QuarterStatus {
  quarter: string;
  status: string;
  hasFiling: boolean;
  isOverdue: boolean;
  dueDate: string;
  totalEmployees: number;
  tdsDeducted: number;
  tdsDeposited: number;
  acknowledgementNumber?: string;
}

export interface Form24QFilingStatistics {
  financialYear: string;
  totalFilings: number;
  draftCount: number;
  validatedCount: number;
  fvuGeneratedCount: number;
  submittedCount: number;
  acknowledgedCount: number;
  rejectedCount: number;
  pendingCount: number;
  overdueCount: number;
  totalTdsDeducted: number;
  totalTdsDeposited: number;
  totalVariance: number;
  q1?: QuarterStatus;
  q2?: QuarterStatus;
  q3?: QuarterStatus;
  q4?: QuarterStatus;
}

export interface Form24QPreviewData {
  financialYear: string;
  quarter: string;
  totalEmployees: number;
  totalSalaryPaid: number;
  totalTdsDeducted: number;
  totalTdsDeposited: number;
  variance: number;
  challans: ChallanPreview[];
  employees: EmployeePreview[];
}

export interface ChallanPreview {
  bsrCode: string;
  challanDate: string;
  challanSerial: string;
  amount: number;
  cin?: string;
}

export interface EmployeePreview {
  employeeId: string;
  employeeName: string;
  pan: string;
  grossSalary: number;
  tdsDeducted: number;
}

export interface Form24QValidationResult {
  isValid: boolean;
  errors: ValidationError[];
  warnings: ValidationWarning[];
}

export interface ValidationError {
  code: string;
  message: string;
  field?: string;
  recordId?: string;
}

export interface ValidationWarning {
  code: string;
  message: string;
  field?: string;
  recordId?: string;
}

export interface Form24QFilingFilterParams {
  pageNumber?: number;
  pageSize?: number;
  financialYear?: string;
  quarter?: string;
  status?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface CreateForm24QFilingRequest {
  companyId: string;
  financialYear: string;
  quarter: string;
  createdBy?: string;
}

export interface RecordAcknowledgementRequest {
  acknowledgementNumber: string;
  tokenNumber?: string;
  filingDate?: string;
  updatedBy?: string;
}

export interface RejectFilingRequest {
  rejectionReason: string;
  updatedBy?: string;
}

// ==================== Service ====================

/**
 * Form 24Q Filing Service - Handles quarterly TDS return filing operations
 * Backend Controller: /api/tax/form-24q-filings
 */
export class Form24QFilingService {
  private readonly endpoint = 'tax/form-24q-filings';

  /**
   * Get paginated list of Form 24Q filings for a company
   * Backend: GET /api/tax/form-24q-filings/company/{companyId}
   */
  async getPaged(
    companyId: string,
    params?: Form24QFilingFilterParams
  ): Promise<PagedResponse<Form24QFilingSummary>> {
    const queryParams = new URLSearchParams();
    if (params?.pageNumber) queryParams.set('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString());
    if (params?.financialYear) queryParams.set('financialYear', params.financialYear);
    if (params?.quarter) queryParams.set('quarter', params.quarter);
    if (params?.status) queryParams.set('status', params.status);
    if (params?.sortBy) queryParams.set('sortBy', params.sortBy);
    if (params?.sortDescending) queryParams.set('sortDescending', params.sortDescending.toString());

    const query = queryParams.toString();
    return apiClient.get<PagedResponse<Form24QFilingSummary>>(
      `${this.endpoint}/company/${companyId}${query ? `?${query}` : ''}`
    );
  }

  /**
   * Get Form 24Q filing by ID
   * Backend: GET /api/tax/form-24q-filings/{id}
   */
  async getById(id: string): Promise<Form24QFiling> {
    return apiClient.get<Form24QFiling>(`${this.endpoint}/${id}`);
  }

  /**
   * Get Form 24Q filing for specific company/FY/quarter
   * Backend: GET /api/tax/form-24q-filings/company/{companyId}/{financialYear}/{quarter}
   */
  async getByCompanyQuarter(
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<Form24QFiling> {
    return apiClient.get<Form24QFiling>(
      `${this.endpoint}/company/${companyId}/${financialYear}/${quarter}`
    );
  }

  /**
   * Get Form 24Q filing statistics for a financial year
   * Backend: GET /api/tax/form-24q-filings/company/{companyId}/statistics/{financialYear}
   */
  async getStatistics(companyId: string, financialYear: string): Promise<Form24QFilingStatistics> {
    return apiClient.get<Form24QFilingStatistics>(
      `${this.endpoint}/company/${companyId}/statistics/${financialYear}`
    );
  }

  /**
   * Get all filings for a financial year
   * Backend: GET /api/tax/form-24q-filings/company/{companyId}/year/{financialYear}
   */
  async getByFinancialYear(
    companyId: string,
    financialYear: string
  ): Promise<Form24QFilingSummary[]> {
    return apiClient.get<Form24QFilingSummary[]>(
      `${this.endpoint}/company/${companyId}/year/${financialYear}`
    );
  }

  /**
   * Get pending filings for a financial year
   * Backend: GET /api/tax/form-24q-filings/company/{companyId}/pending/{financialYear}
   */
  async getPendingFilings(
    companyId: string,
    financialYear: string
  ): Promise<Form24QFilingSummary[]> {
    return apiClient.get<Form24QFilingSummary[]>(
      `${this.endpoint}/company/${companyId}/pending/${financialYear}`
    );
  }

  /**
   * Get overdue filings
   * Backend: GET /api/tax/form-24q-filings/company/{companyId}/overdue
   */
  async getOverdueFilings(companyId: string): Promise<Form24QFilingSummary[]> {
    return apiClient.get<Form24QFilingSummary[]>(`${this.endpoint}/company/${companyId}/overdue`);
  }

  /**
   * Preview Form 24Q data without saving
   * Backend: GET /api/tax/form-24q-filings/company/{companyId}/preview/{financialYear}/{quarter}
   */
  async preview(
    companyId: string,
    financialYear: string,
    quarter: string
  ): Promise<Form24QPreviewData> {
    return apiClient.get<Form24QPreviewData>(
      `${this.endpoint}/company/${companyId}/preview/${financialYear}/${quarter}`
    );
  }

  /**
   * Create a draft Form 24Q filing
   * Backend: POST /api/tax/form-24q-filings
   */
  async createDraft(request: CreateForm24QFilingRequest): Promise<Form24QFiling> {
    return apiClient.post<Form24QFiling>(this.endpoint, request);
  }

  /**
   * Refresh filing data from current payroll transactions
   * Backend: POST /api/tax/form-24q-filings/{id}/refresh
   */
  async refreshData(id: string, updatedBy?: string): Promise<Form24QFiling> {
    const query = updatedBy ? `?updatedBy=${updatedBy}` : '';
    return apiClient.post<Form24QFiling>(`${this.endpoint}/${id}/refresh${query}`, {});
  }

  /**
   * Validate Form 24Q filing data
   * Backend: POST /api/tax/form-24q-filings/{id}/validate
   */
  async validate(id: string): Promise<Form24QValidationResult> {
    return apiClient.post<Form24QValidationResult>(`${this.endpoint}/${id}/validate`, {});
  }

  /**
   * Generate FVU file for NSDL upload
   * Backend: POST /api/tax/form-24q-filings/{id}/generate-fvu
   */
  async generateFvu(id: string, generatedBy?: string): Promise<Form24QFiling> {
    const query = generatedBy ? `?generatedBy=${generatedBy}` : '';
    return apiClient.post<Form24QFiling>(`${this.endpoint}/${id}/generate-fvu${query}`, {});
  }

  /**
   * Download the generated FVU file
   * Backend: GET /api/tax/form-24q-filings/{id}/download-fvu
   */
  async downloadFvu(id: string): Promise<Blob> {
    const response = await fetch(`/api/${this.endpoint}/${id}/download-fvu`, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('admin_access_token')}`,
      },
    });
    if (!response.ok) {
      throw new Error('Failed to download FVU file');
    }
    return response.blob();
  }

  /**
   * Mark filing as submitted to NSDL
   * Backend: POST /api/tax/form-24q-filings/{id}/submit
   */
  async markAsSubmitted(
    id: string,
    filingDate?: string,
    submittedBy?: string
  ): Promise<Form24QFiling> {
    return apiClient.post<Form24QFiling>(`${this.endpoint}/${id}/submit`, {
      filingDate,
      submittedBy,
    });
  }

  /**
   * Record acknowledgement from NSDL
   * Backend: POST /api/tax/form-24q-filings/{id}/acknowledge
   */
  async recordAcknowledgement(
    id: string,
    request: RecordAcknowledgementRequest
  ): Promise<Form24QFiling> {
    return apiClient.post<Form24QFiling>(`${this.endpoint}/${id}/acknowledge`, request);
  }

  /**
   * Mark filing as rejected by NSDL
   * Backend: POST /api/tax/form-24q-filings/{id}/reject
   */
  async markAsRejected(id: string, request: RejectFilingRequest): Promise<Form24QFiling> {
    return apiClient.post<Form24QFiling>(`${this.endpoint}/${id}/reject`, request);
  }

  /**
   * Create a correction return based on an original filing
   * Backend: POST /api/tax/form-24q-filings/{id}/create-correction
   */
  async createCorrection(id: string, createdBy?: string): Promise<Form24QFiling> {
    const query = createdBy ? `?createdBy=${createdBy}` : '';
    return apiClient.post<Form24QFiling>(`${this.endpoint}/${id}/create-correction${query}`, {});
  }

  /**
   * Get correction returns for an original filing
   * Backend: GET /api/tax/form-24q-filings/{id}/corrections
   */
  async getCorrections(id: string): Promise<Form24QFilingSummary[]> {
    return apiClient.get<Form24QFilingSummary[]>(`${this.endpoint}/${id}/corrections`);
  }

  /**
   * Delete a draft filing
   * Backend: DELETE /api/tax/form-24q-filings/{id}
   */
  async deleteDraft(id: string): Promise<void> {
    await apiClient.delete(`${this.endpoint}/${id}`);
  }
}

export const form24QFilingService = new Form24QFilingService();

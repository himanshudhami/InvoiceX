import { apiClient } from '../../client';
import type {
  Form16Data,
  Form16Summary,
  GenerateForm16Request,
  Form16FilterParams,
  PagedResponse,
} from '../../types';

/**
 * Form 16 Service - Handles TDS Certificate generation for employees
 * Follows SRP: Single responsibility for Form 16 API operations
 *
 * Backend Controller: /api/tax/form16
 */
export class Form16Service {
  private readonly endpoint = 'tax/form16';

  /**
   * Get paginated list of Form 16 records for a company
   * Backend: GET /api/tax/form16/{companyId}/list
   */
  async getPaged(companyId: string, params?: Form16FilterParams): Promise<PagedResponse<Form16Data>> {
    const queryParams = new URLSearchParams();
    if (params?.pageNumber) queryParams.set('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString());
    if (params?.financialYear) queryParams.set('financialYear', params.financialYear);
    if (params?.status) queryParams.set('status', params.status);
    if (params?.searchTerm) queryParams.set('searchTerm', params.searchTerm);
    if (params?.sortBy) queryParams.set('sortBy', params.sortBy);
    if (params?.sortDescending) queryParams.set('sortDescending', params.sortDescending.toString());

    const query = queryParams.toString();
    return apiClient.get<PagedResponse<Form16Data>>(
      `${this.endpoint}/${companyId}/list${query ? `?${query}` : ''}`
    );
  }

  /**
   * Get Form 16 by ID
   * Backend: GET /api/tax/form16/{id}
   */
  async getById(id: string): Promise<Form16Data> {
    return apiClient.get<Form16Data>(`${this.endpoint}/${id}`);
  }

  /**
   * Get Form 16 for specific employee and financial year
   * Backend: GET /api/tax/form16/{companyId}/employee/{employeeId}/{financialYear}
   */
  async getByEmployeeAndFY(companyId: string, employeeId: string, financialYear: string): Promise<Form16Data> {
    return apiClient.get<Form16Data>(
      `${this.endpoint}/${companyId}/employee/${employeeId}/${financialYear}`
    );
  }

  /**
   * Get Form 16 statistics/summary for a company and financial year
   * Backend: GET /api/tax/form16/{companyId}/statistics/{financialYear}
   */
  async getSummary(companyId: string, financialYear: string): Promise<Form16Summary> {
    return apiClient.get<Form16Summary>(
      `${this.endpoint}/${companyId}/statistics/${financialYear}`
    );
  }

  /**
   * Preview Form 16 data without saving
   * Backend: GET /api/tax/form16/{companyId}/preview/{employeeId}/{financialYear}
   */
  async preview(companyId: string, employeeId: string, financialYear: string): Promise<Form16Data> {
    return apiClient.get<Form16Data>(
      `${this.endpoint}/${companyId}/preview/${employeeId}/${financialYear}`
    );
  }

  /**
   * Generate Form 16 for a single employee
   * Backend: POST /api/tax/form16/{companyId}/generate/{employeeId}/{financialYear}
   */
  async generateForEmployee(
    companyId: string,
    employeeId: string,
    financialYear: string,
    generatedBy?: string
  ): Promise<Form16Data> {
    const query = generatedBy ? `?generatedBy=${generatedBy}` : '';
    return apiClient.post<Form16Data>(
      `${this.endpoint}/${companyId}/generate/${employeeId}/${financialYear}${query}`,
      {}
    );
  }

  /**
   * Bulk generate Form 16 for all eligible employees
   * Backend: POST /api/tax/form16/{companyId}/generate-bulk/{financialYear}
   */
  async generateBulk(
    companyId: string,
    financialYear: string,
    regenerateExisting: boolean = false,
    generatedBy?: string
  ): Promise<{ successCount: number; failedCount: number; errors: string[] }> {
    const queryParams = new URLSearchParams();
    if (regenerateExisting) queryParams.set('regenerateExisting', 'true');
    if (generatedBy) queryParams.set('generatedBy', generatedBy);
    const query = queryParams.toString();

    return apiClient.post<{ successCount: number; failedCount: number; errors: string[] }>(
      `${this.endpoint}/${companyId}/generate-bulk/${financialYear}${query ? `?${query}` : ''}`,
      {}
    );
  }

  /**
   * Generate (legacy wrapper for bulk generation)
   */
  async generate(request: GenerateForm16Request): Promise<{ successCount: number; failedCount: number; errors: string[] }> {
    return this.generateBulk(
      request.companyId,
      request.financialYear,
      request.regenerateExisting ?? false
    );
  }

  /**
   * Regenerate Form 16 for a specific record
   * Backend: POST /api/tax/form16/{id}/regenerate
   */
  async regenerate(id: string, regeneratedBy?: string): Promise<Form16Data> {
    const query = regeneratedBy ? `?regeneratedBy=${regeneratedBy}` : '';
    return apiClient.post<Form16Data>(`${this.endpoint}/${id}/regenerate${query}`, {});
  }

  /**
   * Verify Form 16 (HR/Finance approval)
   * Backend: POST /api/tax/form16/{id}/verify
   */
  async verify(id: string, verifiedBy: string, place: string): Promise<Form16Data> {
    return apiClient.post<Form16Data>(`${this.endpoint}/${id}/verify`, {
      verifiedBy,
      place,
    });
  }

  /**
   * Mark Form 16 as issued
   * Backend: POST /api/tax/form16/{id}/issue
   * Note: Form 16 must be in 'generated' or 'verified' status
   */
  async markAsIssued(id: string, issuedBy?: string, sendEmail: boolean = false): Promise<Form16Data> {
    return apiClient.post<Form16Data>(`${this.endpoint}/${id}/issue`, {
      issuedBy: issuedBy || '00000000-0000-0000-0000-000000000000', // System user if not provided
      sendEmail,
      remarks: null,
    });
  }

  /**
   * Cancel Form 16
   * Backend: POST /api/tax/form16/{id}/cancel
   */
  async cancel(id: string, reason: string, cancelledBy: string): Promise<void> {
    return apiClient.post<void>(`${this.endpoint}/${id}/cancel`, {
      reason,
      cancelledBy,
    });
  }

  /**
   * Validate if Form 16 can be generated for an employee
   * Backend: GET /api/tax/form16/{companyId}/validate/{employeeId}/{financialYear}
   */
  async validate(companyId: string, employeeId: string, financialYear: string): Promise<{
    isValid: boolean;
    errors: string[];
    warnings: string[];
  }> {
    return apiClient.get<{ isValid: boolean; errors: string[]; warnings: string[] }>(
      `${this.endpoint}/${companyId}/validate/${employeeId}/${financialYear}`
    );
  }

  /**
   * Check if Form 16 can be generated
   * Backend: GET /api/tax/form16/{companyId}/can-generate/{employeeId}/{financialYear}
   */
  async canGenerate(companyId: string, employeeId: string, financialYear: string): Promise<boolean> {
    return apiClient.get<boolean>(
      `${this.endpoint}/${companyId}/can-generate/${employeeId}/${financialYear}`
    );
  }

  /**
   * Generate PDF for Form 16
   * Backend: POST /api/tax/form16/{id}/generate-pdf
   */
  async generatePdf(id: string, generatedBy?: string): Promise<{ pdfPath: string }> {
    const query = generatedBy ? `?generatedBy=${generatedBy}` : '';
    return apiClient.post<{ pdfPath: string }>(
      `${this.endpoint}/${id}/generate-pdf${query}`,
      {}
    );
  }

  /**
   * Bulk generate PDFs
   * Backend: POST /api/tax/form16/{companyId}/generate-pdf-bulk/{financialYear}
   */
  async generateBulkPdf(
    companyId: string,
    financialYear: string,
    generatedBy?: string
  ): Promise<{ successCount: number; failedCount: number }> {
    const query = generatedBy ? `?generatedBy=${generatedBy}` : '';
    return apiClient.post<{ successCount: number; failedCount: number }>(
      `${this.endpoint}/${companyId}/generate-pdf-bulk/${financialYear}${query}`,
      {}
    );
  }

  /**
   * Download Form 16 PDF
   * Backend: GET /api/tax/form16/{id}/download
   */
  async downloadPdf(id: string): Promise<Blob> {
    const response = await fetch(`/api/${this.endpoint}/${id}/download`, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('admin_access_token')}`,
      },
    });
    if (!response.ok) {
      throw new Error('Failed to download PDF');
    }
    return response.blob();
  }

  /**
   * Get download URL for Form 16
   */
  getDownloadUrl(id: string): string {
    return `/api/${this.endpoint}/${id}/download`;
  }
}

export const form16Service = new Form16Service();

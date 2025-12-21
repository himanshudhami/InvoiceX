import { apiClient } from '../client';
import {
  FircTracking,
  CreateFircDto,
  UpdateFircDto,
  FircAutoMatchResult,
  RealizationAlert,
  PagedResponse,
  FilterParams
} from '../types';

export interface FircFilterParams extends FilterParams {
  companyId?: string;
  edpmsReported?: boolean;
  fromDate?: string;
  toDate?: string;
  currency?: string;
}

export class FircService {
  private readonly endpoint = 'fircs';

  async getPaged(params?: FircFilterParams): Promise<PagedResponse<FircTracking>> {
    return apiClient.getPaged<FircTracking>(this.endpoint, params);
  }

  async getAll(): Promise<FircTracking[]> {
    return apiClient.get<FircTracking[]>(this.endpoint);
  }

  async getById(id: string): Promise<FircTracking> {
    return apiClient.get<FircTracking>(`${this.endpoint}/${id}`);
  }

  async getByCompanyId(companyId: string): Promise<FircTracking[]> {
    return apiClient.get<FircTracking[]>(`${this.endpoint}/by-company/${companyId}`);
  }

  async create(data: CreateFircDto): Promise<FircTracking> {
    return apiClient.post<FircTracking>(this.endpoint, data);
  }

  async update(id: string, data: UpdateFircDto): Promise<FircTracking> {
    return apiClient.put<FircTracking>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  // Link FIRC to payment
  async linkToPayment(fircId: string, paymentId: string): Promise<FircTracking> {
    return apiClient.post<FircTracking>(`${this.endpoint}/${fircId}/link-payment/${paymentId}`, {});
  }

  // Link FIRC to invoices
  async linkToInvoices(fircId: string, invoiceIds: string[]): Promise<FircTracking> {
    return apiClient.post<FircTracking>(`${this.endpoint}/${fircId}/link-invoices`, { invoiceIds });
  }

  // Mark as EDPMS reported
  async markEdpmsReported(fircId: string, reportDate?: string): Promise<FircTracking> {
    return apiClient.put<FircTracking>(`${this.endpoint}/${fircId}/mark-edpms-reported`, {
      edpmsReportDate: reportDate || new Date().toISOString().split('T')[0]
    });
  }

  // Auto-match FIRCs with payments
  async autoMatch(companyId: string): Promise<FircAutoMatchResult[]> {
    return apiClient.post<FircAutoMatchResult[]>(`${this.endpoint}/auto-match/${companyId}`, {});
  }

  // Get realization alerts (approaching/overdue FEMA deadline)
  async getRealizationAlerts(companyId: string): Promise<RealizationAlert[]> {
    return apiClient.get<RealizationAlert[]>(`${this.endpoint}/realization-alerts/${companyId}`);
  }

  // Get EDPMS compliance summary
  async getEdpmsComplianceSummary(companyId: string): Promise<{
    totalFircs: number;
    reported: number;
    pending: number;
    complianceRate: number;
  }> {
    return apiClient.get(`${this.endpoint}/edpms-summary/${companyId}`);
  }

  // Get FIRCs pending reconciliation
  async getPendingReconciliation(companyId: string): Promise<FircTracking[]> {
    return apiClient.get<FircTracking[]>(`${this.endpoint}/pending-reconciliation/${companyId}`);
  }
}

export const fircService = new FircService();

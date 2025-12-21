import { apiClient } from '../client';
import {
  LutRegister,
  CreateLutDto,
  UpdateLutDto,
  LutValidationResult,
  LutExpiryAlert,
  PagedResponse,
  FilterParams
} from '../types';

export interface LutFilterParams extends FilterParams {
  companyId?: string;
  financialYear?: string;
  status?: 'active' | 'expired' | 'superseded';
}

export interface LutUtilizationReport {
  lutId: string;
  lutNumber: string;
  financialYear: string;
  validFrom: string;
  validTo: string;
  totalExportInvoices: number;
  totalExportValue: number;
  totalExportValueInr: number;
  invoices: Array<{
    invoiceId: string;
    invoiceNumber: string;
    invoiceDate: string;
    customerName: string;
    currency: string;
    amount: number;
    amountInr: number;
  }>;
}

export class LutService {
  private readonly endpoint = 'luts';

  async getPaged(params?: LutFilterParams): Promise<PagedResponse<LutRegister>> {
    return apiClient.getPaged<LutRegister>(this.endpoint, params);
  }

  async getAll(): Promise<LutRegister[]> {
    return apiClient.get<LutRegister[]>(this.endpoint);
  }

  async getById(id: string): Promise<LutRegister> {
    return apiClient.get<LutRegister>(`${this.endpoint}/${id}`);
  }

  async getByCompanyId(companyId: string): Promise<LutRegister[]> {
    return apiClient.get<LutRegister[]>(`${this.endpoint}/by-company/${companyId}`);
  }

  async create(data: CreateLutDto): Promise<LutRegister> {
    return apiClient.post<LutRegister>(this.endpoint, data);
  }

  async update(id: string, data: UpdateLutDto): Promise<LutRegister> {
    return apiClient.put<LutRegister>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  // Get active LUT for a company
  async getActiveLut(companyId: string): Promise<LutRegister | null> {
    try {
      return await apiClient.get<LutRegister>(`${this.endpoint}/active/${companyId}`);
    } catch {
      return null;
    }
  }

  // Validate LUT for a specific date
  async validateForDate(companyId: string, date: string): Promise<LutValidationResult> {
    return apiClient.get<LutValidationResult>(`${this.endpoint}/validate/${companyId}`, {
      date
    });
  }

  // Get expiry alerts for all companies or specific company
  async getExpiryAlerts(companyId?: string): Promise<LutExpiryAlert[]> {
    const url = companyId
      ? `${this.endpoint}/expiry-alerts/${companyId}`
      : `${this.endpoint}/expiry-alerts`;
    return apiClient.get<LutExpiryAlert[]>(url);
  }

  // Get utilization report for a specific LUT
  async getUtilizationReport(lutId: string): Promise<LutUtilizationReport> {
    return apiClient.get<LutUtilizationReport>(`${this.endpoint}/${lutId}/utilization`);
  }

  // Renew an existing LUT (create new one with reference to old)
  async renewLut(lutId: string, newLutData: CreateLutDto): Promise<LutRegister> {
    return apiClient.post<LutRegister>(`${this.endpoint}/${lutId}/renew`, newLutData);
  }

  // Get compliance summary
  async getComplianceSummary(companyId: string): Promise<{
    hasActiveLut: boolean;
    activeLut: LutRegister | null;
    daysToExpiry: number | null;
    totalLuts: number;
    exportInvoicesWithoutLut: number;
  }> {
    return apiClient.get(`${this.endpoint}/compliance-summary/${companyId}`);
  }
}

export const lutService = new LutService();

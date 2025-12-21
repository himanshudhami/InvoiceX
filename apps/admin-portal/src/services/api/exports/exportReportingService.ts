import { apiClient } from '../client';
import {
  ExportReceivablesAgeingReport,
  CustomerExportReceivable,
  ForexGainLossReport,
  UnrealizedForexPosition,
  FemaComplianceDashboard,
  FemaViolationAlert,
  ExportRealizationReport,
  MonthlyRealizationTrend,
  ExportDashboard,
  Gstr1ExportData
} from '../types';

export class ExportReportingService {
  private readonly endpoint = 'exportreporting';

  // ==================== Receivables Ageing ====================

  /**
   * Get export receivables ageing report (in foreign currency + INR)
   */
  async getReceivablesAgeing(
    companyId: string,
    asOfDate?: string
  ): Promise<ExportReceivablesAgeingReport> {
    const params = asOfDate ? { asOfDate } : {};
    return apiClient.get<ExportReceivablesAgeingReport>(
      `${this.endpoint}/receivables-ageing/${companyId}`,
      params
    );
  }

  /**
   * Get customer-wise export receivables
   */
  async getCustomerWiseReceivables(
    companyId: string,
    asOfDate?: string
  ): Promise<CustomerExportReceivable[]> {
    const params = asOfDate ? { asOfDate } : {};
    return apiClient.get<CustomerExportReceivable[]>(
      `${this.endpoint}/customer-receivables/${companyId}`,
      params
    );
  }

  // ==================== Forex Reports ====================

  /**
   * Get forex gain/loss report (realized + unrealized)
   */
  async getForexGainLossReport(
    companyId: string,
    fromDate: string,
    toDate: string
  ): Promise<ForexGainLossReport> {
    return apiClient.get<ForexGainLossReport>(
      `${this.endpoint}/forex-gain-loss/${companyId}`,
      { fromDate, toDate }
    );
  }

  /**
   * Get unrealized forex position (open receivables at current rates)
   */
  async getUnrealizedForexPosition(
    companyId: string,
    asOfDate: string,
    currentExchangeRate: number
  ): Promise<UnrealizedForexPosition> {
    return apiClient.get<UnrealizedForexPosition>(
      `${this.endpoint}/unrealized-forex/${companyId}`,
      { asOfDate, currentExchangeRate }
    );
  }

  // ==================== FEMA Compliance Dashboard ====================

  /**
   * Get FEMA compliance dashboard data
   */
  async getFemaComplianceDashboard(companyId: string): Promise<FemaComplianceDashboard> {
    return apiClient.get<FemaComplianceDashboard>(
      `${this.endpoint}/fema-dashboard/${companyId}`
    );
  }

  /**
   * Get FEMA violation alerts
   */
  async getFemaViolationAlerts(companyId: string): Promise<FemaViolationAlert[]> {
    return apiClient.get<FemaViolationAlert[]>(
      `${this.endpoint}/fema-violations/${companyId}`
    );
  }

  // ==================== Export Realization ====================

  /**
   * Get export realization tracking report
   */
  async getExportRealizationReport(
    companyId: string,
    financialYear?: string
  ): Promise<ExportRealizationReport> {
    const params = financialYear ? { financialYear } : {};
    return apiClient.get<ExportRealizationReport>(
      `${this.endpoint}/realization/${companyId}`,
      params
    );
  }

  /**
   * Get monthly export realization trend
   */
  async getRealizationTrend(
    companyId: string,
    months: number = 12
  ): Promise<MonthlyRealizationTrend[]> {
    return apiClient.get<MonthlyRealizationTrend[]>(
      `${this.endpoint}/realization-trend/${companyId}`,
      { months }
    );
  }

  // ==================== Combined Export Dashboard ====================

  /**
   * Get comprehensive export dashboard with all key metrics
   */
  async getExportDashboard(companyId: string): Promise<ExportDashboard> {
    return apiClient.get<ExportDashboard>(
      `${this.endpoint}/dashboard/${companyId}`
    );
  }

  // ==================== GSTR-1 Export Data ====================

  /**
   * Get GSTR-1 export data (Table 6A)
   */
  async getGstr1ExportData(
    companyId: string,
    returnPeriod: string
  ): Promise<Gstr1ExportData> {
    return apiClient.get<Gstr1ExportData>(
      `${this.endpoint}/gstr1-exports/${companyId}`,
      { returnPeriod }
    );
  }
}

export const exportReportingService = new ExportReportingService();

import { useQuery } from '@tanstack/react-query';
import { exportReportingService } from '@/services/api/exports/exportReportingService';

// Query keys for React Query cache management
export const exportReportKeys = {
  all: ['exportReports'] as const,
  dashboard: (companyId: string) => [...exportReportKeys.all, 'dashboard', companyId] as const,
  receivablesAgeing: (companyId: string, asOfDate?: string) =>
    [...exportReportKeys.all, 'receivablesAgeing', companyId, asOfDate] as const,
  customerReceivables: (companyId: string, asOfDate?: string) =>
    [...exportReportKeys.all, 'customerReceivables', companyId, asOfDate] as const,
  forexGainLoss: (companyId: string, fromDate: string, toDate: string) =>
    [...exportReportKeys.all, 'forexGainLoss', companyId, fromDate, toDate] as const,
  unrealizedForex: (companyId: string, asOfDate: string, rate: number) =>
    [...exportReportKeys.all, 'unrealizedForex', companyId, asOfDate, rate] as const,
  femaCompliance: (companyId: string) =>
    [...exportReportKeys.all, 'femaCompliance', companyId] as const,
  femaAlerts: (companyId: string) =>
    [...exportReportKeys.all, 'femaAlerts', companyId] as const,
  realization: (companyId: string, fy?: string) =>
    [...exportReportKeys.all, 'realization', companyId, fy] as const,
  realizationTrend: (companyId: string, months: number) =>
    [...exportReportKeys.all, 'realizationTrend', companyId, months] as const,
  gstr1Exports: (companyId: string, period: string) =>
    [...exportReportKeys.all, 'gstr1Exports', companyId, period] as const,
};

/**
 * Hook for fetching comprehensive export dashboard
 */
export const useExportDashboard = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: exportReportKeys.dashboard(companyId),
    queryFn: () => exportReportingService.getExportDashboard(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching export receivables ageing report
 */
export const useExportReceivablesAgeing = (
  companyId: string,
  asOfDate?: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: exportReportKeys.receivablesAgeing(companyId, asOfDate),
    queryFn: () => exportReportingService.getReceivablesAgeing(companyId, asOfDate),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching customer-wise export receivables
 */
export const useCustomerExportReceivables = (
  companyId: string,
  asOfDate?: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: exportReportKeys.customerReceivables(companyId, asOfDate),
    queryFn: () => exportReportingService.getCustomerWiseReceivables(companyId, asOfDate),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching forex gain/loss report
 */
export const useForexGainLossReport = (
  companyId: string,
  fromDate: string,
  toDate: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: exportReportKeys.forexGainLoss(companyId, fromDate, toDate),
    queryFn: () => exportReportingService.getForexGainLossReport(companyId, fromDate, toDate),
    enabled: enabled && !!companyId && !!fromDate && !!toDate,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching unrealized forex position
 */
export const useUnrealizedForexPosition = (
  companyId: string,
  asOfDate: string,
  currentExchangeRate: number,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: exportReportKeys.unrealizedForex(companyId, asOfDate, currentExchangeRate),
    queryFn: () => exportReportingService.getUnrealizedForexPosition(
      companyId,
      asOfDate,
      currentExchangeRate
    ),
    enabled: enabled && !!companyId && !!asOfDate && currentExchangeRate > 0,
    staleTime: 1 * 60 * 1000, // Shorter cache for forex positions
    gcTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching FEMA compliance dashboard
 */
export const useFemaComplianceDashboard = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: exportReportKeys.femaCompliance(companyId),
    queryFn: () => exportReportingService.getFemaComplianceDashboard(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching FEMA violation alerts
 */
export const useFemaViolationAlerts = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: exportReportKeys.femaAlerts(companyId),
    queryFn: () => exportReportingService.getFemaViolationAlerts(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching export realization report
 */
export const useExportRealizationReport = (
  companyId: string,
  financialYear?: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: exportReportKeys.realization(companyId, financialYear),
    queryFn: () => exportReportingService.getExportRealizationReport(companyId, financialYear),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching monthly realization trend
 */
export const useRealizationTrend = (
  companyId: string,
  months: number = 12,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: exportReportKeys.realizationTrend(companyId, months),
    queryFn: () => exportReportingService.getRealizationTrend(companyId, months),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook for fetching GSTR-1 export data
 */
export const useGstr1ExportData = (
  companyId: string,
  returnPeriod: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: exportReportKeys.gstr1Exports(companyId, returnPeriod),
    queryFn: () => exportReportingService.getGstr1ExportData(companyId, returnPeriod),
    enabled: enabled && !!companyId && !!returnPeriod,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

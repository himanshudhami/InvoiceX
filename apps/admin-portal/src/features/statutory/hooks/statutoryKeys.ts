/**
 * React Query keys for statutory compliance features
 * Follows consistent key structure for cache management
 */

export const statutoryKeys = {
  // Root keys
  all: ['statutory'] as const,

  // Form 16 keys
  form16: {
    all: ['statutory', 'form16'] as const,
    lists: () => [...statutoryKeys.form16.all, 'list'] as const,
    list: (params?: Record<string, any>) => [...statutoryKeys.form16.lists(), params ?? {}] as const,
    details: () => [...statutoryKeys.form16.all, 'detail'] as const,
    detail: (id: string) => [...statutoryKeys.form16.details(), id] as const,
    byEmployee: (employeeId: string, fy: string) =>
      [...statutoryKeys.form16.all, 'employee', employeeId, fy] as const,
    byCompany: (companyId: string, fy: string) =>
      [...statutoryKeys.form16.all, 'company', companyId, fy] as const,
    summary: (companyId: string, fy: string) =>
      [...statutoryKeys.form16.all, 'summary', companyId, fy] as const,
  },

  // TDS Challan keys
  tdsChallan: {
    all: ['statutory', 'tds-challan'] as const,
    lists: () => [...statutoryKeys.tdsChallan.all, 'list'] as const,
    list: (params?: Record<string, any>) => [...statutoryKeys.tdsChallan.lists(), params ?? {}] as const,
    details: () => [...statutoryKeys.tdsChallan.all, 'detail'] as const,
    detail: (id: string) => [...statutoryKeys.tdsChallan.details(), id] as const,
    pending: (companyId: string, fy?: string) =>
      [...statutoryKeys.tdsChallan.all, 'pending', companyId, fy] as const,
    summary: (companyId: string, fy: string) =>
      [...statutoryKeys.tdsChallan.all, 'summary', companyId, fy] as const,
    preview: (companyId: string, tdsType: string, year: number, month: number) =>
      [...statutoryKeys.tdsChallan.all, 'preview', companyId, tdsType, year, month] as const,
  },

  // PF ECR keys
  pfEcr: {
    all: ['statutory', 'pf-ecr'] as const,
    lists: () => [...statutoryKeys.pfEcr.all, 'list'] as const,
    list: (params?: Record<string, any>) => [...statutoryKeys.pfEcr.lists(), params ?? {}] as const,
    details: () => [...statutoryKeys.pfEcr.all, 'detail'] as const,
    detail: (id: string) => [...statutoryKeys.pfEcr.details(), id] as const,
    generate: (companyId: string, year: number, month: number) =>
      [...statutoryKeys.pfEcr.all, 'generate', companyId, year, month] as const,
    preview: (companyId: string, year: number, month: number) =>
      [...statutoryKeys.pfEcr.all, 'preview', companyId, year, month] as const,
    pending: (companyId: string, fy?: string) =>
      [...statutoryKeys.pfEcr.all, 'pending', companyId, fy] as const,
    filed: (companyId: string, fy: string) =>
      [...statutoryKeys.pfEcr.all, 'filed', companyId, fy] as const,
    summary: (companyId: string, fy: string) =>
      [...statutoryKeys.pfEcr.all, 'summary', companyId, fy] as const,
    reconcile: (companyId: string, fy: string) =>
      [...statutoryKeys.pfEcr.all, 'reconcile', companyId, fy] as const,
  },

  // ESI Return keys
  esiReturn: {
    all: ['statutory', 'esi-return'] as const,
    lists: () => [...statutoryKeys.esiReturn.all, 'list'] as const,
    list: (params?: Record<string, any>) => [...statutoryKeys.esiReturn.lists(), params ?? {}] as const,
    details: () => [...statutoryKeys.esiReturn.all, 'detail'] as const,
    detail: (id: string) => [...statutoryKeys.esiReturn.details(), id] as const,
    generate: (companyId: string, year: number, month: number) =>
      [...statutoryKeys.esiReturn.all, 'generate', companyId, year, month] as const,
    preview: (companyId: string, year: number, month: number) =>
      [...statutoryKeys.esiReturn.all, 'preview', companyId, year, month] as const,
    pending: (companyId: string, fy?: string) =>
      [...statutoryKeys.esiReturn.all, 'pending', companyId, fy] as const,
    filed: (companyId: string, fy: string) =>
      [...statutoryKeys.esiReturn.all, 'filed', companyId, fy] as const,
    summary: (companyId: string, fy: string) =>
      [...statutoryKeys.esiReturn.all, 'summary', companyId, fy] as const,
    reconcile: (companyId: string, fy: string) =>
      [...statutoryKeys.esiReturn.all, 'reconcile', companyId, fy] as const,
  },

  // Form 24Q Filing keys (Quarterly TDS Return for Salary)
  form24QFiling: {
    all: ['statutory', 'form-24q-filing'] as const,
    lists: () => [...statutoryKeys.form24QFiling.all, 'list'] as const,
    list: (params?: Record<string, any>) => [...statutoryKeys.form24QFiling.lists(), params ?? {}] as const,
    details: () => [...statutoryKeys.form24QFiling.all, 'detail'] as const,
    detail: (id: string) => [...statutoryKeys.form24QFiling.details(), id] as const,
    byCompanyQuarter: (companyId: string, fy: string, quarter: string) =>
      [...statutoryKeys.form24QFiling.all, 'company', companyId, fy, quarter] as const,
    byFinancialYear: (companyId: string, fy: string) =>
      [...statutoryKeys.form24QFiling.all, 'year', companyId, fy] as const,
    statistics: (companyId: string, fy: string) =>
      [...statutoryKeys.form24QFiling.all, 'statistics', companyId, fy] as const,
    pending: (companyId: string, fy: string) =>
      [...statutoryKeys.form24QFiling.all, 'pending', companyId, fy] as const,
    overdue: (companyId: string) =>
      [...statutoryKeys.form24QFiling.all, 'overdue', companyId] as const,
    preview: (companyId: string, fy: string, quarter: string) =>
      [...statutoryKeys.form24QFiling.all, 'preview', companyId, fy, quarter] as const,
    corrections: (id: string) =>
      [...statutoryKeys.form24QFiling.all, 'corrections', id] as const,
  },
};

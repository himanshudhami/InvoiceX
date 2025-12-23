/**
 * Query keys for GST Compliance features
 * Following the factory pattern for consistent cache management
 */
export const gstKeys = {
  // ITC Blocked
  itcBlocked: {
    all: ['itc-blocked'] as const,
    categories: () => [...gstKeys.itcBlocked.all, 'categories'] as const,
    summary: (companyId: string, returnPeriod: string) =>
      [...gstKeys.itcBlocked.all, 'summary', companyId, returnPeriod] as const,
  },

  // ITC Reversal
  itcReversal: {
    all: ['itc-reversal'] as const,
    calculation: (params: object) => [...gstKeys.itcReversal.all, 'calculation', params] as const,
  },

  // ITC Reports
  itcReports: {
    all: ['itc-reports'] as const,
    availability: (companyId: string, returnPeriod: string) =>
      [...gstKeys.itcReports.all, 'availability', companyId, returnPeriod] as const,
  },

  // TDS Returns
  tdsReturns: {
    all: ['tds-returns'] as const,
    form26Q: (companyId: string, fy: string, quarter: string) =>
      [...gstKeys.tdsReturns.all, '26q', companyId, fy, quarter] as const,
    form26QSummary: (companyId: string, fy: string, quarter: string) =>
      [...gstKeys.tdsReturns.all, '26q-summary', companyId, fy, quarter] as const,
    form26QValidation: (companyId: string, fy: string, quarter: string) =>
      [...gstKeys.tdsReturns.all, '26q-validation', companyId, fy, quarter] as const,
    form24Q: (companyId: string, fy: string, quarter: string) =>
      [...gstKeys.tdsReturns.all, '24q', companyId, fy, quarter] as const,
    form24QSummary: (companyId: string, fy: string, quarter: string) =>
      [...gstKeys.tdsReturns.all, '24q-summary', companyId, fy, quarter] as const,
    form24QValidation: (companyId: string, fy: string, quarter: string) =>
      [...gstKeys.tdsReturns.all, '24q-validation', companyId, fy, quarter] as const,
    annexureII: (companyId: string, fy: string) =>
      [...gstKeys.tdsReturns.all, 'annexure-ii', companyId, fy] as const,
    dueDates: (fy: string) => [...gstKeys.tdsReturns.all, 'due-dates', fy] as const,
    pending: (companyId: string) => [...gstKeys.tdsReturns.all, 'pending', companyId] as const,
    filingHistory: (companyId: string, fy?: string) =>
      [...gstKeys.tdsReturns.all, 'filing-history', companyId, fy] as const,
    challans: (companyId: string, fy: string, quarter: string) =>
      [...gstKeys.tdsReturns.all, 'challans', companyId, fy, quarter] as const,
    reconciliation: (companyId: string, fy: string, quarter: string) =>
      [...gstKeys.tdsReturns.all, 'reconciliation', companyId, fy, quarter] as const,
    combinedSummary: (companyId: string, fy: string, quarter: string) =>
      [...gstKeys.tdsReturns.all, 'combined-summary', companyId, fy, quarter] as const,
  },

  // RCM
  rcm: {
    all: ['rcm'] as const,
    lists: () => [...gstKeys.rcm.all, 'list'] as const,
    list: (companyId?: string) => [...gstKeys.rcm.lists(), companyId] as const,
    paged: (params: object) => [...gstKeys.rcm.all, 'paged', params] as const,
    details: () => [...gstKeys.rcm.all, 'detail'] as const,
    detail: (id: string) => [...gstKeys.rcm.details(), id] as const,
    pending: (companyId: string) => [...gstKeys.rcm.all, 'pending', companyId] as const,
    pendingItc: (companyId: string) => [...gstKeys.rcm.all, 'pending-itc', companyId] as const,
    summary: (companyId: string, returnPeriod: string) =>
      [...gstKeys.rcm.all, 'summary', companyId, returnPeriod] as const,
  },

  // LDC (Lower Deduction Certificates)
  ldc: {
    all: ['ldc'] as const,
    lists: () => [...gstKeys.ldc.all, 'list'] as const,
    list: (companyId?: string) => [...gstKeys.ldc.lists(), companyId] as const,
    paged: (params: object) => [...gstKeys.ldc.all, 'paged', params] as const,
    details: () => [...gstKeys.ldc.all, 'detail'] as const,
    detail: (id: string) => [...gstKeys.ldc.details(), id] as const,
    active: (companyId: string) => [...gstKeys.ldc.all, 'active', companyId] as const,
    expiring: (companyId: string) => [...gstKeys.ldc.all, 'expiring', companyId] as const,
    byPan: (companyId: string, pan: string) =>
      [...gstKeys.ldc.all, 'by-pan', companyId, pan] as const,
    usage: (certificateId: string) => [...gstKeys.ldc.all, 'usage', certificateId] as const,
  },

  // TCS
  tcs: {
    all: ['tcs'] as const,
    lists: () => [...gstKeys.tcs.all, 'list'] as const,
    list: (companyId?: string) => [...gstKeys.tcs.lists(), companyId] as const,
    paged: (params: object) => [...gstKeys.tcs.all, 'paged', params] as const,
    details: () => [...gstKeys.tcs.all, 'detail'] as const,
    detail: (id: string) => [...gstKeys.tcs.details(), id] as const,
    pending: (companyId: string) => [...gstKeys.tcs.all, 'pending', companyId] as const,
    summary: (companyId: string, fy: string, quarter?: string) =>
      [...gstKeys.tcs.all, 'summary', companyId, fy, quarter] as const,
    liability: (companyId: string, fy: string) =>
      [...gstKeys.tcs.all, 'liability', companyId, fy] as const,
  },
};

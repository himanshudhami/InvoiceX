/**
 * Query keys for Advance Tax (Section 207) features
 * Following the factory pattern for consistent cache management
 */
export const advanceTaxKeys = {
  all: ['advance-tax'] as const,

  // Assessments
  assessments: {
    all: () => [...advanceTaxKeys.all, 'assessment'] as const,
    detail: (id: string) => [...advanceTaxKeys.assessments.all(), id] as const,
    byCompanyFy: (companyId: string, financialYear: string) =>
      [...advanceTaxKeys.assessments.all(), 'company-fy', companyId, financialYear] as const,
    listByCompany: (companyId: string) =>
      [...advanceTaxKeys.assessments.all(), 'list', companyId] as const,
    pending: (companyId?: string) =>
      [...advanceTaxKeys.assessments.all(), 'pending', companyId] as const,
  },

  // Schedules
  schedules: {
    all: () => [...advanceTaxKeys.all, 'schedule'] as const,
    byAssessment: (assessmentId: string) =>
      [...advanceTaxKeys.schedules.all(), assessmentId] as const,
  },

  // Payments
  payments: {
    all: () => [...advanceTaxKeys.all, 'payment'] as const,
    byAssessment: (assessmentId: string) =>
      [...advanceTaxKeys.payments.all(), assessmentId] as const,
  },

  // Interest
  interest: {
    all: () => [...advanceTaxKeys.all, 'interest'] as const,
    breakdown: (assessmentId: string) =>
      [...advanceTaxKeys.interest.all(), 'breakdown', assessmentId] as const,
  },

  // Scenarios
  scenarios: {
    all: () => [...advanceTaxKeys.all, 'scenario'] as const,
    byAssessment: (assessmentId: string) =>
      [...advanceTaxKeys.scenarios.all(), assessmentId] as const,
  },

  // Tracker/Dashboard
  tracker: {
    all: () => [...advanceTaxKeys.all, 'tracker'] as const,
    byCompanyFy: (companyId: string, financialYear: string) =>
      [...advanceTaxKeys.tracker.all(), companyId, financialYear] as const,
  },

  // Tax Computation
  computation: {
    all: () => [...advanceTaxKeys.all, 'computation'] as const,
    byAssessment: (assessmentId: string) =>
      [...advanceTaxKeys.computation.all(), assessmentId] as const,
  },

  // YTD Preview
  ytdPreview: {
    all: () => [...advanceTaxKeys.all, 'ytd-preview'] as const,
    byCompanyFy: (companyId: string, financialYear: string) =>
      [...advanceTaxKeys.ytdPreview.all(), companyId, financialYear] as const,
  },
};

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

  // TDS/TCS Preview
  tdsTcsPreview: {
    all: () => [...advanceTaxKeys.all, 'tds-tcs-preview'] as const,
    byCompanyFy: (companyId: string, financialYear: string) =>
      [...advanceTaxKeys.tdsTcsPreview.all(), companyId, financialYear] as const,
  },

  // Revisions
  revisions: {
    all: () => [...advanceTaxKeys.all, 'revision'] as const,
    byAssessment: (assessmentId: string) =>
      [...advanceTaxKeys.revisions.all(), assessmentId] as const,
    status: (assessmentId: string) =>
      [...advanceTaxKeys.revisions.all(), 'status', assessmentId] as const,
  },

  // MAT Credit
  matCredit: {
    all: () => [...advanceTaxKeys.all, 'mat-credit'] as const,
    computation: (assessmentId: string) =>
      [...advanceTaxKeys.matCredit.all(), 'computation', assessmentId] as const,
    summary: (companyId: string, financialYear: string) =>
      [...advanceTaxKeys.matCredit.all(), 'summary', companyId, financialYear] as const,
    byCompany: (companyId: string) =>
      [...advanceTaxKeys.matCredit.all(), 'company', companyId] as const,
    utilizations: (matCreditId: string) =>
      [...advanceTaxKeys.matCredit.all(), 'utilizations', matCreditId] as const,
  },

  // Form 280 (Challan)
  form280: {
    all: () => [...advanceTaxKeys.all, 'form280'] as const,
    data: (assessmentId: string, quarter?: number) =>
      [...advanceTaxKeys.form280.all(), 'data', assessmentId, quarter] as const,
  },
};

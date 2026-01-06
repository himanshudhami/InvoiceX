// Export & Forex Types - FIRC, LUT, FEMA Compliance

// FIRC Tracking (Foreign Inward Remittance Certificate)
export interface FircTracking {
  id: string;
  companyId: string;
  paymentId?: string;
  fircNumber: string;
  fircDate: string;
  bankName: string;
  purposeCode?: string;
  foreignCurrency: string;
  foreignAmount: number;
  inrAmount: number;
  exchangeRate: number;
  beneficiaryName?: string;
  remitterName?: string;
  remitterCountry?: string;
  invoiceIds?: string[];
  edpmsReported: boolean;
  edpmsReportDate?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateFircDto {
  companyId: string;
  fircNumber: string;
  fircDate: string;
  bankName: string;
  purposeCode?: string;
  foreignCurrency: string;
  foreignAmount: number;
  inrAmount: number;
  exchangeRate: number;
  beneficiaryName?: string;
  remitterName?: string;
  remitterCountry?: string;
}

export interface UpdateFircDto {
  fircNumber?: string;
  fircDate?: string;
  bankName?: string;
  purposeCode?: string;
  foreignCurrency?: string;
  foreignAmount?: number;
  inrAmount?: number;
  exchangeRate?: number;
  beneficiaryName?: string;
  remitterName?: string;
  remitterCountry?: string;
  edpmsReported?: boolean;
  edpmsReportDate?: string;
}

export interface FircAutoMatchResult {
  fircId: string;
  matchedPaymentId?: string;
  matchedInvoiceIds: string[];
  matchConfidence: number;
  matchReason: string;
  isAutoMatched: boolean;
}

// LUT Register (Letter of Undertaking for GST Exports)
export interface LutRegister {
  id: string;
  companyId: string;
  lutNumber: string;
  financialYear: string;
  validFrom: string;
  validTo: string;
  gstin: string;
  filingDate?: string;
  arn?: string;
  status: 'active' | 'expired' | 'superseded';
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateLutDto {
  companyId: string;
  lutNumber: string;
  financialYear: string;
  validFrom: string;
  validTo: string;
  gstin: string;
  filingDate?: string;
  arn?: string;
}

export interface UpdateLutDto {
  lutNumber?: string;
  financialYear?: string;
  validFrom?: string;
  validTo?: string;
  gstin?: string;
  filingDate?: string;
  arn?: string;
  status?: 'active' | 'expired' | 'superseded';
}

export interface LutValidationResult {
  isValid: boolean;
  lutNumber?: string;
  validFrom?: string;
  validTo?: string;
  daysRemaining?: number;
  message: string;
}

export interface LutExpiryAlert {
  lutId: string;
  lutNumber: string;
  financialYear: string;
  validTo: string;
  daysToExpiry: number;
  severity: 'info' | 'warning' | 'critical';
  message: string;
}

// Export Receivables Ageing
export interface ExportReceivablesAgeingReport {
  companyId: string;
  asOfDate: string;
  current: number;
  currentInr: number;
  currentCount: number;
  days31To60: number;
  days31To60Inr: number;
  days31To60Count: number;
  days61To90: number;
  days61To90Inr: number;
  days61To90Count: number;
  days91To180: number;
  days91To180Inr: number;
  days91To180Count: number;
  days181To270: number;
  days181To270Inr: number;
  days181To270Count: number;
  over270Days: number;
  over270DaysInr: number;
  over270DaysCount: number;
  totalReceivablesForeign: number;
  totalReceivablesInr: number;
  invoices: AgeingInvoice[];
  currencyBreakdown: Record<string, CurrencyAgeing>;
}

export interface AgeingInvoice {
  invoiceId: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  daysOutstanding: number;
  customerId: string;
  customerName: string;
  currency: string;
  invoiceAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  outstandingAmountInr: number;
  ageingBucket: string;
  femaDeadline: string;
  daysToFemaDeadline: number;
  isFemaOverdue: boolean;
}

export interface CurrencyAgeing {
  currency: string;
  totalAmount: number;
  totalAmountInr: number;
  invoiceCount: number;
  current: number;
  days31To60: number;
  days61To90: number;
  over90Days: number;
}

export interface CustomerExportReceivable {
  customerId: string;
  customerName: string;
  country?: string;
  primaryCurrency: string;
  invoiceCount: number;
  totalInvoiced: number;
  totalPaid: number;
  totalOutstanding: number;
  totalOutstandingInr: number;
  oldestInvoiceDays: number;
  femaOverdueCount: number;
  femaOverdueAmount: number;
}

// Forex Gain/Loss Report
export interface ForexGainLossReport {
  companyId: string;
  fromDate: string;
  toDate: string;
  realizedGainTotal: number;
  realizedLossTotal: number;
  netRealizedGainLoss: number;
  unrealizedGainTotal: number;
  unrealizedLossTotal: number;
  netUnrealizedGainLoss: number;
  realizedTransactionCount: number;
  realizedTransactions: ForexTransactionDetail[];
  monthlyTrend: MonthlyForexSummary[];
}

export interface ForexTransactionDetail {
  transactionId: string;
  transactionDate: string;
  transactionType: string;
  documentNumber: string;
  currency: string;
  foreignAmount: number;
  bookingRate: number;
  settlementRate: number;
  bookingAmountInr: number;
  settlementAmountInr: number;
  gainLoss: number;
}

export interface MonthlyForexSummary {
  month: number;
  year: number;
  monthName: string;
  realizedGainLoss: number;
  unrealizedGainLoss: number;
  totalGainLoss: number;
  transactionCount: number;
}

export interface UnrealizedForexPosition {
  companyId: string;
  asOfDate: string;
  currentExchangeRate: number;
  totalOpenPositionForeign: number;
  totalOpenPositionInrAtBooking: number;
  totalOpenPositionInrAtCurrent: number;
  totalUnrealizedGainLoss: number;
  currencyBreakdown: Record<string, CurrencyForexPosition>;
}

export interface CurrencyForexPosition {
  currency: string;
  openAmount: number;
  avgBookingRate: number;
  currentRate: number;
  bookingValueInr: number;
  currentValueInr: number;
  unrealizedGainLoss: number;
}

// FEMA Compliance Dashboard
export interface FemaComplianceDashboard {
  companyId: string;
  complianceScore: number;
  overallStatus: 'compliant' | 'warning' | 'critical' | 'non_compliant';
  totalOpenInvoices: number;
  totalExportReceivables: number;
  fullyRealizedCount: number;
  fullyRealizedAmount: number;
  partiallyRealizedCount: number;
  partiallyRealizedAmount: number;
  pendingRealizationCount: number;
  pendingRealizationAmount: number;
  overdueCount: number;
  overdueAmount: number;
  fircsReceived: number;
  fircsPending: number;
  fircsCoverage: number;
  edpmsReported: number;
  edpmsPending: number;
  hasActiveLut: boolean;
  activeLutNumber?: string;
  daysToLutExpiry?: number;
  criticalAlerts: number;
  warningAlerts: number;
  topAlerts: FemaViolationAlert[];
}

export interface FemaViolationAlert {
  alertType: string;
  severity: 'info' | 'warning' | 'critical';
  title: string;
  description: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  documentNumber?: string;
  amount?: number;
  currency?: string;
  daysOverdue?: number;
}

// Export Realization Report
export interface ExportRealizationReport {
  companyId: string;
  financialYear: string;
  totalExportInvoices: number;
  totalExportValue: number;
  totalRealizedValue: number;
  totalPendingValue: number;
  realizationPercentage: number;
  avgRealizationDays: number;
  byStatus: RealizationStatusSummary[];
  byCustomer: CustomerRealization[];
  monthlyBreakdown: MonthlyRealization[];
  atRiskInvoices: AtRiskInvoice[];
}

export interface RealizationStatusSummary {
  status: string;
  count: number;
  amount: number;
}

export interface CustomerRealization {
  customerId: string;
  customerName: string;
  invoiceCount: number;
  totalExportValue: number;
  realizedValue: number;
  pendingValue: number;
  realizationPercentage: number;
  avgRealizationDays: number;
}

export interface MonthlyRealization {
  month: number;
  year: number;
  monthName: string;
  invoiceCount: number;
  invoicedAmount: number;
  realizedAmount: number;
  realizationPercentage: number;
}

export interface AtRiskInvoice {
  invoiceId: string;
  invoiceNumber: string;
  invoiceDate: string;
  femaDeadline: string;
  daysToDeadline: number;
  customerName: string;
  outstandingAmount: number;
  currency: string;
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
}

export interface MonthlyRealizationTrend {
  month: number;
  year: number;
  monthName: string;
  invoiced: number;
  realized: number;
  outstanding: number;
  realizationRate: number;
  avgDaysToRealize: number;
}

// Combined Export Dashboard
export interface ExportDashboard {
  companyId: string;
  totalExportReceivables: number;
  totalExportRevenueFy: number;
  totalRealizedFy: number;
  totalInvoicesFy: number;
  overdueInvoices: number;
  totalCustomers: number;
  netForexGainLossFy: number;
  unrealizedForexPosition: number;
  avgRealizationDays: number;
  femaComplianceScore: number;
  hasActiveLut: boolean;
  pendingFircs: number;
  criticalAlerts: number;
  warningAlerts: number;
  realizationTrend: MonthlyRealizationTrend[];
  forexTrend: MonthlyForexSummary[];
  receivablesByCustomer: Record<string, number>;
  receivablesByCurrency: Record<string, number>;
}

// GSTR-1 Export Types
export interface Gstr1ExportData {
  companyId: string;
  returnPeriod: string;
  exports: Gstr1Export[];
  totalInvoices: number;
  totalTaxableValue: number;
  totalIgst: number;
}

export interface Gstr1Export {
  invoiceNumber: string;
  invoiceDate: string;
  portCode?: string;
  shippingBillNumber?: string;
  shippingBillDate?: string;
  customerName: string;
  countryCode: string;
  currency: string;
  foreignValue: number;
  exchangeRate: number;
  taxableValue: number;
  igstRate: number;
  igstAmount: number;
  exportType: 'EXPWP' | 'EXPWOP';
  irnNumber?: string;
}

// Realization Alert
export interface RealizationAlert {
  invoiceId: string;
  invoiceNumber: string;
  invoiceDate: string;
  customerId: string;
  customerName: string;
  currency: string;
  invoiceAmount: number;
  outstandingAmount: number;
  outstandingAmountInr: number;
  femaDeadline: string;
  daysToDeadline: number;
  daysOverdue?: number;
  alertLevel: 'approaching' | 'imminent' | 'overdue';
  alertMessage: string;
}

// Tax Rule Pack Types

export interface TaxRulePack {
  id: string;
  packCode: string;
  packName: string;
  financialYear: string;
  version: number;
  sourceNotification?: string;
  description?: string;
  status: 'draft' | 'active' | 'superseded';
  incomeTaxSlabs?: Record<string, unknown>;
  standardDeductions?: Record<string, unknown>;
  rebateThresholds?: Record<string, unknown>;
  cessRates?: Record<string, unknown>;
  surchargeRates?: Record<string, unknown>;
  tdsRates?: Record<string, unknown>;
  pfEsiRates?: Record<string, unknown>;
  professionalTaxConfig?: Record<string, unknown>;
  gstRates?: Record<string, unknown>;
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
  activatedAt?: string;
  activatedBy?: string;
}

export interface CreateTaxRulePackDto {
  packCode: string;
  packName: string;
  financialYear: string;
  sourceNotification?: string;
  description?: string;
}

export interface TdsSectionRate {
  id: string;
  rulePackId: string;
  sectionCode: string;
  sectionName?: string;
  rateIndividual: number;
  rateCompany: number;
  rateNoPan: number;
  thresholdAmount?: number;
  thresholdType?: string;
  payeeTypes?: string[];
  isActive: boolean;
  notes?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  createdAt: string;
}

export interface TdsCalculationRequest {
  sectionCode: string;
  payeeType?: string;
  amount: number;
  hasPan?: boolean;
  transactionDate?: string;
}

export interface TdsCalculationResult {
  success: boolean;
  errorMessage?: string;
  rulePackId?: string;
  sectionCode?: string;
  applicableRate: number;
  thresholdAmount?: number;
  isExempt: boolean;
  tdsAmount: number;
}

export interface IncomeTaxCalculationRequest {
  taxableIncome: number;
  regime?: 'new' | 'old';
  ageCategory?: 'general' | 'senior' | 'super_senior';
  financialYear: string;
}

export interface IncomeTaxCalculationResult {
  success: boolean;
  regime: string;
  ageCategory?: string;
  grossIncome: number;
  taxableIncome: number;
  baseTax: number;
  rebate: number;
  surcharge: number;
  cess: number;
  totalTax: number;
  effectiveRate: number;
  slabBreakdown: IncomeTaxSlabBreakdown[];
  message?: string;
  financialYear?: string;
  rulePackVersion?: number;
}

export interface IncomeTaxSlabBreakdown {
  slabMin: number;
  slabMax?: number;
  rate: number;
  taxableAmount: number;
  taxAmount: number;
}

export interface PfEsiRates {
  employeePfRate: number;
  employerPfRate: number;
  pfWageCeiling: number;
  employeeEsiRate: number;
  employerEsiRate: number;
  esiWageCeiling: number;
  effectiveFrom?: string;
}

export interface RulePackUsageLog {
  id: string;
  rulePackId: string;
  companyId?: string;
  computationType: string;
  computationId: string;
  computationDate?: string;
  inputAmount?: number;
  computedTax?: number;
  effectiveRate?: number;
  computedAt: string;
  computedBy?: string;
}

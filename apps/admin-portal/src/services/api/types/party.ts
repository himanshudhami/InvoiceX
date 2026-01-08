// Unified Party types (replaces separate Vendor/Customer)
import type { PaginationParams } from './common';

// ==================== Party Types ====================

export interface Party {
  id: string;
  companyId: string;

  // Core Identity
  name: string;
  displayName?: string;
  legalName?: string;
  partyCode?: string;

  // Role Flags (supports dual-role parties)
  isCustomer: boolean;
  isVendor: boolean;
  isEmployee: boolean;

  // Contact
  email?: string;
  phone?: string;
  mobile?: string;

  // Address
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  stateCode?: string;
  pincode?: string;
  country?: string;

  // Indian Tax IDs
  panNumber?: string;
  gstin?: string;
  isGstRegistered?: boolean;
  gstStateCode?: string;

  // Classification
  partyType?: string;  // 'individual' | 'company' | 'firm' | 'trust' | 'government' | 'aop'

  // Status & Tally Migration
  isActive: boolean;
  tallyLedgerGuid?: string;
  tallyLedgerName?: string;
  tallyGroupName?: string;
  tallyMigrationBatchId?: string;

  // Profiles (loaded when requested)
  vendorProfile?: PartyVendorProfile;
  customerProfile?: PartyCustomerProfile;

  // Tags
  tags?: PartyTag[];

  createdAt?: string;
  updatedAt?: string;
  createdBy?: string;
}

// ==================== Vendor Profile Types ====================

export interface PartyVendorProfile {
  id: string;
  partyId: string;
  companyId: string;

  // Vendor Type
  vendorType?: string;  // 'b2b' | 'b2c' | 'import' | 'rcm_applicable'

  // TDS Configuration
  tdsApplicable: boolean;
  defaultTdsSection?: string;  // '194C' | '194J' | '194H' | '194I' | '194A' | '194Q' | '195'
  defaultTdsRate?: number;
  tanNumber?: string;

  // Lower/Nil TDS Certificate
  lowerTdsCertificate?: string;
  lowerTdsRate?: number;
  lowerTdsValidFrom?: string;
  lowerTdsValidTill?: string;

  // MSME Compliance
  msmeRegistered: boolean;
  msmeRegistrationNumber?: string;
  msmeCategory?: string;  // 'micro' | 'small' | 'medium'

  // Bank Details
  bankAccountNumber?: string;
  bankIfscCode?: string;
  bankName?: string;
  bankBranch?: string;
  bankAccountHolder?: string;
  bankAccountType?: string;  // 'savings' | 'current' | 'cc' | 'od'

  // Default Accounts
  defaultExpenseAccountId?: string;
  defaultPayableAccountId?: string;

  // Payment Terms
  paymentTermsDays?: number;
  creditLimit?: number;

  createdAt?: string;
  updatedAt?: string;
}

// ==================== Customer Profile Types ====================

export interface PartyCustomerProfile {
  id: string;
  partyId: string;
  companyId: string;

  customerType?: string;  // 'b2b' | 'b2c' | 'overseas' | 'sez'
  creditLimit?: number;
  paymentTermsDays?: number;
  defaultRevenueAccountId?: string;
  defaultReceivableAccountId?: string;
  eInvoiceApplicable: boolean;

  createdAt?: string;
  updatedAt?: string;
}

// ==================== Party Tag Types ====================

export interface PartyTag {
  id: string;
  partyId: string;
  tagId: string;
  tagName?: string;
  tagCode?: string;
  tagGroup?: string;
  tagColor?: string;
  source?: string;  // 'manual' | 'migration' | 'rule' | 'ai_suggested'
  confidenceScore?: number;  // For AI suggestions (0-100)
  createdAt?: string;
  createdBy?: string;
}

// ==================== TDS Tag Rule Types (Tag-driven TDS) ====================

/**
 * TDS Tag Rule - Links tags to TDS configuration.
 * Tags drive TDS behavior instead of hard-coded vendor types.
 */
export interface TdsTagRule {
  id: string;
  companyId: string;
  tagId: string;
  tagName?: string;
  tagCode?: string;
  tagColor?: string;

  // TDS Section (e.g., '194C', '194J', '194H', 'EXEMPT')
  tdsSection: string;
  tdsSectionClause?: string;  // e.g., '(a)' for 194J(a)

  // Rates (FY 2024-25 compliant)
  tdsRateWithPan: number;
  tdsRateWithoutPan: number;  // Higher rate without PAN (Section 206AA)
  tdsRateIndividual?: number;  // Different rate for individuals/HUF
  tdsRateCompany?: number;     // Different rate for companies

  // Thresholds
  thresholdSinglePayment?: number;
  thresholdAnnual: number;

  // Entity Type Applicability
  appliesToIndividual: boolean;
  appliesToHuf: boolean;
  appliesToCompany: boolean;
  appliesToFirm: boolean;
  appliesToLlp: boolean;
  appliesToTrust: boolean;
  appliesToAopBoi: boolean;
  appliesToGovernment: boolean;

  // Special Provisions
  lowerCertificateAllowed: boolean;
  nilCertificateAllowed: boolean;
  exemptionNotes?: string;

  // Validity Period
  effectiveFrom: string;  // DateOnly as ISO string
  effectiveTo?: string;
  isActive: boolean;

  createdAt?: string;
  updatedAt?: string;
}

// ==================== TDS Detection Result ====================

/**
 * Result of TDS detection for a party based on assigned tags.
 */
export interface TdsDetectionResult {
  partyId: string;
  partyName: string;
  pan?: string;

  // Detection result
  isApplicable: boolean;
  tdsSection?: string;
  tdsSectionClause?: string;
  tdsRate?: number;

  // Thresholds
  thresholdAnnual?: number;
  thresholdSinglePayment?: number;
  isBelowThreshold?: boolean;

  // Detection method
  detectionMethod: string;  // 'manual' | 'manual_exempt' | 'tag' | 'tag_exempt' | 'no_tag'
  matchedTagId?: string;
  matchedTagName?: string;
  exemptionNotes?: string;
  notes?: string;
}

// ==================== Legacy TDS Section Rule (deprecated, use TdsTagRule) ====================

/** @deprecated Use TdsTagRule instead. Kept for backwards compatibility during migration. */
export interface TdsSectionRule {
  id: string;
  companyId: string;
  name: string;
  description?: string;

  // Matching Criteria
  tagId?: string;
  tagName?: string;
  partyNamePattern?: string;
  tallyGroupName?: string;

  // TDS Configuration
  tdsSection: string;
  tdsRate: number;
  tdsRateNoPan?: number;
  thresholdAmount?: number;
  singlePaymentThreshold?: number;

  isActive: boolean;
  priority: number;

  createdAt?: string;
  updatedAt?: string;
}

/** @deprecated Use TdsDetectionResult instead */
export interface TdsConfiguration {
  tdsSection: string;
  tdsRate: number;
  tdsRateNoPan?: number;
  thresholdAmount?: number;
  singlePaymentThreshold?: number;
  matchedBy: string;  // 'vendor_profile' | 'tag_rule' | 'tally_group_rule'
  ruleName?: string;
}

// ==================== Create/Update DTOs ====================

export interface CreatePartyDto {
  name: string;
  displayName?: string;
  legalName?: string;
  partyCode?: string;

  isCustomer?: boolean;
  isVendor?: boolean;
  isEmployee?: boolean;

  email?: string;
  phone?: string;
  mobile?: string;

  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  stateCode?: string;
  pincode?: string;
  country?: string;

  panNumber?: string;
  gstin?: string;
  isGstRegistered?: boolean;
  gstStateCode?: string;

  partyType?: string;
  isActive?: boolean;

  // Profiles (optional, create with party)
  vendorProfile?: CreatePartyVendorProfileDto;
  customerProfile?: CreatePartyCustomerProfileDto;

  // Tag IDs to assign
  tagIds?: string[];
}

export interface UpdatePartyDto extends Omit<CreatePartyDto, 'vendorProfile' | 'customerProfile' | 'tagIds'> {}

export interface CreatePartyVendorProfileDto {
  vendorType?: string;
  tdsApplicable?: boolean;
  defaultTdsSection?: string;
  defaultTdsRate?: number;
  tanNumber?: string;
  lowerTdsCertificate?: string;
  lowerTdsRate?: number;
  lowerTdsValidFrom?: string;
  lowerTdsValidTill?: string;
  msmeRegistered?: boolean;
  msmeRegistrationNumber?: string;
  msmeCategory?: string;
  bankAccountNumber?: string;
  bankIfscCode?: string;
  bankName?: string;
  bankBranch?: string;
  bankAccountHolder?: string;
  bankAccountType?: string;
  defaultExpenseAccountId?: string;
  defaultPayableAccountId?: string;
  paymentTermsDays?: number;
  creditLimit?: number;
}

export interface UpdatePartyVendorProfileDto extends CreatePartyVendorProfileDto {}

export interface CreatePartyCustomerProfileDto {
  customerType?: string;
  creditLimit?: number;
  paymentTermsDays?: number;
  defaultRevenueAccountId?: string;
  defaultReceivableAccountId?: string;
  eInvoiceApplicable?: boolean;
}

export interface UpdatePartyCustomerProfileDto extends CreatePartyCustomerProfileDto {}

// ==================== TDS Tag Rule DTOs ====================

export interface CreateTdsTagRuleDto {
  tagId: string;
  tdsSection: string;
  tdsSectionClause?: string;
  tdsRateWithPan: number;
  tdsRateWithoutPan?: number;  // Defaults to 20%
  tdsRateIndividual?: number;
  tdsRateCompany?: number;
  thresholdSinglePayment?: number;
  thresholdAnnual: number;
  exemptionNotes?: string;
  effectiveFrom?: string;  // Defaults to FY start
  effectiveTo?: string;
}

export interface UpdateTdsTagRuleDto {
  tdsSection?: string;
  tdsSectionClause?: string;
  tdsRateWithPan?: number;
  tdsRateWithoutPan?: number;
  tdsRateIndividual?: number;
  tdsRateCompany?: number;
  thresholdSinglePayment?: number;
  thresholdAnnual?: number;
  exemptionNotes?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  isActive?: boolean;
}

// ==================== Legacy DTOs (deprecated) ====================

/** @deprecated Use CreateTdsTagRuleDto instead */
export interface CreateTdsSectionRuleDto {
  name: string;
  description?: string;
  tagId?: string;
  partyNamePattern?: string;
  tallyGroupName?: string;
  tdsSection: string;
  tdsRate: number;
  tdsRateNoPan?: number;
  thresholdAmount?: number;
  singlePaymentThreshold?: number;
  isActive?: boolean;
  priority?: number;
}

/** @deprecated Use UpdateTdsTagRuleDto instead */
export interface UpdateTdsSectionRuleDto extends CreateTdsSectionRuleDto {}

// ==================== Filter Parameters ====================

export interface PartiesFilterParams extends PaginationParams {
  name?: string;
  email?: string;
  isVendor?: boolean;
  isCustomer?: boolean;
  isEmployee?: boolean;
  isActive?: boolean;
  partyType?: string;
  isGstRegistered?: boolean;
  state?: string;
  city?: string;
  tagId?: string;
  tallyGroupName?: string;
}

export interface TdsSectionRulesFilterParams extends PaginationParams {
  tdsSection?: string;
  isActive?: boolean;
}

// ==================== Summary Types ====================

export interface PartyOutstanding {
  partyId: string;
  partyName: string;
  isVendor: boolean;
  isCustomer: boolean;
  totalInvoiced: number;
  totalPaid: number;
  totalTdsDeducted: number;
  outstandingAmount: number;
  overdueAmount: number;
  advanceBalance: number;
}

export interface PartyAgingSummary {
  partyId: string;
  partyName: string;
  isVendor: boolean;
  isCustomer: boolean;
  current: number;      // 0-30 days
  days30: number;       // 31-60 days
  days60: number;       // 61-90 days
  days90: number;       // 91-120 days
  days120Plus: number;  // 120+ days
  total: number;
}

export interface PartyTdsSummary {
  partyId: string;
  partyName: string;
  panNumber?: string;
  tdsSection: string;
  totalDeducted: number;
  totalDeposited: number;
  pendingDeposit: number;
}

// ==================== List Response Types ====================

export interface PartyListItem {
  id: string;
  name: string;
  displayName?: string;
  partyCode?: string;
  isCustomer: boolean;
  isVendor: boolean;
  isEmployee: boolean;
  email?: string;
  phone?: string;
  city?: string;
  state?: string;
  gstin?: string;
  panNumber?: string;
  isActive: boolean;
  partyType?: string;
  tallyGroupName?: string;
  // Vendor-specific (if isVendor)
  tdsApplicable?: boolean;
  defaultTdsSection?: string;
  msmeRegistered?: boolean;
  // Tag summary
  tagCount?: number;
  primaryTag?: string;
}

// Tag Types

export interface Tag {
  id: string;
  companyId: string;
  name: string;
  code?: string;
  tagGroup: TagGroup;
  description?: string;
  parentTagId?: string;
  fullPath?: string;
  level: number;
  color?: string;
  icon?: string;
  sortOrder: number;
  budgetAmount?: number;
  budgetPeriod?: string;
  budgetYear?: string;
  tallyCostCenterGuid?: string;
  tallyCostCenterName?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  createdBy?: string;
  updatedBy?: string;
  // Computed fields from views
  transactionCount?: number;
  totalAllocatedAmount?: number;
  lastUsedAt?: string;
  // For tree display
  children?: Tag[];
}

export type TagGroup = 'department' | 'project' | 'client' | 'region' | 'cost_center' | 'custom';

export interface CreateTagDto {
  companyId?: string;
  name: string;
  code?: string;
  tagGroup: TagGroup;
  description?: string;
  parentTagId?: string;
  color?: string;
  icon?: string;
  sortOrder?: number;
  budgetAmount?: number;
  budgetPeriod?: string;
  budgetYear?: string;
}

export interface UpdateTagDto {
  name?: string;
  code?: string;
  tagGroup?: TagGroup;
  description?: string;
  parentTagId?: string;
  color?: string;
  icon?: string;
  sortOrder?: number;
  budgetAmount?: number;
  budgetPeriod?: string;
  budgetYear?: string;
  isActive?: boolean;
}

export interface TagSummary {
  id: string;
  name: string;
  tagGroup: TagGroup;
  fullPath: string;
  color?: string;
  isActive: boolean;
}

export interface TagsFilterParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
  companyId?: string;
  tagGroup?: TagGroup;
  parentTagId?: string;
  isActive?: boolean;
}

// Transaction Tag Types

export interface TransactionTag {
  id: string;
  transactionId: string;
  transactionType: TransactionType;
  tagId: string;
  tagName?: string;
  tagGroup?: TagGroup;
  tagColor?: string;
  allocatedAmount?: number;
  allocationPercentage?: number;
  allocationMethod: AllocationMethod;
  source: TagSource;
  attributionRuleId?: string;
  confidenceScore?: number;
  createdAt: string;
  createdBy?: string;
}

export type TransactionType =
  | 'invoice'
  | 'vendor_invoice'
  | 'payment'
  | 'vendor_payment'
  | 'expense_claim'
  | 'journal_entry'
  | 'journal_line'
  | 'bank_transaction'
  | 'salary_transaction'
  | 'asset'
  | 'subscription'
  | 'contractor_payment';

export type AllocationMethod = 'full' | 'amount' | 'percentage' | 'split_equal';
export type TagSource = 'manual' | 'rule' | 'ai_suggested' | 'imported';

export interface ApplyTagDto {
  tagId: string;
  allocatedAmount?: number;
  allocationPercentage?: number;
  allocationMethod?: AllocationMethod;
}

export interface ApplyTagsToTransactionDto {
  transactionId: string;
  transactionType: TransactionType;
  tags: ApplyTagDto[];
}

// Attribution Rule Types

export interface AttributionRule {
  id: string;
  companyId: string;
  name: string;
  description?: string;
  ruleType: RuleType;
  appliesTo: string; // JSON string of transaction types
  conditions: string; // JSON string of conditions
  tagAssignments: string; // JSON string of tag assignments
  allocationMethod: string;
  splitMetric?: string;
  priority: number;
  stopOnMatch: boolean;
  overwriteExisting: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  timesApplied: number;
  lastAppliedAt?: string;
  totalAmountTagged: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  createdBy?: string;
  updatedBy?: string;
  // Computed
  currentTagsCount?: number;
}

export type RuleType =
  | 'vendor'
  | 'customer'
  | 'account'
  | 'product'
  | 'keyword'
  | 'amount_range'
  | 'employee'
  | 'composite';

export interface RuleConditions {
  vendorId?: string;
  customerId?: string;
  accountId?: string;
  productId?: string;
  keywords?: string[];
  minAmount?: number;
  maxAmount?: number;
  employeeId?: string;
  departmentId?: string;
  descriptionContains?: string;
  descriptionRegex?: string;
  // Composite conditions
  and?: RuleConditions[];
  or?: RuleConditions[];
}

export interface TagAssignment {
  tagId: string;
  allocationPercentage?: number;
  allocatedAmount?: number;
}

export interface CreateAttributionRuleDto {
  companyId?: string;
  name: string;
  description?: string;
  ruleType: RuleType;
  appliesTo: TransactionType[];
  conditions: RuleConditions;
  tagAssignments: TagAssignment[];
  allocationMethod?: string;
  splitMetric?: string;
  priority?: number;
  stopOnMatch?: boolean;
  overwriteExisting?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export interface UpdateAttributionRuleDto {
  name?: string;
  description?: string;
  ruleType?: RuleType;
  appliesTo?: TransactionType[];
  conditions?: RuleConditions;
  tagAssignments?: TagAssignment[];
  allocationMethod?: string;
  splitMetric?: string;
  priority?: number;
  stopOnMatch?: boolean;
  overwriteExisting?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  isActive?: boolean;
}

export interface AttributionRulesFilterParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
  companyId?: string;
  ruleType?: RuleType;
  isActive?: boolean;
}

export interface RulePerformanceSummary {
  ruleId: string;
  ruleName: string;
  ruleType: RuleType;
  priority: number;
  isActive: boolean;
  timesApplied: number;
  totalAmountTagged: number;
  lastAppliedAt?: string;
  currentTagsCount: number;
}

// Auto Attribution

export interface AutoAttributeRequest {
  companyId?: string;
  transactionId: string;
  transactionType: TransactionType;
  amount: number;
  vendorId?: string;
  customerId?: string;
  accountId?: string;
  description?: string;
}

export interface AppliedTagResult {
  tagId: string;
  tagName: string;
  allocatedAmount: number;
  allocationPercentage: number;
  source: TagSource;
  ruleId?: string;
  ruleName?: string;
}

export interface AutoAttributionResult {
  transactionId: string;
  transactionType: TransactionType;
  appliedTags: AppliedTagResult[];
  success: boolean;
  message?: string;
}

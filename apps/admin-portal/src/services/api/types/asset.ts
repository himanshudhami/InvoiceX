// Asset management types
import type { PaginationParams } from './common';
import type { Employee } from './employee';

// Asset base types
export interface Asset {
  id: string;
  companyId: string;
  categoryId?: string;
  category?: string;            // Denormalized category name
  modelId?: string;
  assetType: string;
  status: string;
  assetTag: string;
  serialNumber?: string;
  name: string;
  description?: string;
  location?: string;
  vendor?: string;
  purchaseType?: string;
  invoiceReference?: string;
  purchaseDate?: string;
  inServiceDate?: string;
  depreciationStartDate?: string;
  warrantyExpiration?: string;
  purchaseCost?: number;
  purchasePrice?: number;       // Alias for purchaseCost
  currency?: string;
  depreciationMethod?: string;
  usefulLifeMonths?: number;
  salvageValue?: number;
  residualBookValue?: number;
  customProperties?: unknown;
  notes?: string;
  // Assignment info (denormalized)
  assignedToName?: string;
  assignedToId?: string;
  assignedToType?: 'employee' | 'company';
  // Loan-related fields
  linkedLoanId?: string;
  downPaymentAmount?: number;
  gstAmount?: number;
  gstRate?: number;
  itcEligible?: boolean;
  tdsOnInterest?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface AssetAssignment {
  id: string;
  assetId: string;
  targetType: 'employee' | 'company';
  companyId: string;
  employeeId?: string;
  assignedOn: string;
  returnedOn?: string;
  conditionOut?: string;
  conditionIn?: string;
  isActive: boolean;
  notes?: string;
}

export interface CreateAssetDto {
  companyId: string;
  categoryId?: string;
  modelId?: string;
  assetType: string;
  status?: string;
  assetTag: string;
  name: string;
  serialNumber?: string;
  description?: string;
  location?: string;
  vendor?: string;
  purchaseType?: string;
  invoiceReference?: string;
  purchaseDate?: string;
  inServiceDate?: string;
  depreciationStartDate?: string;
  warrantyExpiration?: string;
  purchaseCost?: number;
  currency?: string;
  depreciationMethod?: string;
  usefulLifeMonths?: number;
  salvageValue?: number;
  residualBookValue?: number;
  customProperties?: unknown;
  notes?: string;
  // Loan-related fields
  linkedLoanId?: string;
  downPaymentAmount?: number;
  gstAmount?: number;
  gstRate?: number;
  itcEligible?: boolean;
  tdsOnInterest?: number;
}

export interface UpdateAssetDto extends Partial<CreateAssetDto> {}

export interface CreateAssetAssignmentDto {
  targetType: 'employee' | 'company';
  companyId: string;
  employeeId?: string;
  assignedOn?: string;
  conditionOut?: string;
  notes?: string;
}

export interface ReturnAssetAssignmentDto {
  returnedOn?: string;
  conditionIn?: string;
}

export interface AssetMaintenance {
  id: string;
  assetId: string;
  title: string;
  description?: string;
  status: string;
  openedAt: string;
  closedAt?: string;
  vendor?: string;
  cost?: number;
  currency?: string;
  dueDate?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateAssetMaintenanceDto {
  title: string;
  description?: string;
  status?: string;
  openedAt?: string;
  dueDate?: string;
  vendor?: string;
  cost?: number;
  currency?: string;
  notes?: string;
}

export interface UpdateAssetMaintenanceDto extends Partial<CreateAssetMaintenanceDto> {
  closedAt?: string;
}

export interface AssetDocument {
  id: string;
  assetId: string;
  name: string;
  url: string;
  contentType?: string;
  uploadedAt?: string;
  notes?: string;
}

export interface CreateAssetDocumentDto {
  name: string;
  url: string;
  contentType?: string;
  notes?: string;
}

export interface AssetDisposal {
  id: string;
  assetId: string;
  disposedOn: string;
  method: string;
  proceeds?: number;
  disposalCost?: number;
  currency?: string;
  buyer?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateAssetDisposalDto {
  disposedOn?: string;
  method?: string;
  proceeds?: number;
  disposalCost?: number;
  currency?: string;
  buyer?: string;
  notes?: string;
}

export interface AssetCostSummary {
  assetId: string;
  purchaseType: string;
  currency?: string;
  purchaseCost: number;
  maintenanceCost: number;
  depreciationBase: number;
  accumulatedDepreciation: number;
  monthlyDepreciation: number;
  netBookValue: number;
  salvageValue: number;
  depreciationMethod: string;
  usefulLifeMonths?: number;
  depreciationStartDate?: string;
  ageMonths: number;
  remainingLifeMonths: number;
  disposalProceeds: number;
  disposalCost: number;
  disposalGainLoss: number;
}

export interface AssetCostReportRow {
  companyId?: string;
  categoryId?: string;
  purchaseType?: string;
  assetCount?: number;
  purchaseCost: number;
  maintenanceCost: number;
  accumulatedDepreciation: number;
  netBookValue: number;
}

export interface AssetAgingBucket {
  label: string;
  assetCount: number;
  purchaseCost: number;
  netBookValue: number;
}

export interface AssetMaintenanceSpend {
  assetId: string;
  companyId: string;
  assetTag: string;
  assetName: string;
  status: string;
  maintenanceCost: number;
}

export interface AssetCostReport {
  totalPurchaseCost: number;
  totalMaintenanceCost: number;
  totalAccumulatedDepreciation: number;
  totalNetBookValue: number;
  totalCapexPurchase: number;
  totalOpexSpend: number;
  totalDisposalProceeds: number;
  totalDisposalCosts: number;
  totalDisposalGainLoss: number;
  averageAgeMonths: number;
  byCompany: AssetCostReportRow[];
  byCategory: AssetCostReportRow[];
  byPurchaseType: AssetCostReportRow[];
  agingBuckets: AssetAgingBucket[];
  topMaintenanceSpend: AssetMaintenanceSpend[];
}

export interface BulkAssetsDto {
  assets: CreateAssetDto[];
  skipValidationErrors?: boolean;
  createdBy?: string;
}

// Asset Request types
export type AssetRequestStatus = 'pending' | 'in_progress' | 'approved' | 'rejected' | 'fulfilled' | 'cancelled';
export type AssetRequestPriority = 'low' | 'normal' | 'high' | 'urgent';

export interface AssetRequestSummary {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeCode?: string;
  assetType: string;
  category: string;
  title: string;
  priority: AssetRequestPriority;
  status: AssetRequestStatus;
  quantity: number;
  estimatedBudget?: number;
  requestedAt: string;
  approvedAt?: string;
  fulfilledAt?: string;
}

export interface AssetRequestDetail extends AssetRequestSummary {
  companyId: string;
  department?: string;
  description?: string;
  justification?: string;
  specifications?: string;
  requestedByDate?: string;
  createdAt: string;
  updatedAt: string;
  approvedBy?: string;
  approvedByName?: string;
  rejectionReason?: string;
  cancelledAt?: string;
  cancellationReason?: string;
  assignedAssetId?: string;
  assignedAssetName?: string;
  fulfilledBy?: string;
  fulfilledByName?: string;
  fulfillmentNotes?: string;
  canEdit: boolean;
  canCancel: boolean;
  canFulfill: boolean;
  approvalRequestId?: string;
  hasApprovalWorkflow: boolean;
  currentApprovalStep?: number;
  totalApprovalSteps?: number;
}

export interface AssetRequestStats {
  totalRequests: number;
  pendingRequests: number;
  approvedRequests: number;
  rejectedRequests: number;
  fulfilledRequests: number;
  unfulfilledApproved: number;
}

export interface CreateAssetRequestDto {
  assetType: string;
  category: string;
  title: string;
  description?: string;
  justification?: string;
  specifications?: string;
  priority?: AssetRequestPriority;
  quantity?: number;
  estimatedBudget?: number;
  requestedByDate?: string;
}

export interface UpdateAssetRequestDto extends Partial<CreateAssetRequestDto> {}

export interface ApproveAssetRequestDto {
  comments?: string;
}

export interface RejectAssetRequestDto {
  reason: string;
}

export interface CancelAssetRequestDto {
  reason?: string;
}

export interface FulfillAssetRequestDto {
  assetId: string;
  notes?: string;
}

export interface AssetRequestFilterParams extends PaginationParams {
  companyId?: string;
  employeeId?: string;
  status?: string;
  priority?: string;
  category?: string;
  fromDate?: string;
  toDate?: string;
}

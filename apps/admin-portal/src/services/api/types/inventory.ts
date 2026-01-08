// Inventory management types
import type { PaginationParams } from './common';

// ============================================
// Warehouse types
// ============================================

export interface Warehouse {
  id: string;
  companyId: string;
  name: string;
  code?: string;
  address?: string;
  city?: string;
  state?: string;
  pinCode?: string;
  isDefault: boolean;
  parentWarehouseId?: string;
  tallyGodownGuid?: string;
  tallyGodownName?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  // Navigation
  parentWarehouseName?: string;
  companyName?: string;
}

export interface CreateWarehouseDto {
  companyId?: string;
  name: string;
  code?: string;
  address?: string;
  city?: string;
  state?: string;
  pinCode?: string;
  isDefault?: boolean;
  parentWarehouseId?: string;
  isActive?: boolean;
}

export interface UpdateWarehouseDto {
  name: string;
  code?: string;
  address?: string;
  city?: string;
  state?: string;
  pinCode?: string;
  isDefault?: boolean;
  parentWarehouseId?: string;
  isActive?: boolean;
}

export interface WarehouseFilterParams extends PaginationParams {
  companyId?: string;
  isActive?: boolean;
  isDefault?: boolean;
}

// ============================================
// Stock Group types
// ============================================

export interface StockGroup {
  id: string;
  companyId: string;
  name: string;
  parentStockGroupId?: string;
  tallyStockGroupGuid?: string;
  tallyStockGroupName?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  // Navigation
  parentStockGroupName?: string;
  companyName?: string;
  fullPath?: string;
  // For tree view
  children?: StockGroup[];
}

export interface CreateStockGroupDto {
  companyId?: string;
  name: string;
  parentStockGroupId?: string;
  isActive?: boolean;
}

export interface UpdateStockGroupDto {
  name: string;
  parentStockGroupId?: string;
  isActive?: boolean;
}

export interface StockGroupFilterParams extends PaginationParams {
  companyId?: string;
  parentStockGroupId?: string;
  isActive?: boolean;
}

// ============================================
// Unit of Measure types
// ============================================

export interface UnitOfMeasure {
  id: string;
  companyId?: string;
  name: string;
  symbol: string;
  decimalPlaces: number;
  isSystemUnit: boolean;
  tallyUnitGuid?: string;
  tallyUnitName?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateUnitOfMeasureDto {
  companyId?: string;
  name: string;
  symbol: string;
  decimalPlaces?: number;
  tallyUnitGuid?: string;
  tallyUnitName?: string;
}

export interface UpdateUnitOfMeasureDto {
  name: string;
  symbol: string;
  decimalPlaces?: number;
  tallyUnitGuid?: string;
  tallyUnitName?: string;
}

export interface UnitOfMeasureFilterParams extends PaginationParams {
  companyId?: string;
}

// ============================================
// Stock Item types
// ============================================

export interface StockItem {
  id: string;
  companyId: string;
  name: string;
  sku?: string;
  description?: string;
  stockGroupId?: string;
  baseUnitId: string;
  hsnSacCode?: string;
  gstRate: number;
  openingQuantity: number;
  openingValue: number;
  currentQuantity: number;
  currentValue: number;
  reorderLevel?: number;
  reorderQuantity?: number;
  minimumStock?: number;
  maximumStock?: number;
  costPrice?: number;
  sellingPrice?: number;
  mrp?: number;
  isBatchEnabled: boolean;
  valuationMethod: 'fifo' | 'lifo' | 'weighted_avg';
  tallyStockItemGuid?: string;
  tallyStockItemName?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  // Navigation
  stockGroupName?: string;
  baseUnitName?: string;
  baseUnitSymbol?: string;
  companyName?: string;
}

export interface CreateStockItemDto {
  companyId?: string;
  name: string;
  sku?: string;
  description?: string;
  stockGroupId?: string;
  baseUnitId: string;
  hsnSacCode?: string;
  gstRate?: number;
  openingQuantity?: number;
  openingValue?: number;
  reorderLevel?: number;
  reorderQuantity?: number;
  minimumStock?: number;
  maximumStock?: number;
  costPrice?: number;
  sellingPrice?: number;
  mrp?: number;
  isBatchEnabled?: boolean;
  valuationMethod?: 'fifo' | 'lifo' | 'weighted_avg';
  isActive?: boolean;
}

export interface UpdateStockItemDto {
  name: string;
  sku?: string;
  description?: string;
  stockGroupId?: string;
  baseUnitId: string;
  hsnSacCode?: string;
  gstRate?: number;
  reorderLevel?: number;
  reorderQuantity?: number;
  minimumStock?: number;
  maximumStock?: number;
  costPrice?: number;
  sellingPrice?: number;
  mrp?: number;
  isBatchEnabled?: boolean;
  valuationMethod?: 'fifo' | 'lifo' | 'weighted_avg';
  isActive?: boolean;
}

export interface StockItemFilterParams extends PaginationParams {
  companyId?: string;
  stockGroupId?: string;
  isActive?: boolean;
  lowStock?: boolean;
}

export interface StockPositionDto {
  stockItemId: string;
  stockItemName: string;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
  value: number;
}

// ============================================
// Stock Movement types
// ============================================

export type MovementType =
  | 'purchase'
  | 'sale'
  | 'transfer_in'
  | 'transfer_out'
  | 'adjustment'
  | 'opening'
  | 'return_in'
  | 'return_out';

export type SourceType =
  | 'sales_invoice'
  | 'purchase_invoice'
  | 'stock_journal'
  | 'stock_transfer'
  | 'credit_note'
  | 'debit_note';

export interface StockMovement {
  id: string;
  companyId: string;
  stockItemId: string;
  warehouseId: string;
  batchId?: string;
  movementDate: string;
  movementType: MovementType;
  quantity: number;
  rate?: number;
  value?: number;
  sourceType?: SourceType;
  sourceId?: string;
  sourceNumber?: string;
  journalEntryId?: string;
  tallyVoucherGuid?: string;
  runningQuantity?: number;
  runningValue?: number;
  notes?: string;
  createdBy?: string;
  createdAt?: string;
  // Navigation
  stockItemName?: string;
  stockItemSku?: string;
  warehouseName?: string;
  batchNumber?: string;
  unitSymbol?: string;
  companyName?: string;
}

export interface CreateStockMovementDto {
  companyId?: string;
  stockItemId: string;
  warehouseId: string;
  batchId?: string;
  movementDate: string;
  movementType: MovementType;
  quantity: number;
  rate?: number;
  sourceType?: SourceType;
  sourceId?: string;
  sourceNumber?: string;
  notes?: string;
}

export interface StockMovementFilterParams extends PaginationParams {
  companyId?: string;
  stockItemId?: string;
  warehouseId?: string;
  movementType?: MovementType;
  fromDate?: string;
  toDate?: string;
}

export interface StockLedgerEntry {
  date: string;
  voucherType: string;
  voucherNumber: string;
  inwardQuantity?: number;
  inwardRate?: number;
  inwardValue?: number;
  outwardQuantity?: number;
  outwardRate?: number;
  outwardValue?: number;
  closingQuantity: number;
  closingValue: number;
}

// ============================================
// Stock Transfer types
// ============================================

export type TransferStatus = 'draft' | 'in_transit' | 'completed' | 'cancelled';

export interface StockTransfer {
  id: string;
  companyId: string;
  transferNumber: string;
  transferDate: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  status: TransferStatus;
  totalQuantity: number;
  totalValue: number;
  notes?: string;
  tallyVoucherGuid?: string;
  createdBy?: string;
  approvedBy?: string;
  approvedAt?: string;
  completedBy?: string;
  completedAt?: string;
  createdAt?: string;
  updatedAt?: string;
  // Navigation
  fromWarehouseName?: string;
  toWarehouseName?: string;
  companyName?: string;
  createdByName?: string;
  approvedByName?: string;
  completedByName?: string;
  // Line items
  items?: StockTransferItem[];
}

export interface StockTransferItem {
  id: string;
  stockTransferId: string;
  stockItemId: string;
  batchId?: string;
  quantity: number;
  rate?: number;
  value?: number;
  receivedQuantity?: number;
  notes?: string;
  createdAt?: string;
  // Navigation
  stockItemName?: string;
  stockItemSku?: string;
  batchNumber?: string;
  unitSymbol?: string;
}

export interface CreateStockTransferDto {
  companyId?: string;
  transferDate: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  notes?: string;
  items: CreateStockTransferItemDto[];
}

export interface CreateStockTransferItemDto {
  stockItemId: string;
  batchId?: string;
  quantity: number;
  rate?: number;
  notes?: string;
}

export interface UpdateStockTransferDto {
  transferDate: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  notes?: string;
  items: CreateStockTransferItemDto[];
}

export interface StockTransferFilterParams extends PaginationParams {
  companyId?: string;
  status?: TransferStatus;
  fromWarehouseId?: string;
  toWarehouseId?: string;
  fromDate?: string;
  toDate?: string;
}

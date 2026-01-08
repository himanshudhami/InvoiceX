// Bill of Materials types
export interface BillOfMaterials {
  id: string;
  companyId: string;
  finishedGoodId: string;
  finishedGoodName?: string;
  finishedGoodSku?: string;
  name: string;
  code?: string;
  version: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  outputQuantity: number;
  outputUnitId?: string;
  outputUnitName?: string;
  outputUnitSymbol?: string;
  isActive: boolean;
  notes?: string;
  items?: BomItem[];
  createdAt: string;
  updatedAt?: string;
}

export interface BomItem {
  id?: string;
  bomId?: string;
  componentId: string;
  componentName?: string;
  componentSku?: string;
  quantity: number;
  unitId?: string;
  unitName?: string;
  unitSymbol?: string;
  scrapPercentage: number;
  isOptional: boolean;
  sequence: number;
  notes?: string;
}

export interface CreateBomDto {
  companyId?: string;
  finishedGoodId: string;
  name: string;
  code?: string;
  version?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  outputQuantity?: number;
  outputUnitId?: string;
  isActive?: boolean;
  notes?: string;
  items: BomItemDto[];
}

export interface UpdateBomDto {
  finishedGoodId: string;
  name: string;
  code?: string;
  version?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  outputQuantity?: number;
  outputUnitId?: string;
  isActive?: boolean;
  notes?: string;
  items: BomItemDto[];
}

export interface BomItemDto {
  id?: string;
  componentId: string;
  quantity: number;
  unitId?: string;
  scrapPercentage?: number;
  isOptional?: boolean;
  sequence?: number;
  notes?: string;
}

export interface CopyBomDto {
  name: string;
  code?: string;
  version?: string;
}

// Production Order types
export interface ProductionOrder {
  id: string;
  companyId: string;
  orderNumber: string;
  bomId: string;
  bomName?: string;
  finishedGoodId: string;
  finishedGoodName?: string;
  finishedGoodSku?: string;
  warehouseId: string;
  warehouseName?: string;
  plannedQuantity: number;
  actualQuantity: number;
  plannedStartDate?: string;
  plannedEndDate?: string;
  actualStartDate?: string;
  actualEndDate?: string;
  status: 'draft' | 'released' | 'in_progress' | 'completed' | 'cancelled';
  notes?: string;
  releasedBy?: string;
  releasedByName?: string;
  releasedAt?: string;
  startedBy?: string;
  startedByName?: string;
  startedAt?: string;
  completedBy?: string;
  completedByName?: string;
  completedAt?: string;
  cancelledBy?: string;
  cancelledAt?: string;
  items?: ProductionOrderItem[];
  createdAt: string;
  updatedAt?: string;
}

export interface ProductionOrderItem {
  id?: string;
  productionOrderId?: string;
  componentId: string;
  componentName?: string;
  componentSku?: string;
  plannedQuantity: number;
  consumedQuantity: number;
  unitId?: string;
  unitName?: string;
  unitSymbol?: string;
  batchId?: string;
  batchNumber?: string;
  warehouseId?: string;
  warehouseName?: string;
  notes?: string;
}

export interface CreateProductionOrderDto {
  companyId?: string;
  orderNumber?: string;
  bomId: string;
  warehouseId: string;
  plannedQuantity: number;
  plannedStartDate?: string;
  plannedEndDate?: string;
  notes?: string;
  items?: ProductionOrderItemDto[];
}

export interface UpdateProductionOrderDto {
  bomId: string;
  warehouseId: string;
  plannedQuantity: number;
  plannedStartDate?: string;
  plannedEndDate?: string;
  notes?: string;
  items?: ProductionOrderItemDto[];
}

export interface ProductionOrderItemDto {
  id?: string;
  componentId: string;
  plannedQuantity: number;
  consumedQuantity?: number;
  unitId?: string;
  batchId?: string;
  warehouseId?: string;
  notes?: string;
}

export interface ReleaseProductionOrderDto {
  userId?: string;
}

export interface StartProductionOrderDto {
  userId?: string;
}

export interface CompleteProductionOrderDto {
  userId?: string;
  actualQuantity: number;
}

export interface CancelProductionOrderDto {
  userId?: string;
  reason?: string;
}

export interface ConsumeItemDto {
  itemId: string;
  quantity: number;
  batchId?: string;
}

// Serial Number types
export interface SerialNumber {
  id: string;
  companyId: string;
  stockItemId: string;
  stockItemName?: string;
  stockItemSku?: string;
  serialNo: string;
  warehouseId?: string;
  warehouseName?: string;
  batchId?: string;
  batchNumber?: string;
  status: 'available' | 'sold' | 'reserved' | 'damaged';
  manufacturingDate?: string;
  warrantyExpiry?: string;
  productionOrderId?: string;
  productionOrderNumber?: string;
  soldAt?: string;
  soldInvoiceId?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateSerialNumberDto {
  companyId?: string;
  stockItemId: string;
  serialNo: string;
  warehouseId?: string;
  batchId?: string;
  status?: string;
  manufacturingDate?: string;
  warrantyExpiry?: string;
  productionOrderId?: string;
  notes?: string;
}

export interface UpdateSerialNumberDto {
  warehouseId?: string;
  batchId?: string;
  status?: string;
  manufacturingDate?: string;
  warrantyExpiry?: string;
  notes?: string;
}

export interface BulkCreateSerialNumberDto {
  companyId?: string;
  stockItemId: string;
  prefix: string;
  count: number;
  startNumber?: number;
  warehouseId?: string;
  batchId?: string;
  manufacturingDate?: string;
  warrantyExpiry?: string;
  productionOrderId?: string;
}

export interface MarkSerialAsSoldDto {
  invoiceId: string;
}

export interface SerialNumberFilterParams {
  companyId?: string;
  stockItemId?: string;
  warehouseId?: string;
  status?: string;
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}

// Paged response types
export interface PagedBomResponse {
  items: BillOfMaterials[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface PagedProductionOrderResponse {
  items: ProductionOrder[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface PagedSerialNumberResponse {
  items: SerialNumber[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

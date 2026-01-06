// Product types
import type { PaginationParams } from './common';

export interface Product {
  id: string;
  companyId?: string;
  name: string;
  description?: string;
  sku?: string;
  category?: string;
  type?: string;
  unitPrice: number;
  unit?: string;
  taxRate?: number;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  // GST Compliance fields
  hsnSacCode?: string;           // HSN code (goods) or SAC code (services)
  isService?: boolean;           // True for SAC code (services), false for HSN code (goods)
  defaultGstRate?: number;       // Default GST rate percentage (0, 5, 12, 18, 28)
  cessRate?: number;             // Cess rate percentage for specific goods
}

export interface CreateProductDto {
  companyId?: string;
  name: string;
  description?: string;
  sku?: string;
  category?: string;
  type?: string;
  unitPrice: number;
  unit?: string;
  taxRate?: number;
  isActive?: boolean;
  // GST Compliance fields
  hsnSacCode?: string;
  isService?: boolean;
  defaultGstRate?: number;
  cessRate?: number;
}

export interface UpdateProductDto extends CreateProductDto {}

export interface ProductsFilterParams extends PaginationParams {
  searchTerm?: string;
  name?: string;
  category?: string;
  type?: string;
  isActive?: boolean;
  companyId?: string;
}

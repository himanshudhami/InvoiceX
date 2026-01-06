// Customer types
import type { PaginationParams } from './common';

export interface Customer {
  id: string;
  companyId?: string;
  name: string;
  companyName?: string;
  email?: string;
  phone?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  taxNumber?: string;
  notes?: string;
  creditLimit?: number;
  paymentTerms?: number;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  // GST Compliance fields
  gstin?: string;              // GSTIN (15 characters)
  gstStateCode?: string;       // GST State Code (2 digits)
  customerType?: string;       // 'b2b' | 'b2c' | 'overseas' | 'sez'
  isGstRegistered?: boolean;   // Whether customer is GST registered
  panNumber?: string;          // PAN Number (for TDS purposes)
}

export interface CreateCustomerDto {
  companyId?: string;
  name: string;
  companyName?: string;
  email?: string;
  phone?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  taxNumber?: string;
  notes?: string;
  creditLimit?: number;
  paymentTerms?: number;
  isActive?: boolean;
  // GST Compliance fields
  gstin?: string;
  gstStateCode?: string;
  customerType?: string;
  isGstRegistered?: boolean;
  panNumber?: string;
}

export interface UpdateCustomerDto extends CreateCustomerDto {}

export interface CustomersFilterParams extends PaginationParams {
  name?: string;
  companyName?: string;
  email?: string;
  city?: string;
  state?: string;
  isActive?: boolean;
  companyId?: string;
}

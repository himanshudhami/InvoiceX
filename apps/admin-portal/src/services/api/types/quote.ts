// Quote types
import type { PaginationParams } from './common';

export interface QuoteItem {
  id: string;
  quoteId?: string;
  productId?: string;
  description: string;
  quantity: number;
  unitPrice: number;
  taxRate?: number;
  discountRate?: number;
  lineTotal: number;
  sortOrder?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface Quote {
  id: string;
  companyId?: string;
  customerId?: string;
  quoteNumber: string;
  quoteDate: string;
  expiryDate: string;
  status?: string;
  subtotal: number;
  discountType?: string;
  discountValue?: number;
  discountAmount?: number;
  taxAmount?: number;
  totalAmount: number;
  currency?: string;
  notes?: string;
  terms?: string;
  paymentInstructions?: string;
  poNumber?: string;
  projectName?: string;
  sentAt?: string;
  viewedAt?: string;
  acceptedAt?: string;
  rejectedAt?: string;
  rejectedReason?: string;
  createdAt?: string;
  updatedAt?: string;
  items?: QuoteItem[]; // Client-side: Quote items embedded for TanStack DB
}

export interface CreateQuoteDto {
  companyId?: string;
  customerId?: string;
  quoteNumber: string;
  quoteDate: string;
  expiryDate: string;
  status?: string;
  subtotal: number;
  discountType?: string;
  discountValue?: number;
  discountAmount?: number;
  taxAmount?: number;
  totalAmount: number;
  currency?: string;
  notes?: string;
  terms?: string;
  paymentInstructions?: string;
  poNumber?: string;
  projectName?: string;
}

export interface UpdateQuoteDto extends CreateQuoteDto {
  id: string;
}

export interface QuotesFilterParams extends PaginationParams {
  status?: string;
  quoteNumber?: string;
  projectName?: string;
  currency?: string;
  companyId?: string;
}

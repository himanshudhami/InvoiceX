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
  partyId?: string;
  quoteNumber: string;
  quoteDate: string;
  validUntil?: string;
  status?: string;
  subtotal: number;
  discountAmount?: number;
  taxAmount?: number;
  totalAmount: number;
  currency?: string;
  notes?: string;
  terms?: string;
  convertedToInvoiceId?: string;
  convertedAt?: string;
  createdAt?: string;
  updatedAt?: string;
  items?: QuoteItem[]; // Client-side: Quote items embedded for TanStack DB
}

export interface CreateQuoteDto {
  companyId?: string;
  partyId?: string;
  quoteNumber: string;
  quoteDate: string;
  validUntil?: string;
  status?: string;
  subtotal: number;
  discountAmount?: number;
  taxAmount?: number;
  totalAmount: number;
  currency?: string;
  notes?: string;
  terms?: string;
}

export interface UpdateQuoteDto extends CreateQuoteDto {
  id: string;
}

export interface QuotesFilterParams extends PaginationParams {
  status?: string;
  currency?: string;
  companyId?: string;
  partyId?: string;
  validUntilFrom?: string;
  validUntilTo?: string;
  totalAmountFrom?: number;
  totalAmountTo?: number;
}

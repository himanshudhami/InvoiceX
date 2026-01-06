// Payment types - Enhanced for Indian tax compliance
import type { PaginationParams } from './common';

export interface Payment {
  id: string;
  // Linking
  invoiceId?: string;
  companyId?: string;
  customerId?: string;
  // Computed/Joined fields
  invoiceNumber?: string;  // Populated from JOIN with invoices table
  // Payment details
  paymentDate: string;
  amount: number;
  amountInInr?: number;
  currency?: string;
  paymentMethod?: string;
  referenceNumber?: string;
  notes?: string;
  description?: string;
  // Classification
  paymentType?: 'invoice_payment' | 'advance_received' | 'direct_income' | 'refund_received';
  incomeCategory?: 'export_services' | 'domestic_services' | 'product_sale' | 'interest' | 'other';
  // TDS tracking
  tdsApplicable?: boolean;
  tdsSection?: string;  // 194J, 194C, 194H, 194O
  tdsRate?: number;
  tdsAmount?: number;
  grossAmount?: number;
  // Financial year
  financialYear?: string;  // Format: 2024-25
  // Timestamps
  createdAt?: string;
  updatedAt?: string;
}

// Income summary for financial reports
export interface IncomeSummary {
  totalGross: number;
  totalTds: number;
  totalNet: number;
  totalInr: number;
}

// TDS summary for compliance reporting
export interface TdsSummary {
  customerName?: string;
  customerPan?: string;
  tdsSection?: string;
  paymentCount: number;
  totalGross: number;
  totalTds: number;
  totalNet: number;
}

// Payment filters for server-side paging
export interface PaymentsFilterParams extends PaginationParams {
  invoiceId?: string;
  companyId?: string;
  customerId?: string;
  paymentType?: string;
  incomeCategory?: string;
  tdsApplicable?: boolean;
  tdsSection?: string;
  financialYear?: string;
  currency?: string;
  fromDate?: string;
  toDate?: string;
}

// TDS Receivable Types - for Form 26AS reconciliation
import type { PaginationParams } from './common';

export interface TdsReceivable {
  id: string;
  companyId: string;
  financialYear: string; // Format: '2024-25'
  quarter: string; // 'Q1', 'Q2', 'Q3', 'Q4'
  // Deductor details
  customerId?: string;
  deductorName: string;
  deductorTan?: string;
  deductorPan?: string;
  // Transaction details
  paymentDate: string;
  tdsSection: string; // 194J, 194C, 194H, 194O
  grossAmount: number;
  tdsRate: number;
  tdsAmount: number;
  netReceived: number;
  // Certificate details
  certificateNumber?: string;
  certificateDate?: string;
  certificateDownloaded: boolean;
  // Linked records
  paymentId?: string;
  invoiceId?: string;
  // 26AS matching
  matchedWith26As: boolean;
  form26AsAmount?: number;
  amountDifference?: number;
  matchedAt?: string;
  // Status
  status: 'pending' | 'matched' | 'claimed' | 'disputed' | 'written_off';
  claimedInReturn?: string;
  // Additional
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateTdsReceivableDto {
  companyId: string;
  financialYear: string;
  quarter: string;
  customerId?: string;
  deductorName: string;
  deductorTan?: string;
  deductorPan?: string;
  paymentDate: string;
  tdsSection: string;
  grossAmount: number;
  tdsRate: number;
  tdsAmount: number;
  netReceived: number;
  certificateNumber?: string;
  certificateDate?: string;
  certificateDownloaded?: boolean;
  paymentId?: string;
  invoiceId?: string;
  notes?: string;
}

export interface UpdateTdsReceivableDto {
  financialYear?: string;
  quarter?: string;
  customerId?: string;
  deductorName?: string;
  deductorTan?: string;
  deductorPan?: string;
  paymentDate?: string;
  tdsSection?: string;
  grossAmount?: number;
  tdsRate?: number;
  tdsAmount?: number;
  netReceived?: number;
  certificateNumber?: string;
  certificateDate?: string;
  certificateDownloaded?: boolean;
  paymentId?: string;
  invoiceId?: string;
  notes?: string;
}

export interface Match26AsDto {
  form26AsAmount: number;
}

export interface UpdateTdsStatusDto {
  status: 'pending' | 'matched' | 'claimed' | 'disputed' | 'written_off';
  claimedInReturn?: string;
}

export interface TdsReceivableSummary {
  financialYear: string;
  totalGrossAmount: number;
  totalTdsAmount: number;
  totalNetReceived: number;
  totalEntries: number;
  matchedEntries: number;
  unmatchedEntries: number;
  matchedAmount: number;
  unmatchedAmount: number;
  quarterlySummary: TdsQuarterlySummary[];
}

export interface TdsQuarterlySummary {
  quarter: string;
  tdsAmount: number;
  entryCount: number;
}

export interface TdsReceivableFilterParams extends PaginationParams {
  companyId?: string;
  financialYear?: string;
  quarter?: string;
  status?: string;
  matchedWith26As?: boolean;
}

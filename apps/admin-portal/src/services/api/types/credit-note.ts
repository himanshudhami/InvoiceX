// Credit Note types - GST compliant as per Section 34 of CGST Act
import type { PaginationParams } from './common';

// Reasons for issuing credit note as per GST rules
export type CreditNoteReason =
  | 'goods_returned'           // Goods returned by recipient
  | 'post_sale_discount'       // Discount given after invoice
  | 'deficiency_in_services'   // Services found deficient
  | 'excess_amount_charged'    // Excess taxable value charged
  | 'excess_tax_charged'       // Excess tax charged in original invoice
  | 'change_in_pos'            // Change in Place of Supply
  | 'export_refund'            // Refund on export goods
  | 'other';                   // Other reasons (with description)

export interface CreditNoteItem {
  id: string;
  creditNoteId?: string;
  originalInvoiceItemId?: string;  // Reference to original invoice item
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
  // GST Compliance fields
  hsnSacCode?: string;           // HSN code (goods) or SAC code (services)
  isService?: boolean;           // True for SAC code (services), false for HSN code (goods)
  cgstRate?: number;             // Central GST rate percentage
  cgstAmount?: number;           // Central GST amount calculated
  sgstRate?: number;             // State GST rate percentage
  sgstAmount?: number;           // State GST amount calculated
  igstRate?: number;             // Integrated GST rate percentage
  igstAmount?: number;           // Integrated GST amount calculated
  cessRate?: number;             // Cess rate percentage
  cessAmount?: number;           // Cess amount calculated
}

export interface CreditNote {
  id: string;
  companyId?: string;
  partyId?: string;              // References parties table (customer)

  // Credit Note identification
  creditNoteNumber: string;
  creditNoteDate: string;

  // Original invoice reference (MANDATORY as per GST)
  originalInvoiceId: string;
  originalInvoiceNumber: string;
  originalInvoiceDate: string;

  // Reason for credit note (required for GST compliance)
  reason: CreditNoteReason;
  reasonDescription?: string;    // Detailed description, especially for 'other'

  // Status
  status?: string;               // 'draft' | 'issued' | 'cancelled'

  // Financial details
  subtotal: number;
  taxAmount?: number;
  discountAmount?: number;
  totalAmount: number;
  currency?: string;

  // Additional details
  notes?: string;
  terms?: string;

  // Timestamps
  issuedAt?: string;
  cancelledAt?: string;
  createdAt?: string;
  updatedAt?: string;

  // Line items (embedded for TanStack DB)
  items?: CreditNoteItem[];

  // GST Classification (inherited from original invoice)
  invoiceType?: string;          // 'export' | 'domestic_b2b' | 'domestic_b2c' | 'sez' | 'deemed_export'
  supplyType?: string;           // 'intra_state' | 'inter_state' | 'export'
  placeOfSupply?: string;        // State code or 'export'
  reverseCharge?: boolean;       // Whether reverse charge mechanism applies

  // GST Totals
  totalCgst?: number;            // Total CGST amount
  totalSgst?: number;            // Total SGST amount
  totalIgst?: number;            // Total IGST amount
  totalCess?: number;            // Total Cess amount

  // E-invoicing fields (credit notes also need IRN for applicable businesses)
  eInvoiceApplicable?: boolean;
  irn?: string;                  // Invoice Reference Number for credit note
  irnGeneratedAt?: string;
  irnCancelledAt?: string;
  qrCodeData?: string;
  eInvoiceSignedJson?: string;
  eInvoiceStatus?: string;       // 'not_applicable' | 'pending' | 'generated' | 'cancelled' | 'error'

  // Forex (for export invoices)
  foreignCurrency?: string;
  exchangeRate?: number;
  amountInInr?: number;

  // 2025 Amendment: ITC Reversal Tracking
  itcReversalRequired?: boolean;  // Whether recipient needs to reverse ITC
  itcReversalConfirmed?: boolean; // Whether recipient has confirmed ITC reversal
  itcReversalDate?: string;       // Date of ITC reversal confirmation
  itcReversalCertificate?: string; // CA/CMA certificate reference for tax > 5L

  // GSTR-1 Reporting
  reportedInGstr1?: boolean;
  gstr1Period?: string;          // e.g., '202501' for Jan 2025
  gstr1FilingDate?: string;

  // Time limit tracking (must be issued before 30th Nov of next FY)
  timeBarredDate?: string;       // Calculated: 30th Nov of next FY from original invoice
  isTimeBarred?: boolean;        // Whether credit note is past deadline
}

export interface CreateCreditNoteDto {
  companyId?: string;
  partyId?: string;
  creditNoteNumber: string;
  creditNoteDate: string;

  // Original invoice reference (MANDATORY)
  originalInvoiceId: string;

  // Reason
  reason: CreditNoteReason;
  reasonDescription?: string;

  status?: string;
  subtotal: number;
  taxAmount?: number;
  discountAmount?: number;
  totalAmount: number;
  currency?: string;
  notes?: string;
  terms?: string;

  // GST Classification
  invoiceType?: string;
  supplyType?: string;
  placeOfSupply?: string;
  reverseCharge?: boolean;

  // GST Totals
  totalCgst?: number;
  totalSgst?: number;
  totalIgst?: number;
  totalCess?: number;

  // Forex
  exchangeRate?: number;

  // E-invoicing
  eInvoiceApplicable?: boolean;
}

export interface UpdateCreditNoteDto extends CreateCreditNoteDto {
  id: string;
}

export interface CreditNotesFilterParams extends PaginationParams {
  status?: string;
  creditNoteNumber?: string;
  originalInvoiceNumber?: string;
  reason?: CreditNoteReason;
  currency?: string;
  companyId?: string;
  partyId?: string;
  fromDate?: string;
  toDate?: string;
}

// Helper type for credit note creation from invoice
export interface CreateCreditNoteFromInvoice {
  invoiceId: string;
  reason: CreditNoteReason;
  reasonDescription?: string;
  // If partial credit note, specify which items and quantities
  items?: {
    originalItemId: string;
    quantity: number;
    unitPrice?: number;  // Override if needed
  }[];
  // If full credit note, this can be omitted and all items will be copied
  isFullCreditNote?: boolean;
}

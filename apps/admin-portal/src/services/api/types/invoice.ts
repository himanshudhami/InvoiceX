// Invoice types
import type { PaginationParams } from './common';

export interface InvoiceItem {
  id: string;
  invoiceId?: string;
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

export interface Invoice {
  id: string;
  companyId?: string;
  partyId?: string;  // References parties table (customer)
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  status?: string;
  subtotal: number;
  taxAmount?: number;
  discountAmount?: number;
  totalAmount: number;
  paidAmount?: number;
  currency?: string;
  notes?: string;
  terms?: string;
  paymentInstructions?: string;
  poNumber?: string;
  projectName?: string;
  sentAt?: string;
  viewedAt?: string;
  paidAt?: string;
  createdAt?: string;
  updatedAt?: string;
  items?: InvoiceItem[]; // Client-side: Invoice items embedded for TanStack DB
  // GST Classification
  invoiceType?: string;          // 'export' | 'domestic_b2b' | 'domestic_b2c' | 'sez' | 'deemed_export'
  supplyType?: string;           // 'intra_state' | 'inter_state' | 'export'
  placeOfSupply?: string;        // State code or 'export'
  reverseCharge?: boolean;       // Whether reverse charge mechanism applies
  // GST Totals
  totalCgst?: number;            // Total CGST amount
  totalSgst?: number;            // Total SGST amount
  totalIgst?: number;            // Total IGST amount
  totalCess?: number;            // Total Cess amount
  // E-invoicing fields
  eInvoiceApplicable?: boolean;  // Whether e-invoicing is applicable
  eInvoiceIrn?: string;          // Invoice Reference Number from e-invoice portal (legacy)
  eInvoiceAckNumber?: string;    // Acknowledgement number from e-invoice portal
  eInvoiceAckDate?: string;      // Acknowledgement date from e-invoice portal
  eInvoiceQrCode?: string;       // QR code data for e-invoice (legacy)
  // New E-invoice fields (IRP integration)
  irn?: string;                  // Invoice Reference Number
  irnGeneratedAt?: string;       // When IRN was generated
  irnCancelledAt?: string;       // When IRN was cancelled
  qrCodeData?: string;           // QR code base64 data from IRP
  eInvoiceSignedJson?: string;   // Signed invoice JSON from IRP
  eInvoiceStatus?: string;       // 'not_applicable' | 'pending' | 'generated' | 'cancelled' | 'error'
  // Export-specific fields
  exportType?: string;           // 'EXPWP' | 'EXPWOP' (with/without payment)
  portCode?: string;             // Port of export code
  shippingBillNumber?: string;   // Shipping bill number
  shippingBillDate?: string;     // Shipping bill date
  foreignCurrency?: string;      // Foreign currency code (USD, EUR, etc.)
  exchangeRate?: number;         // Exchange rate to INR
  countryOfDestination?: string; // Destination country code
  // SEZ-specific fields
  sezCategory?: string;          // 'SEZWP' | 'SEZWOP'
  // Shipping details
  shippingAddress?: string;      // Shipping/delivery address
  transporterName?: string;      // Name of transporter
  vehicleNumber?: string;        // Vehicle number
  ewayBillNumber?: string;       // E-way bill number
  // INR conversion for exports
  invoiceAmountInr?: number;     // Total amount in INR for exports
}

export interface CreateInvoiceDto {
  companyId?: string;
  partyId?: string;  // References parties table (customer)
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  status?: string;
  subtotal: number;
  taxAmount?: number;
  discountAmount?: number;
  totalAmount: number;
  paidAmount?: number;
  currency?: string;
  notes?: string;
  terms?: string;
  paymentInstructions?: string;
  poNumber?: string;
  projectName?: string;
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
  // E-invoicing fields
  eInvoiceApplicable?: boolean;
  eInvoiceIrn?: string;
  eInvoiceAckNumber?: string;
  eInvoiceAckDate?: string;
  eInvoiceQrCode?: string;
  // Shipping details
  shippingAddress?: string;
  transporterName?: string;
  vehicleNumber?: string;
  ewayBillNumber?: string;
}

export interface UpdateInvoiceDto extends CreateInvoiceDto {
  id: string;
}

export interface InvoicesFilterParams extends PaginationParams {
  status?: string;
  invoiceNumber?: string;
  projectName?: string;
  currency?: string;
  companyId?: string;
  partyId?: string;  // References parties table (customer)
}

// E-Invoice Types

export interface EInvoiceCredentials {
  id: string;
  companyId: string;
  gspProvider: 'cleartax' | 'iris' | 'nic_direct';
  environment: 'sandbox' | 'production';
  clientId?: string;
  username?: string;
  autoGenerateIrn: boolean;
  autoCancelOnVoid: boolean;
  generateEwayBill: boolean;
  einvoiceThreshold: number;
  isActive: boolean;
  tokenExpiry?: string;
  createdAt: string;
  updatedAt: string;
}

export interface SaveEInvoiceCredentialsDto {
  companyId: string;
  gspProvider: string;
  environment?: string;
  clientId?: string;
  clientSecret?: string;
  username?: string;
  password?: string;
  autoGenerateIrn: boolean;
  autoCancelOnVoid: boolean;
  generateEwayBill: boolean;
  einvoiceThreshold?: number;
  isActive: boolean;
}

export interface EInvoiceGenerationResult {
  success: boolean;
  irn?: string;
  ackNumber?: string;
  ackDate?: string;
  qrCode?: string;
  ewayBillNumber?: string;
  errorCode?: string;
  errorMessage?: string;
}

export interface EInvoiceAuditLog {
  id: string;
  companyId: string;
  invoiceId?: string;
  actionType: string;
  requestTimestamp: string;
  responseStatus?: string;
  irn?: string;
  ackNumber?: string;
  ackDate?: string;
  errorCode?: string;
  errorMessage?: string;
  gspProvider?: string;
  environment?: string;
  responseTimeMs?: number;
  createdAt: string;
}

export interface EInvoiceQueueItem {
  id: string;
  companyId: string;
  invoiceId: string;
  actionType: string;
  priority: number;
  status: 'pending' | 'processing' | 'completed' | 'failed' | 'cancelled';
  retryCount: number;
  maxRetries: number;
  nextRetryAt?: string;
  startedAt?: string;
  completedAt?: string;
  errorCode?: string;
  errorMessage?: string;
  createdAt: string;
  updatedAt: string;
}

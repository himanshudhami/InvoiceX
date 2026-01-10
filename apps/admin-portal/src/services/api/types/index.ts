/**
 * Types Index - Re-exports all types from domain-specific modules
 *
 * This file maintains backward compatibility with existing imports.
 * Types are now organized into domain-specific modules for better maintainability.
 *
 * For cleaner, domain-specific imports, you can import directly from:
 * - '@/services/api/types/common' for ApiError, PagedResponse, PaginationParams
 * - '@/services/api/types/company' for Company types
 * - '@/services/api/types/customer' for Customer types
 * - '@/services/api/types/product' for Product types
 * - '@/services/api/types/invoice' for Invoice types
 * - '@/services/api/types/credit-note' for Credit Note types
 * - '@/services/api/types/quote' for Quote types
 * - '@/services/api/types/tax-rate' for TaxRate types
 * - '@/services/api/types/invoice-template' for InvoiceTemplate types
 * - '@/services/api/types/dashboard' for Dashboard types
 * - '@/services/api/types/employee' for Employee types
 * - '@/services/api/types/vendor' for Vendor, VendorInvoice, VendorPayment types
 * - '@/services/api/types/asset' for Asset management types
 * - '@/services/api/types/subscription' for Subscription types
 * - '@/services/api/types/loan' for Loan types
 * - '@/services/api/types/payment' for Payment types
 * - '@/services/api/types/bank' for BankAccount, BankTransaction, BRS types
 * - '@/services/api/types/ledger' for ChartOfAccount, JournalEntry types
 * - '@/services/api/types/tds-receivable' for TDS Receivable types
 * - '@/services/api/types/leave' for Leave management types
 * - '@/services/api/types/einvoice' for E-Invoice types
 * - '@/services/api/types/tax-rule-pack' for Tax Rule Pack types
 * - '@/services/api/types/forex' for Export & Forex types
 * - '@/services/api/types/gst' for GST Compliance types
 * - '@/services/api/types/tds-return' for TDS Return (Form 26Q/24Q) types
 * - '@/services/api/types/statutory' for RCM, LDC, TCS, Form 16, PF, ESI types
 *
 * Usage:
 * - import { Customer, Invoice, Vendor } from '@/services/api/types'
 * - import type { Vendor } from '@/services/api/types/vendor'
 */

// Common/shared types
export * from './common';

// Core entity types
export * from './company';
export * from './customer';
export * from './product';
export * from './invoice';
export * from './credit-note';
export * from './quote';
export * from './tax-rate';
export * from './invoice-template';
export * from './dashboard';

// Employee & HR types
export * from './employee';
export * from './leave';

// Finance types
export * from './payment';
export * from './bank';
export * from './ledger';
export * from './loan';

// Asset management
export * from './asset';
export * from './subscription';

// Accounts Payable (Vendor) types
export * from './vendor';

// Tax compliance types
export * from './tds-receivable';
export * from './einvoice';
export * from './tax-rule-pack';
export * from './forex';
export * from './gst';
export * from './tds-return';
export * from './statutory';

// Tags & Attribution types
export * from './tag';

// Unified Party Management types
export * from './party';

// Inventory management types
export * from './inventory';

// Manufacturing types
export * from './manufacturing';

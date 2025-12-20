-- Down migration for e-invoice tables

-- Drop triggers
DROP TRIGGER IF EXISTS trigger_einvoice_queue_updated_at ON einvoice_queue;
DROP TRIGGER IF EXISTS trigger_einvoice_credentials_updated_at ON einvoice_credentials;
DROP FUNCTION IF EXISTS update_einvoice_updated_at();

-- Drop indexes
DROP INDEX IF EXISTS idx_einvoice_queue_company;
DROP INDEX IF EXISTS idx_einvoice_queue_invoice;
DROP INDEX IF EXISTS idx_einvoice_queue_retry;
DROP INDEX IF EXISTS idx_einvoice_queue_pending;

DROP INDEX IF EXISTS idx_einvoice_audit_errors;
DROP INDEX IF EXISTS idx_einvoice_audit_action;
DROP INDEX IF EXISTS idx_einvoice_audit_irn;
DROP INDEX IF EXISTS idx_einvoice_audit_invoice;
DROP INDEX IF EXISTS idx_einvoice_audit_company;

DROP INDEX IF EXISTS idx_einvoice_creds_active;
DROP INDEX IF EXISTS idx_einvoice_creds_company;

-- Drop tables
DROP TABLE IF EXISTS einvoice_queue;
DROP TABLE IF EXISTS einvoice_audit_log;
DROP TABLE IF EXISTS einvoice_credentials;

-- Remove invoice columns
ALTER TABLE invoices DROP COLUMN IF EXISTS e_invoice_signed_json;
ALTER TABLE invoices DROP COLUMN IF EXISTS e_invoice_status;
ALTER TABLE invoices DROP COLUMN IF EXISTS e_invoice_cancel_date;
ALTER TABLE invoices DROP COLUMN IF EXISTS e_invoice_cancel_reason;
ALTER TABLE invoices DROP COLUMN IF EXISTS eway_bill_number;
ALTER TABLE invoices DROP COLUMN IF EXISTS eway_bill_date;
ALTER TABLE invoices DROP COLUMN IF EXISTS eway_bill_valid_until;
ALTER TABLE invoices DROP COLUMN IF EXISTS export_type;
ALTER TABLE invoices DROP COLUMN IF EXISTS port_code;
ALTER TABLE invoices DROP COLUMN IF EXISTS shipping_bill_number;
ALTER TABLE invoices DROP COLUMN IF EXISTS shipping_bill_date;
ALTER TABLE invoices DROP COLUMN IF EXISTS export_duty;
ALTER TABLE invoices DROP COLUMN IF EXISTS foreign_currency;
ALTER TABLE invoices DROP COLUMN IF EXISTS exchange_rate;
ALTER TABLE invoices DROP COLUMN IF EXISTS foreign_currency_amount;
ALTER TABLE invoices DROP COLUMN IF EXISTS sez_category;
ALTER TABLE invoices DROP COLUMN IF EXISTS b2c_large;

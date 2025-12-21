-- Rollback: Remove forex tracking fields from invoices

DROP INDEX IF EXISTS idx_invoices_realization_due;
DROP INDEX IF EXISTS idx_invoices_is_posted;

ALTER TABLE invoices DROP COLUMN IF EXISTS realization_due_date;
ALTER TABLE invoices DROP COLUMN IF EXISTS posted_at;
ALTER TABLE invoices DROP COLUMN IF EXISTS posted_journal_id;
ALTER TABLE invoices DROP COLUMN IF EXISTS is_posted;
ALTER TABLE invoices DROP COLUMN IF EXISTS ad_bank_name;
ALTER TABLE invoices DROP COLUMN IF EXISTS purpose_code;
ALTER TABLE invoices DROP COLUMN IF EXISTS lut_valid_to;
ALTER TABLE invoices DROP COLUMN IF EXISTS lut_valid_from;
ALTER TABLE invoices DROP COLUMN IF EXISTS lut_number;
ALTER TABLE invoices DROP COLUMN IF EXISTS invoice_amount_inr;
ALTER TABLE invoices DROP COLUMN IF EXISTS invoice_exchange_rate;

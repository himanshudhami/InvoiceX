-- Rollback migration for credit notes

-- Drop triggers
DROP TRIGGER IF EXISTS credit_note_invoice_total_trigger ON credit_notes;
DROP TRIGGER IF EXISTS credit_note_time_barred_trigger ON credit_notes;

-- Drop functions
DROP FUNCTION IF EXISTS update_invoice_credit_note_total();
DROP FUNCTION IF EXISTS set_credit_note_time_barred();
DROP FUNCTION IF EXISTS calculate_time_barred_date(DATE);

-- Remove columns from invoices
ALTER TABLE invoices DROP COLUMN IF EXISTS credit_note_total;
ALTER TABLE invoices DROP COLUMN IF EXISTS has_credit_notes;

-- Drop tables (items first due to foreign key)
DROP TABLE IF EXISTS credit_note_items;
DROP TABLE IF EXISTS credit_notes;

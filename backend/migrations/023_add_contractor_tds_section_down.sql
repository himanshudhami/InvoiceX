-- Rollback: Remove contractor TDS section fields

DROP INDEX IF EXISTS idx_contractor_payments_pan;

ALTER TABLE contractor_payments
DROP COLUMN IF EXISTS tds_section,
DROP COLUMN IF EXISTS contractor_pan,
DROP COLUMN IF EXISTS pan_verified;

-- Migration: 131_add_missing_tally_columns.sql
-- Description: Add missing tally_voucher_type column to payments and other tables

-- Add tally_voucher_type to payments table
ALTER TABLE payments ADD COLUMN IF NOT EXISTS tally_voucher_type VARCHAR(50);

-- Add tally_voucher_type to vendor_payments table (if missing)
ALTER TABLE vendor_payments ADD COLUMN IF NOT EXISTS tally_voucher_type VARCHAR(50);
ALTER TABLE vendor_payments ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100);
ALTER TABLE vendor_payments ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(100);
ALTER TABLE vendor_payments ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

-- Add indices for tally columns
CREATE INDEX IF NOT EXISTS idx_vendor_payments_tally ON vendor_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

COMMENT ON COLUMN payments.tally_voucher_type IS 'Tally voucher type (Receipt, Payment, etc.)';
COMMENT ON COLUMN vendor_payments.tally_voucher_type IS 'Tally voucher type (Payment, etc.)';

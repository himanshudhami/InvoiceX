-- 136_statutory_payments_tally_fields.sql
-- Add Tally migration tracking fields to statutory_payments

ALTER TABLE statutory_payments
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id);

-- Index for duplicate detection during import
CREATE INDEX IF NOT EXISTS idx_statutory_payments_tally_guid
ON statutory_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

-- Index for batch queries
CREATE INDEX IF NOT EXISTS idx_statutory_payments_tally_batch
ON statutory_payments(tally_migration_batch_id) WHERE tally_migration_batch_id IS NOT NULL;

-- 135_contractor_payments_tally_fields.sql
-- Add Tally migration tracking fields to contractor_payments and employees

-- ==================== contractor_payments ====================

-- Add Tally tracking columns
ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id);

-- Index for duplicate detection during import
CREATE INDEX IF NOT EXISTS idx_contractor_payments_tally_guid
ON contractor_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

-- Index for batch queries
CREATE INDEX IF NOT EXISTS idx_contractor_payments_tally_batch
ON contractor_payments(tally_migration_batch_id) WHERE tally_migration_batch_id IS NOT NULL;

-- ==================== employees ====================

-- Add employment_type for distinguishing contractors
ALTER TABLE employees
ADD COLUMN IF NOT EXISTS employment_type VARCHAR(20) DEFAULT 'employee';

-- Add Tally ledger GUID for migration tracking
ALTER TABLE employees
ADD COLUMN IF NOT EXISTS tally_ledger_guid VARCHAR(100);

-- Index for Tally lookup
CREATE INDEX IF NOT EXISTS idx_employees_tally_guid
ON employees(tally_ledger_guid) WHERE tally_ledger_guid IS NOT NULL;

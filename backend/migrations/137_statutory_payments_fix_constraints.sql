-- Migration: Fix statutory_payments constraints for Indian compliance
-- The period-based unique constraint is incorrect for Indian statutory payments
-- Multiple payments per period are valid: annual PT, multiple challans, reversals, arrears

-- Drop the problematic period-based unique constraint
DROP INDEX IF EXISTS idx_statutory_payments_unique;

-- Add payment_category for sub-types (annual, arrear, penalty, etc.)
ALTER TABLE statutory_payments
ADD COLUMN IF NOT EXISTS payment_category VARCHAR(30) DEFAULT 'regular';

COMMENT ON COLUMN statutory_payments.payment_category IS
'Payment sub-type: regular, annual, arrear, penalty, interest, revision';

-- Add reference-based uniqueness (prevents true duplicates while allowing multiple per period)
-- Uses reference_number which contains challan/TRRN/bank reference
CREATE UNIQUE INDEX IF NOT EXISTS idx_statutory_payments_reference
ON statutory_payments(company_id, payment_type, reference_number)
WHERE reference_number IS NOT NULL;

-- Keep Tally GUID uniqueness for import deduplication
CREATE UNIQUE INDEX IF NOT EXISTS idx_statutory_payments_tally_guid
ON statutory_payments(company_id, tally_voucher_guid)
WHERE tally_voucher_guid IS NOT NULL;

-- Add index for common queries (period-based reporting)
CREATE INDEX IF NOT EXISTS idx_statutory_payments_period
ON statutory_payments(company_id, payment_type, financial_year, period_month, period_year);

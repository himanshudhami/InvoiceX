-- Add matched entity fields for Tally import linking
-- Separate from reconciliation fields (reconciled_* is for bank statement matching)
-- matched_entity_* tracks which business entity this bank txn was imported from

ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS matched_entity_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS matched_entity_id UUID;

-- Index for efficient lookups by matched entity
CREATE INDEX IF NOT EXISTS idx_bank_transactions_matched_entity
ON bank_transactions(matched_entity_type, matched_entity_id)
WHERE matched_entity_id IS NOT NULL;

-- Comments explaining the distinction
COMMENT ON COLUMN bank_transactions.matched_entity_type IS
    'Source entity type from Tally import: vendor_payments, contractor_payments, statutory_payments, journal_entries';
COMMENT ON COLUMN bank_transactions.matched_entity_id IS
    'Source entity ID from Tally import - the business entity this bank transaction was created from';

-- Note: tally_voucher_guid, tally_voucher_number, tally_migration_batch_id already exist

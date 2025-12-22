-- Rollback: Remove reversal pairing support from bank_transactions

-- Drop indexes first
DROP INDEX IF EXISTS idx_bank_transactions_paired_transaction_id;
DROP INDEX IF EXISTS idx_bank_transactions_is_reversal;
DROP INDEX IF EXISTS idx_bank_transactions_reversal_je;

-- Drop constraint
ALTER TABLE bank_transactions
DROP CONSTRAINT IF EXISTS chk_pair_type;

-- Drop columns
ALTER TABLE bank_transactions
DROP COLUMN IF EXISTS paired_transaction_id,
DROP COLUMN IF EXISTS pair_type,
DROP COLUMN IF EXISTS reversal_journal_entry_id,
DROP COLUMN IF EXISTS is_reversal_transaction;

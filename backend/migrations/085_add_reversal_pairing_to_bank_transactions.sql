-- Migration: Add reversal pairing support to bank_transactions
-- This enables linking failed transactions with their reversals

-- Add columns for reversal pairing
ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS paired_transaction_id UUID REFERENCES bank_transactions(id),
ADD COLUMN IF NOT EXISTS pair_type VARCHAR(20), -- 'original' or 'reversal'
ADD COLUMN IF NOT EXISTS reversal_journal_entry_id UUID REFERENCES journal_entries(id),
ADD COLUMN IF NOT EXISTS is_reversal_transaction BOOLEAN DEFAULT FALSE;

-- Add constraint to ensure pair_type is valid
ALTER TABLE bank_transactions
ADD CONSTRAINT chk_pair_type
CHECK (pair_type IS NULL OR pair_type IN ('original', 'reversal'));

-- Add index for faster lookups of paired transactions
CREATE INDEX IF NOT EXISTS idx_bank_transactions_paired_transaction_id
ON bank_transactions(paired_transaction_id)
WHERE paired_transaction_id IS NOT NULL;

-- Add index for finding reversal transactions
CREATE INDEX IF NOT EXISTS idx_bank_transactions_is_reversal
ON bank_transactions(is_reversal_transaction)
WHERE is_reversal_transaction = TRUE;

-- Add index for finding transactions with reversal journal entries
CREATE INDEX IF NOT EXISTS idx_bank_transactions_reversal_je
ON bank_transactions(reversal_journal_entry_id)
WHERE reversal_journal_entry_id IS NOT NULL;

-- Comment on columns
COMMENT ON COLUMN bank_transactions.paired_transaction_id IS
'Links original failed transaction with its reversal. Both point to each other.';

COMMENT ON COLUMN bank_transactions.pair_type IS
'original = the failed outgoing payment, reversal = the credit that cancelled it';

COMMENT ON COLUMN bank_transactions.reversal_journal_entry_id IS
'If original was posted to ledger, this is the reversal JE that corrects it';

COMMENT ON COLUMN bank_transactions.is_reversal_transaction IS
'Auto-detected based on description patterns like REV-, REVERSAL, etc.';

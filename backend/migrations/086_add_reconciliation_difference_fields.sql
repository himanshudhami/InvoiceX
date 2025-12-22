-- Migration: Add reconciliation difference handling fields to bank_transactions
-- This enables tracking of differences between bank transactions and reconciled records
-- Following ICAI guidelines for bank reconciliation adjusting entries

-- Add columns for reconciliation difference tracking
ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS reconciliation_difference_amount DECIMAL(18,2),
ADD COLUMN IF NOT EXISTS reconciliation_difference_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS reconciliation_difference_notes VARCHAR(500),
ADD COLUMN IF NOT EXISTS reconciliation_tds_section VARCHAR(20),
ADD COLUMN IF NOT EXISTS reconciliation_adjustment_journal_id UUID REFERENCES journal_entries(id);

-- Add index for finding transactions with adjustment journals
CREATE INDEX IF NOT EXISTS idx_bank_transactions_adjustment_je
ON bank_transactions(reconciliation_adjustment_journal_id)
WHERE reconciliation_adjustment_journal_id IS NOT NULL;

-- Add index for finding transactions by difference type
CREATE INDEX IF NOT EXISTS idx_bank_transactions_diff_type
ON bank_transactions(reconciliation_difference_type)
WHERE reconciliation_difference_type IS NOT NULL;

-- Comments
COMMENT ON COLUMN bank_transactions.reconciliation_difference_amount IS
'Amount difference between bank transaction and reconciled record. Positive = bank received more, Negative = bank received less';

COMMENT ON COLUMN bank_transactions.reconciliation_difference_type IS
'Classification: bank_interest, bank_charges, tds_deducted, round_off, forex_gain, forex_loss, other_income, other_expense, suspense';

COMMENT ON COLUMN bank_transactions.reconciliation_difference_notes IS
'Optional notes explaining the reconciliation difference';

COMMENT ON COLUMN bank_transactions.reconciliation_tds_section IS
'TDS section if difference type is tds_deducted (e.g., 194C, 194J)';

COMMENT ON COLUMN bank_transactions.reconciliation_adjustment_journal_id IS
'Journal entry ID created to account for the difference';

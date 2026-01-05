-- Migration: Add bank transaction reconciliation tracking to expense_claims

-- Add reconciliation columns to expense_claims
ALTER TABLE expense_claims
ADD COLUMN IF NOT EXISTS bank_transaction_id UUID REFERENCES bank_transactions(id),
ADD COLUMN IF NOT EXISTS reconciled_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS reconciled_by VARCHAR(255);

-- Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_expense_bank_txn ON expense_claims(bank_transaction_id) WHERE bank_transaction_id IS NOT NULL;

-- Comments for documentation
COMMENT ON COLUMN expense_claims.bank_transaction_id IS 'Reference to the bank transaction this expense reimbursement is reconciled with';
COMMENT ON COLUMN expense_claims.reconciled_at IS 'Timestamp when the reconciliation was done';
COMMENT ON COLUMN expense_claims.reconciled_by IS 'User who performed the reconciliation';

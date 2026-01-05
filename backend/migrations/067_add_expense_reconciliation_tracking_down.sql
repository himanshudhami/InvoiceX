-- Rollback migration: Remove bank transaction reconciliation tracking from expense_claims

-- Drop index first
DROP INDEX IF EXISTS idx_expense_bank_txn;

-- Remove columns from expense_claims
ALTER TABLE expense_claims
DROP COLUMN IF EXISTS bank_transaction_id,
DROP COLUMN IF EXISTS reconciled_at,
DROP COLUMN IF EXISTS reconciled_by;

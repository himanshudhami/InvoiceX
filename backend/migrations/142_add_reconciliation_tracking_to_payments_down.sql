-- Rollback: Remove reconciliation tracking columns from payments

DROP INDEX IF EXISTS idx_payments_is_reconciled;

ALTER TABLE payments
DROP COLUMN IF EXISTS is_reconciled,
DROP COLUMN IF EXISTS reconciled_at,
DROP COLUMN IF EXISTS reconciled_by;

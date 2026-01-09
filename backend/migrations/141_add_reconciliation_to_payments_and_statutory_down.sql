-- Rollback: Remove reconciliation tracking from payments and statutory_payments

DROP INDEX IF EXISTS idx_statutory_is_reconciled;
DROP INDEX IF EXISTS idx_statutory_bank_txn;

ALTER TABLE statutory_payments
DROP COLUMN IF EXISTS reconciled_by,
DROP COLUMN IF EXISTS reconciled_at,
DROP COLUMN IF EXISTS is_reconciled,
DROP COLUMN IF EXISTS bank_transaction_id;

DROP INDEX IF EXISTS idx_payments_bank_txn;

ALTER TABLE payments
DROP COLUMN IF EXISTS bank_transaction_id;

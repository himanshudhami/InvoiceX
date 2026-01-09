-- Migration: Add reconciliation tracking columns to payments

ALTER TABLE payments
ADD COLUMN IF NOT EXISTS is_reconciled BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS reconciled_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS reconciled_by VARCHAR(255);

CREATE INDEX IF NOT EXISTS idx_payments_is_reconciled
    ON payments(is_reconciled)
    WHERE is_reconciled = false;

COMMENT ON COLUMN payments.is_reconciled IS
    'Whether this payment is reconciled with bank statement';
COMMENT ON COLUMN payments.reconciled_at IS
    'Timestamp when the reconciliation was done';
COMMENT ON COLUMN payments.reconciled_by IS
    'User who performed the reconciliation';

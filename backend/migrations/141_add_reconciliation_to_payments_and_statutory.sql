-- Migration: Add reconciliation tracking to payments and statutory_payments

-- Add bank transaction linkage to payments
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS bank_transaction_id UUID REFERENCES bank_transactions(id);

CREATE INDEX IF NOT EXISTS idx_payments_bank_txn
    ON payments(bank_transaction_id)
    WHERE bank_transaction_id IS NOT NULL;

COMMENT ON COLUMN payments.bank_transaction_id IS
    'Reference to the bank transaction this payment is reconciled with';

-- Add reconciliation tracking to statutory_payments
ALTER TABLE statutory_payments
ADD COLUMN IF NOT EXISTS bank_transaction_id UUID REFERENCES bank_transactions(id),
ADD COLUMN IF NOT EXISTS is_reconciled BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS reconciled_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS reconciled_by VARCHAR(255);

CREATE INDEX IF NOT EXISTS idx_statutory_bank_txn
    ON statutory_payments(bank_transaction_id)
    WHERE bank_transaction_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_statutory_is_reconciled
    ON statutory_payments(company_id, is_reconciled);

COMMENT ON COLUMN statutory_payments.bank_transaction_id IS
    'Reference to the bank transaction this statutory payment is reconciled with';
COMMENT ON COLUMN statutory_payments.is_reconciled IS
    'Whether the statutory payment is reconciled with bank statement';
COMMENT ON COLUMN statutory_payments.reconciled_at IS
    'Timestamp when the reconciliation was done';
COMMENT ON COLUMN statutory_payments.reconciled_by IS
    'User who performed the reconciliation';

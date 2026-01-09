-- Migration: Add source_voucher_type to bank_transactions
-- Purpose: Track the originating Tally voucher type for audit trail
-- Values: 'payment', 'receipt', 'contra', 'journal', 'sales', 'purchase'

ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS source_voucher_type VARCHAR(20);

-- Add index for filtering by voucher type
CREATE INDEX IF NOT EXISTS idx_bank_tx_source_voucher_type
ON bank_transactions(source_voucher_type);

-- Comment for documentation
COMMENT ON COLUMN bank_transactions.source_voucher_type IS
'Tally voucher type that created this transaction: payment, receipt, contra, journal, sales, purchase';

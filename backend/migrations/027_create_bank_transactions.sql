-- Migration: Create bank_transactions table
-- Phase A: Bank Integration - enables importing and tracking bank statement entries

CREATE TABLE IF NOT EXISTS bank_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bank_account_id UUID NOT NULL REFERENCES bank_accounts(id) ON DELETE CASCADE,

    -- Transaction details
    transaction_date DATE NOT NULL,
    value_date DATE,
    description TEXT,
    reference_number VARCHAR(255),
    cheque_number VARCHAR(50),

    -- Amount
    transaction_type VARCHAR(20) NOT NULL, -- 'credit', 'debit'
    amount DECIMAL(18,2) NOT NULL,
    balance_after DECIMAL(18,2),

    -- Categorization
    category VARCHAR(100), -- 'customer_payment', 'vendor_payment', 'salary', 'tax', 'bank_charges', 'transfer', 'other'

    -- Reconciliation
    is_reconciled BOOLEAN DEFAULT false,
    reconciled_type VARCHAR(50), -- 'payment', 'expense', 'payroll', 'tax_payment', 'transfer', 'contractor'
    reconciled_id UUID, -- ID of linked payment/expense/payroll record
    reconciled_at TIMESTAMP,
    reconciled_by VARCHAR(255),

    -- Import tracking
    import_source VARCHAR(100) DEFAULT 'manual', -- 'manual', 'csv', 'pdf', 'api'
    import_batch_id UUID,
    raw_data JSONB, -- Store original imported row for audit

    -- Duplicate detection
    transaction_hash VARCHAR(64), -- SHA256 hash of date+amount+description for dedup

    -- Timestamps
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_bank_tx_account ON bank_transactions(bank_account_id);
CREATE INDEX IF NOT EXISTS idx_bank_tx_date ON bank_transactions(transaction_date);
CREATE INDEX IF NOT EXISTS idx_bank_tx_reconciled ON bank_transactions(is_reconciled);
CREATE INDEX IF NOT EXISTS idx_bank_tx_hash ON bank_transactions(transaction_hash);
CREATE INDEX IF NOT EXISTS idx_bank_tx_type ON bank_transactions(transaction_type);
CREATE INDEX IF NOT EXISTS idx_bank_tx_category ON bank_transactions(category);
CREATE INDEX IF NOT EXISTS idx_bank_tx_import_batch ON bank_transactions(import_batch_id);

-- Composite index for reconciliation queries
CREATE INDEX IF NOT EXISTS idx_bank_tx_account_unreconciled ON bank_transactions(bank_account_id, is_reconciled)
    WHERE is_reconciled = false;

-- Log migration
DO $$
BEGIN
    RAISE NOTICE 'Created bank_transactions table for bank statement import and reconciliation';
END $$;

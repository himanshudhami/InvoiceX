-- Migration: Create bank_accounts table
-- Phase A: Bank Integration - enables tracking company bank accounts for reconciliation

CREATE TABLE IF NOT EXISTS bank_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,

    -- Account details
    account_name VARCHAR(255) NOT NULL,
    account_number VARCHAR(50) NOT NULL,
    bank_name VARCHAR(255) NOT NULL,
    ifsc_code VARCHAR(20),
    branch_name VARCHAR(255),

    -- Account classification
    account_type VARCHAR(50) DEFAULT 'current', -- 'current', 'savings', 'cc', 'foreign'
    currency VARCHAR(10) DEFAULT 'INR',

    -- Balance tracking
    opening_balance DECIMAL(18,2) DEFAULT 0,
    current_balance DECIMAL(18,2) DEFAULT 0,
    as_of_date DATE,

    -- Status flags
    is_primary BOOLEAN DEFAULT false,
    is_active BOOLEAN DEFAULT true,

    -- Additional info
    notes TEXT,

    -- Timestamps
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_bank_accounts_company ON bank_accounts(company_id);
CREATE INDEX IF NOT EXISTS idx_bank_accounts_active ON bank_accounts(is_active);
CREATE INDEX IF NOT EXISTS idx_bank_accounts_primary ON bank_accounts(company_id, is_primary) WHERE is_primary = true;

-- Add unique constraint on account number per company to prevent duplicates
CREATE UNIQUE INDEX IF NOT EXISTS idx_bank_accounts_unique_number ON bank_accounts(company_id, account_number);

-- Log migration
DO $$
BEGIN
    RAISE NOTICE 'Created bank_accounts table for bank reconciliation';
END $$;

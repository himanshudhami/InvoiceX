-- Migration: Create forex_transactions table for tracking forex bookings, settlements, and revaluations
-- Purpose: Ind AS 21 compliance - track exchange differences on foreign currency transactions

CREATE TABLE IF NOT EXISTS forex_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Transaction reference
    transaction_date DATE NOT NULL,
    financial_year VARCHAR(10) NOT NULL,          -- e.g., "2025-26"

    -- Source document
    source_type VARCHAR(50) NOT NULL,             -- invoice, payment, revaluation
    source_id UUID,                               -- FK to source document
    source_number VARCHAR(100),                   -- invoice number, payment reference

    -- Currency details
    currency VARCHAR(3) NOT NULL,                 -- USD, EUR, GBP, etc.
    foreign_amount NUMERIC(18,2) NOT NULL,        -- Amount in foreign currency
    exchange_rate NUMERIC(18,6) NOT NULL,         -- Rate used for conversion
    inr_amount NUMERIC(18,2) NOT NULL,            -- Equivalent INR amount

    -- Transaction classification
    transaction_type VARCHAR(20) NOT NULL,        -- booking, settlement, revaluation

    -- Forex gain/loss (populated on settlement/revaluation)
    forex_gain_loss NUMERIC(18,2),                -- Positive = gain, Negative = loss
    gain_loss_type VARCHAR(20),                   -- realized, unrealized

    -- Related forex transaction (for settlement linking to booking)
    related_forex_id UUID REFERENCES forex_transactions(id),

    -- Ledger posting
    journal_entry_id UUID REFERENCES journal_entries(id),
    is_posted BOOLEAN DEFAULT FALSE,

    -- Audit
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID
);

-- Indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_forex_company ON forex_transactions(company_id);
CREATE INDEX IF NOT EXISTS idx_forex_source ON forex_transactions(source_type, source_id);
CREATE INDEX IF NOT EXISTS idx_forex_date ON forex_transactions(transaction_date);
CREATE INDEX IF NOT EXISTS idx_forex_currency ON forex_transactions(currency);
CREATE INDEX IF NOT EXISTS idx_forex_type ON forex_transactions(transaction_type);
CREATE INDEX IF NOT EXISTS idx_forex_unposted ON forex_transactions(is_posted) WHERE is_posted = FALSE;
CREATE INDEX IF NOT EXISTS idx_forex_fy ON forex_transactions(financial_year);

-- Trigger for updated_at
CREATE OR REPLACE FUNCTION update_forex_transactions_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_update_forex_transactions_updated_at ON forex_transactions;
CREATE TRIGGER trigger_update_forex_transactions_updated_at
    BEFORE UPDATE ON forex_transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_forex_transactions_updated_at();

-- Comments
COMMENT ON TABLE forex_transactions IS 'Tracks all foreign currency transactions for Ind AS 21 compliance';
COMMENT ON COLUMN forex_transactions.transaction_type IS 'booking: initial recognition, settlement: payment received, revaluation: month-end adjustment';
COMMENT ON COLUMN forex_transactions.forex_gain_loss IS 'Exchange difference - positive for gain, negative for loss';
COMMENT ON COLUMN forex_transactions.gain_loss_type IS 'realized: on settlement, unrealized: on revaluation';
COMMENT ON COLUMN forex_transactions.related_forex_id IS 'Links settlement to original booking transaction';

-- Migration: Create TDS Receivable table
-- Phase C: TDS Credit Tracking - enables Form 26AS reconciliation

CREATE TABLE IF NOT EXISTS tds_receivable (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Financial period
    financial_year VARCHAR(10) NOT NULL, -- '2024-25' format
    quarter VARCHAR(5) NOT NULL, -- 'Q1', 'Q2', 'Q3', 'Q4'

    -- Deductor (customer) details
    customer_id UUID REFERENCES customers(id),
    deductor_name VARCHAR(255) NOT NULL,
    deductor_tan VARCHAR(20), -- TAN of the deductor
    deductor_pan VARCHAR(15),

    -- Transaction details
    payment_date DATE NOT NULL,
    tds_section VARCHAR(20) NOT NULL, -- '194J', '194C', '194H', '194O', etc.
    gross_amount DECIMAL(18,2) NOT NULL,
    tds_rate DECIMAL(5,2) NOT NULL,
    tds_amount DECIMAL(18,2) NOT NULL,
    net_received DECIMAL(18,2) NOT NULL,

    -- Certificate details (Form 16A)
    certificate_number VARCHAR(100),
    certificate_date DATE,
    certificate_downloaded BOOLEAN DEFAULT false,

    -- Linked records
    payment_id UUID REFERENCES payments(id),
    invoice_id UUID REFERENCES invoices(id),

    -- 26AS matching
    matched_with_26as BOOLEAN DEFAULT false,
    form_26as_amount DECIMAL(18,2),
    amount_difference DECIMAL(18,2),
    matched_at TIMESTAMP,

    -- Claiming status
    status VARCHAR(50) DEFAULT 'pending',
    -- Values: 'pending', 'matched', 'claimed', 'disputed', 'written_off'
    claimed_in_return VARCHAR(50), -- 'ITR-2024-25'

    -- Additional info
    notes TEXT,

    -- Timestamps
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_tds_recv_company_fy ON tds_receivable(company_id, financial_year);
CREATE INDEX IF NOT EXISTS idx_tds_recv_customer ON tds_receivable(customer_id);
CREATE INDEX IF NOT EXISTS idx_tds_recv_status ON tds_receivable(status);
CREATE INDEX IF NOT EXISTS idx_tds_recv_quarter ON tds_receivable(financial_year, quarter);
CREATE INDEX IF NOT EXISTS idx_tds_recv_payment ON tds_receivable(payment_id) WHERE payment_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_tds_recv_section ON tds_receivable(tds_section);
CREATE INDEX IF NOT EXISTS idx_tds_recv_matched ON tds_receivable(matched_with_26as);

-- Composite index for unmatched TDS lookup
CREATE INDEX IF NOT EXISTS idx_tds_recv_unmatched ON tds_receivable(company_id, financial_year, matched_with_26as)
    WHERE matched_with_26as = false;

-- Log migration
DO $$
BEGIN
    RAISE NOTICE 'Created tds_receivable table for TDS credit tracking and Form 26AS reconciliation';
END $$;

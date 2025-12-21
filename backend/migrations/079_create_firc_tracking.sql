-- Migration: Create firc_tracking table for FEMA/RBI compliance
-- Purpose: Track Foreign Inward Remittance Certificates for export payments

CREATE TABLE IF NOT EXISTS firc_tracking (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- FIRC details
    firc_number VARCHAR(50),                      -- FIRC reference number from bank
    firc_date DATE,                               -- Date on FIRC
    bank_name VARCHAR(100) NOT NULL,              -- AD Bank name
    bank_branch VARCHAR(100),                     -- Branch name
    bank_swift_code VARCHAR(20),                  -- SWIFT/BIC code

    -- Remittance details
    purpose_code VARCHAR(10) NOT NULL,            -- RBI purpose code (P0802, etc.)
    foreign_currency VARCHAR(3) NOT NULL,         -- USD, EUR, GBP, etc.
    foreign_amount NUMERIC(18,2) NOT NULL,        -- Amount in foreign currency
    inr_amount NUMERIC(18,2) NOT NULL,            -- INR credited to account
    exchange_rate NUMERIC(18,6) NOT NULL,         -- Bank conversion rate

    -- Remitter details
    remitter_name VARCHAR(200),                   -- Who sent the money
    remitter_country VARCHAR(100),                -- Country of origin
    remitter_bank VARCHAR(200),                   -- Remitter's bank

    -- Beneficiary (should match company)
    beneficiary_name VARCHAR(200) NOT NULL,       -- Company name
    beneficiary_account VARCHAR(50),              -- Account credited

    -- Linked records
    payment_id UUID REFERENCES payments(id),      -- Link to payment record

    -- EDPMS compliance
    edpms_reported BOOLEAN DEFAULT FALSE,         -- Reported to RBI EDPMS
    edpms_report_date DATE,                       -- When reported
    edpms_reference VARCHAR(100),                 -- EDPMS reference number

    -- Status
    status VARCHAR(20) DEFAULT 'received',        -- received, linked, reconciled

    -- Audit
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID,
    notes TEXT
);

-- Junction table for FIRC to Invoice mapping (one FIRC can cover multiple invoices)
CREATE TABLE IF NOT EXISTS firc_invoice_links (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    firc_id UUID NOT NULL REFERENCES firc_tracking(id) ON DELETE CASCADE,
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    allocated_amount NUMERIC(18,2) NOT NULL,      -- Amount of FIRC allocated to this invoice
    allocated_amount_inr NUMERIC(18,2),           -- INR equivalent
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(firc_id, invoice_id)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_firc_company ON firc_tracking(company_id);
CREATE INDEX IF NOT EXISTS idx_firc_number ON firc_tracking(firc_number);
CREATE INDEX IF NOT EXISTS idx_firc_date ON firc_tracking(firc_date);
CREATE INDEX IF NOT EXISTS idx_firc_payment ON firc_tracking(payment_id);
CREATE INDEX IF NOT EXISTS idx_firc_status ON firc_tracking(status);
CREATE INDEX IF NOT EXISTS idx_firc_edpms ON firc_tracking(edpms_reported) WHERE edpms_reported = FALSE;
CREATE INDEX IF NOT EXISTS idx_firc_links_firc ON firc_invoice_links(firc_id);
CREATE INDEX IF NOT EXISTS idx_firc_links_invoice ON firc_invoice_links(invoice_id);

-- Trigger for updated_at
CREATE OR REPLACE FUNCTION update_firc_tracking_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_update_firc_tracking_updated_at ON firc_tracking;
CREATE TRIGGER trigger_update_firc_tracking_updated_at
    BEFORE UPDATE ON firc_tracking
    FOR EACH ROW
    EXECUTE FUNCTION update_firc_tracking_updated_at();

-- Comments
COMMENT ON TABLE firc_tracking IS 'Foreign Inward Remittance Certificate tracking for FEMA compliance';
COMMENT ON COLUMN firc_tracking.purpose_code IS 'RBI purpose code - P0802 for software, P0801 for IT services';
COMMENT ON COLUMN firc_tracking.edpms_reported IS 'Whether reported to RBI Export Data Processing and Monitoring System';
COMMENT ON TABLE firc_invoice_links IS 'Links FIRC to invoices - one FIRC can settle multiple invoices';

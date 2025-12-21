-- Migration: Create lut_register table for GST LUT tracking
-- Purpose: Track Letter of Undertaking for zero-rated export supplies

CREATE TABLE IF NOT EXISTS lut_register (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- LUT details
    lut_number VARCHAR(50) NOT NULL,              -- LUT reference number
    financial_year VARCHAR(10) NOT NULL,          -- e.g., "2025-26"
    gstin VARCHAR(15) NOT NULL,                   -- Company GSTIN

    -- Validity period
    valid_from DATE NOT NULL,                     -- Start of validity (usually Apr 1)
    valid_to DATE NOT NULL,                       -- End of validity (usually Mar 31)

    -- Filing details
    filing_date DATE,                             -- Date of LUT filing
    arn VARCHAR(50),                              -- Application Reference Number

    -- Status
    status VARCHAR(20) DEFAULT 'active',          -- active, expired, superseded, cancelled

    -- Audit
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID,
    notes TEXT
);

-- Unique constraint: one active LUT per company per FY
CREATE UNIQUE INDEX IF NOT EXISTS idx_lut_company_fy_active
ON lut_register(company_id, financial_year)
WHERE status = 'active';

-- Indexes
CREATE INDEX IF NOT EXISTS idx_lut_company ON lut_register(company_id);
CREATE INDEX IF NOT EXISTS idx_lut_fy ON lut_register(financial_year);
CREATE INDEX IF NOT EXISTS idx_lut_status ON lut_register(status);
CREATE INDEX IF NOT EXISTS idx_lut_validity ON lut_register(valid_from, valid_to);

-- Trigger for updated_at
CREATE OR REPLACE FUNCTION update_lut_register_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_update_lut_register_updated_at ON lut_register;
CREATE TRIGGER trigger_update_lut_register_updated_at
    BEFORE UPDATE ON lut_register
    FOR EACH ROW
    EXECUTE FUNCTION update_lut_register_updated_at();

-- Comments
COMMENT ON TABLE lut_register IS 'GST Letter of Undertaking register for export without IGST payment';
COMMENT ON COLUMN lut_register.lut_number IS 'LUT reference number from GST portal';
COMMENT ON COLUMN lut_register.arn IS 'Application Reference Number from GST portal';
COMMENT ON COLUMN lut_register.status IS 'active: current LUT, expired: past FY, superseded: replaced by new filing';

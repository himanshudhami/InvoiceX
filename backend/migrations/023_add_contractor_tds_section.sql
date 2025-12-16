-- Migration: Add TDS section support for contractor payments
-- Section 194C: Contractors (2% for individuals, 1% for transporters)
-- Section 194J: Professional/Technical services (10%)
-- If no PAN provided: 20% TDS rate applies

ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS tds_section VARCHAR(10) DEFAULT '194J',
ADD COLUMN IF NOT EXISTS contractor_pan VARCHAR(15),
ADD COLUMN IF NOT EXISTS pan_verified BOOLEAN DEFAULT false;

-- Comments
COMMENT ON COLUMN contractor_payments.tds_section IS 'TDS section: 194C (contractors 2%) or 194J (professionals 10%)';
COMMENT ON COLUMN contractor_payments.contractor_pan IS 'Contractor PAN number for Form 26Q filing';
COMMENT ON COLUMN contractor_payments.pan_verified IS 'Whether PAN has been verified against income tax records';

-- Index for PAN lookups
CREATE INDEX IF NOT EXISTS idx_contractor_payments_pan ON contractor_payments(contractor_pan) WHERE contractor_pan IS NOT NULL;

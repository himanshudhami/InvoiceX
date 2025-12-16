-- Migration: Add senior citizen tax slab support
-- Adds applicable_to_category column to distinguish general, senior (60+), and super-senior (80+) taxpayers

-- Add category column to tax_slabs
ALTER TABLE tax_slabs
ADD COLUMN IF NOT EXISTS applicable_to_category VARCHAR(20) DEFAULT 'all';

-- Update existing slabs to be 'all' (general taxpayers)
UPDATE tax_slabs SET applicable_to_category = 'all' WHERE applicable_to_category IS NULL;

-- Insert senior citizen slabs for Old Regime FY 2024-25
-- Senior Citizens (60-79 years): Basic exemption up to 3 lakh
INSERT INTO tax_slabs (regime, financial_year, min_income, max_income, rate, cess_rate, applicable_to_category, is_active) VALUES
('old', '2024-25', 0, 300000, 0, 4, 'senior', true),
('old', '2024-25', 300001, 500000, 5, 4, 'senior', true),
('old', '2024-25', 500001, 1000000, 20, 4, 'senior', true),
('old', '2024-25', 1000001, NULL, 30, 4, 'senior', true);

-- Super Senior Citizens (80+ years): Basic exemption up to 5 lakh
INSERT INTO tax_slabs (regime, financial_year, min_income, max_income, rate, cess_rate, applicable_to_category, is_active) VALUES
('old', '2024-25', 0, 500000, 0, 4, 'super_senior', true),
('old', '2024-25', 500001, 1000000, 20, 4, 'super_senior', true),
('old', '2024-25', 1000001, NULL, 30, 4, 'super_senior', true);

-- Insert senior citizen slabs for Old Regime FY 2025-26
INSERT INTO tax_slabs (regime, financial_year, min_income, max_income, rate, cess_rate, applicable_to_category, is_active) VALUES
('old', '2025-26', 0, 300000, 0, 4, 'senior', true),
('old', '2025-26', 300001, 500000, 5, 4, 'senior', true),
('old', '2025-26', 500001, 1000000, 20, 4, 'senior', true),
('old', '2025-26', 1000001, NULL, 30, 4, 'senior', true);

INSERT INTO tax_slabs (regime, financial_year, min_income, max_income, rate, cess_rate, applicable_to_category, is_active) VALUES
('old', '2025-26', 0, 500000, 0, 4, 'super_senior', true),
('old', '2025-26', 500001, 1000000, 20, 4, 'super_senior', true),
('old', '2025-26', 1000001, NULL, 30, 4, 'super_senior', true);

-- Note: New Regime does not have separate slabs for senior citizens
-- The same slabs apply to all age groups under new regime

-- Create index for efficient querying
CREATE INDEX IF NOT EXISTS idx_tax_slabs_category ON tax_slabs(regime, financial_year, applicable_to_category);

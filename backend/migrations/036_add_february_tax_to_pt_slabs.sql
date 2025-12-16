-- Migration: Add february_tax column to professional_tax_slabs
-- Some Indian states (Karnataka, Maharashtra) have different PT rates in February
-- to ensure the annual total matches the statutory cap (e.g., Rs 2,500 in Karnataka)

-- Add february_tax column
ALTER TABLE professional_tax_slabs
ADD COLUMN IF NOT EXISTS february_tax NUMERIC(10,2);

-- Add effective_from and effective_to for better slab versioning
ALTER TABLE professional_tax_slabs
ADD COLUMN IF NOT EXISTS effective_from DATE;

ALTER TABLE professional_tax_slabs
ADD COLUMN IF NOT EXISTS effective_to DATE;

-- Update Karnataka February rate (Rs 300 in Feb, Rs 200 other months = Rs 2,500 annual)
UPDATE professional_tax_slabs
SET february_tax = 300
WHERE state = 'Karnataka' AND monthly_tax = 200;

-- Update Maharashtra February rate (Rs 300 in Feb for salary > Rs 10,000)
UPDATE professional_tax_slabs
SET february_tax = 300
WHERE state = 'Maharashtra' AND monthly_tax = 200;

-- Add index for effective date filtering
CREATE INDEX IF NOT EXISTS idx_pt_slabs_effective ON professional_tax_slabs(effective_from, effective_to);

-- Add comment
COMMENT ON COLUMN professional_tax_slabs.february_tax IS 'Special PT amount for February month (some states have different rates to meet annual cap)';
COMMENT ON COLUMN professional_tax_slabs.effective_from IS 'Date from which this slab is effective';
COMMENT ON COLUMN professional_tax_slabs.effective_to IS 'Date until which this slab is effective (NULL = currently active)';

-- 017_add_employee_compliance_fields.sql
-- Add compliance fields to employee_payroll_info for Rule 1 - Master Data

-- Residential status for tax calculation (resident, non_resident, rnor)
ALTER TABLE employee_payroll_info
ADD COLUMN IF NOT EXISTS residential_status VARCHAR(20) DEFAULT 'resident'
    CHECK (residential_status IN ('resident', 'non_resident', 'rnor'));

-- Date of birth for senior citizen determination (affects tax rebates and deductions)
ALTER TABLE employee_payroll_info
ADD COLUMN IF NOT EXISTS date_of_birth DATE;

-- Date when tax regime choice takes effect (allows mid-year regime changes to be tracked)
ALTER TABLE employee_payroll_info
ADD COLUMN IF NOT EXISTS tax_regime_effective_from DATE;

-- Work state for professional tax calculation (different states have different PT slabs)
ALTER TABLE employee_payroll_info
ADD COLUMN IF NOT EXISTS work_state VARCHAR(50);

-- Index for work_state (used in PT calculation lookups)
CREATE INDEX IF NOT EXISTS idx_employee_payroll_work_state ON employee_payroll_info(work_state);

-- Comments for documentation
COMMENT ON COLUMN employee_payroll_info.residential_status IS 'Tax residency status: resident (default), non_resident, rnor (Resident but Not Ordinarily Resident)';
COMMENT ON COLUMN employee_payroll_info.date_of_birth IS 'Date of birth for senior citizen tax benefits (60+ for senior, 80+ for super senior)';
COMMENT ON COLUMN employee_payroll_info.tax_regime_effective_from IS 'Date from which current tax regime applies (for audit trail)';
COMMENT ON COLUMN employee_payroll_info.work_state IS 'State where employee works - determines professional tax slab to apply';

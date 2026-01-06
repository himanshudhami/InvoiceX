-- Migration: 118_column_388a_other_tds_tcs.sql
-- Description: Add Column 388A fields for other TDS/TCS credits in Form 24Q
-- Reference: CBDT Circular dated February 20, 2025
-- Date: 2025-01-06

-- =====================================================================
-- Column 388A: Other TDS/TCS Credits
-- =====================================================================
-- This allows employees to declare TDS/TCS credits from sources other
-- than current employer salary, which can be adjusted against salary TDS.
-- Examples:
--   - TDS on FD interest (Section 194A)
--   - TDS on dividends (Section 194)
--   - TCS on foreign travel/remittance (Section 206C)
--   - TDS from previous employer (if not already captured)
-- =====================================================================

-- Add other TDS/TCS credit fields to employee_tax_declarations
ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS other_tds_interest NUMERIC(18,2) DEFAULT 0;

ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS other_tds_dividend NUMERIC(18,2) DEFAULT 0;

ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS other_tds_commission NUMERIC(18,2) DEFAULT 0;

ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS other_tds_rent NUMERIC(18,2) DEFAULT 0;

ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS other_tds_professional NUMERIC(18,2) DEFAULT 0;

ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS other_tds_others NUMERIC(18,2) DEFAULT 0;

-- TCS credits
ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS tcs_foreign_remittance NUMERIC(18,2) DEFAULT 0;

ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS tcs_overseas_tour NUMERIC(18,2) DEFAULT 0;

ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS tcs_vehicle_purchase NUMERIC(18,2) DEFAULT 0;

ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS tcs_others NUMERIC(18,2) DEFAULT 0;

-- JSON field for detailed breakdown with source information
ALTER TABLE employee_tax_declarations
ADD COLUMN IF NOT EXISTS other_tds_tcs_details JSONB;

-- Add comments for documentation
COMMENT ON COLUMN employee_tax_declarations.other_tds_interest IS
    'TDS on interest income (FD, RD) from banks/NBFCs - Section 194A';

COMMENT ON COLUMN employee_tax_declarations.other_tds_dividend IS
    'TDS on dividend income - Section 194';

COMMENT ON COLUMN employee_tax_declarations.other_tds_commission IS
    'TDS on commission/brokerage - Section 194H';

COMMENT ON COLUMN employee_tax_declarations.other_tds_rent IS
    'TDS on rental income - Section 194I';

COMMENT ON COLUMN employee_tax_declarations.other_tds_professional IS
    'TDS on professional/technical fees - Section 194J';

COMMENT ON COLUMN employee_tax_declarations.other_tds_others IS
    'TDS from any other sources not listed above';

COMMENT ON COLUMN employee_tax_declarations.tcs_foreign_remittance IS
    'TCS on foreign remittance under LRS - Section 206C(1G)';

COMMENT ON COLUMN employee_tax_declarations.tcs_overseas_tour IS
    'TCS on overseas tour packages - Section 206C(1G)';

COMMENT ON COLUMN employee_tax_declarations.tcs_vehicle_purchase IS
    'TCS on motor vehicle purchase > 10 lakhs - Section 206C(1F)';

COMMENT ON COLUMN employee_tax_declarations.tcs_others IS
    'TCS from any other sources';

COMMENT ON COLUMN employee_tax_declarations.other_tds_tcs_details IS
    'JSON array with detailed breakdown: [{section, deductorTan, amount, certificateNo}]';

-- Create view for total 388A credit per employee
CREATE OR REPLACE VIEW v_employee_other_tds_tcs AS
SELECT
    id as declaration_id,
    employee_id,
    financial_year,
    -- TDS credits
    COALESCE(other_tds_interest, 0) as tds_interest,
    COALESCE(other_tds_dividend, 0) as tds_dividend,
    COALESCE(other_tds_commission, 0) as tds_commission,
    COALESCE(other_tds_rent, 0) as tds_rent,
    COALESCE(other_tds_professional, 0) as tds_professional,
    COALESCE(other_tds_others, 0) as tds_others,
    -- TCS credits
    COALESCE(tcs_foreign_remittance, 0) as tcs_foreign_remittance,
    COALESCE(tcs_overseas_tour, 0) as tcs_overseas_tour,
    COALESCE(tcs_vehicle_purchase, 0) as tcs_vehicle,
    COALESCE(tcs_others, 0) as tcs_others,
    -- Totals
    COALESCE(other_tds_interest, 0) +
    COALESCE(other_tds_dividend, 0) +
    COALESCE(other_tds_commission, 0) +
    COALESCE(other_tds_rent, 0) +
    COALESCE(other_tds_professional, 0) +
    COALESCE(other_tds_others, 0) as total_other_tds,

    COALESCE(tcs_foreign_remittance, 0) +
    COALESCE(tcs_overseas_tour, 0) +
    COALESCE(tcs_vehicle_purchase, 0) +
    COALESCE(tcs_others, 0) as total_tcs,

    COALESCE(other_tds_interest, 0) +
    COALESCE(other_tds_dividend, 0) +
    COALESCE(other_tds_commission, 0) +
    COALESCE(other_tds_rent, 0) +
    COALESCE(other_tds_professional, 0) +
    COALESCE(other_tds_others, 0) +
    COALESCE(tcs_foreign_remittance, 0) +
    COALESCE(tcs_overseas_tour, 0) +
    COALESCE(tcs_vehicle_purchase, 0) +
    COALESCE(tcs_others, 0) as total_column_388a
FROM employee_tax_declarations;

-- Add comment to view
COMMENT ON VIEW v_employee_other_tds_tcs IS
    'View for Column 388A calculation - Other TDS/TCS credits to be adjusted against salary TDS';

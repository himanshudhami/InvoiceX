-- Migration: Add PF calculation modes and trust types
-- Purpose: Enable flexible PF calculation (ceiling-based vs actual wage) and support for private PF trusts

-- Step 1: Add PF calculation mode column
-- Modes:
--   'ceiling_based' - 12% of PF wage capped at ceiling (default, current behavior)
--   'actual_wage' - 12% of actual PF wage (no ceiling)
--   'restricted_pf' - For employees earning >15k who opt for lower PF
ALTER TABLE company_statutory_configs
    ADD COLUMN IF NOT EXISTS pf_calculation_mode VARCHAR(20) DEFAULT 'ceiling_based';

-- Step 2: Add PF trust type column
-- Types:
--   'epfo' - Government EPFO (default)
--   'private_trust' - Private PF trust
ALTER TABLE company_statutory_configs
    ADD COLUMN IF NOT EXISTS pf_trust_type VARCHAR(20) DEFAULT 'epfo';

-- Step 3: Add private trust details (only used when pf_trust_type = 'private_trust')
ALTER TABLE company_statutory_configs
    ADD COLUMN IF NOT EXISTS pf_trust_name VARCHAR(255);

ALTER TABLE company_statutory_configs
    ADD COLUMN IF NOT EXISTS pf_trust_registration_number VARCHAR(50);

-- Step 4: Add restricted PF max wage (used when pf_calculation_mode = 'restricted_pf')
ALTER TABLE company_statutory_configs
    ADD COLUMN IF NOT EXISTS restricted_pf_max_wage NUMERIC(12,2) DEFAULT 15000.00;

-- Step 5: Add check constraints for valid values (drop first if exists to make idempotent)
ALTER TABLE company_statutory_configs
    DROP CONSTRAINT IF EXISTS chk_pf_calculation_mode;

ALTER TABLE company_statutory_configs
    ADD CONSTRAINT chk_pf_calculation_mode
    CHECK (pf_calculation_mode IN ('ceiling_based', 'actual_wage', 'restricted_pf'));

ALTER TABLE company_statutory_configs
    DROP CONSTRAINT IF EXISTS chk_pf_trust_type;

ALTER TABLE company_statutory_configs
    ADD CONSTRAINT chk_pf_trust_type
    CHECK (pf_trust_type IN ('epfo', 'private_trust'));

-- Step 6: Add employee-level PF opt-in for restricted mode
-- This allows employees earning >15k to opt for PF on statutory minimum
ALTER TABLE employee_payroll_info
    ADD COLUMN IF NOT EXISTS opted_for_restricted_pf BOOLEAN DEFAULT FALSE;

-- Add comment for documentation
COMMENT ON COLUMN company_statutory_configs.pf_calculation_mode IS
    'PF calculation mode: ceiling_based (12% of capped wage), actual_wage (12% of full wage), restricted_pf (employee opt-in for lower PF)';

COMMENT ON COLUMN company_statutory_configs.pf_trust_type IS
    'PF trust type: epfo (government) or private_trust';

COMMENT ON COLUMN employee_payroll_info.opted_for_restricted_pf IS
    'For restricted_pf mode: if true, employee PF is calculated on statutory minimum instead of full wage';

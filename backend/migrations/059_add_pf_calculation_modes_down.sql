-- Down migration: Remove PF calculation modes and trust types

BEGIN;

-- Remove employee opt-in column
ALTER TABLE employee_payroll_info
    DROP COLUMN IF EXISTS opted_for_restricted_pf;

-- Remove check constraints
ALTER TABLE company_statutory_configs
    DROP CONSTRAINT IF EXISTS chk_pf_calculation_mode;

ALTER TABLE company_statutory_configs
    DROP CONSTRAINT IF EXISTS chk_pf_trust_type;

-- Remove PF-related columns
ALTER TABLE company_statutory_configs
    DROP COLUMN IF EXISTS pf_calculation_mode;

ALTER TABLE company_statutory_configs
    DROP COLUMN IF EXISTS pf_trust_type;

ALTER TABLE company_statutory_configs
    DROP COLUMN IF EXISTS pf_trust_name;

ALTER TABLE company_statutory_configs
    DROP COLUMN IF EXISTS pf_trust_registration_number;

ALTER TABLE company_statutory_configs
    DROP COLUMN IF EXISTS restricted_pf_max_wage;

COMMIT;

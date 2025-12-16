-- 017_add_employee_compliance_fields_down.sql
-- Rollback: Remove employee compliance fields

DROP INDEX IF EXISTS idx_employee_payroll_work_state;

ALTER TABLE employee_payroll_info
DROP COLUMN IF EXISTS residential_status,
DROP COLUMN IF EXISTS date_of_birth,
DROP COLUMN IF EXISTS tax_regime_effective_from,
DROP COLUMN IF EXISTS work_state;

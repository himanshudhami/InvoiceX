-- 013_create_payroll_schema_down.sql
-- Rollback script for payroll schema

DROP TABLE IF EXISTS contractor_payments CASCADE;
DROP TABLE IF EXISTS payroll_transactions CASCADE;
DROP TABLE IF EXISTS payroll_runs CASCADE;
DROP TABLE IF EXISTS employee_tax_declarations CASCADE;
DROP TABLE IF EXISTS employee_salary_structures CASCADE;
DROP TABLE IF EXISTS employee_payroll_info CASCADE;
DROP TABLE IF EXISTS company_statutory_configs CASCADE;
DROP TABLE IF EXISTS professional_tax_slabs CASCADE;
DROP TABLE IF EXISTS tax_slabs CASCADE;

-- Note: This does NOT restore the old employee_salary_transactions table
-- as we are starting fresh

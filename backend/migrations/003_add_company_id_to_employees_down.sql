-- 003_add_company_id_to_employees_down.sql
-- Rolls back the company_id column addition to employees table

-- Drop index
DROP INDEX IF EXISTS idx_employees_company_id;

-- Remove company_id column
ALTER TABLE employees DROP COLUMN IF EXISTS company_id;



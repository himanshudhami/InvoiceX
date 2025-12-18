-- 009_add_company_id_to_salary_transactions_down.sql
-- Rollback migration to remove company_id from employee_salary_transactions

DROP INDEX IF EXISTS idx_employee_salary_transactions_company_id;

ALTER TABLE employee_salary_transactions
    DROP COLUMN IF EXISTS company_id;




